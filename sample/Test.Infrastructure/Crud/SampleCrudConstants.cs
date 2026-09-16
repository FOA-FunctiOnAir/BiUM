using BiApp.Test.Infrastructure.DynamicApi;
using BiUM.Specialized.Services.Crud;
using System;

namespace BiApp.Test.Infrastructure.Crud;

public static class SampleCrudConstants
{
    public static readonly Guid TenantId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001");

    public static readonly Guid TitleFieldId = Guid.Parse("dddddddd-eeee-ffff-1111-222222222222");

    public static readonly Guid NotesCrudId = Guid.Parse("eeeeeeee-ffff-1111-2222-333333333333");

    public static readonly Guid NotesListApiId = Guid.Parse("ffffffff-1111-2222-3333-444444444444");

    public const string NotesCrudCode = "sample-notes";

    public const string NotesTableName = "SAMPLE_NOTES";

    public const string NotesListApiCode = "sample-notes-list";

    public const string NotesListCallUrl = "/api/base/DynamicApi/Get/sample-notes-list";

    public static string ResolveSchema() =>
        CrudSchemaHelper.ResolveSchema(SampleDynamicApiConstants.ApplicationId, TenantId);
}