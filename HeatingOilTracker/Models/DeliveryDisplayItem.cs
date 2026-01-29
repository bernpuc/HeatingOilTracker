namespace HeatingOilTracker.Models;

public class DeliveryDisplayItem
{
    public OilDelivery Delivery { get; }

    public DateTime Date => Delivery.Date;
    public decimal Gallons => Delivery.Gallons;
    public decimal PricePerGallon => Delivery.PricePerGallon;
    public string Notes => Delivery.Notes;
    public Guid Id => Delivery.Id;

    public decimal TotalCost => Math.Round(Gallons * PricePerGallon, 2);

    public int? DaysSinceLastFill { get; }
    public decimal? GallonsPerDay { get; }
    public decimal? HDD { get; }
    public decimal? KFactor { get; }

    public string DaysSinceLastFillDisplay => DaysSinceLastFill.HasValue
        ? DaysSinceLastFill.Value.ToString()
        : "--";

    public string GallonsPerDayDisplay => GallonsPerDay.HasValue
        ? GallonsPerDay.Value.ToString("F1")
        : "--";

    public string HDDDisplay => HDD.HasValue
        ? HDD.Value.ToString("F0")
        : "--";

    public string KFactorDisplay => KFactor.HasValue
        ? KFactor.Value.ToString("F3")
        : "--";

    public DeliveryDisplayItem(OilDelivery delivery, OilDelivery? previousDelivery, decimal? hdd = null)
    {
        Delivery = delivery;
        HDD = hdd;

        if (previousDelivery != null)
        {
            var days = (int)(delivery.Date - previousDelivery.Date).TotalDays;
            if (days > 0)
            {
                DaysSinceLastFill = days;
                GallonsPerDay = Math.Round(delivery.Gallons / days, 1);

                // Calculate K-Factor if we have HDD data
                if (hdd.HasValue && hdd.Value > 0)
                {
                    KFactor = Math.Round(delivery.Gallons / hdd.Value, 4);
                }
            }
        }
    }
}
