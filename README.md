# Heating Oil Tracker

A Windows desktop application for tracking heating oil deliveries, monitoring tank levels, analyzing usage patterns, and understanding your carbon footprint.

## Features

### Dashboard
- **Tank Level Estimation**: Real-time estimate of remaining gallons using K-Factor with weather data (falls back to burn rate)
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
- **ZIP Code Lookup**: Automatic location detection from ZIP code

### Charts
- **Price History**: Track oil price trends over time
- **Burn Rate Trends**: Visualize consumption patterns
- **K-Factor Chart**: Monitor heating efficiency changes
- **Interactive Tooltips**: Crosshair tooltips for detailed data inspection

### Cost Reports
- **Annual Summaries**: Total cost, gallons, and average price per year
- **Seasonal Breakdown**: Compare heating season (Oct-Mar) vs off-season usage
- **Year-over-Year Comparison**: Table with cost, gallons, HDD, $/HDD, and K-Factor
- **Weather-Normalized Costs**: $/HDD metric for fair year-to-year comparison

### Carbon Footprint
- **CO₂ Emissions Tracking**: Calculates emissions using EPA standard (22.38 lbs CO₂/gallon)
- **Weather-Normalized Emissions**: CO₂/HDD for comparing across different winters
- **Seasonal Carbon Breakdown**: Heating season vs off-season emissions
- **Offset Cost Estimator**: Estimated cost to neutralize emissions ($15-$50/ton range)

### Settings
- **Tank Configuration**: Set tank capacity for accurate level estimates
- **Location Settings**: Configure ZIP code for weather data
- **Reminder Thresholds**: Set gallon and days-remaining alert thresholds
- **Cloud Backup**: Automatic backup to OneDrive, Dropbox, or other cloud folders

### Reference Guide
- **In-App Documentation**: Explains all calculations (HDD, K-Factor, burn rate, carbon footprint)
- **Links to Resources**: External links for further reading on heating efficiency and carbon offsets

## Tech Stack

- .NET 9
- WPF (Windows Presentation Foundation)
- Prism MVVM Framework
- LiveCharts2 for charting
- CsvHelper for CSV operations
- Open-Meteo API for weather data
- Zippopotam.us for ZIP code geocoding

## Installation

### Using the Installer (Recommended)

Download the latest `HeatingOilTracker x.x.x Installer.exe` from the [Releases](https://github.com/bernpuc/HeatingOilTracker/releases) page and run it. The installer will:
- Install to `C:\Program Files\HeatingOilTracker`
- Create Start Menu and Desktop shortcuts
- Register in Programs & Features for easy uninstall

### Build from Source

#### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10/11

#### Build and Run

```bash
# Clone the repository
git clone https://github.com/bernpuc/HeatingOilTracker.git
cd HeatingOilTracker

# Build
dotnet build

# Run
dotnet run --project HeatingOilTracker
```

#### Build the Installer

Requires [NSIS](https://nsis.sourceforge.io/Download) (Nullsoft Scriptable Install System).

```bash
# Build Release version
dotnet publish HeatingOilTracker -c Release

# Create installer
cd HeatingOilTracker/Package
makensis -DVERSION=1.0.0 Installer.nsi
```

## Data Storage

Data is stored locally in JSON format at:
```
%APPDATA%\HeatingOilTracker\data.json
```

Optional cloud backup can be configured to sync this data to a cloud-synced folder.

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
