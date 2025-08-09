using AutoMapper;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using BiUM.Core.Database;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;

namespace BiUM.Specialized.Database;

public class BaseRepository : IBaseRepository
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContext _baseContext;

    public readonly ICurrentUserService _currentUserService;
    public readonly IMapper _mapper;

    public readonly BiAppOptions _biAppOptions;

    public BaseRepository(IServiceProvider serviceProvider, IDbContext baseContext)
    {
        _baseContext = baseContext;
        _serviceProvider = serviceProvider;

        _currentUserService = _serviceProvider.GetRequiredService<ICurrentUserService>();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();

        _biAppOptions = _serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;
    }

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _baseContext.SaveChangesAsync(cancellationToken);
    }

    public virtual IApiResponse AddMessage(IApiResponse response, IResponseMessage message)
    {
        response.AddMessage(message);

        return response;
    }

    public virtual IApiResponse AddMessage(IApiResponse response, IReadOnlyList<IResponseMessage> messages)
    {
        response.AddMessage(messages);

        return response;
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        CancellationToken cancellationToken)

    {
        return await AddMessage(response, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)

    {
        return await AddMessage(response, code, string.Empty, severity, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        CancellationToken cancellationToken)

    {
        return await AddMessage(response, code, GetFullExceptionMessage(exception), MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)

    {
        return await AddMessage(response, code, GetFullExceptionMessage(exception), severity, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)

    {
        var translation = await _baseContext.DomainTranslations
            .AsNoTracking()
            .Include(dt => dt.DomainTranslationDetails.Where(dtd => dtd.LanguageId == _currentUserService.LanguageId))
            .Where(x => x.Code.Equals(code) && x.ApplicationId == _currentUserService.ApplicationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (translation is null || translation.DomainTranslationDetails is null || translation.DomainTranslationDetails.Count == 0)
        {
            response.AddMessage(new ResponseMessage
            {
                Code = $"{_biAppOptions.Domain}.{code}",
                Exception = exception,
                Severity = severity
            });

            return response;
        }

        var message = translation.DomainTranslationDetails.First().Text;

        response.AddMessage(new ResponseMessage
        {
            Code = $"{_biAppOptions.Domain}.{code}",
            Message = message,
            Exception = exception,
            Severity = severity
        });

        return response;
    }

    public virtual async Task SaveTranslations(
        DbSet<TranslationBaseEntity> dbSetTranslationEntity,
        Guid recordId,
        string columnName,
        IReadOnlyList<BaseTranslationDto> translations,
        CancellationToken cancellationToken)

    {
        if (translations is null || translations.Count == 0)
        {
            return;
        }

        foreach (var translation in translations)
        {
            if (translation._rowStatus == RowStatuses.New)
            {
                var applicationTranslation = translation.ToTranslationEntity<TranslationBaseEntity>(recordId, columnName);

                dbSetTranslationEntity.Add(applicationTranslation);
            }
            else if (translation._rowStatus == RowStatuses.Edited)
            {
                var applicationTranslation = await dbSetTranslationEntity.FirstOrDefaultAsync(f => f.Id == translation.Id, cancellationToken);

                applicationTranslation!.Translation = translation.Translation;

                dbSetTranslationEntity.Update(applicationTranslation);
            }
            else if (translation._rowStatus == RowStatuses.Deleted)
            {
                var applicationTranslation = await dbSetTranslationEntity.FirstOrDefaultAsync(f => f.Id == translation.Id, cancellationToken);

                dbSetTranslationEntity.Remove(applicationTranslation!);
            }
        }
    }

    public static string GetFullExceptionMessage(Exception ex)
    {
        if (ex is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var level = 0;

        while (ex is not null && level < 4)
        {
            sb.AppendLine($"[Level {level}] {ex.GetType().FullName}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");
            sb.AppendLine(new string('-', 80));

            ex = ex.InnerException;
            level++;
        }

        return sb.ToString();
    }
}