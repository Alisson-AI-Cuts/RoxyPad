// Captures "CABLE Output" for N seconds and prints RMS per second.
#include <windows.h>
#include <mmdeviceapi.h>
#include <audioclient.h>
#include <propsys.h>
#include <cmath>
#include <cstdio>
#include <cwchar>
const PROPERTYKEY K = { { 0xa45c254e, 0xdf1c, 0x4efd, { 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0 } }, 14 };
int wmain(int argc, wchar_t** argv){
 int secs = argc>1?_wtoi(argv[1]):10;
 CoInitialize(nullptr); IMMDeviceEnumerator* e; CoCreateInstance(__uuidof(MMDeviceEnumerator),nullptr,CLSCTX_ALL,__uuidof(IMMDeviceEnumerator),(void**)&e);
 IMMDeviceCollection* c; e->EnumAudioEndpoints(eCapture,DEVICE_STATE_ACTIVE,&c); UINT n; c->GetCount(&n); IMMDevice* dev=nullptr;
 for(UINT i=0;i<n;i++){IMMDevice* d; c->Item(i,&d); IPropertyStore* s; d->OpenPropertyStore(STGM_READ,&s); PROPVARIANT v; PropVariantInit(&v); s->GetValue(K,&v); if(wcsstr(v.pwszVal,L"CABLE Output")){dev=d;wprintf(L"medindo: %ls\n",v.pwszVal);}}
 if(!dev){wprintf(L"CABLE Output nao encontrado\n");return 1;}
 IAudioClient* a; dev->Activate(__uuidof(IAudioClient),CLSCTX_ALL,nullptr,(void**)&a);
 WAVEFORMATEX f{WAVE_FORMAT_IEEE_FLOAT,2,48000,48000*8,8,32,0};
 HRESULT hr=a->Initialize(AUDCLNT_SHAREMODE_SHARED,AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM|AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY,2000000,0,&f,nullptr);
 if(FAILED(hr)){wprintf(L"init 0x%08X\n",hr);return 1;}
 IAudioCaptureClient* cap; a->GetService(__uuidof(IAudioCaptureClient),(void**)&cap); a->Start();
 for(int s=0;s<secs;s++){double sq=0; unsigned long long cnt=0; ULONGLONG t=GetTickCount64();
  while(GetTickCount64()-t<1000){Sleep(10); UINT32 p; while(SUCCEEDED(cap->GetNextPacketSize(&p))&&p){BYTE* d; UINT32 fr; DWORD fl; cap->GetBuffer(&d,&fr,&fl,nullptr,nullptr); const float* x=(const float*)d; for(UINT32 i=0;i<fr*2;i++){double y=(fl&AUDCLNT_BUFFERFLAGS_SILENT)?0:x[i]; sq+=y*y;} cnt+=fr*2; cap->ReleaseBuffer(fr);}}
  wprintf(L"t=%ds cable_rms=%.5f frames=%llu\n",s+1,cnt?sqrt(sq/cnt):0.0,cnt/2);}
 return 0;}
