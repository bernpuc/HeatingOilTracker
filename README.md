# Heating Oil Tracker

A heating oil delivery tracker available as both a **Windows desktop app (WPF)** and a **cross-platform mobile/desktop app (MAUI)** for Android and Windows. Track deliveries, monitor tank levels, analyze usage patterns, and understand your carbon footprint.

![Dashboard Screenshot](docs/Screenshot%20Dashboard.png)

## Features

### Dashboard
- **Tank Level Estimation**: Real-time estimate of remaining gallons using K-Factor with weather data (falls back to burn rate); gauge displays gallons, percentage, and eighths (e.g. 6/8ths)
- **Days Remaining**: Predicted days until tank reaches threshold
- **Low Tank Alerts**: Configurable alerts when tank level or days remaining drops below threshold
- **Refill Prediction**: Estimated date when you'll need to reorder

### Delivery Management
- **Delivery Tracking**: Record deliveries with date, gallons, price per gallon, and notes
- **Burn Rate Calculation**: Automatic gallons/day consumption tracking between deliveries
- **K-Factor Tracking**: Industry-standard efficiency metric (HDD/gallon) for each delivery
- **CSV Import/Export**: Import historical data or export for backup and analysis

### Weather Integration
- **Automatic Updates**: Weather data fetched automatically on startup when location is configured
- **Heating Degree Days (HDD)**: Historical temperature data from Open-Meteo API
- **Weather-Normalized Metrics**: Compare efficiency across years with different temperatures
- **Location Search**: Search by city name to configure your location for weather data

### Charts
- **Price History**: Track oil price trends over time
- **Burn Rate Trends**: Visualize consumption patterns
- **K-Factor Chart**: Monitor heating efficiency changes
- **Interactive Tooltips**: Crosshair tooltips for detailed data inspection
- **Interactive Zoom**: Ctrl + scroll to zoom in/out on any chart; double-click to reset to full view

### Oil Prices
- **Regional Market Prices**: Weekly retail heating oil prices from the U.S. Energy Information Administration (EIA) — requires a free API key from eia.gov/opendata/register.php
- **Auto-Detected Region**: EIA price region is automatically determined from your configured location (New England, Central Atlantic, Lower Atlantic, Midwest, Gulf Coast, Rocky Mountain, West Coast, or U.S. Average); can be overridden in Settings
- **vs. National Average**: Latest regional price shown alongside the U.S. average with the dollar and percentage difference
- **Trend Analysis**: Compares the 4-week average to the 13-week average to classify prices as trending up, down, or stable — with week-over-week and year-over-year changes
- **Buy/Wait Suggestion**: Actionable recommendation based on the trend direction and whether your region is above or below the national average
- **52-Week Price Chart**: Line chart of your region and the U.S. average; summer gaps (Apr–Oct) are expected as the EIA only surveys heating oil prices during the heating season

### Cost Reports
- **Annual Summaries**: Total cost, gallons, and average price per year
- **Seasonal Breakdown**: Compare heating season vs off-season usage; season months are configurable (default Oct–Mar for Northern Hemisphere, Apr–Sep for Southern)
- **Year-over-Year Comparison**: Table with cost, gallons, HDD, $/HDD, and K-Factor
- **Weather-Normalized Costs**: $/HDD metric for fair year-to-year comparison

### Carbon Footprint
- **CO₂ Emissions Tracking**: Calculates emissions using EPA standard (22.38 lbs CO₂/gallon)
- **Weather-Normalized Emissions**: CO₂/HDD for comparing across different winters
- **Seasonal Carbon Breakdown**: Heating season vs off-season emissions
- **Offset Cost Estimator**: Estimated cost to neutralize emissions ($15-$50/ton range)

