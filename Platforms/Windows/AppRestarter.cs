using System.Diagnostics;
using System.Reflection;

namespace AMS.Platforms.Windows
{
    public static class AppRestarter
    {
        public static void Restart()
        {
            try
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe))
                {
                    Process.Start(exe);
                }
            }
            catch { }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}