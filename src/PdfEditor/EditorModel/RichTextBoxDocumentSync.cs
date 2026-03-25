using System.Drawing;

namespace PdfEditor.EditorModel;

/// <summary>
/// <see cref="RichTextBox"/> 与 <see cref="RichDocument"/> 互转（不处理图片/嵌入对象）。
/// </summary>
public static class RichTextBoxDocumentSync
{
    public static RichDocument FromRichTextBox(RichTextBox rtb)
    {
        var doc = new RichDocument();
        string text = rtb.Text;
        int savedStart = rtb.SelectionStart;
        int savedLen = rtb.SelectionLength;

        try
        {
            var runsBuf = new List<RichRun>();
            RichRun? merge = null;

            void FlushRun()
            {
                if (merge != null)
                {
                    runsBuf.Add(merge);
                    merge = null;
                }
            }

            void FlushParagraph()
            {
                FlushRun();
                if (runsBuf.Count == 0)
                    runsBuf.Add(new RichRun { Text = "" });
                doc.Paragraphs.Add(new RichParagraph { Runs = runsBuf.ToList() });
                runsBuf.Clear();
            }

            if (text.Length == 0)
            {
                FlushParagraph();
                return doc;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '\r')
                    continue;
                if (ch == '\n')
                {
                    FlushParagraph();
                    continue;
                }

                rtb.Select(i, 1);
                Font sf = rtb.SelectionFont ?? rtb.Font;
                Color col = rtb.SelectionColor;
                bool bold = sf.Bold;
                bool italic = sf.Italic;
                string fn = sf.FontFamily.Name;
                float sz = sf.SizeInPoints;
                int argb = col.ToArgb();

                string piece = ch.ToString();
                if (merge != null
                    && merge.FontName == fn
                    && Math.Abs(merge.FontSizePt - sz) < 0.01f
                    && merge.Bold == bold
                    && merge.Italic == italic
                    && merge.ColorArgb == argb)
                {
                    merge.Text += piece;
                }
                else
                {
                    FlushRun();
                    merge = new RichRun
                    {
                        Text = piece,
                        FontName = fn,
                        FontSizePt = sz,
                        Bold = bold,
                        Italic = italic,
                        ColorArgb = argb
                    };
                }
            }

            FlushParagraph();
        }
        finally
        {
            rtb.Select(savedStart, savedLen);
        }

        return doc;
    }

    public static void ApplyToRichTextBox(RichTextBox rtb, RichDocument doc)
    {
        rtb.SuspendLayout();
        try
        {
            rtb.Clear();
            rtb.Font = new Font("Microsoft YaHei UI", 11f);

            for (int pi = 0; pi < doc.Paragraphs.Count; pi++)
            {
                var para = doc.Paragraphs[pi];
                foreach (var run in para.Runs)
                {
                    if (string.IsNullOrEmpty(run.Text))
                        continue;

                    FontStyle st = FontStyle.Regular;
                    if (run.Bold) st |= FontStyle.Bold;
                    if (run.Italic) st |= FontStyle.Italic;

                    using var tryFont = new Font(run.FontName, run.FontSizePt, st, GraphicsUnit.Point);
                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionLength = 0;
                    rtb.SelectionFont = tryFont;
                    rtb.SelectionColor = Color.FromArgb(run.ColorArgb);
                    rtb.SelectedText = run.Text;
                }

                if (pi < doc.Paragraphs.Count - 1)
                {
                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionLength = 0;
                    rtb.SelectedText = "\n";
                }
            }
        }
        finally
        {
            rtb.ResumeLayout();
        }
    }
}
