using FluentAssertions;
using HeatingOilTracker.Models;
using HeatingOilTracker.Services;
using Xunit;

namespace HeatingOilTracker.Tests.Services;

public class CsvImportServiceTests : IDisposable
{
    private readonly CsvImportService _sut;
    private readonly string _tempDirectory;
    private readonly List<string> _tempFiles = new();

    public CsvImportServiceTests()
    {
        _sut = new CsvImportService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "HeatingOilTrackerTests");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        if (Directory.Exists(_tempDirectory))
        {
            try { Directory.Delete(_tempDirectory, true); } catch { }
        }
    }

    private string CreateTempCsvFile(string content)
    {
        var filePath = Path.Combine(_tempDirectory, $"test_{Guid.NewGuid()}.csv");
        File.WriteAllText(filePath, content);
        _tempFiles.Add(filePath);
        return filePath;
    }

    #region Valid Import Tests

    [Fact]
    public async Task ImportFromCsvAsync_ValidCsv_ImportsSuccessfully()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,150,3.50,Winter delivery
2024-02-20,175,3.75,Cold snap";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedCount.Should().Be(2);
        result.TotalRows.Should().Be(2);
        result.Errors.Should().BeEmpty();
        result.ImportedDeliveries.Should().HaveCount(2);

        var first = result.ImportedDeliveries.First(d => d.Date.Day == 15);
        first.Gallons.Should().Be(150m);
        first.PricePerGallon.Should().Be(3.50m);
        first.Notes.Should().Be("Winter delivery");
    }

    [Fact]
    public async Task ImportFromCsvAsync_FlexibleColumnNames_ImportsSuccessfully()
    {
        // Arrange - using alternative column names
        var csv = @"delivery_date,Quantity,$/Gal,Comment
2024-03-01,200,4.00,Spring delivery";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedCount.Should().Be(1);
        result.ImportedDeliveries[0].Gallons.Should().Be(200m);
        result.ImportedDeliveries[0].PricePerGallon.Should().Be(4.00m);
    }

    [Fact]
    public async Task ImportFromCsvAsync_NoNotes_ImportsWithEmptyNotes()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon
2024-01-15,150,3.50";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedDeliveries[0].Notes.Should().BeEmpty();
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task ImportFromCsvAsync_ZeroGallons_ReportsError()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,0,3.50,Invalid";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeFalse();
        result.ImportedCount.Should().Be(0);
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Gallons must be greater than 0");
        result.Errors[0].Should().Contain("Row 1");
    }

    [Fact]
    public async Task ImportFromCsvAsync_NegativeGallons_ReportsError()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,-50,3.50,Invalid";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Gallons must be greater than 0");
    }

    [Fact]
    public async Task ImportFromCsvAsync_ZeroPricePerGallon_ReportsError()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,150,0,Invalid";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Price per gallon must be greater than 0");
    }

    [Fact]
    public async Task ImportFromCsvAsync_NegativePricePerGallon_ReportsError()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,150,-3.50,Invalid";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Price per gallon must be greater than 0");
    }

    [Fact]
    public async Task ImportFromCsvAsync_InvalidDate_ReportsError()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon,Notes
not-a-date,150,3.50,Invalid";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ImportFromCsvAsync_MultipleErrors_ReportsAllErrors()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,0,3.50,Invalid gallons
2024-02-20,150,-1,Invalid price";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors[0].Should().Contain("Row 1");
        result.Errors[1].Should().Contain("Row 2");
    }

    #endregion

    #region Duplicate Detection Tests

    [Fact]
    public async Task ImportFromCsvAsync_DuplicateDate_SkipsRow()
    {
        // Arrange
        var existingDeliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 15), Gallons = 100m, PricePerGallon = 3.00m }
        };

        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,150,3.50,Duplicate
2024-02-20,175,3.75,New";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, existingDeliveries);

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.TotalRows.Should().Be(2);
        result.ImportedDeliveries.Should().HaveCount(1);
        result.ImportedDeliveries[0].Date.Should().Be(new DateTime(2024, 2, 20));
    }

    [Fact]
    public async Task ImportFromCsvAsync_AllDuplicates_SkipsAll()
    {
        // Arrange
        var existingDeliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 15), Gallons = 100m, PricePerGallon = 3.00m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 2, 20), Gallons = 100m, PricePerGallon = 3.00m }
        };

        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,150,3.50,Duplicate
2024-02-20,175,3.75,Duplicate";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, existingDeliveries);

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedCount.Should().Be(0);
        result.SkippedCount.Should().Be(2);
    }

    [Fact]
    public async Task ImportFromCsvAsync_DuplicatesWithinCsv_OnlyImportsFirst()
    {
        // Arrange - same date appears twice in CSV
        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,150,3.50,First
2024-01-15,200,4.00,Duplicate within file";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.ImportedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.ImportedDeliveries[0].Gallons.Should().Be(150m); // First one wins
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ImportFromCsvAsync_EmptyFile_ReturnsSuccessWithNoImports()
    {
        // Arrange - empty file with just headers
        var csv = @"Date,Gallons,PricePerGallon,Notes";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedCount.Should().Be(0);
        result.TotalRows.Should().Be(0);
    }

    [Fact]
    public async Task ImportFromCsvAsync_FileNotFound_ReportsError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.csv");

        // Act
        var result = await _sut.ImportFromCsvAsync(nonExistentPath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Import failed");
    }

    [Fact]
    public async Task ImportFromCsvAsync_MixedValidAndInvalid_ImportsValidRows()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,150,3.50,Valid
2024-02-20,0,3.75,Invalid - zero gallons
2024-03-10,175,4.00,Valid";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.Success.Should().BeFalse(); // Has errors
        result.ImportedCount.Should().Be(2);
        result.Errors.Should().HaveCount(1);
        result.ImportedDeliveries.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportFromCsvAsync_CreatesUniqueIds()
    {
        // Arrange
        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,150,3.50,First
2024-02-20,175,3.75,Second";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, new List<OilDelivery>());

        // Assert
        result.ImportedDeliveries[0].Id.Should().NotBe(Guid.Empty);
        result.ImportedDeliveries[1].Id.Should().NotBe(Guid.Empty);
        result.ImportedDeliveries[0].Id.Should().NotBe(result.ImportedDeliveries[1].Id);
    }

    [Fact]
    public async Task ImportFromCsvAsync_SummaryProperty_FormatsCorrectly()
    {
        // Arrange
        var existingDeliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 15), Gallons = 100m, PricePerGallon = 3.00m }
        };

        var csv = @"Date,Gallons,PricePerGallon,Notes
2024-01-15,150,3.50,Duplicate
2024-02-20,175,3.75,Valid
2024-03-10,0,4.00,Invalid";
        var filePath = CreateTempCsvFile(csv);

        // Act
        var result = await _sut.ImportFromCsvAsync(filePath, existingDeliveries);

        // Assert
        result.Summary.Should().Contain("1 imported");
        result.Summary.Should().Contain("1 skipped");
        result.Summary.Should().Contain("1 errors");
    }

    #endregion
}
