using BiUM.Specialized.Database;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.Database;

public sealed class ApplyContainsFallbackTests
{
    private sealed class Child
    {
        public string? Translation { get; set; }
        public int LanguageId { get; set; }
    }

    private sealed class Sample
    {
        public string? Name { get; set; }
        public bool IsAdditional { get; set; }
        public Sample? Nested { get; set; }
        public List<Child>? Translations { get; set; }
    }

    [Theory]
    [InlineData("Nilay", "Nıl", false)]
    [InlineData("Nilay", "NIL", true)]
    [InlineData("Nilay", "NİL", true)]
    [InlineData("NILAY", "NİL", false)]
    [InlineData("NILAY", "nıl", true)]
    [InlineData("NILAY", "nil", true)]
    [InlineData("NİLAY", "nıl", false)]
    [InlineData("NİLAY", "NIL", false)]
    [InlineData("NİLAY", "NİL", true)]
    [InlineData("NİLAY", "nil", true)]
    public void ApplyContains_on_entity_matches_Turkish_and_invariant_folding_correctly(
        string storedName, string search, bool shouldMatch)
    {
        var sample = new Sample { Name = storedName };

        sample.ApplyContains(x => x.Name, search).Should().Be(shouldMatch);
    }

    [Fact]
    public void ApplyContains_does_not_throw_when_selected_field_is_null()
    {
        var sample = new Sample { Name = null };

        var act = () => sample.ApplyContains(x => x.Name, "anything");

        act.Should().NotThrow();
        sample.ApplyContains(x => x.Name, "anything").Should().BeFalse();
    }

    [Fact]
    public void ApplyContains_does_not_throw_when_null_forgiving_chain_is_actually_null()
    {
        var sample = new Sample { Nested = null };

        var act = () => sample.ApplyContains(x => x.Nested!.Name, "anything");

        act.Should().NotThrow();
        sample.ApplyContains(x => x.Nested!.Name, "anything").Should().BeFalse();
    }

    [Fact]
    public void ApplyContains_with_null_or_empty_search_always_matches()
    {
        var sample = new Sample { Name = null };

        sample.ApplyContains(x => x.Name, null).Should().BeTrue();
        sample.ApplyContains(x => x.Name, "").Should().BeTrue();
        sample.ApplyContains(x => x.Name, "   ").Should().BeTrue();
    }

    [Theory]
    [InlineData(true, "PRE-Nilay", "PRE-NIL", true)]
    [InlineData(false, "Nilay", "PRE-NIL", false)]
    public void ApplyContains_evaluates_ternary_and_string_concat_selectors(
        bool isAdditional, string name, string search, bool shouldMatch)
    {
        var sample = new Sample { IsAdditional = isAdditional, Name = name };

        sample.ApplyContains(x => x.IsAdditional ? "PRE-" + x.Name : string.Empty, search)
            .Should().Be(shouldMatch);
    }

    [Fact]
    public void ApplyAnyContains_matches_any_child_passing_the_filter()
    {
        var sample = new Sample
        {
            Translations =
            [
                new Child { Translation = "Nilay", LanguageId = 1 },
                new Child { Translation = "NİLAY", LanguageId = 2 }
            ]
        };

        sample.ApplyAnyContains(x => x.Translations!, c => c.Translation, "nil", childFilter: c => c.LanguageId == 1)
            .Should().BeTrue();

        // stored "Nilay" (language 1): "ı" (dotless) must not cross-match the "i" in "Nilay"
        sample.ApplyAnyContains(x => x.Translations!, c => c.Translation, "Nıl", childFilter: c => c.LanguageId == 1)
            .Should().BeFalse();
    }

    [Fact]
    public void ApplyAnyContains_does_not_throw_on_null_collection_or_null_children()
    {
        var emptySample = new Sample { Translations = null };

        var act = () => emptySample.ApplyAnyContains(x => x.Translations!, c => c.Translation, "nil");

        act.Should().NotThrow();
        emptySample.ApplyAnyContains(x => x.Translations!, c => c.Translation, "nil").Should().BeFalse();

        var sampleWithNullChild = new Sample
        {
            Translations = [null!, new Child { Translation = "Nilay", LanguageId = 1 }]
        };

        sampleWithNullChild.ApplyAnyContains(x => x.Translations!, c => c.Translation, "nil").Should().BeTrue();
    }
}