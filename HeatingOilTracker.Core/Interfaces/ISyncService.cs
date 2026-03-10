using HeatingOilTracker.Core.Models;

namespace HeatingOilTracker.Core.Interfaces;

public interface ISyncService
{
    Task<SyncResult> SyncOnStartupAsync(TrackerData localData);
    Task PushAsync(TrackerData data);
    Task<bool> SignInAsync();
    Task SignOutAsync();
    bool IsConfigured { get; }
    bool IsSignedIn { get; }
    string? AccountEmail { get; }
    DateTime? LastSyncAt { get; }
}

public record SyncResult(TrackerData MergedData, SyncStatus Status,
    DateTime? LastSyncAt, string? ErrorMessage = null);

public enum SyncStatus { Success, Offline, NotConfigured, NotSignedIn, Error }
