using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PdfTool.App.Services;

internal static class DocxPreviewService
{
    public static string LoadPlainText(string docxPath)
    {
        using var doc = WordprocessingDocument.Open(docxPath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null)
            return string.Empty;

        var sb = new StringBuilder();
        AppendBodyElements(sb, body);
        return sb.ToString();
    }

    private static void AppendBodyElements(StringBuilder sb, Body body)
    {
        foreach (var child in body.Elements())
        {
            switch (child)
            {
                case Paragraph p:
                    sb.AppendLine(GetParagraphText(p));
                    break;
                case Table t:
                    AppendTable(sb, t);
                    break;
            }
        }
    }

    private static void AppendTable(StringBuilder sb, Table table)
    {
        foreach (var row in table.Elements<TableRow>())
        {
            var cells = row.Elements<TableCell>().Select(GetCellText).ToList();
            if (cells.Count > 0)
                sb.AppendLine(string.Join("\t", cells));
        }

        sb.AppendLine();
    }

    private static string GetCellText(TableCell cell)
    {
        var sb = new StringBuilder();
        foreach (var p in cell.Descendants<Paragraph>())
        {
            var t = GetParagraphText(p);
            if (t.Length > 0)
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                sb.Append(t);
            }
        }

        return sb.ToString();
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }
}
