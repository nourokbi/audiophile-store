using Audiophile.Application.Common;

namespace Audiophile.Tests;

// The shop's days are Cairo days, UTC+2 in winter and UTC+3 in summer
public class ShopTimeTests
{
    [Fact]
    public void An_order_after_midnight_in_Cairo_belongs_to_the_Cairo_day()
    {
        // 22:30 UTC on 25 October is 01:30 on 26 October in Cairo, still summer time
        Assert.Equal(new DateOnly(2026, 10, 26), ShopTime.DayOf(new DateTime(2026, 10, 25, 22, 30, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void A_day_starts_at_Cairo_midnight()
    {
        Assert.Equal(new DateTime(2026, 1, 9, 22, 0, 0, DateTimeKind.Utc), ShopTime.StartOf(new DateOnly(2026, 1, 10)));
    }

    [Fact]
    public void The_day_summer_time_starts_begins_at_one_in_the_morning()
    {
        // Egypt skips from midnight to 1 a.m. on the last Friday of April, 01:00 at UTC+3 is 22:00 UTC the day before
        Assert.Equal(new DateTime(2026, 4, 23, 22, 0, 0, DateTimeKind.Utc), ShopTime.StartOf(new DateOnly(2026, 4, 24)));
    }
}
