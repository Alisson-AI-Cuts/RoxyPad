// RoxyBridge: mixes the physical microphone with the audio played by the
// RoxyPad process and renders the result to the VB-CABLE "CABLE Input"
// endpoint. Discord (or any app) then uses "CABLE Output" as its microphone.
#include "BridgeProtocol.h"
#include "rnnoise/rnnoise.h"

#include <audioclient.h>
#include <audioclientactivationparams.h>
#include <propsys.h>
#include <mmdeviceapi.h>
#include <ksmedia.h>
#include <mfapi.h>
#include <sddl.h>
#include <wrl.h>
#include <cmath>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <cwchar>
#include <string>
#include <vector>

using Microsoft::WRL::ComPtr;
using Microsoft::WRL::FtmBase;
using Microsoft::WRL::Make;
using Microsoft::WRL::RuntimeClass;
using Microsoft::WRL::RuntimeClassFlags;

namespace {
HANDLE g_stopEvent = nullptr;
bool g_verbose = false;

// PKEY_Device_FriendlyName, declared here to avoid functiondiscoverykeys_devpkey.h.
const PROPERTYKEY FriendlyNameKey = { { 0xa45c254e, 0xdf1c, 0x4efd, { 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0 } }, 14 };
// The endpoint is "CABLE Input (VB-Audio Virtual Cable)"; matching the device
// part survives a user renaming "CABLE Input". The 16-channel variant is skipped.
constexpr wchar_t CableRenderName[] = L"VB-Audio Virtual Cable";
constexpr wchar_t CableSkipName[] = L"16 Ch";
constexpr wchar_t CableTag[] = L"CABLE";
constexpr UINT32 TickMs = 5;
constexpr UINT32 RenderTargetFrames = RoxyBridge::SampleRate * 30 / 1000;  // ~30 ms queued at the cable
constexpr UINT32 JitterFrames = RoxyBridge::SampleRate * 20 / 1000;        // cushion before a source is consumed
constexpr UINT32 MaxBacklogFrames = RoxyBridge::SampleRate * 120 / 1000;   // drop older audio beyond this
constexpr REFERENCE_TIME CaptureBuffer = 2000000;                          // 200 ms
constexpr REFERENCE_TIME RenderBuffer = 1000000;                           // 100 ms

BOOL WINAPI ConsoleControl(DWORD) {
    if (g_stopEvent) SetEvent(g_stopEvent);
    return TRUE;
}

class ActivationHandler final : public RuntimeClass<
    RuntimeClassFlags<Microsoft::WRL::ClassicCom>,
    FtmBase,
    IActivateAudioInterfaceCompletionHandler> {
public:
    explicit ActivationHandler(HANDLE ready) : ready_(ready) {}

    STDMETHOD(ActivateCompleted)(IActivateAudioInterfaceAsyncOperation* operation) override {
        HRESULT activation = E_UNEXPECTED;
        ComPtr<IUnknown> unknown;
        HRESULT hr = operation->GetActivateResult(&activation, &unknown);
        result = FAILED(hr) ? hr : activation;
        if (SUCCEEDED(result)) result = unknown.As(&audioClient);
        SetEvent(ready_);
        return S_OK;
    }

    HANDLE ready_;
    HRESULT result = E_UNEXPECTED;
    ComPtr<IAudioClient> audioClient;
};

// Stereo float FIFO with a jitter cushion: a source is only consumed once it
// has buffered JitterFrames, so small scheduling differences between the
// capture and render clocks do not turn into clicks.
struct Ring {
    std::vector<float> data = std::vector<float>(static_cast<std::size_t>(RoxyBridge::SampleRate) * RoxyBridge::Channels);
    std::uint64_t write = 0, read = 0;
    bool primed = false;
    double sumSquares = 0;
    std::uint64_t counted = 0;

    std::size_t Capacity() const { return data.size() / RoxyBridge::Channels; }
    UINT32 Available() const { return static_cast<UINT32>(write - read); }

