using System.Diagnostics;
using System.Text.Json;

namespace QemuManager;

public class Config
{
    public string Arch { get; set; } = "x86_64 (64-bit)";
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
    public string Display { get; set; } = "gtk";
    public string Sound { get; set; } = "none";
    public string Vga { get; set; } = "std";
    public string Extra { get; set; } = "";
}

public partial class Form1 : Form
{
    private const string WorkDir = @"G:\vm";
    private const string QemuDir = @"C:\Program Files\qemu";
    private readonly string ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "QEMU_Manager_Configs");
    private readonly Dictionary<string, Config> VmConfigs = new();
    private readonly ToolTip tip = new() { AutoPopDelay = 8000, InitialDelay = 400, ReshowDelay = 200 };
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
        ["Personalizado"] = "CUSTOM"
    };

    private readonly Dictionary<string, Config> Presets = new()
    {
        ["Windows 11 x64"] = new() { Arch = "x86_64 (64-bit)", Disk = "win11.qcow2", Size = "80G", Ram = "8G", Cpus = "4", CpuModel = "host", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "std", Usb = true },
        ["Windows 10 x64"] = new() { Arch = "x86_64 (64-bit)", Disk = "win10.qcow2", Size = "80G", Ram = "8G", Cpus = "4", CpuModel = "host", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "std", Usb = true },
        ["Windows 10 x86"] = new() { Arch = "i386 (32-bit)", Disk = "win10x86.qcow2", Size = "60G", Ram = "4G", Cpus = "2", CpuModel = "qemu32", Machine = "pc", DiskIf = "ide", Nic = "e1000", Vga = "std", Usb = true },
        ["Windows 7 x64"] = new() { Arch = "x86_64 (64-bit)", Disk = "win7.qcow2", Size = "60G", Ram = "4G", Cpus = "2", CpuModel = "qemu64", Machine = "pc", DiskIf = "ide", Nic = "e1000" },
        ["Windows XP"] = new() { Arch = "i386 (32-bit)", Disk = "winxp.qcow2", Size = "40G", Ram = "2G", Cpus = "1", CpuModel = "qemu32", Machine = "pc", DiskIf = "ide", Nic = "rtl8139", Vga = "cirrus", Usb = false },
        ["Ubuntu 24.04 x64"] = new() { Arch = "x86_64 (64-bit)", Disk = "ubuntu.qcow2", Size = "40G", Ram = "4G", Cpus = "4", CpuModel = "host", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "virtio", Gl = true },
        ["macOS (OpenCore x64)"] = new() { Arch = "x86_64 (64-bit)", Disk = "macos.qcow2", Size = "80G", Ram = "8G", Cpus = "4", CpuModel = "Penryn", Machine = "q35", DiskIf = "virtio", Nic = "virtio-net-pci", Vga = "virtio", Extra = "-device isa-applesmc,osk=\"ourhardworkbythesewordsguardedpleasedontsteal(c)AppleComputerInc\" -smbios type=2" }
    };

    private ListBox lstVms = null!;
    private ComboBox cboPreset = null!, cboArch = null!, cboRam = null!, cboCpuModel = null!, cboMachine = null!, cboDiskIf = null!, cboNic = null!, cboDisplay = null!, cboSound = null!, cboVga = null!;
    private TextBox txtDisk = null!, txtSize = null!, txtIso = null!, txtVirtio = null!, txtExtra = null!;
    private NumericUpDown numCpus = null!;
    private CheckBox chkGl = null!, chkUsb = null!;
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
        Size = new Size(900, 640);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 560);
        Font = new Font("Segoe UI", 9F);
        Icon = AppIcons.App;

        var tabs = new TabControl { Dock = DockStyle.Fill, ImageList = AppIcons.Tabs };
        Controls.Add(tabs);

        // ===== VMs =====
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
        tabVms.Controls.Add(panelBtns);
        tabs.TabPages.Add(tabVms);

        // ===== Principal =====
        var tabMain = new TabPage("Principal") { ImageKey = "main" };
        var mainPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        int y = 16;
        const int labelX = 16;
        const int controlX = 150;
        const int controlW = 520;
        const int btnX = 680;

        AddLabel(mainPanel, "Perfil:", labelX, y);
        cboPreset = CreateCombo(mainPanel, controlX, y - 2, 400, Presets.Keys.ToArray());
        tip.SetToolTip(cboPreset, "Perfis prontos com configurações recomendadas.");
        var btnLoad = new Button { Text = "Carregar", Location = new Point(btnX, y - 3), Size = new Size(80, 26) };
        btnLoad.Click += (s, e) => LoadPreset(cboPreset.Text);
        tip.SetToolTip(btnLoad, "Aplica o perfil selecionado.");
        mainPanel.Controls.Add(btnLoad);
        y += 36;

        AddLabel(mainPanel, "Arquitetura:", labelX, y);
        cboArch = CreateCombo(mainPanel, controlX, y - 2, controlW, Architectures.Keys.ToArray());
        tip.SetToolTip(cboArch, "Arquitetura da VM. Cada uma usa um binário qemu-system-* diferente.");
        y += 36;

        AddLabel(mainPanel, "Disco:", labelX, y);
        txtDisk = AddTextBox(mainPanel, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtDisk, "Arquivo do disco virtual (.qcow2). Será criado se não existir.");
        AddBrowseButton(mainPanel, btnX, y - 3, () => BrowseSave(txtDisk), "Escolher ou criar o disco");
        y += 36;

        AddLabel(mainPanel, "Tamanho:", labelX, y);
        txtSize = AddTextBox(mainPanel, controlX, y - 2, 120);
        tip.SetToolTip(txtSize, "Tamanho do disco ao criar (ex: 40G, 80G).");
        y += 36;

        AddLabel(mainPanel, "ISO:", labelX, y);
        txtIso = AddTextBox(mainPanel, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtIso, "ISO de instalação (opcional se o sistema já estiver instalado).");
        AddBrowseButton(mainPanel, btnX, y - 3, () => BrowseOpen(txtIso, "ISO|*.iso"), "Selecionar ISO");
        y += 36;

        AddLabel(mainPanel, "VirtIO ISO:", labelX, y);
        txtVirtio = AddTextBox(mainPanel, controlX, y - 2, controlW - 40);
        tip.SetToolTip(txtVirtio, "ISO dos drivers VirtIO (recomendado para Windows).");
        AddBrowseButton(mainPanel, btnX, y - 3, () => BrowseOpen(txtVirtio, "ISO|*.iso"), "Selecionar VirtIO ISO");

        tabMain.Controls.Add(mainPanel);
        tabs.TabPages.Add(tabMain);

        // ===== Hardware =====
        var tabHw = new TabPage("Hardware") { ImageKey = "hw" };
        var hwPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        y = 16;

        AddLabel(hwPanel, "RAM:", labelX, y);
        cboRam = CreateCombo(hwPanel, controlX, y - 2, 160, new[] { "512M", "1G", "2G", "4G", "8G", "16G", "32G", "64G" });
        tip.SetToolTip(cboRam, "Quantidade de memória RAM da VM.");
        y += 36;

        AddLabel(hwPanel, "CPUs:", labelX, y);
        numCpus = new NumericUpDown { Location = new Point(controlX, y - 2), Size = new Size(80, 24), Minimum = 1, Maximum = 128, Value = 4 };
        hwPanel.Controls.Add(numCpus);
        tip.SetToolTip(numCpus, "Número de núcleos de CPU virtuais.");
        y += 36;

        AddLabel(hwPanel, "Modelo CPU:", labelX, y);
        cboCpuModel = CreateCombo(hwPanel, controlX, y - 2, 200, new[] { "host", "qemu64", "qemu32", "Penryn", "cortex-a72", "rv64", "max" });
        tip.SetToolTip(cboCpuModel, "Modelo de CPU apresentado ao guest. 'host' é o mais rápido.");
        y += 36;

        AddLabel(hwPanel, "Máquina:", labelX, y);
        cboMachine = CreateCombo(hwPanel, controlX, y - 2, 160, new[] { "q35", "pc", "virt" });
        tip.SetToolTip(cboMachine, "Tipo de máquina (chipset).");
        y += 36;

        AddLabel(hwPanel, "Disco IF:", labelX, y);
        cboDiskIf = CreateCombo(hwPanel, controlX, y - 2, 160, new[] { "virtio", "ide", "sata", "scsi" });
        tip.SetToolTip(cboDiskIf, "Interface do disco. virtio é a mais rápida (precisa de drivers no Windows).");
        y += 36;

        AddLabel(hwPanel, "Rede:", labelX, y);
        cboNic = CreateCombo(hwPanel, controlX, y - 2, 200, new[] { "virtio-net-pci", "e1000", "e1000e", "rtl8139", "vmxnet3" });
        tip.SetToolTip(cboNic, "Modelo da placa de rede. virtio-net-pci é a mais rápida.");
        y += 36;

        chkGl = new CheckBox { Text = "OpenGL", Location = new Point(controlX, y), AutoSize = true };
        hwPanel.Controls.Add(chkGl);
        tip.SetToolTip(chkGl, "Ativa aceleração OpenGL no display.");
        y += 30;

        chkUsb = new CheckBox { Text = "USB Mouse / Teclado", Location = new Point(controlX, y), AutoSize = true, Checked = true };
        hwPanel.Controls.Add(chkUsb);
        tip.SetToolTip(chkUsb, "Adiciona mouse e teclado USB (melhor integração do cursor).");
        y += 36;

        AddLabel(hwPanel, "Display:", labelX, y);
        cboDisplay = CreateCombo(hwPanel, controlX, y - 2, 160, new[] { "gtk", "sdl", "vnc", "none" });
        tip.SetToolTip(cboDisplay, "Tipo de saída de vídeo. gtk = janela gráfica padrão.");
        y += 36;

        AddLabel(hwPanel, "Som:", labelX, y);
        cboSound = CreateCombo(hwPanel, controlX, y - 2, 160, new[] { "none", "hda", "ac97", "sb16", "es1370" });
        tip.SetToolTip(cboSound, "Placa de som. hda = Intel HD Audio (recomendado).");
        y += 36;

        AddLabel(hwPanel, "VGA:", labelX, y);
        cboVga = CreateCombo(hwPanel, controlX, y - 2, 160, new[] { "std", "virtio", "qxl", "cirrus", "vmware" });
        tip.SetToolTip(cboVga, "Modelo de placa de vídeo. virtio ou qxl são melhores para Linux.");

        tabHw.Controls.Add(hwPanel);
        tabs.TabPages.Add(tabHw);

        // ===== Extra =====
        var tabExtra = new TabPage("Extra") { ImageKey = "extra" };
        var extraPanel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 2 };
        extraPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        extraPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        extraPanel.Controls.Add(new Label { Text = "Argumentos extras:", AutoSize = true }, 0, 0);
        txtExtra = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9F) };
        tip.SetToolTip(txtExtra, "Argumentos extras livres do QEMU.\nEx: -vga virtio -device virtio-rng-pci");
        extraPanel.Controls.Add(txtExtra, 0, 1);
        tabExtra.Controls.Add(extraPanel);
        tabs.TabPages.Add(tabExtra);

        // Botão iniciar
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 64 };
        var btnStart = new Button
        {
            Text = "  INICIAR MÁQUINA VIRTUAL",
            Size = new Size(300, 40),
            Location = new Point(16, 12),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Image = AppIcons.Play,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft
        };
        btnStart.Click += (s, e) => StartVm();
        tip.SetToolTip(btnStart, "Inicia a máquina virtual com as configurações atuais.");
        bottom.Controls.Add(btnStart);
        Controls.Add(bottom);

        lblStatus = new Label { Text = "Pronto", Dock = DockStyle.Bottom, Height = 22, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
        Controls.Add(lblStatus);
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

    private ComboBox CreateCombo(Panel p, int x, int y, int w, string[] items)
    {
        var cb = new ComboBox
        {
            Location = new Point(x, y),
            Size = new Size(w, 24),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cb.Items.AddRange(items);
        if (items.Length > 0) cb.SelectedIndex = 0;
        p.Controls.Add(cb);
        return cb;
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
        if (dlg.ShowDialog() == DialogResult.OK) target.Text = dlg.FileName;
    }

    private void BrowseOpen(TextBox target, string filter)
    {
        using var dlg = new OpenFileDialog { Filter = filter, InitialDirectory = WorkDir };
        if (dlg.ShowDialog() == DialogResult.OK) target.Text = dlg.FileName;
    }

    private void LoadPreset(string name)
    {
        if (!Presets.TryGetValue(name, out var p)) return;
        cboArch.Text = p.Arch;
        txtDisk.Text = Path.Combine(WorkDir, p.Disk);
        txtSize.Text = p.Size;
        cboRam.Text = p.Ram;
        numCpus.Value = int.Parse(p.Cpus);
        cboCpuModel.Text = p.CpuModel;
        cboMachine.Text = p.Machine;
        cboDiskIf.Text = p.DiskIf;
        cboNic.Text = p.Nic;
        cboVga.Text = p.Vga;
        chkGl.Checked = p.Gl;
        chkUsb.Checked = p.Usb;
        cboSound.Text = p.Sound;
        cboDisplay.Text = "gtk";
        txtExtra.Text = p.Extra;
        txtVirtio.Text = Path.Combine(WorkDir, "virtio-win.iso");
    }

    private void RefreshVmList()
    {
        lstVms.Items.Clear();
        VmConfigs.Clear();
        if (!Directory.Exists(ConfigDir)) return;
        foreach (var f in Directory.GetFiles(ConfigDir, "*.json"))
        {
            try
            {
                var cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText(f));
                if (cfg == null) continue;
                var name = Path.GetFileNameWithoutExtension(f);
                var mark = File.Exists(cfg.Disk) ? "✓" : "✗";
                lstVms.Items.Add($"{mark}  {name}");
                VmConfigs[name] = cfg;
            }
            catch { }
        }
        lblStatus.Text = $"{VmConfigs.Count} VM(s)";
    }

    private string? GetSelectedVmName()
    {
        if (lstVms.SelectedItem == null)
        {
            MessageBox.Show("Selecione uma VM", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            ApplyConfig(cfg);
    }

    private void DeleteSelectedVm()
    {
        var name = GetSelectedVmName();
        if (name != null && MessageBox.Show($"Excluir '{name}'?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            File.Delete(Path.Combine(ConfigDir, name + ".json"));
            RefreshVmList();
        }
    }

    private void ApplyConfig(Config c)
    {
        cboArch.Text = c.Arch;
        txtDisk.Text = c.Disk;
        txtSize.Text = c.Size;
        txtIso.Text = c.Iso;
        txtVirtio.Text = c.Virtio;
        cboRam.Text = c.Ram;
        numCpus.Value = decimal.Parse(c.Cpus);
        cboCpuModel.Text = c.CpuModel;
        cboMachine.Text = c.Machine;
        cboDiskIf.Text = c.DiskIf;
        cboNic.Text = c.Nic;
        chkGl.Checked = c.Gl;
        chkUsb.Checked = c.Usb;
        cboDisplay.Text = c.Display;
        cboSound.Text = c.Sound;
        cboVga.Text = c.Vga;
        txtExtra.Text = c.Extra;
    }

    private Config GetCurrentConfig() => new()
    {
        Arch = cboArch.Text,
        Disk = txtDisk.Text,
        Size = txtSize.Text,
        Iso = txtIso.Text,
        Virtio = txtVirtio.Text,
        Ram = cboRam.Text,
        Cpus = numCpus.Value.ToString(),
        CpuModel = cboCpuModel.Text,
        Machine = cboMachine.Text,
        DiskIf = cboDiskIf.Text,
        Nic = cboNic.Text,
        Gl = chkGl.Checked,
        Usb = chkUsb.Checked,
        Display = cboDisplay.Text,
        Sound = cboSound.Text,
        Vga = cboVga.Text,
        Extra = txtExtra.Text
    };

    private string GetQemuBinary() =>
        cboArch.Text == "Personalizado" ? "" :
        Path.Combine(QemuDir, Architectures.GetValueOrDefault(cboArch.Text, "qemu-system-x86_64.exe"));

    private void StartVm()
    {
        var cfg = GetCurrentConfig();
        if (string.IsNullOrWhiteSpace(cfg.Disk))
        {
            MessageBox.Show("Disco obrigatório", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var qemu = GetQemuBinary();
        if (!File.Exists(qemu))
        {
            MessageBox.Show($"QEMU não encontrado:\n{qemu}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!File.Exists(cfg.Disk))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(QemuDir, "qemu-img.exe"),
                    Arguments = $"create -f qcow2 \"{cfg.Disk}\" {cfg.Size}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                })?.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        var args = new List<string>();
        if (cfg.Arch.Contains("x86") || cfg.Arch.Contains("i386"))
            args.AddRange(new[] { "-accel", "whpx", "-machine", $"{cfg.Machine},accel=whpx" });
        else
            args.AddRange(new[] { "-machine", cfg.Machine });

        args.AddRange(new[] { "-m", cfg.Ram, "-smp", cfg.Cpus, "-cpu", cfg.CpuModel });
        args.AddRange(new[] { "-drive", $"file={cfg.Disk},format=qcow2,if={cfg.DiskIf}" });

        if (cfg.Usb)
            args.AddRange(new[] { "-device", "usb-tablet", "-device", "usb-kbd" });

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

        args.AddRange(new[] { "-nic", $"user,model={cfg.Nic}" });

        if (!string.IsNullOrEmpty(cfg.Iso) && File.Exists(cfg.Iso))
            args.AddRange(new[] { "-cdrom", cfg.Iso });
        if (!string.IsNullOrEmpty(cfg.Virtio) && File.Exists(cfg.Virtio) && (cfg.Arch.Contains("x86") || cfg.Arch.Contains("i386")))
            args.AddRange(new[] { "-drive", $"file={cfg.Virtio},media=cdrom,index=1" });

        if (!string.IsNullOrWhiteSpace(cfg.Extra))
            args.AddRange(cfg.Extra.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = qemu,
                Arguments = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
                UseShellExecute = false
            });
            lblStatus.Text = "VM iniciada";
            MessageBox.Show("Máquina virtual iniciada!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
