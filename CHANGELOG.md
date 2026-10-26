# Não lançado

- Tecla de atalho por áudio: botão direito → **Definir tecla de atalho**. A tecla (ou combinação com Ctrl/Alt/Shift) aparece na linha do áudio e toca o som mesmo com o RoxyPad escondido ou em outro app. Repetir uma tecla já usada pergunta se deve substituir. Teclas reservadas pelo Windows ou por outro app ficam marcadas com ⚠. Fica salvo em `library.xml`.
- Menu do botão direito redesenhado no visual do app, com ícones e a tecla atual à direita.

# 0.6.1 — 19/10/2026

- Distribuição pública no GitHub, instalador por usuário em português e ZIP portátil.
- Guia offline, requisitos Windows 11 x64/.NET 4.8 e instruções de Discord.
- VB-CABLE oficial incluído com instalação opcional explícita, atribuição e hash fixo.
- Runtime C++ estático; descoberta do Visual Studio por vswhere no build.
- Licença proprietária de uso pessoal gratuito, sem venda ou monetização.
- Licenças de RNNoise/Poppins preservadas e avisos de terceiros incluídos.
- Testes de build, biblioteca, volume e instalação/desinstalação: docs/VALIDACAO.md.
# 0.6 — 18/10/2026

## Isolar voz (RNNoise)

- Botão **Isolar voz** ao lado do slider da voz: aplica o RNNoise (xiph/rnnoise 0.2, BSD-3, em `native/rnnoise/`) só no microfone, antes da mistura. Os áudios do RoxyPad não passam pelo filtro, e o Discord pode ficar no modo estúdio. A escolha fica salva em `isolar-voz.txt`.
- O pacote 0.2 não traz o `os_support.h` usado pelo caminho genérico; foi escrito um mínimo equivalente (`native/rnnoise/os_support.h`).
- Medido em silêncio no CABLE Output: ruído de ~0,0005 (desligado) para ~0,0001–0,0002 (ligado). A naturalidade da voz ainda depende de escuta.
- Observação: a cada recompilação do `RoxyBridge.exe`, o Kaspersky trata o arquivo como app novo e segura o acesso ao microfone até o aviso ser respondido; enquanto isso, o bridge fica em "iniciando".

# 0.5.2 — 18/10/2026

## Correção do eco em loop

- O áudio ia ao Discord repetido várias vezes e cada vez mais alto. Causa: a captura por processo pega o RoxyPad **e os filhos dele**; o RoxyBridge era filho e capturava a própria saída no cabo, realimentando a mistura sem fim.
- Agora o `RoxyBridge.exe` se relança com `--child` e o primeiro processo sai na hora; o bridge real fica fora da árvore do RoxyPad. Ele continua fechando junto com o app.
- Medido no CABLE Output com um tom de 1 s: antes ficava preso em ~0,115 até fechar; agora fica em silêncio logo após o tom.

# 0.5.1 — 18/10/2026

## Volume da voz

- Novo slider **Voz** (100% a 800%, padrão 400%) embaixo do player: aumenta só a sua voz antes de ir para o CABLE; os memes não mudam. A escolha fica salva em `mic-gain.txt` na biblioteca.
- Motivo: o modo estúdio do Discord desliga o ganho automático, e o Fuxi-H3 capta baixo (cerca de −34 dBFS falando). O controle de volume do Windows não ajuda: a faixa do driver é de 0 a 0,1 dB.
- Limitador suave acima de 0,8 para gritos ou memes altos saturarem de leve em vez de estourar.
- Medido: com ganho 400%, a saída ficou 4,0x o mic (0,0035 → 0,0140).

# 0.5 — 18/10/2026

## Saída no microfone refeita com VB-CABLE

- Removida a abordagem por APO (`RoxyMicAPO.dll`): o Windows não carrega APOs sem assinatura e sem pacote de driver, por isso nada chegava ao microfone. Registro do Fuxi-H3 restaurado e DLL apagada.
- `RoxyBridge.exe` agora mistura o microfone padrão com o áudio do RoxyPad e envia ao **CABLE Input**; no Discord use **CABLE Output**.
- O app mostra "VB-CABLE não instalado" no `!` quando o cabo não existe e se reconecta sozinho quando dispositivos mudam.
- Testado com tom de teste em outra saída: captura do app, mistura e silêncio ao pausar conferem. Falta teste em chamada real.

# 0.4 — 18/10/2026

## Integração direta com o microfone

