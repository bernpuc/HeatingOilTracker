using CsvHelper;
using CsvHelper.Configuration;
using HeatingOilTracker.Models;
using System.Globalization;
using System.IO;

namespace HeatingOilTracker.Services;

public class CsvImportService : ICsvImportService
{
    public async Task<CsvImportResult> ImportFromCsvAsync(string filePath, List<OilDelivery> existingDeliveries)
    {
        var result = new CsvImportResult { Success = true };

        try
        {
            var existingDates = new HashSet<DateTime>(
                existingDeliveries.Select(d => d.Date.Date));

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<OilDeliveryImportMap>();

            var records = csv.GetRecords<OilDeliveryImportDto>();
            var imported = new List<OilDelivery>();

            var rowNumber = 0;
            foreach (var record in records)
            {
                rowNumber++;
                result.TotalRows++;

                // Validate
                if (record.Gallons <= 0)
                {
                    result.Errors.Add($"Row {rowNumber}: Gallons must be greater than 0 (got {record.Gallons})");
                    continue;
                }

                if (record.PricePerGallon <= 0)
                {
                    result.Errors.Add($"Row {rowNumber}: Price per gallon must be greater than 0 (got {record.PricePerGallon})");
                    continue;
                }

                if (record.Date == default)
                {
                    result.Errors.Add($"Row {rowNumber}: Invalid or missing date");
                    continue;
                }

                // Check for duplicate by date
                if (existingDates.Contains(record.Date.Date))
                {
                    result.SkippedCount++;
                    continue;
                }

                var delivery = new OilDelivery
                {
                    Id = Guid.NewGuid(),
                    Date = record.Date,
                    Gallons = record.Gallons,
                    PricePerGallon = record.PricePerGallon,
                    Notes = record.Notes ?? string.Empty
                };

                imported.Add(delivery);
                existingDates.Add(record.Date.Date);
                result.ImportedCount++;
            }

            result.Success = result.Errors.Count == 0;

            // Return imported deliveries via a property for the caller to persist
            result.ImportedDeliveries = imported;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return await Task.FromResult(result);
    }
}

public class OilDeliveryImportDto
{
    public DateTime Date { get; set; }
    public decimal Gallons { get; set; }
    public decimal PricePerGallon { get; set; }
    public decimal? TotalInvoice { get; set; }
    public string? Notes { get; set; }
}

public class OilDeliveryImportMap : ClassMap<OilDeliveryImportDto>
{
    public OilDeliveryImportMap()
    {
        Map(m => m.Date).Name("Date", "date", "Delivery Date", "delivery_date");
        Map(m => m.Gallons).Name("Gallons", "gallons", "Quantity", "quantity", "Gal");
        Map(m => m.PricePerGallon).Name("Price Per Gallon", "PricePerGallon", "price_per_gallon", "$/Gal", "Price", "PPG");
        Map(m => m.TotalInvoice).Name("Total Invoice", "TotalInvoice", "Total", "total", "Invoice Total").Optional();
        Map(m => m.Notes).Name("Notes", "notes", "Comment", "Comments").Optional();
    }
}