    void Push(const float* input, UINT32 frames, bool silent) {
        for (UINT32 i = 0; i < frames; ++i) {
            const std::size_t slot = static_cast<std::size_t>(write % Capacity()) * RoxyBridge::Channels;
            data[slot] = silent ? 0.0f : input[static_cast<std::size_t>(i) * 2];
            data[slot + 1] = silent ? 0.0f : input[static_cast<std::size_t>(i) * 2 + 1];
            ++write;
        }
        if (Available() > Capacity()) read = write - Capacity();
    }

    void Trim() {
        if (Available() > MaxBacklogFrames) read = write - JitterFrames;
    }

    // Returns false when the source has nothing to offer for this frame.
    bool Pop(float& left, float& right) {
        if (!primed) {
            if (Available() < JitterFrames) return false;
            primed = true;
        }
        if (Available() == 0) { primed = false; return false; }
        const std::size_t slot = static_cast<std::size_t>(read % Capacity()) * RoxyBridge::Channels;
        left = data[slot];
        right = data[slot + 1];
        ++read;
        sumSquares += 0.5 * (static_cast<double>(left) * left + static_cast<double>(right) * right);
        ++counted;
        return true;
    }

    double TakeRms() {
        const double rms = counted ? std::sqrt(sumSquares / counted) : 0.0;
        sumSquares = 0; counted = 0;
        return rms;
    }
};

// Linear up to 0.8, then bends smoothly toward 1.0 so a boosted voice or a
// loud meme saturates gently instead of hard clipping.
float SoftClip(float x) {
    const float a = std::fabs(x);
    if (a <= 0.8f) return x;
    const float y = 0.8f + 0.2f * std::tanh((a - 0.8f) / 0.2f);
    return x < 0 ? -y : y;
}

WAVEFORMATEXTENSIBLE MixFormat() {
    WAVEFORMATEXTENSIBLE format{};
    format.Format.wFormatTag = WAVE_FORMAT_EXTENSIBLE;
    format.Format.nChannels = RoxyBridge::Channels;
    format.Format.nSamplesPerSec = RoxyBridge::SampleRate;
    format.Format.wBitsPerSample = 32;
    format.Format.nBlockAlign = RoxyBridge::Channels * sizeof(float);
    format.Format.nAvgBytesPerSec = RoxyBridge::SampleRate * format.Format.nBlockAlign;
    format.Format.cbSize = sizeof(WAVEFORMATEXTENSIBLE) - sizeof(WAVEFORMATEX);
    format.Samples.wValidBitsPerSample = 32;
    format.dwChannelMask = SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT;
    format.SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
    return format;
}

std::wstring FriendlyName(IMMDevice* device) {
    ComPtr<IPropertyStore> store;
    if (FAILED(device->OpenPropertyStore(STGM_READ, &store))) return L"";
    PROPVARIANT value;
    PropVariantInit(&value);
    std::wstring name;
    if (SUCCEEDED(store->GetValue(FriendlyNameKey, &value)) && value.vt == VT_LPWSTR) name = value.pwszVal;
    PropVariantClear(&value);
    return name;
}

// Render side: the VB-CABLE input (the output endpoint apps play into).
ComPtr<IMMDevice> FindCableInput(IMMDeviceEnumerator* enumerator) {
    ComPtr<IMMDeviceCollection> devices;
    if (FAILED(enumerator->EnumAudioEndpoints(eRender, DEVICE_STATE_ACTIVE, &devices))) return nullptr;
    // ROXY_CABLE_NAME lets a test point the mix at another output.
    wchar_t name[128] = {};
    if (!GetEnvironmentVariableW(L"ROXY_CABLE_NAME", name, 128)) wcscpy_s(name, CableRenderName);
    UINT count = 0;
    devices->GetCount(&count);
    for (UINT i = 0; i < count; ++i) {
        ComPtr<IMMDevice> device;
        if (FAILED(devices->Item(i, &device))) continue;
        const std::wstring friendly = FriendlyName(device.Get());
        if (friendly.find(name) != std::wstring::npos && friendly.find(CableSkipName) == std::wstring::npos)
            return device;
    }
    return nullptr;
}

// Capture side: the Windows default microphone, unless that is the cable
// itself (which would loop the mix back), in which case the first other mic.
ComPtr<IMMDevice> FindMicrophone(IMMDeviceEnumerator* enumerator) {
    ComPtr<IMMDevice> device;
    if (SUCCEEDED(enumerator->GetDefaultAudioEndpoint(eCapture, eConsole, &device)) &&
        FriendlyName(device.Get()).find(CableTag) == std::wstring::npos)
        return device;
    ComPtr<IMMDeviceCollection> devices;
    if (FAILED(enumerator->EnumAudioEndpoints(eCapture, DEVICE_STATE_ACTIVE, &devices))) return nullptr;
    UINT count = 0;
    devices->GetCount(&count);
    for (UINT i = 0; i < count; ++i) {
        ComPtr<IMMDevice> candidate;
        if (SUCCEEDED(devices->Item(i, &candidate)) && FriendlyName(candidate.Get()).find(CableTag) == std::wstring::npos)
            return candidate;
    }
    return nullptr;
}

HRESULT OpenEndpoint(IMMDevice* device, bool capture, ComPtr<IAudioClient>& client) {
    HRESULT hr = device->Activate(__uuidof(IAudioClient), CLSCTX_ALL, nullptr, &client);
    if (FAILED(hr)) return hr;
    WAVEFORMATEXTENSIBLE format = MixFormat();
    return client->Initialize(AUDCLNT_SHAREMODE_SHARED,
        AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY,
        capture ? CaptureBuffer : RenderBuffer, 0, &format.Format, nullptr);
}

HRESULT OpenProcessLoopback(DWORD parentPid, HANDLE parentProcess, HANDLE sampleEvent,
                            ComPtr<IAudioClient>& client) {
    AUDIOCLIENT_ACTIVATION_PARAMS params{};
    params.ActivationType = AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK;
    params.ProcessLoopbackParams.TargetProcessId = parentPid;
    params.ProcessLoopbackParams.ProcessLoopbackMode = PROCESS_LOOPBACK_MODE_INCLUDE_TARGET_PROCESS_TREE;

    PROPVARIANT activation{};
    activation.vt = VT_BLOB;
    activation.blob.cbSize = sizeof(params);
    activation.blob.pBlobData = reinterpret_cast<BYTE*>(&params);

    HANDLE ready = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    if (!ready) return HRESULT_FROM_WIN32(GetLastError());
    auto callback = Make<ActivationHandler>(ready);
    if (!callback) { CloseHandle(ready); return E_OUTOFMEMORY; }
    ComPtr<IActivateAudioInterfaceAsyncOperation> operation;
    HRESULT hr = ActivateAudioInterfaceAsync(VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK,
        __uuidof(IAudioClient), &activation, callback.Get(), &operation);
    if (SUCCEEDED(hr)) {
        HANDLE waits[] = { parentProcess, ready };
        DWORD wait = WaitForMultipleObjects(2, waits, FALSE, 10000);
        hr = wait == WAIT_OBJECT_0 + 1 ? callback->result
           : wait == WAIT_OBJECT_0 ? HRESULT_FROM_WIN32(ERROR_PROCESS_ABORTED)
           : HRESULT_FROM_WIN32(ERROR_TIMEOUT);
    }
    CloseHandle(ready);
    if (FAILED(hr)) return hr;
    client = callback->audioClient;

    WAVEFORMATEXTENSIBLE format = MixFormat();
    hr = client->Initialize(AUDCLNT_SHAREMODE_SHARED,
        AUDCLNT_STREAMFLAGS_LOOPBACK | AUDCLNT_STREAMFLAGS_EVENTCALLBACK |
        AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY,
        CaptureBuffer, 0, &format.Format, nullptr);
    if (FAILED(hr)) return hr;
    return client->SetEventHandle(sampleEvent);
}

// Voice isolation for the microphone only (memes never pass through it).
// RNNoise works on 10 ms mono frames at 48 kHz in 16-bit scale, so the mic is
// collected into 480-sample frames. Besides denoising, RNNoise returns the
// probability that a frame is speech; the gate closes the mic below
// `threshold` (sniffs, breathing, light coughs, keys). Each frame is held back
// one frame (10 ms look-ahead) so word onsets open the gate in time, and the
// gate stays open for HoldFrames after the last speech so word endings survive.
struct Denoiser {
    static constexpr UINT32 Size = RoxyBridge::SampleRate / 100;
    static constexpr int HoldFrames = 25;                  // 250 ms
    static constexpr float AttackStep = 1.0f / 240;        // opens in ~5 ms
    static constexpr float ReleaseStep = 1.0f / 3840;      // closes in ~80 ms

