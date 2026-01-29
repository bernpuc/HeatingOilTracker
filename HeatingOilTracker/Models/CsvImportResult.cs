namespace HeatingOilTracker.Models;

public class CsvImportResult
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<OilDelivery> ImportedDeliveries { get; set; } = new();

    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (ImportedCount > 0) parts.Add($"{ImportedCount} imported");
            if (SkippedCount > 0) parts.Add($"{SkippedCount} skipped (duplicates)");
            if (Errors.Count > 0) parts.Add($"{Errors.Count} errors");
            return string.Join(", ", parts);
        }
    }
}
