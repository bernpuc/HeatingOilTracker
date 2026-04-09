namespace HeatingOilTracker.Core.Models;

/// <summary>
/// Maps geographic coordinates to the nearest EIA heating oil price region (PADD sub-region).
/// Uses longitude/latitude bounding boxes for the US. Returns <see cref="EiaRegion.USAverage"/>
/// for non-US coordinates or when location is unavailable.
/// </summary>
public static class EiaRegionMapper
{
    /// <summary>
    /// Returns the EIA duoarea code for the given coordinates.
    /// </summary>
    public static string FromCoordinates(decimal latitude, decimal longitude)
    {
        // Outside US bounding box (covers continental US + AK + HI)
        if (latitude < 18m || latitude > 72m || longitude < -180m || longitude > -66m)
            return EiaRegion.USAverage;

        // New England: CT, MA, ME, NH, RI, VT
        if (latitude >= 41m && longitude >= -73.7m)
            return EiaRegion.NewEngland;

        // Central Atlantic: DC, DE, MD, NJ, NY, PA
        if (latitude >= 38m && longitude >= -80.5m)
            return EiaRegion.CentralAtlantic;

        // Lower Atlantic: FL, GA, NC, SC, VA, WV
        if (longitude >= -85m)
            return EiaRegion.LowerAtlantic;

        // Midwest: IL, IN, IA, KS, KY, MI, MN, MO, NE, ND, OH, OK, SD, TN, WI
        if (latitude >= 36m && longitude >= -104m)
            return EiaRegion.Midwest;

        // Gulf Coast: AL, AR, LA, MS, NM, TX
        if (longitude >= -104m)
            return EiaRegion.GulfCoast;

        // Rocky Mountain: CO, ID, MT, UT, WY
        if (longitude >= -116m)
            return EiaRegion.RockyMountain;

        // West Coast: AK, AZ, CA, HI, NV, OR, WA
        return EiaRegion.WestCoast;
    }
}
