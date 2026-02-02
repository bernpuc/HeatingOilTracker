namespace HeatingOilTracker.Models;

/// <summary>
/// Configuration for delivery reminder alerts.
/// </summary>
public class ReminderSettings
{
    /// <summary>
    /// Whether reminders are enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Alert threshold in gallons. When estimated level drops below this, show alert.
    /// </summary>
    public decimal ThresholdGallons { get; set; } = 50m;

    /// <summary>
    /// Optional: Alert threshold in days remaining.
    /// </summary>
    public int? ThresholdDays { get; set; }
}
