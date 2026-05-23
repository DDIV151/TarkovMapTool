using System;
using System.Windows.Forms;

namespace TarkovMapTool
{
    public class HotkeyCaptureForm : Form
    {
        private Label lblPrompt;
        private Label lblCurrentKey;
        private Button btnOK;
        private Button btnCancel;

        public Keys KeyCode { get; private set; }
        public bool Control { get; private set; }
        public bool Shift { get; private set; }
        public bool Alt { get; private set; }

        public HotkeyCaptureForm()
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "按下快捷键组合";
            this.Size = new System.Drawing.Size(300, 160);

            lblPrompt = new Label
            {
                Text = "请按下键盘上的组合键（可包含 Ctrl/Shift/Alt）",
                Location = new System.Drawing.Point(12, 12),
                AutoSize = true
            };

            lblCurrentKey = new Label
            {
                Text = "当前：未按下",
                Location = new System.Drawing.Point(12, 50),
                AutoSize = true,
                Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold)
            };

            btnOK = new Button
            {
                Text = "确定",
                Location = new System.Drawing.Point(120, 85),
                Size = new System.Drawing.Size(75, 23),
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button
            {
                Text = "取消",
                Location = new System.Drawing.Point(200, 85),
                Size = new System.Drawing.Size(75, 23),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblPrompt);
            this.Controls.Add(lblCurrentKey);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            this.KeyPreview = true;
            this.KeyDown += HotkeyCaptureForm_KeyDown;
            this.KeyUp += HotkeyCaptureForm_KeyUp;
        }

        private void HotkeyCaptureForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu)
                return;

            Control = e.Control;
            Shift = e.Shift;
            Alt = e.Alt;
            KeyCode = e.KeyCode;

            string hotkeyText = "";
            if (Control) hotkeyText += "Ctrl + ";
            if (Shift) hotkeyText += "Shift + ";
            if (Alt) hotkeyText += "Alt + ";
            hotkeyText += KeyCode.ToString();

            lblCurrentKey.Text = "当前：" + hotkeyText;
            e.SuppressKeyPress = true;
        }

        private void HotkeyCaptureForm_KeyUp(object sender, KeyEventArgs e) { }

        public string ToHotkeyString()
        {
            if (KeyCode == Keys.None) return "";
            string result = "";
            if (Control) result += "Ctrl+";
            if (Shift) result += "Shift+";
            if (Alt) result += "Alt+";
            result += KeyCode.ToString();
            return result;
        }

        public void FromHotkeyString(string hotkeyStr)
        {
            Control = Shift = Alt = false;
            KeyCode = Keys.None;
            if (string.IsNullOrWhiteSpace(hotkeyStr)) return;

            string[] parts = hotkeyStr.Split('+');
            foreach (string part in parts)
            {
                string trim = part.Trim();
                if (trim.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
                    Control = true;
                else if (trim.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    Shift = true;
                else if (trim.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    Alt = true;
                else if (Enum.TryParse(trim, true, out Keys key))
                    KeyCode = key;
            }
        }
    }
}