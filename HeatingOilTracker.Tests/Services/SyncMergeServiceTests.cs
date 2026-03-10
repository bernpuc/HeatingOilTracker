using FluentAssertions;
using HeatingOilTracker.Core.Models;
using HeatingOilTracker.Core.Services;
using Xunit;

namespace HeatingOilTracker.Tests.Services;

public class SyncMergeServiceTests
{
    private readonly SyncMergeService _sut = new();

    private static OilDelivery MakeDelivery(Guid id, DateTime modifiedAt, bool isDeleted = false) =>
        new()
        {
            Id = id,
            Date = new DateTime(2025, 1, 1),
            Gallons = 100m,
            PricePerGallon = 3.50m,
            ModifiedAt = modifiedAt,
            IsDeleted = isDeleted
        };

    private static TrackerData EmptyData(List<OilDelivery>? deliveries = null) =>
        new()
        {
            Deliveries = deliveries ?? [],
            SettingsModifiedAt = DateTime.UtcNow
        };

    // ──────────────────────────────────────────────────────────────────────────
    // Delivery merge scenarios
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Merge_LocalOnlyDelivery_AppearsInResult()
    {
        var id = Guid.NewGuid();
        var local = EmptyData([MakeDelivery(id, DateTime.UtcNow)]);
        var remote = EmptyData();

        var result = _sut.Merge(local, remote);

        result.Deliveries.Should().ContainSingle(d => d.Id == id);
    }

    [Fact]
    public void Merge_RemoteOnlyDelivery_AppearsInResult()
    {
        var id = Guid.NewGuid();
        var local = EmptyData();
        var remote = EmptyData([MakeDelivery(id, DateTime.UtcNow)]);

        var result = _sut.Merge(local, remote);

        result.Deliveries.Should().ContainSingle(d => d.Id == id);
    }

    [Fact]
    public void Merge_SameDelivery_RemoteIsNewer_RemoteWins()
    {
        var id = Guid.NewGuid();
        var older = DateTime.UtcNow.AddMinutes(-10);
        var newer = DateTime.UtcNow;

        var localDelivery = MakeDelivery(id, older);
        localDelivery.Gallons = 80m;

        var remoteDelivery = MakeDelivery(id, newer);
        remoteDelivery.Gallons = 120m;

        var result = _sut.Merge(EmptyData([localDelivery]), EmptyData([remoteDelivery]));

        result.Deliveries.Should().ContainSingle();
        result.Deliveries[0].Gallons.Should().Be(120m);
    }

    [Fact]
    public void Merge_SameDelivery_LocalIsNewer_LocalWins()
    {
        var id = Guid.NewGuid();
        var older = DateTime.UtcNow.AddMinutes(-10);
        var newer = DateTime.UtcNow;

        var localDelivery = MakeDelivery(id, newer);
        localDelivery.Gallons = 80m;

        var remoteDelivery = MakeDelivery(id, older);
        remoteDelivery.Gallons = 120m;

        var result = _sut.Merge(EmptyData([localDelivery]), EmptyData([remoteDelivery]));

        result.Deliveries.Should().ContainSingle();
        result.Deliveries[0].Gallons.Should().Be(80m);
    }

    [Fact]
    public void Merge_DeletedOnRemoteWithNewerTimestamp_DeliveryIsDeleted()
    {
        var id = Guid.NewGuid();
        var localDelivery = MakeDelivery(id, DateTime.UtcNow.AddMinutes(-10), isDeleted: false);
        var remoteDelivery = MakeDelivery(id, DateTime.UtcNow, isDeleted: true);

        var result = _sut.Merge(EmptyData([localDelivery]), EmptyData([remoteDelivery]));

        result.Deliveries.Should().ContainSingle(d => d.Id == id && d.IsDeleted);
    }

