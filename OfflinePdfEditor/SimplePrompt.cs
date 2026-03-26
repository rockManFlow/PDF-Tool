namespace OfflinePdfEditor;

internal static class SimplePrompt
{
    public static string? Ask(IWin32Window? owner, string caption, string label, string defaultText = "", bool multiline = false)
    {
        using var f = new Form
        {
            Text = caption,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            Width = multiline ? 540 : 460,
            Height = multiline ? 260 : 170
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lbl = new Label { Text = label, AutoSize = true, Dock = DockStyle.Fill };
        var tb = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = defaultText,
            Multiline = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
        };

        var flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true };
        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, AutoSize = true };
        flow.Controls.Add(cancel);
        flow.Controls.Add(ok);

        layout.Controls.Add(lbl, 0, 0);
        layout.Controls.Add(tb, 0, 1);
        layout.Controls.Add(flow, 0, 2);

        f.Controls.Add(layout);
        f.AcceptButton = ok;
        f.CancelButton = cancel;

        return f.ShowDialog(owner) == DialogResult.OK ? tb.Text : null;
    }
}
