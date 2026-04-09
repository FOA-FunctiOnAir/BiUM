namespace BiUM.Infrastructure.Common.Services;

public interface IRandomGeneratorService
{
    public string GetNewRandomPassword(string? defaultPassword, int length = 6);
}