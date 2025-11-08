using AMS.Data;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Diagnostics;
using AMS.Services.Interfaces;

namespace AMS
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;
        public static IServiceProvider Services { get; private set; }

        public App(IServiceProvider serviceProvider, IAuthService authService)
        {
            InitializeComponent();
            Services = serviceProvider;
            _authService = authService;

            // IMPORTANT: apply any pending downloaded DB before EF/DbContext initialization
            ApplyPendingDatabaseRestoreIfAny();

            try
            {
                System.Diagnostics.Debug.WriteLine("Bắt đầu khởi tạo database (migrate)...");
                DatabaseInitializer.Initialize(Services); // sync migrate
                System.Diagnostics.Debug.WriteLine("✅ Khởi tạo database thành công.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không thể khởi tạo database: {ex.Message}");
            }
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            Page root = _authService.IsLoggedIn()
                ? Services.GetRequiredService<AppShell>()
                : Services.GetRequiredService<LoginShell>();

            return new Window
            {
                Page = root
            };
        }

        public static void SetRootPage(Page page)
        {
            var window = Current?.Windows.FirstOrDefault();
            if (window != null)
            {
                window.Page = page;
            }
        }

        // New: Apply pending restore (look for ams_download.db recorded in Preferences)
        // replace or update ApplyPendingDatabaseRestoreIfAny in App.xaml.cs
        private void ApplyPendingDatabaseRestoreIfAny()
        {
            try
            {
                if (!Preferences.ContainsKey("db_restore_pending_path"))
                {
                    System.Diagnostics.Debug.WriteLine("[DB Restore] No pending restore flag found.");
                    return;
                }

                var downloadPath = Preferences.Get("db_restore_pending_path", string.Empty);
                if (string.IsNullOrWhiteSpace(downloadPath) || !File.Exists(downloadPath))
                {
                    System.Diagnostics.Debug.WriteLine("[DB Restore] Pending file missing; clearing flag.");
                    Preferences.Remove("db_restore_pending_path");
                    Preferences.Remove("db_restore_pending_filename");
                    return;
                }

                var livePath = Path.Combine(FileSystem.AppDataDirectory, "ams.db");
                var walPath = livePath + "-wal";
                var shmPath = livePath + "-shm";

                System.Diagnostics.Debug.WriteLine($"[DB Restore] Pending downloaded DB: {downloadPath}");
                System.Diagnostics.Debug.WriteLine($"[DB Restore] Live DB path: {livePath}");

                // Backup old DB (if any)
                if (File.Exists(livePath))
                {
                    try
                    {
                        var backup = livePath + ".pre_restore_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                        File.Copy(livePath, backup, overwrite: true);
                        System.Diagnostics.Debug.WriteLine($"[DB Restore] Backed up live DB to {backup}");
                    }
                    catch (Exception be)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DB Restore] Backup failed: {be.Message}");
                    }
                }

                // Ensure WAL/SHM are removed BEFORE swapping
                try
                {
                    if (File.Exists(walPath))
                    {
                        File.Delete(walPath);
                        System.Diagnostics.Debug.WriteLine("[DB Restore] Deleted existing -wal file.");
                    }
                    if (File.Exists(shmPath))
                    {
                        File.Delete(shmPath);
                        System.Diagnostics.Debug.WriteLine("[DB Restore] Deleted existing -shm file.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DB Restore] Warning deleting wal/shm: {ex.Message}");
                }

                // Attempt atomic move, fallback to copy+delete, and ensure download file removed on success
                bool swapSucceeded = false;
                try
                {
                    // If live exists, delete it (we already backed it up)
                    if (File.Exists(livePath))
                        File.Delete(livePath);

                    File.Move(downloadPath, livePath); // this removes downloadPath on success
                    swapSucceeded = true;
                    System.Diagnostics.Debug.WriteLine("[DB Restore] Moved downloaded DB into live path (File.Move).");
                }
                catch (Exception mvEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[DB Restore] File.Move failed: {mvEx.Message}. Attempting Copy+Delete fallback.");

                    try
                    {
                        // Fallback: copy downloaded file to live location then delete downloaded file
                        File.Copy(downloadPath, livePath, overwrite: true);
                        // If copy succeeded, attempt to delete the download file
                        try
                        {
                            File.Delete(downloadPath);
                        }
                        catch (Exception delEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DB Restore] Warning: could not delete download file after copy: {delEx.Message}");
                            // Not fatal; we'll still consider swapSucceeded = true because live DB has been overwritten
                        }
                        swapSucceeded = true;
                        System.Diagnostics.Debug.WriteLine("[DB Restore] Copy+Delete fallback succeeded.");
                    }
                    catch (Exception copyEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DB Restore] Copy fallback failed: {copyEx.Message}");
                        swapSucceeded = false;
                    }
                }

                // If swap succeeded, do a quick sanity check and clear the pending flag
                if (swapSucceeded)
                {
                    try
                    {
                        // Optional sanity: size/hash or table count checks could go here
                        System.Diagnostics.Debug.WriteLine("[DB Restore] Swap succeeded. Clearing pending flag.");
                        Preferences.Remove("db_restore_pending_path");
                        Preferences.Remove("db_restore_pending_filename");

                        // Ensure no leftover download file remains
                        try
                        {
                            if (File.Exists(downloadPath))
                            {
                                File.Delete(downloadPath);
                                System.Diagnostics.Debug.WriteLine("[DB Restore] Deleted leftover downloaded file.");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DB Restore] Could not delete leftover download file: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DB Restore] Post-swap cleanup failed: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DB Restore] Swap did not succeed; leaving pending flag for retry.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB Restore] Unexpected error: {ex.Message}");
            }
        }

        private static int QueryRowCountFromSqlite(string dbPath, string tableName)
        {
            try
            {
                if (!File.Exists(dbPath)) return -1;
                var csb = new SqliteConnectionStringBuilder { DataSource = dbPath, Mode = SqliteOpenMode.ReadOnly };
                using var conn = new SqliteConnection(csb.ToString());
                conn.Open();

                using var cmdChk = conn.CreateCommand();
                cmdChk.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@t";
                cmdChk.Parameters.AddWithValue("@t", tableName);
                var exists = Convert.ToInt32(cmdChk.ExecuteScalar() ?? 0);
                if (exists == 0) return -1;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(1) FROM \"{tableName}\";";
                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB Restore] QueryRowCountFromSqlite error: {ex.Message}");
                return -1;
            }
        }

        private static string ComputeSHA256(string path)
        {
            try
            {
                using var sha = SHA256.Create();
                using var fs = File.OpenRead(path);
                var hash = sha.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}