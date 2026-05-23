using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TarkovMapTool.Properties;

namespace TarkovMapTool
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004;
        private const int HOTKEY_ID_SCREENSHOT = 1, HOTKEY_ID_ZOOM_IN = 2, HOTKEY_ID_ZOOM_OUT = 3;

        private string selectedMap = "海关";
        private MapOverlayForm overlayForm = null;
        private MapPreviewForm previewForm = null;
        private bool isUnlocked = false;
        private FileSystemWatcher screenshotWatcher;
        private string screenshotDir = "";

        // 校准点列表（无数量限制）
        private List<(Point pixel, float x, float z)> calibPoints = new List<(Point, float, float)>();
        private bool waitingForCalibClick = false;
        private Label persistentHintLabel = null;

        public Form1()
        {
            InitializeComponent();
            BindEvents();
            LoadSettings();
            InitializeScreenshotWatcher();
            this.KeyPreview = true;
            this.FormClosed += Form1_FormClosed;
        }

        private void BindEvents()
        {
            buttonBrowse.Click += ButtonBrowse_Click;
            buttonSetHotkey.Click += ButtonSetHotkey_Click;
            buttonSetZoomInHotkey.Click += ButtonSetZoomInHotkey_Click;
            buttonSetZoomOutHotkey.Click += ButtonSetZoomOutHotkey_Click;
            buttonLock.Click += ButtonLock_Click;
            buttonStart.Click += ButtonStart_Click;
            buttonAddCalibPoint.Click += ButtonAddCalibPoint_Click;
            buttonFinishCalib.Click += ButtonFinishCalib_Click;
            buttonResetCalib.Click += ButtonResetCalib_Click;
            buttonPreviewMap.Click += ButtonPreviewMap_Click;
            foreach (var btn in mapButtons) btn.Click += MapButton_Click;
            HighlightMapButton(selectedMap);
            UpdateCalibButtonStates();
        }

        private void LoadSettings()
        {
            textBoxScreenshotDir.Text = Settings.Default.ScreenshotPath ?? "";
            textBoxHotkey.Text = string.IsNullOrEmpty(Settings.Default.ScreenshotHotkey) ? "（未设置）" : Settings.Default.ScreenshotHotkey;
            textBoxZoomInHotkey.Text = string.IsNullOrEmpty(Settings.Default.ZoomInHotkey) ? "（未设置）" : Settings.Default.ZoomInHotkey;
            textBoxZoomOutHotkey.Text = string.IsNullOrEmpty(Settings.Default.ZoomOutHotkey) ? "（未设置）" : Settings.Default.ZoomOutHotkey;
        }

        // ---------- 全局热键 ----------
        private void RegisterHotKeysForOverlay()
        {
            UnregisterHotKeysForOverlay();
            TryRegisterHotkey(Settings.Default.ScreenshotHotkey, HOTKEY_ID_SCREENSHOT);
            TryRegisterHotkey(Settings.Default.ZoomInHotkey, HOTKEY_ID_ZOOM_IN);
            TryRegisterHotkey(Settings.Default.ZoomOutHotkey, HOTKEY_ID_ZOOM_OUT);
        }
        private void UnregisterHotKeysForOverlay()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID_SCREENSHOT);
            UnregisterHotKey(this.Handle, HOTKEY_ID_ZOOM_IN);
            UnregisterHotKey(this.Handle, HOTKEY_ID_ZOOM_OUT);
        }
        private void TryRegisterHotkey(string hotkeyString, int id)
        {
            if (string.IsNullOrWhiteSpace(hotkeyString)) return;
            ParseHotkeyString(hotkeyString, out uint modifiers, out uint vk);
            if (vk != 0 && !RegisterHotKey(this.Handle, id, modifiers, vk))
                MessageBox.Show($"热键“{hotkeyString}”注册失败，可能被占用。", "热键冲突");
        }
        private void ParseHotkeyString(string hotkey, out uint modifiers, out uint key)
        {
            modifiers = 0; key = 0;
            foreach (string part in hotkey.Split('+'))
            {
                string t = part.Trim();
                if (t.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) modifiers |= MOD_CONTROL;
                else if (t.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= MOD_SHIFT;
                else if (t.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= MOD_ALT;
                else if (Enum.TryParse(t, true, out Keys k)) key = (uint)k;
            }
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                switch (id)
                {
                    case HOTKEY_ID_SCREENSHOT: ProcessLatestScreenshot(); break;
                    case HOTKEY_ID_ZOOM_IN: overlayForm?.ZoomIn(); break;
                    case HOTKEY_ID_ZOOM_OUT: overlayForm?.ZoomOut(); break;
                }
            }
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnregisterHotKeysForOverlay();
            if (overlayForm != null && !overlayForm.IsDisposed)
            {
                if (isUnlocked) SaveOverlayPosition();
                overlayForm.Close();
            }
            previewForm?.Close();
        }

        // ---------- 截图监控 ----------
        private void InitializeScreenshotWatcher()
        {
            string path = Settings.Default.ScreenshotPath;
            if (!string.IsNullOrEmpty(path)) { screenshotDir = path; StartWatcher(path); }
        }
        private void StartWatcher(string path)
        {
            if (!Directory.Exists(path)) return;
            if (screenshotWatcher != null) { screenshotWatcher.EnableRaisingEvents = false; screenshotWatcher.Dispose(); }
            screenshotWatcher = new FileSystemWatcher(path, "*.png");
            screenshotWatcher.Created += OnScreenshotCreated;
            screenshotWatcher.EnableRaisingEvents = true;
        }
        private void OnScreenshotCreated(object sender, FileSystemEventArgs e)
        {
            if (this.InvokeRequired) this.Invoke(new Action(ProcessLatestScreenshot));
            else ProcessLatestScreenshot();
        }
        private void ProcessLatestScreenshot()
        {
            if (overlayForm == null || overlayForm.IsDisposed) return;
            var coords = GetLatestScreenshotCoordinates();
            if (coords != null) overlayForm.UpdatePosition(coords.Value.x, coords.Value.z, coords.Value.yaw);
        }
        private (float x, float z, float yaw)? GetLatestScreenshotCoordinates()
        {
            if (string.IsNullOrEmpty(screenshotDir)) return null;
            string[] files = Directory.GetFiles(screenshotDir, "*.png");
            if (files.Length == 0) return null;
            Array.Sort(files, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
            return ParseScreenshotFile(files[0]);
        }
        private (float x, float z, float yaw)? ParseScreenshotFile(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            var match = Regex.Match(fileName,
                @"\[\d{2}-\d{2}\]_(-?\d+\.\d+), (-?\d+\.\d+), (-?\d+\.\d+)_(-?\d+\.\d+), (-?\d+\.\d+), (-?\d+\.\d+), (-?\d+\.\d+)_");
            if (!match.Success) return null;
            float x = float.Parse(match.Groups[1].Value);
            float z = float.Parse(match.Groups[3].Value);
            float qx = float.Parse(match.Groups[4].Value), qy = float.Parse(match.Groups[5].Value);
            float qz = float.Parse(match.Groups[6].Value), qw = float.Parse(match.Groups[7].Value);
            float yaw = (float)Math.Atan2(2 * (qw * qy + qx * qz), 1 - 2 * (qy * qy + qx * qx));
            return (x, z, yaw);
        }

        // ---------- 校准功能（支持多点仿射拟合） ----------
        private MapConfigItem GetCurrentConfig() => MapConfigManager.GetConfig(selectedMap);

        private void UpdateCalibButtonStates()
        {
            bool anyMapOpen = (overlayForm != null && !overlayForm.IsDisposed) ||
                              (previewForm != null && !previewForm.IsDisposed);
            buttonAddCalibPoint.Enabled = anyMapOpen && !waitingForCalibClick;  // 无数量限制
            buttonFinishCalib.Enabled = anyMapOpen && calibPoints.Count >= 2;   // 至少2个点才能校准
            buttonResetCalib.Enabled = anyMapOpen && calibPoints.Count > 0;
            labelCalibStatus.Text = calibPoints.Count > 0 ? $"已记录 {calibPoints.Count} 个校准点" : "";
            RefreshCalibListBox();
        }

        private void RefreshCalibListBox()
        {
            listBoxCalibPoints.Items.Clear();
            foreach (var pt in calibPoints)
                listBoxCalibPoints.Items.Add($"像素({pt.pixel.X},{pt.pixel.Y}) → 游戏({pt.x:F2}, {pt.z:F2})");
        }

        private void ButtonAddCalibPoint_Click(object sender, EventArgs e)
        {
            Form targetForm = null;
            Action<Action<Point>> enterCalib = null;
            Action exitCalib = null;

            if (overlayForm != null && !overlayForm.IsDisposed)
            {
                targetForm = overlayForm;
                enterCalib = (cb) => overlayForm.EnterCalibrationMode(cb);
                exitCalib = () => overlayForm.ExitCalibrationMode();
            }
            else if (previewForm != null && !previewForm.IsDisposed)
            {
                targetForm = previewForm;
                enterCalib = (cb) => previewForm.EnterCalibrationMode(cb);
                exitCalib = () => previewForm.ExitCalibrationMode();
            }
            else
            {
                MessageBox.Show("请先启动地图浮窗或打开预览地图。");
                return;
            }

            using (var input = new CoordinateInputForm())
            {
                if (input.ShowDialog(this) != DialogResult.OK || !input.Confirmed) return;
                float gameX = input.X;
                float gameZ = input.Z;

                waitingForCalibClick = true;
                buttonAddCalibPoint.Enabled = false;
                enterCalib((pixel) =>
                {
                    exitCalib();
                    waitingForCalibClick = false;
                    persistentHintLabel?.Dispose();
                    persistentHintLabel = null;
                    calibPoints.Add((pixel, gameX, gameZ));
                    UpdateCalibButtonStates();
                    ShowTemporaryMessage($"已添加点 ({gameX:F2}, {gameZ:F2})", Color.LimeGreen);
                });
                ShowTemporaryMessage("请在地图上左键点击对应位置", Color.Cyan, 0);
            }
        }

        // 最小二乘仿射拟合（6参数）或相似变换（2点）
        private void ButtonFinishCalib_Click(object sender, EventArgs ea)
        {
            int n = calibPoints.Count;
            if (n < 2)
            {
                MessageBox.Show("至少需要2个校准点。");
                return;
            }

            double[] affine = null;

            if (n == 2)
            {
                // 两点相似变换（旋转+统一缩放）
                var p1 = calibPoints[0];
                var p2 = calibPoints[1];
                double dx = p2.pixel.X - p1.pixel.X;
                double dy = p2.pixel.Y - p1.pixel.Y;
                double dgx = p2.x - p1.x;
                double dgz = p2.z - p1.z;

                if (Math.Abs(dx) < 1e-6 && Math.Abs(dy) < 1e-6)
                {
                    MessageBox.Show("两个点像素位置相同，无法校准。");
                    return;
                }

                double denom = dx * dx + dy * dy;
                double a = (dgx * dx + dgz * dy) / denom;
                double b = (dgz * dx - dgx * dy) / denom;
                double c = p1.x - a * p1.pixel.X + b * p1.pixel.Y;
                double f = p1.z - b * p1.pixel.X - a * p1.pixel.Y;

                affine = new double[] { a, -b, c, b, a, f };
            }
            else
            {
                // 多点最小二乘仿射拟合
                // 构建方程组 Ax = b，其中 A 为 2n x 6 矩阵，x = [a,b,c,d,e,f]^T
                int m = 2 * n;
                double[,] A = new double[m, 6];
                double[] B = new double[m];

                for (int i = 0; i < n; i++)
                {
                    double px = calibPoints[i].pixel.X;
                    double py = calibPoints[i].pixel.Y;
                    double gx = calibPoints[i].x;
                    double gz = calibPoints[i].z;

                    // X 方程：gx = a*px + b*py + c
                    A[2 * i, 0] = px; A[2 * i, 1] = py; A[2 * i, 2] = 1.0;
                    A[2 * i, 3] = 0; A[2 * i, 4] = 0; A[2 * i, 5] = 0;
                    B[2 * i] = gx;

                    // Z 方程：gz = d*px + e*py + f
                    A[2 * i + 1, 0] = 0; A[2 * i + 1, 1] = 0; A[2 * i + 1, 2] = 0;
                    A[2 * i + 1, 3] = px; A[2 * i + 1, 4] = py; A[2 * i + 1, 5] = 1.0;
                    B[2 * i + 1] = gz;
                }

                // 正规方程 (A^T A) x = A^T B
                double[,] AtA = new double[6, 6];
                double[] AtB = new double[6];

                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        AtB[j] += A[i, j] * B[i];
                        for (int k = 0; k < 6; k++)
                            AtA[j, k] += A[i, j] * A[i, k];
                    }
                }

                // 高斯消元求解 6x6 方程组
                double[] x = SolveLinearSystem(AtA, AtB, 6);
                if (x == null)
                {
                    MessageBox.Show("矩阵奇异，无法计算仿射矩阵。请确保校准点不共线且分布均匀。");
                    return;
                }

                affine = new double[] { x[0], x[1], x[2], x[3], x[4], x[5] };
            }

            if (affine == null) return;

            var config = MapConfigManager.GetConfig(selectedMap);
            if (config == null) return;

            MapConfigManager.UpdateAffine(selectedMap,
                affine[0], affine[1], affine[2],
                affine[3], affine[4], affine[5],
                0, 0);

            calibPoints.Clear();
            UpdateCalibButtonStates();
            ShowTemporaryMessage("校准成功", Color.LimeGreen, 1500);
        }

        // 解线性方程组（高斯消元）
        private double[] SolveLinearSystem(double[,] A, double[] b, int size)
        {
            double[,] a = new double[size, size];
            double[] y = new double[size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++) a[i, j] = A[i, j];
                y[i] = b[i];
            }

            // 前向消元
            for (int k = 0; k < size - 1; k++)
            {
                if (Math.Abs(a[k, k]) < 1e-12) return null;
                for (int i = k + 1; i < size; i++)
                {
                    double factor = a[i, k] / a[k, k];
                    for (int j = k; j < size; j++) a[i, j] -= factor * a[k, j];
                    y[i] -= factor * y[k];
                }
            }

            // 回代
            double[] x = new double[size];
            for (int i = size - 1; i >= 0; i--)
            {
                double sum = y[i];
                for (int j = i + 1; j < size; j++) sum -= a[i, j] * x[j];
                if (Math.Abs(a[i, i]) < 1e-12) return null;
                x[i] = sum / a[i, i];
            }
            return x;
        }

        private void ButtonResetCalib_Click(object sender, EventArgs e)
        {
            persistentHintLabel?.Dispose();
            persistentHintLabel = null;
            calibPoints.Clear();
            UpdateCalibButtonStates();
            ShowTemporaryMessage("校准点已清空", Color.White);
        }

        private void ButtonPreviewMap_Click(object sender, EventArgs e)
        {
            if (previewForm != null && !previewForm.IsDisposed)
            {
                previewForm.BringToFront();
                return;
            }
            previewForm = new MapPreviewForm(selectedMap);
            previewForm.FormClosed += (s, ev) =>
            {
                previewForm = null;
                UpdateCalibButtonStates();
            };
            previewForm.Show();
            UpdateCalibButtonStates();
        }

        private void ShowTemporaryMessage(string text, Color color, int durationMs = 2000)
        {
            Label lbl = new Label
            {
                Text = text,
                ForeColor = color,
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Parent = this,
                Location = new Point(15, 180)
            };
            lbl.BringToFront();

            if (durationMs > 0)
            {
                Timer timer = new Timer { Interval = durationMs };
                timer.Tick += (s, ev) => { lbl.Dispose(); timer.Stop(); };
                timer.Start();
            }
            else
            {
                persistentHintLabel?.Dispose();
                persistentHintLabel = lbl;
            }
        }

        // ---------- 锁定/解锁 ----------
        private void ButtonLock_Click(object sender, EventArgs e)
        {
            if (overlayForm == null || overlayForm.IsDisposed) return;
            isUnlocked = !isUnlocked;
            overlayForm.SetLocked(!isUnlocked);
            buttonLock.Text = isUnlocked ? "锁定" : "解锁";
            if (!isUnlocked) SaveOverlayPosition();
        }
        private void SaveOverlayPosition()
        {
            if (overlayForm == null || overlayForm.IsDisposed) return;
            Settings.Default.MapWindowX = overlayForm.Location.X;
            Settings.Default.MapWindowY = overlayForm.Location.Y;
            Settings.Default.MapWindowWidth = overlayForm.Size.Width;
            Settings.Default.MapWindowHeight = overlayForm.Size.Height;
            Settings.Default.Save();
        }

        // ---------- 启动/关闭浮窗 ----------
        private void ButtonStart_Click(object sender, EventArgs e)
        {
            if (overlayForm != null && !overlayForm.IsDisposed)
            {
                if (isUnlocked) { isUnlocked = false; overlayForm.SetLocked(true); SaveOverlayPosition(); }
                overlayForm.Close();
                UnregisterHotKeysForOverlay();
                buttonLock.Enabled = false;
                UpdateCalibButtonStates();
                return;
            }

            int x = Settings.Default.MapWindowX;
            int y = Settings.Default.MapWindowY;
            int w = Settings.Default.MapWindowWidth;
            int h = Settings.Default.MapWindowHeight;
            if (w < 50) w = 400;
            if (h < 50) h = 400;
            if (x < 0 || x > Screen.PrimaryScreen.Bounds.Width - 100) x = 100;
            if (y < 0 || y > Screen.PrimaryScreen.Bounds.Height - 100) y = 100;

            overlayForm = new MapOverlayForm(selectedMap);
            overlayForm.FormClosed += OverlayForm_FormClosed;
            overlayForm.Location = new Point(x, y);
            overlayForm.Size = new Size(w, h);
            overlayForm.Show();

            isUnlocked = false;
            overlayForm.SetLocked(true);
            buttonLock.Enabled = true;
            buttonLock.Text = "解锁";
            RegisterHotKeysForOverlay();

            if (!string.IsNullOrEmpty(screenshotDir))
                ProcessLatestScreenshot();

            UpdateStartButtonText();
            UpdateCalibButtonStates();
        }

        private void OverlayForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            persistentHintLabel?.Dispose();
            persistentHintLabel = null;
            if (isUnlocked) { isUnlocked = false; SaveOverlayPosition(); }
            overlayForm = null;
            buttonLock.Enabled = false;
            UnregisterHotKeysForOverlay();
            UpdateStartButtonText();
            calibPoints.Clear();
            waitingForCalibClick = false;
            UpdateCalibButtonStates();
        }

        private void UpdateStartButtonText()
        {
            bool open = overlayForm != null && !overlayForm.IsDisposed;
            buttonStart.Text = open ? "关闭地图浮窗" : "启动地图浮窗";
            buttonStart.BackColor = open ? Color.LightCoral : Color.LightGreen;
        }

        // ---------- 地图切换 ----------
        private void MapButton_Click(object sender, EventArgs e)
        {
            Button clicked = sender as Button;
            if (clicked == null) return;
            string mapName = clicked.Tag as string;
            selectedMap = mapName;
            HighlightMapButton(mapName);
            overlayForm?.SetMap(mapName);
            previewForm?.SetMap(mapName);
            persistentHintLabel?.Dispose();
            persistentHintLabel = null;
            calibPoints.Clear();
            UpdateCalibButtonStates();
        }
        private void HighlightMapButton(string mapName)
        {
            foreach (var btn in mapButtons)
                btn.BackColor = (string)btn.Tag == mapName ? Color.LightBlue : SystemColors.Control;
        }

        // ---------- 本地按键 ----------
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            string combo = "";
            if (e.Control) combo += "Ctrl+";
            if (e.Shift) combo += "Shift+";
            if (e.Alt) combo += "Alt+";
            combo += e.KeyCode.ToString();

            if (combo == Settings.Default.ScreenshotHotkey) { ProcessLatestScreenshot(); e.Handled = true; return; }
            if (combo == Settings.Default.ZoomInHotkey) { overlayForm?.ZoomIn(); e.Handled = true; return; }
            if (combo == Settings.Default.ZoomOutHotkey) { overlayForm?.ZoomOut(); e.Handled = true; return; }
        }

        // ---------- 快捷键设置等 ----------
        private void ButtonBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog() { Description = "选择截图保存的文件夹", ShowNewFolderButton = false })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string newPath = dialog.SelectedPath;
                    textBoxScreenshotDir.Text = newPath;
                    Settings.Default.ScreenshotPath = newPath;
                    Settings.Default.Save();
                    screenshotDir = newPath;
                    StartWatcher(newPath);
                }
            }
        }
        private void ButtonSetHotkey_Click(object sender, EventArgs e)
        {
            HotkeyCaptureForm capture = new HotkeyCaptureForm();
            if (!string.IsNullOrEmpty(Settings.Default.ScreenshotHotkey)) capture.FromHotkeyString(Settings.Default.ScreenshotHotkey);
            if (capture.ShowDialog(this) == DialogResult.OK)
            {
                string hotkey = capture.ToHotkeyString();
                if (!string.IsNullOrEmpty(hotkey))
                {
                    textBoxHotkey.Text = hotkey;
                    Settings.Default.ScreenshotHotkey = hotkey;
                    Settings.Default.Save();
                    if (overlayForm != null && !overlayForm.IsDisposed) { UnregisterHotKeysForOverlay(); RegisterHotKeysForOverlay(); }
                }
            }
        }
        private void ButtonSetZoomInHotkey_Click(object sender, EventArgs e)
        {
            HotkeyCaptureForm capture = new HotkeyCaptureForm();
            if (!string.IsNullOrEmpty(Settings.Default.ZoomInHotkey)) capture.FromHotkeyString(Settings.Default.ZoomInHotkey);
            if (capture.ShowDialog(this) == DialogResult.OK)
            {
                string hotkey = capture.ToHotkeyString();
                if (!string.IsNullOrEmpty(hotkey))
                {
                    textBoxZoomInHotkey.Text = hotkey;
                    Settings.Default.ZoomInHotkey = hotkey;
                    Settings.Default.Save();
                    if (overlayForm != null && !overlayForm.IsDisposed) { UnregisterHotKeysForOverlay(); RegisterHotKeysForOverlay(); }
                }
            }
        }
        private void ButtonSetZoomOutHotkey_Click(object sender, EventArgs e)
        {
            HotkeyCaptureForm capture = new HotkeyCaptureForm();
            if (!string.IsNullOrEmpty(Settings.Default.ZoomOutHotkey)) capture.FromHotkeyString(Settings.Default.ZoomOutHotkey);
            if (capture.ShowDialog(this) == DialogResult.OK)
            {
                string hotkey = capture.ToHotkeyString();
                if (!string.IsNullOrEmpty(hotkey))
                {
                    textBoxZoomOutHotkey.Text = hotkey;
                    Settings.Default.ZoomOutHotkey = hotkey;
                    Settings.Default.Save();
                    if (overlayForm != null && !overlayForm.IsDisposed) { UnregisterHotKeysForOverlay(); RegisterHotKeysForOverlay(); }
                }
            }
        }
    }
}