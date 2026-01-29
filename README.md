# Heating Oil Tracker

A Windows desktop application for tracking heating oil deliveries and analyzing usage patterns over time.

## Features

- **Delivery Tracking**: Record deliveries with date, gallons, price per gallon, and notes
- **Burn Rate Calculation**: Automatically calculates gallons/day consumption between deliveries
- **Interactive Charts**: Visualize burn rate and price trends over time with crosshair tooltips
- **CSV Import/Export**: Import historical data or export for backup and analysis
- **Summary Statistics**: View total deliveries, average price, and average burn rate

## Screenshots

*Coming soon*

## Tech Stack

- .NET 9
- WPF (Windows Presentation Foundation)
- Prism MVVM Framework
- LiveCharts2 for charting
- CsvHelper for CSV operations

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

Delivery data is stored locally in JSON format at:
```
%APPDATA%\HeatingOilTracker\data.json
```

## License

MIT License
