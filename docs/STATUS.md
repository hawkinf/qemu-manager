# STATUS — estado vivo de QEMU Manager

> **REGRA OBRIGATÓRIA (qualquer IA ou pessoa):** toda tarefa que altere este repositório DEVE terminar atualizando este arquivo
> ("Estado atual", "Pendências" e uma linha no "Log") **no mesmo commit** da mudança. Veja `AGENTS.md`.
> Se o estado aqui divergir do código, o código vale — corrija este arquivo.

Última atualização: 2026-11-10 (Grok 4.6).

## Objetivo
Gerenciador gráfico (WinForms) e scripts de máquinas virtuais QEMU, com perfis prontos para Windows, Linux, macOS e outras arquiteturas.

## Estado atual
| Módulo / área | Estado | Observações |
|---|---|---|
| GUI C# WinForms (`programa/QemuManager.cs`) | 🔄 | Abas VMs/Principal/Hardware/Extra, perfis, tooltips, iniciar VM |
| Ícones / recursos | ✅ | `app.ico` no exe e na janela; PNGs 16/32 embutidos; botões e abas com ícone |
| Salvar/carregar config (C#) | 🔄 | Lista/editar/excluir JSON existem; falta botão Salvar da tela atual |
| Recursos avançados QEMU (C#) | ⏳ | UEFI/pflash, TPM, 9p, kernel, boot order, snapshots — só no Python |
| GUI Python (`qemu-manager-definitivo.py`) | ✅ | Referência completa (Tkinter) |
| Script Linux | ⏳ | Citado no handoff antigo; arquivo ainda não está neste repo |

Legenda: ✅ pronto · 🔄 em andamento · ⏳ não iniciado · ⚠️ com problema.

## Como rodar / validar
```powershell
cd G:\vm\programa
.\compilar-qemu-manager.ps1
# ou:
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
Start-Process .\publish\QemuManager.exe
```
Requisitos: .NET SDK 8+, QEMU em `C:\Program Files\qemu\`, ISOs/discos em `G:\vm\` (VirtIO esperado em `G:\vm\virtio-win.iso`).
Configs das VMs: `%USERPROFILE%\QEMU_Manager_Configs\*.json`.

## Decisões de arquitetura (não reverter sem motivo)
- 2026-11-10 — ícones vivem em `programa/Resources/` e entram como `EmbeddedResource` + `ApplicationIcon`; a UI não depende de `app.ico` ao lado do exe.
- 2026-11-10 — C# WinForms é a versão “atual” para o usuário no Windows; o Python permanece como referência de funcionalidades a portar.
- 2026-11-10 — aceleração no Windows é WHPX (`-accel whpx`).

## Pendências / riscos conhecidos
- Salvar a configuração atual em JSON na versão C# (Python já faz) — responsável: qualquer IA
- Portar UEFI/pflash, TPM, 9p share, kernel/initrd, boot order, snapshots do Python para o C#
- Validação de caminhos (disco, ISO, qemu-system-*.exe) com mensagem amigável
- Log da saída do QEMU na GUI
- Trazer `qemu-manager-linux.sh` para este repositório se ainda for desejado

## Como continuar (qualquer IA)
1. Leia `AGENTS.md`, este arquivo e `docs/ARCHITECTURE.md`.
2. `git status`, `git log --oneline -15`.
3. Pegue a primeira pendência acima (ou pergunte ao usuário) e trabalhe em passos pequenos.
4. Atualize este arquivo e commite junto com a mudança.

## Log (mais recente primeiro; acrescente uma linha por tarefa)
- 2026-11-10 — Grok 4.6 — ícones profissionais em `programa/Resources/` (app.ico 16–256 + 12 PNGs), embutidos no csproj, janela/abas/botões usam `AppIcons`; `dotnet publish` ok, exe em `programa/publish/QemuManager.exe` com ícone associado 32x32. Validar: abrir o exe e conferir ícone da janela, abas e botões Atualizar/Iniciar/Editar/Excluir.
- 2026-11-10 — projeto criado com o padrão de handoff (Grok 4.6).
