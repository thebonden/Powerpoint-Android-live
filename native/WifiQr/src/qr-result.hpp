// SPDX-License-Identifier: GPL-2.0-or-later
#pragma once
#include "pairing-common.hpp"
namespace au {
inline std::string qr_result(const std::string &output){
    if(output.empty())return {}; // User closed the pairing window.
    std::istringstream words(output);std::string marker,endpoint,extra;
    if(!(words>>marker>>endpoint)||(words>>extra)||marker!="ANDROID_PPT_QR_V1"||!valid_endpoint(endpoint))
        throw std::runtime_error("Invalid QR pairing result");
    return endpoint;
}
}