- Executável recompilado com o ícone R como recurso Win32 para que o Explorador e o atalho do menu Iniciar exibam a marca corretamente.
- Adicionado o `RoxyBridge.exe`, que captura somente o áudio do processo RoxyPad por loopback e publica PCM para a APO.
- Adicionado o `RoxyMicAPO.dll`, que mistura o áudio do player ao fluxo de captura do Fuxi-H3. Play ativa a mistura; pausa, fim e parar deixam apenas a voz passar.
- O controle de volume do player também regula o áudio encaminhado ao microfone. A saída do Discord não entra no loopback do RoxyPad.
- A integração foi registrada apenas no endpoint de captura do Fuxi-H3 neste PC. O instalador guarda os valores anteriores em `C:\ProgramData\RoxyPad\AudioIntegration\backup.json`; `AudioIntegration.ps1 -Uninstall` restaura esses valores e remove apenas os registros próprios do RoxyPad.
- O teste de sinal real detecta captura ativa antes de tocar o tom. Como o Discord estava usando o microfone, a reprodução de teste foi bloqueada antes de emitir qualquer som.
- O harness offline do APO agora aborta sem alterar o mapeamento quando encontra uma instância do canal de áudio em execução.

## Estado de validação

- Build x64 e testes internos do misturador já passaram em execução isolada: voz em passthrough, áudio misturado, entrada silenciosa, volume zero e pausa.
- Nesta conferência, os resultados do harness offline executado com o RoxyPad aberto foram descartados: o harness antigo reutilizou o mapeamento do app. A correção evita repetir isso; a execução corrigida deve ser feita com o app fechado.
- Registro do endpoint, backup, classe COM e caminho da DLL foram conferidos. O canal do RoxyPad está pronto e inativo.
- Ainda não foi confirmado que o Windows carregou a APO no fluxo ativo do Discord, nem foi feita uma escuta ou captura real de voz e meme juntos. A presença de efeitos padrão em `IAudioEffectsManager` não identifica por si só o CLSID do RoxyPad.

# 0.3 — 18/10/2026

## Interface

- Fundo escuro com faixas de luz violeta/azul, cantos e contorno refinados a partir das referências.
- Poppins Bold empacotada com licença OFL; novo ícone R em ICO e PNG.
- Aviso de funcionamento local movido do rodapé para `!` com dica ao passar o mouse.
- Removida a caixa fixa de arrastar. O destaque de soltar aparece apenas durante o arraste; `+` continua no topo.
- Player compacto mostra a última faixa escolhida e oferece reproduzir/pausar, liberando mais altura para a biblioteca.
- O botão quadrado de parar foi substituído pelo ícone de volume. A barra e a porcentagem aparecem ao passar o mouse; clicar no ícone alterna entre mudo e o último volume usado.
- A lista manteve os 18 itens existentes. Nenhuma alteração foi feita nos arquivos de áudio ou no índice da biblioteca durante a inspeção visual.

## Verificação

- Compilação x64 passou e a janela foi inspecionada com a biblioteca existente.
- O ponteiro sobre o ícone abriu o controle; clicar alternou Mudo e restaurou o volume salvo em 100%. Não havia áudio tocando.
- Ajustar a faixa do slider, arraste real de arquivos, nova reprodução e integração ao microfone ainda não foram exercitados nesta atualização.

# 0.2 — 18/10/2026

## Aparência

Contorno em gradiente discreto, sombra, cantos arredondados, painel separado para reprodução, barras finas com controle circular e rolagem estreita. Volume tem porcentagem e indicação de mudo. Clique direto no trilho escolhe a posição.

## Correção do volume

O defeito foi reproduzido no computador: definir `MediaPlayer.Volume = 0.13` e chamar `Close()` devolvia `Volume = 0.5`. O aplicativo usava Close ao parar/trocar faixas; o slider não acompanhava essa redefinição.

O adaptador `LocalAudio` guarda o valor desejado fora do player e o reaplica em Close, Open, MediaOpened e Play. Zero aplica também IsMuted. A preferência é gravada separadamente do índice da biblioteca, com atualização atômica, e restaurada ao iniciar.

## Verificação

- 12 ciclos de Close sobre o MediaPlayer real passaram em quatro níveis de volume.
- WAV silencioso abriu em 13%, avançou a posição, aceitou mudança em reprodução e reabriu em zero/mudo.
- Clique na barra da janela mostrou 9%; fechar/reabrir preservou esse valor. Ao terminar a conferência, a barra foi devolvida para aproximadamente 50%.
- Biblioteca do usuário manteve os mesmos 18 registros, com hash idêntico ao backup anterior à atualização.
- A nova janela foi inspecionada visualmente. Arquivo `previa-v02.png` mostra a janela real.

Não houve escuta perceptual dos memes, captura do microfone nem teste em chamada. Esta versão continua com reprodução local; a integração direta ao microfone ainda não está implementada.