### Settings
- **Tank Configuration**: Set tank capacity for accurate level estimates
- **Location Settings**: Search and set your location for weather data
- **Reminder Thresholds**: Set gallon and days-remaining alert thresholds
- **Heating Season**: Configure start and end months to match your climate (Northern or Southern Hemisphere)
- **Cloud Backup**: Automatic backup to OneDrive, Dropbox, or other cloud folders
- **Google Drive Sync**: Connect your Google account to sync data across devices (MAUI)
- **EIA Price Region**: Override the auto-detected region for oil price lookups

### Reference Guide
- **In-App Documentation**: Explains all calculations (HDD, K-Factor, burn rate, carbon footprint)
- **Links to Resources**: External links for further reading on heating efficiency and carbon offsets

## Tech Stack

### Shared (Core Library)
- .NET 9
- LiveCharts2 for charting
- CsvHelper for CSV operations
- Open-Meteo API for weather data and geocoding

### WPF Desktop App
- WPF (Windows Presentation Foundation)
- Prism 9 MVVM Framework with DryIoc DI

### MAUI App (Android + Windows)
- .NET MAUI 9
- Microsoft.Extensions dependency injection
- Google Drive API with OAuth2 PKCE for cross-device sync

## Installation

### WPF Desktop App (Windows)

Download the latest `HeatingOilTracker x.x.x Installer.exe` from the [Releases](https://github.com/bernpuc/HeatingOilTracker/releases) page and run it. The installer will:
- Install to `C:\Program Files\HeatingOilTracker`
- Create Start Menu and Desktop shortcuts
- Register in Programs & Features for easy uninstall

### MAUI App

Build from source (see below). Android APK and Windows MSIX packages are not yet published to stores.

## Build from Source

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10/11

### WPF Desktop App

```bash
git clone https://github.com/bernpuc/HeatingOilTracker.git
cd HeatingOilTracker

dotnet build
dotnet run --project HeatingOilTracker
```

#### Build the Installer

Requires [NSIS](https://nsis.sourceforge.io/Download) (Nullsoft Scriptable Install System).

```bash
dotnet publish HeatingOilTracker -c Release

cd HeatingOilTracker/Package
makensis -DVERSION=1.0.0 Installer.nsi
```

### MAUI App

The MAUI app requires Google OAuth credentials for the Google Drive sync feature.

1. Copy `HeatingOilTracker.Maui/Secrets.template.cs` to `HeatingOilTracker.Maui/Secrets.cs`
2. Fill in your Google Cloud OAuth client IDs and secret (see `Secrets.template.cs` for instructions)

```bash
# Run on Windows
dotnet run --project HeatingOilTracker.Maui -f net9.0-windows10.0.19041.0

# Build for Android (requires Android SDK / Visual Studio with MAUI workload)
dotnet build HeatingOilTracker.Maui -f net9.0-android
```

Google Drive sync is optional — the app works without credentials, just without cross-device sync.

## Data Storage

Data is stored locally in JSON format at:
```
%APPDATA%\HeatingOilTracker\data.json
```

Optional cloud backup can be configured to sync this data to a cloud-synced folder. The MAUI app additionally supports Google Drive sync to share data between Android and Windows.

## Sample Data

A sample dataset is included in the `samples/` folder to demonstrate the app's features without entering real data. To use it:

1. Go to **Deliveries** > **Import CSV**
2. Select `samples/demo-deliveries.csv`

See [samples/README.md](samples/README.md) for details.

## Key Calculations

| Metric | Formula | Description |
|--------|---------|-------------|
| HDD | max(0, 65 - avg temp) | Heating Degree Days per day |
| K-Factor | HDD / Gallons | Efficiency metric (higher = better) |
| Tank Estimate | HDD / K-Factor | Gallons used since last fill |
| Burn Rate | Gallons / Days | Average daily consumption (fallback) |
| CO₂ | Gallons × 22.38 | Pounds of CO₂ emitted |
| Offset Cost | CO₂ tons × $15-$50 | Estimated carbon offset cost |

## License

MIT License
