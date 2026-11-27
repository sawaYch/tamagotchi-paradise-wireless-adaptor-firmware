using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

var exe = args.FirstOrDefault(File.Exists);
if (exe is null)
{
    var installed = @"E:\TamaParadiseTool\TamaParadiseTool.exe";
    exe = File.Exists(installed) ? installed : null;
}
if (exe is null)
{
    Show("TamaParadiseTool.exe not found. Path can be passed as argument.");
    return;
}

var hook = Path.Combine(AppContext.BaseDirectory, "TamaHomeBaudHook.dll");
if (!File.Exists(hook))
{
    Show("Cannot found TamaHomeBaudHook.dll. Please put TamaHomeLaunch.exe into the same folder.");
    return;
}

try
{
    var start = new ProcessStartInfo(Path.GetFullPath(exe))
    {
        WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(exe)),
        UseShellExecute = false,
    };
    foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        start.Environment[(string)entry.Key!] = entry.Value?.ToString();
    start.Environment["DOTNET_STARTUP_HOOKS"] = hook;
    start.Environment["DOTNET_ReadyToRun"] = "0";
    Process.Start(start);
}
catch (Exception ex)
{
    Show(ex.Message);
}

static void Show(string message)
{
    MessageBox(IntPtr.Zero, message, "TamaHome", 0x10);
}

[DllImport("user32", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
static extern int MessageBox(IntPtr owner, string text, string caption, uint type);
