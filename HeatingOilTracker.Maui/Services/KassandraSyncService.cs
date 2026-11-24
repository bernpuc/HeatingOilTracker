using System.Net.Http.Json;
using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;

namespace HeatingOilTracker.Maui.Services;

/// <summary>
/// Syncs TrackerData with HeatingOilTracker.Server running on kassandra, reached over
/// Tailscale. There's no app-level authentication — tailnet membership is the access control
/// (see KASSANDRA_SYNC_PLAN.md) — so this implementation repurposes the ISyncService
/// sign-in surface rather than leaving it meaningless:
///   - IsConfigured is always true (the base URL is a compile-time constant, not user config).
///   - IsSignedIn/AccountEmail/SignInAsync/SignOutAsync become a lightweight reachability
///     check instead of real authentication. SignInAsync pings the server and, on success,
///     also reports "signed in"; AccountEmail is repurposed to show the server host instead of
///     an email address. SignOutAsync just flips IsSignedIn to false locally (nothing to revoke
///     server-side) so the Settings page's "Disconnect" affordance still does something sane.
/// This changes what the Settings page's sync-status section displays (e.g. "Connected as
/// 100.x.x.x:8086" instead of "Connected as user@gmail.com") — flagged in the implementation
/// report since the plan didn't require touching that UI directly.
/// </summary>
public class KassandraSyncService : ISyncService
{
    private readonly HttpClient _http;

    public bool IsConfigured => true;
    public bool IsSignedIn { get; private set; }
    public string? AccountEmail { get; private set; }
    public DateTime? LastSyncAt { get; private set; }

    public KassandraSyncService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(Secrets.KassandraSyncBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
        AccountEmail = new Uri(Secrets.KassandraSyncBaseUrl).Authority;
    }

    public async Task<bool> SignInAsync()
    {
        IsSignedIn = await CheckReachableAsync();
        return IsSignedIn;
    }

    public Task SignOutAsync()
    {
        IsSignedIn = false;
        return Task.CompletedTask;
    }

    private async Task<bool> CheckReachableAsync()
    {
        try
        {
            using var response = await _http.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] Reachability check failed: {ex.Message}");
            return false;
        }
    }

    public async Task<SyncResult> SyncOnStartupAsync(TrackerData localData)
    {
        try
        {
            var payload = SyncPayload.FromTrackerData(localData);
            using var response = await _http.PutAsJsonAsync("/api/tracker", payload);

            if (!response.IsSuccessStatusCode)
                return new SyncResult(localData, SyncStatus.Error, LastSyncAt, $"Server returned {response.StatusCode}");

            var mergedPayload = await response.Content.ReadFromJsonAsync<SyncPayload>();
            if (mergedPayload is null)
                return new SyncResult(localData, SyncStatus.Error, LastSyncAt, "Failed to parse server response");

            var merged = mergedPayload.ToTrackerData();
            // WeatherHistory and BackupFolderPath never travel over the wire — keep local's.
            merged.WeatherHistory = localData.WeatherHistory;
            merged.BackupFolderPath = localData.BackupFolderPath;

            IsSignedIn = true;
            LastSyncAt = DateTime.UtcNow;
            return new SyncResult(merged, SyncStatus.Success, LastSyncAt);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] SyncOnStartup network error: {ex.Message}");
            IsSignedIn = false;
            return new SyncResult(localData, SyncStatus.Offline, LastSyncAt);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] SyncOnStartup error: {ex.Message}");
            return new SyncResult(localData, SyncStatus.Error, LastSyncAt, ex.Message);
        }
    }

    public async Task PushAsync(TrackerData data)
    {
        try
        {
            var payload = SyncPayload.FromTrackerData(data);
            using var response = await _http.PutAsJsonAsync("/api/tracker", payload);
            if (response.IsSuccessStatusCode)
            {
                IsSignedIn = true;
                LastSyncAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            // Never throw to callers — sync is best-effort.
            System.Diagnostics.Debug.WriteLine($"[Sync] Push failed: {ex.Message}");
        }
    }
}
