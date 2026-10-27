namespace Audiophile.Application.Common;

// The shop counts Cairo days, so an order placed after midnight in Cairo belongs to that day and not to the UTC day before
public static class ShopTime
{
  public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");

  public static DateOnly DayOf(DateTime utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, Zone));

  // The UTC moment a Cairo day starts, Egypt's summer time starts at midnight so that one day starts at 1 a.m.
  public static DateTime StartOf(DateOnly day)
  {
    var midnight = day.ToDateTime(TimeOnly.MinValue);
    if (Zone.IsInvalidTime(midnight))
      midnight = midnight.AddHours(1);
    return TimeZoneInfo.ConvertTimeToUtc(midnight, Zone);
  }
}