    DenoiseState* state = rnnoise_create(nullptr);
    float frame[Size] = {};
    float pending[Size] = {};
    float pendingVad = 1.0f;
    UINT32 filled = 0;
    int hold = 0;
    float gain = 1.0f;
    bool enabled = false;
    float threshold = 0.0f;                                // 0 = gate off
    std::uint64_t framesSeen = 0, framesOpen = 0;

    ~Denoiser() { if (state) rnnoise_destroy(state); }

    void Push(const float* input, UINT32 frames, bool silent, Ring& ring) {
        for (UINT32 i = 0; i < frames; ++i) {
            frame[filled++] = silent ? 0.0f
                : 0.5f * (input[static_cast<std::size_t>(i) * 2] + input[static_cast<std::size_t>(i) * 2 + 1]);
            if (filled < Size) continue;
            filled = 0;
            float out[Size];
            float vad = 1.0f;
            if (enabled && state) {
                float scaled[Size];
                for (UINT32 j = 0; j < Size; ++j) scaled[j] = frame[j] * 32768.0f;
                vad = rnnoise_process_frame(state, out, scaled);
                for (UINT32 j = 0; j < Size; ++j) out[j] /= 32768.0f;
            } else {
                std::memcpy(out, frame, sizeof(out));
            }

            // Decide the gate for the held-back frame using its own speech
            // probability and the next frame's (the look-ahead).
            const bool gateOn = enabled && threshold > 0.0f;
            bool open = !gateOn;
            if (gateOn) {
                if ((pendingVad > vad ? pendingVad : vad) >= threshold) hold = HoldFrames;
                else if (hold > 0) --hold;
                open = hold > 0;
            }
            ++framesSeen;
            if (open) ++framesOpen;

            float stereo[Size * 2];
            for (UINT32 j = 0; j < Size; ++j) {
                gain = open ? (gain + AttackStep > 1.0f ? 1.0f : gain + AttackStep)
                            : (gain - ReleaseStep < 0.0f ? 0.0f : gain - ReleaseStep);
                stereo[j * 2] = stereo[j * 2 + 1] = pending[j] * gain;
            }
            ring.Push(stereo, Size, false);
            std::memcpy(pending, out, sizeof(pending));
            pendingVad = vad;
        }
    }

