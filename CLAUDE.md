# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Build all projects
dotnet build

# Run the MAUI app (Windows)
dotnet build -t:Run -f net9.0-windows10.0.19041.0 HeatingOilTracker.Maui/HeatingOilTracker.Maui.csproj

# Run all tests
dotnet test

# Run tests for a specific project with verbose output
dotnet test HeatingOilTracker.Tests --logger "console;verbosity=detailed"

# Run a single test class
dotnet test HeatingOilTracker.Tests --filter "FullyQualifiedName~TankEstimatorServiceTests"
```

## Project Structure

Three projects in `HeatingOilTracker.sln`:

- **HeatingOilTracker.Maui** — Cross-platform MAUI app (.NET 9), Android + Windows. Uses Shell navigation with Microsoft.Extensions DI. Pages + ViewModels in `Pages/` and `ViewModels/`. This is the actively used app (desktop and Android); the old WPF project was retired.
- **HeatingOilTracker.Core** — Shared class library (.NET 9). Contains all models, interfaces, and service implementations.
- **HeatingOilTracker.Tests** — xUnit tests (.NET 9-windows) with Moq and FluentAssertions. Mirrors the Core structure under `Services/` and `Models/`.

## Architecture

The Core library is the heart of the application. The MAUI app depends on it and only contains UI code.

**Key interfaces (HeatingOilTracker.Core/Interfaces/):**
- `IDataService` — JSON persistence at `%APPDATA%\HeatingOilTracker\data.json`. Uses atomic write (temp file → rename). Optionally syncs to a user-configured cloud folder.
- `ITankEstimatorService` — Core calculation engine. Estimates current tank level using K-Factor (weather-based, primary) or burn rate (fallback).
- `IWeatherService` — Fetches historical weather from Open-Meteo API; computes HDD (Heating Degree Days).
- `IReportService` — Annual/seasonal summaries, cost analysis, carbon footprint.
- `ICsvImportService` — CSV import/export using CsvHelper.

**Root data model:** `TrackerData` holds the tank capacity, all deliveries, location, weather history, and settings. This is what `IDataService` loads and saves.

## Key Domain Concepts

**HDD (Heating Degree Days):** `max(0, BaseTemp - AvgDailyTemp)`. Base is 65°F (US) or 18°C (metric).

**K-Factor:** `HDD / Gallons`. The primary efficiency metric used to estimate consumption from weather data. Higher = more efficient.

**Tank level estimation:**
- Primary: `HDD_since_last_delivery / K-Factor = gallons_used`
- Fallback: `BurnRate × Days = gallons_used`

**Burn rate:** Weighted average across deliveries, with most-recent delivery weighted 1.0 and older deliveries decaying by 0.2 each. Seasonal split (before/after March 15) is applied.

## DI Registration

- **MAUI:** `MauiProgram.cs` → `CreateMauiApp()` uses `builder.Services` to register services.

## Charting

Uses LiveCharts2 (`LiveChartsCore.SkiaSharpView`). Charts are configured in ViewModels as `ISeries[]` and `Axis[]` properties bound to the chart controls.

## Testing Patterns

Tests use Arrange-Act-Assert with Moq for dependency mocking and FluentAssertions for readable assertions. `Fixtures/TestData.cs` contains shared test data builders. The test project targets `net9.0-windows` because some Core dependencies require Windows.

## Target Frameworks
- MAUI: `net9.0-android`, `net9.0-windows10.0.19041.0`

## MAUI-Specific
- Interaction model: tap-to-start / tap-to-stop (no press-and-hold)
- Use `SecureStorage` for any persisted credentials
- Pull-to-refresh implemented on list views — preserve this pattern when adding new data views
- Weather chart uses LiveCharts2