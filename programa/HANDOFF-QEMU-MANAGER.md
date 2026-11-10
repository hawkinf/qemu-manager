# HANDOFF - QEMU Manager

**Data:** 2026-11-10  
**Usuário:** Aguinaldo Baptistini  
**Clone git:** `G:\vm\programa`  
**Trabalho (discos/ISOs/atalhos):** `G:\vm\`  
**Idioma:** Português do Brasil  
**GitHub:** https://github.com/hawkinf/qemu-manager  

O estado vivo é `docs/STATUS.md`. Este arquivo só resume caminhos e como rodar.

---

## O que é este projeto

Gerenciador gráfico (e terminal) de máquinas virtuais QEMU, criado para substituir VirtualBox. Permite criar, configurar, salvar e iniciar VMs com perfis prontos para vários sistemas operacionais.

---

## Arquivos principais

| Arquivo | Descrição | Local versionado |
|---|---|---|
| `qemu-manager-definitivo.py` | GUI Python + Tkinter (referência) | `programa/` |
| `QemuManager.cs` | GUI C# WinForms (produto Windows) | `programa/` |
| `compilar-qemu-manager.ps1` | Publish single-file | `programa/` |
| `qemu-manager-linux.sh` | Menu de terminal Linux | `programa/` |
| `Resources/app.ico` | Ícone da janela/exe | `programa/Resources/` |

Atalhos no diretório de trabalho do usuário:

- `G:\vm\qemu-manager-definitivo.py`
- `G:\vm\QemuManager.exe`
- `G:\vm\qemu-manager-linux.sh`

---

## Como compilar (Windows)

```powershell
cd G:\vm\programa\programa
.\compilar-qemu-manager.ps1
# exe: G:\vm\programa\programa\publish\QemuManager.exe
```

**Requisitos:** .NET SDK 8+, QEMU em `C:\Program Files\qemu\`

Rodar sem compilar:

```powershell
python G:\vm\qemu-manager-definitivo.py
Start-Process G:\vm\QemuManager.exe
```

---

## Recursos

- Abas: VMs, Principal, Hardware, Extra, Log (C#)
- Perfis: Windows 11/10/7/XP/98, Ubuntu x64/ARM, Debian, Alpine, RISC-V, macOS (OpenCore), **Teste PXE (rede interna)**
- Salvar/carregar JSON (`%USERPROFILE%\QEMU_Manager_Configs\`)
- UEFI/pflash, 9p, kernel/initrd, snapshots, RNG, balloon, TPM, hostfwd
- Rede: user (NAT), tap, bridge, none
- PXE LAN: TAP/bridge + e1000 + boot `n` + bootindex=1
- Linux: `--self-test`, `--print-args`, `--check`

---

## Pendências de host (não são código)

1. TPM no Windows exige `swtpm` (socket Unix).
2. PXE interno: em `ncpa.cpl`, ponte do TAP `OpenVPN TAP-Windows6` com a placa **cabo**.
3. VirtIO ISO esperado em `G:\vm\virtio-win.iso` (ainda não está neste PC).
4. No WSL, `qemu-img`/`qemu-system-*` podem faltar; o `--self-test` não precisa deles.

---

## Observações

- Aceleração Windows: só `-accel whpx` (não juntar com `-machine accel=`).
- NAT (`user`) não vê o servidor PXE da LAN.
- ifname TAP com espaço: `ifname="OpenVPN TAP-Windows6"`.
- JSON PascalCase na gravação; leitura também aceita snake_case.

---

## Como continuar

1. `AGENTS.md`
2. `docs/STATUS.md`
3. `docs/ARCHITECTURE.md`
4. `git log --oneline -15`
