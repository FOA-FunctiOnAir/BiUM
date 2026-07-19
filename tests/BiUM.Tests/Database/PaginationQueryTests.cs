using BiUM.Specialized.Database;
using Xunit;

namespace BiUM.Tests.Database;

public class PaginationQueryTests
{
    [Fact]
    public void ToPageBaseQuery_defaults_to_zero_and_ten()
    {
        var query = PaginationQuery.ToPageBaseQuery();

        Assert.Equal(0, query.PageStart);
        Assert.Equal(10, query.PageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 25)]
    [InlineData(50, 5)]
    [InlineData(null, null)]
    public void ToPageBaseQuery_preserves_provided_values(int? pageStart, int? pageSize)
    {
        var query = PaginationQuery.ToPageBaseQuery(pageStart, pageSize);

        Assert.Equal(pageStart, query.PageStart);
        Assert.Equal(pageSize, query.PageSize);
    }

    [Fact]
    public void ToPageBaseQuery_returns_null_for_unset_filter_fields()
    {
        var query = PaginationQuery.ToPageBaseQuery(0, 10);

        Assert.Null(query.Q);
        Assert.Null(query.SortBy);
        Assert.Null(query.SortDirection);
        Assert.Null(query.Filters);
    }
}