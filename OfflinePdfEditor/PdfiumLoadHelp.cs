namespace OfflinePdfEditor;

/// <summary>
/// pdfium.dll 加载失败时的说明（依赖 VC++ 运行库 + 输出目录需含本机库）。
/// </summary>
internal static class PdfiumLoadHelp
{
    internal static string Format(Exception ex)
    {
        string all = ex.ToString();
        bool likelyPdfium = all.Contains("pdfium", StringComparison.OrdinalIgnoreCase)
                          || all.Contains("8007007E", StringComparison.OrdinalIgnoreCase)
                          || all.Contains("DllNotFoundException", StringComparison.OrdinalIgnoreCase);

        if (!likelyPdfium)
            return ex.Message;

        return
            "无法加载 PDF 显示引擎 pdfium.dll。\r\n\r\n" +
            "请依次检查：\r\n" +
            "1）安装 Microsoft Visual C++ 2015–2022 可再发行组件（x64）：\r\n" +
            "   https://aka.ms/vs/17/release/vc_redist.x64.exe\r\n\r\n" +
            "2）从带 win-x64 的输出目录运行程序（例如：bin\\Release\\net8.0-windows10.0.17763.0\\win-x64\\），\r\n" +
            "   并确认该目录下存在 pdfium.dll（与 OfflinePdfEditor.exe 同级）。\r\n\r\n" +
            "3）若使用「单文件发布」，需带 IncludeNativeLibrariesForSelfExtract；不要用裁剪过度的发布选项。\r\n\r\n" +
            "原始错误：\r\n" + ex.Message;
    }
}
