using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Word;
using WordApp = Microsoft.Office.Interop.Word.Application;
using WordDoc = Microsoft.Office.Interop.Word.Document;

namespace OfflinePdfEditor.Services;

/// <summary>
/// 通过本机已安装的 Microsoft Word（COM）进行 PDF↔Word 转换。
/// 版式尽量接近 Word 引擎能力；扫描件 PDF 会变成可编辑性很差的版式或图片页。
/// </summary>
public static class WordInteropConversionService
{
    public static void PdfToDocx(string pdfPath, string docxPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(pdfPath);
        ArgumentException.ThrowIfNullOrEmpty(docxPath);
        if (!File.Exists(pdfPath))
            throw new FileNotFoundException("找不到 PDF 文件。", pdfPath);

        string ext = Path.GetExtension(docxPath);
        if (!ext.Equals(".docx", StringComparison.OrdinalIgnoreCase))
            docxPath = Path.ChangeExtension(docxPath, ".docx");

        WordApp? app = null;
        WordDoc? doc = null;
        try
        {
            app = new WordApp { Visible = false, DisplayAlerts = WdAlertLevel.wdAlertsNone };
            doc = app.Documents.Open(
                FileName: pdfPath,
                ConfirmConversions: false,
                ReadOnly: true,
                AddToRecentFiles: false);
            doc.SaveAs2(FileName: docxPath, FileFormat: WdSaveFormat.wdFormatXMLDocument);
        }
        finally
        {
            if (doc != null)
            {
                try
                {
                    doc.Close(SaveChanges: false);
                }
                catch
                {
                    // ignore
                }

                Marshal.ReleaseComObject(doc);
            }

            if (app != null)
            {
                try
                {
                    app.Quit(SaveChanges: false);
                }
                catch
                {
                    // ignore
                }

                Marshal.ReleaseComObject(app);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public static void DocxToPdf(string wordPath, string pdfPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(wordPath);
        ArgumentException.ThrowIfNullOrEmpty(pdfPath);
        if (!File.Exists(wordPath))
            throw new FileNotFoundException("找不到 Word 文件。", wordPath);

        string ext = Path.GetExtension(pdfPath);
        if (!ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            pdfPath = Path.ChangeExtension(pdfPath, ".pdf");

        WordApp? app = null;
        WordDoc? doc = null;
        try
        {
            app = new WordApp { Visible = false, DisplayAlerts = WdAlertLevel.wdAlertsNone };
            doc = app.Documents.Open(
                FileName: wordPath,
                ConfirmConversions: false,
                ReadOnly: true,
                AddToRecentFiles: false);
            object extPtr = Type.Missing;
            doc.ExportAsFixedFormat(
                OutputFileName: pdfPath,
                ExportFormat: WdExportFormat.wdExportFormatPDF,
                OpenAfterExport: false,
                OptimizeFor: WdExportOptimizeFor.wdExportOptimizeForPrint,
                Range: WdExportRange.wdExportAllDocument,
                From: 1,
                To: 1,
                Item: WdExportItem.wdExportDocumentContent,
                IncludeDocProps: true,
                KeepIRM: true,
                CreateBookmarks: WdExportCreateBookmarks.wdExportCreateWordBookmarks,
                DocStructureTags: true,
                BitmapMissingFonts: true,
                UseISO19005_1: false,
                FixedFormatExtClassPtr: ref extPtr);
        }
        finally
        {
            if (doc != null)
            {
                try
                {
                    doc.Close(SaveChanges: false);
                }
                catch
                {
                    // ignore
                }

                Marshal.ReleaseComObject(doc);
            }

            if (app != null)
            {
                try
                {
                    app.Quit(SaveChanges: false);
                }
                catch
                {
                    // ignore
                }

                Marshal.ReleaseComObject(app);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
