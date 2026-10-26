// SPDX-License-Identifier: GPL-2.0-or-later
#include "windows.hpp"
#include "qr-pairing.hpp"
#include "../vendor/qrcodegen/qrcodegen.hpp"
#include <bcrypt.h>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <shellapi.h>
#include <thread>
#include <atomic>
#include <mutex>
using namespace au;
namespace {
constexpr UINT changed=WM_APP+1;
struct App {
    HWND window{},status_box{},address_box{},copy_button{},start_button{},address_label{};
    HFONT font{},title_font{};int dpi=96;
    std::wstring adb_path;
    std::unique_ptr<qrcodegen::QrCode> qr;
    std::atomic<bool> stop{false};std::thread worker;std::mutex mutex;
    std::string message="Tryk Vis QR-kode for at begynde.", endpoint;
    bool finished=true,integrated_mode=false;ULONGLONG expires=0;
    int px(int value)const{return MulDiv(value,dpi,96);}
    void publish(std::string text,std::string address={},bool done=false){
        {std::lock_guard<std::mutex> lock(mutex);message=std::move(text);endpoint=std::move(address);finished=done;}
        PostMessageW(window,changed,0,0);
    }
    void shutdown(){stop=true;if(worker.joinable())worker.join();}
    ~App(){shutdown();if(font)DeleteObject(font);if(title_font)DeleteObject(title_font);}
    static std::string random_hex(unsigned count){
        std::vector<UCHAR> bytes(count);
        if(BCryptGenRandom(nullptr,bytes.data(),ULONG(bytes.size()),BCRYPT_USE_SYSTEM_PREFERRED_RNG)<0)
            throw std::runtime_error("Kunne ikke danne en sikker QR-kode.");
        const char *hex="0123456789abcdef";std::string s;
        for(auto b:bytes){s+=hex[b>>4];s+=hex[b&15];}return s;
    }
    void start(){
        shutdown();stop=false;
        auto name="ppt-"+random_hex(10), secret=random_hex(16);
        qr=std::make_unique<qrcodegen::QrCode>(qrcodegen::QrCode::encodeText(qr_payload(name,secret).c_str(),qrcodegen::QrCode::Ecc::MEDIUM));
        expires=GetTickCount64()+180000;
        publish("Scan QR-koden på tabletten. Venter på enheden …");
        EnableWindow(start_button,FALSE);EnableWindow(copy_button,FALSE);SetWindowTextW(address_box,L"");InvalidateRect(window,nullptr,TRUE);
        worker=std::thread([this,name,secret]{
            try{
                auto cancelled=[&]{return stop.load()||GetTickCount64()>=expires;};
                auto command=[&](const std::vector<std::string>&args,const std::string &input){
                    std::vector<std::wstring> wide_args{L"-H",L"localhost",L"-P",L"5037"};
                    for(const auto &arg:args)wide_args.push_back(wide(arg));
                    return Process(adb_path,wide_args,input).wait(cancelled,15000);
                };
                auto pause=[&]{for(int i=0;i<20&&!cancelled();++i)Sleep(50);};
                auto address=pair_qr(name,secret,command,[&](const std::string &s){publish(s);},cancelled,pause);
                if(!stop)publish("Forbundet. Kopiér adressen herunder til Wi-Fi-feltet i PowerPoint, og start tabletbilledet.",address,true);
            }catch(const std::exception &e){
                if(!stop){std::string error=e.what();
                    for(size_t p=error.find(secret);p!=std::string::npos;p=error.find(secret))error.replace(p,secret.size(),"[skjult]");
                    if(GetTickCount64()>=expires)error="QR-koden er udløbet. Tryk Vis QR-kode og scan igen.";
                    publish(error,{},true);
                }
            }
        });
    }
    void apply(){
        std::string text,address;bool done;
        {std::lock_guard<std::mutex> lock(mutex);text=message;address=endpoint;done=finished;}
        SetWindowTextW(status_box,wide(text).c_str());SetWindowTextW(address_box,wide(address).c_str());
        EnableWindow(copy_button,!address.empty());EnableWindow(start_button,done);
        if(done){qr.reset();InvalidateRect(window,nullptr,TRUE);}
        if(done && integrated_mode && valid_endpoint(address)){
            // Only an authenticated, connected endpoint crosses the private pipe.
            std::cout<<"ANDROID_PPT_QR_V1 "<<address<<"\n"<<std::flush;
            SendMessageW(window,WM_CLOSE,0,0);
        }
    }
    void clipboard(){
        std::string value;{std::lock_guard<std::mutex> lock(mutex);value=endpoint;}
        if(!valid_endpoint(value))return;
        auto text=wide(value);SIZE_T bytes=(text.size()+1)*sizeof(wchar_t);
        HGLOBAL memory=GlobalAlloc(GMEM_MOVEABLE,bytes);if(!memory)return;
        void *p=GlobalLock(memory);if(!p){GlobalFree(memory);return;}memcpy(p,text.c_str(),bytes);GlobalUnlock(memory);
        if(!OpenClipboard(window)){GlobalFree(memory);return;}
        if(!EmptyClipboard()||!SetClipboardData(CF_UNICODETEXT,memory)){GlobalFree(memory);CloseClipboard();return;}
        CloseClipboard();SetWindowTextW(status_box,L"Adressen er kopieret. Indsæt den i PowerPoint-kildens Wi-Fi-felt med Ctrl+V, og start tabletbilledet.");
    }
    HWND control(const wchar_t *type,const wchar_t *text,DWORD style,int x,int y,int w,int h,int id){
        HWND child=CreateWindowExW(0,type,text,WS_CHILD|WS_VISIBLE|style,px(x),px(y),px(w),px(h),window,reinterpret_cast<HMENU>(INT_PTR(id)),GetModuleHandleW(nullptr),nullptr);
        SendMessageW(child,WM_SETFONT,reinterpret_cast<WPARAM>(font),TRUE);return child;
    }
    void create_controls(){
        font=CreateFontW(-px(17),0,0,0,FW_NORMAL,FALSE,FALSE,FALSE,DEFAULT_CHARSET,0,0,CLEARTYPE_QUALITY,0,L"Segoe UI");
        title_font=CreateFontW(-px(27),0,0,0,FW_SEMIBOLD,FALSE,FALSE,FALSE,DEFAULT_CHARSET,0,0,CLEARTYPE_QUALITY,0,L"Segoe UI");
        control(L"STATIC",integrated_mode ? L"1. Forbind din tablet direkte til PowerPoint.\r\n\r\n2. På Android: Trådløs fejlfinding → Par enhed ved hjælp af en QR-kode.\r\n\r\n3. Scan QR-koden til venstre. PowerPoint indsætter adressen og starter billedet automatisk.\r\n\r\nHold enheden låst op på samme lokale netværk." : L"1. Åbn din præsentation i PowerPoint.\r\n\r\n2. På Android: Trådløs fejlfinding → Par enhed ved hjælp af en QR-kode.\r\n\r\n3. Scan QR-koden til venstre. Hold enheden låst op.\r\n\r\nPC og tablet skal bruge samme lokale netværk.",SS_LEFT,350,112,380,295,0);
        status_box=control(L"STATIC",L"Tryk Vis QR-kode for at begynde.",SS_LEFT,24,430,706,82,0);
        address_label=control(L"STATIC",L"Adresse til PowerPoint — forbindelsesport",SS_LEFT,24,516,706,24,0);
        address_box=control(L"EDIT",L"",WS_BORDER|ES_READONLY|ES_AUTOHSCROLL|WS_TABSTOP,24,543,454,34,0);
        copy_button=control(L"BUTTON",L"Kopiér adresse",WS_TABSTOP,492,541,238,38,2);
        start_button=control(L"BUTTON",L"Vis QR-kode",WS_TABSTOP|BS_DEFPUSHBUTTON,24,594,220,40,1);
        control(L"BUTTON",L"Luk",WS_TABSTOP,610,594,120,40,3);
        EnableWindow(copy_button,FALSE);
        if(integrated_mode){ShowWindow(copy_button,SW_HIDE);ShowWindow(address_box,SW_HIDE);ShowWindow(address_label,SW_HIDE);SetWindowTextW(start_button,L"Prøv igen");}
    }
    void draw(HDC dc){
        RECT client;GetClientRect(window,&client);FillRect(dc,&client,reinterpret_cast<HBRUSH>(GetStockObject(WHITE_BRUSH)));SetBkMode(dc,TRANSPARENT);SetTextColor(dc,RGB(25,35,50));
        auto old=SelectObject(dc,title_font);RECT title{px(24),px(18),px(730),px(56)};
        DrawTextW(dc,L"Forbind Android med QR-kode",-1,&title,DT_LEFT|DT_SINGLELINE);
        SelectObject(dc,font);RECT sub{px(24),px(64),px(730),px(100)};
        DrawTextW(dc,L"Android 11+  ·  Scan med Androids Trådløs fejlfinding",-1,&sub,DT_LEFT|DT_SINGLELINE);
        RECT box{px(24),px(112),px(328),px(416)};FillRect(dc,&box,reinterpret_cast<HBRUSH>(GetStockObject(WHITE_BRUSH)));
        if(qr){int cells=qr->getSize()+8;int unit=(box.right-box.left)/cells;
            int left=box.left+((box.right-box.left)-cells*unit)/2+4*unit;
            int top=box.top+((box.bottom-box.top)-cells*unit)/2+4*unit;
            for(int y=0;y<qr->getSize();++y)for(int x=0;x<qr->getSize();++x)if(qr->getModule(x,y)){
                RECT pixel{left+x*unit,top+y*unit,left+(x+1)*unit,top+(y+1)*unit};FillRect(dc,&pixel,reinterpret_cast<HBRUSH>(GetStockObject(BLACK_BRUSH)));}
        }else{SetTextColor(dc,RGB(90,100,110));DrawTextW(dc,L"QR-koden vises her",-1,&box,DT_CENTER|DT_VCENTER|DT_SINGLELINE);}
        SelectObject(dc,old);
    }
};
LRESULT CALLBACK procedure(HWND window,UINT msg,WPARAM w,LPARAM l){
    App *app=reinterpret_cast<App*>(GetWindowLongPtrW(window,GWLP_USERDATA));
    if(msg==WM_NCCREATE){app=static_cast<App*>(reinterpret_cast<CREATESTRUCTW*>(l)->lpCreateParams);app->window=window;SetWindowLongPtrW(window,GWLP_USERDATA,reinterpret_cast<LONG_PTR>(app));}
    if(!app)return DefWindowProcW(window,msg,w,l);
    try{
        switch(msg){
        case WM_CREATE:app->create_controls();return 0;
        case WM_PAINT:{PAINTSTRUCT ps;HDC dc=BeginPaint(window,&ps);app->draw(dc);EndPaint(window,&ps);return 0;}
        case WM_PRINTCLIENT:app->draw(reinterpret_cast<HDC>(w));return 0;
        case changed:app->apply();return 0;
        case WM_COMMAND:
            if(LOWORD(w)==1)app->start();else if(LOWORD(w)==2)app->clipboard();else if(LOWORD(w)==3)SendMessageW(window,WM_CLOSE,0,0);return 0;
        case WM_CLOSE:app->shutdown();DestroyWindow(window);return 0;
        case WM_DESTROY:PostQuitMessage(0);return 0;
        case WM_CTLCOLORSTATIC:{auto dc=reinterpret_cast<HDC>(w);SetBkColor(dc,RGB(255,255,255));SetTextColor(dc,RGB(25,35,50));return reinterpret_cast<LRESULT>(GetStockObject(WHITE_BRUSH));}
        }
    }catch(const std::exception&e){app->publish(std::string("Fejl: ")+e.what(),{},true);}
    return DefWindowProcW(window,msg,w,l);
}
}
int WINAPI wWinMain(HINSTANCE instance,HINSTANCE,PWSTR,int show){
    SetProcessDPIAware();
    try{
        App app;
        int mode_argc=0;wchar_t **mode_argv=CommandLineToArgvW(GetCommandLineW(),&mode_argc);
        if(mode_argv){for(int i=1;i<mode_argc;++i){std::wstring arg=mode_argv[i];if(arg==L"--powerpoint")app.integrated_mode=true;else if(arg==L"--adb" && i+1<mode_argc)app.adb_path=mode_argv[++i];}LocalFree(mode_argv);}
        HDC dc=GetDC(nullptr);app.dpi=GetDeviceCaps(dc,LOGPIXELSX);ReleaseDC(nullptr,dc);
        // PowerPoint passes the explicit adb.exe beside its selected scrcpy.exe.
        // No OBS binary, embedded ADB version, download or shared-server restart is required.
        if(app.adb_path.empty() || !std::filesystem::path(app.adb_path).is_absolute() ||
           !std::filesystem::is_regular_file(app.adb_path) || std::filesystem::path(app.adb_path).filename()!=L"adb.exe")
            throw std::runtime_error("Vælg den komplette scrcpy-mappe i PowerPoint. adb.exe mangler.");
        WNDCLASSW cls{};cls.lpfnWndProc=procedure;cls.hInstance=instance;cls.lpszClassName=L"AndroidPowerPointQrPairing";
        cls.hCursor=LoadCursor(nullptr,IDC_ARROW);cls.hbrBackground=reinterpret_cast<HBRUSH>(GetStockObject(WHITE_BRUSH));RegisterClassW(&cls);
        RECT rect{0,0,app.px(754),app.px(654)};DWORD style=WS_OVERLAPPED|WS_CAPTION|WS_SYSMENU|WS_MINIMIZEBOX;AdjustWindowRect(&rect,style,FALSE);
        HWND window=CreateWindowExW(0,cls.lpszClassName,L"Android → PowerPoint · QR-parring",style,CW_USEDEFAULT,CW_USEDEFAULT,rect.right-rect.left,rect.bottom-rect.top,nullptr,nullptr,instance,&app);
        if(!window)throw std::runtime_error("Kunne ikke åbne parringsvinduet.");
        int argc=0;wchar_t **argv=CommandLineToArgvW(GetCommandLineW(),&argc);
        std::wstring preview_path;if(argv){for(int i=1;i+1<argc;++i)if(std::wstring(argv[i])==L"--self-test")preview_path=argv[i+1];LocalFree(argv);}
        if(!preview_path.empty()){
            // Deterministic fixture only: no ADB commands, live credentials or clipboard access.
            app.qr=std::make_unique<qrcodegen::QrCode>(qrcodegen::QrCode::encodeText(qr_payload("ppt-0123456789abcdef0123","0123456789abcdef0123456789abcdef").c_str(),qrcodegen::QrCode::Ecc::MEDIUM));
            SetWindowTextW(app.status_box,L"Scan QR-koden på tabletten. Venter på enheden …");
            RECT r;GetClientRect(window,&r);int width=r.right,height=r.bottom;
            BITMAPINFO bi{};bi.bmiHeader.biSize=sizeof(BITMAPINFOHEADER);bi.bmiHeader.biWidth=width;bi.bmiHeader.biHeight=height;bi.bmiHeader.biPlanes=1;bi.bmiHeader.biBitCount=32;bi.bmiHeader.biCompression=BI_RGB;
            void *pixels=nullptr;HDC screen=GetDC(nullptr);HDC mem=CreateCompatibleDC(screen);HBITMAP bmp=CreateDIBSection(screen,&bi,DIB_RGB_COLORS,&pixels,nullptr,0);ReleaseDC(nullptr,screen);
            if(!bmp||!pixels)throw std::runtime_error("Preview bitmap failed");auto old=SelectObject(mem,bmp);
            app.draw(mem);
            for(HWND child=GetWindow(window,GW_CHILD);child;child=GetWindow(child,GW_HWNDNEXT)){
                if(!(GetWindowLongPtrW(child,GWL_STYLE)&WS_VISIBLE))continue;
                RECT cr;GetWindowRect(child,&cr);POINT pos{cr.left,cr.top};ScreenToClient(window,&pos);
                int saved=SaveDC(mem);SetViewportOrgEx(mem,pos.x,pos.y,nullptr);
                IntersectClipRect(mem,0,0,cr.right-cr.left,cr.bottom-cr.top);
                SendMessageW(child,WM_PRINT,reinterpret_cast<WPARAM>(mem),PRF_CLIENT|PRF_NONCLIENT|PRF_ERASEBKGND);RestoreDC(mem,saved);
            }
            BITMAPFILEHEADER header{};header.bfType=0x4d42;header.bfOffBits=sizeof(header)+sizeof(bi.bmiHeader);header.bfSize=header.bfOffBits+DWORD(width*height*4);
            std::ofstream out(std::filesystem::path(preview_path),std::ios::binary);out.write(reinterpret_cast<char*>(&header),sizeof(header));out.write(reinterpret_cast<char*>(&bi.bmiHeader),sizeof(bi.bmiHeader));out.write(static_cast<char*>(pixels),size_t(width)*height*4);
            SelectObject(mem,old);DeleteObject(bmp);DeleteDC(mem);DestroyWindow(window);return out?0:1;
        }
        ShowWindow(window,show);UpdateWindow(window);if(app.integrated_mode)app.start();
        MSG msg{};while(GetMessageW(&msg,nullptr,0,0)>0){if(!IsDialogMessageW(window,&msg)){TranslateMessage(&msg);DispatchMessageW(&msg);}}return 0;
    }catch(const std::exception&e){MessageBoxW(nullptr,wide(e.what()).c_str(),L"Android QR-parring",MB_ICONERROR);return 1;}
}
