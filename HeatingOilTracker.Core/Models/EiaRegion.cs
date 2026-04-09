namespace HeatingOilTracker.Core.Models;

/// <summary>Simple bindable option for EIA region pickers.</summary>
public record EiaRegionOption(string Code, string Name);

/// <summary>
/// EIA duoarea codes for heating oil retail price regions.
/// Source: https://www.eia.gov/opendata/browser/petroleum/pri/wfr
/// </summary>
public static class EiaRegion
{
    public const string NewEngland       = "R1X";
    public const string CentralAtlantic  = "R1Y";
    public const string LowerAtlantic    = "R1Z";
    public const string Midwest          = "R20";
    public const string GulfCoast        = "R30";
    public const string RockyMountain    = "R40";
    public const string WestCoast        = "R50";
    public const string USAverage        = "NUS";

    public static readonly IReadOnlyList<(string Code, string Name)> All =
    [
        (NewEngland,      "New England"),
        (CentralAtlantic, "Central Atlantic"),
        (LowerAtlantic,   "Lower Atlantic"),
        (Midwest,         "Midwest"),
        (GulfCoast,       "Gulf Coast"),
        (RockyMountain,   "Rocky Mountain"),
        (WestCoast,       "West Coast"),
        (USAverage,       "U.S. Average"),
    ];

    public static string GetName(string code) =>
        All.FirstOrDefault(r => r.Code == code).Name ?? code;
}
