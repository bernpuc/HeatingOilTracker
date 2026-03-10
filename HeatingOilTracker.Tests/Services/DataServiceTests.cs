using FluentAssertions;
using HeatingOilTracker.Core.Models;
using HeatingOilTracker.Core.Services;
using Xunit;

namespace HeatingOilTracker.Tests.Services;

/// <summary>
/// Tests DataService using a real temp directory — no mocks needed since
/// DataService depends only on the filesystem.
/// </summary>
public class DataServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DataService _sut;

    public DataServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DataServiceTests_" + Guid.NewGuid().ToString("N"));
        _sut = new DataService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    #region GetDataFilePath / InvalidateCache

    [Fact]
    public void GetDataFilePath_ReturnsDataJsonInDirectory()
    {
        var path = _sut.GetDataFilePath();

        path.Should().Be(Path.Combine(_tempDir, "data.json"));
    }

    [Fact]
    public async Task InvalidateCache_ForcesReloadFromDisk()
    {
        // Load once to populate cache
        var data1 = await _sut.LoadAsync();
        data1.TankCapacityGallons = 300m;
        await _sut.SaveAsync(data1);

        // Directly modify the file behind the service's back
        var json = await File.ReadAllTextAsync(_sut.GetDataFilePath());
        json = json.Replace("300", "400");
        await File.WriteAllTextAsync(_sut.GetDataFilePath(), json);

        // Without invalidation, cache returns stale value
        var cached = await _sut.LoadAsync();
        cached.TankCapacityGallons.Should().Be(300m);

        // After invalidation, fresh data is loaded
        _sut.InvalidateCache();
        var fresh = await _sut.LoadAsync();
        fresh.TankCapacityGallons.Should().Be(400m);
    }

    #endregion

    #region LoadAsync

    [Fact]
    public async Task LoadAsync_NoFile_ReturnsEmptyTrackerData()
    {
        var result = await _sut.LoadAsync();

        result.Should().NotBeNull();
        result.Deliveries.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ExistingFile_DeserializesData()
    {
        // Arrange: write a known delivery to disk
        var delivery = new OilDelivery
        {
            Id = Guid.NewGuid(),
            Date = new DateTime(2024, 1, 15),
            Gallons = 150m,
            PricePerGallon = 3.99m
        };
        var data = new TrackerData();
        data.Deliveries.Add(delivery);
        await _sut.SaveAsync(data);
        _sut.InvalidateCache();

        var result = await _sut.LoadAsync();

        result.Deliveries.Should().HaveCount(1);
        result.Deliveries[0].Gallons.Should().Be(150m);
        result.Deliveries[0].PricePerGallon.Should().Be(3.99m);
    }

    [Fact]
    public async Task LoadAsync_CachesResult_ReturnsSameInstance()
    {
        var result1 = await _sut.LoadAsync();
        var result2 = await _sut.LoadAsync();

        result1.Should().BeSameAs(result2);
    }

    #endregion

    #region SaveAsync

    [Fact]
    public async Task SaveAsync_CreatesDirectoryIfNotExists()
    {
        Directory.Exists(_tempDir).Should().BeFalse();

        await _sut.SaveAsync(new TrackerData());

        Directory.Exists(_tempDir).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_WritesJsonFileToDisk()
    {
        var data = new TrackerData { TankCapacityGallons = 500m };

        await _sut.SaveAsync(data);

        File.Exists(_sut.GetDataFilePath()).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_RoundTrips_DataCorrectly()
    {
        var delivery = new OilDelivery
        {
            Id = Guid.NewGuid(),
            Date = new DateTime(2024, 6, 1),
            Gallons = 123.5m,
            PricePerGallon = 4.25m,
            Notes = "Test notes"
        };
        var data = new TrackerData { TankCapacityGallons = 275m };
        data.Deliveries.Add(delivery);

        await _sut.SaveAsync(data);
        _sut.InvalidateCache();
        var loaded = await _sut.LoadAsync();

        loaded.TankCapacityGallons.Should().Be(275m);
        loaded.Deliveries.Should().HaveCount(1);
        loaded.Deliveries[0].Gallons.Should().Be(123.5m);
        loaded.Deliveries[0].Notes.Should().Be("Test notes");
    }

    [Fact]
    public async Task SaveAsync_NoTempFileLeftBehind()
    {
        await _sut.SaveAsync(new TrackerData());

        var tempFile = _sut.GetDataFilePath() + ".tmp";
        File.Exists(tempFile).Should().BeFalse();
    }

    #endregion

    #region GetDeliveriesAsync

    [Fact]
    public async Task GetDeliveriesAsync_EmptyData_ReturnsEmpty()
    {
        var result = await _sut.GetDeliveriesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDeliveriesAsync_FiltersDeletedDeliveries()
    {
        var active = new OilDelivery { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3m };
        var deleted = new OilDelivery { Id = Guid.NewGuid(), Date = new DateTime(2024, 2, 1), Gallons = 120m, PricePerGallon = 3m, IsDeleted = true };
        var data = new TrackerData();
        data.Deliveries.AddRange([active, deleted]);
        await _sut.SaveAsync(data);

        var result = await _sut.GetDeliveriesAsync();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(active.Id);
    }

    [Fact]
    public async Task GetDeliveriesAsync_OrdersByDateDescending()
    {
        var data = new TrackerData();
        data.Deliveries.Add(new OilDelivery { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3m });
        data.Deliveries.Add(new OilDelivery { Id = Guid.NewGuid(), Date = new DateTime(2024, 3, 1), Gallons = 110m, PricePerGallon = 3m });
        data.Deliveries.Add(new OilDelivery { Id = Guid.NewGuid(), Date = new DateTime(2024, 2, 1), Gallons = 120m, PricePerGallon = 3m });
        await _sut.SaveAsync(data);

        var result = await _sut.GetDeliveriesAsync();

        result[0].Date.Should().Be(new DateTime(2024, 3, 1));
        result[1].Date.Should().Be(new DateTime(2024, 2, 1));
        result[2].Date.Should().Be(new DateTime(2024, 1, 1));
    }

    #endregion

    #region AddDeliveryAsync

    [Fact]
    public async Task AddDeliveryAsync_AddsDeliveryAndPersists()
    {
        var delivery = new OilDelivery
        {
            Id = Guid.NewGuid(),
            Date = new DateTime(2024, 5, 10),
            Gallons = 90m,
            PricePerGallon = 3.75m
        };

        await _sut.AddDeliveryAsync(delivery);
        _sut.InvalidateCache();

        var deliveries = await _sut.GetDeliveriesAsync();
        deliveries.Should().HaveCount(1);
        deliveries[0].Gallons.Should().Be(90m);
    }

    #endregion

    #region UpdateDeliveryAsync

    [Fact]
    public async Task UpdateDeliveryAsync_UpdatesExistingDelivery()
    {
        var id = Guid.NewGuid();
        await _sut.AddDeliveryAsync(new OilDelivery { Id = id, Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3m });

        var updated = new OilDelivery { Id = id, Date = new DateTime(2024, 1, 1), Gallons = 150m, PricePerGallon = 3.5m };
        await _sut.UpdateDeliveryAsync(updated);
        _sut.InvalidateCache();

        var deliveries = await _sut.GetDeliveriesAsync();
        deliveries.Should().HaveCount(1);
        deliveries[0].Gallons.Should().Be(150m);
    }

    [Fact]
    public async Task UpdateDeliveryAsync_SetsModifiedAt()
    {
        var id = Guid.NewGuid();
        await _sut.AddDeliveryAsync(new OilDelivery { Id = id, Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3m });

        var before = DateTime.UtcNow.AddSeconds(-1);
        await _sut.UpdateDeliveryAsync(new OilDelivery { Id = id, Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3m });
        _sut.InvalidateCache();

        var data = await _sut.LoadAsync();
        data.Deliveries[0].ModifiedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateDeliveryAsync_NonexistentId_DoesNothing()
    {
        await _sut.AddDeliveryAsync(new OilDelivery { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3m });

        // Should not throw
        await _sut.UpdateDeliveryAsync(new OilDelivery { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 1), Gallons = 999m, PricePerGallon = 3m });
        _sut.InvalidateCache();

        var deliveries = await _sut.GetDeliveriesAsync();
        deliveries.Should().HaveCount(1);
        deliveries[0].Gallons.Should().Be(100m);
    }

    #endregion

    #region DeleteDeliveryAsync

    [Fact]
    public async Task DeleteDeliveryAsync_SoftDeletesDelivery()
    {
        var id = Guid.NewGuid();
        await _sut.AddDeliveryAsync(new OilDelivery { Id = id, Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3m });

        await _sut.DeleteDeliveryAsync(id);
        _sut.InvalidateCache();

        // GetDeliveries should filter it out
        var visible = await _sut.GetDeliveriesAsync();
        visible.Should().BeEmpty();

        // But it remains in raw data with IsDeleted = true
        var data = await _sut.LoadAsync();
        data.Deliveries.Should().HaveCount(1);
        data.Deliveries[0].IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDeliveryAsync_SetsModifiedAt()
    {
        var id = Guid.NewGuid();
        await _sut.AddDeliveryAsync(new OilDelivery { Id = id, Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3m });

        var before = DateTime.UtcNow.AddSeconds(-1);
        await _sut.DeleteDeliveryAsync(id);
        _sut.InvalidateCache();

        var data = await _sut.LoadAsync();
        data.Deliveries[0].ModifiedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task DeleteDeliveryAsync_NonexistentId_DoesNothing()
    {
        await _sut.AddDeliveryAsync(new OilDelivery { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3m });

        // Should not throw
        await _sut.DeleteDeliveryAsync(Guid.NewGuid());
        _sut.InvalidateCache();

        var deliveries = await _sut.GetDeliveriesAsync();
        deliveries.Should().HaveCount(1);
    }

    #endregion

    #region TankCapacity

    [Fact]
    public async Task GetTankCapacityAsync_DefaultValue_ReturnsDefault()
    {
        var result = await _sut.GetTankCapacityAsync();

        result.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public async Task SetTankCapacityAsync_PersistsValue()
    {
        await _sut.SetTankCapacityAsync(330m);
        _sut.InvalidateCache();

        var result = await _sut.GetTankCapacityAsync();
        result.Should().Be(330m);
    }

    [Fact]
    public async Task SetTankCapacityAsync_SetsSettingsModifiedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        await _sut.SetTankCapacityAsync(275m);
        _sut.InvalidateCache();

        var data = await _sut.LoadAsync();
        data.SettingsModifiedAt.Should().BeAfter(before);
    }

    #endregion

    #region Location

    [Fact]
    public async Task SetLocationAsync_PersistsLocation()
    {
        var location = new Location
        {
            Latitude = 42.3m,
            Longitude = -71.0m,
            DisplayName = "Boston, MA, US"
        };

        await _sut.SetLocationAsync(location);
        _sut.InvalidateCache();

        var result = await _sut.GetLocationAsync();
        result.Latitude.Should().Be(42.3m);
        result.Longitude.Should().Be(-71.0m);
        result.DisplayName.Should().Be("Boston, MA, US");
    }

    [Fact]
    public async Task SetLocationAsync_SetsSettingsModifiedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        await _sut.SetLocationAsync(new Location());
        _sut.InvalidateCache();

        var data = await _sut.LoadAsync();
        data.SettingsModifiedAt.Should().BeAfter(before);
    }

    #endregion

    #region WeatherHistory / AddWeatherData

    [Fact]
    public async Task AddWeatherDataAsync_AddsNewEntries()
    {
        var weatherData = new List<DailyWeather>
        {
            new() { Date = new DateTime(2024, 1, 1), HighTempF = 40m, LowTempF = 25m },
            new() { Date = new DateTime(2024, 1, 2), HighTempF = 38m, LowTempF = 22m }
        };

        await _sut.AddWeatherDataAsync(weatherData);
        _sut.InvalidateCache();

        var history = await _sut.GetWeatherHistoryAsync();
        history.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddWeatherDataAsync_DeduplicatesByDate()
    {
        var day1 = new DailyWeather { Date = new DateTime(2024, 1, 1), HighTempF = 40m, LowTempF = 25m };
        await _sut.AddWeatherDataAsync([day1]);

        // Add the same date again with different temps
        var day1Updated = new DailyWeather { Date = new DateTime(2024, 1, 1), HighTempF = 50m, LowTempF = 30m };
        await _sut.AddWeatherDataAsync([day1Updated]);
        _sut.InvalidateCache();

        var history = await _sut.GetWeatherHistoryAsync();
        history.Should().HaveCount(1);
        history[0].HighTempF.Should().Be(40m); // original kept, duplicate skipped
    }

    [Fact]
    public async Task AddWeatherDataAsync_SortsByDate()
    {
        var weatherData = new List<DailyWeather>
        {
            new() { Date = new DateTime(2024, 1, 3), HighTempF = 35m, LowTempF = 20m },
            new() { Date = new DateTime(2024, 1, 1), HighTempF = 40m, LowTempF = 25m },
            new() { Date = new DateTime(2024, 1, 2), HighTempF = 38m, LowTempF = 22m }
        };

        await _sut.AddWeatherDataAsync(weatherData);
        _sut.InvalidateCache();

        var history = await _sut.GetWeatherHistoryAsync();
        history[0].Date.Should().Be(new DateTime(2024, 1, 1));
        history[1].Date.Should().Be(new DateTime(2024, 1, 2));
        history[2].Date.Should().Be(new DateTime(2024, 1, 3));
    }

    #endregion

    #region ReminderSettings

    [Fact]
    public async Task SetReminderSettingsAsync_PersistsSettings()
    {
        var settings = new ReminderSettings
        {
            ThresholdGallons = 50m,
            IsEnabled = true
        };

        await _sut.SetReminderSettingsAsync(settings);
        _sut.InvalidateCache();

        var result = await _sut.GetReminderSettingsAsync();
        result.ThresholdGallons.Should().Be(50m);
        result.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region BackupFolderPath

    [Fact]
    public async Task SetBackupFolderPathAsync_PersistsPath()
    {
        var path = @"C:\MyBackups\Oil";

        await _sut.SetBackupFolderPathAsync(path);
        _sut.InvalidateCache();

        var result = await _sut.GetBackupFolderPathAsync();
        result.Should().Be(path);
    }

    [Fact]
    public async Task SetBackupFolderPathAsync_NullPath_ClearsValue()
    {
        await _sut.SetBackupFolderPathAsync(@"C:\SomePath");
        await _sut.SetBackupFolderPathAsync(null);
        _sut.InvalidateCache();

        var result = await _sut.GetBackupFolderPathAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_BacksUpToCloudFolder_WhenPathIsSet()
    {
        var backupDir = Path.Combine(_tempDir, "backup");
        var data = new TrackerData
        {
            TankCapacityGallons = 275m,
            BackupFolderPath = backupDir
        };

        await _sut.SaveAsync(data);

        var backupFile = Path.Combine(backupDir, "HeatingOilTracker_backup.json");
        File.Exists(backupFile).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_SkipsBackup_WhenPathIsEmpty()
    {
        var data = new TrackerData { TankCapacityGallons = 275m };

        // No exception, no backup directory created in temp (besides the data dir itself)
        await _sut.SaveAsync(data);

        // The only directory that exists is _tempDir
        var subdirs = Directory.GetDirectories(_tempDir);
        subdirs.Should().BeEmpty();
    }

    #endregion

    #region RegionalSettings

    [Fact]
    public async Task SetRegionalSettingsAsync_PersistsSettings()
    {
        var settings = new RegionalSettings { TemperatureUnit = "C" };

        await _sut.SetRegionalSettingsAsync(settings);
        _sut.InvalidateCache();

        var result = await _sut.GetRegionalSettingsAsync();
        result.TemperatureUnit.Should().Be("C");
    }

    [Fact]
    public async Task SetRegionalSettingsAsync_SetsSettingsModifiedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        await _sut.SetRegionalSettingsAsync(new RegionalSettings());
        _sut.InvalidateCache();

        var data = await _sut.LoadAsync();
        data.SettingsModifiedAt.Should().BeAfter(before);
    }

    #endregion
}
