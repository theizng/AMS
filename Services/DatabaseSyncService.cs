using AMS.Data;  // Add for AMSDbContext
using Microsoft.EntityFrameworkCore;  // Add for DbContext
using Microsoft.Extensions.DependencyInjection;  // Add for IServiceScopeFactory
using Microsoft.Identity.Client;
using Microsoft.Maui.Devices;  // For DeviceInfo
using Microsoft.Maui.Storage;  // For FileSystem
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Maui.ApplicationModel.Platform;  // For Platform.CurrentActivity on Android

namespace AMS.Services
{
    public interface IDatabaseSyncService
    {
        Task UploadDatabaseAsync(string fileName, CancellationToken cancellationToken = default);
        Task DownloadDatabaseAsync(string fileName, CancellationToken cancellationToken = default);
    }

    public class DatabaseSyncService : IDatabaseSyncService
    {
        private readonly string _localDbPath;
        private readonly IPublicClientApplication _pca;
        private readonly IServiceScopeFactory _scopeFactory;  // Add for DbContext scope
        private readonly string[] _scopes = { "Files.ReadWrite.AppFolder" };

        private const string ClientId = "5a97c8f3-02ca-4908-9d2f-19a323910b0d";
        private const string Tenant = "consumers";  // Keep as-is for personal

        public DatabaseSyncService(IServiceScopeFactory scopeFactory)  // Inject factory
        {
            _scopeFactory = scopeFactory;
            _localDbPath = Path.Combine(FileSystem.AppDataDirectory, "ams.db");

            string redirectUri;
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                redirectUri = $"msal{ClientId}://auth";
            }
            else
            {
                redirectUri = "http://localhost";
            }

            _pca = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount)
                .WithRedirectUri(redirectUri)
                .Build();
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            AuthenticationResult result;
            try
            {
                var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
                result = await _pca.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                // Interactive fallback with parent Activity for Android
                var builder = _pca.AcquireTokenInteractive(_scopes);

#if ANDROID
        builder = builder.WithParentActivityOrWindow(Platform.CurrentActivity);
#endif

                result = await builder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            return result.AccessToken;
        }

        private HttpClient CreateHttpClient(string accessToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        // Upload (create or replace) file to /me/drive/special/approot:/{fileName}:/content
        public async Task UploadDatabaseAsync(string fileName, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_localDbPath))
                throw new FileNotFoundException("Local DB not found.", _localDbPath);

            // Copy DB to temp file to avoid lock (safe, fast for small DBs)
            string tempPath = Path.GetTempFileName() + ".db";  // e.g., C:\Temp\tmp123.db
            try
            {
                File.Copy(_localDbPath, tempPath, overwrite: true);  // Copies unlocked
                System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Copied DB to temp: {tempPath}");

                var token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                using var client = CreateHttpClient(token);

                var url = $"https://graph.microsoft.com/v1.0/me/drive/special/approot:/{Uri.EscapeDataString(fileName)}:/content";

                await using var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var content = new StreamContent(fs);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                using var req = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };

                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Upload failed ({(int)resp.StatusCode}): {body}");
                    resp.EnsureSuccessStatusCode();
                }

                System.Diagnostics.Debug.WriteLine("[DatabaseSync] Upload succeeded.");
            }
            finally
            {
                // Clean up temp
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                    System.Diagnostics.Debug.WriteLine("[DatabaseSync] Temp file deleted.");
                }
            }
        }

        // replace or update the DownloadDatabaseAsync method in your DatabaseSyncService
        public async Task DownloadDatabaseAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            using var client = CreateHttpClient(token);

            var url = $"https://graph.microsoft.com/v1.0/me/drive/special/approot:/{Uri.EscapeDataString(fileName)}:/content";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Download failed ({(int)resp.StatusCode}): {body}");
                resp.EnsureSuccessStatusCode();
            }

            // Save directly as ams_download.db (no .tmp)
            string downloadPath = Path.Combine(FileSystem.AppDataDirectory, "ams_download.db");

            // Delete any previous download file to avoid stale data (best-effort)
            try
            {
                if (File.Exists(downloadPath))
                    File.Delete(downloadPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Could not delete previous download file: {ex.Message}");
                // continue - we'll still write a new file (CreateNew will fail if still exists)
            }

            // Write response to downloadPath (use Create to overwrite if any race)
            await using (var responseStream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var outFs = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await responseStream.CopyToAsync(outFs, 81920, cancellationToken).ConfigureAwait(false);
                await outFs.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Downloaded file saved to: {downloadPath}");

            // Mark that a restore is pending; on next app start we'll swap this file into place
            Preferences.Set("db_restore_pending_path", downloadPath);
            Preferences.Set("db_restore_pending_filename", fileName);
            System.Diagnostics.Debug.WriteLine("[DatabaseSync] Restore pending flag set. App restart will apply the downloaded DB.");
        }
    }
}