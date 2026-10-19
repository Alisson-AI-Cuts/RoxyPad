@echo off
setlocal
if defined VSCMD_VER goto ready
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" exit /b 1
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSROOT=%%i"
if not defined VSROOT exit /b 1
set "VSDEVCMD=%VSROOT%\Common7\Tools\VsDevCmd.bat"
call "%VSDEVCMD%" -arch=x64 -host_arch=x64 >nul
if errorlevel 1 exit /b 1
:ready
pushd "%~dp0rnnoise"
rem RNNoise (BSD-3, xiph/rnnoise 0.2): voice isolation for the microphone.
cl.exe /nologo /c /O2 /MT /W1 /TC /I. /D_USE_MATH_DEFINES /D_CRT_SECURE_NO_WARNINGS celt_lpc.c denoise.c kiss_fft.c nnet.c nnet_default.c parse_lpcnet_weights.c pitch.c rnn.c rnnoise_data.c rnnoise_tables.c
if errorlevel 1 exit /b 1
popd
pushd "%~dp0"
cl.exe /nologo /std:c++17 /EHsc /O2 /MT /W4 /DUNICODE /D_UNICODE /DWIN32_LEAN_AND_MEAN /D_NOMINMAX /I. /Fe:"..\RoxyBridge.exe" RoxyBridge.cpp rnnoise\*.obj /link mmdevapi.lib ole32.lib uuid.lib mfplat.lib mfuuid.lib advapi32.lib
if errorlevel 1 exit /b 1
popd
exit /b 0

