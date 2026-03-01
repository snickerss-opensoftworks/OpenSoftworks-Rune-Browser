using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace RuneS.Installer
{
    public partial class SetupWindow : Window
    {
        // ── Page state ────────────────────────────────────────────────────────
        private enum Page { Welcome = 1, License, Options, Installing, Done }
        private Page _current = Page.Welcome;

        // ── DWM dark mode ─────────────────────────────────────────────────────
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
                                                         ref int val, int size);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_BORDER_COLOR            = 34;

        // ─────────────────────────────────────────────────────────────────────
        public SetupWindow()
        {
            InitializeComponent();

            // Default install path
            TxtInstallPath.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                InstallerCore.AppName);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int v = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int));
            int border = 0x301E1E; // #1E2130 in BGR
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref border, sizeof(int));

            ShowPage(Page.Welcome);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Window chrome
        // ══════════════════════════════════════════════════════════════════════

        private void TitleBar_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnWindowClose_Click(object sender, RoutedEventArgs e)
        {
            if (_current == Page.Done || _current == Page.Installing) return;
            if (ConfirmCancel()) Close();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Navigation
        // ══════════════════════════════════════════════════════════════════════

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            switch (_current)
            {
                case Page.Welcome:
                    ShowPage(Page.License);
                    break;

                case Page.License:
                    if (ChkAcceptLicense.IsChecked != true)
                    {
                        ShowError("You must accept the license agreement to continue.");
                        return;
                    }
                    ShowPage(Page.Options);
                    break;

                case Page.Options:
                    if (!ValidateOptions()) return;
                    ShowPage(Page.Installing);
                    StartInstall();
                    break;

                case Page.Done:
                    // Launch RuneS and close setup
                    TryLaunch();
                    Close();
                    break;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            switch (_current)
            {
                case Page.License:  ShowPage(Page.Welcome);  break;
                case Page.Options:  ShowPage(Page.License);  break;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_current == Page.Done) { Close(); return; }
            if (ConfirmCancel()) Close();
        }

        private bool ConfirmCancel()
        {
            // Simple confirmation via a custom tiny dialog would be ideal,
            // but MessageBox is fine for an installer.
            var result = MessageBox.Show(
                "Are you sure you want to cancel the installation?",
                "Cancel Setup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Page rendering
        // ══════════════════════════════════════════════════════════════════════

        private void ShowPage(Page page)
        {
            _current = page;

            // Hide all pages
            PageWelcome.Visibility   = Visibility.Collapsed;
            PageLicense.Visibility   = Visibility.Collapsed;
            PageOptions.Visibility   = Visibility.Collapsed;
            PageInstalling.Visibility = Visibility.Collapsed;
            PageDone.Visibility      = Visibility.Collapsed;

            // Update step dots
            var accent = (Brush)FindResource("AccentBrush");
            var dim    = (Brush)FindResource("DimBrush");
            var green  = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x4E));

            void SetDot(System.Windows.Shapes.Ellipse dot, int dotPage)
            {
                if ((int)page > dotPage)       dot.Fill = green;
                else if ((int)page == dotPage) dot.Fill = accent;
                else                           dot.Fill = dim;
            }

            SetDot(Dot1, 1); SetDot(Dot2, 2); SetDot(Dot3, 3);
            SetDot(Dot4, 4); SetDot(Dot5, 5);

            switch (page)
            {
                case Page.Welcome:
                    PageWelcome.Visibility = Visibility.Visible;
                    HeaderSubtitle.Text    = "Welcome";
                    BtnNext.Content        = "Next";
                    BtnBack.IsEnabled      = false;
                    BtnCancel.Visibility   = Visibility.Visible;
                    break;

                case Page.License:
                    PageLicense.Visibility = Visibility.Visible;
                    HeaderSubtitle.Text    = "License Agreement";
                    BtnNext.Content        = "Next";
                    BtnBack.IsEnabled      = true;
                    break;

                case Page.Options:
                    PageOptions.Visibility = Visibility.Visible;
                    HeaderSubtitle.Text    = "Installation Options";
                    BtnNext.Content        = "Install";
                    BtnBack.IsEnabled      = true;
                    UpdateSpaceInfo();
                    break;

                case Page.Installing:
                    PageInstalling.Visibility = Visibility.Visible;
                    HeaderSubtitle.Text       = "Installing...";
                    BtnNext.IsEnabled         = false;
                    BtnBack.IsEnabled         = false;
                    BtnCancel.IsEnabled       = false;
                    InstallBar.Value          = 0;
                    break;

                case Page.Done:
                    PageDone.Visibility  = Visibility.Visible;
                    HeaderSubtitle.Text  = "Complete";
                    BtnNext.Content      = "Launch RuneS";
                    BtnNext.IsEnabled    = true;
                    BtnBack.IsEnabled    = false;
                    BtnCancel.Content    = "Close";
                    BtnCancel.IsEnabled  = true;
                    BtnCancel.Visibility = Visibility.Visible;
                    break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Options page helpers
        // ══════════════════════════════════════════════════════════════════════

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            // Use the old-school FolderBrowserDialog via WinForms interop
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description         = "Select installation folder";
                dlg.SelectedPath        = TxtInstallPath.Text;
                dlg.ShowNewFolderButton = true;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TxtInstallPath.Text = dlg.SelectedPath;
                    UpdateSpaceInfo();
                }
            }
        }

        private void UpdateSpaceInfo()
        {
            try
            {
                var payload = InstallerCore.GetPayloadFolder();
                long bytes = 0;
                if (Directory.Exists(payload))
                    foreach (var f in Directory.GetFiles(payload, "*", SearchOption.AllDirectories))
                        try { bytes += new FileInfo(f).Length; } catch { }

                var mb = bytes / (1024.0 * 1024.0);
                TxtSpaceInfo.Text = mb < 1
                    ? $"Required disk space: {bytes / 1024:N0} KB"
                    : $"Required disk space: {mb:N1} MB";
            }
            catch
            {
                TxtSpaceInfo.Text = "Required disk space: unknown";
            }
        }

        private bool ValidateOptions()
        {
            var path = TxtInstallPath.Text?.Trim();
            if (string.IsNullOrEmpty(path))
            {
                ShowError("Please enter an installation folder.");
                return false;
            }

            try
            {
                // Check the path is a valid absolute path
                var full = Path.GetFullPath(path);
                var root = Path.GetPathRoot(full);
                if (!Directory.Exists(root))
                {
                    ShowError("The selected drive does not exist.");
                    return false;
                }
            }
            catch
            {
                ShowError("The installation path contains invalid characters.");
                return false;
            }

            return true;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Install
        // ══════════════════════════════════════════════════════════════════════

        private async void StartInstall()
        {
            var installDir         = Path.GetFullPath(TxtInstallPath.Text.Trim());
            var desktopShortcut    = ChkDesktop.IsChecked == true;
            var startMenuShortcut  = ChkStartMenu.IsChecked == true;

            var progress = new Progress<(int pct, string msg)>(report =>
            {
                InstallBar.Value      = report.pct;
                TxtInstallStatus.Text = report.msg;
                TxtInstallPct.Text    = report.pct + "%";
            });

            try
            {
                await Task.Run(() =>
                    InstallerCore.Install(installDir, desktopShortcut,
                                          startMenuShortcut, progress));

                ShowPage(Page.Done);
            }
            catch (UnauthorizedAccessException)
            {
                ShowPage(Page.Options);
                ShowError("Access denied. Try running the installer as Administrator.");
                BtnNext.IsEnabled   = true;
                BtnBack.IsEnabled   = true;
                BtnCancel.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowPage(Page.Options);
                ShowError("Installation failed:\n" + ex.Message);
                BtnNext.IsEnabled   = true;
                BtnBack.IsEnabled   = true;
                BtnCancel.IsEnabled = true;
            }
        }

        private void TryLaunch()
        {
            try
            {
                var installDir = InstallerCore.GetInstalledPath()
                              ?? Path.Combine(
                                     Environment.GetFolderPath(
                                         Environment.SpecialFolder.ProgramFiles),
                                     InstallerCore.AppName);
                var exe = Path.Combine(installDir, InstallerCore.ExeName);
                if (File.Exists(exe))
                    System.Diagnostics.Process.Start(exe);
            }
            catch { /* Ignore launch errors */ }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════════

        private void ShowError(string message)
        {
            MessageBox.Show(message, "RuneS Setup",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
