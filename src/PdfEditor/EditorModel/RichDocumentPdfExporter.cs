using System.Drawing;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using LayoutDocument = iText.Layout.Document;

namespace PdfEditor.EditorModel;

/// <summary>
/// 将 <see cref="RichDocument"/> 导出为 PDF（iText Layout）。依赖本机字体目录中的黑体/宋体以支持中文。
/// </summary>
public static class RichDocumentPdfExporter
{
    private const double MmToPt = 72.0 / 25.4;

    public static void Export(RichDocument doc, string pdfPath)
    {
        var marginPt = (float)(doc.MarginMm * MmToPt);
        doc.Paragraphs ??= new List<RichParagraph>();

        using var writer = new PdfWriter(pdfPath);
        using var pdf = new PdfDocument(writer);
        using var layout = new LayoutDocument(pdf, PageSize.A4);
        layout.SetMargins(marginPt, marginPt, marginPt, marginPt);

        var fontCache = new Dictionary<FontKey, PdfFont>();

        foreach (var para in doc.Paragraphs)
        {
            para.Runs ??= new List<RichRun>();
            var p = new Paragraph();
            bool any = false;
            foreach (var run in para.Runs)
            {
                if (string.IsNullOrEmpty(run.Text))
                    continue;

                PdfFont font = GetCachedPdfFont(fontCache, run.FontName ?? "Microsoft YaHei UI", run.Bold, run.Italic);
                var t = new Text(run.Text).SetFont(font).SetFontSize(run.FontSizePt);
                ApplyColor(t, run.ColorArgb);
                p.Add(t);
                any = true;
            }

            if (!any)
                p.Add(new Text(" "));

            layout.Add(p);
        }

        if (!string.IsNullOrWhiteSpace(doc.Title))
            pdf.GetDocumentInfo().SetTitle(doc.Title);
    }

    private readonly struct FontKey : IEquatable<FontKey>
    {
        public FontKey(string name, bool bold, bool italic)
        {
            Name = name;
            Bold = bold;
            Italic = italic;
        }

        public string Name { get; }
        public bool Bold { get; }
        public bool Italic { get; }

        public bool Equals(FontKey other) =>
            string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && Bold == other.Bold && Italic == other.Italic;

        public override bool Equals(object? obj) => obj is FontKey k && Equals(k);
        public override int GetHashCode() => HashCode.Combine(Name.ToLowerInvariant(), Bold, Italic);
    }

    private static PdfFont GetCachedPdfFont(Dictionary<FontKey, PdfFont> cache, string fontName, bool bold, bool italic)
    {
        var key = new FontKey(fontName, bold, italic);
        if (cache.TryGetValue(key, out var f))
            return f;

        f = CreatePdfFont(fontName, bold, italic);
        cache[key] = f;
        return f;
    }

    private static PdfFont CreatePdfFont(string fontName, bool bold, bool italic)
    {
        _ = italic;
        string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string? path = null;
        int ttcIndex = bold ? 1 : 0;

        if (fontName.Contains("YaHei", StringComparison.OrdinalIgnoreCase) ||
            fontName.Contains("微软雅黑", StringComparison.Ordinal))
        {
            path = Path.Combine(fonts, "msyh.ttc");
            if (!File.Exists(path))
                path = null;
        }
        else if (fontName.Contains("SimSun", StringComparison.OrdinalIgnoreCase) ||
                 fontName.Contains("宋体", StringComparison.Ordinal))
        {
            path = Path.Combine(fonts, "simsun.ttc");
            ttcIndex = 0;
        }

        if (path != null && File.Exists(path))
        {
            try
            {
                return PdfFontFactory.CreateFont(path + "," + ttcIndex, PdfEncodings.IDENTITY_H,
                    PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            }
            catch
            {
                try
                {
                    return PdfFontFactory.CreateFont(path + ",0", PdfEncodings.IDENTITY_H,
                        PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                }
                catch
                {
                    // fall through
                }
            }
        }

        return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
    }

    private static void ApplyColor(Text t, int argb)
    {
        Color c = Color.FromArgb(argb);
        if (c.A == 0)
            return;
        var dev = new DeviceRgb(c.R, c.G, c.B);
        t.SetFontColor(dev);
    }
}
