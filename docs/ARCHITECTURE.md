# ARCHITECTURE — QEMU Manager

Mapa do código para quem chega sem contexto (qualquer IA ou pessoa). Mantenha curto e verdadeiro; ao mudar a estrutura, atualize aqui
no mesmo commit.

## Visão geral
Aplicativo Windows para criar, configurar e iniciar VMs QEMU. A GUI C# (WinForms, .NET 8) é o produto atual. O Python Tkinter é a referência mais completa de opções QEMU a portar.

## Estrutura de pastas
- `programa/QemuManager.cs` — Form1, Config, Program (um arquivo)
- `programa/AppIcons.cs` — carrega ícones de recursos embutidos
- `programa/QemuManager.csproj` — WinExe net8.0-windows, `ApplicationIcon` + EmbeddedResource
- `programa/Resources/` — `app.ico`, `app.png`, `{nome}.png` 32px e `{nome}-16.png`
- `programa/compilar-qemu-manager.ps1` — publish single-file
- `programa/qemu-manager-definitivo.py` — GUI Python de referência
- `programa/HANDOFF-QEMU-MANAGER.md` — notas originais; o estado vivo é `docs/STATUS.md`
- `docs/` — STATUS e ARCHITECTURE (handoff entre IAs)

## Fluxos principais
- Abrir app → carrega perfil "Windows 10 x64" → lista JSON em `%USERPROFILE%\QEMU_Manager_Configs\`.
- Aba Principal/Hardware/Extra → monta `Config` → `StartVm()` escolhe `qemu-system-*.exe`, cria qcow2 se faltar (`qemu-img`), dispara processo com WHPX em x86.
- Duplo clique na lista / botão Iniciar → `ApplyConfig` + `StartVm`.
- Excluir remove só o JSON, não o disco.

## Dados e integrações
- Banco/armazenamento: arquivos JSON em `%USERPROFILE%\QEMU_Manager_Configs\` (classe `Config`).
- Binários: `C:\Program Files\qemu\qemu-system-*.exe` e `qemu-img.exe`.
- Discos/ISOs: padrão `G:\vm\` (WorkDir).

## Convenções
- Layout WinForms com coordenadas absolutas nas abas Principal/Hardware.
- Tooltips via `ToolTip` em todos os controles.
- Ícones: `AppIcons` (EmbeddedResource `QemuManager.Resources.*`). Não ler arquivo solto ao lado do exe.

## Armadilhas conhecidas
- `ApplicationIcon` vazio no csproj fazia o exe nascer sem ícone mesmo existindo `app.ico`.
- Publish single-file: recursos precisam ser EmbeddedResource; copiar PNG para `publish/` não é necessário.
- Argumentos extras em `StartVm` fazem split por espaço — quebra valores com espaços mesmo entre aspas simples.
