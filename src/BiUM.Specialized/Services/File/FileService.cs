using BiUM.Infrastructure.Services.File;
using SimpleHtmlToPdf;
using SimpleHtmlToPdf.Interfaces;
using SimpleHtmlToPdf.Settings;
using SimpleHtmlToPdf.Settings.Enums;
using SimpleHtmlToPdf.UnmanagedHandler;

namespace BiUM.Specialized.Services.File;

public class FileService : IFileService
{
    private readonly IConverter _converter;

    public FileService(IConverter converter)
    {
        _converter = converter;
    }

    public byte[] HtmlToPdf(string htmlDocument)
    {
        var doc = new HtmlToPdfDocument
        {
            GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4Plus,
            },
            Objects = {
                new ObjectSettings
                {
                    HtmlContent = htmlDocument,
                    WebSettings = { DefaultEncoding = "utf-8" },
                },
            }
        };

        return _converter.Convert(doc);
    }
}