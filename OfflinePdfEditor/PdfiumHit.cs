using PdfiumViewer;

namespace OfflinePdfEditor;

/// <summary>
/// 通过反射调用 PdfRenderer.PointToPdf，兼容不同 PdfiumViewer 分支的属性命名（Page / PageIndex / PageNumber）。
/// </summary>
internal static class PdfiumHit
{
    public static bool TryHit(Control renderer, Point clientLocationOnRenderer, out int page0, out float xBl, out float yBl)
    {
        page0 = 0;
        xBl = yBl = 0;
        try
        {
            object? ppt = renderer.GetType().GetMethod("PointToPdf", new[] { typeof(Point) })?.Invoke(renderer, new object[] { clientLocationOnRenderer });
            if (ppt == null)
                return false;

            page0 = ReadPageIndex0(ppt);
            if (!TryReadLocation(ppt, out xBl, out yBl))
                return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Control? FindRenderer(PdfViewer viewer)
    {
        foreach (Control c in viewer.Controls)
        {
            if (c.GetType().Name == "PdfRenderer")
                return c;
        }

        return null;
    }

    private static int ReadPageIndex0(object pdfPoint)
    {
        var t = pdfPoint.GetType();
        var pi = t.GetProperty("PageIndex") ?? t.GetProperty("Page");
        if (pi != null)
            return Math.Max(0, Convert.ToInt32(pi.GetValue(pdfPoint)!));

        var pn = t.GetProperty("PageNumber");
        if (pn != null)
            return Math.Max(0, Convert.ToInt32(pn.GetValue(pdfPoint)!) - 1);

        return 0;
    }

    private static bool TryReadLocation(object pdfPoint, out float x, out float y)
    {
        x = y = 0;
        var t = pdfPoint.GetType();
        object? loc = t.GetProperty("Location")?.GetValue(pdfPoint);
        if (loc == null)
            return false;

        var lt = loc.GetType();
        x = Convert.ToSingle(lt.GetProperty("X")!.GetValue(loc)!);
        y = Convert.ToSingle(lt.GetProperty("Y")!.GetValue(loc)!);
        return true;
    }
}
