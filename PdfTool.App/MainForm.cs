using PdfTool.App.Services;

namespace PdfTool.App;

public partial class MainForm : Form
{
    private enum OpenDocKind
    {
        None,
        Pdf,
        Word
    }

    private string? _currentPath;
    private OpenDocKind _docKind = OpenDocKind.None;
    private readonly LibreOfficeConversionService _libreOffice = new();

    public MainForm()
    {
        InitializeComponent();
        UpdateFunctionMenuState();
    }

    private void OpenToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "打开 PDF 或 Word 文件",
            Filter = "PDF 文件 (*.pdf)|*.pdf|Word 文档 (*.docx)|*.docx|所有支持的文件|*.pdf;*.docx",
            FilterIndex = 3
        };

        if (ofd.ShowDialog() != DialogResult.OK)
            return;

        var path = ofd.FileName;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        try
        {
            if (ext == ".pdf")
            {
                var pages = PdfPreviewService.LoadPageTexts(path);
                viewerTextBox.Clear();
                foreach (var (pageNumber, text) in pages)
                {
                    viewerTextBox.AppendText($"──────── 第 {pageNumber} 页 ────────\r\n");
                    viewerTextBox.AppendText(string.IsNullOrWhiteSpace(text) ? "（本页无可提取文本，可能为扫描件或纯图像 PDF）\r\n" : text);
                    viewerTextBox.AppendText("\r\n\r\n");
                }

                _currentPath = path;
                _docKind = OpenDocKind.Pdf;
                statusLabel.Text = $"已打开 PDF：{path}（iText 文本预览）";
            }
            else if (ext == ".docx")
            {
                var text = DocxPreviewService.LoadPlainText(path);
                viewerTextBox.Text = text;
                _currentPath = path;
                _docKind = OpenDocKind.Word;
                statusLabel.Text = $"已打开 Word：{path}";
            }
            else
            {
                MessageBox.Show(this, "请选择 .pdf 或 .docx 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"无法打开文件：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        UpdateFunctionMenuState();
    }

    private void SaveAsToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentPath) || _docKind == OpenDocKind.None)
            return;

        string filter;
        string defaultName = Path.GetFileName(_currentPath);
        if (_docKind == OpenDocKind.Pdf)
            filter = "PDF 文件 (*.pdf)|*.pdf";
        else
            filter = "Word 文档 (*.docx)|*.docx";

        using var sfd = new SaveFileDialog
        {
            Title = "另存为",
            Filter = filter,
            FileName = defaultName,
            OverwritePrompt = true
        };

        if (sfd.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            File.Copy(_currentPath, sfd.FileName, true);
            statusLabel.Text = $"已另存为：{sfd.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"另存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PdfToWordToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        if (_docKind != OpenDocKind.Pdf || string.IsNullOrEmpty(_currentPath))
            return;

        using var fbd = new FolderBrowserDialog
        {
            Description = "选择输出文件夹（PDF 转 Word 结果将保存在此，当前为占位功能）"
        };

        if (fbd.ShowDialog() != DialogResult.OK)
            return;

        statusLabel.Text = $"已选择输出目录：{fbd.SelectedPath}（PDF 转 Word 尚未实现）";
        MessageBox.Show(
            this,
            "PDF 转 Word 功能尚未实现（占位）。\r\n\r\n您选择的输出文件夹：\r\n" + fbd.SelectedPath,
            "PDF 转 Word",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private async void WordToPdfToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        if (_docKind != OpenDocKind.Word || string.IsNullOrEmpty(_currentPath))
            return;

        using var fbd = new FolderBrowserDialog
        {
            Description = "选择 PDF 输出文件夹"
        };

        if (fbd.ShowDialog() != DialogResult.OK)
            return;

        UseWaitCursor = true;
        pdfToWordToolStripMenuItem.Enabled = false;
        wordToPdfToolStripMenuItem.Enabled = false;
        try
        {
            var result = await Task.Run(() => _libreOffice.ConvertWordToPdf(_currentPath, fbd.SelectedPath)).ConfigureAwait(true);
            if (result.Success)
            {
                statusLabel.Text = "Word 已转为 PDF：" + result.OutputPdfPath;
                MessageBox.Show(this, "转换完成。\r\n\r\n" + result.OutputPdfPath, "Word 转 PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, result.Message ?? "未知错误", "Word 转 PDF 失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            UseWaitCursor = false;
            UpdateFunctionMenuState();
        }
    }

    private void UpdateFunctionMenuState()
    {
        var hasFile = !string.IsNullOrEmpty(_currentPath) && _docKind != OpenDocKind.None;
        saveAsToolStripMenuItem.Enabled = hasFile;
        pdfToWordToolStripMenuItem.Enabled = hasFile && _docKind == OpenDocKind.Pdf;
        wordToPdfToolStripMenuItem.Enabled = hasFile && _docKind == OpenDocKind.Word;
    }
}