    double TakeOpenRatio() {
        const double ratio = framesSeen ? static_cast<double>(framesOpen) / framesSeen : 0.0;
        framesSeen = framesOpen = 0;
        return ratio;
    }
};

HRESULT Drain(IAudioCaptureClient* capture, Ring& ring, Denoiser* denoiser = nullptr) {
    UINT32 packetFrames = 0;
    HRESULT hr;
    while (SUCCEEDED(hr = capture->GetNextPacketSize(&packetFrames)) && packetFrames > 0) {
        BYTE* data = nullptr;
        UINT32 frames = 0;
        DWORD flags = 0;
        hr = capture->GetBuffer(&data, &frames, &flags, nullptr, nullptr);
        if (FAILED(hr)) return hr;
        const bool silent = (flags & AUDCLNT_BUFFERFLAGS_SILENT) != 0 || data == nullptr;
        if (denoiser) denoiser->Push(reinterpret_cast<const float*>(data), frames, silent, ring);
        else ring.Push(reinterpret_cast<const float*>(data), frames, silent);
        hr = capture->ReleaseBuffer(frames);
        if (FAILED(hr)) return hr;
    }
    ring.Trim();
    return hr;
}

void Heartbeat(RoxyBridge::SharedHeader* header) {
    LARGE_INTEGER qpc{};
    QueryPerformanceCounter(&qpc);
    InterlockedExchange64(&header->heartbeatQpc, qpc.QuadPart);
}

// Returns S_FALSE when asked to stop (stop event or parent exit); a failure
// HRESULT means the pipeline should be rebuilt (device unplugged, etc.).
HRESULT RunMixer(DWORD parentPid, RoxyBridge::SharedHeader* header,
                 HANDLE parentProcess, HANDLE sampleEvent, HANDLE timer) {
    ComPtr<IMMDeviceEnumerator> enumerator;
    HRESULT hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), nullptr, CLSCTX_ALL, IID_PPV_ARGS(&enumerator));
    if (FAILED(hr)) return hr;

    ComPtr<IMMDevice> cable = FindCableInput(enumerator.Get());
    if (!cable) { InterlockedExchange(&header->status, RoxyBridge::StatusNoCable); return HRESULT_FROM_WIN32(ERROR_NOT_FOUND); }
    ComPtr<IMMDevice> mic = FindMicrophone(enumerator.Get());
    if (!mic) { InterlockedExchange(&header->status, RoxyBridge::StatusNoMic); return HRESULT_FROM_WIN32(ERROR_NOT_FOUND); }
    if (g_verbose) fwprintf(stderr, L"mic: %ls\ncable: %ls\n", FriendlyName(mic.Get()).c_str(), FriendlyName(cable.Get()).c_str());

    ComPtr<IAudioClient> renderClient, micClient, appClient;
    hr = OpenEndpoint(cable.Get(), false, renderClient);
    if (FAILED(hr)) { InterlockedExchange(&header->status, RoxyBridge::StatusClientFailed); return hr; }
    hr = OpenEndpoint(mic.Get(), true, micClient);
    if (FAILED(hr)) { InterlockedExchange(&header->status, RoxyBridge::StatusNoMic); return hr; }
    hr = OpenProcessLoopback(parentPid, parentProcess, sampleEvent, appClient);
    if (FAILED(hr)) { InterlockedExchange(&header->status, RoxyBridge::StatusActivationFailed); return hr; }

    ComPtr<IAudioRenderClient> render;
    ComPtr<IAudioCaptureClient> micCapture, appCapture;
    UINT32 renderBufferFrames = 0;
    if (FAILED(hr = renderClient->GetService(IID_PPV_ARGS(&render))) ||
        FAILED(hr = micClient->GetService(IID_PPV_ARGS(&micCapture))) ||
        FAILED(hr = appClient->GetService(IID_PPV_ARGS(&appCapture))) ||
        FAILED(hr = renderClient->GetBufferSize(&renderBufferFrames))) {
        InterlockedExchange(&header->status, RoxyBridge::StatusClientFailed);
        return hr;
    }
    if (FAILED(hr = micClient->Start()) || FAILED(hr = appClient->Start()) || FAILED(hr = renderClient->Start())) {
        InterlockedExchange(&header->status, RoxyBridge::StatusCaptureFailed);
        return hr;
    }
    InterlockedExchange(&header->status, RoxyBridge::StatusReady);

    Ring micRing, appRing;
    Denoiser denoiser;
    double outSquares = 0;
    std::uint64_t outCount = 0;
    ULONGLONG lastReport = GetTickCount64();
    HANDLE waits[] = { g_stopEvent, parentProcess, timer };
    for (;;) {
        DWORD result = WaitForMultipleObjects(3, waits, FALSE, 1000);
        if (result == WAIT_OBJECT_0 || result == WAIT_OBJECT_0 + 1) { hr = S_FALSE; break; }
        denoiser.enabled = InterlockedCompareExchange(&header->denoise, 0, 0) != 0;
        const LONG gatePercent = InterlockedCompareExchange(&header->gatePercent, 0, 0);
        denoiser.threshold = (gatePercent < 0 ? 0 : (gatePercent > 100 ? 100 : gatePercent)) / 100.0f;
        if (FAILED(hr = Drain(micCapture.Get(), micRing, &denoiser))) break;
        if (FAILED(hr = Drain(appCapture.Get(), appRing))) break;

        UINT32 padding = 0;
        if (FAILED(hr = renderClient->GetCurrentPadding(&padding))) break;
        const UINT32 target = RenderTargetFrames < renderBufferFrames ? RenderTargetFrames : renderBufferFrames;
        if (padding < target) {
            const UINT32 frames = target - padding;
            BYTE* buffer = nullptr;
            if (FAILED(hr = render->GetBuffer(frames, &buffer))) break;
            auto* out = reinterpret_cast<float*>(buffer);
            const bool active = InterlockedCompareExchange(&header->active, 0, 0) != 0;
            LONG gainPercent = InterlockedCompareExchange(&header->micGainPercent, 0, 0);
            gainPercent = gainPercent < 0 ? 0 : (gainPercent > 1000 ? 1000 : gainPercent);
            const float micGain = gainPercent / 100.0f;
            for (UINT32 i = 0; i < frames; ++i) {
                float micL = 0, micR = 0, appL = 0, appR = 0;
                micRing.Pop(micL, micR);
                // The player audio is always consumed so it stays in sync; it is
                // only added while RoxyPad reports active playback.
                if (!appRing.Pop(appL, appR) || !active) appL = appR = 0;
                const float l = SoftClip(micL * micGain + appL), r = SoftClip(micR * micGain + appR);
                out[static_cast<std::size_t>(i) * 2] = l;
                out[static_cast<std::size_t>(i) * 2 + 1] = r;
                outSquares += 0.5 * (static_cast<double>(l) * l + static_cast<double>(r) * r);
                ++outCount;
            }
            if (FAILED(hr = render->ReleaseBuffer(frames, 0))) break;
            InterlockedExchangeAdd64(&header->writeFrame, frames);
        }
        Heartbeat(header);

        if (g_verbose && GetTickCount64() - lastReport >= 1000) {
            lastReport = GetTickCount64();
            fwprintf(stderr, L"gate=%.2f mic=%.5f app=%.5f out=%.5f active=%d micBuf=%u appBuf=%u\n",
                denoiser.TakeOpenRatio(), micRing.TakeRms(), appRing.TakeRms(), outCount ? std::sqrt(outSquares / outCount) : 0.0,
                static_cast<int>(InterlockedCompareExchange(&header->active, 0, 0)), micRing.Available(), appRing.Available());
            outSquares = 0; outCount = 0;
        }
    }
    renderClient->Stop();
    micClient->Stop();
    appClient->Stop();
    return hr;
}
}

