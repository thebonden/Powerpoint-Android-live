// SPDX-License-Identifier: GPL-2.0-or-later
#pragma once
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#include <functional>
#include <memory>
#include "pairing-common.hpp"

namespace au {
using Cancel = std::function<bool()>;
std::wstring wide(const std::string &s);
std::string utf8(const std::wstring &s);
void check(HRESULT hr, const char *operation);
std::string sha256(const std::wstring &file);
struct Handle {
    HANDLE h{};
    Handle() = default;
    explicit Handle(HANDLE v) : h(v) {}
    ~Handle() { if (h && h != INVALID_HANDLE_VALUE) CloseHandle(h); }
    Handle(const Handle &) = delete;
    Handle &operator=(const Handle &) = delete;
};
class Process {
    Handle process, output;
    std::string captured;
    bool graceful_close=false;
public:
    Process(const std::wstring &exe, const std::vector<std::wstring> &args, const std::string &input_data = {}, bool gui = false);
    ~Process();
    void poll();
    bool done() const;
    std::string wait(const Cancel &cancel, unsigned timeout_ms);
    const std::string &text() const { return captured; }
};
std::string adb(const std::wstring &exe, const std::vector<std::wstring> &args,
                const Cancel &cancel, unsigned timeout_ms = 8000);
}
