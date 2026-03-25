using System.Drawing;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using PdfRectangle = iText.Kernel.Geom.Rectangle;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;

namespace PdfEditor.Services;

/// <summary>
/// PDF 读写与修改（全部使用 iText7）。批注使用 Annotation；文本型 PDF 的“编辑/插图”在内容流末尾叠加绘制。
/// </summary>
public sealed class PdfEditorService : IDisposable
{
    private readonly string _workPath;
    private bool _disposed;

    public string WorkPath => _workPath;

    private static PdfFont CreateUiFont()
    {
        try
        {
            string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string yahei = Path.Combine(fonts, "msyh.ttc");
            if (File.Exists(yahei))
                return PdfFontFactory.CreateFont(yahei + ",0", PdfEncodings.IDENTITY_H, true);
            string simsun = Path.Combine(fonts, "simsun.ttc");
            if (File.Exists(simsun))
                return PdfFontFactory.CreateFont(simsun + ",0", PdfEncodings.IDENTITY_H, true);
        }
        catch
        {
            // 回退到标准字体（仅适合西欧字符）
        }

        return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
    }

    public PdfEditorService(string workPath)
    {
        _workPath = workPath ?? throw new ArgumentNullException(nameof(workPath));
    }

    public static PdfEditorService CreateWorkCopy(string sourcePath)
    {
        string work = Path.Combine(Path.GetTempPath(), "PdfEditor_" + Guid.NewGuid().ToString("N") + ".pdf");
        File.Copy(sourcePath, work, overwrite: true);
        return new PdfEditorService(work);
    }

    public static PdfEditorService CreateNewBlank(string path, int pages = 1)
    {
        using var writer = new PdfWriter(path);
        using var pdf = new PdfDocument(writer);
        for (int i = 0; i < Math.Max(1, pages); i++)
            pdf.AddNewPage(iText.Kernel.Geom.PageSize.A4);
        pdf.Close();
        return new PdfEditorService(path);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        try
        {
            if (File.Exists(_workPath))
                File.Delete(_workPath);
        }
        catch
        {
            // 临时文件删除失败时忽略
        }
    }

    public void SaveAs(string destinationPath)
    {
        File.Copy(_workPath, destinationPath, overwrite: true);
    }

    private void Rewrite(Action<PdfDocument> mutate)
    {
        string temp = Path.Combine(Path.GetTempPath(), "PdfEditor_rw_" + Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            using (var reader = new PdfReader(_workPath))
            using (var writer = new PdfWriter(temp))
            using (var pdf = new PdfDocument(reader, writer))
            {
                mutate(pdf);
            }

            File.Delete(_workPath);
            File.Move(temp, _workPath);
        }
        catch
        {
            if (File.Exists(temp))
                try { File.Delete(temp); } catch { /* ignore */ }
            throw;
        }
    }

    /// <summary>高亮（批注，不改变原有内容流）。</summary>
    public void AddHighlight(int page1Based, PdfRectangle pdfRect)
    {
        Rewrite(pdf =>
        {
            var page = pdf.GetPage(page1Based);
            float llx = pdfRect.GetLeft();
            float lly = pdfRect.GetBottom();
            float urx = pdfRect.GetRight();
            float ury = pdfRect.GetTop();

            float[] quad =
            {
                llx, ury, urx, ury, llx, lly, urx, lly
            };

            var annot = PdfTextMarkupAnnotation.CreateHighLight(pdfRect, quad);
            annot.SetColor(new float[] { 1f, 1f, 0f });
            annot.GetPdfObject().Put(PdfName.CA, new PdfNumber(0.35f));
            page.AddAnnotation(annot);
        });
    }

