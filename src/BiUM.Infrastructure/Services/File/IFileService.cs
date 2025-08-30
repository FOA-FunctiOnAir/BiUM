namespace BiUM.Infrastructure.Services.File;

public interface IFileService
{
    byte[] HtmlToPdf(string htmlDocument);
}