using BiUM.Core.Common.Utils;
using BiUM.Specialized.Common.Utils;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.Utils;

public sealed class CoreUtilsTests
{
    private enum SampleColor
    {
        Red,
        Blue
    }

    [Theory]
    [InlineData("DataType-Guid", "dac8f46f-a8be-5506-ac5d-a68a1ec87cd1")]
    [InlineData("DataType-DateOnly", "83f05349-b162-5b50-999c-9f97da79985f")]
    [InlineData("Component-TextInput", "486430bb-b521-53da-aa92-79030a8ad8c6")]
    public void GuidGenerator_NewGuid_ShouldBeDeterministicUuidV5(string seed, string expected)
    {
        GuidGenerator.NewGuid(seed).Should().Be(Guid.Parse(expected));
        GuidGenerator.NewGuid(seed).Should().Be(GuidGenerator.NewGuid(seed));
    }

    [Fact]
    public void GuidGenerator_New_ShouldCreateSortableVersion7Guids()
    {
        var first = GuidGenerator.New();

        Thread.Sleep(2);

        var second = GuidGenerator.New();

        first.ToString()[14].Should().Be('7');
        second.Should().NotBe(first);
        string.CompareOrdinal(first.ToString(), second.ToString()).Should().BeNegative();
    }

    [Fact]
    public void CommaSeparatedValueHelper_ShouldParseTrimmedValues()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        CommaSeparatedValueHelper.ParseAsGuids($" {first} ,{second},, ").Should().Equal(first, second);
        CommaSeparatedValueHelper.ParseAsIntegers("1, 2,3").Should().Equal(1, 2, 3);
        CommaSeparatedValueHelper.ParseAsStrings(" a , b ").Should().Equal("a", "b");
        CommaSeparatedValueHelper.ParseAsEnums<SampleColor>("red,BLUE").Should().Equal(SampleColor.Red, SampleColor.Blue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("1,x,3")]
    public void CommaSeparatedValueHelper_ShouldReturnEmpty_WhenTextIsEmptyOrAnyPartIsInvalid(string? text)
    {
        CommaSeparatedValueHelper.ParseAsIntegers(text).Should().BeEmpty();
    }

    [Fact]
    public void EncryptionHelper_ShouldEncryptAndDecryptWithRandomIv()
    {
        const string key = "unit-test-key";

        var first = EncryptionHelper.Encrypt("05551234567", key);
        var second = EncryptionHelper.Encrypt("05551234567", key);

        first.Should().NotBe(second);
        EncryptionHelper.Decrypt(first, key).Should().Be("05551234567");
        EncryptionHelper.Decrypt(second, key).Should().Be("05551234567");
    }

    [Fact]
    public void EncryptionHelper_ShouldHashAndVerify()
    {
        var hash = EncryptionHelper.Hash("secret");

        hash.Should().Contain(":");
        EncryptionHelper.Verify("secret", hash).Should().BeTrue();
        EncryptionHelper.Verify("Secret", hash).Should().BeFalse();
        EncryptionHelper.Verify("secret", "not-a-hash").Should().BeFalse();
        EncryptionHelper.Verify("", hash).Should().BeFalse();
    }

    [Fact]
    public void EncryptionHelper_ProtectAndUnprotect_ShouldFollowReversibleFlag()
    {
        const string key = "unit-test-key";

        var encrypted = EncryptionHelper.Protect("value", key, reversible: true);
        var hashed = EncryptionHelper.Protect("value", key, reversible: false);

        EncryptionHelper.Unprotect(encrypted, key, reversible: true).Should().Be("value");
        EncryptionHelper.Unprotect(hashed, key, reversible: false).Should().Be(hashed);
        EncryptionHelper.Verify("value", hashed).Should().BeTrue();
        EncryptionHelper.Protect("", key, reversible: true).Should().BeEmpty();
    }

    [Theory]
    [InlineData("ServiceCatalogMappingRequestedEvent", "service_catalog_mapping_requested_event")]
    [InlineData("GradeCreatedEvent", "grade_created_event")]
    [InlineData("AIJobEvent", "a_i_job_event")]
    [InlineData("event", "event")]
    [InlineData("", "")]
    public void ToSnakeCase_ShouldKeepRoutingKeyFormat(string input, string expected)
    {
        input.ToSnakeCase().Should().Be(expected);
    }

    [Theory]
    [InlineData("d1e55702-e670-5d44-a6b0-7bf7ee934a73", "d1e55702-e670-5d44-a6b0-7bf7ee934a73")]
    [InlineData("not-a-guid", "00000000-0000-0000-0000-000000000000")]
    public void ToGuid_ShouldReturnEmptyForInvalidText(string input, string expected)
    {
        input.ToGuid().Should().Be(Guid.Parse(expected));
        input.ToNullableGuid().Should().Be(expected == Guid.Empty.ToString() ? null : Guid.Parse(expected));
    }

    [Theory]
    [InlineData("education/assessment", "education-assessment")]
    [InlineData("/api/Education/Coach/", "education-coach")]
    [InlineData("api/parameters", "parameters")]
    [InlineData(" configuration ", "configuration")]
    [InlineData(null, "")]
    public void MicroserviceCodeHelper_FromRootPath_ShouldBuildDomainKey(string? rootPath, string expected)
    {
        MicroserviceCodeHelper.FromRootPath(rootPath).Should().Be(expected);
    }

    [Theory]
    [InlineData("0", "Sıfır Türk Lirası")]
    [InlineData("1", "Bir Türk Lirası")]
    [InlineData("1000", "Bin Türk Lirası")]
    [InlineData("1234.56", "Bin İki Yüz Otuz Dört Türk Lirası Elli Altı Kuruş")]
    [InlineData("0.05", "Beş Kuruş")]
    [InlineData("-15", "Eksi On Beş Türk Lirası")]
    [InlineData("2000000", "İki Milyon Türk Lirası")]
    [InlineData("1500000000", "Bir Milyar Beş Yüz Milyon Türk Lirası")]
    [InlineData("3000000001", "Üç Milyar Bir Türk Lirası")]
    public void MoneyHelper_ShouldWriteAmountInTurkish(string amount, string expected)
    {
        MoneyHelper.ConvertMoneyToWords(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)).Should().Be(expected);
    }
}