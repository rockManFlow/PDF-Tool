namespace PdfEditor;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;

    private System.Windows.Forms.ToolStrip toolStrip;
    private System.Windows.Forms.ToolStripButton btnOpen;
    private System.Windows.Forms.ToolStripButton btnSave;
    private System.Windows.Forms.ToolStripButton btnNew;
    private System.Windows.Forms.ToolStripSeparator sep1;
    private System.Windows.Forms.ToolStripButton btnText;
    private System.Windows.Forms.ToolStripButton btnLine;
    private System.Windows.Forms.ToolStripButton btnHighlight;
    private System.Windows.Forms.ToolStripSeparator sep2;
    private System.Windows.Forms.ToolStripButton btnWatermark;
    private System.Windows.Forms.ToolStripButton btnImage;
    private System.Windows.Forms.ToolStripButton btnEditText;
    private System.Windows.Forms.ToolStripSeparator sep3;
    private System.Windows.Forms.ToolStripButton btnCancelTool;
    private PdfiumViewer.PdfViewer pdfViewer;
    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        toolStrip = new System.Windows.Forms.ToolStrip();
        btnOpen = new System.Windows.Forms.ToolStripButton();
        btnSave = new System.Windows.Forms.ToolStripButton();
        btnNew = new System.Windows.Forms.ToolStripButton();
        sep1 = new System.Windows.Forms.ToolStripSeparator();
        btnText = new System.Windows.Forms.ToolStripButton();
        btnLine = new System.Windows.Forms.ToolStripButton();
        btnHighlight = new System.Windows.Forms.ToolStripButton();
        sep2 = new System.Windows.Forms.ToolStripSeparator();
        btnWatermark = new System.Windows.Forms.ToolStripButton();
        btnImage = new System.Windows.Forms.ToolStripButton();
        btnEditText = new System.Windows.Forms.ToolStripButton();
        sep3 = new System.Windows.Forms.ToolStripSeparator();
        btnCancelTool = new System.Windows.Forms.ToolStripButton();
        pdfViewer = new PdfiumViewer.PdfViewer();
        statusStrip = new System.Windows.Forms.StatusStrip();
        lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
        toolStrip.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        btnOpen.Text = "打开";
        btnOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnOpen.Click += BtnOpen_Click;

        btnSave.Text = "保存";
        btnSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnSave.Click += BtnSave_Click;

        btnNew.Text = "新建";
        btnNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnNew.Click += BtnNew_Click;

        btnText.Text = "文字";
        btnText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnText.Click += BtnText_Click;

        btnLine.Text = "划线";
        btnLine.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnLine.Click += BtnLine_Click;

        btnHighlight.Text = "高亮";
        btnHighlight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnHighlight.Click += BtnHighlight_Click;

        btnWatermark.Text = "水印";
        btnWatermark.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnWatermark.Click += BtnWatermark_Click;

        btnImage.Text = "图片";
        btnImage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnImage.Click += BtnImage_Click;

        btnEditText.Text = "编辑文字";
        btnEditText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnEditText.Click += BtnEditText_Click;

        btnCancelTool.Text = "取消工具";
        btnCancelTool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        btnCancelTool.Click += (_, _) => SetTool(EditorTool.None);

        toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
        {
            btnOpen, btnSave, btnNew, sep1,
            btnText, btnLine, btnHighlight, sep2,
            btnWatermark, btnImage, btnEditText, sep3,
            btnCancelTool
        });
        toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
        toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
        toolStrip.Location = new System.Drawing.Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new System.Drawing.Size(1100, 28);
        toolStrip.TabIndex = 0;
        toolStrip.Text = "toolStrip";

        pdfViewer.Dock = System.Windows.Forms.DockStyle.Fill;
        pdfViewer.Location = new System.Drawing.Point(0, 28);
        pdfViewer.Name = "pdfViewer";
        pdfViewer.Size = new System.Drawing.Size(1100, 622);
        pdfViewer.TabIndex = 1;
        pdfViewer.MouseDown += PdfViewer_MouseDown;
        pdfViewer.MouseMove += PdfViewer_MouseMove;
        pdfViewer.MouseUp += PdfViewer_MouseUp;

        lblStatus.Name = "lblStatus";
        lblStatus.Size = new System.Drawing.Size(0, 17);

        statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
        statusStrip.Location = new System.Drawing.Point(0, 650);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new System.Drawing.Size(1100, 22);
        statusStrip.TabIndex = 2;
        statusStrip.Text = "statusStrip";

        AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1100, 672);
        Controls.Add(pdfViewer);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);
        MinimumSize = new System.Drawing.Size(800, 500);
        Name = "MainForm";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "离线 PDF 编辑 (iText7)";
        FormClosing += MainForm_FormClosing;
        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