// The process loopback captures RoxyPad's whole process tree. If the bridge
// stayed a child of RoxyPad, it would capture its own output on the cable and
// feed it back into the mix forever. So the first instance only relaunches
// itself with --child and exits: the real bridge has a dead parent and is no
// longer part of RoxyPad's tree.
int Relaunch(int argc, wchar_t** argv) {
    wchar_t path[MAX_PATH];
    if (!GetModuleFileNameW(nullptr, path, MAX_PATH)) return 9;
    std::wstring command = L"\"" + std::wstring(path) + L"\"";
    for (int i = 1; i < argc; ++i) command += L" " + std::wstring(argv[i]);
    command += L" --child";
    STARTUPINFOW startup{ sizeof(startup) };
    startup.dwFlags = STARTF_USESTDHANDLES;
    startup.hStdInput = GetStdHandle(STD_INPUT_HANDLE);
    startup.hStdOutput = GetStdHandle(STD_OUTPUT_HANDLE);
    startup.hStdError = GetStdHandle(STD_ERROR_HANDLE);
    PROCESS_INFORMATION process{};
    if (!CreateProcessW(path, command.data(), nullptr, nullptr, TRUE, CREATE_NO_WINDOW,
                        nullptr, nullptr, &startup, &process)) return 9;
    CloseHandle(process.hThread);
    CloseHandle(process.hProcess);
    return 0;
}

