using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace OfflinePdfEditor.Services;

/// <summary>
/// 使用 PDFsharp 在现有 PDF 上追加绘制（MIT 许可）。显示由 PdfiumViewer/PDFium 负责。
/// </summary>
public static class PdfEditService
{
    private static double BottomLeftYToTopLeft(double pageHeightPt, double yFromBottom) =>
        pageHeightPt - yFromBottom;

    public static void DrawText(
        string pdfPath,
        int pageIndex,
        double xFromLeft,
        double yFromBottom,
        string text,
        string fontFamily,
        double emSize,
        bool bold,
        bool italic,
        bool underline,
        XColor color)
    {
        if (string.IsNullOrEmpty(text))
            return;

        using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify);
        if (pageIndex < 0 || pageIndex >= doc.PageCount)
            return;

        var page = doc.Pages[pageIndex];
        double h = page.Height.Point;

        XFontStyleEx st = XFontStyleEx.Regular;
        if (bold) st |= XFontStyleEx.Bold;
        if (italic) st |= XFontStyleEx.Italic;
        if (underline) st |= XFontStyleEx.Underline;

        var font = new XFont(fontFamily, emSize, st);
        var brush = new XSolidBrush(color);

        using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
        {
            double yTop = BottomLeftYToTopLeft(h, yFromBottom) - emSize * 0.85;
            gfx.DrawString(text, font, brush, xFromLeft, yTop, XStringFormats.TopLeft);
        }

        SaveDoc(doc, pdfPath);
    }

    public static void DrawHighlight(string pdfPath, int pageIndex, double x1bl, double y1bl, double x2bl, double y2bl)
    {
        using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify);
        if (pageIndex < 0 || pageIndex >= doc.PageCount)
            return;

        var page = doc.Pages[pageIndex];
        double h = page.Height.Point;

        double xl = Math.Min(x1bl, x2bl);
        double xr = Math.Max(x1bl, x2bl);
        double yt = BottomLeftYToTopLeft(h, Math.Max(y1bl, y2bl));
        double yb = BottomLeftYToTopLeft(h, Math.Min(y1bl, y2bl));
        double w = Math.Max(1, xr - xl);
        double hh = Math.Max(1, yb - yt);

        using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
        using (var brush = new XSolidBrush(XColor.FromArgb(120, 255, 255, 0)))
            gfx.DrawRectangle(brush, xl, yt, w, hh);

        SaveDoc(doc, pdfPath);
    }

    public static void DrawInk(string pdfPath, int pageIndex, IReadOnlyList<(double x, double y)> pointsBottomLeft)
    {
        if (pointsBottomLeft.Count < 2)
            return;

        using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify);
        if (pageIndex < 0 || pageIndex >= doc.PageCount)
            return;

        var page = doc.Pages[pageIndex];
        double h = page.Height.Point;
        using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
        using var pen = new XPen(XColors.Red, 1.2);
        for (int i = 1; i < pointsBottomLeft.Count; i++)
        {
            var a = pointsBottomLeft[i - 1];
            var b = pointsBottomLeft[i];
            var pa = new XPoint(a.x, BottomLeftYToTopLeft(h, a.y));
            var pb = new XPoint(b.x, BottomLeftYToTopLeft(h, b.y));
            gfx.DrawLine(pen, pa, pb);
        }

        SaveDoc(doc, pdfPath);
    }

    public static void DrawImage(string pdfPath, int pageIndex, double xLeft, double yBottom, string imagePath)
    {
        using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify);
        if (pageIndex < 0 || pageIndex >= doc.PageCount)
            return;

        var page = doc.Pages[pageIndex];
        double h = page.Height.Point;

        using var img = XImage.FromFile(imagePath);
        double rx = Math.Max(96, img.HorizontalResolution);
        double ry = Math.Max(96, img.VerticalResolution);
        double drawW = img.PixelWidth * 72.0 / rx;
        double drawH = img.PixelHeight * 72.0 / ry;
        double yTop = BottomLeftYToTopLeft(h, yBottom) - drawH;

        using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
            gfx.DrawImage(img, xLeft, yTop, drawW, drawH);

        SaveDoc(doc, pdfPath);
    }

    public static void DrawTextBox(string pdfPath, int pageIndex, double xLeft, double yBottom, double widthPt, double heightPt,
        string text, string fontFamily, double emSize, XColor border, XColor fore)
    {
        using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify);
        if (pageIndex < 0 || pageIndex >= doc.PageCount)
            return;

        var page = doc.Pages[pageIndex];
        double h = page.Height.Point;
        double yTop = BottomLeftYToTopLeft(h, yBottom) - heightPt;

        using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
        {
            gfx.DrawRectangle(new XPen(border, 0.8), XBrushes.White, xLeft, yTop, widthPt, heightPt);
            var font = new XFont(fontFamily, emSize, XFontStyleEx.Regular);
            var rect = new XRect(xLeft + 2, yTop + 2, widthPt - 4, heightPt - 4);
            gfx.DrawString(text, font, new XSolidBrush(fore), rect, XStringFormats.TopLeft);
        }

        SaveDoc(doc, pdfPath);
    }

    public static void DrawCommentBubble(string pdfPath, int pageIndex, double xLeft, double yBottom, string note)
    {
        const double w = 180;
        const double hh = 64;
        DrawTextBox(pdfPath, pageIndex, xLeft, yBottom, w, hh, note, "Verdana", 9, XColors.DarkGoldenrod, XColors.Black);
    }

    public static void DeletePage(string pdfPath, int pageIndex)
    {
        using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify);
        if (pageIndex < 0 || pageIndex >= doc.PageCount || doc.PageCount <= 1)
            return;
        doc.Pages.RemoveAt(pageIndex);
        SaveDoc(doc, pdfPath);
    }

    private static void SaveDoc(PdfDocument doc, string path)
    {
        using var ms = new MemoryStream();
        doc.Save(ms);
        File.WriteAllBytes(path, ms.ToArray());
    }
}
