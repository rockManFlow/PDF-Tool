using System.Text.Json;
using PdfEditor.EditorModel;

namespace PdfEditor;

/// <summary>
/// 自建富文本模型：主编辑区 + 工具条 + JSON 持久化 + 导出 PDF。
/// </summary>
public partial class MainForm
{
    private TabControl _workspaceTabs = null!;
    private RichTextBox _richEditor = null!;
    private ToolStrip _editorMiniStrip = null!;
    private ToolStripButton _edBold = null!;
    private ToolStripButton _edItalic = null!;
    private ToolStripComboBox _edSize = null!;
    private ToolStripButton _edColor = null!;

    private bool _richDocDirty;
    private string? _richDocPath;
    private bool _syncingEditorChrome;

    private static readonly JsonSerializerOptions JsonOpt = new() { WriteIndented = true };

    private void BuildRichWorkspaceUi()
    {
        _workspaceTabs = new TabControl { Dock = DockStyle.Fill };

        var tabRich = new TabPage("富文本编辑");
        var host = new Panel { Dock = DockStyle.Fill };
        _editorMiniStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            AutoSize = false,
            Height = 28
        };

        _edBold = new ToolStripButton("粗体") { CheckOnClick = true, DisplayStyle = ToolStripItemDisplayStyle.Text };
        _edItalic = new ToolStripButton("斜体") { CheckOnClick = true, DisplayStyle = ToolStripItemDisplayStyle.Text };
        _edSize = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 52 };
        foreach (var s in new[] { "9", "10", "11", "12", "14", "16", "18", "24" })
            _edSize.Items.Add(s);
        _edSize.SelectedIndex = 2;
        _edColor = new ToolStripButton("颜色") { DisplayStyle = ToolStripItemDisplayStyle.Text };

        _edBold.Click += (_, _) => ToggleFontStyle(FontStyle.Bold);
        _edItalic.Click += (_, _) => ToggleFontStyle(FontStyle.Italic);
        _edSize.SelectedIndexChanged += EdSize_SelectedIndexChanged;
        _edColor.Click += EdColor_Click;

        _editorMiniStrip.Items.AddRange(new ToolStripItem[]
        {
            _edBold, _edItalic, new ToolStripSeparator(), _edSize, new ToolStripSeparator(), _edColor
        });

        _richEditor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 11f),
            BorderStyle = BorderStyle.FixedSingle,
            DetectUrls = false,
            AcceptsTab = true
        };
        _richEditor.SelectionChanged += RichEditor_SelectionChanged;
        _richEditor.TextChanged += (_, _) => { _richDocDirty = true; };

        host.Controls.Add(_editorMiniStrip);
        host.Controls.Add(_richEditor);
        tabRich.Controls.Add(host);

        var tabPdf = new TabPage("PDF 打开与标注");
        panelBody.Controls.Remove(scrollPdf);
        scrollPdf.Dock = DockStyle.Fill;
        tabPdf.Controls.Add(scrollPdf);

        _workspaceTabs.TabPages.Add(tabRich);
        _workspaceTabs.TabPages.Add(tabPdf);
        _workspaceTabs.SelectedIndex = 0;

        panelBody.Controls.Add(_workspaceTabs);
    }

    private void RichEditor_SelectionChanged(object? sender, EventArgs e)
    {
        if (_syncingEditorChrome)
            return;

        _syncingEditorChrome = true;
        try
        {
            Font? cf = _richEditor.SelectionFont ?? _richEditor.Font;
            _edBold.Checked = cf.Bold;
            _edItalic.Checked = cf.Italic;
            string sz = Math.Round(cf.SizeInPoints).ToString("0");
            int idx = _edSize.Items.IndexOf(sz);
            if (idx >= 0)
                _edSize.SelectedIndex = idx;
        }
        finally
        {
            _syncingEditorChrome = false;
        }
    }

    private void ToggleFontStyle(FontStyle bit)
    {
        Font cf = _richEditor.SelectionFont ?? _richEditor.Font;
        FontStyle st = (cf.Style & bit) == bit ? (cf.Style & ~bit) : (cf.Style | bit);
        try
        {
            _richEditor.SelectionFont = new Font(cf.FontFamily, cf.Size, st, GraphicsUnit.Point);
        }
        catch
        {
            _richEditor.SelectionFont = new Font("Microsoft YaHei UI", cf.Size, st, GraphicsUnit.Point);
        }

        _richDocDirty = true;
    }

    private void EdSize_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_syncingEditorChrome || _edSize.SelectedItem is not string s || !float.TryParse(s, out var pt))
            return;

        Font cf = _richEditor.SelectionFont ?? _richEditor.Font;
        try
        {
            _richEditor.SelectionFont = new Font(cf.FontFamily, pt, cf.Style, GraphicsUnit.Point);
        }
        catch
        {
            _richEditor.SelectionFont = new Font("Microsoft YaHei UI", pt, cf.Style, GraphicsUnit.Point);
        }

        _richDocDirty = true;
    }

    private void EdColor_Click(object? sender, EventArgs e)
    {
        using var dlg = new ColorDialog { Color = _richEditor.SelectionColor };
        if (dlg.ShowDialog() != DialogResult.OK)
            return;
        _richEditor.SelectionColor = dlg.Color;
        _richDocDirty = true;
    }

    private void InsertRichDocumentToolStrip()
    {
        var btnNewDoc = new ToolStripButton("新建文档") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var btnOpenDoc = new ToolStripButton("打开文档") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var btnSaveDoc = new ToolStripButton("保存文档") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var btnExportPdf = new ToolStripButton("导出PDF") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        btnNewDoc.Click += (_, _) => NewRichDocument();
        btnOpenDoc.Click += (_, _) => OpenRichDocument();
        btnSaveDoc.Click += (_, _) => SaveRichDocument(false);
        btnExportPdf.Click += (_, _) => ExportRichDocumentToPdf();

        var group = new ToolStripItem[]
        {
            new ToolStripSeparator(),
            btnNewDoc,
            btnOpenDoc,
            btnSaveDoc,
            btnExportPdf,
            new ToolStripSeparator()
        };
        toolStrip.Items.InsertRange(3, group);
    }

    private void NewRichDocument()
    {
        if (!ConfirmDiscardRichDoc())
            return;

        _richDocPath = null;
        _richEditor.Clear();
        _richEditor.Text =
            "在此编辑正文（类似 Word 的输入体验）。\r\n\r\n" +
            "工具条可设置粗体、斜体、字号与颜色。\r\n\r\n" +
            "「保存文档」将版面保存为 .json；「导出PDF」生成 PDF 文件。\r\n\r\n" +
            "第二页签可继续打开 PDF 做划线、高亮等标注。";
        _richEditor.SelectAll();
        _richEditor.SelectionFont = new Font("Microsoft YaHei UI", 11f);
        _richEditor.SelectionColor = Color.Black;
        _richEditor.Select(_richEditor.TextLength, 0);
        _richDocDirty = false;
        lblStatus.Text = "富文本：已新建";
    }

    private bool ConfirmDiscardRichDoc()
    {
        if (!_richDocDirty)
            return true;
        var r = MessageBox.Show(this, "当前文档未保存，是否放弃更改？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        return r == DialogResult.Yes;
    }

    private void OpenRichDocument()
    {
        if (!ConfirmDiscardRichDoc())
            return;

        using var ofd = new OpenFileDialog { Filter = "文档|*.json|所有文件|*.*", Title = "打开文档" };
        if (ofd.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            string json = File.ReadAllText(ofd.FileName);
            var d = JsonSerializer.Deserialize<RichDocument>(json, JsonOpt) ?? RichDocument.CreateEmpty();
            d.Paragraphs ??= new List<RichParagraph>();
            foreach (var p in d.Paragraphs)
            {
                p.Runs ??= new List<RichRun>();
                foreach (var r in p.Runs)
                {
                    r.Text ??= "";
                    if (string.IsNullOrEmpty(r.FontName))
                        r.FontName = "Microsoft YaHei UI";
                }
            }

            if (d.Paragraphs.Count == 0)
                d.Paragraphs.Add(new RichParagraph());
            RichTextBoxDocumentSync.ApplyToRichTextBox(_richEditor, d);
            _richDocPath = ofd.FileName;
            _richDocDirty = false;
            lblStatus.Text = "已打开文档：" + Path.GetFileName(ofd.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "打开失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <returns>是否已成功保存（未取消）。</returns>
    private bool SaveRichDocument(bool saveAs)
    {
        string path = _richDocPath ?? "";
        if (saveAs || string.IsNullOrEmpty(path))
        {
            using var sfd = new SaveFileDialog { Filter = "文档|*.json", Title = "保存文档", DefaultExt = "json" };
            if (sfd.ShowDialog() != DialogResult.OK)
                return false;
            path = sfd.FileName;
        }

        try
        {
            var doc = RichTextBoxDocumentSync.FromRichTextBox(_richEditor);
            doc.Title = Path.GetFileNameWithoutExtension(path);
            File.WriteAllText(path, JsonSerializer.Serialize(doc, JsonOpt));
            _richDocPath = path;
            _richDocDirty = false;
            lblStatus.Text = "已保存文档";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void ExportRichDocumentToPdf()
    {
        using var sfd = new SaveFileDialog { Filter = "PDF|*.pdf", Title = "导出 PDF", DefaultExt = "pdf" };
        if (sfd.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            var doc = RichTextBoxDocumentSync.FromRichTextBox(_richEditor);
            doc.Title = Path.GetFileNameWithoutExtension(sfd.FileName);
            RichDocumentPdfExporter.Export(doc, sfd.FileName);
            lblStatus.Text = "已导出 PDF";
            MessageBox.Show(this, "导出完成。", "导出 PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool PromptSaveRichIfNeededOnClose()
    {
        if (!_richDocDirty)
            return true;
        var r = MessageBox.Show(this, "是否保存当前富文本文档？", "关闭", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (r == DialogResult.Cancel)
            return false;
        if (r == DialogResult.No)
            return true;
        return SaveRichDocument(false);
    }
}
