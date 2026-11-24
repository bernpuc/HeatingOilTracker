using System.Text.Json;
using HeatingOilTracker.Core.Models;

namespace HeatingOilTracker.Server;

/// <summary>
/// Single-document JSON store for the server's authoritative TrackerData copy.
/// Serializes access with a semaphore since PUT performs a read-merge-write and two
/// concurrent requests from the two devices must not interleave.
/// </summary>
public class TrackerStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TrackerStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<T> WithLockAsync<T>(Func<TrackerData?, Task<T>> action)
    {
        await _lock.WaitAsync();
        try
        {
            var current = await LoadAsync();
            return await action(current);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<TrackerData?> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<TrackerData>(json, JsonOptions);
    }

    public async Task SaveAsync(TrackerData data)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(data, JsonOptions);

        // Atomic write: write to temp file then rename, same pattern as the client's DataService.
        var tempPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json);

        if (File.Exists(_filePath))
            File.Delete(_filePath);

        File.Move(tempPath, _filePath);
    }
}
