using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using iText.Kernel.Pdf;

namespace PdfEditor.Services;

/// <summary>
/// 使用 Docnet.Core（PDFium、.NET Standard 2.0）栅格化页面供预览；页面尺寸（pt）用 iText 读取以与编辑坐标一致。
/// </summary>
public static class PdfRasterPreview
{
    public static int GetPageCount(string pdfPath)
    {
        using var reader = new PdfReader(pdfPath);
        using var pdf = new PdfDocument(reader);
        return pdf.GetNumberOfPages();
    }

    public static (float widthPt, float heightPt) GetPageSizePts(string pdfPath, int page0)
    {
        using var reader = new PdfReader(pdfPath);
        using var pdf = new PdfDocument(reader);
        var box = pdf.GetPage(page0 + 1).GetPageSize();
        return (box.GetWidth(), box.GetHeight());
    }

    /// <summary>按视口宽度栅格化一页，保持纵横比。</summary>
    public static Bitmap RenderPage(string pdfPath, int page0, int viewportWidth, int viewportHeight)
    {
        var (pw, ph) = GetPageSizePts(pdfPath, page0);
        if (pw <= 0 || ph <= 0)
            throw new InvalidOperationException("无效的页面尺寸。");

        int maxW = Math.Max(320, viewportWidth - 4);
        int maxH = Math.Max(240, viewportHeight - 4);
        int targetW = maxW;
        int targetH = (int)(targetW * ph / pw);
        if (targetH > maxH)
        {
            targetH = maxH;
            targetW = (int)(targetH * pw / ph);
        }

        using var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(targetW, targetH));
        using var pageReader = docReader.GetPageReader(page0);
        int w = pageReader.GetPageWidth();
        int h = pageReader.GetPageHeight();
        byte[] bytes = pageReader.GetImage();

        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int copy = Math.Min(bytes.Length, Math.Abs(data.Stride) * h);
            Marshal.Copy(bytes, 0, data.Scan0, copy);
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        return bmp;
    }

    /// <summary>按固定栏宽渲染整页（用于纵向连续滚动列表）。</summary>
    public static Bitmap RenderPageForContentWidth(string pdfPath, int page0, int contentWidth)
    {
        return RenderPage(pdfPath, page0, contentWidth, 100_000);
    }
}
