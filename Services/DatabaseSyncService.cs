using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly string[] _scopes = { "Files.ReadWrite.AppFolder" }; // app folder scope

        // TODO: replace with your AAD app registration details
        private const string ClientId = "YOUR_CLIENT_ID";
        private const string Tenant = "common";

        public DatabaseSyncService()
        {
            _localDbPath = Path.Combine(FileSystem.AppDataDirectory, "ams.db");

            _pca = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, Tenant)
                .WithRedirectUri($"msal{ClientId}://auth")
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
                // Interactive fallback
                result = await _pca.AcquireTokenInteractive(_scopes)
                    // .WithParentActivityOrWindow(...) // optionally supply platform-specific parent
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
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

            var token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            using var client = CreateHttpClient(token);

            // Graph upload URL for a file in the app's appFolder:
            var url = $"https://graph.microsoft.com/v1.0/me/drive/special/approot:/{Uri.EscapeDataString(fileName)}:/content";

            await using var fs = new FileStream(_localDbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var content = new StreamContent(fs);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var req = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Upload failed ({(int)resp.StatusCode}): {body}");
                resp.EnsureSuccessStatusCode(); // will throw with status code info
            }

            System.Diagnostics.Debug.WriteLine("[DatabaseSync] Upload succeeded.");
        }

        // Download file from /me/drive/special/approot:/{fileName}:/content
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

            // Backup local DB
            if (File.Exists(_localDbPath))
            {
                try
                {
                    File.Copy(_localDbPath, _localDbPath + ".backup", overwrite: true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Backup failed: {ex.Message}");
                    // continue - we still attempt to write the downloaded file
                }
            }

            await using var responseStream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var outFs = new FileStream(_localDbPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await responseStream.CopyToAsync(outFs, 81920, cancellationToken).ConfigureAwait(false);
            await outFs.FlushAsync(cancellationToken).ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine("[DatabaseSync] Download and write succeeded.");
        }
    }
}