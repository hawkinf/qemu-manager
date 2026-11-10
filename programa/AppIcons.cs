using System.Reflection;

namespace QemuManager;

internal static class AppIcons
{
    private static readonly Assembly Asm = typeof(AppIcons).Assembly;

    public static readonly Icon App;
    public static readonly Image Play;
    public static readonly Image Refresh;
    public static readonly Image Edit;
    public static readonly Image Delete;
    public static readonly Image Browse;
    public static readonly Image Save;
    public static readonly Image Disk;
    public static readonly Image Iso;
    public static readonly Image Hardware;
    public static readonly Image Extra;
    public static readonly Image Vm;
    public static readonly Image Add;
    public static readonly ImageList Tabs;

    static AppIcons()
    {
        App = LoadIcon("app.ico") ?? SystemIcons.Application;
        Play = LoadPng("play.png", 20);
        Refresh = LoadPng("refresh.png", 20);
        Edit = LoadPng("edit.png", 20);
        Delete = LoadPng("delete.png", 20);
        Browse = LoadPng("browse.png", 18);
        Save = LoadPng("save.png", 20);
        Disk = LoadPng("disk.png", 20);
        Iso = LoadPng("iso.png", 20);
        Hardware = LoadPng("hardware.png", 20);
        Extra = LoadPng("extra.png", 20);
        Vm = LoadPng("vm.png", 20);
        Add = LoadPng("add.png", 20);

        Tabs = new ImageList
        {
            ImageSize = new Size(16, 16),
            ColorDepth = ColorDepth.Depth32Bit
        };
        Tabs.Images.Add("vms", LoadPng("vm-16.png", 16));
        Tabs.Images.Add("main", LoadPng("add-16.png", 16));
        Tabs.Images.Add("hw", LoadPng("hardware-16.png", 16));
        Tabs.Images.Add("extra", LoadPng("extra-16.png", 16));
    }

    private static Icon? LoadIcon(string name)
    {
        using var stream = Open(name);
        return stream == null ? null : new Icon(stream);
    }

    private static Image LoadPng(string name, int size)
    {
        using var stream = Open(name);
        if (stream == null)
            return new Bitmap(size, size);
        using var src = Image.FromStream(stream);
        return new Bitmap(src, new Size(size, size));
    }

    private static Stream? Open(string name)
    {
        var resource = $"QemuManager.Resources.{name}";
        var stream = Asm.GetManifestResourceStream(resource);
        if (stream != null)
            return stream;

        foreach (var candidate in Asm.GetManifestResourceNames())
        {
            if (candidate.EndsWith("." + name, StringComparison.OrdinalIgnoreCase))
                return Asm.GetManifestResourceStream(candidate);
        }
        return null;
    }
}
