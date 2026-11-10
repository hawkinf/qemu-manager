using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace QemuManager;

public class Config
{
    public string Arch { get; set; } = "x86_64 (64-bit)";
    public string CustomQemu { get; set; } = "";
    public string Disk { get; set; } = "";
    public string Size { get; set; } = "40G";
    public string Iso { get; set; } = "";
    public string Virtio { get; set; } = "";
    public string Ram { get; set; } = "4G";
    public string Cpus { get; set; } = "2";
    public string CpuModel { get; set; } = "host";
    public string Machine { get; set; } = "q35";
    public string DiskIf { get; set; } = "virtio";
    public string Nic { get; set; } = "virtio-net-pci";
    public bool Gl { get; set; }
    public bool Usb { get; set; } = true;
    public string Boot { get; set; } = "menu";
    public string BootCustom { get; set; } = "ncd";
    public bool BootMenu { get; set; } = true;
    public string Display { get; set; } = "gtk";
    public string Sound { get; set; } = "none";
    public string Vga { get; set; } = "std";
    public string Bios { get; set; } = "";
    public string Pflash { get; set; } = "";
    public string ShareHost { get; set; } = "";
    public string ShareName { get; set; } = "hostshare";
    public string ExtraDisk { get; set; } = "";
    public string ExtraDiskIf { get; set; } = "virtio";
    public string Snapshot { get; set; } = "";
    public string Kernel { get; set; } = "";
    public string Initrd { get; set; } = "";
    public string Append { get; set; } = "";
    public bool Rng { get; set; } = true;
    public bool Balloon { get; set; }
    public bool Tpm { get; set; }
    public string Keyboard { get; set; } = "en-us";
    public string NetMode { get; set; } = "user";
    public string HostFwd { get; set; } = "";
    public string Extra { get; set; } = "";

    public static Config FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        return new Config
        {
            Arch = Str(r, "Arch", "x86_64 (64-bit)"),
            CustomQemu = Str(r, "CustomQemu"),
            Disk = Str(r, "Disk"),
            Size = Str(r, "Size", "40G"),
            Iso = Str(r, "Iso"),
            Virtio = Str(r, "Virtio"),
            Ram = Str(r, "Ram", "4G"),
            Cpus = Str(r, "Cpus", "2"),
            CpuModel = Str(r, "CpuModel", "host"),
            Machine = Str(r, "Machine", "q35"),
            DiskIf = Str(r, "DiskIf", "virtio"),
            Nic = Str(r, "Nic", "virtio-net-pci"),
            Gl = Flag(r, "Gl"),
            Usb = Flag(r, "Usb", true),
            Boot = Str(r, "Boot", "menu"),
            BootCustom = Str(r, "BootCustom", "ncd"),
            BootMenu = Flag(r, "BootMenu", true),
            Display = Str(r, "Display", "gtk"),
            Sound = Str(r, "Sound", "none"),
            Vga = Str(r, "Vga", "std"),
            Bios = Str(r, "Bios"),
            Pflash = Str(r, "Pflash"),
            ShareHost = Str(r, "ShareHost"),
            ShareName = Str(r, "ShareName", "hostshare"),
            ExtraDisk = Str(r, "ExtraDisk"),
            ExtraDiskIf = Str(r, "ExtraDiskIf", "virtio"),
            Snapshot = Str(r, "Snapshot"),
            Kernel = Str(r, "Kernel"),
            Initrd = Str(r, "Initrd"),
            Append = Str(r, "Append"),
            Rng = Flag(r, "Rng", true),
            Balloon = Flag(r, "Balloon"),
            Tpm = Flag(r, "Tpm"),
            Keyboard = Str(r, "Keyboard", "en-us"),
            NetMode = Str(r, "NetMode", "user"),
            HostFwd = Str(r, "HostFwd"),
            Extra = Str(r, "Extra")
        };
    }

    private static string Str(JsonElement r, string name, string def = "")
    {
        foreach (var p in r.EnumerateObject())
        {
            if (!NamesMatch(p.Name, name)) continue;
            return p.Value.ValueKind switch
            {
                JsonValueKind.String => p.Value.GetString() ?? def,
                JsonValueKind.Number => p.Value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => def
            };
        }
        return def;
    }

    private static bool Flag(JsonElement r, string name, bool def = false)
    {
        foreach (var p in r.EnumerateObject())
        {
            if (!NamesMatch(p.Name, name)) continue;
            return p.Value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => p.Value.GetString() is "true" or "True" or "1",
                JsonValueKind.Number => p.Value.GetInt32() != 0,
                _ => def
            };
        }
        return def;
    }

    private static bool NamesMatch(string jsonName, string prop) =>
        jsonName.Equals(prop, StringComparison.OrdinalIgnoreCase) ||
        jsonName.Replace("_", "").Equals(prop, StringComparison.OrdinalIgnoreCase);
}

