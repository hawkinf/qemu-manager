#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
QEMU Manager DEFINITIVO - Todas as opções + Tooltips completos
"""

import os
import json
import subprocess
import time
import tkinter as tk
from tkinter import ttk, filedialog, messagebox, scrolledtext
import tkinter.simpledialog

QEMU_DIR = r"C:\Program Files\qemu"
QEMU_IMG = os.path.join(QEMU_DIR, "qemu-img.exe")
CONFIG_DIR = os.path.join(os.path.expanduser("~"), "QEMU_Manager_Configs")
os.makedirs(CONFIG_DIR, exist_ok=True)

ARCHITECTURES = {
    "x86_64 (64-bit)": "qemu-system-x86_64.exe",
    "i386 (32-bit)": "qemu-system-i386.exe",
    "aarch64 (ARM 64-bit)": "qemu-system-aarch64.exe",
    "arm (ARM 32-bit)": "qemu-system-arm.exe",
    "riscv64": "qemu-system-riscv64.exe",
    "riscv32": "qemu-system-riscv32.exe",
    "ppc64 (PowerPC 64)": "qemu-system-ppc64.exe",
    "ppc (PowerPC 32)": "qemu-system-ppc.exe",
    "mips64el": "qemu-system-mips64el.exe",
    "mipsel": "qemu-system-mipsel.exe",
    "sparc64": "qemu-system-sparc64.exe",
    "sparc": "qemu-system-sparc.exe",
    "s390x": "qemu-system-s390x.exe",
    "Personalizado": "CUSTOM"
}

MACHINE_SUGGESTIONS = {
    "x86_64 (64-bit)": ["q35", "pc", "virt"],
    "i386 (32-bit)": ["pc", "q35"],
    "aarch64 (ARM 64-bit)": ["virt", "virt-2.12", "raspi3b", "raspi4b"],
    "arm (ARM 32-bit)": ["virt", "integratorcp", "realview-pb-a8"],
    "riscv64": ["virt", "sifive_u"],
    "riscv32": ["virt", "sifive_u"],
    "ppc64 (PowerPC 64)": ["pseries", "powernv", "mac99"],
    "ppc (PowerPC 32)": ["mac99", "g3beige"],
    "mips64el": ["malta", "virt"],
    "mipsel": ["malta"],
    "sparc64": ["sun4u"],
    "sparc": ["SS-5", "SS-10"],
    "s390x": ["s390-ccw-virtio"],
}

class ToolTip:
    def __init__(self, widget, text):
        self.widget = widget
        self.text = text
        self.tipwindow = None
        self.widget.bind("<Enter>", self.show)
        self.widget.bind("<Leave>", self.hide)
        self.widget.bind("<ButtonPress>", self.hide)

    def show(self, event=None):
        if self.tipwindow or not self.text:
            return
        x = self.widget.winfo_rootx() + 20
        y = self.widget.winfo_rooty() + self.widget.winfo_height() + 4
        self.tipwindow = tw = tk.Toplevel(self.widget)
        tw.wm_overrideredirect(True)
        tw.wm_geometry(f"+{x}+{y}")
        label = tk.Label(tw, text=self.text, justify="left", background="#ffffe0",
                         relief="solid", borderwidth=1, font=("Segoe UI", 9), wraplength=400)
        label.pack(ipadx=6, ipady=4)

    def hide(self, event=None):
        if self.tipwindow:
            self.tipwindow.destroy()
            self.tipwindow = None

PRESETS = {
    "Windows 11 x64": {
        "arch": "x86_64 (64-bit)", "disk": "win11.qcow2", "size": "80G", "ram": "8G", "cpus": "4",
        "cpu_model": "host", "machine": "q35", "disk_if": "virtio", "nic": "virtio-net-pci",
        "boot": "menu", "vga": "std", "usb": True, "rng": True, "gl": False
    },
    "Windows 10 x64": {
        "arch": "x86_64 (64-bit)", "disk": "win10.qcow2", "size": "80G", "ram": "8G", "cpus": "4",
        "cpu_model": "host", "machine": "q35", "disk_if": "virtio", "nic": "virtio-net-pci",
        "boot": "menu", "vga": "std", "usb": True, "rng": True, "gl": False
    },
    "Windows 10 x86": {
        "arch": "i386 (32-bit)", "disk": "win10x86.qcow2", "size": "60G", "ram": "4G", "cpus": "2",
        "cpu_model": "qemu32", "machine": "pc", "disk_if": "ide", "nic": "e1000",
        "boot": "menu", "vga": "std", "usb": True, "rng": False
    },
    "Windows 7 x64": {
        "arch": "x86_64 (64-bit)", "disk": "win7.qcow2", "size": "60G", "ram": "4G", "cpus": "2",
        "cpu_model": "qemu64", "machine": "pc", "disk_if": "ide", "nic": "e1000",
        "boot": "menu", "vga": "std", "usb": True
    },
    "Windows XP": {
        "arch": "i386 (32-bit)", "disk": "winxp.qcow2", "size": "40G", "ram": "2G", "cpus": "1",
        "cpu_model": "qemu32", "machine": "pc", "disk_if": "ide", "nic": "rtl8139",
        "boot": "menu", "vga": "cirrus", "usb": False
    },
    "Ubuntu 24.04 x64": {
        "arch": "x86_64 (64-bit)", "disk": "ubuntu.qcow2", "size": "40G", "ram": "4G", "cpus": "4",
        "cpu_model": "host", "machine": "q35", "disk_if": "virtio", "nic": "virtio-net-pci",
        "boot": "menu", "vga": "virtio", "gl": True, "usb": True, "rng": True
    },
    "Ubuntu ARM64": {
        "arch": "aarch64 (ARM 64-bit)", "disk": "ubuntu-arm.qcow2", "size": "40G", "ram": "4G", "cpus": "4",
        "cpu_model": "cortex-a72", "machine": "virt", "disk_if": "virtio", "nic": "virtio-net-pci",
        "boot": "menu", "vga": "virtio", "usb": True, "rng": True
    },
    "Debian x64": {
        "arch": "x86_64 (64-bit)", "disk": "debian.qcow2", "size": "30G", "ram": "2G", "cpus": "2",
        "cpu_model": "host", "machine": "q35", "disk_if": "virtio", "nic": "virtio-net-pci",
        "boot": "menu", "vga": "virtio", "gl": True, "usb": True, "rng": True
    },
    "RISC-V 64": {
        "arch": "riscv64", "disk": "riscv.qcow2", "size": "20G", "ram": "2G", "cpus": "2",
        "cpu_model": "rv64", "machine": "virt", "disk_if": "virtio", "nic": "virtio-net-pci",
        "boot": "menu", "vga": "virtio", "usb": True
    },
    "Alpine / Mínimo": {
        "arch": "x86_64 (64-bit)", "disk": "alpine.qcow2", "size": "10G", "ram": "1G", "cpus": "1",
        "cpu_model": "host", "machine": "q35", "disk_if": "virtio", "nic": "virtio-net-pci",
        "boot": "menu", "vga": "virtio", "usb": True
    },
    "Windows 98 / DOS": {
        "arch": "i386 (32-bit)", "disk": "win98.qcow2", "size": "8G", "ram": "512M", "cpus": "1",
        "cpu_model": "qemu32", "machine": "pc", "disk_if": "ide", "nic": "rtl8139",
        "boot": "menu", "vga": "cirrus", "usb": False, "sound": "sb16"
    },
    "macOS (OpenCore x64)": {
        "arch": "x86_64 (64-bit)", "disk": "macos.qcow2", "size": "80G", "ram": "8G", "cpus": "4",
        "cpu_model": "Penryn", "machine": "q35", "disk_if": "virtio", "nic": "virtio-net-pci",
        "boot": "menu", "vga": "virtio", "usb": True, "rng": True,
        "extra": '-device isa-applesmc,osk="ourhardworkbythesewordsguardedpleasedontsteal(c)AppleComputerInc" -smbios type=2'
    },
    "Teste PXE (rede interna)": {
        "arch": "x86_64 (64-bit)", "disk": "pxe-test.qcow2", "size": "2G", "ram": "2G", "cpus": "2",
        "cpu_model": "qemu64", "machine": "q35", "disk_if": "none", "nic": "e1000",
        "boot": "n", "boot_menu": True, "vga": "std", "usb": True, "rng": False,
        "virtio": "", "iso": "", "net_mode": "tap",
        "tap_if": "OpenVPN TAP-Windows6", "mac": "52:54:00:12:34:56"
    },
}

class QemuManager:
    def __init__(self, root):
        self.root = root
        self.root.title("QEMU Manager DEFINITIVO - Tooltips Completos")
        self.root.geometry("1000x880")
        self.root.minsize(960, 820)

        self.arch_var = tk.StringVar(value="x86_64 (64-bit)")
        self.custom_qemu_var = tk.StringVar()
        self.vm_configs = {}
        self.create_ui()
        self.refresh_vm_list()
        self.load_preset("Windows 10 x64")

    def create_ui(self):
        notebook = ttk.Notebook(self.root)
        notebook.pack(fill="both", expand=True, padx=8, pady=8)

        # ==================== VMs ====================
        tab_vms = ttk.Frame(notebook, padding=8)
        notebook.add(tab_vms, text="VMs")

        f_list = ttk.LabelFrame(tab_vms, text="Máquinas salvas (duplo clique inicia)", padding=8)
        f_list.pack(fill="both", expand=True)
        self.vm_listbox = tk.Listbox(f_list, height=20, font=("Segoe UI", 10))
        self.vm_listbox.pack(side="left", fill="both", expand=True)
        ttk.Scrollbar(f_list, orient="vertical", command=self.vm_listbox.yview).pack(side="right", fill="y")
        self.vm_listbox.config(yscrollcommand=lambda *a: None)
        self.vm_listbox.bind("<Double-Button-1>", lambda e: self.start_selected_vm())
        ToolTip(self.vm_listbox, "Lista de VMs salvas.\n✓ = disco existe | ✗ = disco não encontrado\nDuplo clique inicia a VM.")

        bf = ttk.Frame(tab_vms)
        bf.pack(fill="x", pady=6)
        for t, c, tip in [("Atualizar", self.refresh_vm_list, "Recarrega a lista de VMs"),
                          ("Iniciar", self.start_selected_vm, "Inicia a VM selecionada"),
                          ("Editar", self.edit_selected_vm, "Carrega a configuração para edição"),
                          ("Excluir", self.delete_selected_vm, "Remove apenas o arquivo de configuração")]:
            b = ttk.Button(bf, text=t, command=c)
            b.pack(side="left", padx=4)
            ToolTip(b, tip)

        # ==================== PRINCIPAL ====================
        tab_main = ttk.Frame(notebook, padding=8)
        notebook.add(tab_main, text="Principal")

        f_preset = ttk.LabelFrame(tab_main, text="Perfis", padding=6)
        f_preset.pack(fill="x", pady=3)
        ttk.Label(f_preset, text="Perfil:").pack(side="left")
        self.preset_combo = ttk.Combobox(f_preset, values=list(PRESETS.keys()), width=40, state="readonly")
        self.preset_combo.pack(side="left", padx=4)
        self.preset_combo.set("Windows 10 x64")
        ToolTip(self.preset_combo, "Perfis prontos com configurações recomendadas para cada sistema.")
        b1 = ttk.Button(f_preset, text="Carregar", command=self.load_selected_preset)
        b1.pack(side="left", padx=4)
        ToolTip(b1, "Aplica o perfil selecionado nos campos.")
        b2 = ttk.Button(f_preset, text="Salvar como...", command=self.save_current_as_preset)
        b2.pack(side="left", padx=4)
        ToolTip(b2, "Salva a configuração atual com um nome personalizado.")

        f_arch = ttk.LabelFrame(tab_main, text="Arquitetura", padding=6)
        f_arch.pack(fill="x", pady=3)
        ttk.Label(f_arch, text="Arquitetura:").grid(row=0, column=0, sticky="w")
        self.arch_combo = ttk.Combobox(f_arch, textvariable=self.arch_var, values=list(ARCHITECTURES.keys()), width=30, state="readonly")
        self.arch_combo.grid(row=0, column=1, padx=4)
        self.arch_combo.bind("<<ComboboxSelected>>", lambda e: self.on_arch_change())
        ToolTip(self.arch_combo, "Arquitetura da máquina virtual.\nCada uma usa um binário qemu-system-* diferente.")
        ttk.Label(f_arch, text="QEMU custom:").grid(row=1, column=0, sticky="w", pady=3)
        self.custom_entry = ttk.Entry(f_arch, textvariable=self.custom_qemu_var, width=50)
        self.custom_entry.grid(row=1, column=1, padx=4)
        ToolTip(self.custom_entry, "Caminho completo do executável QEMU (usado apenas se escolher Personalizado).")
        bq = ttk.Button(f_arch, text="...", width=3, command=self.browse_qemu)
        bq.grid(row=1, column=2)
        ToolTip(bq, "Procurar o arquivo qemu-system-*.exe")

        f_disk = ttk.LabelFrame(tab_main, text="Disco Principal", padding=6)
        f_disk.pack(fill="x", pady=3)
        ttk.Label(f_disk, text="Arquivo:").grid(row=0, column=0, sticky="w")
        self.disk_var = tk.StringVar()
        e_disk = ttk.Entry(f_disk, textvariable=self.disk_var, width=68)
        e_disk.grid(row=0, column=1, padx=4)
        ToolTip(e_disk, "Arquivo do disco virtual (.qcow2). Será criado automaticamente se não existir.")
        bd = ttk.Button(f_disk, text="...", width=3, command=self.browse_disk)
        bd.grid(row=0, column=2)
        ToolTip(bd, "Escolher ou criar o arquivo de disco.")
        ttk.Label(f_disk, text="Tamanho:").grid(row=1, column=0, sticky="w", pady=3)
        self.size_var = tk.StringVar()
        e_size = ttk.Entry(f_disk, textvariable=self.size_var, width=14)
        e_size.grid(row=1, column=1, sticky="w", padx=4)
        ToolTip(e_size, "Tamanho do disco ao criar (ex: 40G, 80G, 512M).")

        f_iso = ttk.LabelFrame(tab_main, text="ISO", padding=6)
        f_iso.pack(fill="x", pady=3)
        ttk.Label(f_iso, text="ISO:").grid(row=0, column=0, sticky="w")
        self.iso_var = tk.StringVar()
        e_iso = ttk.Entry(f_iso, textvariable=self.iso_var, width=68)
        e_iso.grid(row=0, column=1, padx=4)
        ToolTip(e_iso, "Arquivo ISO de instalação (opcional se o sistema já estiver instalado).")
        bi = ttk.Button(f_iso, text="...", width=3, command=self.browse_iso)
        bi.grid(row=0, column=2)
        ToolTip(bi, "Selecionar arquivo ISO.")

        self.f_virtio = ttk.LabelFrame(tab_main, text="VirtIO ISO", padding=6)
        self.f_virtio.pack(fill="x", pady=3)
        ttk.Label(self.f_virtio, text="VirtIO:").grid(row=0, column=0, sticky="w")
        self.virtio_var = tk.StringVar(value=os.path.join(os.getcwd(), "virtio-win.iso"))
        e_virt = ttk.Entry(self.f_virtio, textvariable=self.virtio_var, width=68)
        e_virt.grid(row=0, column=1, padx=4)
        ToolTip(e_virt, "ISO dos drivers VirtIO (recomendado para Windows para melhor desempenho).")
        bv = ttk.Button(self.f_virtio, text="...", width=3, command=self.browse_virtio)
        bv.grid(row=0, column=2)
        ToolTip(bv, "Selecionar ISO VirtIO.")

        f_boot = ttk.LabelFrame(tab_main, text="Boot", padding=6)
        f_boot.pack(fill="x", pady=3)
        self.boot_var = tk.StringVar(value="menu")
        boot_tips = {
            "menu": "Mostra o menu interativo de boot do QEMU.",
            "d": "Boota preferencialmente do CD/ISO (order=d).",
            "c": "Boota preferencialmente do disco rígido (order=c).",
            "n": "Boot PXE (order=n). NAT (user) NÃO vê o PXE da LAN.\nUse o perfil 'Teste PXE (rede interna)' com modo tap.",
            "a": "Boota de disquete (order=a).",
            "dc": "Tenta CD e depois disco (order=dc).",
            "nc": "Tenta rede/PXE e depois disco (order=nc).",
            "once_n": "Boota da rede apenas nesta inicialização (once=n).",
            "once_d": "Boota do CD apenas nesta inicialização (once=d).",
            "custom": "Usa a ordem personalizada do campo abaixo."
        }
        opts = [("Menu", "menu"), ("CD", "d"), ("Disco", "c"), ("Rede/PXE", "n"),
                ("Floppy", "a"), ("CD→Disco", "dc"), ("Rede→Disco", "nc"),
                ("Once Rede", "once_n"), ("Once CD", "once_d"), ("Custom", "custom")]
        for i, (t, v) in enumerate(opts):
            rb = ttk.Radiobutton(f_boot, text=t, variable=self.boot_var, value=v)
            rb.grid(row=i//5, column=i%5, sticky="w", padx=4)
            ToolTip(rb, boot_tips.get(v, ""))

        ttk.Label(f_boot, text="Ordem custom:").grid(row=2, column=0, sticky="w", pady=3)
        self.boot_custom_var = tk.StringVar(value="ncd")
        e_bc = ttk.Entry(f_boot, textvariable=self.boot_custom_var, width=16)
        e_bc.grid(row=2, column=1, sticky="w")
        ToolTip(e_bc, "Letras: a=floppy, c=disco, d=CD, n=rede/PXE\nExemplo: ncd = rede, CD, disco.")
        self.boot_menu_var = tk.BooleanVar(value=True)
        cb_menu = ttk.Checkbutton(f_boot, text="menu=on", variable=self.boot_menu_var)
        cb_menu.grid(row=2, column=2, sticky="w")
        ToolTip(cb_menu, "Força a exibição do menu de boot mesmo com ordem definida.")

        # ==================== HARDWARE ====================
        tab_hw = ttk.Frame(notebook, padding=8)
        notebook.add(tab_hw, text="Hardware")

        f_cpu = ttk.LabelFrame(tab_hw, text="CPU / Memória", padding=6)
        f_cpu.pack(fill="x", pady=3)
        ttk.Label(f_cpu, text="RAM:").grid(row=0, column=0, sticky="w")
        self.ram_var = tk.StringVar()
        cb_ram = ttk.Combobox(f_cpu, textvariable=self.ram_var, values=["256M","512M","1G","2G","4G","6G","8G","12G","16G","32G","64G","128G"], width=10)
        cb_ram.grid(row=0, column=1, padx=4)
        ToolTip(cb_ram, "Quantidade de memória RAM da VM.")
        ttk.Label(f_cpu, text="CPUs:").grid(row=0, column=2, sticky="w", padx=10)
        self.cpus_var = tk.StringVar()
        sp_cpu = ttk.Spinbox(f_cpu, from_=1, to=128, textvariable=self.cpus_var, width=6)
        sp_cpu.grid(row=0, column=3)
        ToolTip(sp_cpu, "Número de núcleos de CPU virtuais.")
        ttk.Label(f_cpu, text="Modelo:").grid(row=1, column=0, sticky="w", pady=4)
        self.cpu_model_var = tk.StringVar()
        cb_cpu = ttk.Combobox(f_cpu, textvariable=self.cpu_model_var, values=["host","qemu64","qemu32","max","cortex-a72","rv64","Skylake-Client","Haswell","Westmere","Nehalem"], width=18)
        cb_cpu.grid(row=1, column=1, columnspan=2, sticky="w", padx=4)
        ToolTip(cb_cpu, "Modelo de CPU apresentado ao guest. 'host' é o mais rápido.")
        ttk.Label(f_cpu, text="Máquina:").grid(row=2, column=0, sticky="w")
        self.machine_var = tk.StringVar()
        self.machine_combo = ttk.Combobox(f_cpu, textvariable=self.machine_var, width=18)
        self.machine_combo.grid(row=2, column=1, sticky="w", padx=4)
        ToolTip(self.machine_combo, "Tipo de máquina. As sugestões mudam conforme a arquitetura.")

        f_dev = ttk.LabelFrame(tab_hw, text="Dispositivos", padding=6)
        f_dev.pack(fill="x", pady=4)
        ttk.Label(f_dev, text="Disco:").grid(row=0, column=0, sticky="w")
        self.diskif_var = tk.StringVar()
        cb_dif = ttk.Combobox(f_dev, textvariable=self.diskif_var, values=["virtio","ide","sata","scsi","none"], width=12)
        cb_dif.grid(row=0, column=1, sticky="w", padx=4)
        ToolTip(cb_dif, "Interface do disco. virtio é a mais rápida (precisa de drivers no Windows).")
        ttk.Label(f_dev, text="Rede:").grid(row=0, column=2, sticky="w", padx=10)
        self.nic_var = tk.StringVar()
        cb_nic = ttk.Combobox(f_dev, textvariable=self.nic_var, values=["virtio-net-pci","e1000","e1000e","rtl8139","vmxnet3","none"], width=16)
        cb_nic.grid(row=0, column=3)
        ToolTip(cb_nic, "Modelo da placa de rede. virtio-net-pci é a mais rápida.")

        self.gl_var = tk.BooleanVar()
        cb_gl = ttk.Checkbutton(f_dev, text="OpenGL", variable=self.gl_var)
        cb_gl.grid(row=1, column=0, sticky="w", pady=4)
        ToolTip(cb_gl, "Ativa aceleração OpenGL no display (recomendado para Linux).")
        self.usb_var = tk.BooleanVar(value=True)
        cb_usb = ttk.Checkbutton(f_dev, text="USB Mouse", variable=self.usb_var)
        cb_usb.grid(row=1, column=1, sticky="w")
        ToolTip(cb_usb, "Adiciona mouse e teclado USB (melhor integração do cursor).")

        ttk.Label(f_dev, text="Display:").grid(row=2, column=0, sticky="w")
        self.display_var = tk.StringVar(value="gtk")
        cb_disp = ttk.Combobox(f_dev, textvariable=self.display_var, values=["gtk","sdl","vnc","curses","none","egl-headless"], width=14)
        cb_disp.grid(row=2, column=1, sticky="w", padx=4)
        ToolTip(cb_disp, "Tipo de saída de vídeo. gtk = janela gráfica padrão.")
        ttk.Label(f_dev, text="Som:").grid(row=2, column=2, sticky="w", padx=10)
        self.sound_var = tk.StringVar(value="none")
        cb_snd = ttk.Combobox(f_dev, textvariable=self.sound_var, values=["none","hda","ac97","es1370","sb16","pcspk"], width=12)
        cb_snd.grid(row=2, column=3)
        ToolTip(cb_snd, "Placa de som. hda = Intel HD Audio (recomendado).")

        ttk.Label(f_dev, text="VGA:").grid(row=3, column=0, sticky="w", pady=4)
        self.vga_var = tk.StringVar(value="std")
        cb_vga = ttk.Combobox(f_dev, textvariable=self.vga_var, values=["std","virtio","qxl","cirrus","vmware","bochs","none"], width=12)
        cb_vga.grid(row=3, column=1, sticky="w", padx=4)
        ToolTip(cb_vga, "Modelo de placa de vídeo. virtio ou qxl são melhores para Linux.")

        # ==================== EXTRA ====================
        tab_extra = ttk.Frame(notebook, padding=8)
        notebook.add(tab_extra, text="Extra")

        f_bios = ttk.LabelFrame(tab_extra, text="Firmware / UEFI", padding=6)
        f_bios.pack(fill="x", pady=3)
        ttk.Label(f_bios, text="BIOS/OVMF:").grid(row=0, column=0, sticky="w")
        self.bios_var = tk.StringVar()
        e_bios = ttk.Entry(f_bios, textvariable=self.bios_var, width=55)
        e_bios.grid(row=0, column=1, padx=4)
        ToolTip(e_bios, "Arquivo de firmware (ex: OVMF_CODE.fd para UEFI ou SeaBIOS custom).")
        b_bios = ttk.Button(f_bios, text="...", width=3, command=lambda: self.browse_file(self.bios_var, "BIOS/OVMF"))
        b_bios.grid(row=0, column=2)
        ToolTip(b_bios, "Selecionar arquivo de BIOS/OVMF.")
        ttk.Label(f_bios, text="pflash (vars):").grid(row=1, column=0, sticky="w", pady=3)
        self.pflash_var = tk.StringVar()
        e_pf = ttk.Entry(f_bios, textvariable=self.pflash_var, width=55)
        e_pf.grid(row=1, column=1, padx=4)
        ToolTip(e_pf, "Arquivo de variáveis UEFI (OVMF_VARS.fd). Usado junto com OVMF.")
        b_pf = ttk.Button(f_bios, text="...", width=3, command=lambda: self.browse_file(self.pflash_var, "UEFI vars"))
        b_pf.grid(row=1, column=2)
        ToolTip(b_pf, "Selecionar arquivo de variáveis UEFI (pflash).")

        f_share = ttk.LabelFrame(tab_extra, text="Pasta Compartilhada (9p)", padding=6)
        f_share.pack(fill="x", pady=3)
        ttk.Label(f_share, text="Pasta host:").grid(row=0, column=0, sticky="w")
        self.share_host_var = tk.StringVar()
        e_sh = ttk.Entry(f_share, textvariable=self.share_host_var, width=48)
        e_sh.grid(row=0, column=1, padx=4)
        ToolTip(e_sh, "Pasta do Windows que será compartilhada com a VM via 9p.")
        b_sh = ttk.Button(f_share, text="...", width=3, command=lambda: self.browse_dir(self.share_host_var))
        b_sh.grid(row=0, column=2)
        ToolTip(b_sh, "Selecionar pasta do host para compartilhar.")
        ttk.Label(f_share, text="Tag na VM:").grid(row=1, column=0, sticky="w", pady=3)
        self.share_name_var = tk.StringVar(value="hostshare")
        e_sn = ttk.Entry(f_share, textvariable=self.share_name_var, width=20)
        e_sn.grid(row=1, column=1, sticky="w", padx=4)
        ToolTip(e_sn, "Nome da pasta dentro da VM (mount_tag).")

        f_edisk = ttk.LabelFrame(tab_extra, text="Disco Extra", padding=6)
        f_edisk.pack(fill="x", pady=3)
        ttk.Label(f_edisk, text="Arquivo:").grid(row=0, column=0, sticky="w")
        self.extra_disk_var = tk.StringVar()
        e_ed = ttk.Entry(f_edisk, textvariable=self.extra_disk_var, width=55)
        e_ed.grid(row=0, column=1, padx=4)
        ToolTip(e_ed, "Segundo disco virtual (opcional).")
        b_ed = ttk.Button(f_edisk, text="...", width=3, command=lambda: self.browse_file(self.extra_disk_var, "Disco extra", [("QCOW2","*.qcow2")]))
        b_ed.grid(row=0, column=2)
        ToolTip(b_ed, "Selecionar disco extra.")
        ttk.Label(f_edisk, text="Interface:").grid(row=1, column=0, sticky="w", pady=3)
        self.extra_diskif_var = tk.StringVar(value="virtio")
        cb_edif = ttk.Combobox(f_edisk, textvariable=self.extra_diskif_var, values=["virtio","ide","sata"], width=12)
        cb_edif.grid(row=1, column=1, sticky="w", padx=4)
        ToolTip(cb_edif, "Interface do disco extra.")

        f_snap = ttk.LabelFrame(tab_extra, text="Snapshot / Kernel direto", padding=6)
        f_snap.pack(fill="x", pady=3)
        ttk.Label(f_snap, text="Snapshot:").grid(row=0, column=0, sticky="w")
        self.snapshot_var = tk.StringVar()
        e_snap = ttk.Entry(f_snap, textvariable=self.snapshot_var, width=28)
        e_snap.grid(row=0, column=1, sticky="w", padx=4)
        ToolTip(e_snap, "Nome do snapshot para carregar ao iniciar (-loadvm).")
        ttk.Label(f_snap, text="Kernel:").grid(row=1, column=0, sticky="w", pady=3)
        self.kernel_var = tk.StringVar()
        e_k = ttk.Entry(f_snap, textvariable=self.kernel_var, width=50)
        e_k.grid(row=1, column=1, padx=4)
        ToolTip(e_k, "Arquivo de kernel para boot direto (-kernel).")
        b_k = ttk.Button(f_snap, text="...", width=3, command=lambda: self.browse_file(self.kernel_var, "Kernel"))
        b_k.grid(row=1, column=2)
        ToolTip(b_k, "Selecionar arquivo de kernel.")
        ttk.Label(f_snap, text="Initrd:").grid(row=2, column=0, sticky="w")
        self.initrd_var = tk.StringVar()
        e_i = ttk.Entry(f_snap, textvariable=self.initrd_var, width=50)
        e_i.grid(row=2, column=1, padx=4)
        ToolTip(e_i, "Arquivo initrd/initramfs (-initrd).")
        b_i = ttk.Button(f_snap, text="...", width=3, command=lambda: self.browse_file(self.initrd_var, "Initrd"))
        b_i.grid(row=2, column=2)
        ToolTip(b_i, "Selecionar arquivo initrd.")
        ttk.Label(f_snap, text="Append:").grid(row=3, column=0, sticky="w", pady=3)
        self.append_var = tk.StringVar()
        e_a = ttk.Entry(f_snap, textvariable=self.append_var, width=50)
        e_a.grid(row=3, column=1, columnspan=2, sticky="w", padx=4)
        ToolTip(e_a, "Linha de comando do kernel (-append). Ex: root=/dev/sda1")

        f_misc = ttk.LabelFrame(tab_extra, text="Diversos", padding=6)
        f_misc.pack(fill="x", pady=3)
        self.rng_var = tk.BooleanVar(value=True)
        cb_rng = ttk.Checkbutton(f_misc, text="VirtIO RNG", variable=self.rng_var)
        cb_rng.grid(row=0, column=0, sticky="w")
        ToolTip(cb_rng, "Adiciona dispositivo VirtIO RNG (melhor entropia no guest).")
        self.balloon_var = tk.BooleanVar()
        cb_bal = ttk.Checkbutton(f_misc, text="VirtIO Balloon", variable=self.balloon_var)
        cb_bal.grid(row=0, column=1, sticky="w", padx=10)
        ToolTip(cb_bal, "Permite ajustar a memória dinamicamente (virtio-balloon).")
        self.tpm_var = tk.BooleanVar()
        cb_tpm = ttk.Checkbutton(f_misc, text="TPM (emulator)", variable=self.tpm_var)
        cb_tpm.grid(row=0, column=2, sticky="w")
        ToolTip(cb_tpm, "Adiciona TPM emulado (requer swtpm rodando).")
        ttk.Label(f_misc, text="Teclado:").grid(row=1, column=0, sticky="w", pady=4)
        self.keyboard_var = tk.StringVar(value="en-us")
        cb_kb = ttk.Combobox(f_misc, textvariable=self.keyboard_var, values=["en-us","pt-br","es","fr","de","it","ja","ru"], width=12)
        cb_kb.grid(row=1, column=1, sticky="w")
        ToolTip(cb_kb, "Layout de teclado do guest.")

        f_net = ttk.LabelFrame(tab_extra, text="Rede", padding=6)
        f_net.pack(fill="x", pady=3)
        ttk.Label(f_net, text="Modo:").grid(row=0, column=0, sticky="w")
        self.net_mode_var = tk.StringVar(value="user")
        cb_net = ttk.Combobox(f_net, textvariable=self.net_mode_var, values=["user", "tap", "bridge", "none"], width=10)
        cb_net.grid(row=0, column=1, sticky="w", padx=4)
        ToolTip(cb_net, "user = NAT (não vê PXE da LAN).\ntap = adaptador TAP na rede interna (PXE real).\nbridge = ponte Linux (br0).\nnone = sem rede.")
        ttk.Label(f_net, text="TAP/bridge:").grid(row=0, column=2, sticky="w", padx=8)
        self.tap_if_var = tk.StringVar(value="OpenVPN TAP-Windows6")
        e_tap = ttk.Entry(f_net, textvariable=self.tap_if_var, width=28)
        e_tap.grid(row=0, column=3, sticky="w")
        ToolTip(e_tap, "Windows: nome do TAP (ex: OpenVPN TAP-Windows6).\nFaça a PONTE do TAP com a placa da LAN (cabo) em ncpa.cpl.\nLinux tap: tap0  |  Linux bridge: br0")
        ttk.Label(f_net, text="MAC:").grid(row=1, column=0, sticky="w", pady=3)
        self.mac_var = tk.StringVar()
        e_mac = ttk.Entry(f_net, textvariable=self.mac_var, width=20)
        e_mac.grid(row=1, column=1, sticky="w", padx=4)
        ToolTip(e_mac, "MAC da NIC virtual. Alguns PXE filtram por MAC.\nEx: 52:54:00:12:34:56")
        ttk.Label(f_net, text="Hostfwd:").grid(row=2, column=0, sticky="w", pady=3)
        self.hostfwd_var = tk.StringVar()
        e_fwd = ttk.Entry(f_net, textvariable=self.hostfwd_var, width=50)
        e_fwd.grid(row=2, column=1, columnspan=3, sticky="w", padx=4)
        ToolTip(e_fwd, "Só no modo user. Ex: tcp::2222-:22")
        ttk.Label(f_net, text="TFTP local:").grid(row=3, column=0, sticky="w", pady=3)
        self.tftp_var = tk.StringVar()
        e_tftp = ttk.Entry(f_net, textvariable=self.tftp_var, width=40)
        e_tftp.grid(row=3, column=1, columnspan=2, sticky="w", padx=4)
        ToolTip(e_tftp, "Pasta TFTP só no modo user (PXE emulado, não é a LAN).")
        ttk.Label(f_net, text="Bootfile:").grid(row=4, column=0, sticky="w")
        self.bootfile_var = tk.StringVar()
        e_bf = ttk.Entry(f_net, textvariable=self.bootfile_var, width=30)
        e_bf.grid(row=4, column=1, sticky="w", padx=4)
        ToolTip(e_bf, "Arquivo PXE no TFTP local. Ex: pxelinux.0, undionly.kpxe")

        f_extra = ttk.LabelFrame(tab_extra, text="Argumentos extras (qualquer coisa do QEMU)", padding=6)
        f_extra.pack(fill="x", pady=3)
        self.extra_args = scrolledtext.ScrolledText(f_extra, height=6, width=82)
        self.extra_args.pack(fill="x")
        ToolTip(self.extra_args, "Argumentos extras livres do QEMU.\nExemplos: -vga virtio -device virtio-rng-pci\n-bios OVMF.fd -smp 4,sockets=1,cores=4")

        f_save = ttk.LabelFrame(tab_extra, text="Configurações", padding=6)
        f_save.pack(fill="x", pady=4)
        bs = ttk.Button(f_save, text="Salvar", command=self.save_config)
        bs.pack(side="left", padx=4)
        ToolTip(bs, "Salva a configuração atual em arquivo .json.")
        bl = ttk.Button(f_save, text="Carregar", command=self.load_config)
        bl.pack(side="left", padx=4)
        ToolTip(bl, "Carrega uma configuração salva.")
        bo = ttk.Button(f_save, text="Abrir Pasta", command=self.open_config_folder)
        bo.pack(side="left", padx=4)
        ToolTip(bo, "Abre a pasta onde ficam as configurações.")

        btn_start = ttk.Button(self.root, text="▶ INICIAR MÁQUINA VIRTUAL", command=self.start_vm, width=40)
        btn_start.pack(pady=6)
        ToolTip(btn_start, "Inicia a máquina virtual com as configurações atuais.")
        self.status = ttk.Label(self.root, text="Pronto", relief="sunken", anchor="w")
        self.status.pack(side="bottom", fill="x")

        self.on_arch_change()

    # O restante dos métodos permanece o mesmo (copiado da versão anterior)
    def on_arch_change(self):
        arch = self.arch_var.get()
        sugg = MACHINE_SUGGESTIONS.get(arch, ["virt"])
        self.machine_combo["values"] = sugg
        if sugg and not self.machine_var.get():
            self.machine_var.set(sugg[0])
        if arch == "Personalizado":
            self.custom_entry.state(["!disabled"])
        else:
            self.custom_entry.state(["disabled"])

    def get_qemu_binary(self):
        arch = self.arch_var.get()
        if arch == "Personalizado":
            return self.custom_qemu_var.get().strip()
        return os.path.join(QEMU_DIR, ARCHITECTURES.get(arch, "qemu-system-x86_64.exe"))

    def refresh_vm_list(self):
        self.vm_listbox.delete(0, tk.END)
        self.vm_configs.clear()
        if not os.path.exists(CONFIG_DIR):
            return
        for f in sorted(os.listdir(CONFIG_DIR)):
            if f.endswith(".json"):
                try:
                    with open(os.path.join(CONFIG_DIR, f), "r", encoding="utf-8") as fp:
                        cfg = json.load(fp)
                    name = f[:-5]
                    mark = "✓" if os.path.exists(cfg.get("disk", "")) else "✗"
                    self.vm_listbox.insert(tk.END, f"{mark}  {name}")
                    self.vm_configs[name] = cfg
                except:
                    pass
        self.status.config(text=f"{len(self.vm_configs)} VM(s)")

    def get_selected_vm_name(self):
        sel = self.vm_listbox.curselection()
        if not sel:
            messagebox.showwarning("Aviso", "Selecione uma VM")
            return None
        return self.vm_listbox.get(sel[0])[3:].strip()

    def start_selected_vm(self):
        name = self.get_selected_vm_name()
        if name and name in self.vm_configs:
            self.apply_config(self.vm_configs[name])
            self.start_vm()

    def edit_selected_vm(self):
        name = self.get_selected_vm_name()
        if name and name in self.vm_configs:
            self.apply_config(self.vm_configs[name])
            self.root.winfo_children()[0].select(1)

    def delete_selected_vm(self):
        name = self.get_selected_vm_name()
        if name and messagebox.askyesno("Confirmar", f"Excluir '{name}'?"):
            p = os.path.join(CONFIG_DIR, f"{name}.json")
            if os.path.exists(p):
                os.remove(p)
            self.refresh_vm_list()

    def get_current_config(self):
        return {
            "arch": self.arch_var.get(), "custom_qemu": self.custom_qemu_var.get(),
            "disk": self.disk_var.get(), "size": self.size_var.get(),
            "iso": self.iso_var.get(), "virtio": self.virtio_var.get(),
            "ram": self.ram_var.get(), "cpus": self.cpus_var.get(),
            "cpu_model": self.cpu_model_var.get(), "machine": self.machine_var.get(),
            "disk_if": self.diskif_var.get(), "nic": self.nic_var.get(),
            "gl": self.gl_var.get(), "usb": self.usb_var.get(),
            "boot": self.boot_var.get(), "boot_custom": self.boot_custom_var.get(),
            "boot_menu": self.boot_menu_var.get(), "display": self.display_var.get(),
            "sound": self.sound_var.get(), "vga": self.vga_var.get(),
            "bios": self.bios_var.get(), "pflash": self.pflash_var.get(),
            "share_host": self.share_host_var.get(), "share_name": self.share_name_var.get(),
            "extra_disk": self.extra_disk_var.get(), "extra_diskif": self.extra_diskif_var.get(),
            "snapshot": self.snapshot_var.get(), "kernel": self.kernel_var.get(),
            "initrd": self.initrd_var.get(), "append": self.append_var.get(),
            "rng": self.rng_var.get(), "balloon": self.balloon_var.get(), "tpm": self.tpm_var.get(),
            "keyboard": self.keyboard_var.get(), "net_mode": self.net_mode_var.get(),
            "hostfwd": self.hostfwd_var.get(), "tap_if": self.tap_if_var.get(),
            "mac": self.mac_var.get(), "tftp": self.tftp_var.get(),
            "bootfile": self.bootfile_var.get(),
            "extra": self.extra_args.get("1.0", "end").strip()
        }

    def apply_config(self, cfg):
        self.arch_var.set(cfg.get("arch", "x86_64 (64-bit)"))
        self.custom_qemu_var.set(cfg.get("custom_qemu", ""))
        base = os.getcwd()
        self.disk_var.set(cfg.get("disk", os.path.join(base, "vm.qcow2")))
        self.size_var.set(cfg.get("size", "40G"))
        self.iso_var.set(cfg.get("iso", ""))
        self.virtio_var.set(cfg.get("virtio", os.path.join(base, "virtio-win.iso")))
        self.ram_var.set(cfg.get("ram", "4G"))
        self.cpus_var.set(cfg.get("cpus", "2"))
        self.cpu_model_var.set(cfg.get("cpu_model", "host"))
        self.machine_var.set(cfg.get("machine", "q35"))
        self.diskif_var.set(cfg.get("disk_if", "virtio"))
        self.nic_var.set(cfg.get("nic", "virtio-net-pci"))
        self.gl_var.set(cfg.get("gl", False))
        self.usb_var.set(cfg.get("usb", True))
        self.boot_var.set(cfg.get("boot", "menu"))
        self.boot_custom_var.set(cfg.get("boot_custom", "ncd"))
        self.boot_menu_var.set(cfg.get("boot_menu", True))
        self.display_var.set(cfg.get("display", "gtk"))
        self.sound_var.set(cfg.get("sound", "none"))
        self.vga_var.set(cfg.get("vga", "std"))
        self.bios_var.set(cfg.get("bios", ""))
        self.pflash_var.set(cfg.get("pflash", ""))
        self.share_host_var.set(cfg.get("share_host", ""))
        self.share_name_var.set(cfg.get("share_name", "hostshare"))
        self.extra_disk_var.set(cfg.get("extra_disk", ""))
        self.extra_diskif_var.set(cfg.get("extra_diskif", "virtio"))
        self.snapshot_var.set(cfg.get("snapshot", ""))
        self.kernel_var.set(cfg.get("kernel", ""))
        self.initrd_var.set(cfg.get("initrd", ""))
        self.append_var.set(cfg.get("append", ""))
        self.rng_var.set(cfg.get("rng", True))
        self.balloon_var.set(cfg.get("balloon", False))
        self.tpm_var.set(cfg.get("tpm", False))
        self.keyboard_var.set(cfg.get("keyboard", "en-us"))
        self.net_mode_var.set(cfg.get("net_mode", "user"))
        self.hostfwd_var.set(cfg.get("hostfwd", ""))
        self.tap_if_var.set(cfg.get("tap_if", cfg.get("tapif", "")))
        self.mac_var.set(cfg.get("mac", ""))
        self.tftp_var.set(cfg.get("tftp", ""))
        self.bootfile_var.set(cfg.get("bootfile", cfg.get("boot_file", "")))
        self.extra_args.delete("1.0", "end")
        self.extra_args.insert("1.0", cfg.get("extra", ""))
        self.on_arch_change()

    def load_preset(self, name):
        if name in PRESETS:
            p = PRESETS[name].copy()
            if p.get("disk") and not os.path.isabs(p["disk"]):
                p["disk"] = os.path.join(os.getcwd(), p["disk"])
            self.apply_config(p)
            if name.startswith("Teste PXE"):
                messagebox.showinfo(
                    "PXE na LAN",
                    "Este perfil usa TAP (não NAT) para alcançar o servidor PXE interno.\n\n"
                    "Windows:\n"
                    "1. Extra → TAP/bridge = OpenVPN TAP-Windows6\n"
                    "2. Em ncpa.cpl, selecione o TAP e a placa da LAN (cabo) → Ponte de Rede\n"
                    "3. Boot = Rede/PXE, NIC = e1000\n\n"
                    "Linux: modo bridge e interface br0 (ou tap0)."
                )

    def load_selected_preset(self):
        self.load_preset(self.preset_combo.get())

    def save_current_as_preset(self):
        name = tk.simpledialog.askstring("Salvar", "Nome:")
        if name:
            cfg = self.get_current_config()
            with open(os.path.join(CONFIG_DIR, f"{name}.json"), "w", encoding="utf-8") as f:
                json.dump(cfg, f, indent=2, ensure_ascii=False)
            self.refresh_vm_list()
            messagebox.showinfo("OK", "Salvo")

    def save_config(self):
        path = filedialog.asksaveasfilename(initialdir=CONFIG_DIR, defaultextension=".json", filetypes=[("JSON","*.json")])
        if path:
            with open(path, "w", encoding="utf-8") as f:
                json.dump(self.get_current_config(), f, indent=2, ensure_ascii=False)
            self.refresh_vm_list()

    def load_config(self):
        path = filedialog.askopenfilename(initialdir=CONFIG_DIR, filetypes=[("JSON","*.json")])
        if path:
            try:
                with open(path, "r", encoding="utf-8") as f:
                    self.apply_config(json.load(f))
            except Exception as e:
                messagebox.showerror("Erro", str(e))

    def open_config_folder(self):
        os.startfile(CONFIG_DIR)

    def browse_qemu(self):
        p = filedialog.askopenfilename(filetypes=[("Exe","*.exe")])
        if p:
            self.custom_qemu_var.set(p)
            self.arch_var.set("Personalizado")
            self.on_arch_change()

    def browse_disk(self):
        p = filedialog.asksaveasfilename(defaultextension=".qcow2", filetypes=[("QCOW2","*.qcow2")])
        if p: self.disk_var.set(p)

    def browse_iso(self):
        p = filedialog.askopenfilename(filetypes=[("ISO","*.iso")])
        if p: self.iso_var.set(p)

    def browse_virtio(self):
        p = filedialog.askopenfilename(filetypes=[("ISO","*.iso")])
        if p: self.virtio_var.set(p)

    def browse_file(self, var, title, types=None):
        if types is None: types = [("Todos","*.*")]
        p = filedialog.askopenfilename(title=title, filetypes=types)
        if p: var.set(p)

    def browse_dir(self, var):
        p = filedialog.askdirectory()
        if p: var.set(p)

    def build_boot_args(self, cfg):
        boot = cfg["boot"]
        menu = cfg.get("boot_menu", True)
        custom = cfg.get("boot_custom", "").strip()
        if boot == "menu":
            return ["-boot", "menu=on"]
        elif boot == "custom" and custom:
            order = custom
        elif boot.startswith("once_"):
            return ["-boot", f"once={boot.split('_')[1]}"]
        else:
            order = boot
        if order and order not in ("menu", "custom"):
            s = f"order={order}"
            if menu: s += ",menu=on"
            return ["-boot", s]
        return ["-boot", "menu=on"] if menu else []

    def start_vm(self):
        cfg = self.get_current_config()
        disk = cfg["disk"]
        need_disk = (cfg.get("disk_if") or "virtio") != "none"
        if need_disk:
            if not disk:
                messagebox.showerror("Erro", "Disco obrigatório")
                return
            if not os.path.exists(disk):
                try:
                    subprocess.run([QEMU_IMG, "create", "-f", "qcow2", disk, cfg["size"] or "40G"], check=True, capture_output=True)
                except Exception as e:
                    messagebox.showerror("Erro", str(e))
                    return

        qemu = self.get_qemu_binary()
        if not os.path.exists(qemu):
            messagebox.showerror("Erro", f"QEMU não encontrado:\n{qemu}")
            return

        args = [qemu]
        machine = (cfg["machine"] or "q35").split(",")[0]
        if "x86" in cfg["arch"] or "i386" in cfg["arch"]:
            # QEMU recente recusa -accel junto com -machine accel=
            args += ["-accel", "whpx", "-machine", machine]
        else:
            args += ["-machine", machine]

        args += ["-m", cfg["ram"], "-smp", cfg["cpus"], "-cpu", cfg["cpu_model"]]
        if need_disk:
            args += ["-drive", f"file={disk},format=qcow2,if={cfg['disk_if']}"]

        if cfg["extra_disk"] and os.path.exists(cfg["extra_disk"]):
            args += ["-drive", f"file={cfg['extra_disk']},format=qcow2,if={cfg['extra_diskif']}"]

        if cfg["usb"]:
            args += ["-usb", "-device", "usb-tablet", "-device", "usb-kbd"]

        disp = cfg["display"]
        if cfg["gl"] and disp == "gtk":
            args += ["-display", "gtk,gl=on"]
        else:
            args += ["-display", disp]
        if cfg["vga"] != "none":
            args += ["-vga", cfg["vga"]]

        if cfg["sound"] != "none":
            if cfg["sound"] == "hda":
                args += ["-device", "intel-hda", "-device", "hda-duplex"]
            else:
                args += ["-device", cfg["sound"]]

        pxe = cfg["boot"] in ("n", "once_n", "nc") or (
            cfg["boot"] == "custom" and str(cfg.get("boot_custom", "")).startswith("n")
        )
        nic_model = cfg["nic"] if cfg["nic"] and cfg["nic"] != "none" else "e1000"
        mac = (cfg.get("mac") or "").strip()
        mac_opt = f",mac={mac}" if mac else ""
        bootidx = ",bootindex=1" if pxe else ""
        net_mode = cfg.get("net_mode") or "user"

        if cfg["nic"] == "none" or net_mode == "none":
            args += ["-nic", "none"]
        elif net_mode == "user":
            n = f"user,model={nic_model}{mac_opt}"
            if cfg.get("hostfwd"):
                n += f",hostfwd={cfg['hostfwd']}"
            tftp = (cfg.get("tftp") or "").strip()
            bootfile = (cfg.get("bootfile") or "").strip()
            if tftp:
                n += f",tftp={tftp}"
            if bootfile:
                n += f",bootfile={bootfile}"
            if pxe:
                n += ",bootindex=1"
            args += ["-nic", n]
        elif net_mode in ("tap", "bridge"):
            ifname = (cfg.get("tap_if") or "").strip()
            if not ifname:
                messagebox.showerror(
                    "PXE / TAP",
                    "Informe a interface TAP/bridge na aba Extra.\n"
                    "Windows: OpenVPN TAP-Windows6, em ponte com a placa da LAN (cabo).\n"
                    "Linux: tap0 ou br0."
                )
                return
            if " " in ifname and not ifname.startswith('"'):
                ifname_q = f'"{ifname}"'
            else:
                ifname_q = ifname
            if net_mode == "tap":
                nd = f"tap,id=net0,ifname={ifname_q}"
            else:
                nd = f"bridge,id=net0,br={ifname_q}"
            args += ["-netdev", nd, "-device", f"{nic_model},netdev=net0{mac_opt}{bootidx}"]
        else:
            args += ["-nic", "none"]

        if cfg["iso"] and os.path.exists(cfg["iso"]):
            args += ["-cdrom", cfg["iso"]]
        if cfg["virtio"] and os.path.exists(cfg["virtio"]) and ("x86" in cfg["arch"] or "i386" in cfg["arch"]):
            args += ["-drive", f"file={cfg['virtio']},media=cdrom,index=1"]

        if cfg["bios"] and os.path.exists(cfg["bios"]):
            args += ["-bios", cfg["bios"]]
        if cfg["pflash"] and os.path.exists(cfg["pflash"]):
            args += ["-drive", f"if=pflash,format=raw,file={cfg['pflash']}"]

        if cfg["share_host"] and os.path.isdir(cfg["share_host"]):
            tag = cfg["share_name"] or "hostshare"
            args += ["-fsdev", f"local,id=fs0,path={cfg['share_host']},security_model=none",
                     "-device", f"virtio-9p-pci,fsdev=fs0,mount_tag={tag}"]

        if cfg["snapshot"]:
            args += ["-loadvm", cfg["snapshot"]]

        if cfg["kernel"] and os.path.exists(cfg["kernel"]):
            args += ["-kernel", cfg["kernel"]]
        if cfg["initrd"] and os.path.exists(cfg["initrd"]):
            args += ["-initrd", cfg["initrd"]]
        if cfg["append"]:
            args += ["-append", cfg["append"]]

        if cfg["rng"]:
            args += ["-device", "virtio-rng-pci"]
        if cfg["balloon"]:
            args += ["-device", "virtio-balloon-pci"]
        if cfg["tpm"]:
            args += ["-tpmdev", "emulator,id=tpm0,chardev=chrtpm", "-chardev", "socket,id=chrtpm,path=./swtpm-sock", "-device", "tpm-tis,tpmdev=tpm0"]

        if cfg["keyboard"] != "en-us":
            args += ["-k", cfg["keyboard"]]

        args += self.build_boot_args(cfg)
        if cfg["extra"]:
            args += cfg["extra"].split()

        os.makedirs(CONFIG_DIR, exist_ok=True)
        log_path = os.path.join(CONFIG_DIR, "last-qemu.log")
        try:
            with open(log_path, "w", encoding="utf-8") as log:
                log.write("CMD: " + " ".join(args) + "\n\n")
                log.flush()
                proc = subprocess.Popen(args, stdout=log, stderr=subprocess.STDOUT)
            time.sleep(0.8)
            rc = proc.poll()
            if rc is not None:
                try:
                    err = open(log_path, encoding="utf-8", errors="replace").read()
                except OSError:
                    err = f"QEMU saiu com código {rc}"
                messagebox.showerror("QEMU encerrou na hora", err[-2500:])
                self.status.config(text="QEMU falhou — veja last-qemu.log")
                return
            self.status.config(text="VM iniciada")
        except Exception as e:
            messagebox.showerror("Erro", str(e))


if __name__ == "__main__":
    root = tk.Tk()
    app = QemuManager(root)
    root.mainloop()
