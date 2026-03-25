using System.Drawing;
using PdfEditor.Services;
using GeomRectangle = iText.Kernel.Geom.Rectangle;

namespace PdfEditor;

public enum EditorTool
{
    None,
    Text,
    Line,
    Highlight,
    Watermark,
    Image,
    EditText
}

public partial class MainForm : Form
{
    private PdfEditorService? _service;
    private bool _isScanned;
    private EditorTool _tool = EditorTool.None;

    private int _page0;
    private int _pageCount;
    private float _pageWidthPt;
    private float _pageHeightPt;

    private Point _dragStart;
    private Point _dragCurrent;
    private bool _dragging;
    private int _page0Down;
    private readonly List<PointF> _inkPointsPdf = new();

    public MainForm()
    {
        InitializeComponent();
        UpdateUiState();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _service?.Dispose();
        picPdf.Image?.Dispose();
        picPdf.Image = null;
    }

    private void MainForm_ResizeEnd(object? sender, EventArgs e)
    {
        if (_service != null && _pageCount > 0)
            RenderCurrentPage();
    }

    private void BtnPrevPage_Click(object? sender, EventArgs e)
    {
        if (_page0 <= 0)
            return;
        _page0--;
        RenderCurrentPage();
    }

    private void BtnNextPage_Click(object? sender, EventArgs e)
    {
        if (_page0 >= _pageCount - 1)
            return;
        _page0++;
        RenderCurrentPage();
    }

    private void UpdatePageNav()
    {
        lblPage.Text = _pageCount <= 0 ? "-" : $"{_page0 + 1} / {_pageCount}";
        btnPrevPage.Enabled = _pageCount > 0 && _page0 > 0;
        btnNextPage.Enabled = _pageCount > 0 && _page0 < _pageCount - 1;
    }

    private void SetTool(EditorTool t)
    {
        _tool = t;
        _dragging = false;
        _inkPointsPdf.Clear();
        lblStatus.Text = t switch
        {
            EditorTool.Text => _isScanned
                ? "文字：单击在页面上叠加文字（仅追加内容流，不修改原有内容）。"
                : "文字：单击在页面顶层叠加文字。",
            EditorTool.Line => "划线：按下拖动绘制墨迹线。",
            EditorTool.Highlight => "高亮：按下拖动框选区域。",
            EditorTool.Watermark => "水印：为全部页面添加半透明文字（扫描件允许）。",
            EditorTool.Image => "图片：拖动矩形区域，再选择图片文件。",
            EditorTool.EditText => "编辑文字：拖动矩形覆盖原区域，再输入替换文字（仅文本型 PDF）。",
            _ => _service == null ? "请先打开或新建 PDF。" : (_isScanned ? "扫描件模式：仅批注/划线/高亮/水印。" : "文本型 PDF：可叠加编辑、插图。")
        };
    }

    private void UpdateUiState()
    {
        bool hasDoc = _service != null && File.Exists(_service.WorkPath);
        btnSave.Enabled = hasDoc;
        btnText.Enabled = hasDoc;
        btnLine.Enabled = hasDoc;
        btnHighlight.Enabled = hasDoc;

        btnWatermark.Enabled = hasDoc && _isScanned;
        btnImage.Enabled = hasDoc && !_isScanned;
        btnEditText.Enabled = hasDoc && !_isScanned;

        if (!hasDoc)
            SetTool(EditorTool.None);
        else if (_isScanned && (_tool == EditorTool.Image || _tool == EditorTool.EditText))
            SetTool(EditorTool.None);
        else if (!_isScanned && _tool == EditorTool.Watermark)
            SetTool(EditorTool.None);
    }

