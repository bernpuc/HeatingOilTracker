namespace HeatingOilTracker.Core.Models;

/// <summary>
/// A single weekly heating oil retail price data point from the EIA API.
/// </summary>
public record EiaHeatingOilPrice(
    DateOnly Period,
    string RegionCode,
    string RegionName,
    decimal PricePerGallon
);
