using BiUM.Infrastructure.MagicOnion.Filters.Server;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class TransactionalUnitOfWorkFilterTests
{
    [Fact]
    public void Order_is_inner_relative_to_GlobalApiResponseFilter()
    {
        // GlobalApiResponseFilter.Order == 0 (outermost, swallows exceptions into ApiResponse).
        // TransactionalUnitOfWorkFilter must run with a higher Order so it sits closer to the
        // actual method call and observes exceptions before GlobalApiResponseFilter swallows them.
        var filter = new TransactionalUnitOfWorkFilter();

        filter.Order.Should().Be(100);
        filter.Order.Should().BeGreaterThan(0);
    }
}