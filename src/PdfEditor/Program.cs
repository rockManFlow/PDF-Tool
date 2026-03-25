using System.Reflection;
using System.Windows.Forms;

namespace PdfEditor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // 含中文等 CJK 编码的 PDF 会引用 UniGB-UTF16-H 等 CMap；资源在 itext.font_asian 中，需尽早加载该程序集
        try
        {
            Assembly.Load(new AssemblyName("itext.font_asian"));
        }
        catch
        {
            // 未部署 font-asian 包时忽略；此时打开部分中文 PDF 仍可能失败
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
