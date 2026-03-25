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
    private const int DragThresholdPx = 6;

    private PdfEditorService? _service;
    private bool _isScanned;
    private EditorTool _tool = EditorTool.None;

    private int _pageCount;
    private int _lastContentWidth;
    private readonly List<PageViewEntry> _pageEntries = new();

    private Point _dragStart;
    private Point _dragCurrent;
    private bool _dragging;
    private int _page0Down;
    private PictureBox? _activePageBox;
    private readonly List<PointF> _inkPointsPdf = new();

    private readonly TextBox _inlineEditor;
    private bool _inlineSuppressLostFocus;
    private PendingInlineEdit _pendingInline;

    public MainForm()
    {
        InitializeComponent();

        _inlineEditor = new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei UI", 10f),
            Visible = false,
            Size = new Size(320, 26),
            Multiline = false
        };
        _inlineEditor.KeyDown += InlineEditor_KeyDown;
        _inlineEditor.LostFocus += InlineEditor_LostFocus;
        panelPagesHost.Controls.Add(_inlineEditor);

        UpdateUiState();
    }

    private void ScrollPdf_Scroll(object? sender, ScrollEventArgs e)
    {
        UpdatePageIndicator();
    }

    private sealed class PageViewEntry
    {
        public required PictureBox Picture { get; init; }
        public int Page0 { get; init; }
        public float PdfWidthPt { get; init; }
        public float PdfHeightPt { get; init; }
    }

    private struct PendingInlineEdit
    {
        public int Page1Based;
        public float PdfX;
        public float PdfY;
        public GeomRectangle? WhiteoutRect;
        public float FontSize;
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _service?.Dispose();
        ClearDocumentPages();
    }

    private void MainForm_ResizeEnd(object? sender, EventArgs e)
    {
        if (_service != null && _pageCount > 0)
            RebuildDocumentView();
    }

    private void ClearDocumentPages()
    {
        HideInlineEditor();
        for (int i = panelPagesHost.Controls.Count - 1; i >= 0; i--)
        {
            var c = panelPagesHost.Controls[i];
            if (c == _inlineEditor)
                continue;
            c.Dispose();
        }

        _pageEntries.Clear();
    }

    private int GetContentWidth()
    {
        int w = scrollPdf.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 24;
        return Math.Max(160, w);
    }

    private void RebuildDocumentView()
    {
        if (_service == null || !File.Exists(_service.WorkPath) || _pageCount <= 0)
        {
            ClearDocumentPages();
            lblPageIndicator.Text = "";
            return;
        }

        int cw = GetContentWidth();
        if (cw == _lastContentWidth && _pageEntries.Count == _pageCount && _pageCount > 0)
        {
            UpdatePageIndicator();
            return;
        }

        _lastContentWidth = cw;
        HideInlineEditor();

        scrollPdf.SuspendLayout();
        panelPagesHost.SuspendLayout();
        try
        {
            ClearDocumentPages();
            panelPagesHost.Width = cw;

            for (int i = 0; i < _pageCount; i++)
            {
                var (pw, ph) = PdfRasterPreview.GetPageSizePts(_service.WorkPath, i);
                Bitmap clone;
                using (var bmp = PdfRasterPreview.RenderPageForContentWidth(_service.WorkPath, i, cw))
                    clone = new Bitmap(bmp);

                var pb = new PictureBox
                {
                    Image = clone,
                    SizeMode = PictureBoxSizeMode.Normal,
                    Size = clone.Size,
                    Margin = new Padding(8, 8, 8, 8),
                    BackColor = Color.White
                };
                pb.MouseDown += PagePicture_MouseDown;
                pb.MouseMove += PagePicture_MouseMove;
                pb.MouseUp += PagePicture_MouseUp;

                _pageEntries.Add(new PageViewEntry
                {
                    Picture = pb,
                    Page0 = i,
                    PdfWidthPt = pw,
                    PdfHeightPt = ph
                });
                panelPagesHost.Controls.Add(pb);
            }

            panelPagesHost.Controls.Add(_inlineEditor);
            _inlineEditor.BringToFront();
        }
        finally
        {
            panelPagesHost.ResumeLayout(true);
            scrollPdf.ResumeLayout(true);
        }

        UpdatePageIndicator();
    }

    private void UpdatePageIndicator()
    {
        if (_pageCount <= 0 || _pageEntries.Count == 0)
        {
            lblPageIndicator.Text = "";
            return;
        }

        int yCenter = scrollPdf.VerticalScroll.Value + scrollPdf.ClientSize.Height / 2;
        for (int i = 0; i < _pageEntries.Count; i++)
        {
            var pb = _pageEntries[i].Picture;
            int top = pb.Top;
            int bottom = top + pb.Height;
            if (yCenter >= top && yCenter < bottom)
            {
                lblPageIndicator.Text = $"第 {i + 1} 页，共 {_pageCount} 页";
                return;
            }
        }

        lblPageIndicator.Text = $"共 {_pageCount} 页";
    }

    private PageViewEntry? FindEntry(PictureBox pb)
    {
        foreach (var e in _pageEntries)
        {
            if (e.Picture == pb)
                return e;
        }

        return null;
    }

    private bool TryMapPictureBoxToPdf(PictureBox pb, Point clientOnPb, out PointF pdf, out int page0)
    {
        pdf = default;
        page0 = 0;
        var entry = FindEntry(pb);
        if (entry == null || pb.Image == null)
            return false;

        if (clientOnPb.X < 0 || clientOnPb.Y < 0 || clientOnPb.X > pb.Image.Width || clientOnPb.Y > pb.Image.Height)
            return false;

        float wImg = pb.Image.Width;
        float hImg = pb.Image.Height;
        pdf.X = clientOnPb.X / wImg * entry.PdfWidthPt;
        pdf.Y = entry.PdfHeightPt - (clientOnPb.Y / hImg * entry.PdfHeightPt);
        page0 = entry.Page0;
        return true;
    }

    private void HideInlineEditor()
    {
        _inlineEditor.Visible = false;
        _inlineEditor.Text = "";
    }

    private void BeginInlineEdit(Point hostLocation, PendingInlineEdit pending)
    {
        _pendingInline = pending;
        _inlineSuppressLostFocus = true;
        _inlineEditor.Location = new Point(
            Math.Max(0, hostLocation.X),
            Math.Max(0, hostLocation.Y));
        _inlineEditor.Text = "";
        _inlineEditor.Visible = true;
        _inlineEditor.BringToFront();
        _inlineEditor.Focus();
        _inlineSuppressLostFocus = false;
    }

    private void InlineEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            CommitInlineEdit();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            CancelInlineEdit();
        }
    }

    private void InlineEditor_LostFocus(object? sender, EventArgs e)
    {
        if (_inlineSuppressLostFocus || !_inlineEditor.Visible)
            return;

        BeginInvoke(() =>
        {
            if (!_inlineEditor.Visible || _inlineEditor.Focused)
                return;
            CommitInlineEdit();
        });
    }

    private void CommitInlineEdit()
    {
        if (!_inlineEditor.Visible || _service == null)
            return;

        string t = _inlineEditor.Text.TrimEnd('\r', '\n');
        HideInlineEditor();

        if (string.IsNullOrEmpty(t))
            return;

        try
        {
            int p = _pendingInline.Page1Based;
            if (_pendingInline.WhiteoutRect.HasValue)
            {
                _service.WhiteoutAndDrawText(p, _pendingInline.WhiteoutRect.Value, t, _pendingInline.FontSize);
            }
            else
            {
                float fs = _isScanned ? 12f : 14f;
                _service.AddTextOverlay(p, _pendingInline.PdfX, _pendingInline.PdfY, t, fs);
            }

            ReloadViewer();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "写入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CancelInlineEdit()
    {
        HideInlineEditor();
    }

    private void SetTool(EditorTool t)
    {
        CancelInlineEdit();
        _tool = t;
        _dragging = false;
        _inkPointsPdf.Clear();
        lblStatus.Text = t switch
        {
            EditorTool.Text => "文字：在页面上单击，直接输入（Enter 确认，Esc 取消）。",
            EditorTool.Line => "划线：按下拖动绘制墨迹线。",
            EditorTool.Highlight => "高亮：按下拖动框选区域。",
            EditorTool.Watermark => "水印：为全部页面添加半透明文字（扫描件）。",
            EditorTool.Image => "图片：拖动矩形区域，再选择图片文件。",
            EditorTool.EditText => "编辑文字：单击在光标处插入；或拖动框选区域后输入以替换该区域（仅文本型 PDF）。",
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
            ClearDocumentPages();
            _pageCount = 0;
            lblPageIndicator.Text = "";
            return;
        }

        try
        {
            _pageCount = PdfRasterPreview.GetPageCount(_service.WorkPath);
            _lastContentWidth = 0;
            RebuildDocumentView();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "预览失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    private static int DistanceSq(Point a, Point b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private void PagePicture_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _service == null || _tool == EditorTool.None)
            return;

        if (sender is not PictureBox pb)
            return;

        if (!TryMapPictureBoxToPdf(pb, e.Location, out var pDown, out _page0Down))
            return;

        _activePageBox = pb;
        _dragStart = e.Location;
        _dragCurrent = e.Location;
        _dragging = true;

        if (_tool == EditorTool.Line)
            _inkPointsPdf.Add(pDown);
    }

    private void PagePicture_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging || _service == null || sender is not PictureBox pb || pb != _activePageBox)
            return;

        _dragCurrent = e.Location;

        if (_tool == EditorTool.Line && e.Button == MouseButtons.Left &&
            TryMapPictureBoxToPdf(pb, e.Location, out var p, out _))
            _inkPointsPdf.Add(p);
    }

    private void PagePicture_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_dragging || e.Button != MouseButtons.Left || _service == null || sender is not PictureBox pb)
            return;

        _dragging = false;

        if (!TryMapPictureBoxToPdf(pb, _dragStart, out var p0, out _))
            return;

        int page1 = _page0Down + 1;
        bool smallMove = DistanceSq(_dragStart, _dragCurrent) < DragThresholdPx * DragThresholdPx;

        try
        {
            switch (_tool)
            {
                case EditorTool.Highlight:
                    if (TryMapPictureBoxToPdf(pb, _dragCurrent, out var p1, out _))
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
                    if (smallMove && TryMapPictureBoxToPdf(pb, _dragCurrent, out var pText, out _))
                    {
                        Point screen = pb.PointToScreen(new Point(_dragCurrent.X, _dragCurrent.Y));
                        Point host = panelPagesHost.PointToClient(screen);
                        BeginInlineEdit(host, new PendingInlineEdit
                        {
                            Page1Based = page1,
                            PdfX = pText.X,
                            PdfY = pText.Y,
                            WhiteoutRect = null,
                            FontSize = 14f
                        });
                    }
                    break;

                case EditorTool.Image:
                    if (!smallMove && TryMapPictureBoxToPdf(pb, _dragCurrent, out var p1b, out _))
                    {
                        using var ofd = new OpenFileDialog
                        {
                            Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
                            Title = "选择图片"
                        };
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            var rect = ToPdfRectangle(p0, p1b);
                            _service.AddImageOverlay(page1, ofd.FileName, rect);
                            ReloadViewer();
                        }
                    }
                    break;

                case EditorTool.EditText:
                    if (!_isScanned)
                    {
                        if (smallMove && TryMapPictureBoxToPdf(pb, _dragCurrent, out var pIns, out _))
                        {
                            Point screen = pb.PointToScreen(new Point(_dragCurrent.X, _dragCurrent.Y));
                            Point host = panelPagesHost.PointToClient(screen);
                            BeginInlineEdit(host, new PendingInlineEdit
                            {
                                Page1Based = page1,
                                PdfX = pIns.X,
                                PdfY = pIns.Y,
                                WhiteoutRect = null,
                                FontSize = 14f
                            });
                        }
                        else if (!smallMove && TryMapPictureBoxToPdf(pb, _dragCurrent, out var p2, out _))
                        {
                            var rect = ToPdfRectangle(p0, p2);
                            float fs = Math.Clamp(rect.GetHeight() * 0.55f, 8f, 28f);
                            int lx = Math.Min(_dragStart.X, _dragCurrent.X);
                            int by = Math.Max(_dragStart.Y, _dragCurrent.Y);
                            Point blClient = new Point(lx, by);
                            Point screen = pb.PointToScreen(blClient);
                            Point host = panelPagesHost.PointToClient(screen);
                            BeginInlineEdit(host, new PendingInlineEdit
                            {
                                Page1Based = page1,
                                PdfX = 0,
                                PdfY = 0,
                                WhiteoutRect = rect,
                                FontSize = fs
                            });
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        _activePageBox = null;
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
            ClearDocumentPages();
            _pageCount = 0;
            _lastContentWidth = 0;
            _service = PdfEditorService.CreateWorkCopy(dlg.FileName);
            _isScanned = PdfScanDetector.IsScannedPdf(_service.WorkPath);
            ReloadViewer();
            SetTool(EditorTool.None);
            lblStatus.Text = _isScanned
                ? "已打开（扫描件/图片型）：仅批注、划线、高亮、水印。"
                : "已打开（文本型）：可叠加编辑、插图、划线、高亮。";
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
            ClearDocumentPages();
            _pageCount = 0;
            _lastContentWidth = 0;
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
}
