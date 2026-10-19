# Instalação e solução de problemas

## Instalação normal

Baixe o Setup na release oficial. A instalação do RoxyPad é por usuário e não exige administrador. Ela cria atalhos, inclui todos os arquivos de execução e registra o desinstalador. Atualizações usam a mesma pasta e preservam a biblioteca; encerre o app pela bandeja antes de atualizar.

O guia offline também fica no menu Iniciar. Selecione no final do Setup **Abrir instalador oficial do VB-CABLE** somente se ainda não possuir o cabo. Ele abre uma janela própria e pode pedir administrador. Clique em Install Driver e reinicie o PC após concluir. Não reinstale um cabo que já funciona. Origem: https://vb-audio.com/Cable/ ; donationware, contribuições são bem-vindas.

Se pulou essa etapa, abra `ThirdParty\VB-CABLE\VBCABLE_Setup_x64.exe` na pasta instalada como administrador. O pacote oficial está inteiro, com seus arquivos de driver e avisos.

## Configuração para Discord

| Local | Escolha |
| --- | --- |
| Windows: entrada padrão | Seu microfone físico |
| Windows: saída padrão | Seu fone ou alto-falante |
| Discord: entrada | CABLE Output (VB-Audio Virtual Cable) |
| Discord: saída | Seu fone ou alto-falante |

**Nunca coloque a saída do Discord ou a saída padrão do Windows em CABLE Input:** isso pode causar retorno ou misturar a chamada. O RoxyPad envia sua mistura ao CABLE Input sozinho.

Permita acesso ao microfone em Configurações → Privacidade e segurança → Microfone, incluindo aplicativos de área de trabalho. Mantenha o RoxyPad aberto durante a chamada. Verifique o botão `!` para detalhes do áudio. Fechar o app interrompe o caminho da voz pelo cabo; para chamar sem ele, volte a entrada do Discord ao microfone físico.

Comece em volume baixo. Ajuste **Voz** para a voz e o alto-falante do player para os sons. **Isolar voz** trata apenas o microfone; o portão pode cortar fala baixa. Se sons forem cortados no Discord, teste desligar supressão de ruído/cancelamento de eco e ajuste a sensibilidade. Use fone e faça um teste fora de chamadas ao vivo.

## Problemas comuns

- **VB-CABLE não instalado:** instale pelo pacote incluído e reinicie. Confirme os dois dispositivos CABLE no Windows.
- **Microfone não encontrado:** confirme o microfone físico padrão, permissões e conexão do headset.
- **Bridge fica iniciando:** verifique se o antivírus está pedindo permissão de microfone para RoxyBridge.exe. Não desative a proteção. Confira origem/hash do pacote antes de autorizar.
- **Sem voz/sons no Discord:** saída deve ser o fone, entrada CABLE Output; feche pela bandeja e reabra o RoxyPad. Se acabou de instalar o driver, reinicie o Windows.
- **Windows N / erro de codec:** instale o Media Feature Pack pelo Windows e use arquivo em formato compatível. Um arquivo com extensão aceita pode ter codec não suportado.
- **Janela sumiu ao clicar ×:** procure o ícone R na bandeja ou abra novamente pelo menu Iniciar.
- **Som baixo/distorcido:** diminua primeiro os ganhos e o volume. O ganho de voz pode chegar a 800%; ajuste conforme seu microfone.
- **Arquivo bloqueado pelo Windows:** confira release e SHA-256. O app não é assinado; não há garantia de ausência de alertas e não se deve desativar o antivírus.

## Portátil

Extraia o ZIP inteiro numa pasta de sua escolha e abra RoxyPad.exe. Preserve Assets, Licenses e RoxyBridge.exe ao lado do aplicativo. O driver é instalado separadamente pelo mesmo pacote ThirdParty. A biblioteca continua em `%LOCALAPPDATA%\RoxyPad`; não acompanha automaticamente a pasta portátil.

## Seus dados

Importar copia arquivos para `%LOCALAPPDATA%\RoxyPad\audio`. Metadados em library.xml; preferências em arquivos de texto. Faça backup dessa pasta com o aplicativo fechado. O desinstalador preserva os dados. Remover um item da lista também preserva a cópia interna nesta versão; limpeza manual exige cuidado.
