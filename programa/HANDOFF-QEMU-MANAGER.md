# HANDOFF - QEMU Manager

**Data:** 2026-11-10  
**Usuário:** Aguinaldo Baptistini  
**Diretório de trabalho do usuário:** `G:\vm\`  
**Idioma:** Português do Brasil

---

## O que é este projeto

Gerenciador gráfico (e terminal) de máquinas virtuais QEMU, criado para substituir VirtualBox. Permite criar, configurar, salvar e iniciar VMs com perfis prontos para vários sistemas operacionais.

---

## Arquivos principais

| Arquivo | Descrição | Local |
|---------|-----------|-------|
| `qemu-manager-definitivo.py` | Versão Python + Tkinter (mais completa, com tooltips, todos os recursos) | `/workspace/artifacts/` |
| `QemuManager.cs` | Versão C# WinForms (atual, com tooltips e ícone) | `/workspace/artifacts/` |
| `compilar-qemu-manager.ps1` | Script PowerShell para compilar o C# | `/workspace/artifacts/` |
| `qemu-manager-linux.sh` | Script de terminal para Linux (menus) | `programa/` |
| `app.ico` | Ícone simples da janela | `/workspace/artifacts/` |

---

## Como compilar (Windows)

```powershell
cd G:\vm
# Substitua o QemuManager.cs pelo mais recente
.\compilar-qemu-manager.ps1
# Executável gerado em: G:\vm\publish\QemuManager.exe
```

**Requisitos:** .NET SDK 6+ ou 8+  
**QEMU:** Deve estar instalado em `C:\Program Files\qemu\`

---

## Recursos já implementados

### Versão Python (`qemu-manager-definitivo.py`)
- Abas: VMs, Principal, Hardware, Extra
- Perfis prontos: Windows 11/10/7/XP, Ubuntu, Debian, Alpine, RISC-V, macOS (OpenCore), Windows 98
- Tooltips em todos os controles
- Salvar/carregar configurações em JSON
- Lista de VMs salvas
- Boot order completo (incluindo PXE/LAN)
- UEFI/pflash, TPM, 9p share, kernel/initrd, snapshots, RNG, Balloon, VGA, som, etc.
- Argumentos extras livres

### Versão C# (`QemuManager.cs`) — atual
- Abas: VMs, Principal, Hardware, Extra, Log
- Perfis (Windows 11/10/7/XP/98, Ubuntu x64/ARM, Debian, RISC-V, Alpine, macOS)
- Tooltips e ícones embutidos
- Salvar/carregar JSON, boot, UEFI/pflash, 9p, kernel, snapshots, RNG/balloon/TPM, hostfwd
- Validação de caminhos e log do QEMU
- Caminhos padrão em `G:\vm\`

### Linux (`qemu-manager-linux.sh`)
- Menu interativo no terminal
- Listar, iniciar, criar VMs
- Perfis rápidos

---

## Estrutura de configuração (JSON)

As VMs salvas ficam em: `%USERPROFILE%\QEMU_Manager_Configs\`

Campos principais:
- `Arch`, `Disk`, `Size`, `Iso`, `Virtio`
- `Ram`, `Cpus`, `CpuModel`, `Machine`
- `DiskIf`, `Nic`, `Vga`, `Display`, `Sound`
- `Gl`, `Usb`, `Extra`

---

## Pendências / Melhorias possíveis

1. TPM no Windows exige `swtpm` (socket Unix); tratar backend nativo se alguém precisar
2. Validar `qemu-manager-linux.sh` num host Linux

---

## Observações importantes

- Usuário exige respostas em **português do Brasil**
- Diretório de trabalho fixo: `G:\vm\`
- VirtIO ISO esperado em: `G:\vm\virtio-win.iso`
- QEMU deve estar em `C:\Program Files\qemu\`
- Aceleração no Windows: WHPX (`-accel whpx`)

---

## Histórico resumido

1. Começou com scripts PowerShell
2. Evoluiu para GUI Python (Tkinter) com muitos recursos
3. Convertido para C# WinForms
4. Vários ajustes de layout e tooltips
5. Usuário quer arquivos prontos em `G:\vm\`

---

## Como continuar

Estado vivo do projeto: `docs/STATUS.md` (leia antes de `AGENTS.md` e `docs/ARCHITECTURE.md`).

Qualquer agente pode:
1. Ler `AGENTS.md` + `docs/STATUS.md`
2. Abrir `QemuManager.cs` ou `qemu-manager-definitivo.py`
3. Adicionar funcionalidades faltantes
4. Gerar o `.exe` com `programa/compilar-qemu-manager.ps1`

**Última versão estável:** `QemuManager.cs` (tooltips + ícones embutidos)  
**Versão mais completa:** `qemu-manager-definitivo.py`