int wmain(int argc, wchar_t** argv) {
    if (argc < 2 || argc > 4) return 2;
    bool child = false;
    for (int i = 2; i < argc; ++i) {
        if (wcscmp(argv[i], L"--verbose") == 0) g_verbose = true;
        else if (wcscmp(argv[i], L"--child") == 0) child = true;
        else return 2;
    }
    wchar_t* end = nullptr;
    const unsigned long parentPidLong = wcstoul(argv[1], &end, 10);
    if (!end || *end != L'\0' || parentPidLong == 0 || parentPidLong > MAXDWORD) return 2;
    const DWORD parentPid = static_cast<DWORD>(parentPidLong);
    if (!child) return Relaunch(argc, argv);

    HANDLE parent = OpenProcess(SYNCHRONIZE, FALSE, parentPid);
    if (!parent) return 3;
    PSECURITY_DESCRIPTOR descriptor = nullptr;
    if (!ConvertStringSecurityDescriptorToSecurityDescriptorW(
            L"D:(A;;GA;;;OW)(A;;GR;;;AU)(A;;GR;;;LS)(A;;GR;;;SY)",
            SDDL_REVISION_1, &descriptor, nullptr)) {
        CloseHandle(parent);
        return 4;
    }
    SECURITY_ATTRIBUTES security{ sizeof(SECURITY_ATTRIBUTES), descriptor, FALSE };
    HANDLE mapping = CreateFileMappingW(INVALID_HANDLE_VALUE, &security, PAGE_READWRITE, 0,
        static_cast<DWORD>(RoxyBridge::HeaderBytes), RoxyBridge::MappingName);
    LocalFree(descriptor);
    if (!mapping) { CloseHandle(parent); return 4; }
    auto* header = static_cast<RoxyBridge::SharedHeader*>(MapViewOfFile(mapping,
        FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, RoxyBridge::HeaderBytes));
    if (!header) { CloseHandle(mapping); CloseHandle(parent); return 5; }
    std::memset(header, 0, RoxyBridge::HeaderBytes);
    header->magic = RoxyBridge::Magic;
    header->version = RoxyBridge::Version;
    header->sampleRate = RoxyBridge::SampleRate;
    header->channels = RoxyBridge::Channels;
    header->ringFrames = RoxyBridge::RingFrames;
    InterlockedExchange(&header->volumePermille, 500);
    InterlockedExchange(&header->micGainPercent, 400);
    InterlockedExchange(&header->status, RoxyBridge::StatusStarting);

    g_stopEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    HANDLE sampleEvent = CreateEventW(nullptr, FALSE, FALSE, nullptr);
    HANDLE timer = CreateWaitableTimerExW(nullptr, nullptr, CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, TIMER_ALL_ACCESS);
    if (!g_stopEvent || !sampleEvent || !timer) {
        InterlockedExchange(&header->status, RoxyBridge::StatusCaptureFailed);
        return 7;
    }
    LARGE_INTEGER due{};
    due.QuadPart = -static_cast<LONGLONG>(TickMs) * 10000;
    SetWaitableTimer(timer, &due, TickMs, nullptr, nullptr, FALSE);
    SetConsoleCtrlHandler(ConsoleControl, TRUE);

    HRESULT hr = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
    if (SUCCEEDED(hr)) hr = MFStartup(MF_VERSION, MFSTARTUP_LITE);
    // Rebuild the pipeline whenever a device disappears or is not there yet
    // (headset unplugged, VB-CABLE installed while RoxyPad is open, ...).
    while (SUCCEEDED(hr)) {
        hr = RunMixer(parentPid, header, parent, sampleEvent, timer);
        if (hr == S_FALSE) { hr = S_OK; break; }
        if (g_verbose) fwprintf(stderr, L"pipeline: 0x%08X, retrying\n", static_cast<unsigned int>(hr));
        HANDLE waits[] = { g_stopEvent, parent };
        if (WaitForMultipleObjects(2, waits, FALSE, 2000) != WAIT_TIMEOUT) { hr = S_OK; break; }
        hr = S_OK;
    }
    if (FAILED(hr)) fwprintf(stderr, L"RoxyBridge failed: 0x%08X\n", static_cast<unsigned int>(hr));
    InterlockedExchange(&header->active, 0);
    InterlockedExchange(&header->status, SUCCEEDED(hr) ? RoxyBridge::StatusStopped : RoxyBridge::StatusCaptureFailed);
    MFShutdown();
    CoUninitialize();

    SetConsoleCtrlHandler(ConsoleControl, FALSE);
    CloseHandle(timer);
    CloseHandle(sampleEvent);
    CloseHandle(g_stopEvent);
    UnmapViewOfFile(header);
    CloseHandle(mapping);
    CloseHandle(parent);
    return SUCCEEDED(hr) ? 0 : 8;
}
