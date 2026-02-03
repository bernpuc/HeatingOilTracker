# Heating Oil Tracker

A Windows desktop application for tracking heating oil deliveries, monitoring tank levels, analyzing usage patterns, and understanding your carbon footprint.

## Features

### Dashboard
- **Tank Level Estimation**: Real-time estimate of remaining gallons based on delivery history and burn rate
- **Days Remaining**: Predicted days until tank reaches threshold
- **Low Tank Alerts**: Configurable alerts when tank level or days remaining drops below threshold
- **Refill Prediction**: Estimated date when you'll need to reorder

### Delivery Management
- **Delivery Tracking**: Record deliveries with date, gallons, price per gallon, and notes
- **Burn Rate Calculation**: Automatic gallons/day consumption tracking between deliveries
- **K-Factor Tracking**: Industry-standard efficiency metric (HDD/gallon) for each delivery
- **CSV Import/Export**: Import historical data or export for backup and analysis

### Weather Integration
- **Heating Degree Days (HDD)**: Fetches historical temperature data from Open-Meteo API
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

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10/11

### Build and Run

```bash
# Clone the repository
git clone https://github.com/bernpuc/HeatingOilTracker.git
cd HeatingOilTracker

# Build
dotnet build

# Run
dotnet run --project HeatingOilTracker
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
| Burn Rate | Gallons / Days | Average daily consumption |
| CO₂ | Gallons × 22.38 | Pounds of CO₂ emitted |
| Offset Cost | CO₂ tons × $15-$50 | Estimated carbon offset cost |

## License

MIT License
