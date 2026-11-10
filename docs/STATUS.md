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
| GUI C# WinForms (`programa/QemuManager.cs`) | ✅ | Abas VMs/Principal/Hardware/Extra/Log; perfis; tooltips; iniciar VM |
| Ícones / recursos | ✅ | `app.ico` no exe e na janela; PNGs 16/32 embutidos |
| Salvar/carregar config (C#) | ✅ | Salvar, Carregar JSON, Abrir pasta; lê PascalCase e snake_case |
| Recursos avançados QEMU (C#) | ✅ | UEFI/pflash, 9p, kernel/initrd, boot, snapshot, RNG, balloon, TPM, hostfwd |
| Validação de caminhos | ✅ | Disco/QEMU/ISO/firmware/kernel com mensagens em português |
| Log da saída do QEMU | ✅ | Aba Log captura stdout/stderr e código de saída |
| GUI Python (`qemu-manager-definitivo.py`) | ✅ | Referência Tkinter (paridade com o C#) |
| Script Linux | ✅ | `programa/qemu-manager-linux.sh` — listar/iniciar/criar |

Legenda: ✅ pronto · 🔄 em andamento · ⏳ não iniciado · ⚠️ com problema.

## Como rodar / validar
```powershell
cd G:\vm\programa
.\compilar-qemu-manager.ps1
Start-Process .\publish\QemuManager.exe
```
Requisitos: .NET SDK 8+, QEMU em `C:\Program Files\qemu\`, ISOs/discos em `G:\vm\` (VirtIO esperado em `G:\vm\virtio-win.iso`).
Configs das VMs: `%USERPROFILE%\QEMU_Manager_Configs\*.json`.
Linux: `bash programa/qemu-manager-linux.sh` (precisa de `qemu-system-*`, `qemu-img`, `python3`).

## Decisões de arquitetura (não reverter sem motivo)
- 2026-11-10 — ícones vivem em `programa/Resources/` e entram como `EmbeddedResource` + `ApplicationIcon`; a UI não depende de `app.ico` ao lado do exe.
- 2026-11-10 — C# WinForms é a versão atual no Windows; o Python permanece como referência.
- 2026-11-10 — aceleração no Windows é WHPX (`-accel whpx`).
- 2026-11-10 — `ProcessStartInfo.ArgumentList` para montar a linha do QEMU (aspas tratadas). Extra args respeitam aspas duplas.
- 2026-11-10 — JSON é gravado em PascalCase; a leitura aceita também snake_case do Python.

## Pendências / riscos conhecidos
- TPM emulado depende de `swtpm` com socket `./swtpm-sock` (igual ao Python); no Windows isso costuma falhar sem um swtpm extra.
- O script Linux não foi executado neste ambiente Windows — validar num host Linux quando possível.

## Como continuar (qualquer IA)
1. Leia `AGENTS.md`, este arquivo e `docs/ARCHITECTURE.md`.
2. `git status`, `git log --oneline -15`.
3. Pegue a primeira pendência acima (ou pergunte ao usuário) e trabalhe em passos pequenos.
4. Atualize este arquivo e commite junto com a mudança.

## Log (mais recente primeiro; acrescente uma linha por tarefa)
- 2026-11-10 — Grok 4.6 — paridade C# com o Python: salvar/carregar, UEFI/pflash, 9p, kernel/initrd/append, boot, snapshots (`qemu-img snapshot -l`), RNG/balloon/TPM, hostfwd, validação de caminhos, aba Log, perfis extras e `qemu-manager-linux.sh`. `dotnet publish` ok; exe abre com título QEMU Manager. Validar: Salvar uma VM, recarregar a lista, Iniciar (sem QEMU a mensagem deve ser amigável) e ver a aba Log.
- 2026-11-10 — Grok 4.6 — ícones profissionais em `programa/Resources/` (app.ico 16–256 + 12 PNGs), embutidos no csproj, janela/abas/botões usam `AppIcons`; `dotnet publish` ok, exe em `programa/publish/QemuManager.exe` com ícone associado 32x32. Validar: abrir o exe e conferir ícone da janela, abas e botões Atualizar/Iniciar/Editar/Excluir.
- 2026-11-10 — projeto criado com o padrão de handoff (Grok 4.6).
