using Audiophile.Domain.Entities;

namespace Audiophile.Tests;

// New rows get time ordered ids, so each insert lands at the end of the index
public class IdTests
{
    [Fact]
    public async Task A_later_entity_gets_a_larger_version_7_id()
    {
        var first = new Cart(Guid.NewGuid());
        await Task.Delay(2);
        var second = new Cart(Guid.NewGuid());

        Assert.Equal(7, first.Id.Version);
        Assert.Equal(7, second.Id.Version);
        Assert.True(second.Id.CompareTo(first.Id) > 0);
    }
}
