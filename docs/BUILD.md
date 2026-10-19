# Build da distribuição 0.6.1

Requisitos de desenvolvimento: Windows 11 x64, .NET Framework 4.x, Visual Studio/Build Tools com MSVC x64 e Windows SDK. Inno Setup 7.1 x64 para gerar Setup. Usuários finais não precisam dessas ferramentas.

Com uma cópia limpa do código e o compilador Inno disponível:

```powershell
powershell -NoProfile -File .\package.ps1 -ISCC 'C:\caminho\Inno Setup\ISCC.exe'
```

O script compila os dois executáveis, usa CRT estático para o bridge, copia recursos e licenças, obtém o pacote oficial VB-CABLE com SHA-256 fixo e valida a assinatura Authenticode do instalador do fornecedor. Executa o self-test sem iniciar captura do microfone, gera Setup, ZIP e SHA256SUMS em dist. Nenhum driver é instalado durante o build.

O hash fixo é uma trava contra mudanças do download, calculada sobre o pacote obtido do domínio oficial em 19/10/2026; não é uma assinatura independente do fornecedor. Atualizações exigem revisão de origem, assinatura, licença e conteúdo antes de mudar o hash.

O compilador encontra Visual Studio por vswhere. RNNoise usa código genérico, sem extensões de CPU obrigatórias; há aviso de desempenho na compilação. Saídas nativas usam /MT; o aplicativo WPF usa .NET Framework disponível no Windows. O Setup verifica Windows 11 e .NET 4.8. Não é certificado nem assinado pelo autor.

O script recusa empacotar sobre payload antigo. Use outra cópia limpa para refazer a distribuição. O produto pode ser assinado futuramente com certificado próprio; não há assinatura simulada.
