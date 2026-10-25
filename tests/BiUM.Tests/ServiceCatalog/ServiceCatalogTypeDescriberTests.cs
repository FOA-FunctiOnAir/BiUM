using BiUM.Contract.Models.Api;
using BiUM.Specialized.Services.ServiceCatalog;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.ServiceCatalog;

public sealed class ServiceCatalogTypeDescriberTests
{
    private enum SampleStatus
    {
        Active
    }

    private sealed class SampleItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
        public SampleStatus Status { get; set; }
        public List<Guid> TagIds { get; set; } = [];
        public SampleItem? Parent { get; set; }
    }

    [Fact]
    public void DescribeMembers_ShouldMapClrTypesNullabilityAndArrays()
    {
        var properties = ServiceCatalogTypeDescriber.DescribeMembers(typeof(SampleItem), ServiceCatalogPropertySources.Body);

        properties.Select(p => p.Name).Should().Equal("id", "name", "description", "amount", "status", "tagIds", "parent");

        properties.Single(p => p.Name == "id").Should().Match<ServiceCatalogPropertyDto>(p => p.ClrType == ServiceCatalogClrTypes.Guid && !p.IsNullable);
        properties.Single(p => p.Name == "name").IsNullable.Should().BeFalse();
        properties.Single(p => p.Name == "description").IsNullable.Should().BeTrue();
        properties.Single(p => p.Name == "amount").Should().Match<ServiceCatalogPropertyDto>(p => p.ClrType == ServiceCatalogClrTypes.Decimal && p.IsNullable);
        properties.Single(p => p.Name == "status").Should().Match<ServiceCatalogPropertyDto>(p => p.ClrType == ServiceCatalogClrTypes.Enum && p.EnumTypeName == nameof(SampleStatus));
        properties.Single(p => p.Name == "tagIds").Should().Match<ServiceCatalogPropertyDto>(p => p.ClrType == ServiceCatalogClrTypes.Guid && p.IsArray);
    }

    private sealed class SampleListDto : BiUM.Infrastructure.Common.Models.BaseDto
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class SampleExamDto
    {
        public bool Test { get; set; }
    }

    [Fact]
    public void DescribeMembers_ShouldSkipBiUMBaseTestProperty_ButKeepOwnTestProperty()
    {
        var listProperties = ServiceCatalogTypeDescriber.DescribeMembers(typeof(SampleListDto), ServiceCatalogPropertySources.Response).Select(p => p.Name).ToList();
        var examProperties = ServiceCatalogTypeDescriber.DescribeMembers(typeof(SampleExamDto), ServiceCatalogPropertySources.Body).Select(p => p.Name).ToList();

        listProperties.Should().Contain(["code", "id", "createdBy", "active"]);
        listProperties.Should().NotContain("test");
        examProperties.Should().Equal("test");
    }

    [Fact]
    public void DescribeMembers_ShouldStopOnRecursiveTypes()
    {
        var properties = ServiceCatalogTypeDescriber.DescribeMembers(typeof(SampleItem), ServiceCatalogPropertySources.Response);

        var parent = properties.Single(p => p.Name == "parent");

        parent.ClrType.Should().Be(ServiceCatalogClrTypes.Object);
        parent.Children.Should().BeEmpty();
    }

    [Fact]
    public void UnwrapResponseType_ShouldResolveApiResponseAndPaginatedValues()
    {
        ServiceCatalogTypeDescriber.UnwrapResponseType(typeof(Task<ApiResponse<SampleItem>>))
            .Should().Be((typeof(SampleItem), false));

        var (valueType, isPaginated) = ServiceCatalogTypeDescriber.UnwrapResponseType(typeof(Task<PaginatedApiResponse<SampleItem>>));

        isPaginated.Should().BeTrue();
        ServiceCatalogTypeDescriber.GetCollectionElementType(valueType!).Should().Be(typeof(SampleItem));

        ServiceCatalogTypeDescriber.UnwrapResponseType(typeof(Task<ApiResponse>)).ValueType.Should().BeNull();
    }
}