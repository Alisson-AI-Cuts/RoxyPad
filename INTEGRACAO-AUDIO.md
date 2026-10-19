# Saída no microfone — como funciona (0.5)

## Por que a versão 0.4 não funcionava

A 0.4 tentava somar o áudio dentro do driver do Fuxi-H3 com uma DLL própria (`RoxyMicAPO.dll`, um "Audio Processing Object"). No Windows 11 isso não funciona para um app caseiro:

- o mecanismo de áudio (`audiodg`) só carrega APOs **assinados** e instalados como componente de driver (pacote INF). A DLL não tinha assinatura (`NotSigned`) e foi registrada só por chaves de registro;
- o próprio teste da 0.4 mediu a captura do Fuxi-H3 com e sem a mistura e obteve praticamente zero nos dois casos (`before=0.00002`, `active=0.00003`).

A integração foi removida em 18/10/2026 com o `-Uninstall` do script antigo: valores do endpoint restaurados, DLL e CLSID apagados.

## Como funciona agora

1. O RoxyPad toca o áudio normalmente no seu fone.
2. O `RoxyBridge.exe` (iniciado pelo RoxyPad) captura:
   - o microfone padrão do Windows (ignora dispositivos com "CABLE" no nome, para não realimentar);
   - somente o áudio do processo RoxyPad (captura por processo), sem o som do Discord.
3. Ele mistura os dois a 48 kHz estéreo e envia para o **CABLE Input** do VB-CABLE.
4. O Discord usa **CABLE Output** como microfone.

O áudio do player só entra na mistura enquanto toca; pausado/parado passa só a voz. Se um dispositivo sumir (headset desconectado, VB-CABLE instalado com o app aberto), o bridge refaz tudo a cada 2 s.

## Instalação do VB-CABLE

1. Baixe o VB-CABLE em https://vb-audio.com/Cable/ (gratuito/donationware, driver assinado).
2. Extraia o zip e execute `VBCABLE_Setup_x64.exe` como administrador → *Install Driver*.
3. Reinicie o PC se o instalador pedir.
4. Confira que o seu microfone padrão do Windows continua sendo o Fuxi-H3 (Configurações → Som → Entrada).
5. Abra o RoxyPad; no Discord: Configurações → Voz e vídeo → Dispositivo de entrada → **CABLE Output**.

## Testes feitos

- Sem VB-CABLE: o bridge reporta "VB-CABLE não instalado" (status -6) e tenta de novo; fecha junto com o RoxyPad.
- Com outra saída no lugar do cabo (`ROXY_CABLE_NAME=S/PDIF`, variável só para teste) e um processo tocando um tom de 440 Hz com amplitude 0,15: captura do app com RMS 0,106 (o esperado, 0,15/√2), saída igual. Com `active=0`: app 0,106 e saída 0.
- O RoxyPad compilado inicia o `RoxyBridge.exe` como filho e o encerra ao fechar.

- Após reiniciar o PC (18/10/2026), com o RoxyPad real: o cabo recebeu a voz (RMS até ~0,025) e os áudios (até ~0,148), e o Discord ficou capturando o CABLE Output. O usuário confirmou que funcionou no Discord.

## Problemas encontrados na configuração

- **Saída do Discord no CABLE Input:** o Discord ocupa o cabo, você não escuta a chamada e o RoxyPad falha com `0x8889000A`. A saída tem que ser o fone; só a entrada é CABLE Output.
- **RoxyPad antigo:** os executáveis da versão do Codex foram renomeados para `.exe.antigo` para não aparecerem na busca.
- **Sem som mesmo com tudo certo:** o primeiro teste falhou e voltou a funcionar só reiniciando o RoxyPad (causa não comprovada). Se acontecer: Sair pela bandeja e abrir de novo.
- Se os memes sumirem no Discord: desligar cancelamento de eco e supressão de ruído, e ajustar a sensibilidade de entrada manualmente.

Diagnóstico manual: `RoxyBridge.exe <PID do RoxyPad> --verbose` mostra por segundo o RMS do mic, do app e da saída.

## Ferramentas de diagnóstico (`diagnostico/`)

- `test-bridge.ps1`: inicia o bridge com um processo que toca um tom de 440 Hz e mostra o log (`-Active 0` testa o silêncio ao pausar; `-CableName` troca a saída).
- `cablemeter.cpp`: mede por segundo o RMS que chega no **CABLE Output** (o que o Discord ouve). Compilar: `cl /EHsc cablemeter.cpp ole32.lib`.

Observação: logo após instalar o VB-CABLE, abrir o CABLE Input passou a falhar com `0x8889000A` (dispositivo em uso). O readme do VB-CABLE exige reiniciar o PC para finalizar a instalação.
