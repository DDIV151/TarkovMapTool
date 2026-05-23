using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace TarkovMapTool
{
    public class MapPreviewForm : Form
    {
        private string currentMap;
        private Image mapImage;
        private MapConfigItem config;
        private float zoom = 1.0f;
        private float offsetX = 0, offsetY = 0;
        private bool isPanning = false;
        private Point panStartPoint;
        private float panStartOffsetX, panStartOffsetY;

        // 校准模式
        private bool calibrationMode = false;
        private Action<Point> onCalibrationClick;

        // 鼠标坐标标签
        private Label lblCoord;

        public MapPreviewForm(string mapName)
        {
            currentMap = mapName;
            this.Text = $"地图预览 - {mapName}";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.KeyPreview = true;

            lblCoord = new Label
            {
                Text = "坐标: ---",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(128, Color.Black),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            this.Controls.Add(lblCoord);

            this.Paint += MapPreviewForm_Paint;
            this.MouseDown += MapPreviewForm_MouseDown;
            this.MouseMove += MapPreviewForm_MouseMove;
            this.MouseUp += MapPreviewForm_MouseUp;
            this.MouseClick += MapPreviewForm_MouseClick;
            this.MouseWheel += MapPreviewForm_MouseWheel;
            this.Load += MapPreviewForm_Load;
            this.FormClosed += MapPreviewForm_FormClosed;

            LoadMap();
        }

        private void MapPreviewForm_Load(object sender, EventArgs e) => FitImage();

        private void MapPreviewForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            mapImage?.Dispose();
        }

        public void SetMap(string mapName)
        {
            currentMap = mapName;
            this.Text = $"地图预览 - {mapName}";
            LoadMap();
            FitImage();
            Invalidate();
        }

        private void LoadMap()
        {
            mapImage?.Dispose();
            mapImage = null;
            config = MapConfigManager.GetConfig(currentMap);
            if (config == null) return;

            string imgPath = Path.Combine(Application.StartupPath, "maps", config.FileName);
            if (File.Exists(imgPath))
            {
                try { mapImage = Image.FromFile(imgPath); }
                catch { mapImage = null; }
            }
        }

        private void FitImage()
        {
            if (mapImage == null) return;
            float scaleX = (float)this.ClientSize.Width / mapImage.Width;
            float scaleY = (float)this.ClientSize.Height / mapImage.Height;
            zoom = Math.Min(scaleX, scaleY);
            offsetX = (this.ClientSize.Width - mapImage.Width * zoom) / 2;
            offsetY = (this.ClientSize.Height - mapImage.Height * zoom) / 2;
        }

        public void EnterCalibrationMode(Action<Point> callback)
        {
            calibrationMode = true;
            onCalibrationClick = callback;
            this.Cursor = Cursors.Cross;
            lblCoord.Text = "校准模式：左键点击标记位置";
            Invalidate();
        }

        public void ExitCalibrationMode()
        {
            calibrationMode = false;
            onCalibrationClick = null;
            this.Cursor = Cursors.Default;
            Invalidate();
        }

        // 鼠标事件
        private void MapPreviewForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (calibrationMode) return;
            if (e.Button == MouseButtons.Right)
            {
                isPanning = true;
                panStartPoint = e.Location;
                panStartOffsetX = offsetX;
                panStartOffsetY = offsetY;
                this.Cursor = Cursors.Hand;
            }
        }

        private void MapPreviewForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (calibrationMode)
            {
                // 显示鼠标位置的游戏坐标
                Point imgPt = WindowToImage(e.Location);
                string coordText = $"像素({imgPt.X},{imgPt.Y})";
                if (config != null && config.HasAffine)
                {
                    double gx = config.AffineA * imgPt.X + config.AffineB * imgPt.Y + config.AffineC;
                    double gz = config.AffineD * imgPt.X + config.AffineE * imgPt.Y + config.AffineF;
                    coordText += $"  游戏({gx:F2}, {gz:F2})";
                }
                else
                {
                    coordText += "  游戏(未校准)";
                }
                lblCoord.Text = coordText;
                return;
            }

            if (isPanning)
            {
                offsetX = panStartOffsetX + e.X - panStartPoint.X;
                offsetY = panStartOffsetY + e.Y - panStartPoint.Y;
                Invalidate();
            }
            else
            {
                // 非拖动时显示坐标
                Point imgPt = WindowToImage(e.Location);
                string coordText = $"像素({imgPt.X},{imgPt.Y})";
                if (config != null && config.HasAffine)
                {
                    double gx = config.AffineA * imgPt.X + config.AffineB * imgPt.Y + config.AffineC;
                    double gz = config.AffineD * imgPt.X + config.AffineE * imgPt.Y + config.AffineF;
                    coordText += $"  游戏({gx:F2}, {gz:F2})";
                }
                else
                {
                    coordText += "  游戏(未校准)";
                }
                lblCoord.Text = coordText;
            }
        }

        private void MapPreviewForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (calibrationMode) return;
            isPanning = false;
            this.Cursor = Cursors.Default;
        }

        private void MapPreviewForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (!calibrationMode) return;
            if (e.Button != MouseButtons.Left) return;
            Point imgPt = WindowToImage(e.Location);
            onCalibrationClick?.Invoke(imgPt);
        }

        private void MapPreviewForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (mapImage == null) return;
            float factor = e.Delta > 0 ? 1.1f : 1 / 1.1f;
            float oldZoom = zoom;
            zoom = Math.Max(0.05f, Math.Min(5.0f, zoom * factor));
            // 以鼠标为中心缩放
            float imgX = (e.X - offsetX) / oldZoom;
            float imgY = (e.Y - offsetY) / oldZoom;
            offsetX = e.X - imgX * zoom;
            offsetY = e.Y - imgY * zoom;
            Invalidate();
        }

        private Point WindowToImage(Point windowPt)
        {
            float imgX = (windowPt.X - offsetX) / zoom;
            float imgY = (windowPt.Y - offsetY) / zoom;
            return new Point((int)Math.Round(imgX), (int)Math.Round(imgY));
        }

        private void MapPreviewForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (mapImage == null)
            {
                g.Clear(Color.Black);
                g.DrawString("地图图片未找到", SystemFonts.DefaultFont, Brushes.White, 10, 10);
                return;
            }

            int drawW = (int)(mapImage.Width * zoom);
            int drawH = (int)(mapImage.Height * zoom);
            Rectangle destRect = new Rectangle((int)offsetX, (int)offsetY, drawW, drawH);
            g.DrawImage(mapImage, destRect);

            if (calibrationMode)
            {
                g.DrawString("校准模式：左键点击标记位置", SystemFonts.DefaultFont, Brushes.Yellow, 10, 30);
            }
        }
    }
}