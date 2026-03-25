using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PdfEditor.Services;

/// <summary>
/// 启发式判断“扫描件/纯图片型 PDF”：页面几乎无可选中文本且存在渲染级图片时，计为扫描页。
/// </summary>
public static class PdfScanDetector
{
    private const int MaxCharsForScannedPage = 48;
    private const double ScannedPageRatioThreshold = 0.55;

    public static bool IsScannedPdf(string pdfPath)
    {
        using var reader = new PdfReader(pdfPath);
        using var pdf = new PdfDocument(reader);
        return IsScannedPdf(pdf);
    }

    public static bool IsScannedPdf(PdfDocument pdf)
    {
        int n = pdf.GetNumberOfPages();
        if (n <= 0)
            return false;

        int scannedVotes = 0;
        for (int i = 1; i <= n; i++)
        {
            var page = pdf.GetPage(i);
            string text = PdfTextExtractor.GetTextFromPage(page).Trim();
            bool hasRenderedImage = PageHasRenderedImage(page);
            if (text.Length <= MaxCharsForScannedPage && hasRenderedImage)
                scannedVotes++;
        }

        return scannedVotes >= Math.Ceiling(n * ScannedPageRatioThreshold);
    }

    private static bool PageHasRenderedImage(PdfPage page)
    {
        var listener = new RenderImageListener();
        var processor = new PdfCanvasProcessor(listener);
        processor.ProcessPageContent(page);
        return listener.ImageRenderCount > 0;
    }

    private sealed class RenderImageListener : IEventListener
    {
        public int ImageRenderCount { get; private set; }

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type == EventType.RENDER_IMAGE)
                ImageRenderCount++;
        }

        public ICollection<EventType> GetSupportedEvents() =>
            new[] { EventType.RENDER_IMAGE };
    }
}
