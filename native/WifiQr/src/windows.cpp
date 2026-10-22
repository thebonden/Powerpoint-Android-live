// SPDX-License-Identifier: GPL-2.0-or-later
#include "windows.hpp"
#include <bcrypt.h>
#include <fstream>
#include <filesystem>
#include <iomanip>
#include <chrono>

namespace au {
void check(HRESULT hr, const char *operation) {
    if (FAILED(hr)) {
        std::ostringstream s; s << operation << " (HRESULT 0x" << std::hex << uint32_t(hr) << ")";
        throw std::runtime_error(s.str());
    }
}
std::wstring wide(const std::string &s) {
    if (s.empty()) return {};
    int n = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, s.data(), int(s.size()), nullptr, 0);
    if (!n) throw std::runtime_error("Invalid UTF-8 path");
    std::wstring result(n, 0);
    MultiByteToWideChar(CP_UTF8, 0, s.data(), int(s.size()), result.data(), n); return result;
}
std::string utf8(const std::wstring &s) {
    if (s.empty()) return {};
    int n = WideCharToMultiByte(CP_UTF8, 0, s.data(), int(s.size()), nullptr, 0, nullptr, nullptr);
    std::string result(n, 0);
    WideCharToMultiByte(CP_UTF8, 0, s.data(), int(s.size()), result.data(), n, nullptr, nullptr); return result;
}
std::string sha256(const std::wstring &file) {
    std::ifstream in(std::filesystem::path(file), std::ios::binary);
    if (!in) throw std::runtime_error("Missing input file");
    BCRYPT_ALG_HANDLE alg{}; BCRYPT_HASH_HANDLE hash{};
    if (BCryptOpenAlgorithmProvider(&alg, BCRYPT_SHA256_ALGORITHM, nullptr, 0) < 0)
        throw std::runtime_error("Cannot open SHA-256 provider");
    struct Cleanup { BCRYPT_ALG_HANDLE &a; BCRYPT_HASH_HANDLE &h;
        ~Cleanup() { if (h) BCryptDestroyHash(h); BCryptCloseAlgorithmProvider(a, 0); } } cleanup{alg, hash};
    if (BCryptCreateHash(alg, &hash, nullptr, 0, nullptr, 0, 0) < 0) throw std::runtime_error("Cannot create SHA-256 hash");
    char buf[8192];
    while (in.read(buf, sizeof(buf)) || in.gcount()) {
        if (BCryptHashData(hash, reinterpret_cast<PUCHAR>(buf), ULONG(in.gcount()), 0) < 0)
            throw std::runtime_error("SHA-256 read failure");
    }
    if (!in.eof()) throw std::runtime_error("Cannot read input file");
    UCHAR digest[32];
    if (BCryptFinishHash(hash, digest, sizeof(digest), 0) < 0) throw std::runtime_error("SHA-256 failure");
    std::ostringstream out;
    for (auto b : digest) out << std::hex << std::setw(2) << std::setfill('0') << unsigned(b);
    return out.str();
}
Process::Process(const std::wstring &exe, const std::vector<std::wstring> &args, const std::string &input_data, bool gui) : graceful_close(gui) {
    SECURITY_ATTRIBUTES sa{sizeof(sa), nullptr, TRUE};
    Handle writeEnd, input, inputWriter;
    if (!CreatePipe(&output.h, &writeEnd.h, &sa, 0)) throw std::runtime_error("CreatePipe failed");
    SetHandleInformation(output.h, HANDLE_FLAG_INHERIT, 0);
    if (input_data.empty()) {
        input.h = CreateFileW(L"NUL", GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, &sa, OPEN_EXISTING, 0, nullptr);
        if (input.h == INVALID_HANDLE_VALUE) throw std::runtime_error("Cannot open NUL");
    } else {
        if(input_data.size()>512)throw std::runtime_error("Process input too large");
        if(!CreatePipe(&input.h,&inputWriter.h,&sa,1024))throw std::runtime_error("Input pipe failed");
        if(!SetHandleInformation(inputWriter.h,HANDLE_FLAG_INHERIT,0))throw std::runtime_error("Input handle failed");
        DWORD written=0;
        if(!WriteFile(inputWriter.h,input_data.data(),DWORD(input_data.size()),&written,nullptr) || written!=input_data.size())
            throw std::runtime_error("Process input write failed");
    }
    STARTUPINFOEXW si{}; si.StartupInfo.cb = sizeof(si);
    si.StartupInfo.dwFlags = STARTF_USESTDHANDLES;
    si.StartupInfo.hStdInput = input.h;
    si.StartupInfo.hStdOutput = si.StartupInfo.hStdError = writeEnd.h;
    SIZE_T bytes = 0; InitializeProcThreadAttributeList(nullptr, 1, 0, &bytes);
    std::vector<uint8_t> storage(bytes);
    si.lpAttributeList = reinterpret_cast<LPPROC_THREAD_ATTRIBUTE_LIST>(storage.data());
    if (!InitializeProcThreadAttributeList(si.lpAttributeList, 1, 0, &bytes))
        throw std::runtime_error("Process attributes failed");
    struct Cleanup { LPPROC_THREAD_ATTRIBUTE_LIST p; ~Cleanup(){ DeleteProcThreadAttributeList(p); } } cleanup{si.lpAttributeList};
    HANDLE handles[]{input.h, writeEnd.h};
    if (!UpdateProcThreadAttribute(si.lpAttributeList, 0, PROC_THREAD_ATTRIBUTE_HANDLE_LIST,
        handles, sizeof(handles), nullptr, nullptr)) throw std::runtime_error("Process handle list failed");
    std::wstring cmd = quote(exe);
    for (const auto &a : args) cmd += L" " + quote(a);
    PROCESS_INFORMATION pi{};
    // A full executable path and explicit handle list avoid PATH lookup and cross-source handle leaks.
    if (!CreateProcessW(exe.c_str(), cmd.data(), nullptr, nullptr, TRUE,
        CREATE_NO_WINDOW | EXTENDED_STARTUPINFO_PRESENT, nullptr,
        std::filesystem::path(exe).parent_path().c_str(), &si.StartupInfo, &pi))
        throw std::runtime_error("Cannot start selected ADB (Windows " + std::to_string(GetLastError()) + ")");
    process.h = pi.hProcess; CloseHandle(pi.hThread);
}
Process::~Process() {
    if (process.h && !done()) {
        if(graceful_close){
            DWORD pid=GetProcessId(process.h);
            EnumWindows([](HWND window,LPARAM param)->BOOL {
                DWORD owner=0;GetWindowThreadProcessId(window,&owner);
                if(owner==DWORD(param))PostMessageW(window,WM_CLOSE,0,0);return TRUE;
            },LPARAM(pid));
            WaitForSingleObject(process.h,2000);
        }
        if(!done()){TerminateProcess(process.h,1);WaitForSingleObject(process.h,1000);}
    }
}
bool Process::done() const { return WaitForSingleObject(process.h, 0) == WAIT_OBJECT_0; }
void Process::poll() {
    DWORD n = 0;
    while (PeekNamedPipe(output.h, nullptr, 0, nullptr, &n, nullptr) && n) {
        char buf[4096]; DWORD read = 0;
        if (!ReadFile(output.h, buf, (std::min)(n, DWORD(sizeof(buf))), &read, nullptr) || !read) break;
        captured.append(buf, read);
        if (captured.size() > 65536) captured.erase(0, captured.size() - 65536);
    }
}
std::string Process::wait(const Cancel &cancel, unsigned timeout_ms) {
    auto end = GetTickCount64() + timeout_ms;
    while (!done()) {
        poll();
        if (cancel()) throw std::runtime_error("Cancelled");
        if (GetTickCount64() >= end) throw std::runtime_error("ADB command timed out");
        WaitForSingleObject(process.h, 25);
    }
    poll(); DWORD exit = 1; GetExitCodeProcess(process.h, &exit);
    if (exit) throw std::runtime_error("ADB command failed: " + captured);
    return captured;
}
std::string adb(const std::wstring &exe, const std::vector<std::wstring> &args, const Cancel &cancel, unsigned timeout_ms) {
    // ADB only classifies an empty host or literal localhost as local for auto-start.
    // Explicit host/port also ignore inherited remote ADB server settings.
    std::vector<std::wstring> local{L"-H", L"localhost", L"-P", L"5037"};
    local.insert(local.end(), args.begin(), args.end());
    return Process(exe, local).wait(cancel, timeout_ms);
}
}
