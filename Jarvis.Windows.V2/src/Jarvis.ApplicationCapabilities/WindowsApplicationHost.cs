using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
namespace Jarvis.ApplicationCapabilities;

public sealed class WindowsApplicationHost : IApplicationHost
{
    public ResolvedApplication? Resolve(ApplicationDefinition app)
    {
        string? path = app.Id switch {
            ApplicationId.Calculator => Path.Combine(Environment.SystemDirectory, "calc.exe"),
            ApplicationId.Notepad => Path.Combine(Environment.SystemDirectory, "notepad.exe"),
            ApplicationId.FileExplorer => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
            ApplicationId.Word => ResolveWord(), _ => null };
        return path is not null && File.Exists(path) ? new(app.Id, Path.GetFullPath(path)) : null;
    }
    private static string? ResolveWord()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var key = machine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE");
            if (key?.GetValue(null) is string value && ApprovedWordPath(value.Trim('"'))) return value.Trim('"');
        }
        foreach (var root in ProgramRoots())
            foreach (var suffix in new[] { @"Microsoft Office\root\Office16\WINWORD.EXE", @"Microsoft Office\Office16\WINWORD.EXE" })
            { var path = Path.Combine(root, suffix); if (File.Exists(path)) return path; }
        return null;
    }
    private static IEnumerable<string> ProgramRoots() => new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) }.Where(x => !string.IsNullOrEmpty(x));
    private static bool ApprovedWordPath(string path) => Path.IsPathFullyQualified(path) && File.Exists(path) &&
        Path.GetFileName(path).Equals("WINWORD.EXE", StringComparison.OrdinalIgnoreCase) &&
        ProgramRoots().Any(root => Path.GetFullPath(path).StartsWith(Path.Combine(root, "Microsoft Office") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    public int? Launch(ResolvedApplication app)
    {
        // Absolute allow-listed executable; no shell, arguments, URLs, or caller-provided paths.
        using var process = Process.Start(new ProcessStartInfo { FileName = app.Executable, UseShellExecute = false });
        if (process is null) throw new InvalidOperationException("Windows did not accept launch.");
        return process.Id;
    }
    public WindowEvidence? FindWindow(ResolvedApplication app)
    {
        WindowEvidence? found = null;
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window) || IsIconic(window)) return true;
            var className = ClassName(window);
            if (app.Id == ApplicationId.FileExplorer && className != "CabinetWClass") return true;
            if (app.Id == ApplicationId.Word && className != "OpusApp") return true;
            if (IsCloaked(window)) return true;
            bool Inspect(nint handle)
            {
                GetWindowThreadProcessId(handle, out var pid);
                var path = ImagePath(pid);
                if (path is null || !Matches(app, path)) return true;
                found = new((int)pid, window.ToInt64(), path, className); return false;
            }
            if (!Inspect(window)) return false;
            // Packaged apps can own a child of ApplicationFrameHost's top-level window.
            if (className == "ApplicationFrameWindow") EnumChildWindows(window, (child, _) => Inspect(child), 0);
            return found is null;
        }, 0);
        return found;
    }
    internal static bool Matches(ResolvedApplication app, string path)
    {
        if (Path.GetFullPath(path).Equals(app.Executable, StringComparison.OrdinalIgnoreCase)) return true;
        string? package = app.Id switch { ApplicationId.Calculator => "Microsoft.WindowsCalculator_", ApplicationId.Notepad => "Microsoft.WindowsNotepad_", _ => null };
        if (package is null) return false;
        var windowsApps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps") + Path.DirectorySeparatorChar;
        if (!path.StartsWith(windowsApps, StringComparison.OrdinalIgnoreCase)) return false;
        var folder = path[windowsApps.Length..].Split(Path.DirectorySeparatorChar)[0];
        if (!folder.StartsWith(package, StringComparison.OrdinalIgnoreCase) || !folder.EndsWith("__8wekyb3d8bbwe", StringComparison.OrdinalIgnoreCase)) return false;
        var name = Path.GetFileName(path);
        return app.Id == ApplicationId.Notepad ? name.Equals("Notepad.exe", StringComparison.OrdinalIgnoreCase) : name.Equals("CalculatorApp.exe", StringComparison.OrdinalIgnoreCase) || name.Equals("Calculator.exe", StringComparison.OrdinalIgnoreCase);
    }
    private static string? ImagePath(uint pid)
    {
        var process = OpenProcess(0x1000, false, pid); if (process == 0) return null;
        try { var value = new StringBuilder(32768); var size = value.Capacity; return QueryFullProcessImageName(process, 0, value, ref size) ? value.ToString() : null; }
        finally { CloseHandle(process); }
    }
    private static string ClassName(nint window) { var text = new StringBuilder(256); GetClassName(window, text, text.Capacity); return text.ToString(); }
    private static bool IsCloaked(nint window) => DwmGetWindowAttribute(window, 14, out int cloaked, sizeof(int)) == 0 && cloaked != 0;
    private delegate bool EnumCallback(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, nint state);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint parent, EnumCallback callback, nint state);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder value, int size);
    [DllImport("kernel32.dll")] private static extern nint OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] private static extern bool QueryFullProcessImageName(nint process, int flags, StringBuilder value, ref int size);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}