    private void ReloadViewer()
    {
        if (_service == null || !File.Exists(_service.WorkPath))
        {
            picPdf.Image?.Dispose();
            picPdf.Image = null;
            _pageCount = 0;
            UpdatePageNav();
            return;
        }

        try
        {
            _pageCount = PdfRasterPreview.GetPageCount(_service.WorkPath);
            _page0 = Math.Clamp(_page0, 0, Math.Max(0, _pageCount - 1));
            RenderCurrentPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "预览失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RenderCurrentPage()
    {
        if (_service == null || !File.Exists(_service.WorkPath) || _pageCount <= 0)
            return;

        try
        {
            (_pageWidthPt, _pageHeightPt) = PdfRasterPreview.GetPageSizePts(_service.WorkPath, _page0);
            picPdf.Image?.Dispose();
            picPdf.Image = PdfRasterPreview.RenderPage(_service.WorkPath, _page0, picPdf.ClientSize.Width, picPdf.ClientSize.Height);
            UpdatePageNav();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "渲染页面失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static GeomRectangle ToPdfRectangle(PointF a, PointF b)
    {
        float llx = Math.Min(a.X, b.X);
        float lly = Math.Min(a.Y, b.Y);
        float urx = Math.Max(a.X, b.X);
        float ury = Math.Max(a.Y, b.Y);
        return new GeomRectangle(llx, lly, Math.Max(1f, urx - llx), Math.Max(1f, ury - lly));
    }

    /// <summary>PictureBox Zoom 模式下，图像在控件客户区内的显示矩形（与 GDI+ 缩放一致）。</summary>
    private static RectangleF GetImageDisplayRectangle(PictureBox pb)
    {
        if (pb.Image == null)
            return RectangleF.Empty;

        Size s = pb.ClientSize;
        float iw = pb.Image.Width;
        float ih = pb.Image.Height;
        if (iw <= 0 || ih <= 0)
            return RectangleF.Empty;

        float r = Math.Min(s.Width / iw, s.Height / ih);
        float w = iw * r;
        float h = ih * r;
        float x = (s.Width - w) / 2f;
        float y = (s.Height - h) / 2f;
        return new RectangleF(x, y, w, h);
    }

    private bool TryMapToPdf(Point client, out PointF pdf, out int page0)
    {
        pdf = default;
        page0 = _page0;

        if (picPdf.Image == null || _pageWidthPt <= 0 || _pageHeightPt <= 0)
            return false;

        var disp = GetImageDisplayRectangle(picPdf);
        if (disp.Width <= 0 || disp.Height <= 0)
            return false;

        if (client.X < disp.Left || client.X > disp.Right || client.Y < disp.Top || client.Y > disp.Bottom)
            return false;

        float ix = (client.X - disp.Left) / disp.Width * picPdf.Image.Width;
        float iy = (client.Y - disp.Top) / disp.Height * picPdf.Image.Height;

        pdf.X = ix / picPdf.Image.Width * _pageWidthPt;
        pdf.Y = _pageHeightPt - (iy / picPdf.Image.Height * _pageHeightPt);
        return true;
    }

    private void BtnOpen_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "PDF|*.pdf|所有文件|*.*",
            Title = "打开 PDF"
        };
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            _service?.Dispose();
            picPdf.Image?.Dispose();
            picPdf.Image = null;
            _page0 = 0;
            _service = PdfEditorService.CreateWorkCopy(dlg.FileName);
            _isScanned = PdfScanDetector.IsScannedPdf(_service.WorkPath);
            ReloadViewer();
            SetTool(EditorTool.None);
            lblStatus.Text = _isScanned
                ? "已打开（判定为扫描件/图片型）：仅批注、划线、高亮、水印。"
                : "已打开（文本型）：可编辑叠加、插图、划线、高亮。";
            UpdateUiState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "打开失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_service == null)
            return;

        using var dlg = new SaveFileDialog
        {
            Filter = "PDF|*.pdf",
            Title = "另存为 PDF",
            DefaultExt = "pdf"
        };
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            _service.SaveAs(dlg.FileName);
            MessageBox.Show(this, "已保存。", "保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnNew_Click(object? sender, EventArgs e)
    {
        try
        {
            _service?.Dispose();
            picPdf.Image?.Dispose();
            picPdf.Image = null;
            _page0 = 0;
            string path = Path.Combine(Path.GetTempPath(), "PdfEditor_new_" + Guid.NewGuid().ToString("N") + ".pdf");
            _service = PdfEditorService.CreateNewBlank(path, 1);
            _isScanned = false;
            ReloadViewer();
            SetTool(EditorTool.None);
            lblStatus.Text = "已新建空白 A4 PDF（文本型）。";
            UpdateUiState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "新建失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnText_Click(object? sender, EventArgs e)
    {
        if (_service == null)
            return;
        SetTool(EditorTool.Text);
    }

    private void BtnLine_Click(object? sender, EventArgs e)
    {
        if (_service == null)
            return;
        SetTool(EditorTool.Line);
    }

    private void BtnHighlight_Click(object? sender, EventArgs e)
    {
        if (_service == null)
            return;
        SetTool(EditorTool.Highlight);
    }

    private void BtnWatermark_Click(object? sender, EventArgs e)
    {
        if (_service == null || !_isScanned)
            return;

        string? w = PromptDialog.Ask(this, "输入水印文字：", "水印", "CONFIDENTIAL");
        if (string.IsNullOrWhiteSpace(w))
            return;

        try
        {
            _service.AddTextWatermark(w.Trim(), 0.12f, 44f);
            ReloadViewer();
            lblStatus.Text = "已添加水印。";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "水印失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnImage_Click(object? sender, EventArgs e)
    {
        if (_service == null || _isScanned)
            return;
        SetTool(EditorTool.Image);
    }

    private void BtnEditText_Click(object? sender, EventArgs e)
    {
        if (_service == null || _isScanned)
            return;
        SetTool(EditorTool.EditText);
    }

    private void PicPdf_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _service == null || _tool == EditorTool.None)
            return;

        if (!TryMapToPdf(e.Location, out var pDown, out _page0Down))
            return;

        _dragStart = e.Location;
        _dragCurrent = e.Location;
        _dragging = true;

        if (_tool == EditorTool.Line)
            _inkPointsPdf.Add(pDown);
    }

    private void PicPdf_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging || _service == null)
            return;

        _dragCurrent = e.Location;

        if (_tool == EditorTool.Line && e.Button == MouseButtons.Left && TryMapToPdf(e.Location, out var p, out _))
            _inkPointsPdf.Add(p);
    }

    private void PicPdf_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_dragging || e.Button != MouseButtons.Left || _service == null)
            return;

        _dragging = false;

        if (!TryMapToPdf(_dragStart, out var p0, out _))
            return;

        int page1 = _page0Down + 1;

        try
        {
            switch (_tool)
            {
                case EditorTool.Highlight:
                    if (TryMapToPdf(_dragCurrent, out var p1, out _))
                    {
                        var rect = ToPdfRectangle(p0, p1);
                        _service.AddHighlight(page1, rect);
                        ReloadViewer();
                    }
                    break;

                case EditorTool.Line:
                    if (_inkPointsPdf.Count >= 2)
                        _service.AddInkStroke(page1, _inkPointsPdf);
                    _inkPointsPdf.Clear();
                    ReloadViewer();
                    break;

                case EditorTool.Text:
                {
                    string? t = PromptDialog.Ask(this, "输入文字：", "文字", "备注");
                    if (string.IsNullOrWhiteSpace(t))
                        break;

                    _service.AddTextOverlay(page1, p0.X, p0.Y, t.Trim(), _isScanned ? 12f : 14f);
                    ReloadViewer();
                    break;
                }

                case EditorTool.Image:
                {
                    if (!TryMapToPdf(_dragCurrent, out var p1b, out _))
                        break;
                    using var ofd = new OpenFileDialog
                    {
                        Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
                        Title = "选择图片"
                    };
                    if (ofd.ShowDialog() != DialogResult.OK)
                        break;

                    var rect = ToPdfRectangle(p0, p1b);
                    _service.AddImageOverlay(page1, ofd.FileName, rect);
                    ReloadViewer();
                    break;
                }

                case EditorTool.EditText:
                {
                    if (!TryMapToPdf(_dragCurrent, out var p1c, out _))
                        break;
                    string? nt = PromptDialog.Ask(this, "输入替换文字（可为空，仅涂白）：", "编辑文字", "");
                    if (nt == null)
                        break;

                    var rect = ToPdfRectangle(p0, p1c);
                    float fs = Math.Clamp(rect.GetHeight() * 0.6f, 8f, 24f);
                    _service.WhiteoutAndDrawText(page1, rect, nt, fs);
                    ReloadViewer();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
