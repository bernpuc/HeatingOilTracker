using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;
using HeatingOilTracker.Core.Services;
using Core = HeatingOilTracker.Core;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
#if WINDOWS
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
#endif

namespace HeatingOilTracker.Maui.Services;

/// <summary>
/// Syncs TrackerData to/from Google Drive's App Data folder via OAuth2 PKCE.
///
/// Setup prerequisites:
///   1. Create a Google Cloud project, enable Drive API.
///   2. Create OAuth 2.0 credentials (type: Web application).
///   3. Add authorized redirect URI: com.bernpuc.heatingoiltracker:/oauth2redirect
///   4. Replace GOOGLE_CLIENT_ID below with your actual client ID.
/// </summary>
public class GoogleDriveSyncService : ISyncService
{
    // Android OAuth client (type: Android in Google Cloud Console)
    private const string GoogleClientIdAndroid = Secrets.GoogleClientIdAndroid;
    private const string RedirectUriAndroid = "com.bernpuc.heatingoiltracker:/oauth2redirect";

    // Desktop OAuth client (type: Desktop app in Google Cloud Console)
    private const string GoogleClientIdWindows = Secrets.GoogleClientIdWindows;
    private const string GoogleClientSecretWindows = Secrets.GoogleClientSecretWindows;

#if WINDOWS
    private static string CurrentClientId => GoogleClientIdWindows;
#else
    private static string CurrentClientId => GoogleClientIdAndroid;
#endif

    private const string Scopes = "https://www.googleapis.com/auth/drive.appdata";
    private const string SyncFileName = "heatingoiltracker_sync.json";

    private const string KeyAccessToken = "gdrive_access_token";
    private const string KeyRefreshToken = "gdrive_refresh_token";
    private const string KeyAccountEmail = "gdrive_account_email";
    private const string KeyLastSyncAt = "gdrive_last_sync_at";
    private const string KeyTokenExpiry = "gdrive_token_expiry";

    private readonly HttpClient _http;
    private readonly SyncMergeService _mergeService;

    // Set true during SyncOnStartupAsync so SaveAsync (triggered via PushAsync) is skipped
    private bool _startupSyncInProgress;

    public bool IsConfigured => true;
    public bool IsSignedIn { get; private set; }
    public string? AccountEmail { get; private set; }
    public DateTime? LastSyncAt { get; private set; }

    public GoogleDriveSyncService()
    {
        _http = new HttpClient();
        _mergeService = new SyncMergeService();
        _ = LoadStoredStateAsync();
    }

    private async Task LoadStoredStateAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(KeyAccessToken);
            IsSignedIn = !string.IsNullOrEmpty(token);
            AccountEmail = await SecureStorage.Default.GetAsync(KeyAccountEmail);
            var lastSync = await SecureStorage.Default.GetAsync(KeyLastSyncAt);
            if (DateTime.TryParse(lastSync, out var dt))
                LastSyncAt = dt;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] LoadStoredState failed: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Sign-in / Sign-out
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<bool> SignInAsync()
    {
        try
        {
#if WINDOWS
            return await SignInWindowsAsync();
#else
            return await SignInAndroidAsync();
#endif
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] SignIn failed: {ex.Message}");
            return false;
        }
    }

