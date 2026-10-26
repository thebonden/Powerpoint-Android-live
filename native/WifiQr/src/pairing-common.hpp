// SPDX-License-Identifier: GPL-2.0-or-later
// Extracted from the user's android-usb-obs pairing implementation; see NOTICE.md.
#pragma once
#include <cstdint>
#include <string>
#include <vector>
#include <sstream>
#include <stdexcept>
namespace au {
// Deliberately accept an explicit IPv4 endpoint only: no shell tokens, hostnames,
// broadcast, multicast or loopback targets. No discovery scan is performed.
inline bool valid_endpoint(const std::string &s) {
    if(s.empty() || s.size()>21) return false;
    unsigned parts[5]{}; size_t pos=0;
    for(unsigned i=0;i<5;++i){
        size_t start=pos; unsigned value=0;
        while(pos<s.size() && s[pos]>='0' && s[pos]<='9'){
            value=value*10+unsigned(s[pos++]-'0'); if(value>(i==4?65535u:255u))return false;
        }
        if(pos==start || (pos-start>1 && s[start]=='0'))return false;
        parts[i]=value;
        if(i<4){if(pos==s.size() || s[pos++]!=(i==3?':':'.'))return false;}
    }
    return pos==s.size() && parts[4]>0 && parts[0]>0 && parts[0]<224 && parts[0]!=127 && parts[3]!=255;
}
// CommandLineToArgvW / MS CRT escaping; never invoke cmd.exe for ADB.
inline std::wstring quote(const std::wstring &s) {
    std::wstring out = L"\""; size_t slashes = 0;
    for (wchar_t c : s) {
        if (c == L'\\') { ++slashes; continue; }
        out.append(slashes * (c == L'\"' ? 2 : 1), L'\\'); slashes = 0;
        if (c == L'\"') out += L'\\';
        out += c;
    }
    out.append(slashes * 2, L'\\'); return out + L'\"';
}
}
