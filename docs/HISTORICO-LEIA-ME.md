> Documento histórico. Para instalar a distribuição atual, consulte README.md e INSTALACAO.md.

# RoxyPad — prévia local 0.5

Aplicativo Windows compacto para uma biblioteca pessoal de áudios. Nome provisório.

**A versão 0.5 envia os áudios ao microfone pelo VB-CABLE (cabo de áudio virtual gratuito).**

## Saída no microfone (0.5)

- O `RoxyBridge.exe` captura o seu microfone padrão do Windows e o áudio tocado pelo RoxyPad, mistura os dois e envia para o **CABLE Input** do VB-CABLE.
- No Discord (ou em outro app), escolha **CABLE Output** como dispositivo de entrada. Sua voz passa pelo RoxyPad, por isso ele precisa ficar aberto (na bandeja) durante a chamada.
- O som só vai para o microfone enquanto toca; pausado ou parado, passa apenas a voz. O volume do player vale para o seu fone e para o microfone.
- A saída do Discord não é capturada, então o áudio da chamada não volta para o microfone.
- Se o VB-CABLE não estiver instalado, o app avisa no `!` e toca só localmente. Detalhes em `INTEGRACAO-AUDIO.md`.

## Mudanças da versão 0.3

- Visual renovado com fundo escuro e faixas violeta/azul inspiradas na referência, contorno suave e cantos arredondados.
- Fonte Poppins Bold incluída nos arquivos do app, com licença OFL em `Assets/OFL.txt`.
- Ícone próprio com a letra R em `Assets/RoxyPad.ico` e prévia PNG.
- O aviso saiu do rodapé e fica no botão `!`; passe o mouse para ver o resumo ou clique para abrir os detalhes.
- A área fixa de arrastar foi removida. Use `+` para escolher arquivos; ao arrastar um formato aceito sobre a janela, a indicação aparece temporariamente.
- O player mostra o áudio escolhido por último, com controle de reproduzir/pausar. O ícone de alto-falante substitui o botão quadrado; passe o mouse para abrir a barra de volume e clique no ícone para silenciar/restaurar.
- Player mais compacto para dar mais espaço à lista. Textos e ícones de ação foram alinhados ao centro quando apropriado.

## Mudanças da versão 0.2

- Bordas com contorno suave, cantos arredondados e sombra discreta.
- Barras próprias de reprodução e volume: trilho fino, preenchimento violeta, controle circular e clique direto na posição.
- Rolagem da biblioteca com barra estreita.
- Volume mostra porcentagem, indica Mudo em zero e guarda a preferência ao reabrir.
- Corrigido bug reproduzido: `MediaPlayer.Close()` restaurava o volume real para 50%, deixando o slider com outro valor. A escolha agora fica fora do player e é reaplicada ao fechar, abrir e iniciar cada áudio.
- Os arquivos e os nomes existentes da biblioteca foram preservados.

## Abrir

Execute `RoxyPad.exe` ou procure **RoxyPad** no menu Iniciar. O atalho do menu Iniciar aponta para esta versão e usa o ícone R embutido no executável. Não exige instalação, administrador, conexão com a internet ou dependências baixadas. Usa .NET Framework/WPF já presente no computador consultado. O executável é uma compilação local não assinada; não é um instalador certificado.

O botão × esconde a janela. O ícone da bandeja oferece Abrir, Parar áudio e Sair. Abrir o executável novamente também solicita que a instância existente reapareça. Não configura inicialização automática.

## Biblioteca

