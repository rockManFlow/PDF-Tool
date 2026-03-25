using System.Text;

namespace PdfEditor;

/// <summary>
/// iText 会通过 <see cref="Encoding.GetEncoding(string)"/> 解析 PDF 中的编码名。
/// .NET 默认不提供 PDF 的 StandardEncoding 等名称；部分不规范 PDF 使用 /StandardEncoding 会导致打开失败。
/// </summary>
internal static class PdfTextEncodingBootstrap
{
    public static void Register()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.RegisterProvider(new PdfEncodingAliasProvider());
    }

    private sealed class PdfEncodingAliasProvider : EncodingProvider
    {
        public override Encoding? GetEncoding(int codepage) => null;

        public override Encoding? GetEncoding(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // ISO 32000 中 Standard Encoding 与 Latin-1 在常用拉丁区间接近；供 iText 走通 FontEncoding 初始化
            if (name.Equals("StandardEncoding", StringComparison.OrdinalIgnoreCase))
                return Encoding.Latin1;

            return null;
        }
    }
}
