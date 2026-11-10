# ARCHITECTURE — QEMU Manager

Mapa do código para quem chega sem contexto (qualquer IA ou pessoa). Mantenha curto e verdadeiro; ao mudar a estrutura, atualize aqui
no mesmo commit.

## Visão geral
Aplicativo Windows para criar, configurar e iniciar VMs QEMU. A GUI C# (WinForms, .NET 8) é o produto atual. O Python Tkinter e o script Linux cobrem os mesmos conceitos fora do Windows.

## Estrutura de pastas
- `programa/QemuManager.cs` — `Config`, `Form1`, `Program` (um arquivo)
- `programa/AppIcons.cs` — carrega ícones de recursos embutidos
- `programa/QemuManager.csproj` — WinExe net8.0-windows, `ApplicationIcon` + EmbeddedResource
- `programa/Resources/` — `app.ico`, `app.png`, `{nome}.png` 32px e `{nome}-16.png`
- `programa/compilar-qemu-manager.ps1` — publish single-file
- `programa/qemu-manager-definitivo.py` — GUI Python de referência
- `programa/qemu-manager-linux.sh` — menu Linux (listar/iniciar/criar/editar/excluir/snapshots); `--self-test`, `--print-args`, `--check`. Args montados em Python (mesmo JSON da GUI). Aceleração: KVM ou TCG.
- `programa/HANDOFF-QEMU-MANAGER.md` — notas originais; o estado vivo é `docs/STATUS.md`
- `docs/` — STATUS e ARCHITECTURE (handoff entre IAs)

## Fluxos principais
- Abrir app → perfil "Windows 10 x64" → lista JSON em `%USERPROFILE%\QEMU_Manager_Configs\`.
- Aba Principal/Hardware/Extra → `GetCurrentConfig()` → `StartVm()` escolhe `qemu-system-*.exe`, cria qcow2 se faltar (`qemu-img`), dispara o processo (WHPX em x86) e grava stdout/stderr na aba Log.
- Salvar pede um nome e escreve `{ConfigDir}/{nome}.json`.
- Duplo clique na lista / Iniciar → `ApplyConfig` + `StartVm`.
- Excluir remove só o JSON, não o disco.

## Dados e integrações
- Armazenamento: JSON em `%USERPROFILE%\QEMU_Manager_Configs\` (classe `Config`; leitura aceita PascalCase e snake_case).
- Binários: `C:\Program Files\qemu\qemu-system-*.exe` e `qemu-img.exe`.
- Discos/ISOs: padrão `G:\vm\` (WorkDir).
- QEMU extra: `-bios`, `-drive if=pflash`, virtio-9p, `-kernel`/`-initrd`/`-append`, `-loadvm`, virtio-rng/balloon, tpm-tis + swtpm, `-nic user,hostfwd=`.

## Convenções
- Layout WinForms com coordenadas absolutas e `AutoScroll` nas abas.
- Tooltips via `ToolTip` em todos os controles.
- Ícones: `AppIcons` (EmbeddedResource `QemuManager.Resources.*`).
- Argumentos do QEMU via `ProcessStartInfo.ArgumentList`. Extra args: split respeitando aspas duplas.

## Armadilhas conhecidas
- `ApplicationIcon` vazio no csproj fazia o exe nascer sem ícone mesmo existindo `app.ico`.
- Publish single-file: recursos precisam ser EmbeddedResource.
- TPM (`swtpm-sock`) é Unix-socket; no Windows quase sempre precisa de outro backend.
- WHPX só em guests x86/i386; ARM/RISC-V/PPC usam só `-machine`.
