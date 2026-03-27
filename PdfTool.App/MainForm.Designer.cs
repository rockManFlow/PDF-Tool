namespace PdfTool.App;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private MenuStrip menuStrip;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ToolStripMenuItem openToolStripMenuItem;
    private ToolStripMenuItem saveAsToolStripMenuItem;
    private ToolStripMenuItem functionsToolStripMenuItem;
    private ToolStripMenuItem pdfToWordToolStripMenuItem;
    private ToolStripMenuItem wordToPdfToolStripMenuItem;
    private TextBox viewerTextBox;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        menuStrip = new MenuStrip();
        fileToolStripMenuItem = new ToolStripMenuItem();
        openToolStripMenuItem = new ToolStripMenuItem();
        saveAsToolStripMenuItem = new ToolStripMenuItem();
        functionsToolStripMenuItem = new ToolStripMenuItem();
        pdfToWordToolStripMenuItem = new ToolStripMenuItem();
        wordToPdfToolStripMenuItem = new ToolStripMenuItem();
        viewerTextBox = new TextBox();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        menuStrip.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
        {
            openToolStripMenuItem,
            saveAsToolStripMenuItem
        });
        fileToolStripMenuItem.Text = "文件";

        openToolStripMenuItem.Text = "打开…";
        openToolStripMenuItem.Click += OpenToolStripMenuItem_Click;

        saveAsToolStripMenuItem.Text = "另存为…";
        saveAsToolStripMenuItem.Click += SaveAsToolStripMenuItem_Click;

        functionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
        {
            pdfToWordToolStripMenuItem,
            wordToPdfToolStripMenuItem
        });
        functionsToolStripMenuItem.Text = "功能";

        pdfToWordToolStripMenuItem.Text = "PDF 转 Word";
        pdfToWordToolStripMenuItem.Click += PdfToWordToolStripMenuItem_Click;

        wordToPdfToolStripMenuItem.Text = "Word 转 PDF";
        wordToPdfToolStripMenuItem.Click += WordToPdfToolStripMenuItem_Click;

        menuStrip.Items.AddRange(new ToolStripItem[]
        {
            fileToolStripMenuItem,
            functionsToolStripMenuItem
        });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(984, 24);
        menuStrip.TabIndex = 0;
        menuStrip.Text = "menuStrip";

        viewerTextBox.Dock = DockStyle.Fill;
        viewerTextBox.Font = new Font("Microsoft YaHei UI", 10F);
        viewerTextBox.Location = new Point(0, 24);
        viewerTextBox.MaxLength = 0;
        viewerTextBox.Multiline = true;
        viewerTextBox.Name = "viewerTextBox";
        viewerTextBox.ReadOnly = true;
        viewerTextBox.ScrollBars = ScrollBars.Both;
        viewerTextBox.TabIndex = 1;
        viewerTextBox.WordWrap = true;

        statusLabel.Name = "statusLabel";
        statusLabel.Spring = true;
        statusLabel.Text = "请从「文件」菜单打开 PDF 或 Word（.docx）文件";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusStrip.Location = new Point(0, 539);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(984, 22);
        statusStrip.TabIndex = 2;
        statusStrip.Text = "statusStrip";

        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(984, 561);
        Controls.Add(menuStrip);
        Controls.Add(statusStrip);
        Controls.Add(viewerTextBox);
        MainMenuStrip = menuStrip;
        MinimumSize = new Size(640, 400);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "PDF / Word 工具（离线）";
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
