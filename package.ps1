param([Parameter(Mandatory=$true)][string]$ISCC)
$ErrorActionPreference = 'Stop'
$root=$PSScriptRoot
& (Join-Path $root 'build.ps1')
$dist=Join-Path $root 'dist'
$payload=Join-Path $dist 'payload'
if(Test-Path $payload){throw 'dist/payload já existe. Use uma cópia limpa do código para empacotar.'}
New-Item -ItemType Directory -Force $payload | Out-Null
foreach($file in @('RoxyPad.exe','RoxyBridge.exe','LICENSE.txt','THIRD-PARTY-NOTICES.md','README.md','CHANGELOG.md','INTEGRACAO-AUDIO.md')){
 Copy-Item -LiteralPath (Join-Path $root $file) -Destination $payload
}
New-Item -ItemType Directory -Force (Join-Path $payload 'Assets') | Out-Null
foreach($asset in @('RoxyPad.ico','RoxyPad.png','Poppins-Bold.ttf','OFL.txt')){
 Copy-Item -LiteralPath (Join-Path $root ('Assets/'+$asset)) -Destination (Join-Path $payload 'Assets')
}
New-Item -ItemType Directory -Force (Join-Path $payload 'Licenses') | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'native/rnnoise/COPYING') -Destination (Join-Path $payload 'Licenses/RNNoise-BSD.txt')
Copy-Item -LiteralPath (Join-Path $root 'docs/Primeiros-passos.html') -Destination (Join-Path $payload 'Primeiros-passos.html')
Copy-Item -LiteralPath (Join-Path $root 'docs/INSTALACAO.md') -Destination $payload
Copy-Item -LiteralPath (Join-Path $root 'docs') -Destination $payload -Recurse
$vendor=Join-Path $root 'vendor'
New-Item -ItemType Directory -Force $vendor | Out-Null
$zip=Join-Path $vendor 'VBCABLE_Driver_Pack45.zip'
if(!(Test-Path $zip)){Invoke-WebRequest 'https://download.vb-audio.com/Download_CABLE/VBCABLE_Driver_Pack45.zip' -OutFile $zip}
$expected='B950E39F01AF1D04EA623C8F6D8EB9B6EA5C477C637295FABF20631C85116BFB'
if((Get-FileHash $zip -Algorithm SHA256).Hash -ne $expected){throw 'VB-CABLE: hash diferente do pacote revisado. Não distribuído.'}
Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $payload 'ThirdParty/VB-CABLE')
$sig=Get-AuthenticodeSignature (Join-Path $payload 'ThirdParty/VB-CABLE/VBCABLE_Setup_x64.exe')
if($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'BUREL VINCENT'){throw 'Assinatura do instalador VB-CABLE não validada.'}
$qa=Join-Path $dist 'self-test'
New-Item -ItemType Directory -Force $qa | Out-Null
$p=Start-Process -FilePath (Join-Path $payload 'RoxyPad.exe') -ArgumentList @('--self-test',('"'+$qa+'"')) -Wait -PassThru
if($p.ExitCode -ne 0 -or !(Test-Path (Join-Path $qa 'result.txt'))){throw 'Self-test falhou.'}
Get-Content (Join-Path $qa 'result.txt')
& $ISCC (Join-Path $root 'installer/RoxyPad.iss')
if($LASTEXITCODE -ne 0){throw 'Compilação do instalador falhou.'}
$portable=Join-Path $dist 'RoxyPad-0.6.1-Portable-x64.zip'
Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $portable
$hashes=Get-ChildItem $dist -File | Where-Object {$_.Extension -in @('.exe','.zip')} | ForEach-Object { '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(),$_.Name }
Set-Content -LiteralPath (Join-Path $dist 'SHA256SUMS.txt') -Value $hashes -Encoding ascii
