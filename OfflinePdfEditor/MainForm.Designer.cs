namespace OfflinePdfEditor;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private PdfiumViewer.PdfViewer pdfViewer;
    private ToolStrip toolStrip;
    private ToolStripButton tsOpen;
    private ToolStripButton tsSave;
    private ToolStripButton tsSaveAs;
    private ToolStripSeparator tsSep1;
    private ToolStripButton tsFont;
    private ToolStripButton tsColor;
    private ToolStripButton tsBold;
    private ToolStripButton tsItalic;
    private ToolStripButton tsUnderline;
    private ToolStripSeparator tsSep2;
    private ToolStripButton tsImage;
    private ToolStripButton tsHighlight;
    private ToolStripButton tsInk;
    private ToolStripButton tsText;
    private ToolStripButton tsTextBox;
    private ToolStripButton tsComment;
    private ToolStripSeparator tsSep3;
    private ToolStripButton tsDeletePage;
    private ToolStripButton tsCancelTool;
    private Panel panelHost;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            pdfViewer.Document?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        pdfViewer = new PdfiumViewer.PdfViewer();
        toolStrip = new ToolStrip();
        tsOpen = new ToolStripButton();
        tsSave = new ToolStripButton();
        tsSaveAs = new ToolStripButton();
        tsSep1 = new ToolStripSeparator();
        tsFont = new ToolStripButton();
        tsColor = new ToolStripButton();
        tsBold = new ToolStripButton();
        tsItalic = new ToolStripButton();
        tsUnderline = new ToolStripButton();
        tsSep2 = new ToolStripSeparator();
        tsImage = new ToolStripButton();
        tsHighlight = new ToolStripButton();
        tsInk = new ToolStripButton();
        tsText = new ToolStripButton();
        tsTextBox = new ToolStripButton();
        tsComment = new ToolStripButton();
        tsSep3 = new ToolStripSeparator();
        tsDeletePage = new ToolStripButton();
        tsCancelTool = new ToolStripButton();
        panelHost = new Panel();
        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel();
        panelHost.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        pdfViewer.Dock = DockStyle.Fill;
        pdfViewer.Location = new Point(0, 0);
        pdfViewer.Name = "pdfViewer";
        pdfViewer.Size = new Size(1100, 620);
        pdfViewer.TabIndex = 0;
        tsOpen.Text = "打开";
        tsOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsSave.Text = "保存";
        tsSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsSaveAs.Text = "另存为";
        tsSaveAs.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsFont.Text = "字体";
        tsFont.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsColor.Text = "颜色";
        tsColor.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsBold.Text = "粗体";
        tsBold.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsBold.CheckOnClick = true;
        tsItalic.Text = "斜体";
        tsItalic.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsItalic.CheckOnClick = true;
        tsUnderline.Text = "下划线";
        tsUnderline.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsUnderline.CheckOnClick = true;
        tsImage.Text = "插入图片";
        tsImage.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsHighlight.Text = "高亮";
        tsHighlight.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsInk.Text = "划线";
        tsInk.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsText.Text = "输入文字";
        tsText.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsTextBox.Text = "文本框";
        tsTextBox.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsComment.Text = "批注";
        tsComment.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsDeletePage.Text = "删除页面";
        tsDeletePage.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsCancelTool.Text = "取消工具";
        tsCancelTool.DisplayStyle = ToolStripItemDisplayStyle.Text;

        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Items.AddRange(new ToolStripItem[]
        {
            tsOpen, tsSave, tsSaveAs, tsSep1,
            tsFont, tsColor, tsBold, tsItalic, tsUnderline, tsSep2,
            tsImage, tsHighlight, tsInk, tsText, tsTextBox, tsComment, tsSep3,
            tsDeletePage, tsCancelTool
        });

        panelHost.Controls.Add(pdfViewer);
        panelHost.Dock = DockStyle.Fill;
        panelHost.Location = new Point(0, 28);
        panelHost.Name = "panelHost";
        panelHost.Size = new Size(1100, 620);
        panelHost.TabIndex = 1;

        lblStatus.Name = "lblStatus";
        lblStatus.Spring = true;
        lblStatus.Text = "就绪（离线）。显示：PDFium + PdfiumViewer；保存：PDFsharp。";

        statusStrip.Items.Add(lblStatus);
        statusStrip.Location = new Point(0, 648);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1100, 22);
        statusStrip.TabIndex = 2;

        toolStrip.Location = new Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(1100, 28);
        toolStrip.TabIndex = 0;

        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 670);
        Controls.Add(panelHost);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);
        MinimumSize = new Size(800, 500);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "离线 PDF 编辑器（PDFium 显示）";
        panelHost.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
