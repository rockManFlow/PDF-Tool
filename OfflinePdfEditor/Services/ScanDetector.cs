using System.Text;
using System.Text.RegularExpressions;

namespace OfflinePdfEditor.Services;

/// <summary>
/// 基于文件字节的轻量启发式（不解析完整 PDF 对象树）：文本操作符稀少且图像较多时视为扫描件。
/// </summary>
public static class ScanDetector
{
    public static bool IsScannedDocument(string pdfPath)
    {
        try
        {
            ReadOnlySpan<byte> bytes = File.ReadAllBytes(pdfPath);
            if (bytes.Length < 200)
                return false;

            string s = Encoding.Latin1.GetString(bytes);
            int textOps = Regex.Matches(s, @"\)\s*Tj").Count
                          + Regex.Matches(s, @"\]\s*TJ").Count
                          + Regex.Matches(s, @"\)\s*TJ\*").Count;
            int images = Regex.Matches(s, @"/Subtype\s*/Image").Count;
            int pages = Regex.Matches(s, @"/Type\s*/Page\b").Count;
            if (pages <= 0)
                pages = 1;

            double textPerPage = textOps / (double)pages;
            return textPerPage < 2.5 && images >= 2;
        }
        catch
        {
            return false;
        }
    }
}
