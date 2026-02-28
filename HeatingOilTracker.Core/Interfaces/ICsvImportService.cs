using HeatingOilTracker.Core.Models;

namespace HeatingOilTracker.Core.Interfaces;

public interface ICsvImportService
{
    Task<CsvImportResult> ImportFromCsvAsync(string filePath, List<OilDelivery> existingDeliveries);
}
