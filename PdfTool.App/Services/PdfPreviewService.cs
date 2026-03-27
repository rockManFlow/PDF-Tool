using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PdfTool.App.Services;

/// <summary>
/// Uses iText 7 to read PDF page text for on-screen preview (community edition has no built-in raster renderer).
/// </summary>
internal static class PdfPreviewService
{
    public static IReadOnlyList<(int PageNumber, string Text)> LoadPageTexts(string pdfPath)
    {
        var list = new List<(int, string)>();
        using var reader = new PdfReader(pdfPath);
        using var pdfDoc = new PdfDocument(reader);
        var n = pdfDoc.GetNumberOfPages();
        for (var i = 1; i <= n; i++)
        {
            var page = pdfDoc.GetPage(i);
            var strategy = new LocationTextExtractionStrategy();
            var text = PdfTextExtractor.GetTextFromPage(page, strategy);
            list.Add((i, text));
        }

        return list;
    }
}
