namespace BiApp.Test.Infrastructure.Crud;

public static class SampleCrudHandlerSources
{
    public static string BuildNotesList(string schema) => $$"""
        var items = await ctx.Entity("{{schema}}", "{{SampleCrudConstants.NotesTableName}}")
            .OrderByDescending(x => x.Title)
            .Select(x => new { x.Id, x.Title })
            .ToListAsync(cancellationToken);

        return new ApiResponse { Value = items };
        """;
}