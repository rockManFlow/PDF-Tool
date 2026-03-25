using System.Drawing;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using PdfRectangle = iText.Kernel.Geom.Rectangle;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Layout;
using iText.Layout.Element;

namespace PdfEditor.Services;

/// <summary>
/// PDF 读写与修改（全部使用 iText7）。高亮/划线写入内容流以便栅格预览可见；水印与文字叠加同内容流。
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
                return PdfFontFactory.CreateFont(yahei + ",0", PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            string simsun = Path.Combine(fonts, "simsun.ttc");
            if (File.Exists(simsun))
                return PdfFontFactory.CreateFont(simsun + ",0", PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
        }
        catch
        {
            // 回退到标准字体（仅适合西欧字符）
        }

        return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
    }

    /// <summary>
    /// 使用 Layout 在指定矩形内写入 Unicode 文本。
    /// 使用「局部 Rectangle + Canvas」而非 Paragraph.SetFixedPosition：后者在 NewContentStreamAfter 上易与页码关联异常导致不绘制。
    /// </summary>
    private static void LayoutDrawText(PdfDocument pdfDoc, PdfPage page, PdfFont font, float fontSize, string text, PdfRectangle layoutArea)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var pdfCanvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDoc);
        using (var layoutCanvas = new Canvas(pdfCanvas, layoutArea))
        {
            layoutCanvas.SetFont(font);
            layoutCanvas.SetFontSize(fontSize);
            layoutCanvas.Add(new Paragraph(text));
        }
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

    /// <summary>
    /// 高亮：半透明黄色矩形，追加到页面内容流末尾。
    /// （若仅用文本批注/墨迹批注，Docnet 等栅格预览常不绘制注释层，看起来像「没生效」。）
    /// </summary>
    public void AddHighlight(int page1Based, PdfRectangle pdfRect)
    {
        Rewrite(pdf =>
        {
            var page = pdf.GetPage(page1Based);
            var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdf);
            canvas.SaveState();
            var gs = new PdfExtGState();
            gs.SetFillOpacity(0.35f);
            canvas.SetExtGState(gs);
            canvas.SetFillColor(ColorConstants.YELLOW);
            canvas.Rectangle(pdfRect);
            canvas.Fill();
            canvas.RestoreState();
        });
    }

    /// <summary>手绘划线：红色折线，追加到内容流末尾（栅格预览可见）。</summary>
    public void AddInkStroke(int page1Based, IReadOnlyList<PointF> pdfPoints, float lineWidth = 1.2f)
    {
        if (pdfPoints == null || pdfPoints.Count < 2)
            return;

        Rewrite(pdf =>
        {
            var page = pdf.GetPage(page1Based);
            var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdf);
            canvas.SaveState();
            canvas.SetStrokeColor(ColorConstants.RED);
            canvas.SetLineWidth(lineWidth);
            canvas.MoveTo(pdfPoints[0].X, pdfPoints[0].Y);
            for (int i = 1; i < pdfPoints.Count; i++)
                canvas.LineTo(pdfPoints[i].X, pdfPoints[i].Y);
            canvas.Stroke();
            canvas.RestoreState();
        });
    }

    /// <summary>在页面顶层叠加文字（追加内容流，不改动原有内容流片段；扫描件/文本型均可用）。</summary>
    public void AddTextOverlay(int page1Based, float pdfX, float pdfY, string text, float fontSize = 14f)
    {
        Rewrite(pdf =>
        {
            var page = pdf.GetPage(page1Based);
            var font = CreateUiFont();
            var box = page.GetPageSize();
            float w = Math.Max(40f, box.GetWidth() - pdfX);
            float h = Math.Max(72f, box.GetHeight() - pdfY - 4f);
            var area = new PdfRectangle(pdfX, pdfY, w, h);
            LayoutDrawText(pdf, page, font, fontSize, text, area);
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
            canvas.RestoreState();
            if (!string.IsNullOrEmpty(newText))
            {
                float bw = Math.Max(40f, pdfRect.GetWidth());
                float bh = Math.Max(fontSize * 3f, pdfRect.GetHeight() + 6f);
                var area = new PdfRectangle(pdfRect.GetLeft(), pdfRect.GetBottom(), bw, bh);
                LayoutDrawText(pdf, page, font, fontSize, newText, area);
            }
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

                var pdfCanvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdf);
                pdfCanvas.SaveState();
                var gs = new PdfExtGState();
                gs.SetFillOpacity(opacity);
                pdfCanvas.SetExtGState(gs);
                float llx = Math.Max(36f, cx - approxWidth / 2f - 18f);
                float lly = Math.Max(36f, cy - fontSize * 1.1f);
                float wBox = Math.Min(size.GetWidth() - 72f, approxWidth + 36f);
                float hBox = fontSize * 2.5f;
                var warea = new PdfRectangle(llx, lly, wBox, hBox);
                using (var layoutCanvas = new Canvas(pdfCanvas, warea))
                {
                    layoutCanvas.SetFont(font);
                    layoutCanvas.SetFontSize(fontSize);
                    layoutCanvas.Add(new Paragraph(text).SetFontColor(ColorConstants.GRAY));
                }

                pdfCanvas.RestoreState();
            }
        });
    }
}
