namespace PdfEditor;

/// <summary>轻量输入框，避免引用 Microsoft.VisualBasic。</summary>
internal static class PromptDialog
{
    public static string? Ask(IWin32Window owner, string prompt, string title, string defaultText = "")
    {
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(420, 120),
            Font = SystemFonts.MessageBoxFont
        };

        var lbl = new Label { Text = prompt, AutoSize = false, Location = new Point(12, 12), Size = new Size(396, 40) };
        var tb = new TextBox { Text = defaultText, Location = new Point(12, 52), Size = new Size(396, 23) };
        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new Point(228, 86), Size = new Size(88, 26) };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(320, 86), Size = new Size(88, 26) };

        form.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialog(owner) == DialogResult.OK ? tb.Text : null;
    }
}