- Clique em `+` ou arraste arquivos para a janela. A área de arraste só aparece durante o gesto.
- Formatos aceitos nesta prévia: MP3, WAV, M4A, AAC e WMA. A decodificação depende dos codecs do Windows; extensão aceita não garante que um arquivo específico seja reproduzível.
- Nenhum limite artificial de duração. Arquivos longos e outros formatos ainda precisam de testes de reprodução.
- Clique em um áudio para reprodução local pela saída padrão do Windows. Um áudio substitui o anterior. Escape e Parar encerram a reprodução.
- Botão direito: renomear, favoritar, salvar cópia em Downloads e remover da lista.
- A exportação usa a pasta Downloads configurada pelo Windows e evita sobrescrever nomes existentes.
- Os originais não são modificados. Importar cria uma cópia em `%LOCALAPPDATA%\RoxyPad\audio`; nomes e favoritos ficam em `library.xml`.
- Remover da lista retém a cópia interna para recuperação nesta prévia. Ela continua ocupando espaço. Limpeza definitiva ainda não foi implementada.
- O índice anterior é preservado em `library.xml.bak`. Um índice inválido gera erro, sem reset silencioso.

## Evidência de verificação — 18/10/2026 (versão 0.2)

- Compilação local x64: passou.
- Testes de biblioteca: passaram para cópia byte a byte, nomes de origem repetidos, reabertura, renomeação, favoritos, busca, exportação sem sobrescrita, nome reservado do Windows, remoção preservando original e rejeição de extensão não aceita.
- Janela real: aberta e inspecionada visualmente por Computer Use.
- Botão ×: verificado que oculta a janela mantendo o processo ativo. Abrir o executável novamente trouxe a mesma instância de volta.
- Seletor de arquivos: abriu. O preenchimento pela automação não funcionou; importação completa via seletor e arrastar/soltar ainda não validados pela interface.
- Qualidade audível, duração longa, dispositivo de saída, latência e reconexão do headset: não validados.
- Regressão do volume: 12 ciclos de fechamento passaram com 0%, 13%, 50% e 100%, verificando volume e mute reais do MediaPlayer.
- Reprodução de WAV silencioso pelo decoder real: abertura a 13%, avanço de posição, mudança de volume durante reprodução e substituição da fonte a 0% passaram. Isso não é uma avaliação perceptual de áudio.
- Interface 0.2: clique no volume atualizou a porcentagem; preferência permaneceu após encerrar/reabrir. Biblioteca reabriu com os 18 itens importados pelo usuário. O usuário relatou que a primeira versão funcionava bem, exceto o volume.
- Clique real no ícone da bandeja e menu Sair: ainda não validados.
- Transmissão ao microfone: mixagem testada com tom de teste em outra saída de áudio; com VB-CABLE + Discord ainda falta o teste na chamada.

## Verificação da interface 0.3 — 18/10/2026

- Compilação x64 da interface 0.3: passou.
- Janela real aberta com a biblioteca existente de 18 itens e inspecionada visualmente; não foi necessário alterar o índice nem os arquivos de áudio.
- Ícone PNG/ICO gerado e empacotado; fonte Poppins e licença incluídas na pasta `Assets`.
- O controle de volume abre ao passar o ponteiro sobre o alto-falante. O clique silenciou e restaurou o volume que estava salvo (100%); não havia áudio tocando durante essa conferência.
- A faixa do volume não foi arrastada nesta atualização. O código mantém o mesmo slider e salvamento de preferência da versão anterior.
- A sobreposição temporária e o arraste real ainda não foram exercitados com um arquivo; a seleção pelo botão `+` já existia na versão anterior.
- Reprodução, volume, bandeja e menu de contexto mantêm as verificações acumuladas da versão 0.2 descritas acima. Não fiz nova escuta de áudio nesta atualização.

## Recompilar

Execute `build.ps1` no PowerShell. Ele usa o compilador C# incluído no .NET Framework e copia a fonte e a licença para `Assets`. Feche a instância desta prévia antes de substituir o executável.

Testes: execute `RoxyPad.exe --self-test <pasta-de-testes>` e leia `result.txt` nessa pasta. Os testes deixam fixtures isoladas para inspeção, não usam a biblioteca pessoal.

O parâmetro `--library <pasta>` permite uma biblioteca separada para QA. Não abra simultaneamente duas bibliotecas: a primeira instância prevalece.

Veja `INTEGRACAO-AUDIO.md` para a investigação e as etapas pendentes.

