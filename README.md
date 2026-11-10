# QEMU Manager

Gerenciador de máquinas virtuais QEMU para Windows (GUI) e Linux (terminal).

## Windows

Requisitos: .NET SDK 8+, QEMU em `C:\Program Files\qemu\`.

```powershell
cd programa
.\compilar-qemu-manager.ps1
Start-Process .\publish\QemuManager.exe
```

Configs: `%USERPROFILE%\QEMU_Manager_Configs\`
Discos/ISOs: `G:\vm\` (VirtIO: `G:\vm\virtio-win.iso`)

## Linux (terminal)

Requisitos: `python3`, `qemu-img`, `qemu-system-*` (pacote `qemu-system` / `qemu-utils`).

```bash
chmod +x programa/qemu-manager-linux.sh
./programa/qemu-manager-linux.sh
```

Comandos úteis:

```bash
./programa/qemu-manager-linux.sh --check
./programa/qemu-manager-linux.sh --list
./programa/qemu-manager-linux.sh --start "Windows 10 x64"
./programa/qemu-manager-linux.sh --print-args ~/QEMU_Manager_Configs/ubuntu.json
./programa/qemu-manager-linux.sh --self-test
```

Configs: `~/QEMU_Manager_Configs/`
Discos: `~/vm/` (ou `QEMU_MANAGER_WORK`)

O JSON é o mesmo da GUI Windows (PascalCase ou snake_case). Caminhos de disco precisam ser Linux (`/home/...`), não `G:\...`.

## PXE na rede interna

Perfil **Teste PXE (rede interna)** (combo de perfis na GUI / opção 3 no Linux).

- Windows: modo `tap`, interface `OpenVPN TAP-Windows6`. Em `ncpa.cpl`, faça a ponte do TAP com a placa da LAN (`cabo`). NAT (`user`) não vê o servidor PXE.
- Linux: modo `bridge`, interface `br0` (ou `tap0`).
- NIC `e1000`, boot `n`, `bootindex=1`, sem disco (`DiskIf=none`).

Aceleração: KVM se `/dev/kvm` existir; senão TCG.

## Handoff para IAs

Leia `AGENTS.md` e `docs/STATUS.md` antes de alterar o código.
