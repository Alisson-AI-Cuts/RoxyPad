# Verificação da distribuição 0.6.1 — 19/10/2026

## Verificado nesta preparação

- Build do código C# e C++ em Windows x64, com RNNoise e runtime C++ estático.
- Testes internos passaram: cópia byte a byte, nomes repetidos, reabertura persistida, renomeação, favoritos, busca, exportação sem sobrescrever, nome reservado, preservação do original e rejeição de extensão não suportada.
- Volume/mute do MediaPlayer real preservados em 12 ciclos de fechamento, nos níveis 0%, 13%, 50% e 100%.
- Inno Setup 7.1 portátil oficial com assinatura Authenticode válida de Pyrsys B.V.; build do instalador concluído.
- VB-CABLE Pack45 baixado do domínio oficial. Hash fixo conferido no empacotamento; assinatura Authenticode do setup x64 válida, titular BUREL VINCENT.
- Setup executado silenciosamente numa pasta de QA, com atalhos e tarefas pós-instalação desativados: arquivos extraídos conferidos byte a byte com o payload; registro de desinstalação e versão conferidos.
- Testes internos executados pelo RoxyPad instalado também passaram.
- Desinstalador retornou 0; executável de QA e registro de desinstalação removidos.

## Limites

- Teste na máquina de desenvolvimento, não em VM limpa nem em outros headsets/computadores.
- Não reinstalamos VB-CABLE nem iniciamos uma captura/teste de microfone durante esta preparação. O driver já existe neste PC; a opção pós-instalação ainda requer teste num Windows limpo.
- Não fizemos nova escuta perceptual, chamada real no Discord, avaliação de latência ou teste da nova compilação do bridge em fluxo de áudio. O registro anterior de confirmação do usuário é da versão de 18/10, descrita em INTEGRACAO-AUDIO.md.
- O teste silencioso não comprova navegação visual completa do assistente, criação de atalhos, seletor de arquivos ou arrastar/soltar.
- RoxyPad/bridge/Setup sem assinatura de código do autor. Alertas do Windows/antivírus podem ocorrer.
- Biblioteca pessoal não faz parte da distribuição. Fixtures de teste ficam fora do pacote. Não há dados de conta, áudios pessoais ou dispositivos específicos incluídos na release.
