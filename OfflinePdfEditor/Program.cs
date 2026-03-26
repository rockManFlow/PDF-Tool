using System.Text;
using PdfSharp.Fonts;

namespace OfflinePdfEditor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }
        catch
        {
            // 旧版 PDFsharp 无此属性时忽略
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
