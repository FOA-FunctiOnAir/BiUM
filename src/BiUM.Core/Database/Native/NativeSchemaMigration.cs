namespace BiUM.Core.Database.Native;

public abstract class NativeSchemaMigration
{
    public abstract string Database { get; }

    public abstract string Id { get; }

    public abstract string Sql { get; }
}