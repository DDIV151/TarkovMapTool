namespace TarkovMapTool
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // 控件声明
        private System.Windows.Forms.Label labelScreenshotDir;
        private System.Windows.Forms.TextBox textBoxScreenshotDir;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelHotkey;
        private System.Windows.Forms.TextBox textBoxHotkey;
        private System.Windows.Forms.Button buttonSetHotkey;
        private System.Windows.Forms.Label labelWindowHotkey;
        private System.Windows.Forms.Button buttonSetZoomInHotkey;
        private System.Windows.Forms.TextBox textBoxZoomInHotkey;
        private System.Windows.Forms.Button buttonSetZoomOutHotkey;
        private System.Windows.Forms.TextBox textBoxZoomOutHotkey;
        private System.Windows.Forms.Button buttonLock;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonAddCalibPoint;
        private System.Windows.Forms.Button buttonFinishCalib;
        private System.Windows.Forms.Button buttonResetCalib;
        private System.Windows.Forms.Button buttonPreviewMap;             // ★ 新增
        private System.Windows.Forms.Label labelCalibStatus;
        private System.Windows.Forms.ListBox listBoxCalibPoints;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelMaps;
        private System.Windows.Forms.Button[] mapButtons;
        private readonly string[] mapNames = new string[]
        {
            "中心区", "塔科夫街区", "海岸线", "立交桥",
            "森林", "灯塔", "储备站", "海关",
            "工厂", "实验室", "迷宫", "码头"
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.labelScreenshotDir = new System.Windows.Forms.Label();
            this.textBoxScreenshotDir = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.labelHotkey = new System.Windows.Forms.Label();
            this.textBoxHotkey = new System.Windows.Forms.TextBox();
            this.buttonSetHotkey = new System.Windows.Forms.Button();
            this.labelWindowHotkey = new System.Windows.Forms.Label();
            this.buttonSetZoomInHotkey = new System.Windows.Forms.Button();
            this.textBoxZoomInHotkey = new System.Windows.Forms.TextBox();
            this.buttonSetZoomOutHotkey = new System.Windows.Forms.Button();
            this.textBoxZoomOutHotkey = new System.Windows.Forms.TextBox();
            this.buttonLock = new System.Windows.Forms.Button();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonAddCalibPoint = new System.Windows.Forms.Button();
            this.buttonFinishCalib = new System.Windows.Forms.Button();
            this.buttonResetCalib = new System.Windows.Forms.Button();
            this.buttonPreviewMap = new System.Windows.Forms.Button();    // ★ 新增
            this.labelCalibStatus = new System.Windows.Forms.Label();
            this.listBoxCalibPoints = new System.Windows.Forms.ListBox();
            this.flowLayoutPanelMaps = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();

            // labelScreenshotDir
            this.labelScreenshotDir.AutoSize = true;
            this.labelScreenshotDir.Location = new System.Drawing.Point(15, 15);
            this.labelScreenshotDir.Size = new System.Drawing.Size(65, 13);
            this.labelScreenshotDir.Text = "截图目录：";
            // textBoxScreenshotDir
            this.textBoxScreenshotDir.Location = new System.Drawing.Point(80, 12);
            this.textBoxScreenshotDir.Size = new System.Drawing.Size(350, 20);
            this.textBoxScreenshotDir.Text = "（未设置）";
            // buttonBrowse
            this.buttonBrowse.Location = new System.Drawing.Point(440, 10);
            this.buttonBrowse.Size = new System.Drawing.Size(60, 23);
            this.buttonBrowse.Text = "浏览...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            // labelHotkey
            this.labelHotkey.AutoSize = true;
            this.labelHotkey.Location = new System.Drawing.Point(15, 50);
            this.labelHotkey.Size = new System.Drawing.Size(91, 13);
            this.labelHotkey.Text = "截图触发快捷键：";
            // textBoxHotkey
            this.textBoxHotkey.Location = new System.Drawing.Point(110, 47);
            this.textBoxHotkey.ReadOnly = true;
            this.textBoxHotkey.Size = new System.Drawing.Size(140, 20);
            this.textBoxHotkey.Text = "（未设置）";
            // buttonSetHotkey
            this.buttonSetHotkey.Location = new System.Drawing.Point(260, 45);
            this.buttonSetHotkey.Size = new System.Drawing.Size(80, 23);
            this.buttonSetHotkey.Text = "设置快捷键";
            this.buttonSetHotkey.UseVisualStyleBackColor = true;
            // labelWindowHotkey
            this.labelWindowHotkey.AutoSize = true;
            this.labelWindowHotkey.Location = new System.Drawing.Point(15, 85);
            this.labelWindowHotkey.Size = new System.Drawing.Size(79, 13);
            this.labelWindowHotkey.Text = "窗口快捷键：";
            // buttonSetZoomInHotkey
            this.buttonSetZoomInHotkey.Location = new System.Drawing.Point(100, 80);
            this.buttonSetZoomInHotkey.Size = new System.Drawing.Size(80, 23);
            this.buttonSetZoomInHotkey.Text = "放大快捷键";
            this.buttonSetZoomInHotkey.UseVisualStyleBackColor = true;
            // textBoxZoomInHotkey
            this.textBoxZoomInHotkey.Location = new System.Drawing.Point(190, 82);
            this.textBoxZoomInHotkey.ReadOnly = true;
            this.textBoxZoomInHotkey.Size = new System.Drawing.Size(100, 20);
            this.textBoxZoomInHotkey.Text = "（未设置）";
            // buttonSetZoomOutHotkey
            this.buttonSetZoomOutHotkey.Location = new System.Drawing.Point(300, 80);
            this.buttonSetZoomOutHotkey.Size = new System.Drawing.Size(80, 23);
            this.buttonSetZoomOutHotkey.Text = "缩小快捷键";
            this.buttonSetZoomOutHotkey.UseVisualStyleBackColor = true;
            // textBoxZoomOutHotkey
            this.textBoxZoomOutHotkey.Location = new System.Drawing.Point(390, 82);
            this.textBoxZoomOutHotkey.ReadOnly = true;
            this.textBoxZoomOutHotkey.Size = new System.Drawing.Size(100, 20);
            this.textBoxZoomOutHotkey.Text = "（未设置）";
            // buttonLock
            this.buttonLock.Enabled = false;
            this.buttonLock.Location = new System.Drawing.Point(15, 120);
            this.buttonLock.Size = new System.Drawing.Size(100, 30);
            this.buttonLock.Text = "解锁";
            this.buttonLock.UseVisualStyleBackColor = true;
            // buttonStart
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.buttonStart.Location = new System.Drawing.Point(125, 120);
            this.buttonStart.Size = new System.Drawing.Size(200, 30);
            this.buttonStart.Text = "启动地图浮窗";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.BackColor = System.Drawing.Color.LightGreen;
            // buttonAddCalibPoint
            this.buttonAddCalibPoint.Enabled = false;
            this.buttonAddCalibPoint.Location = new System.Drawing.Point(340, 120);
            this.buttonAddCalibPoint.Size = new System.Drawing.Size(80, 30);
            this.buttonAddCalibPoint.Text = "添加校准点";
            this.buttonAddCalibPoint.UseVisualStyleBackColor = true;
            // buttonFinishCalib
            this.buttonFinishCalib.Enabled = false;
            this.buttonFinishCalib.Location = new System.Drawing.Point(430, 120);
            this.buttonFinishCalib.Size = new System.Drawing.Size(80, 30);
            this.buttonFinishCalib.Text = "完成校准";
            this.buttonFinishCalib.UseVisualStyleBackColor = true;
            // buttonResetCalib
            this.buttonResetCalib.Enabled = false;
            this.buttonResetCalib.Location = new System.Drawing.Point(340, 155);
            this.buttonResetCalib.Size = new System.Drawing.Size(80, 25);
            this.buttonResetCalib.Text = "清空列表";
            this.buttonResetCalib.UseVisualStyleBackColor = true;
            // buttonPreviewMap ★ 新增
            this.buttonPreviewMap.Location = new System.Drawing.Point(340, 185);    // 位置根据需要微调
            this.buttonPreviewMap.Size = new System.Drawing.Size(170, 25);
            this.buttonPreviewMap.Text = "预览地图";
            this.buttonPreviewMap.UseVisualStyleBackColor = true;
            // labelCalibStatus
            this.labelCalibStatus.AutoSize = true;
            this.labelCalibStatus.Location = new System.Drawing.Point(15, 190);
            this.labelCalibStatus.Size = new System.Drawing.Size(0, 13);
            // listBoxCalibPoints
            this.listBoxCalibPoints.FormattingEnabled = true;
            this.listBoxCalibPoints.Location = new System.Drawing.Point(15, 210);
            this.listBoxCalibPoints.Size = new System.Drawing.Size(490, 82);
            // flowLayoutPanelMaps
            this.flowLayoutPanelMaps.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanelMaps.Location = new System.Drawing.Point(15, 298);
            this.flowLayoutPanelMaps.Size = new System.Drawing.Size(490, 100);
            this.flowLayoutPanelMaps.AutoScroll = true;

            this.mapButtons = new System.Windows.Forms.Button[mapNames.Length];
            for (int i = 0; i < mapNames.Length; i++)
            {
                var btn = new System.Windows.Forms.Button();
                btn.Text = mapNames[i];
                btn.Size = new System.Drawing.Size(90, 30);
                btn.Tag = mapNames[i];
                btn.UseVisualStyleBackColor = true;
                this.mapButtons[i] = btn;
                this.flowLayoutPanelMaps.Controls.Add(btn);
            }

            // Form1
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 410);
            this.Controls.Add(this.labelScreenshotDir);
            this.Controls.Add(this.textBoxScreenshotDir);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.labelHotkey);
            this.Controls.Add(this.textBoxHotkey);
            this.Controls.Add(this.buttonSetHotkey);
            this.Controls.Add(this.labelWindowHotkey);
            this.Controls.Add(this.buttonSetZoomInHotkey);
            this.Controls.Add(this.textBoxZoomInHotkey);
            this.Controls.Add(this.buttonSetZoomOutHotkey);
            this.Controls.Add(this.textBoxZoomOutHotkey);
            this.Controls.Add(this.buttonLock);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.buttonAddCalibPoint);
            this.Controls.Add(this.buttonFinishCalib);
            this.Controls.Add(this.buttonResetCalib);
            this.Controls.Add(this.buttonPreviewMap);           // ★ 新增
            this.Controls.Add(this.labelCalibStatus);
            this.Controls.Add(this.listBoxCalibPoints);
            this.Controls.Add(this.flowLayoutPanelMaps);
            this.Name = "Form1";
            this.Text = "塔科夫地图助手";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}