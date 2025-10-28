using System;
using System.Collections.Generic;
using System.Text;
namespace AMS.Services;

public static class AppRestarter
{
    public static void RestartApp()
    {
#if ANDROID
    Platforms.Android.AppRestarter.Restart();
#elif WINDOWS
    Platforms.Windows.AppRestarter.Restart();
#else
        // fallback: just exit
        try { System.Diagnostics.Process.GetCurrentProcess().Kill(); } catch { Environment.Exit(0); }
#endif
    }
}