#if WINDOWS
    private async Task<bool> SignInWindowsAsync()
    {
        // Pick a random free loopback port
        int port;
        using (var tmp = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0))
        {
            tmp.Start();
            port = ((IPEndPoint)tmp.LocalEndpoint).Port;
        }
        var redirectUri = $"http://127.0.0.1:{port}/";

        // Generate PKCE
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Start listener before opening browser so it's ready for the callback
        var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        // Open browser
        var authUrl = BuildAuthUrl(GoogleClientIdWindows, redirectUri, codeChallenge);
        Process.Start(new ProcessStartInfo(authUrl.ToString()) { UseShellExecute = true });

        HttpListenerContext context;
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            context = await listener.GetContextAsync().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            listener.Stop();
            return false;
        }

        // Send close-tab response before stopping the listener
        var html = Encoding.UTF8.GetBytes("<html><body><h2>Sign-in complete. You can close this tab.</h2></body></html>");
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = html.Length;
        await context.Response.OutputStream.WriteAsync(html);
        context.Response.Close();
        listener.Stop();

        var code = HttpUtility.ParseQueryString(context.Request.Url!.Query)["code"];
        if (string.IsNullOrEmpty(code)) return false;

        var tokens = await ExchangeCodeForTokensAsync(code, codeVerifier, GoogleClientIdWindows, redirectUri, GoogleClientSecretWindows);
        if (tokens?.AccessToken is null) return false;

        await StoreTokensAsync(tokens);
        AccountEmail = await FetchAccountEmailAsync(tokens.AccessToken);
        if (AccountEmail != null)
            await SecureStorage.Default.SetAsync(KeyAccountEmail, AccountEmail);

        IsSignedIn = true;
        return true;
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
#else
    private async Task<bool> SignInAndroidAsync()
    {
        var authUrl = BuildAuthUrl(GoogleClientIdAndroid, RedirectUriAndroid, codeChallenge: null);
        var callbackUrl = new Uri(RedirectUriAndroid);

        var result = await WebAuthenticator.Default.AuthenticateAsync(
            new WebAuthenticatorOptions { Url = authUrl, CallbackUrl = callbackUrl });

        if (result is null) return false;

        string code = result.Properties.TryGetValue("code", out var c) ? c : result.AccessToken ?? string.Empty;
        string codeVerifier = result.Properties.TryGetValue("code_verifier", out var cv) ? cv : string.Empty;

        var tokens = await ExchangeCodeForTokensAsync(code, codeVerifier, GoogleClientIdAndroid, RedirectUriAndroid);
        if (tokens?.AccessToken is null) return false;

        await StoreTokensAsync(tokens);
        AccountEmail = await FetchAccountEmailAsync(tokens.AccessToken);
        if (AccountEmail != null)
            await SecureStorage.Default.SetAsync(KeyAccountEmail, AccountEmail);

        IsSignedIn = true;
        return true;
    }
