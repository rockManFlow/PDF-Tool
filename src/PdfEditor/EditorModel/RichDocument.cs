namespace PdfEditor.EditorModel;

/// <summary>
/// 自建版面模型：段落 + 样式化字符片段，可 JSON 持久化并导出为 PDF。
/// </summary>
public sealed class RichDocument
{
    public string Title { get; set; } = "";

    /// <summary>页边距（毫米），导出 PDF 时换算为 pt。</summary>
    public double MarginMm { get; set; } = 20;

    public List<RichParagraph> Paragraphs { get; set; } = new();

    public static RichDocument CreateEmpty()
    {
        var d = new RichDocument();
        d.Paragraphs.Add(new RichParagraph());
        return d;
    }
}

public sealed class RichParagraph
{
    public List<RichRun> Runs { get; set; } = new();
}

public sealed class RichRun
{
    public string Text { get; set; } = "";

    public string FontName { get; set; } = "Microsoft YaHei UI";

    public float FontSizePt { get; set; } = 11f;

    public bool Bold { get; set; }
    public bool Italic { get; set; }

    /// <summary>ARGB，与 <see cref="Color.ToArgb"/> 一致。</summary>
    public int ColorArgb { get; set; } = unchecked((int)0xFF000000);
}
