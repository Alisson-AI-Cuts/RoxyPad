#pragma once

#include <windows.h>
#include <cstddef>
#include <cstdint>

namespace RoxyBridge {
constexpr wchar_t MappingName[] = L"Local\\RoxyPad.AudioBridge.v1";
constexpr std::uint32_t Magic = 0x59584F52; // ROXY
constexpr std::uint32_t Version = 1;
constexpr std::uint32_t SampleRate = 48000;
constexpr std::uint32_t Channels = 2;
constexpr std::uint32_t RingFrames = 32768;

enum Status : LONG {
    StatusStarting = 1,
    StatusReady = 2,
    StatusStopped = 3,
    StatusInvalidParent = -1,
    StatusActivationFailed = -2,
    StatusClientFailed = -3,
    StatusFormatFailed = -4,
    StatusCaptureFailed = -5,
    StatusNoCable = -6,
    StatusNoMic = -7
};

struct SharedHeader {
    std::uint32_t magic;
    std::uint32_t version;
    std::uint32_t sampleRate;
    std::uint32_t channels;
    std::uint32_t ringFrames;
    volatile LONG volumePermille;
    volatile LONG active;
    volatile LONG status;
    volatile LONG64 writeFrame;
    volatile LONG64 heartbeatQpc;
    volatile LONG micGainPercent; // offset 48: gain applied to the voice (100 = unchanged)
    volatile LONG denoise;        // offset 52: 1 = RNNoise voice isolation on the mic
    volatile LONG gatePercent;    // offset 56: speech probability (%) below which the mic is gated; 0 = off
    std::uint8_t padding[4];
};

constexpr std::size_t HeaderBytes = sizeof(SharedHeader);
static_assert(HeaderBytes == 64, "shared header layout must remain 64 bytes");
constexpr std::size_t MappingBytes = HeaderBytes +
    static_cast<std::size_t>(RingFrames) * Channels * sizeof(float);

inline float* Samples(SharedHeader* header) {
    return reinterpret_cast<float*>(reinterpret_cast<std::uint8_t*>(header) + HeaderBytes);
}
}
