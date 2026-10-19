param([string]$CableName = '', [int]$Active = 1)
$ErrorActionPreference = 'Stop'
if(Get-Process RoxyPad,RoxyBridge,Discord -ErrorAction SilentlyContinue){throw 'Feche RoxyPad, RoxyBridge e Discord antes deste teste: ele emite um tom e usa o canal de áudio compartilhado.'}
$bridgePath = Join-Path (Split-Path $PSScriptRoot -Parent) 'RoxyBridge.exe'
if(!(Test-Path $bridgePath)){throw 'Compile RoxyBridge.exe antes do diagnóstico.'}
$dir = Split-Path $MyInvocation.MyCommand.Path
$wav = Join-Path $dir 'tone.wav'
# 3 s, 440 Hz, amplitude 0.15, 48 kHz mono 16-bit
$rate = 48000; $n = $rate * 3
$ms = New-Object IO.MemoryStream; $w = New-Object IO.BinaryWriter $ms
$w.Write([Text.Encoding]::ASCII.GetBytes('RIFF')); $w.Write([int](36 + $n * 2)); $w.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
$w.Write([int]16); $w.Write([int16]1); $w.Write([int16]1); $w.Write([int]$rate); $w.Write([int]($rate * 2)); $w.Write([int16]2); $w.Write([int16]16)
$w.Write([Text.Encoding]::ASCII.GetBytes('data')); $w.Write([int]($n * 2))
for ($i = 0; $i -lt $n; $i++) { $w.Write([int16](0.15 * 32767 * [Math]::Sin(2 * [Math]::PI * 440 * $i / $rate))) }
[IO.File]::WriteAllBytes($wav, $ms.ToArray())

# Stand-in for RoxyPad: a process that plays the tone after 3 s.
$player = Start-Process powershell -PassThru -WindowStyle Hidden -ArgumentList "-NoProfile -Command Start-Sleep 3; (New-Object Media.SoundPlayer '$wav').PlaySync(); Start-Sleep 3"
if ($CableName) { $env:ROXY_CABLE_NAME = $CableName }
$log = Join-Path $dir 'bridge.log'
$bridge = Start-Process $bridgePath -PassThru -WindowStyle Hidden -ArgumentList "$($player.Id) --verbose" -RedirectStandardError $log
Start-Sleep 1
$map = [IO.MemoryMappedFiles.MemoryMappedFile]::OpenExisting('Local\RoxyPad.AudioBridge.v1')
$view = $map.CreateViewAccessor(0, 64)
$view.Write(24, [int]$Active)
"status apos 1s = $($view.ReadInt32(28))"
$player.WaitForExit()
Start-Sleep 1
"status final = $($view.ReadInt32(28)); bridge saiu = $($bridge.HasExited) codigo=$($bridge.ExitCode)"
$view.Dispose(); $map.Dispose()
Get-Content $log
