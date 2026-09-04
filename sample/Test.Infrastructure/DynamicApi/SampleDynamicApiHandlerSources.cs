namespace BiApp.Test.Infrastructure.DynamicApi;

public static class SampleDynamicApiHandlerSources
{
    public const string CurrencyList = """
        var items = await ctx.Db.Currencies
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.Name, c.Code })
            .ToListAsync(cancellationToken);

        return new ApiResponse { Value = items };
        """;

    public const string CurrencyListPaged = """
        var pageStart = ctx.PageStart ?? 0;
        var pageSize = ctx.PageSize ?? 10;

        var query = ctx.Db.Currencies.OrderBy(c => c.Code);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(pageStart)
            .Take(pageSize)
            .Select(c => new { c.Id, c.Name, c.Code })
            .ToListAsync(cancellationToken);

        return new PaginatedApiResponse(
            items.Select(x => (object)x).ToList(),
            total,
            (pageStart / pageSize) + 1,
            pageSize);
        """;

    public const string GetCurrencies = """
        Guid? filterId = null;

        if (ctx.Parameters.TryGetValue("id", out var idValue) && Guid.TryParse(idValue?.ToString(), out var parsedId))
        {
            filterId = parsedId;
        }

        var filterName = ctx.Parameters.TryGetValue("name", out var nameValue) ? nameValue?.ToString() : null;
        var filterCode = ctx.Parameters.TryGetValue("code", out var codeValue) ? codeValue?.ToString() : null;
        var languageId = ctx.Correlation?.LanguageId ?? Guid.Empty;

        var currencys = __db.Currencies
            .Include(c => c.CurrencyTranslations.Where(ct => ct.LanguageId == languageId))
            .Where(p =>
                (!filterId.HasValue || p.Id == filterId.Value) &&
                (string.IsNullOrWhiteSpace(filterName) || p.Name.ToLower().Contains(filterName.Trim().ToLower())) &&
                (string.IsNullOrEmpty(filterCode) || p.Code == filterCode))
            .OrderBy(c => c.Created);

        var baseQuery = PaginationQuery.ToPageBaseQuery(ctx.PageStart, ctx.PageSize);

        return await currencys.ToPaginatedListAsync(baseQuery, cancellationToken);
        """;

    public const string GetCurrency = """
        if (!ctx.Parameters.TryGetValue("id", out var idValue) || !Guid.TryParse(idValue?.ToString(), out var id) || id == Guid.Empty)
        {
            return new ApiResponse { };
        }

        var languageId = ctx.Correlation?.LanguageId ?? Guid.Empty;
        var currency = await __db.Currencies
            .Include(c => c.CurrencyTranslations.Where(ct => ct.LanguageId == languageId))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return new ApiResponse { Value = currency };
        """;
}