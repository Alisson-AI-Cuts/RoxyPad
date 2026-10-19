param([string]$OutputName = 'RoxyPad.exe')
$ErrorActionPreference = 'Stop'
$nativeBuild = Join-Path $PSScriptRoot 'native\build-native.cmd'
& $nativeBuild
if ($LASTEXITCODE -ne 0) { throw "Compilação dos componentes de áudio falhou ($LASTEXITCODE)." }
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$assets = Join-Path $PSScriptRoot 'Assets'
New-Item -ItemType Directory -Force $assets | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot 'src\Assets\*') -Destination $assets -Force
$refs = @('System.dll','System.Core.dll','System.Xml.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll')
$argsList = @('/nologo','/target:winexe','/platform:x64','/optimize+',('/out:' + (Join-Path $PSScriptRoot $OutputName)),('/resource:' + (Join-Path $PSScriptRoot 'src\Main.xaml') + ',Main.xaml'))
$argsList += ('/resource:' + (Join-Path $PSScriptRoot 'Assets\RoxyPad.ico') + ',RoxyPad.ico')
$argsList += ('/win32icon:' + (Join-Path $PSScriptRoot 'Assets\RoxyPad.ico'))
$assets = Join-Path $PSScriptRoot 'Assets'
New-Item -ItemType Directory -Force $assets | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src\Assets\Poppins-Bold.ttf') -Destination $assets -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src\Assets\OFL.txt') -Destination $assets -Force
foreach ($reference in $refs) { $argsList += '/reference:' + (Join-Path $framework $reference) }
$argsList += Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
& $compiler @argsList
if ($LASTEXITCODE -ne 0) { throw 'Compilação falhou.' }