    [Fact]
    public void Merge_DeletedOnLocal_LiveOnRemoteWithNewerTimestamp_ReappearsInMerge()
    {
        var id = Guid.NewGuid();
        var localDelivery = MakeDelivery(id, DateTime.UtcNow.AddMinutes(-10), isDeleted: true);
        var remoteDelivery = MakeDelivery(id, DateTime.UtcNow, isDeleted: false);

        var result = _sut.Merge(EmptyData([localDelivery]), EmptyData([remoteDelivery]));

        result.Deliveries.Should().ContainSingle(d => d.Id == id && !d.IsDeleted);
    }

    [Fact]
    public void Merge_TombstonesOlderThan60Days_ArePruned()
    {
        var id = Guid.NewGuid();
        // Deleted 61 days ago → should be pruned
        var staleTombstone = MakeDelivery(id, DateTime.UtcNow.AddDays(-61), isDeleted: true);

        var result = _sut.Merge(EmptyData([staleTombstone]), EmptyData());

        result.Deliveries.Should().BeEmpty();
    }

    [Fact]
    public void Merge_TombstonesYoungerThan60Days_AreKept()
    {
        var id = Guid.NewGuid();
        // Deleted 59 days ago → should be kept
        var recentTombstone = MakeDelivery(id, DateTime.UtcNow.AddDays(-59), isDeleted: true);

        var result = _sut.Merge(EmptyData([recentTombstone]), EmptyData());

        result.Deliveries.Should().ContainSingle(d => d.Id == id && d.IsDeleted);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Settings merge scenarios
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Merge_RemoteHasNewerSettingsModifiedAt_RemoteSettingsUsed()
    {
        var local = EmptyData();
        local.SettingsModifiedAt = DateTime.UtcNow.AddMinutes(-10);
        local.TankCapacityGallons = 275m;

        var remote = EmptyData();
        remote.SettingsModifiedAt = DateTime.UtcNow;
        remote.TankCapacityGallons = 330m;

        var result = _sut.Merge(local, remote);

        result.TankCapacityGallons.Should().Be(330m);
        result.SettingsModifiedAt.Should().Be(remote.SettingsModifiedAt);
    }

    [Fact]
    public void Merge_LocalHasNewerSettingsModifiedAt_LocalSettingsUsed()
    {
        var local = EmptyData();
        local.SettingsModifiedAt = DateTime.UtcNow;
        local.TankCapacityGallons = 275m;

        var remote = EmptyData();
        remote.SettingsModifiedAt = DateTime.UtcNow.AddMinutes(-10);
        remote.TankCapacityGallons = 330m;

        var result = _sut.Merge(local, remote);

        result.TankCapacityGallons.Should().Be(275m);
        result.SettingsModifiedAt.Should().Be(local.SettingsModifiedAt);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Local-only fields always come from local
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Merge_WeatherHistory_AlwaysFromLocal()
    {
        var localWeather = new List<DailyWeather>
        {
            new() { Date = new DateTime(2025, 1, 1), HighTempF = 40, LowTempF = 20 }
        };

        var local = EmptyData();
        local.WeatherHistory = localWeather;
        local.SettingsModifiedAt = DateTime.UtcNow.AddMinutes(-10); // remote is newer

        var remote = EmptyData();
        remote.WeatherHistory = []; // remote has no weather
        remote.SettingsModifiedAt = DateTime.UtcNow;

        var result = _sut.Merge(local, remote);

        result.WeatherHistory.Should().BeEquivalentTo(localWeather);
    }

    [Fact]
    public void Merge_BackupFolderPath_AlwaysFromLocal()
    {
        var local = EmptyData();
        local.BackupFolderPath = @"C:\MyBackups";
        local.SettingsModifiedAt = DateTime.UtcNow.AddMinutes(-10); // remote is newer

        var remote = EmptyData();
        remote.BackupFolderPath = @"D:\OtherBackups";
        remote.SettingsModifiedAt = DateTime.UtcNow;

        var result = _sut.Merge(local, remote);

        result.BackupFolderPath.Should().Be(@"C:\MyBackups");
    }
}