    /// <summary>手绘划线（墨迹批注）。</summary>
    public void AddInkStroke(int page1Based, IReadOnlyList<PointF> pdfPoints, float lineWidth = 1.2f)
    {
        if (pdfPoints == null || pdfPoints.Count < 2)
            return;

        float minX = pdfPoints.Min(p => p.X);
        float minY = pdfPoints.Min(p => p.Y);
        float maxX = pdfPoints.Max(p => p.X);
        float maxY = pdfPoints.Max(p => p.Y);
        var rect = new PdfRectangle(minX, minY, Math.Max(1f, maxX - minX), Math.Max(1f, maxY - minY));

        var stroke = new PdfArray();
        foreach (var pt in pdfPoints)
        {
            stroke.Add(new PdfNumber(pt.X));
            stroke.Add(new PdfNumber(pt.Y));
        }

        var inkList = new List<PdfArray> { stroke };

        Rewrite(pdf =>
        {
            var page = pdf.GetPage(page1Based);
            var ink = new PdfInkAnnotation(rect, inkList);
            var bs = new PdfDictionary();
            bs.Put(PdfName.W, new PdfNumber(lineWidth));
            ink.GetPdfObject().Put(PdfName.BS, bs);
            ink.SetColor(new float[] { 1f, 0f, 0f });
            page.AddAnnotation(ink);
        });
    }

    /// <summary>在页面顶层叠加文字（追加内容流，不改动原有内容流片段；扫描件/文本型均可用）。</summary>
    public void AddTextOverlay(int page1Based, float pdfX, float pdfY, string text, float fontSize = 14f)
    {
        Rewrite(pdf =>
        {
            var page = pdf.GetPage(page1Based);
            var font = CreateUiFont();
            var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdf);
            canvas.SaveState();
            canvas.BeginText();
            canvas.SetFontAndSize(font, fontSize);
            canvas.MoveText(pdfX, pdfY);
            canvas.ShowText(text);
            canvas.EndText();
            canvas.RestoreState();
        });
    }

    /// <summary>用白底矩形覆盖后在同一位置叠加新文字（简易“编辑文字”，文本型 PDF）。</summary>
    public void WhiteoutAndDrawText(int page1Based, PdfRectangle pdfRect, string newText, float fontSize)
    {
        Rewrite(pdf =>
        {
            var page = pdf.GetPage(page1Based);
            var font = CreateUiFont();
            var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdf);
            canvas.SaveState();
            canvas.SetFillColor(ColorConstants.WHITE);
            canvas.Rectangle(pdfRect);
            canvas.Fill();
            if (!string.IsNullOrEmpty(newText))
            {
                canvas.BeginText();
                canvas.SetFontAndSize(font, fontSize);
                canvas.MoveText(pdfRect.GetLeft(), pdfRect.GetBottom() + 2f);
                canvas.ShowText(newText);
                canvas.EndText();
            }
            canvas.RestoreState();
        });
    }

    /// <summary>插入位图（文本型 PDF）。</summary>
    public void AddImageOverlay(int page1Based, string imagePath, PdfRectangle pdfRect)
    {
        var data = ImageDataFactory.Create(imagePath);
        Rewrite(pdf =>
        {
            var page = pdf.GetPage(page1Based);
            var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdf);
            canvas.SaveState();
            canvas.AddImageFittedIntoRectangle(data, pdfRect, false);
            canvas.RestoreState();
        });
    }

    /// <summary>整页半透明文字水印（扫描件：新增内容流，不改动原有内容流片段顺序）。</summary>
    public void AddTextWatermark(string text, float opacity = 0.12f, float fontSize = 48f)
    {
        Rewrite(pdf =>
        {
            int n = pdf.GetNumberOfPages();
            var font = CreateUiFont();
            for (int i = 1; i <= n; i++)
            {
                var page = pdf.GetPage(i);
                var size = page.GetPageSize();
                float cx = size.GetWidth() / 2f;
                float cy = size.GetHeight() / 2f;
                float approxWidth = fontSize * 0.45f * text.Length;

                var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdf);
                canvas.SaveState();
                var gs = new PdfExtGState();
                gs.SetFillOpacity(opacity);
                canvas.SetExtGState(gs);
                canvas.SetFillColor(ColorConstants.GRAY);
                canvas.BeginText();
                canvas.SetFontAndSize(font, fontSize);
                canvas.MoveText(Math.Max(36f, cx - approxWidth / 2f), Math.Max(36f, cy - fontSize / 2f));
                canvas.ShowText(text);
                canvas.EndText();
                canvas.RestoreState();
            }
        });
    }
}
