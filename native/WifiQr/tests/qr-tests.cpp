// SPDX-License-Identifier: GPL-2.0-or-later
#include "qr-pairing.hpp"
#include "qr-result.hpp"
#include "../vendor/qrcodegen/qrcodegen.hpp"
#include <iostream>
#include <fstream>
using namespace au;
void require(bool v,const char *s){if(!v)throw std::runtime_error(s);}
int main(int argc,char**argv){try{
    require(qr_result("ANDROID_PPT_QR_V1 10.0.0.8:4444\n")=="10.0.0.8:4444","private helper result");
    require(qr_result("").empty(),"cancelled helper result");
    for(const auto &bad:{"10.0.0.8:4444","ANDROID_PPT_QR_V1 127.0.0.1:4444","ANDROID_PPT_QR_V1 10.0.0.8:4444 extra"}){
        bool rejected=false;try{qr_result(bad);}catch(...){rejected=true;}require(rejected,"invalid helper output rejected");
    }
    auto services=parse_mdns("List of discovered mdns services\nother _adb-tls-pairing._tcp 10.0.0.9:2222\nppt-test _adb-tls-pairing._tcp. 10.0.0.8:3333\nadb-device._adb-tls-connect._tcp. _adb-tls-connect._tcp 10.0.0.8:4444\nbad _adb-tls-connect._tcp 127.0.0.1:55\nbad _adb-tls-connect._tcp 10.0.0.8:55;bad\nbad _adb-tls-connect._tcp 10.0.0.8:55 extra\n");
    require(services.size()==3&&services[2].name=="adb-device","mdns parser boundaries and normalized names");
    const std::string name="ppt-test",secret="0123456789abcdef0123456789abcdef";
    unsigned commands=0,pauses=0;bool paired=false;
    auto command=[&](const std::vector<std::string>&a,const std::string&input){
        ++commands;
        if(a==std::vector<std::string>{"mdns","services"}){
            if(commands==1)return std::string("other _adb-tls-pairing._tcp 10.0.0.9:1111\n");
            return paired?std::string("adb-other _adb-tls-connect._tcp 10.0.0.8:5555\nadb-device _adb-tls-connect._tcp 10.0.0.8:4444\n"):
                std::string("ppt-test _adb-tls-pairing._tcp 10.0.0.8:3333\n");
        }
        if(a==std::vector<std::string>{"pair","10.0.0.8:3333"}){require(input==secret+"\n","secret must use stdin, not argv");paired=true;return std::string("Successfully paired to 10.0.0.8:3333 [guid=adb-device]\n");}
        require(input.empty(),"only pair uses stdin");
        if(a==std::vector<std::string>{"connect","10.0.0.8:4444"})return std::string("connected to 10.0.0.8:4444\n");
        if(a==std::vector<std::string>{"-s","10.0.0.8:4444","get-state"})return std::string("device\n");
        throw std::runtime_error("Unexpected device or port selected");
    };
    auto no=[](const std::string&){};auto never=[]{return false;};auto pause=[&]{++pauses;};
    auto endpoint=pair_qr(name,secret,command,no,never,pause,4,4);
    require(endpoint=="10.0.0.8:4444"&&pauses==1,"pairing port must never be returned for PowerPoint");
    bool caught=false;unsigned before=commands;
    try{pair_qr(name,secret,command,no,[]{return true;},pause);}catch(...){caught=true;}
    require(caught&&commands==before,"cancel before any ADB call");
    caught=false;try{pairing_guid("failed to pair","10.0.0.8:3333");}catch(...){caught=true;}require(caught,"failure not accepted as paired");
    caught=false;try{pairing_guid("Successfully paired to 10.0.0.9:3333 [guid=adb-device]","10.0.0.8:3333");}catch(...){caught=true;}require(caught,"wrong endpoint reply rejected");
    caught=false;try{pair_qr(name,secret,[](const auto&,const auto&){return std::string();},no,never,[]{},1,1);}catch(...){caught=true;}require(caught,"missing discovery times out");
    caught=false;try{pair_qr(name,secret,[](const auto&,const auto&){return std::string("ppt-test _adb-tls-pairing._tcp 10.0.0.8:3333\nppt-test _adb-tls-pairing._tcp 10.0.0.9:3333\n");},no,never,[]{},1,1);}catch(...){caught=true;}require(caught,"ambiguous targets rejected");
    commands=0;paired=false;caught=false;
    try{pair_qr(name,secret,[&](const auto&a,const auto&input){
        if(a==std::vector<std::string>{"-s","10.0.0.8:4444","get-state"})return std::string("offline\n");
        return command(a,input);
    },no,never,pause,4,4);}catch(...){caught=true;}require(caught,"offline state cannot complete pairing");
    commands=0;paired=false;caught=false;
    try{pair_qr(name,secret,[&](const auto&a,const auto&input){
        if(paired && a==std::vector<std::string>{"mdns","services"})return std::string("adb-other _adb-tls-connect._tcp 10.0.0.8:4444\n");
        return command(a,input);
    },no,never,pause,4,1);}catch(...){caught=true;}require(caught,"unrelated GUID is never used for connection");
    commands=0;paired=false;caught=false;
    try{pair_qr(name,secret,[&](const auto&a,const auto&input){
        if(paired && a==std::vector<std::string>{"mdns","services"})return std::string("adb-device _adb-tls-connect._tcp 10.0.0.8:3333\n");
        return command(a,input);
    },no,never,pause,4,1);}catch(...){caught=true;}require(caught,"pairing port cannot be used as connection port");
    commands=0;paired=false;caught=false;
    try{pair_qr(name,secret,command,no,[&]{return paired;},pause,4,4);}catch(...){caught=true;}
    require(caught && commands==3,"cancellation after pairing prevents discovery and connect");
    auto payload=qr_payload(name,secret);require(payload=="WIFI:T:ADB;S:ppt-test;P:0123456789abcdef0123456789abcdef;;","Android QR payload");
    caught=false;try{qr_payload("ppt-evil;P:evil",secret);}catch(...){caught=true;}require(caught,"QR delimiters rejected");
    auto qr=qrcodegen::QrCode::encodeText(payload.c_str(),qrcodegen::QrCode::Ecc::MEDIUM);
    if(argc>1){int side=(qr.getSize()+8)*8;std::ofstream out(argv[1],std::ios::binary);out<<"P5\n"<<side<<" "<<side<<"\n255\n";
        for(int y=0;y<side;++y)for(int x=0;x<side;++x){unsigned char pixel=qr.getModule(x/8-4,y/8-4)?0:255;out.write(reinterpret_cast<char*>(&pixel),1);}}
    std::cout<<"PASS QR payload, mDNS parsing, exact session and device matching, connection-port selection, stdin secret, cancellation, timeout and ambiguity rejection\n";return 0;
}catch(const std::exception&e){std::cerr<<"FAIL "<<e.what()<<"\n";return 1;}}