public partial class Form1 : Form
{
    private const string WorkDir = @"G:\vm";
    private const string QemuDir = @"C:\Program Files\qemu";
    private readonly string ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "QEMU_Manager_Configs");
    private readonly Dictionary<string, Config> VmConfigs = new();
    private readonly List<Process> Running = new();
    private readonly ToolTip tip = new() { AutoPopDelay = 8000, InitialDelay = 400, ReshowDelay = 200 };
    private static readonly JsonSerializerOptions JsonWrite = new() { WriteIndented = true };

    private readonly Dictionary<string, string> Architectures = new()
    {
        ["x86_64 (64-bit)"] = "qemu-system-x86_64.exe",
        ["i386 (32-bit)"] = "qemu-system-i386.exe",
        ["aarch64 (ARM 64-bit)"] = "qemu-system-aarch64.exe",
        ["arm (ARM 32-bit)"] = "qemu-system-arm.exe",
        ["riscv64"] = "qemu-system-riscv64.exe",
        ["riscv32"] = "qemu-system-riscv32.exe",
        ["ppc64 (PowerPC 64)"] = "qemu-system-ppc64.exe",
        ["ppc (PowerPC 32)"] = "qemu-system-ppc.exe",
        ["mips64el"] = "qemu-system-mips64el.exe",
        ["mipsel"] = "qemu-system-mipsel.exe",
        ["sparc64"] = "qemu-system-sparc64.exe",
        ["sparc"] = "qemu-system-sparc.exe",
        ["s390x"] = "qemu-system-s390x.exe",
        ["Personalizado"] = "CUSTOM"
    };

    private readonly Dictionary<string, string[]> MachineSuggestions = new()
    {
        ["x86_64 (64-bit)"] = new[] { "q35", "pc", "virt" },
        ["i386 (32-bit)"] = new[] { "pc", "q35" },
        ["aarch64 (ARM 64-bit)"] = new[] { "virt", "virt-2.12", "raspi3b", "raspi4b" },
        ["arm (ARM 32-bit)"] = new[] { "virt", "integratorcp", "realview-pb-a8" },
        ["riscv64"] = new[] { "virt", "sifive_u" },
        ["riscv32"] = new[] { "virt", "sifive_u" },
        ["ppc64 (PowerPC 64)"] = new[] { "pseries", "powernv", "mac99" },
        ["ppc (PowerPC 32)"] = new[] { "mac99", "g3beige" },
        ["mips64el"] = new[] { "malta", "virt" },
        ["mipsel"] = new[] { "malta" },
        ["sparc64"] = new[] { "sun4u" },
        ["sparc"] = new[] { "SS-5", "SS-10" },
        ["s390x"] = new[] { "s390-ccw-virtio" }
    };

    private readonly Dictionary<string, string> BootModes = new()
    {
        ["Menu"] = "menu",
        ["CD"] = "d",
        ["Disco"] = "c",
        ["Rede/PXE"] = "n",
        ["Floppy"] = "a",
        ["CD → Disco"] = "dc",
        ["Rede → Disco"] = "nc",
        ["Once Rede"] = "once_n",
        ["Once CD"] = "once_d",
        ["Custom"] = "custom"
    };

    private readonly Dictionary<string, Config> Presets = new()
    {
        ["Windows 11 x64"] = new() { Arch = "x86_64 (64-bit)", Disk = "win11.qcow2", Size = "80G", Ram = "8G", Cpus = "4", CpuModel = "host", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "std", Usb = true, Rng = true },
        ["Windows 10 x64"] = new() { Arch = "x86_64 (64-bit)", Disk = "win10.qcow2", Size = "80G", Ram = "8G", Cpus = "4", CpuModel = "host", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "std", Usb = true, Rng = true },
        ["Windows 10 x86"] = new() { Arch = "i386 (32-bit)", Disk = "win10x86.qcow2", Size = "60G", Ram = "4G", Cpus = "2", CpuModel = "qemu32", Machine = "pc", DiskIf = "ide", Nic = "e1000", Vga = "std", Usb = true },
        ["Windows 7 x64"] = new() { Arch = "x86_64 (64-bit)", Disk = "win7.qcow2", Size = "60G", Ram = "4G", Cpus = "2", CpuModel = "qemu64", Machine = "pc", DiskIf = "ide", Nic = "e1000", Usb = true },
        ["Windows XP"] = new() { Arch = "i386 (32-bit)", Disk = "winxp.qcow2", Size = "40G", Ram = "2G", Cpus = "1", CpuModel = "qemu32", Machine = "pc", DiskIf = "ide", Nic = "rtl8139", Vga = "cirrus", Usb = false, Rng = false },
        ["Ubuntu 24.04 x64"] = new() { Arch = "x86_64 (64-bit)", Disk = "ubuntu.qcow2", Size = "40G", Ram = "4G", Cpus = "4", CpuModel = "host", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "virtio", Gl = true, Usb = true, Rng = true },
        ["Ubuntu ARM64"] = new() { Arch = "aarch64 (ARM 64-bit)", Disk = "ubuntu-arm.qcow2", Size = "40G", Ram = "4G", Cpus = "4", CpuModel = "cortex-a72", Machine = "virt", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "virtio", Usb = true, Rng = true },
        ["Debian x64"] = new() { Arch = "x86_64 (64-bit)", Disk = "debian.qcow2", Size = "30G", Ram = "2G", Cpus = "2", CpuModel = "host", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "virtio", Gl = true, Usb = true, Rng = true },
        ["RISC-V 64"] = new() { Arch = "riscv64", Disk = "riscv.qcow2", Size = "20G", Ram = "2G", Cpus = "2", CpuModel = "rv64", Machine = "virt", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "virtio", Usb = true },
        ["Alpine / Mínimo"] = new() { Arch = "x86_64 (64-bit)", Disk = "alpine.qcow2", Size = "10G", Ram = "1G", Cpus = "1", CpuModel = "host", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "virtio", Usb = true },
        ["Windows 98 / DOS"] = new() { Arch = "i386 (32-bit)", Disk = "win98.qcow2", Size = "8G", Ram = "512M", Cpus = "1", CpuModel = "qemu32", Machine = "pc", DiskIf = "ide", Nic = "rtl8139", Vga = "cirrus", Usb = false, Sound = "sb16", Rng = false },
        ["macOS (OpenCore x64)"] = new() { Arch = "x86_64 (64-bit)", Disk = "macos.qcow2", Size = "80G", Ram = "8G", Cpus = "4", CpuModel = "Penryn", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "virtio", Usb = true, Rng = true, Extra = "-device isa-applesmc,osk=\"ourhardworkbythesewordsguardedpleasedontsteal(c)AppleComputerInc\" -smbios type=2" }
    };

    private TabControl tabs = null!;
    private ListBox lstVms = null!;
    private ComboBox cboPreset = null!, cboArch = null!, cboRam = null!, cboCpuModel = null!, cboMachine = null!, cboDiskIf = null!, cboNic = null!, cboDisplay = null!, cboSound = null!, cboVga = null!, cboBoot = null!, cboExtraDiskIf = null!, cboKeyboard = null!, cboNetMode = null!;
    private TextBox txtDisk = null!, txtSize = null!, txtIso = null!, txtVirtio = null!, txtExtra = null!, txtCustomQemu = null!, txtBootCustom = null!, txtBios = null!, txtPflash = null!, txtShareHost = null!, txtShareName = null!, txtExtraDisk = null!, txtSnapshot = null!, txtKernel = null!, txtInitrd = null!, txtAppend = null!, txtHostFwd = null!, txtLog = null!;
    private NumericUpDown numCpus = null!;
    private CheckBox chkGl = null!, chkUsb = null!, chkBootMenu = null!, chkRng = null!, chkBalloon = null!, chkTpm = null!;
    private Label lblStatus = null!;

    public Form1()
    {
        Directory.CreateDirectory(ConfigDir);
        Directory.CreateDirectory(WorkDir);
        InitializeComponent();
        LoadPreset("Windows 10 x64");
        RefreshVmList();
    }

    private void InitializeComponent()
    {
        Text = "QEMU Manager";
        Size = new Size(1020, 800);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(960, 700);
        Font = new Font("Segoe UI", 9F);
        Icon = AppIcons.App;

        tabs = new TabControl { Dock = DockStyle.Fill, ImageList = AppIcons.Tabs };
        Controls.Add(tabs);

        BuildTabVms();
        BuildTabMain();
        BuildTabHardware();
        BuildTabExtra();
        BuildTabLog();

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 64 };
        var btnSave = new Button
        {
            Text = "  Salvar",
            Size = new Size(140, 40),
            Location = new Point(16, 12),
            Image = AppIcons.Save,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft
        };
        btnSave.Click += (s, e) => SaveCurrentAs();
        tip.SetToolTip(btnSave, "Salva a configuração atual em JSON.");
        var btnStart = new Button
        {
            Text = "  INICIAR MÁQUINA VIRTUAL",
            Size = new Size(300, 40),
            Location = new Point(168, 12),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Image = AppIcons.Play,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft
        };
        btnStart.Click += (s, e) => StartVm();
        tip.SetToolTip(btnStart, "Inicia a máquina virtual com as configurações atuais.");
        bottom.Controls.Add(btnSave);
        bottom.Controls.Add(btnStart);
        Controls.Add(bottom);

        lblStatus = new Label { Text = "Pronto", Dock = DockStyle.Bottom, Height = 22, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
        Controls.Add(lblStatus);
    }

    private void BuildTabVms()
    {
        var tabVms = new TabPage("VMs") { ImageKey = "vms" };
        lstVms = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), IntegralHeight = false };
        lstVms.DoubleClick += (s, e) => StartSelectedVm();
        tip.SetToolTip(lstVms, "Lista de VMs salvas.\n✓ = disco existe | ✗ = disco não encontrado\nDuplo clique inicia.");
        tabVms.Controls.Add(lstVms);

        var panelBtns = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(6) };
        AddButton(panelBtns, "Atualizar", 110, RefreshVmList, "Recarrega a lista de VMs", AppIcons.Refresh);
        AddButton(panelBtns, "Iniciar", 100, StartSelectedVm, "Inicia a VM selecionada", AppIcons.Play);
        AddButton(panelBtns, "Editar", 100, EditSelectedVm, "Carrega a configuração para edição", AppIcons.Edit);
        AddButton(panelBtns, "Excluir", 100, DeleteSelectedVm, "Remove apenas o arquivo de configuração", AppIcons.Delete);
        AddButton(panelBtns, "Pasta", 100, OpenConfigFolder, "Abre a pasta das configurações", AppIcons.Browse);
        tabVms.Controls.Add(panelBtns);
        tabs.TabPages.Add(tabVms);
    }

    private void BuildTabMain()
    {
        var tabMain = new TabPage("Principal") { ImageKey = "main" };
        var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        int y = 16;
        const int labelX = 16, controlX = 150, controlW = 560, btnX = 730;

        AddLabel(p, "Perfil:", labelX, y);
        cboPreset = CreateCombo(p, controlX, y - 2, 400, Presets.Keys.ToArray());
        tip.SetToolTip(cboPreset, "Perfis prontos com configurações recomendadas.");
        var btnLoad = new Button { Text = "Carregar", Location = new Point(btnX, y - 3), Size = new Size(90, 26) };
        btnLoad.Click += (s, e) => LoadPreset(cboPreset.Text);
        tip.SetToolTip(btnLoad, "Aplica o perfil selecionado.");
        p.Controls.Add(btnLoad);
        y += 36;

        AddLabel(p, "Arquitetura:", labelX, y);
        cboArch = CreateCombo(p, controlX, y - 2, controlW, Architectures.Keys.ToArray());
        cboArch.SelectedIndexChanged += (s, e) => OnArchChange();
        tip.SetToolTip(cboArch, "Arquitetura da VM. Cada uma usa um binário qemu-system-* diferente.");
        y += 36;

        AddLabel(p, "QEMU custom:", labelX, y);
        txtCustomQemu = AddTextBox(p, controlX, y - 2, controlW - 40);
        txtCustomQemu.Enabled = false;
        tip.SetToolTip(txtCustomQemu, "Caminho do qemu-system-*.exe (apenas se a arquitetura for Personalizado).");
        AddBrowseButton(p, btnX, y - 3, () =>
        {
            BrowseOpen(txtCustomQemu, "Executável|*.exe");
            if (!string.IsNullOrWhiteSpace(txtCustomQemu.Text))
                SetCombo(cboArch, "Personalizado");
        }, "Procurar qemu-system-*.exe");
        y += 36;

        AddLabel(p, "Disco:", labelX, y);
        txtDisk = AddTextBox(p, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtDisk, "Arquivo do disco virtual (.qcow2). Será criado se não existir.");
        AddBrowseButton(p, btnX, y - 3, () => BrowseSave(txtDisk), "Escolher ou criar o disco");
        y += 36;

        AddLabel(p, "Tamanho:", labelX, y);
        txtSize = AddTextBox(p, controlX, y - 2, 120);
        tip.SetToolTip(txtSize, "Tamanho do disco ao criar (ex: 40G, 80G).");
        y += 36;

        AddLabel(p, "ISO:", labelX, y);
        txtIso = AddTextBox(p, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtIso, "ISO de instalação (opcional se o sistema já estiver instalado).");
        AddBrowseButton(p, btnX, y - 3, () => BrowseOpen(txtIso, "ISO|*.iso"), "Selecionar ISO");
        y += 36;

        AddLabel(p, "VirtIO ISO:", labelX, y);
        txtVirtio = AddTextBox(p, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtVirtio, "ISO dos drivers VirtIO (recomendado para Windows).");
        AddBrowseButton(p, btnX, y - 3, () => BrowseOpen(txtVirtio, "ISO|*.iso"), "Selecionar VirtIO ISO");
        y += 40;

        AddLabel(p, "Boot:", labelX, y);
        cboBoot = CreateCombo(p, controlX, y - 2, 180, BootModes.Keys.ToArray());
        tip.SetToolTip(cboBoot, "Ordem de boot do QEMU (menu, CD, disco, PXE...).");
        AddLabel(p, "Ordem:", 350, y);
        txtBootCustom = AddTextBox(p, 410, y - 2, 80);
        txtBootCustom.Text = "ncd";
        tip.SetToolTip(txtBootCustom, "Letras: a=floppy, c=disco, d=CD, n=rede.\nEx: ncd = rede, CD, disco.");
        chkBootMenu = AddCheck(p, "menu=on", 510, y, "Mostra o menu de boot mesmo com ordem definida.", true);
        tabMain.Controls.Add(p);
        tabs.TabPages.Add(tabMain);
    }

    private void BuildTabHardware()
    {
        var tabHw = new TabPage("Hardware") { ImageKey = "hw" };
        var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        int y = 16;
        const int labelX = 16, controlX = 150;

        AddLabel(p, "RAM:", labelX, y);
        cboRam = CreateCombo(p, controlX, y - 2, 160, new[] { "256M", "512M", "1G", "2G", "4G", "6G", "8G", "12G", "16G", "32G", "64G", "128G" }, editable: true);
        tip.SetToolTip(cboRam, "Quantidade de memória RAM da VM.");
        y += 36;

        AddLabel(p, "CPUs:", labelX, y);
        numCpus = new NumericUpDown { Location = new Point(controlX, y - 2), Size = new Size(80, 24), Minimum = 1, Maximum = 128, Value = 4 };
        p.Controls.Add(numCpus);
        tip.SetToolTip(numCpus, "Número de núcleos de CPU virtuais.");
        y += 36;

        AddLabel(p, "Modelo CPU:", labelX, y);
        cboCpuModel = CreateCombo(p, controlX, y - 2, 220, new[] { "host", "qemu64", "qemu32", "max", "Penryn", "cortex-a72", "rv64", "Skylake-Client", "Haswell", "Westmere", "Nehalem" }, editable: true);
        tip.SetToolTip(cboCpuModel, "Modelo de CPU apresentado ao guest. 'host' é o mais rápido.");
        y += 36;

        AddLabel(p, "Máquina:", labelX, y);
        cboMachine = CreateCombo(p, controlX, y - 2, 200, new[] { "q35", "pc", "virt" }, editable: true);
        tip.SetToolTip(cboMachine, "Tipo de máquina (chipset). As sugestões mudam com a arquitetura.");
        y += 36;

        AddLabel(p, "Disco IF:", labelX, y);
        cboDiskIf = CreateCombo(p, controlX, y - 2, 160, new[] { "virtio", "ide", "sata", "scsi", "none" });
        tip.SetToolTip(cboDiskIf, "Interface do disco. virtio é a mais rápida (precisa de drivers no Windows).");
        y += 36;

        AddLabel(p, "Rede:", labelX, y);
        cboNic = CreateCombo(p, controlX, y - 2, 200, new[] { "virtio-net-pci", "e1000", "e1000e", "rtl8139", "vmxnet3", "none" });
        tip.SetToolTip(cboNic, "Modelo da placa de rede. virtio-net-pci é a mais rápida.");
        y += 36;

        chkGl = AddCheck(p, "OpenGL", controlX, y, "Ativa aceleração OpenGL no display.");
        y += 30;
        chkUsb = AddCheck(p, "USB Mouse / Teclado", controlX, y, "Adiciona mouse e teclado USB (melhor integração do cursor).", true);
        y += 36;

        AddLabel(p, "Display:", labelX, y);
        cboDisplay = CreateCombo(p, controlX, y - 2, 160, new[] { "gtk", "sdl", "vnc", "curses", "none", "egl-headless" });
        tip.SetToolTip(cboDisplay, "Tipo de saída de vídeo. gtk = janela gráfica padrão.");
        y += 36;

        AddLabel(p, "Som:", labelX, y);
        cboSound = CreateCombo(p, controlX, y - 2, 160, new[] { "none", "hda", "ac97", "sb16", "es1370", "pcspk" });
        tip.SetToolTip(cboSound, "Placa de som. hda = Intel HD Audio (recomendado).");
        y += 36;

        AddLabel(p, "VGA:", labelX, y);
        cboVga = CreateCombo(p, controlX, y - 2, 160, new[] { "std", "virtio", "qxl", "cirrus", "vmware", "bochs", "none" });
        tip.SetToolTip(cboVga, "Modelo de placa de vídeo. virtio ou qxl são melhores para Linux.");

        tabHw.Controls.Add(p);
        tabs.TabPages.Add(tabHw);
    }

    private void BuildTabExtra()
    {
        var tabExtra = new TabPage("Extra") { ImageKey = "extra" };
        var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        int y = 16;
        const int labelX = 16, controlX = 150, controlW = 560, btnX = 730;

        AddLabel(p, "BIOS/OVMF:", labelX, y);
        txtBios = AddTextBox(p, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtBios, "Firmware UEFI (OVMF_CODE.fd) ou BIOS customizado.");
        AddBrowseButton(p, btnX, y - 3, () => BrowseOpen(txtBios, "Firmware|*.fd;*.bin|Todos|*.*"), "Selecionar BIOS/OVMF");
        y += 36;

        AddLabel(p, "pflash (vars):", labelX, y);
        txtPflash = AddTextBox(p, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtPflash, "Variáveis UEFI (OVMF_VARS.fd), usado junto com OVMF.");
        AddBrowseButton(p, btnX, y - 3, () => BrowseOpen(txtPflash, "UEFI vars|*.fd|Todos|*.*"), "Selecionar pflash");
        y += 36;

        AddLabel(p, "Pasta 9p:", labelX, y);
        txtShareHost = AddTextBox(p, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtShareHost, "Pasta do Windows compartilhada com a VM via 9p.");
        AddBrowseButton(p, btnX, y - 3, () => BrowseFolder(txtShareHost), "Selecionar pasta do host");
        y += 36;

        AddLabel(p, "Tag na VM:", labelX, y);
        txtShareName = AddTextBox(p, controlX, y - 2, 200);
        txtShareName.Text = "hostshare";
        tip.SetToolTip(txtShareName, "mount_tag da pasta 9p dentro do guest.");
        y += 36;

        AddLabel(p, "Disco extra:", labelX, y);
        txtExtraDisk = AddTextBox(p, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtExtraDisk, "Segundo disco virtual (opcional).");
        AddBrowseButton(p, btnX, y - 3, () => BrowseOpen(txtExtraDisk, "QCOW2|*.qcow2|Todos|*.*"), "Selecionar disco extra");
        y += 36;

        AddLabel(p, "Disco extra IF:", labelX, y);
        cboExtraDiskIf = CreateCombo(p, controlX, y - 2, 160, new[] { "virtio", "ide", "sata" });
        tip.SetToolTip(cboExtraDiskIf, "Interface do disco extra.");
        y += 36;

        AddLabel(p, "Snapshot:", labelX, y);
        txtSnapshot = AddTextBox(p, controlX, y - 2, 200);
        tip.SetToolTip(txtSnapshot, "Nome do snapshot para carregar ao iniciar (-loadvm).");
        var btnSnaps = new Button { Text = "Listar", Location = new Point(370, y - 3), Size = new Size(80, 26) };
        btnSnaps.Click += (s, e) => ListSnapshots();
        tip.SetToolTip(btnSnaps, "Lista snapshots do disco via qemu-img.");
        p.Controls.Add(btnSnaps);
        y += 36;

        AddLabel(p, "Kernel:", labelX, y);
        txtKernel = AddTextBox(p, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtKernel, "Kernel para boot direto (-kernel).");
        AddBrowseButton(p, btnX, y - 3, () => BrowseOpen(txtKernel, "Kernel|*.*|Todos|*.*"), "Selecionar kernel");
        y += 36;

        AddLabel(p, "Initrd:", labelX, y);
        txtInitrd = AddTextBox(p, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtInitrd, "initrd/initramfs (-initrd).");
        AddBrowseButton(p, btnX, y - 3, () => BrowseOpen(txtInitrd, "Initrd|*.*|Todos|*.*"), "Selecionar initrd");
        y += 36;

        AddLabel(p, "Append:", labelX, y);
        txtAppend = AddTextBox(p, controlX, y - 2, controlW);
        tip.SetToolTip(txtAppend, "Linha de comando do kernel (-append). Ex: root=/dev/sda1");
        y += 40;

        chkRng = AddCheck(p, "VirtIO RNG", controlX, y, "Melhor entropia no guest (virtio-rng-pci).", true);
        chkBalloon = AddCheck(p, "VirtIO Balloon", 280, y, "Ajusta a memória dinamicamente.");
        chkTpm = AddCheck(p, "TPM (swtpm)", 440, y, "TPM emulado. Exige swtpm rodando (socket ./swtpm-sock).");
        y += 36;

        AddLabel(p, "Teclado:", labelX, y);
        cboKeyboard = CreateCombo(p, controlX, y - 2, 120, new[] { "en-us", "pt-br", "es", "fr", "de", "it", "ja", "ru" });
        tip.SetToolTip(cboKeyboard, "Layout de teclado do guest.");
        y += 36;

        AddLabel(p, "Modo rede:", labelX, y);
        cboNetMode = CreateCombo(p, controlX, y - 2, 120, new[] { "user", "none" });
        tip.SetToolTip(cboNetMode, "user = NAT com internet. none = sem rede.");
        y += 36;

        AddLabel(p, "Hostfwd:", labelX, y);
        txtHostFwd = AddTextBox(p, controlX, y - 2, controlW);
        tip.SetToolTip(txtHostFwd, "Redirecionamento de portas. Ex: tcp::2222-:22 (SSH)\ntcp::3389-:3389 (RDP)");
        y += 40;

        AddLabel(p, "Argumentos extras:", labelX, y);
        y += 24;
        txtExtra = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9F), Location = new Point(labelX, y), Size = new Size(740, 90) };
        tip.SetToolTip(txtExtra, "Argumentos extras livres do QEMU.\nEx: -vga virtio -smp 4,sockets=1,cores=4");
        p.Controls.Add(txtExtra);
        y += 104;

        var btnLoadCfg = new Button { Text = "Carregar JSON…", Location = new Point(labelX, y), Size = new Size(140, 28) };
        btnLoadCfg.Click += (s, e) => LoadConfigDialog();
        tip.SetToolTip(btnLoadCfg, "Carrega uma configuração salva.");
        p.Controls.Add(btnLoadCfg);

        tabExtra.Controls.Add(p);
        tabs.TabPages.Add(tabExtra);
    }

    private void BuildTabLog()
    {
        var tabLog = new TabPage("Log") { ImageKey = "extra" };
        txtLog = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9F),
            WordWrap = false
        };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(6) };
        AddButton(panel, "Limpar", 90, () => txtLog.Clear(), "Limpa o log");
        tabLog.Controls.Add(txtLog);
        tabLog.Controls.Add(panel);
        tabs.TabPages.Add(tabLog);
    }

    private Label AddLabel(Panel p, string text, int x, int y)
    {
        var lbl = new Label { Text = text, Location = new Point(x, y + 4), AutoSize = true };
        p.Controls.Add(lbl);
        return lbl;
    }

    private TextBox AddTextBox(Panel p, int x, int y, int w)
    {
        var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 24) };
        p.Controls.Add(tb);
        return tb;
    }

    private CheckBox AddCheck(Panel p, string text, int x, int y, string tooltip, bool on = false)
    {
        var c = new CheckBox { Text = text, Location = new Point(x, y), AutoSize = true, Checked = on };
        p.Controls.Add(c);
        tip.SetToolTip(c, tooltip);
        return c;
    }

    private ComboBox CreateCombo(Panel p, int x, int y, int w, string[] items, bool editable = false)
    {
        var cb = new ComboBox
        {
            Location = new Point(x, y),
            Size = new Size(w, 24),
            DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList
        };
        cb.Items.AddRange(items);
        if (items.Length > 0) cb.SelectedIndex = 0;
        p.Controls.Add(cb);
        return cb;
    }

    private static void SetCombo(ComboBox cb, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        for (int i = 0; i < cb.Items.Count; i++)
        {
            if (string.Equals(cb.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                cb.SelectedIndex = i;
                return;
            }
        }
        if (cb.DropDownStyle != ComboBoxStyle.DropDownList)
            cb.Text = value;
        else
        {
            cb.Items.Add(value);
            cb.SelectedItem = value;
        }
    }

    private void AddBrowseButton(Panel p, int x, int y, Action action, string tooltip)
    {
        var btn = new Button
        {
            Text = "",
            Image = AppIcons.Browse,
            Location = new Point(x, y),
            Size = new Size(32, 26)
        };
        btn.Click += (s, e) => action();
        tip.SetToolTip(btn, tooltip);
        p.Controls.Add(btn);
    }

    private void AddButton(FlowLayoutPanel panel, string text, int width, Action onClick, string tooltip, Image? icon = null)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            Margin = new Padding(4),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 6, 0)
        };
        if (icon != null)
            btn.Image = icon;
        btn.Click += (s, e) => onClick();
        tip.SetToolTip(btn, tooltip);
        panel.Controls.Add(btn);
    }

    private void BrowseSave(TextBox target)
    {
        using var dlg = new SaveFileDialog { Filter = "QCOW2|*.qcow2", DefaultExt = "qcow2", InitialDirectory = WorkDir };
        if (dlg.ShowDialog(this) == DialogResult.OK) target.Text = dlg.FileName;
    }

    private void BrowseOpen(TextBox target, string filter)
    {
        using var dlg = new OpenFileDialog { Filter = filter, InitialDirectory = WorkDir };
        if (dlg.ShowDialog(this) == DialogResult.OK) target.Text = dlg.FileName;
    }

    private void BrowseFolder(TextBox target)
    {
        using var dlg = new FolderBrowserDialog { SelectedPath = WorkDir };
        if (dlg.ShowDialog(this) == DialogResult.OK) target.Text = dlg.SelectedPath;
    }

    private string Rooted(string path) =>
        string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) ? path : Path.Combine(WorkDir, path);

    private void OnArchChange()
    {
        var arch = cboArch.Text;
        var sugg = MachineSuggestions.GetValueOrDefault(arch, new[] { "virt" });
        var current = cboMachine.Text;
        cboMachine.Items.Clear();
        cboMachine.Items.AddRange(sugg);
        SetCombo(cboMachine, sugg.Contains(current) ? current : sugg[0]);
        var custom = arch == "Personalizado";
        txtCustomQemu.Enabled = custom;
    }

    private void LoadPreset(string name)
    {
        if (!Presets.TryGetValue(name, out var p)) return;
        var json = JsonSerializer.Serialize(p);
        var cfg = Config.FromJson(json);
        ApplyConfig(cfg);
    }

    private void RefreshVmList()
    {
        lstVms.Items.Clear();
        VmConfigs.Clear();
        if (!Directory.Exists(ConfigDir)) return;
        foreach (var f in Directory.GetFiles(ConfigDir, "*.json").OrderBy(x => x))
        {
            try
            {
                var cfg = Config.FromJson(File.ReadAllText(f));
                var name = Path.GetFileNameWithoutExtension(f);
                var disk = Rooted(cfg.Disk);
                var mark = File.Exists(disk) ? "✓" : "✗";
                lstVms.Items.Add($"{mark}  {name}");
                VmConfigs[name] = cfg;
            }
            catch (Exception ex)
            {
                Log($"Falha ao ler {Path.GetFileName(f)}: {ex.Message}");
            }
        }
        lblStatus.Text = $"{VmConfigs.Count} VM(s)";
    }

    private string? GetSelectedVmName()
    {
        if (lstVms.SelectedItem == null)
        {
            MessageBox.Show(this, "Selecione uma VM na lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        return lstVms.SelectedItem.ToString()?[3..].Trim();
    }

    private void StartSelectedVm()
    {
        var name = GetSelectedVmName();
        if (name != null && VmConfigs.TryGetValue(name, out var cfg))
        {
            ApplyConfig(cfg);
            StartVm();
        }
    }

    private void EditSelectedVm()
    {
        var name = GetSelectedVmName();
        if (name != null && VmConfigs.TryGetValue(name, out var cfg))
        {
            ApplyConfig(cfg);
            tabs.SelectedIndex = 1;
            lblStatus.Text = $"Editando {name}";
        }
    }

    private void DeleteSelectedVm()
    {
        var name = GetSelectedVmName();
        if (name == null) return;
        if (MessageBox.Show(this, $"Excluir a configuração '{name}'?\nO disco não será apagado.", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        var path = Path.Combine(ConfigDir, name + ".json");
        try
        {
            if (File.Exists(path)) File.Delete(path);
            RefreshVmList();
            Log($"Configuração '{name}' excluída.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível excluir.\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenConfigFolder()
    {
        Directory.CreateDirectory(ConfigDir);
        Process.Start(new ProcessStartInfo { FileName = ConfigDir, UseShellExecute = true });
    }

    private void ApplyConfig(Config c)
    {
        SetCombo(cboArch, c.Arch);
        OnArchChange();
        txtCustomQemu.Text = c.CustomQemu;
        txtDisk.Text = Rooted(c.Disk);
        txtSize.Text = c.Size;
        txtIso.Text = c.Iso;
        txtVirtio.Text = string.IsNullOrWhiteSpace(c.Virtio) ? Path.Combine(WorkDir, "virtio-win.iso") : Rooted(c.Virtio);
        SetCombo(cboRam, c.Ram);
        if (decimal.TryParse(c.Cpus, out var n)) numCpus.Value = Math.Clamp(n, numCpus.Minimum, numCpus.Maximum);
        SetCombo(cboCpuModel, c.CpuModel);
        SetCombo(cboMachine, c.Machine);
        SetCombo(cboDiskIf, c.DiskIf);
        SetCombo(cboNic, c.Nic);
        chkGl.Checked = c.Gl;
        chkUsb.Checked = c.Usb;
        var bootKey = BootModes.FirstOrDefault(kv => kv.Value == c.Boot).Key;
        SetCombo(cboBoot, string.IsNullOrEmpty(bootKey) ? "Menu" : bootKey);
        txtBootCustom.Text = c.BootCustom;
        chkBootMenu.Checked = c.BootMenu;
        SetCombo(cboDisplay, c.Display);
        SetCombo(cboSound, c.Sound);
        SetCombo(cboVga, c.Vga);
        txtBios.Text = c.Bios;
        txtPflash.Text = c.Pflash;
        txtShareHost.Text = c.ShareHost;
        txtShareName.Text = string.IsNullOrWhiteSpace(c.ShareName) ? "hostshare" : c.ShareName;
        txtExtraDisk.Text = c.ExtraDisk;
        SetCombo(cboExtraDiskIf, c.ExtraDiskIf);
        txtSnapshot.Text = c.Snapshot;
        txtKernel.Text = c.Kernel;
        txtInitrd.Text = c.Initrd;
        txtAppend.Text = c.Append;
        chkRng.Checked = c.Rng;
        chkBalloon.Checked = c.Balloon;
        chkTpm.Checked = c.Tpm;
        SetCombo(cboKeyboard, c.Keyboard);
        SetCombo(cboNetMode, c.NetMode);
        txtHostFwd.Text = c.HostFwd;
        txtExtra.Text = c.Extra;
    }

    private Config GetCurrentConfig() => new()
    {
        Arch = cboArch.Text,
        CustomQemu = txtCustomQemu.Text.Trim(),
        Disk = txtDisk.Text.Trim(),
        Size = txtSize.Text.Trim(),
        Iso = txtIso.Text.Trim(),
        Virtio = txtVirtio.Text.Trim(),
        Ram = cboRam.Text,
        Cpus = numCpus.Value.ToString("0"),
        CpuModel = cboCpuModel.Text,
        Machine = cboMachine.Text,
        DiskIf = cboDiskIf.Text,
        Nic = cboNic.Text,
        Gl = chkGl.Checked,
        Usb = chkUsb.Checked,
        Boot = BootModes.GetValueOrDefault(cboBoot.Text, "menu"),
        BootCustom = txtBootCustom.Text.Trim(),
        BootMenu = chkBootMenu.Checked,
        Display = cboDisplay.Text,
        Sound = cboSound.Text,
        Vga = cboVga.Text,
        Bios = txtBios.Text.Trim(),
        Pflash = txtPflash.Text.Trim(),
        ShareHost = txtShareHost.Text.Trim(),
        ShareName = txtShareName.Text.Trim(),
        ExtraDisk = txtExtraDisk.Text.Trim(),
        ExtraDiskIf = cboExtraDiskIf.Text,
        Snapshot = txtSnapshot.Text.Trim(),
        Kernel = txtKernel.Text.Trim(),
        Initrd = txtInitrd.Text.Trim(),
        Append = txtAppend.Text.Trim(),
        Rng = chkRng.Checked,
        Balloon = chkBalloon.Checked,
        Tpm = chkTpm.Checked,
        Keyboard = cboKeyboard.Text,
        NetMode = cboNetMode.Text,
        HostFwd = txtHostFwd.Text.Trim(),
        Extra = txtExtra.Text.Trim()
    };

    private void SaveCurrentAs()
    {
        var suggested = Path.GetFileNameWithoutExtension(txtDisk.Text);
        if (string.IsNullOrWhiteSpace(suggested)) suggested = "vm";
        var name = PromptName("Salvar configuração", suggested);
        if (name == null) return;
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Informe um nome para a VM.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var path = Path.Combine(ConfigDir, name + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(GetCurrentConfig(), JsonWrite));
            RefreshVmList();
            Log($"Configuração salva: {path}");
            lblStatus.Text = $"Salvo: {name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível salvar.\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadConfigDialog()
    {
        using var dlg = new OpenFileDialog { Filter = "JSON|*.json", InitialDirectory = ConfigDir };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ApplyConfig(Config.FromJson(File.ReadAllText(dlg.FileName)));
            lblStatus.Text = $"Carregado: {Path.GetFileNameWithoutExtension(dlg.FileName)}";
            Log($"Carregado {dlg.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Arquivo inválido.\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string? PromptName(string title, string defaultName)
    {
        using var f = new Form
        {
            Text = title,
            Size = new Size(400, 150),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Icon = AppIcons.App
        };
        var tb = new TextBox { Location = new Point(16, 20), Width = 350, Text = defaultName };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(186, 64), Size = new Size(80, 28) };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(276, 64), Size = new Size(90, 28) };
        f.Controls.AddRange(new Control[] { tb, ok, cancel });
        f.AcceptButton = ok;
        f.CancelButton = cancel;
        return f.ShowDialog(this) == DialogResult.OK ? tb.Text.Trim() : null;
    }

    private string GetQemuBinary(Config cfg)
    {
        if (cfg.Arch == "Personalizado")
            return cfg.CustomQemu;
        return Path.Combine(QemuDir, Architectures.GetValueOrDefault(cfg.Arch, "qemu-system-x86_64.exe"));
    }

    private static List<string> SplitArgs(string text)
    {
        var list = new List<string>();
        var cur = new StringBuilder();
        var inQuote = false;
        foreach (var ch in text)
        {
            if (ch == '"')
            {
                inQuote = !inQuote;
                continue;
            }
            if (char.IsWhiteSpace(ch) && !inQuote)
            {
                if (cur.Length > 0)
                {
                    list.Add(cur.ToString());
                    cur.Clear();
                }
            }
            else
                cur.Append(ch);
        }
        if (cur.Length > 0) list.Add(cur.ToString());
        return list;
    }

    private List<string> BuildBootArgs(Config cfg)
    {
        var boot = cfg.Boot;
        var menu = cfg.BootMenu;
        var custom = cfg.BootCustom;
        if (boot == "menu")
            return new List<string> { "-boot", "menu=on" };
        if (boot == "custom" && !string.IsNullOrWhiteSpace(custom))
            boot = custom;
        if (boot.StartsWith("once_", StringComparison.Ordinal))
            return new List<string> { "-boot", $"once={boot[5..]}" };
        if (!string.IsNullOrWhiteSpace(boot) && boot is not ("menu" or "custom"))
        {
            var s = $"order={boot}";
            if (menu) s += ",menu=on";
            return new List<string> { "-boot", s };
        }
        return menu ? new List<string> { "-boot", "menu=on" } : new List<string>();
    }

    private bool ConfirmMissing(string label, string path)
    {
        return MessageBox.Show(this,
            $"{label} não foi encontrado:\n{path}\n\nIniciar mesmo assim?",
            "Arquivo ausente",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private void ListSnapshots()
    {
        var disk = Rooted(txtDisk.Text.Trim());
        var img = Path.Combine(QemuDir, "qemu-img.exe");
        if (string.IsNullOrWhiteSpace(disk) || !File.Exists(disk))
        {
            MessageBox.Show(this, "Informe um disco existente na aba Principal.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!File.Exists(img))
        {
            MessageBox.Show(this, $"qemu-img.exe não encontrado em:\n{img}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = img,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("snapshot");
            psi.ArgumentList.Add("-l");
            psi.ArgumentList.Add(disk);
            using var proc = Process.Start(psi);
            if (proc == null) return;
            var output = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd();
            proc.WaitForExit(15000);
            Log(output.Trim().Length == 0 ? "Nenhum snapshot neste disco." : output.Trim());
            tabs.SelectedIndex = 4;
            MessageBox.Show(this, string.IsNullOrWhiteSpace(output) ? "Nenhum snapshot neste disco." : output, "Snapshots", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StartVm()
    {
        var cfg = GetCurrentConfig();
        cfg.Disk = Rooted(cfg.Disk);
        cfg.Iso = string.IsNullOrWhiteSpace(cfg.Iso) ? "" : Rooted(cfg.Iso);
        cfg.Virtio = string.IsNullOrWhiteSpace(cfg.Virtio) ? "" : Rooted(cfg.Virtio);
        cfg.ExtraDisk = string.IsNullOrWhiteSpace(cfg.ExtraDisk) ? "" : Rooted(cfg.ExtraDisk);
        cfg.Bios = string.IsNullOrWhiteSpace(cfg.Bios) ? "" : Rooted(cfg.Bios);
        cfg.Pflash = string.IsNullOrWhiteSpace(cfg.Pflash) ? "" : Rooted(cfg.Pflash);
        cfg.Kernel = string.IsNullOrWhiteSpace(cfg.Kernel) ? "" : Rooted(cfg.Kernel);
        cfg.Initrd = string.IsNullOrWhiteSpace(cfg.Initrd) ? "" : Rooted(cfg.Initrd);

        if (string.IsNullOrWhiteSpace(cfg.Disk))
        {
            MessageBox.Show(this, "Informe o arquivo de disco na aba Principal.", "Disco obrigatório", MessageBoxButtons.OK, MessageBoxIcon.Error);
            tabs.SelectedIndex = 1;
            return;
        }

        var qemu = GetQemuBinary(cfg);
        if (string.IsNullOrWhiteSpace(qemu) || !File.Exists(qemu))
        {
            MessageBox.Show(this,
                $"QEMU não encontrado:\n{qemu}\n\nInstale o QEMU em {QemuDir} ou escolha Personalizado com o caminho do executável.",
                "QEMU ausente", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!string.IsNullOrEmpty(cfg.Iso) && !File.Exists(cfg.Iso) && !ConfirmMissing("ISO", cfg.Iso))
            return;
        if (!string.IsNullOrEmpty(cfg.Bios) && !File.Exists(cfg.Bios))
        {
            MessageBox.Show(this, $"Firmware BIOS/OVMF não encontrado:\n{cfg.Bios}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!string.IsNullOrEmpty(cfg.Pflash) && !File.Exists(cfg.Pflash))
        {
            MessageBox.Show(this, $"Arquivo pflash não encontrado:\n{cfg.Pflash}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!string.IsNullOrEmpty(cfg.Kernel) && !File.Exists(cfg.Kernel))
        {
            MessageBox.Show(this, $"Kernel não encontrado:\n{cfg.Kernel}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!string.IsNullOrEmpty(cfg.Initrd) && !File.Exists(cfg.Initrd))
        {
            MessageBox.Show(this, $"Initrd não encontrado:\n{cfg.Initrd}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!string.IsNullOrEmpty(cfg.ExtraDisk) && !File.Exists(cfg.ExtraDisk))
        {
            MessageBox.Show(this, $"Disco extra não encontrado:\n{cfg.ExtraDisk}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!string.IsNullOrEmpty(cfg.ShareHost) && !Directory.Exists(cfg.ShareHost))
        {
            MessageBox.Show(this, $"Pasta compartilhada não encontrada:\n{cfg.ShareHost}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!File.Exists(cfg.Disk))
        {
            var img = Path.Combine(QemuDir, "qemu-img.exe");
            if (!File.Exists(img))
            {
                MessageBox.Show(this, $"O disco ainda não existe e qemu-img.exe não foi encontrado:\n{img}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var size = string.IsNullOrWhiteSpace(cfg.Size) ? "40G" : cfg.Size;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cfg.Disk) ?? WorkDir);
                var create = new ProcessStartInfo
                {
                    FileName = img,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                create.ArgumentList.Add("create");
                create.ArgumentList.Add("-f");
                create.ArgumentList.Add("qcow2");
                create.ArgumentList.Add(cfg.Disk);
                create.ArgumentList.Add(size);
                using var proc = Process.Start(create);
                if (proc == null)
                {
                    MessageBox.Show(this, "Falha ao iniciar qemu-img.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var output = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd();
                proc.WaitForExit(60000);
                Log(string.IsNullOrWhiteSpace(output) ? $"Disco criado: {cfg.Disk} ({size})" : output.Trim());
                if (proc.ExitCode != 0)
                {
                    MessageBox.Show(this, $"qemu-img falhou (código {proc.ExitCode}).\n{output}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Não foi possível criar o disco.\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        var args = new List<string>();
        var isX86 = cfg.Arch.Contains("x86", StringComparison.OrdinalIgnoreCase) || cfg.Arch.Contains("i386", StringComparison.OrdinalIgnoreCase);
        if (isX86)
        {
            args.AddRange(new[] { "-accel", "whpx" });
            var machine = cfg.Machine.Contains("accel=") ? cfg.Machine : $"{cfg.Machine},accel=whpx";
            args.AddRange(new[] { "-machine", machine });
        }
        else
            args.AddRange(new[] { "-machine", cfg.Machine });

        args.AddRange(new[] { "-m", cfg.Ram, "-smp", cfg.Cpus, "-cpu", cfg.CpuModel });
        if (cfg.DiskIf != "none")
            args.AddRange(new[] { "-drive", $"file={cfg.Disk},format=qcow2,if={cfg.DiskIf}" });

        if (!string.IsNullOrEmpty(cfg.ExtraDisk) && File.Exists(cfg.ExtraDisk))
            args.AddRange(new[] { "-drive", $"file={cfg.ExtraDisk},format=qcow2,if={cfg.ExtraDiskIf}" });

        if (cfg.Usb)
            args.AddRange(new[] { "-usb", "-device", "usb-tablet", "-device", "usb-kbd" });

        var disp = cfg.Gl && cfg.Display == "gtk" ? "gtk,gl=on" : cfg.Display;
        args.AddRange(new[] { "-display", disp });
        if (cfg.Vga != "none") args.AddRange(new[] { "-vga", cfg.Vga });

        if (cfg.Sound != "none")
        {
            if (cfg.Sound == "hda")
                args.AddRange(new[] { "-device", "intel-hda", "-device", "hda-duplex" });
            else
                args.AddRange(new[] { "-device", cfg.Sound });
        }

        if (cfg.NetMode == "user" && cfg.Nic != "none")
        {
            var nic = $"user,model={cfg.Nic}";
            if (!string.IsNullOrWhiteSpace(cfg.HostFwd))
                nic += $",hostfwd={cfg.HostFwd}";
            args.AddRange(new[] { "-nic", nic });
        }
        else
            args.AddRange(new[] { "-nic", "none" });

        if (!string.IsNullOrEmpty(cfg.Iso) && File.Exists(cfg.Iso))
            args.AddRange(new[] { "-cdrom", cfg.Iso });
        if (!string.IsNullOrEmpty(cfg.Virtio) && File.Exists(cfg.Virtio) && isX86)
            args.AddRange(new[] { "-drive", $"file={cfg.Virtio},media=cdrom,index=1" });

        if (!string.IsNullOrEmpty(cfg.Bios) && File.Exists(cfg.Bios))
            args.AddRange(new[] { "-bios", cfg.Bios });
        if (!string.IsNullOrEmpty(cfg.Pflash) && File.Exists(cfg.Pflash))
            args.AddRange(new[] { "-drive", $"if=pflash,format=raw,file={cfg.Pflash}" });

        if (!string.IsNullOrEmpty(cfg.ShareHost) && Directory.Exists(cfg.ShareHost))
        {
            var tag = string.IsNullOrWhiteSpace(cfg.ShareName) ? "hostshare" : cfg.ShareName;
            args.AddRange(new[]
            {
                "-fsdev", $"local,id=fs0,path={cfg.ShareHost},security_model=none",
                "-device", $"virtio-9p-pci,fsdev=fs0,mount_tag={tag}"
            });
        }

        if (!string.IsNullOrWhiteSpace(cfg.Snapshot))
            args.AddRange(new[] { "-loadvm", cfg.Snapshot });
        if (!string.IsNullOrEmpty(cfg.Kernel) && File.Exists(cfg.Kernel))
            args.AddRange(new[] { "-kernel", cfg.Kernel });
        if (!string.IsNullOrEmpty(cfg.Initrd) && File.Exists(cfg.Initrd))
            args.AddRange(new[] { "-initrd", cfg.Initrd });
        if (!string.IsNullOrWhiteSpace(cfg.Append))
            args.AddRange(new[] { "-append", cfg.Append });

        if (cfg.Rng) args.AddRange(new[] { "-device", "virtio-rng-pci" });
        if (cfg.Balloon) args.AddRange(new[] { "-device", "virtio-balloon-pci" });
        if (cfg.Tpm)
        {
            Log("TPM: é preciso ter o swtpm em execução (socket ./swtpm-sock).");
            args.AddRange(new[]
            {
                "-tpmdev", "emulator,id=tpm0,chardev=chrtpm",
                "-chardev", "socket,id=chrtpm,path=./swtpm-sock",
                "-device", "tpm-tis,tpmdev=tpm0"
            });
        }

        if (!string.Equals(cfg.Keyboard, "en-us", StringComparison.OrdinalIgnoreCase))
            args.AddRange(new[] { "-k", cfg.Keyboard });

        args.AddRange(BuildBootArgs(cfg));
        if (!string.IsNullOrWhiteSpace(cfg.Extra))
            args.AddRange(SplitArgs(cfg.Extra));

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = qemu,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = WorkDir
            };
            foreach (var a in args)
                psi.ArgumentList.Add(a);

            Log($"Iniciando: {qemu} {string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}");
            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log(e.Data); };
            proc.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log(e.Data); };
            proc.Exited += (s, e) =>
            {
                Log($"QEMU encerrou (código {proc.ExitCode}).");
                BeginInvoke(() => lblStatus.Text = $"QEMU saiu: {proc.ExitCode}");
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            Running.Add(proc);
            lblStatus.Text = "VM iniciada — veja a aba Log se houver erros";
            Log("Máquina virtual iniciada.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível iniciar o QEMU.\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log($"Erro ao iniciar: {ex.Message}");
        }
    }

    private void Log(string msg)
    {
        if (IsDisposed || txtLog == null) return;
        void append()
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }
        if (InvokeRequired) BeginInvoke(append);
        else append();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        foreach (var p in Running)
        {
            try { p.Dispose(); } catch { }
        }
        base.OnFormClosed(e);
    }
}

public static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}
