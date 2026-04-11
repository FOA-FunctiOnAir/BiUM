using BiUM.Specialized.Common.Translation;
using BiUM.Specialized.Services;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiUM.Tests.Translation;

public sealed class TranslationServiceTests
{
    [Fact]
    public async Task SaveDomainTranslationAsync_empty_application_adds_translation_not_found_message()
    {
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };

        await using var sp = BiUMServiceFactory.BuildInMemory(
            correlation,
            Guid.NewGuid().ToString("N"),
            registerRealTranslation: true);

        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ITranslationService>();

        var response = await svc.SaveDomainTranslationAsync(
            new SaveDomainTranslationCommand
            {
                ApplicationId = Guid.Empty,
                Code = "x"
            },
            CancellationToken.None);

        response.Messages.Should().Contain(m => m.Code == "translation_not_found");
    }

    [Fact]
    public async Task SaveDomainTranslationAsync_inserts_and_GetDomainTranslationAsync_round_trips()
    {
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };

        var appId = Guid.NewGuid();
        var langId = correlation.Context.LanguageId!.Value;

        await using var sp = BiUMServiceFactory.BuildInMemory(
            correlation,
            Guid.NewGuid().ToString("N"),
            registerRealTranslation: true);

        Guid translationId;

        using (var scope = sp.CreateScope())
        {
            correlation.Context = CorrelationTestHelper.CreateBpmnLike(
                correlation.Context.TenantId!.Value,
                appId,
                langId);

            var svc = scope.ServiceProvider.GetRequiredService<ITranslationService>();
            var save = await svc.SaveDomainTranslationAsync(
                new SaveDomainTranslationCommand
                {
                    Id = null,
                    ApplicationId = appId,
                    Code = "round_trip_key",
                    Translations =
                    [
                        new SaveDomainTranslationCommandDetail
                        {
                            LanguageId = langId,
                            Text = "hello"
                        }
                    ]
                },
                CancellationToken.None);

            save.Messages.Should().BeEmpty();

            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            var row = db.DomainTranslations.Single(t => t.Code == "round_trip_key");
            translationId = row.Id;
        }

        using (var scope = sp.CreateScope())
        {
            correlation.Context = CorrelationTestHelper.CreateBpmnLike(
                correlation.Context.TenantId!.Value,
                appId,
                langId);

            var svc = scope.ServiceProvider.GetRequiredService<ITranslationService>();
            var got = await svc.GetDomainTranslationAsync(translationId, CancellationToken.None);
            got.Value.Should().NotBeNull();
            got.Value!.Code.Should().Be("round_trip_key");
        }
    }
}
