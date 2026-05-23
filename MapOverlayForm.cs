using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace TarkovMapTool
{
    public class MapOverlayForm : Form
    {
        private string currentMap;
        private Image mapImage = null;
        private MapConfigItem config;

        private bool hasPosition = false;
        private float playerX, playerZ, playerYaw;
        private float zoomScale = 0.3f;
        private const float MIN_ZOOM = 0.1f, MAX_ZOOM = 2.0f;

        private bool isLocked = true;
        private bool dragging = false;
        private Point dragStartPoint;
        private bool resizing = false;
        private Point resizeStartPoint;
        private Size resizeStartSize;

        // 校准模式
        private bool calibrationMode = false;
        private Action<Point> onCalibrationClick;
        private float calibrationZoom = 1.0f;
        private float calibrationOffsetX = 0, calibrationOffsetY = 0;
        private Point? lastPanPoint = null;
        private const float CALIB_MIN_ZOOM = 0.05f, CALIB_MAX_ZOOM = 5.0f;

        public int MapImageWidth => mapImage?.Width ?? 0;
        public int MapImageHeight => mapImage?.Height ?? 0;

        public MapOverlayForm(string map)
        {
            currentMap = map;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.MinimumSize = new Size(100, 100);

            this.Paint += MapOverlayForm_Paint;
            this.Load += MapOverlayForm_Load;
            this.MouseDown += MapOverlayForm_MouseDown;
            this.MouseMove += MapOverlayForm_MouseMove;
            this.MouseUp += MapOverlayForm_MouseUp;
            this.MouseClick += MapOverlayForm_MouseClick;
            this.MouseWheel += MapOverlayForm_MouseWheel;

            MapConfigManager.ConfigChanged += () =>
            {
                if (this.InvokeRequired) this.Invoke(new Action(ReloadConfig));
                else ReloadConfig();
            };
        }

        private void ReloadConfig() => LoadMap(currentMap);
        private void MapOverlayForm_Load(object sender, EventArgs e) => LoadMap(currentMap);

        public void SetMap(string mapName)
        {
            if (currentMap == mapName) return;
            currentMap = mapName;
            LoadMap(mapName);
            Invalidate();
        }

        public void UpdatePosition(float x, float z, float yaw)
        {
            playerX = x;
            playerZ = z;
            playerYaw = yaw + (float)Math.PI;
            if (playerYaw > Math.PI) playerYaw -= 2 * (float)Math.PI;
            else if (playerYaw < -Math.PI) playerYaw += 2 * (float)Math.PI;
            hasPosition = true;
            Invalidate();
        }

        public void ZoomIn()
        {
            if (calibrationMode) CalibZoom(1.2f, this.ClientSize.Width / 2f, this.ClientSize.Height / 2f);
            else { zoomScale = Math.Min(MAX_ZOOM, zoomScale * 1.2f); Invalidate(); }
        }
        public void ZoomOut()
        {
            if (calibrationMode) CalibZoom(1 / 1.2f, this.ClientSize.Width / 2f, this.ClientSize.Height / 2f);
            else { zoomScale = Math.Max(MIN_ZOOM, zoomScale / 1.2f); Invalidate(); }
        }

        public void SetLocked(bool locked) => isLocked = locked;

        public void EnterCalibrationMode(Action<Point> onClickCallback)
        {
            calibrationMode = true;
            onCalibrationClick = onClickCallback;
            FitCalibrationView();
            this.Cursor = Cursors.Cross;
            Invalidate();
        }
        public void ExitCalibrationMode()
        {
            calibrationMode = false;
            onCalibrationClick = null;
            this.Cursor = Cursors.Default;
            Invalidate();
        }

        private void LoadMap(string mapName)
        {
            if (mapImage != null) { mapImage.Dispose(); mapImage = null; }
            config = MapConfigManager.GetConfig(mapName);
            if (config == null) return;

            string imgPath = Path.Combine(Application.StartupPath, "maps", config.FileName);
            if (File.Exists(imgPath))
            {
                try { mapImage = Image.FromFile(imgPath); }
                catch { mapImage = null; }
            }
            hasPosition = false;
        }

        // ---------- 鼠标事件 ----------
        private void MapOverlayForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (calibrationMode)
            {
                if (e.Button == MouseButtons.Right) { lastPanPoint = e.Location; this.Cursor = Cursors.Hand; }
                return;
            }
            if (isLocked) return;
            if (e.X >= this.ClientSize.Width - 20 && e.Y >= this.ClientSize.Height - 20)
            {
                resizing = true;
                resizeStartPoint = Cursor.Position;
                resizeStartSize = this.Size;
            }
            else { dragging = true; dragStartPoint = new Point(e.X, e.Y); }
        }
        private void MapOverlayForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (calibrationMode)
            {
                if (lastPanPoint.HasValue)
                {
                    float panDx = e.X - lastPanPoint.Value.X;
                    float panDy = e.Y - lastPanPoint.Value.Y;
                    calibrationOffsetX += panDx; calibrationOffsetY += panDy;
                    lastPanPoint = e.Location;
                    Invalidate();
                }
                else this.Cursor = Cursors.Cross;
                return;
            }
            if (isLocked) return;
            if (dragging) { this.Left += e.X - dragStartPoint.X; this.Top += e.Y - dragStartPoint.Y; }
            else if (resizing)
            {
                Point delta = new Point(Cursor.Position.X - resizeStartPoint.X, Cursor.Position.Y - resizeStartPoint.Y);
                this.Width = Math.Max(100, resizeStartSize.Width + delta.X);
                this.Height = Math.Max(100, resizeStartSize.Height + delta.Y);
            }
            else this.Cursor = (e.X >= this.ClientSize.Width - 20 && e.Y >= this.ClientSize.Height - 20) ? Cursors.SizeNWSE : Cursors.SizeAll;
        }
        private void MapOverlayForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (calibrationMode) { lastPanPoint = null; this.Cursor = Cursors.Cross; return; }
            dragging = false; resizing = false;
        }
        private void MapOverlayForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (!calibrationMode || mapImage == null) return;
            if (e.Button != MouseButtons.Left) return;
            Point imgPt = WindowToImage(e.Location);
            onCalibrationClick?.Invoke(imgPt);
        }
        private void MapOverlayForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!calibrationMode) return;
            float factor = e.Delta > 0 ? 1.1f : 1 / 1.1f;
            CalibZoom(factor, e.X, e.Y);
        }

        private void CalibZoom(float factor, float centerX, float centerY)
        {
            float oldZoom = calibrationZoom;
            float newZoom = Math.Max(CALIB_MIN_ZOOM, Math.Min(CALIB_MAX_ZOOM, oldZoom * factor));
            if (newZoom == oldZoom) return;
            float imgX = (centerX - calibrationOffsetX) / oldZoom;
            float imgY = (centerY - calibrationOffsetY) / oldZoom;
            calibrationOffsetX = centerX - imgX * newZoom;
            calibrationOffsetY = centerY - imgY * newZoom;
            calibrationZoom = newZoom;
            Invalidate();
        }
        private void FitCalibrationView()
        {
            if (mapImage == null) return;
            float scaleX = (float)this.ClientSize.Width / mapImage.Width;
            float scaleY = (float)this.ClientSize.Height / mapImage.Height;
            calibrationZoom = Math.Min(scaleX, scaleY);
            float imgW = mapImage.Width * calibrationZoom;
            float imgH = mapImage.Height * calibrationZoom;
            calibrationOffsetX = (this.ClientSize.Width - imgW) / 2f;
            calibrationOffsetY = (this.ClientSize.Height - imgH) / 2f;
        }
        private Point WindowToImage(Point windowPt)
        {
            float imgX = (windowPt.X - calibrationOffsetX) / calibrationZoom;
            float imgY = (windowPt.Y - calibrationOffsetY) / calibrationZoom;
            return new Point((int)Math.Round(imgX), (int)Math.Round(imgY));
        }

        // ---------- 绘制 ----------
        private void MapOverlayForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (mapImage == null)
            {
                g.Clear(Color.Black);
                g.DrawString($"地图图片未找到：{currentMap}", SystemFonts.DefaultFont, Brushes.White, 10, 10);
                return;
            }

            if (calibrationMode)
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                int drawW = (int)(mapImage.Width * calibrationZoom);
                int drawH = (int)(mapImage.Height * calibrationZoom);
                Rectangle destRect = new Rectangle((int)calibrationOffsetX, (int)calibrationOffsetY, drawW, drawH);
                g.DrawImage(mapImage, destRect);
                g.DrawString("右键拖动平移，滚轮缩放，左键点击标记", SystemFonts.DefaultFont, Brushes.Yellow, 5, 5);
                return;
            }

            if (!hasPosition || config == null || !config.HasAffine)
            {
                float scale = Math.Min((float)this.ClientSize.Width / mapImage.Width,
                                       (float)this.ClientSize.Height / mapImage.Height);
                int w = (int)(mapImage.Width * scale);
                int h = (int)(mapImage.Height * scale);
                int x = (this.ClientSize.Width - w) / 2;
                int y = (this.ClientSize.Height - h) / 2;
                g.DrawImage(mapImage, x, y, w, h);
                g.DrawString($"{currentMap} (等待定位...)", SystemFonts.DefaultFont, Brushes.White, 5, 5);
                return;
            }

            // 使用仿射矩阵反求像素坐标
            double affA = config.AffineA, affB = config.AffineB, affC = config.AffineC;
            double affD = config.AffineD, affE = config.AffineE, affF = config.AffineF;
            double det = affA * affE - affB * affD;
            if (Math.Abs(det) < 1e-6) return;

            double imgX = (affE * (playerX - affC) - affB * (playerZ - affF)) / det;
            double imgY = (affA * (playerZ - affF) - affD * (playerX - affC)) / det;
            float mapPx = (float)imgX;
            float mapPy = (float)imgY;

            float srcWidth = this.ClientSize.Width / zoomScale;
            float srcHeight = this.ClientSize.Height / zoomScale;
            float srcX = mapPx - srcWidth / 2f;
            float srcY = mapPy - srcHeight / 2f;

            if (srcX < 0) srcX = 0;
            if (srcY < 0) srcY = 0;
            if (srcX + srcWidth > mapImage.Width) srcX = mapImage.Width - srcWidth;
            if (srcY + srcHeight > mapImage.Height) srcY = mapImage.Height - srcHeight;
            if (srcWidth > mapImage.Width) { srcX = 0; srcWidth = mapImage.Width; }
            if (srcHeight > mapImage.Height) { srcY = 0; srcHeight = mapImage.Height; }

            RectangleF srcRect = new RectangleF(srcX, srcY, srcWidth, srcHeight);
            g.DrawImage(mapImage, this.ClientRectangle, srcRect, GraphicsUnit.Pixel);

            float screenX = (mapPx - srcX) * zoomScale;
            float screenY = (mapPy - srcY) * zoomScale;

            int size = 10;
            g.FillEllipse(Brushes.LimeGreen, screenX - size / 2f, screenY - size / 2f, size, size);
            g.DrawEllipse(Pens.Black, screenX - size / 2f, screenY - size / 2f, size, size);

            float arrowLen = 25 * zoomScale;
            float arrowDx = arrowLen * (float)Math.Sin(playerYaw);
            float arrowDy = -arrowLen * (float)Math.Cos(playerYaw);
            PointF arrowTip = new PointF(screenX + arrowDx, screenY + arrowDy);
            using (Pen redPen = new Pen(Color.Red, 2))
                g.DrawLine(redPen, screenX, screenY, arrowTip.X, arrowTip.Y);
            float wingLen = 6 * zoomScale;
            float backAngle = (float)Math.Atan2(-arrowDy, -arrowDx);
            PointF wing1 = new PointF(arrowTip.X + wingLen * (float)Math.Cos(backAngle - Math.PI / 6),
                                       arrowTip.Y + wingLen * (float)Math.Sin(backAngle - Math.PI / 6));
            PointF wing2 = new PointF(arrowTip.X + wingLen * (float)Math.Cos(backAngle + Math.PI / 6),
                                       arrowTip.Y + wingLen * (float)Math.Sin(backAngle + Math.PI / 6));
            g.DrawLine(Pens.Red, arrowTip, wing1);
            g.DrawLine(Pens.Red, arrowTip, wing2);

            g.DrawString($"{currentMap}  X:{playerX:F1} Z:{playerZ:F1}  Zoom:{zoomScale:P0}",
                SystemFonts.DefaultFont, Brushes.White, 5, 5);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) mapImage?.Dispose();
            base.Dispose(disposing);
        }
    }
}