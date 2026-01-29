using HeatingOilTracker.Models;

namespace HeatingOilTracker.Services;

public interface ICsvImportService
{
    Task<CsvImportResult> ImportFromCsvAsync(string filePath, List<OilDelivery> existingDeliveries);
}