#endif

    public async Task SignOutAsync()
    {
        try
        {
            SecureStorage.Default.Remove(KeyAccessToken);
            SecureStorage.Default.Remove(KeyRefreshToken);
            SecureStorage.Default.Remove(KeyAccountEmail);
            SecureStorage.Default.Remove(KeyLastSyncAt);
            SecureStorage.Default.Remove(KeyTokenExpiry);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] SignOut cleanup failed: {ex.Message}");
        }

        IsSignedIn = false;
        AccountEmail = null;
        LastSyncAt = null;
        await Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Sync operations
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<SyncResult> SyncOnStartupAsync(TrackerData localData)
    {
        if (!IsConfigured)
            return new SyncResult(localData, SyncStatus.NotConfigured, LastSyncAt);

        // Do NOT short-circuit on the IsSignedIn field here — it is populated by a
        // fire-and-forget LoadStoredStateAsync() call and may not be set yet when
        // SyncOnStartupAsync is called on startup.  GetValidAccessTokenAsync() reads
        // SecureStorage directly and is the authoritative check.

        _startupSyncInProgress = true;
        try
        {
            var token = await GetValidAccessTokenAsync();
            if (token is null)
                return new SyncResult(localData, SyncStatus.NotSignedIn, LastSyncAt);

            var remoteJson = await DownloadSyncFileAsync(token);
            if (remoteJson is null)
            {
                // No remote file yet — push local data up
                await UploadSyncFileAsync(token, localData);
                LastSyncAt = DateTime.UtcNow;
                await PersistLastSyncAtAsync();
                return new SyncResult(localData, SyncStatus.Success, LastSyncAt);
            }

            var remoteData = DeserializeSyncPayload(remoteJson);
            if (remoteData is null)
                return new SyncResult(localData, SyncStatus.Error, LastSyncAt, "Failed to parse remote data");

            var merged = _mergeService.Merge(localData, remoteData);

            // Push the merged result back to Drive
            await UploadSyncFileAsync(token, merged);
            LastSyncAt = DateTime.UtcNow;
            await PersistLastSyncAtAsync();

            return new SyncResult(merged, SyncStatus.Success, LastSyncAt);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] SyncOnStartup network error: {ex.Message}");
            return new SyncResult(localData, SyncStatus.Offline, LastSyncAt);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] SyncOnStartup error: {ex.Message}");
            return new SyncResult(localData, SyncStatus.Error, LastSyncAt, ex.Message);
        }
        finally
        {
            _startupSyncInProgress = false;
        }
    }

    public async Task PushAsync(TrackerData data)
    {
        if (_startupSyncInProgress) return;
        if (!IsConfigured || !IsSignedIn) return;

        try
        {
            var token = await GetValidAccessTokenAsync();
            if (token is null) return;
            await UploadSyncFileAsync(token, data);
            LastSyncAt = DateTime.UtcNow;
            await PersistLastSyncAtAsync();
        }
        catch (Exception ex)
        {
            // Never throw to callers — sync is best-effort
            System.Diagnostics.Debug.WriteLine($"[Sync] Push failed: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Drive API helpers
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<string?> DownloadSyncFileAsync(string token)
    {
        // Find the file ID
        var fileId = await GetSyncFileIdAsync(token);
        if (fileId is null) return null;

        // Download content
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string?> GetSyncFileIdAsync(string token)
    {
        var query = HttpUtility.UrlEncode($"name='{SyncFileName}'");
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://www.googleapis.com/drive/v3/files?spaces=appDataFolder&q={query}&fields=files(id)");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("files");
        if (files.GetArrayLength() == 0) return null;

        return files[0].GetProperty("id").GetString();
    }

    private async Task UploadSyncFileAsync(string token, TrackerData data)
    {
        var payload = SerializeSyncPayload(data);
        var fileId = await GetSyncFileIdAsync(token);

        if (fileId is null)
        {
            // Create new file
            await CreateSyncFileAsync(token, payload);
        }
        else
        {
            // Update existing file
            using var request = new HttpRequestMessage(HttpMethod.Patch,
                $"https://www.googleapis.com/upload/drive/v3/files/{fileId}?uploadType=media");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }

    private async Task CreateSyncFileAsync(string token, string payload)
    {
        // Multipart upload: metadata part + media part
        var boundary = Guid.NewGuid().ToString("N");
        var metadata = JsonSerializer.Serialize(new
        {
            name = SyncFileName,
            parents = new[] { "appDataFolder" }
        });

        var multipartContent = new MultipartContent("related", boundary);
        var metaPart = new StringContent(metadata, Encoding.UTF8, "application/json");
        var mediaPart = new StringContent(payload, Encoding.UTF8, "application/json");

        multipartContent.Add(metaPart);
        multipartContent.Add(mediaPart);

        using var request = new HttpRequestMessage(HttpMethod.Post,
            "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = multipartContent;

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Token management
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<string?> GetValidAccessTokenAsync()
    {
        var accessToken = await SecureStorage.Default.GetAsync(KeyAccessToken);
        if (!string.IsNullOrEmpty(accessToken))
        {
            // Only use the stored token if it hasn't expired (with a 5-minute buffer).
            var expiryStr = await SecureStorage.Default.GetAsync(KeyTokenExpiry);
            if (DateTime.TryParse(expiryStr, out var expiry) && DateTime.UtcNow < expiry)
                return accessToken;
        }

        // Token absent, expired, or no expiry stored — refresh.
        return await RefreshAccessTokenAsync();
    }

    private async Task<string?> RefreshAccessTokenAsync()
    {
        var refreshToken = await SecureStorage.Default.GetAsync(KeyRefreshToken);
        if (string.IsNullOrEmpty(refreshToken)) return null;

        try
        {
            var fields = new Dictionary<string, string>
            {
                ["client_id"] = CurrentClientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            };
#if WINDOWS
            fields["client_secret"] = GoogleClientSecretWindows;
#endif
            var body = new FormUrlEncodedContent(fields);

            using var response = await _http.PostAsync("https://oauth2.googleapis.com/token", body);
            if (!response.IsSuccessStatusCode)
            {
                // Refresh token invalid — sign out
                await SignOutAsync();
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, JsonSerializerOptions.Default);
            if (tokenResponse?.AccessToken is null) return null;

            await SecureStorage.Default.SetAsync(KeyAccessToken, tokenResponse.AccessToken);
            var expiry = DateTime.UtcNow.AddSeconds(Math.Max(0, tokenResponse.ExpiresIn - 300));
            await SecureStorage.Default.SetAsync(KeyTokenExpiry, expiry.ToString("O"));
            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] Token refresh failed: {ex.Message}");
            return null;
        }
    }

    private async Task StoreTokensAsync(TokenResponse tokens)
    {
        await SecureStorage.Default.SetAsync(KeyAccessToken, tokens.AccessToken ?? string.Empty);
        if (!string.IsNullOrEmpty(tokens.RefreshToken))
            await SecureStorage.Default.SetAsync(KeyRefreshToken, tokens.RefreshToken);
        // Store expiry with a 5-minute buffer so we refresh before the token actually expires.
        var expiry = DateTime.UtcNow.AddSeconds(Math.Max(0, tokens.ExpiresIn - 300));
        await SecureStorage.Default.SetAsync(KeyTokenExpiry, expiry.ToString("O"));
    }

    private async Task<TokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier, string clientId, string redirectUri, string? clientSecret = null)
    {
        var fields = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        };
        if (clientSecret != null)
            fields["client_secret"] = clientSecret;
        var body = new FormUrlEncodedContent(fields);

        using var response = await _http.PostAsync("https://oauth2.googleapis.com/token", body);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] Token exchange failed {response.StatusCode}: {json}");
            return null;
        }

        return JsonSerializer.Deserialize<TokenResponse>(json, JsonSerializerOptions.Default);
    }

    private async Task<string?> FetchAccountEmailAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://www.googleapis.com/oauth2/v3/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("email", out var emailProp)
            ? emailProp.GetString()
            : null;
    }

    private async Task PersistLastSyncAtAsync()
    {
        await SecureStorage.Default.SetAsync(KeyLastSyncAt, LastSyncAt?.ToString("O") ?? string.Empty);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // OAuth URL builder
    // ──────────────────────────────────────────────────────────────────────────

    private static Uri BuildAuthUrl(string clientId, string redirectUri, string? codeChallenge)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = clientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = Scopes + " email";
        query["access_type"] = "offline";
        query["prompt"] = "consent";
        if (codeChallenge != null)
        {
            query["code_challenge"] = codeChallenge;
            query["code_challenge_method"] = "S256";
        }

        return new Uri($"https://accounts.google.com/o/oauth2/v2/auth?{query}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Serialization — WeatherHistory excluded from sync payload
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions SyncJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static string SerializeSyncPayload(TrackerData data)
    {
        // Create a payload that excludes WeatherHistory (device-local) and BackupFolderPath
        var payload = new SyncPayload
        {
            TankCapacityGallons = data.TankCapacityGallons,
            Deliveries = data.Deliveries,
            Location = data.Location,
            ReminderSettings = data.ReminderSettings,
            RegionalSettings = data.RegionalSettings,
            SettingsModifiedAt = data.SettingsModifiedAt
        };
        return JsonSerializer.Serialize(payload, SyncJsonOptions);
    }

    private static TrackerData? DeserializeSyncPayload(string json)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<SyncPayload>(json, SyncJsonOptions);
            if (payload is null) return null;

            return new TrackerData
            {
                TankCapacityGallons = payload.TankCapacityGallons,
                Deliveries = payload.Deliveries ?? [],
                Location = payload.Location ?? new Core.Models.Location(),
                ReminderSettings = payload.ReminderSettings ?? new ReminderSettings(),
                RegionalSettings = payload.RegionalSettings ?? new RegionalSettings(),
                SettingsModifiedAt = payload.SettingsModifiedAt,
                // WeatherHistory and BackupFolderPath are intentionally left empty —
                // they'll be filled in by the caller from local data via SyncMergeService.
                WeatherHistory = [],
                BackupFolderPath = null
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] Deserialize failed: {ex.Message}");
            return null;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private DTOs
    // ──────────────────────────────────────────────────────────────────────────

    private class SyncPayload
    {
        public decimal TankCapacityGallons { get; set; } = 275m;
        public List<OilDelivery>? Deliveries { get; set; }
        public Core.Models.Location? Location { get; set; }
        public ReminderSettings? ReminderSettings { get; set; }
        public RegionalSettings? RegionalSettings { get; set; }
        public DateTime SettingsModifiedAt { get; set; }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
