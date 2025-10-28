using Android.App;
using Android.Content;
using Android.OS; // <-- Add this using directive

namespace AMS.Platforms.Android
{
    public static class AppRestarter
    {
        public static void Restart()
        {
            var activity = MainActivity.Current;
            if (activity == null)
            {
                // fallback: kill
                Process.KillProcess(Process.MyPid());
                return;
            }

            Intent intent = activity.PackageManager.GetLaunchIntentForPackage(activity.PackageName);
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
            activity.StartActivity(intent);
            // give the intent a moment (not guaranteed), then kill process
            Process.KillProcess(Process.MyPid());
        }
    }
}