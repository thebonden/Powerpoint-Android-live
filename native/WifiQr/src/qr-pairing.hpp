// SPDX-License-Identifier: GPL-2.0-or-later
#pragma once
#include "pairing-common.hpp"
#include <algorithm>
#include <functional>
namespace au {
struct MdnsService { std::string name, type, endpoint; };
inline bool service_token(const std::string &s) {
    return !s.empty() && s.size()<=100 && std::all_of(s.begin(),s.end(),[](unsigned char c){
        return (c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='-'||c=='_';});
}
inline std::vector<MdnsService> parse_mdns(const std::string &text) {
    std::vector<MdnsService> result;std::istringstream lines(text);std::string line;
    while(std::getline(lines,line)){
        if(line.size()>1024)continue;
        std::istringstream fields(line);MdnsService s;std::string extra;
        if(!(fields>>s.name>>s.type>>s.endpoint)||(fields>>extra))continue;
        if(!s.type.empty()&&s.type.back()=='.')s.type.pop_back();
        if(s.type!="_adb-tls-pairing._tcp" && s.type!="_adb-tls-connect._tcp")continue;
        if(!s.name.empty()&&s.name.back()=='.')s.name.pop_back();
        auto suffix="."+s.type;
        if(s.name.size()>suffix.size()&&s.name.compare(s.name.size()-suffix.size(),suffix.size(),suffix)==0)
            s.name.resize(s.name.size()-suffix.size());
        if(service_token(s.name)&&valid_endpoint(s.endpoint))result.push_back(s);
    }
    return result;
}
inline std::string pairing_guid(const std::string &reply,const std::string &endpoint){
    if(reply.find("Successfully paired to "+endpoint+" ")==std::string::npos)
        throw std::runtime_error("Parring mislykkedes. Luk parringsvinduet på enheden og prøv en ny QR-kode.");
    auto start=reply.find("[guid=");if(start==std::string::npos)throw std::runtime_error("Parret, men enhedens ID mangler. Brug forbindelsesadressen fra Trådløs fejlfinding.");
    start+=6;auto end=reply.find(']',start);
    auto guid=end==std::string::npos?std::string():reply.substr(start,end-start);
    if(!service_token(guid))throw std::runtime_error("Ugyldigt enheds-ID fra ADB.");return guid;
}
inline std::string qr_payload(const std::string &name,const std::string &secret){
    if(name.rfind("ppt-",0)!=0||!service_token(name)||!service_token(secret)||secret.size()<16)
        throw std::runtime_error("Invalid QR credentials");
    return "WIFI:T:ADB;S:"+name+";P:"+secret+";;";
}
using QrCommand=std::function<std::string(const std::vector<std::string>&,const std::string&)>;
// The runner only addresses the service carrying this session's random name,
// then the exact device GUID returned by authenticated pairing. Never guesses ports.
inline std::string pair_qr(const std::string &name,const std::string &secret,
    const QrCommand &command,const std::function<void(const std::string&)> &status,
    const std::function<bool()> &cancel,const std::function<void()> &pause,unsigned scan_attempts=120,unsigned connect_attempts=30){
    auto check_cancel=[&]{if(cancel())throw std::runtime_error("Annulleret");};
    auto discover=[&](const std::string &wanted,const std::string &type,unsigned attempts){
        for(unsigned i=0;i<attempts;++i){
            check_cancel();auto services=parse_mdns(command({"mdns","services"},{}));check_cancel();
            std::string endpoint;
            for(const auto &s:services)if(s.name==wanted&&s.type==type){
                if(!endpoint.empty()&&endpoint!=s.endpoint)throw std::runtime_error("Flere adresser for samme enhed. Prøv igen på ét lokalt netværk.");
                endpoint=s.endpoint;
            }
            if(!endpoint.empty())return endpoint;
            pause();
        }
        return std::string();
    };
    auto pair=discover(name,"_adb-tls-pairing._tcp",scan_attempts);
    if(pair.empty())throw std::runtime_error("QR-koden udløb uden kontakt. Brug scanner under Trådløs fejlfinding. Kontrollér samme Wi-Fi; netværket skal tillade enhedsopdagelse (mDNS). Kontrollér også, at Trådløs fejlfinding er slået til.");
    check_cancel();status("Enhed fundet. Parrer sikkert …");
    auto reply=command({"pair",pair},secret+"\n");check_cancel();auto guid=pairing_guid(reply,pair);
    status("Parring lykkedes. Finder forbindelsesporten …");
    auto endpoint=discover(guid,"_adb-tls-connect._tcp",connect_attempts);
    if(endpoint.empty())throw std::runtime_error("Parring lykkedes, men forbindelsesporten blev ikke fundet. Brug IP-adresse og port fra HOVEDSIDEN Trådløs fejlfinding i PowerPoint.");
    if(endpoint==pair)throw std::runtime_error("Enheden annoncerede samme port til parring og forbindelse. Prøv igen.");
    check_cancel();command({"connect",endpoint},{});check_cancel();
    auto state=command({"-s",endpoint,"get-state"},{});check_cancel();
    std::istringstream state_words(state);std::string value;state_words>>value;
    if(value!="device")throw std::runtime_error("Parret, men forbindelsen er ikke klar. Kontrollér netværk og Trådløs fejlfinding.");
    return endpoint;
}
}
