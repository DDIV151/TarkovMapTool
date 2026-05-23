using System;
using System.Drawing;
using System.Windows.Forms;

namespace TarkovMapTool
{
    public class CoordinateInputForm : Form
    {
        public float X { get; private set; }
        public float Z { get; private set; }
        public bool Confirmed { get; private set; }

        private TextBox txtX, txtZ;

        public CoordinateInputForm()
        {
            // 基本窗体设置
            this.Text = "输入游戏坐标";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(10);

            // 使用 TableLayoutPanel 自动布局
            var table = new TableLayoutPanel
            {
                ColumnCount = 4,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill
            };

            // 列样式：标签 - 输入框 - 标签 - 输入框
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

            // 第一行：X: [txtX]  Z: [txtZ]
            Label lblX = new Label
            {
                Text = "X:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            txtX = new TextBox { Width = 80 };

            Label lblZ = new Label
            {
                Text = "Z:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            txtZ = new TextBox { Width = 80 };

            table.Controls.Add(lblX, 0, 0);
            table.Controls.Add(txtX, 1, 0);
            table.Controls.Add(lblZ, 2, 0);
            table.Controls.Add(txtZ, 3, 0);

            // 第二行：确定 取消（合并跨列）
            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 10, 0, 0)
            };

            Button btnOK = new Button { Text = "确定", Width = 75, Height = 28 };
            Button btnCancel = new Button { Text = "取消", Width = 75, Height = 28 };
            buttonPanel.Controls.Add(btnOK);
            buttonPanel.Controls.Add(btnCancel);

            table.Controls.Add(buttonPanel, 0, 1);
            table.SetColumnSpan(buttonPanel, 4);

            this.Controls.Add(table);

            // 设置对话框按钮行为
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // 确定事件
            btnOK.Click += (s, e) =>
            {
                if (float.TryParse(txtX.Text, out float x) && float.TryParse(txtZ.Text, out float z))
                {
                    X = x;
                    Z = z;
                    Confirmed = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("请输入有效的数字", "输入错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtX.Focus();
                }
            };

            // 取消事件
            btnCancel.Click += (s, e) =>
            {
                Confirmed = false;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            // 显示后直接聚焦第一个输入框
            this.Shown += (s, e) =>
            {
                this.ActiveControl = txtX;
                txtX.SelectAll();
            };
        }
    }
}