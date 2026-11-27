public static class StartupHook
{
    public static void Initialize()
    {
        try
        {
            var dll = Path.Combine(AppContext.BaseDirectory, "System.IO.Ports.dll");
            var patched = TamaHomeLaunch.BaudBypass.Apply(dll);
            TamaHomeLaunch.BaudBypass.Log($"{patched} baud check(s) skipped in {dll}");
        }
        catch (Exception ex)
        {
            TamaHomeLaunch.BaudBypass.Log(ex.ToString());
        }
    }
}
