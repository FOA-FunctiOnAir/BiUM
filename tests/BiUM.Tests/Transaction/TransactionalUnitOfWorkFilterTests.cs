using BiUM.Infrastructure.MagicOnion.Filters.Server;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class TransactionalUnitOfWorkFilterTests
{
    [Fact]
    public void Order_is_inner_relative_to_GlobalApiResponseFilter()
    {
        var filter = new TransactionalUnitOfWorkFilter();

        filter.Order.Should().Be(100);
        filter.Order.Should().BeGreaterThan(0);
    }
}