using Prism.Events;

namespace HeatingOilTracker.Events;

/// <summary>
/// Published when weather data has been updated.
/// </summary>
public class WeatherDataUpdatedEvent : PubSubEvent { }

/// <summary>
/// Published when dashboard should refresh its data.
/// </summary>
public class DashboardRefreshRequestedEvent : PubSubEvent { }
