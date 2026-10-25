using BiUM.Contract.Models;
using BiUM.Infrastructure.Persistence.ValueConverters;
using BiUM.Specialized.Services.Serialization;
using FluentAssertions;
using MemoryPack;
using Xunit;

namespace BiUM.Tests.Infrastructure;

public sealed class SerializationAndConversionTests
{
    [Fact]
    public void CorrelationContextSerializer_ShouldRoundTrip()
    {
        var serializer = new CorrelationContextSerializer(MemoryPackSerializerOptions.Default);
        var context = new CorrelationContext
        {
            CorrelationId = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            LanguageId = Guid.NewGuid(),
            TimeZoneId = "Europe/Istanbul",
            TraceId = "trace-1"
        };

        var restored = serializer.Deserialize(serializer.Serialize(context));

        restored.Should().NotBeNull();
        restored!.CorrelationId.Should().Be(context.CorrelationId);
        restored.ApplicationId.Should().Be(context.ApplicationId);
        restored.TenantId.Should().Be(context.TenantId);
        restored.LanguageId.Should().Be(context.LanguageId);
        restored.TimeZoneId.Should().Be("Europe/Istanbul");
        restored.TraceId.Should().Be("trace-1");
    }

    [Fact]
    public void EncryptedDataValueConverter_Reversible_ShouldEncryptAndDecrypt()
    {
        var converter = new EncryptedDataValueConverter("unit-test-key", reversible: true);

        var stored = (string?)converter.ConvertToProvider("05551234567");

        stored.Should().NotBe("05551234567");
        converter.ConvertFromProvider(stored).Should().Be("05551234567");
        converter.ConvertToProvider(null).Should().BeNull();
    }

    [Fact]
    public void EncryptedDataValueConverter_ReversibleRead_ShouldKeepLegacyHashedValues()
    {
        var hashed = (string?)new EncryptedDataValueConverter("unit-test-key", reversible: false).ConvertToProvider("password");
        var reversible = new EncryptedDataValueConverter("unit-test-key", reversible: true);

        hashed.Should().Contain(":");
        reversible.ConvertFromProvider(hashed).Should().Be(hashed);
    }
}