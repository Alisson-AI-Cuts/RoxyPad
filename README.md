# RoxyPad

Biblioteca pessoal de sons para Windows, com reprodução no fone e mistura de voz + áudios para Discord e outros aplicativos.

**[Baixar o instalador mais recente](https://github.com/Alisson-AI-Cuts/RoxyPad/releases/latest)** · [Guia de instalação](docs/INSTALACAO.md) · [Licença de uso pessoal gratuito](LICENSE.txt)

## Instalar e usar

1. Baixe **RoxyPad-0.6.2-Setup-x64.exe** na página de releases e execute.
2. Ao terminar, abra o **Guia de primeiros passos**. Se ainda não tiver VB-CABLE, marque a opção de abrir seu instalador oficial. Clique em **Install Driver**, autorize o administrador e reinicie o Windows.
3. Deixe o microfone físico como entrada padrão do Windows e seu fone como saída padrão. Permita o acesso de aplicativos de área de trabalho ao microfone.
4. No Discord, escolha **CABLE Output** na entrada e **seu fone** na saída. Mantenha o RoxyPad aberto, inclusive na bandeja.
5. Abra o RoxyPad no menu Iniciar, clique em **+** para adicionar seus áudios e clique num item para tocar. Comece com volume baixo e faça uma chamada de teste antes de usar ao vivo.

O pacote já contém aplicativo, motor de áudio, fonte, licenças, guia offline e o pacote oficial do VB-CABLE. O cabo tem instalação própria e exige administrador e reinicialização. Sem ele, a reprodução local continua disponível. Não é necessário instalar Python, Node.js, Visual Studio ou um runtime C++ para usar o app.

## Requisitos

- **Windows 11 x64**, com .NET Framework 4.8 ou superior e componentes multimídia do Windows. Edições N precisam do Media Feature Pack.
- Microfone e saída de áudio funcionando. VB-CABLE para enviar a mistura aos aplicativos.
- Internet apenas para baixar; o uso e o guia são locais.
- Windows 10, ARM64, macOS e Linux não são suportados por este pacote. A captura por processo depende de API disponível a partir do build 20348; Windows 10 de desktop comum fica abaixo disso: [Microsoft](https://learn.microsoft.com/en-us/windows/win32/api/audioclientactivationparams/ne-audioclientactivationparams-process_loopback_mode).

## Recursos

- MP3, WAV, M4A, AAC e WMA, conforme codecs disponíveis no Windows.
- Busca, favoritos, renomeação, exportação sem sobrescrever e importação por seleção/arraste.
- Controle de volume dos sons, ganho da voz e isolamento por RNNoise.
- Janela compacta escura e ícone na bandeja. **× esconde; Sair na bandeja encerra.**
- Biblioteca local em `%LOCALAPPDATA%\RoxyPad`. Originais preservados; remoção da lista mantém a cópia interna nesta versão.

## Privacidade e desinstalação

O app não possui conta, telemetria ou upload implementados. O motor acessa o microfone e o áudio do processo para criar a mistura local. Discord e outros aplicativos têm suas próprias políticas.

Desinstale por **Configurações → Aplicativos → RoxyPad**. Sua biblioteca pessoal é preservada. O VB-CABLE é um driver independente: sua remoção é feita pelo instalador oficial dele e requer reinício. Não há inicialização automática configurada pelo RoxyPad.

## Licença

Copyright © 2026 Alisson / Alisson-AI-Cuts. **Uso pessoal gratuito, privado e não comercial. Venda, monetização, uso profissional remunerado e redistribuição dependem de autorização escrita.** O código está visível para estudo e alterações pessoais; este projeto não é open source. Consulte [LICENSE.txt](LICENSE.txt).

Dependências mantêm seus direitos e termos: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). VB-CABLE é donationware da **VB-Audio**, [origem e doações](https://vb-audio.com/Cable/); uso profissional pode exigir licença do fornecedor.

## Compilar

Em Windows x64, instale Visual Studio/Build Tools com **Desenvolvimento para desktop com C++**, Windows SDK e .NET Framework 4.x. Execute:

```powershell
powershell -NoProfile -File .\build.ps1
.\RoxyPad.exe --self-test "$env:TEMP\RoxyPad-tests"
```

Para gerar a distribuição: veja [BUILD.md](docs/BUILD.md). O compilador localiza o Visual Studio por `vswhere`; não depende de caminhos do computador do autor. O bridge usa CRT estático.

## Estado de validação

Veja [VALIDACAO.md](docs/VALIDACAO.md). Os relatos de uso anteriores estão em [INTEGRACAO-AUDIO.md](INTEGRACAO-AUDIO.md); não representam testes desta versão em todos os computadores. Esta versão não tem certificado de assinatura de código: o Windows pode mostrar **editor desconhecido**. Confira origem e SHA-256 da release. Não desative antivírus ou proteções do Windows.
