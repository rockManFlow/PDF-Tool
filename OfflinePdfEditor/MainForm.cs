using System.Diagnostics;
using System.Drawing;
using PdfiumViewer;
using PdfSharp.Drawing;
using OfflinePdfEditor.Services;
using PdfiumPdfDocument = PdfiumViewer.PdfDocument;

namespace OfflinePdfEditor;

public partial class MainForm : Form
{
    private string? _workPath;
    private string? _lastSavedPath;
    private bool _isScanned;
    private InteractiveTool _tool = InteractiveTool.None;
    private string _fontName = "Verdana";
    private double _emSize = 12;
    private Color _color = Color.Black;
    private int _lastPage0 = -1;
    private bool _mouseDown;
    private float _hlX0, _hlY0;
    private int _hlPage = -1;
    private readonly List<(double x, double y)> _inkBl = new();
    private int _inkPage = -1;
    private string? _pendingImagePath;
    private Control? _renderer;

    private enum InteractiveTool
    {
        None,
        Highlight,
        Ink,
        Text,
        TextBox,
        Comment,
        ImagePlace
    }

    public MainForm()
    {
        InitializeComponent();
        tsOpen.Click += (_, _) => OpenPdf();
        tsSave.Click += (_, _) => SavePdf(false);
        tsSaveAs.Click += (_, _) => SavePdf(true);
        tsFont.Click += (_, _) => PickFont();
        tsColor.Click += (_, _) => PickColor();
        tsImage.Click += (_, _) => BeginInsertImage();
        tsHighlight.Click += (_, _) => SetTool(InteractiveTool.Highlight);
        tsInk.Click += (_, _) => SetTool(InteractiveTool.Ink);
        tsText.Click += (_, _) => SetTool(InteractiveTool.Text);
        tsTextBox.Click += (_, _) => SetTool(InteractiveTool.TextBox);
        tsComment.Click += (_, _) => SetTool(InteractiveTool.Comment);
        tsDeletePage.Click += (_, _) => DeleteCurrentPage();
        tsCancelTool.Click += (_, _) => SetTool(InteractiveTool.None);
        tsPdfToWord.Click += (_, _) => ConvertPdfToWord();
        tsWordToPdf.Click += (_, _) => ConvertWordToPdf();
        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        _renderer = PdfiumHit.FindRenderer(pdfViewer);
        if (_renderer == null)
        {
            lblStatus.Text = "错误：未找到 PdfRenderer 子控件。";
            return;
        }

        _renderer.MouseDown += Renderer_MouseDown;
        _renderer.MouseMove += Renderer_MouseMove;
        _renderer.MouseUp += Renderer_MouseUp;
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_workPath) && File.Exists(_workPath))
                File.Delete(_workPath);
        }
        catch
        {
            // ignore
        }
    }

    private void SetTool(InteractiveTool t)
    {
        _tool = t;
        _pendingImagePath = t == InteractiveTool.ImagePlace ? _pendingImagePath : null;
        if (t != InteractiveTool.ImagePlace)
            Cursor = Cursors.Default;
        lblStatus.Text = t switch
        {
            InteractiveTool.Highlight => "高亮：按下拖动矩形区域。",
            InteractiveTool.Ink => "划线：按住拖动绘制。",
            InteractiveTool.Text => "输入文字：单击页面位置。",
            InteractiveTool.TextBox => "文本框：单击放置固定区域。",
            InteractiveTool.Comment => "批注：单击添加注释框。",
            InteractiveTool.ImagePlace => string.IsNullOrEmpty(_pendingImagePath)
                ? "请先通过「插入图片」选择文件。"
                : "单击页面放置图片。",
            _ => "就绪。"
        } + (_isScanned ? " 【扫描件：仅批注类工具可用】" : "");
    }

    private bool EnsureWorkCopy()
    {
        if (string.IsNullOrEmpty(_workPath) || !File.Exists(_workPath))
        {
            MessageBox.Show(this, "请先打开 PDF。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        return true;
    }

    private bool AllowContentEditing()
    {
        if (!_isScanned)
            return true;
        MessageBox.Show(this, "当前为扫描件 PDF：不允许修改正文内容，请仅使用高亮、划线、批注等。", "提示", MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        return false;
    }

    private static string FormatWordInteropError(Exception ex) =>
        ex.Message + Environment.NewLine + Environment.NewLine +
        "请确认本机已安装 Microsoft Word 桌面版（可本地打开文档），且未被策略禁用自动化。";

    /// <summary>使用 Word 将 PDF 转为 DOCX；优先使用当前编辑中的工作副本。</summary>
    private void ConvertPdfToWord()
    {
        string? src = !string.IsNullOrEmpty(_workPath) && File.Exists(_workPath) ? _workPath : null;
        if (src == null)
        {
            using var ofd = new OpenFileDialog { Filter = "PDF|*.pdf|所有文件|*.*", Title = "选择要转换的 PDF" };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            src = ofd.FileName;
        }

        using var sfd = new SaveFileDialog { Filter = "Word|*.docx", DefaultExt = "docx", FileName = Path.GetFileNameWithoutExtension(src) + ".docx", Title = "保存为 Word" };
        if (sfd.ShowDialog() != DialogResult.OK)
            return;

        UseWaitCursor = true;
        try
        {
            WordInteropConversionService.PdfToDocx(src, sfd.FileName);
            lblStatus.Text = "已导出 Word：" + Path.GetFileName(sfd.FileName);
            if (MessageBox.Show(this, "转换完成。是否在默认程序中打开该 DOCX？", "完成", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, FormatWordInteropError(ex), "PDF→Word 失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    /// <summary>使用 Word 将 DOC/DOCX 导出为 PDF。</summary>
    private void ConvertWordToPdf()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Word|*.docx;*.doc|所有文件|*.*",
            Title = "选择 Word 文档"
        };
        if (ofd.ShowDialog() != DialogResult.OK)
            return;

        using var sfd = new SaveFileDialog
        {
            Filter = "PDF|*.pdf",
            DefaultExt = "pdf",
            FileName = Path.GetFileNameWithoutExtension(ofd.FileName) + ".pdf",
            Title = "保存为 PDF"
        };
        if (sfd.ShowDialog() != DialogResult.OK)
            return;

        UseWaitCursor = true;
        try
        {
            WordInteropConversionService.DocxToPdf(ofd.FileName, sfd.FileName);
            lblStatus.Text = "已导出 PDF：" + Path.GetFileName(sfd.FileName);
            if (MessageBox.Show(this, "导出完成。是否打开该 PDF？", "完成", MessageBoxButtons.YesNo, MessageBoxIcon.Information) ==
                DialogResult.Yes)
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, FormatWordInteropError(ex), "Word→PDF 失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void OpenPdf()
    {
        using var ofd = new OpenFileDialog { Filter = "PDF|*.pdf|所有文件|*.*", Title = "打开 PDF" };
        if (ofd.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            pdfViewer.Document?.Dispose();
            pdfViewer.Document = null;
            _workPath = Path.Combine(Path.GetTempPath(), "OfflinePdfEdit_" + Guid.NewGuid().ToString("N") + ".pdf");
            File.Copy(ofd.FileName, _workPath, overwrite: true);
            _lastSavedPath = ofd.FileName;
            _isScanned = ScanDetector.IsScannedDocument(_workPath);
            pdfViewer.Document = PdfiumPdfDocument.Load(_workPath);
            ApplyScanUi();
            lblStatus.Text = "已打开：" + Path.GetFileName(ofd.FileName) + (_isScanned ? "（检测为扫描件）" : "");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, PdfiumLoadHelp.Format(ex), "打开失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyScanUi()
    {
        bool annOnly = _isScanned;
        tsText.Enabled = !annOnly;
        tsTextBox.Enabled = !annOnly;
        tsImage.Enabled = !annOnly;
        tsDeletePage.Enabled = !annOnly;
    }

    private void SavePdf(bool saveAs)
    {
        if (!EnsureWorkCopy())
            return;

        string dest;
        if (saveAs)
        {
            using var sfd = new SaveFileDialog { Filter = "PDF|*.pdf", DefaultExt = "pdf", Title = "另存为 PDF" };
            if (sfd.ShowDialog() != DialogResult.OK)
                return;
            dest = sfd.FileName;
        }
        else if (string.IsNullOrEmpty(_lastSavedPath))
        {
            using var sfd = new SaveFileDialog { Filter = "PDF|*.pdf", DefaultExt = "pdf", Title = "保存 PDF" };
            if (sfd.ShowDialog() != DialogResult.OK)
                return;
            dest = sfd.FileName;
        }
        else
            dest = _lastSavedPath;

        try
        {
            pdfViewer.Document?.Dispose();
            pdfViewer.Document = null;
            File.Copy(_workPath!, dest, overwrite: true);
            _lastSavedPath = dest;
            pdfViewer.Document = PdfiumPdfDocument.Load(_workPath!);
            lblStatus.Text = (saveAs ? "已另存为：" : "已保存：") + Path.GetFileName(dest);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            try
            {
                pdfViewer.Document ??= PdfiumPdfDocument.Load(_workPath!);
            }
            catch
            {
                // ignore
            }
        }
    }

    /// <summary>PDFsharp 写入前必须释放 Pdfium 对同一工作文件的句柄。</summary>
    private void ExecutePdfEdit(Action edit)
    {
        if (string.IsNullOrEmpty(_workPath) || !File.Exists(_workPath))
            return;
        pdfViewer.Document?.Dispose();
        pdfViewer.Document = null;
        try
        {
            edit();
        }
        finally
        {
            try
            {
                pdfViewer.Document = PdfiumPdfDocument.Load(_workPath!);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "重新加载 PDF 失败：" + ex.Message;
            }
        }
    }

    private void PickFont()
    {
        FontStyle st = FontStyle.Regular;
        if (tsBold.Checked) st |= FontStyle.Bold;
        if (tsItalic.Checked) st |= FontStyle.Italic;
        if (tsUnderline.Checked) st |= FontStyle.Underline;
        using var fd = new FontDialog
        {
            Font = new Font(_fontName, (float)_emSize, st, GraphicsUnit.Point)
        };
        if (fd.ShowDialog() != DialogResult.OK)
            return;
        _fontName = fd.Font.FontFamily.Name;
        _emSize = fd.Font.SizeInPoints;
        tsBold.Checked = fd.Font.Bold;
        tsItalic.Checked = fd.Font.Italic;
        tsUnderline.Checked = fd.Font.Underline;
    }

    private void PickColor()
    {
        using var cd = new ColorDialog { Color = _color };
        if (cd.ShowDialog() == DialogResult.OK)
            _color = cd.Color;
    }

    private void BeginInsertImage()
    {
        if (!AllowContentEditing())
            return;
        using var ofd = new OpenFileDialog
        {
            Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
            Title = "选择图片"
        };
        if (ofd.ShowDialog() != DialogResult.OK)
            return;
        _pendingImagePath = ofd.FileName;
        _tool = InteractiveTool.ImagePlace;
        SetTool(InteractiveTool.ImagePlace);
    }

    private void DeleteCurrentPage()
    {
        if (!AllowContentEditing())
            return;
        if (!EnsureWorkCopy())
            return;
        if (_lastPage0 < 0)
        {
            MessageBox.Show(this, "请先在页面上单击一次以确定要删除的页。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(this, $"确定删除第 {_lastPage0 + 1} 页？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
            DialogResult.Yes)
            return;

        try
        {
            int idx = _lastPage0;
            ExecutePdfEdit(() => PdfEditService.DeletePage(_workPath!, idx));
            int pc = pdfViewer.Document?.PageCount ?? 0;
            _lastPage0 = Math.Min(idx, Math.Max(0, pc - 1));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Renderer_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _renderer == null || !EnsureWorkCopy())
            return;
        if (!PdfiumHit.TryHit(_renderer, e.Location, out int page0, out float x, out float y))
            return;

        _lastPage0 = page0;

        switch (_tool)
        {
            case InteractiveTool.Highlight:
                _mouseDown = true;
                _hlPage = page0;
                _hlX0 = x;
                _hlY0 = y;
                break;
            case InteractiveTool.Ink:
                _mouseDown = true;
                _inkPage = page0;
                _inkBl.Clear();
                _inkBl.Add((x, y));
                break;
            case InteractiveTool.Text:
                if (!AllowContentEditing())
                    return;
                CommitTextAt(page0, x, y);
                break;
            case InteractiveTool.TextBox:
                if (!AllowContentEditing())
                    return;
                CommitTextBoxAt(page0, x, y);
                break;
            case InteractiveTool.Comment:
                CommitCommentAt(page0, x, y);
                break;
            case InteractiveTool.ImagePlace:
                if (!AllowContentEditing())
                    return;
                if (string.IsNullOrEmpty(_pendingImagePath))
                    return;
                try
                {
                    ExecutePdfEdit(() => PdfEditService.DrawImage(_workPath!, page0, x, y, _pendingImagePath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "插入图片失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                _pendingImagePath = null;
                SetTool(InteractiveTool.None);
                break;
        }
    }

    private void Renderer_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_mouseDown || _renderer == null || !EnsureWorkCopy())
            return;
        if (!PdfiumHit.TryHit(_renderer, e.Location, out int page0, out float x, out float y))
            return;

        if (_tool == InteractiveTool.Ink && e.Button == MouseButtons.Left)
        {
            if (_inkBl.Count == 0 || Math.Abs(_inkBl[^1].x - x) > 0.2 || Math.Abs(_inkBl[^1].y - y) > 0.2)
                _inkBl.Add((x, y));
        }
    }

    private void Renderer_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_mouseDown || _renderer == null || !EnsureWorkCopy())
            return;
        _mouseDown = false;

        if (_tool == InteractiveTool.Highlight && _hlPage >= 0 &&
            PdfiumHit.TryHit(_renderer, e.Location, out int page1, out float x1, out float y1) && page1 == _hlPage)
        {
            try
            {
                ExecutePdfEdit(() => PdfEditService.DrawHighlight(_workPath!, _hlPage, _hlX0, _hlY0, x1, y1));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "高亮失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        if (_tool == InteractiveTool.Ink && _inkBl.Count >= 2 && _inkPage >= 0)
        {
            try
            {
                ExecutePdfEdit(() => PdfEditService.DrawInk(_workPath!, _inkPage, _inkBl));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "划线失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        _inkBl.Clear();
        _hlPage = -1;
    }

    private void CommitTextAt(int page0, float x, float y)
    {
        string? t = SimplePrompt.Ask(this, "输入文字", "内容：", "", multiline: true);
        if (string.IsNullOrWhiteSpace(t))
            return;
        try
        {
            var xc = XColor.FromArgb(_color.ToArgb());
            ExecutePdfEdit(() => PdfEditService.DrawText(_workPath!, page0, x, y, t.Trim(), _fontName, _emSize, tsBold.Checked,
                tsItalic.Checked,
                tsUnderline.Checked, xc));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "写入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CommitTextBoxAt(int page0, float x, float y)
    {
        string? t = SimplePrompt.Ask(this, "文本框", "内容：", "文本", multiline: true);
        if (t == null)
            return;
        try
        {
            ExecutePdfEdit(() => PdfEditService.DrawTextBox(_workPath!, page0, x, y, 220, 72, t, _fontName, (double)_emSize * 0.9,
                XColors.SteelBlue,
                XColor.FromArgb(_color.ToArgb())));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CommitCommentAt(int page0, float x, float y)
    {
        string? t = SimplePrompt.Ask(this, "批注", "批注内容：", "");
        if (string.IsNullOrWhiteSpace(t))
            return;
        try
        {
            ExecutePdfEdit(() => PdfEditService.DrawCommentBubble(_workPath!, page0, x, y, t.Trim()));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "批注失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
