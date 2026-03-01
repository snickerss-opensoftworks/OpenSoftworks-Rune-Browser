using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Shell;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using RuneS.Helpers;
using RuneS.Models;

namespace RuneS
{
    public partial class MainWindow : Window
    {
        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<BrowserTab> _tabs     = new List<BrowserTab>();
        private BrowserTab                _active;
        private bool                      _closing;

        // ── In-session bookmarks ──────────────────────────────────────────────
        private readonly List<(string Title, string Url)> _bookmarks =
            new List<(string, string)>
            {
                ("Brave",     "https://search.brave.com"),
                ("GitHub",    "https://github.com"),
                ("YouTube",   "https://www.youtube.com"),
                ("Reddit",    "https://www.reddit.com"),
            };

        // ── In-session history (most recent first) ────────────────────────────
        private readonly List<(string Title, string Url)> _history =
            new List<(string, string)>();

        private const string HomePage = "runes://newtab";

        // ── Keyboard commands ─────────────────────────────────────────────────
        private static readonly RoutedCommand CmdNewTab      = new RoutedCommand();
        private static readonly RoutedCommand CmdCloseTab    = new RoutedCommand();
        private static readonly RoutedCommand CmdFocusBar    = new RoutedCommand();
        private static readonly RoutedCommand CmdRefresh     = new RoutedCommand();
        private static readonly RoutedCommand CmdFindToggle  = new RoutedCommand();
        private static readonly RoutedCommand CmdZoomIn      = new RoutedCommand();
        private static readonly RoutedCommand CmdZoomOut     = new RoutedCommand();
        private static readonly RoutedCommand CmdZoomReset   = new RoutedCommand();
        private static readonly RoutedCommand CmdBookmark    = new RoutedCommand();
        private static readonly RoutedCommand CmdHardRefresh = new RoutedCommand();
        private static readonly RoutedCommand CmdDuplicate   = new RoutedCommand();

        // ── Tab zoom levels ───────────────────────────────────────────────────
        private readonly Dictionary<Guid, double> _zoomLevels = new Dictionary<Guid, double>();
        private readonly double[] _zoomSteps = { 0.25, 0.33, 0.5, 0.67, 0.75, 0.9,
                                                  1.0,  1.1,  1.25, 1.5, 1.75, 2.0, 3.0 };

        // ─────────────────────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            RegisterCommands();
            RenderBookmarks();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Window lifetime
        // ══════════════════════════════════════════════════════════════════════

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                WindowEffectsHelper.ApplyRoundedCorners(hwnd);
                WindowEffectsHelper.ApplyDarkMode(hwnd, dark: true);
                WindowEffectsHelper.ApplyBorderColor(hwnd,
                    System.Drawing.Color.FromArgb(0x1E, 0x21, 0x30));

                await CreateTabAsync(HomePage);
            }
            catch (Exception ex)
            {
                StatusText.Text = "Startup error: " + ex.Message;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _closing = true;
            foreach (var t in _tabs.ToList())
                try { t.WebView?.Dispose(); } catch { }
            base.OnClosed(e);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Tab creation
        // ══════════════════════════════════════════════════════════════════════

        private async System.Threading.Tasks.Task CreateTabAsync(
            string url = null, bool activate = true, bool background = false)
        {
            url = url ?? HomePage;
            var tab = new BrowserTab { Url = url };
            _tabs.Add(tab);
            _zoomLevels[tab.Id] = 1.0;

            var wv = new WebView2
            {
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "RuneS", "WebData")
                },
                DefaultBackgroundColor = System.Drawing.Color.White,
                Visibility             = Visibility.Collapsed
            };

            tab.WebView = wv;
            WebViewContainer.Children.Add(wv);

            // Build tab chip first so UI is immediately responsive
            var chip = BuildTabChip(tab);
            tab.WebView.Tag = chip;
            TabsPanel.Children.Add(chip);

            if (activate && !background) ActivateTab(tab);

            // Wire WPF-level events (already on UI thread)
            wv.NavigationStarting  += (s, ev) => OnNavStarting(tab, ev);
            wv.NavigationCompleted += (s, ev) => OnNavCompleted(tab, ev);

            try
            {
                await wv.EnsureCoreWebView2Async();
                if (_closing || !_tabs.Contains(tab)) return;
                ConfigureCore(tab);
                NavigateTo(tab, url);
            }
            catch (Exception ex)
            {
                if (_closing) return;
                tab.Title = "Error";
                StatusText.Text = "WebView2 error: " + ex.Message;
            }
        }

        private void ConfigureCore(BrowserTab tab)
        {
            var core = tab.WebView?.CoreWebView2;
            if (core == null) return;

            core.Settings.IsScriptEnabled                  = true;
            core.Settings.IsWebMessageEnabled              = true;
            core.Settings.IsStatusBarEnabled               = false;
            core.Settings.IsBuiltInErrorPageEnabled        = true;
            core.Settings.IsSwipeNavigationEnabled         = false;
            core.Settings.IsZoomControlEnabled             = false; // we manage zoom
            core.Settings.AreBrowserAcceleratorKeysEnabled = true;
            core.Settings.IsPinchZoomEnabled               = true;

            // CoreWebView2 events fire on background thread — MUST use BeginInvoke
            core.SourceChanged        += (s, ev) => OnSourceChanged(tab);
            core.DocumentTitleChanged += (s, ev) => OnTitleChanged(tab);
            core.FaviconChanged       += (s, ev) => OnFaviconChanged(tab);
            core.ProcessFailed        += (s, ev) => OnProcessFailed(tab, ev);
            core.ContextMenuRequested += (s, ev) => OnContextMenuRequested(tab, ev);

            // Status bar text (hover over links)
            core.StatusBarTextChanged += (s, ev) =>
            {
                var text = core.StatusBarText ?? string.Empty;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_active == tab) StatusText.Text = text;
                }));
            };

            // Apply any stored zoom
            if (_zoomLevels.TryGetValue(tab.Id, out double zoom))
                tab.WebView.ZoomFactor = zoom;

            // Download dialog
            core.DownloadStarting += (s, ev) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    StatusText.Text = "Downloading: " +
                        System.IO.Path.GetFileName(ev.DownloadOperation.ResultFilePath);
                }));
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        // Tab chip (the clickable tab button)
        // ══════════════════════════════════════════════════════════════════════

        private Button BuildTabChip(BrowserTab tab)
        {
            var globe = MakePath("IcoGlobe", 11, "#FF44485A");
            globe.Margin = new Thickness(0, 0, 5, 0);

            var titleTb = new TextBlock
            {
                Text              = tab.DisplayTitle,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming      = TextTrimming.CharacterEllipsis,
                MaxWidth          = 130,
                FontFamily        = new FontFamily("Segoe UI"),
                FontSize          = 11.5,
                Foreground        = (SolidColorBrush)FindResource("TextSubBrush")
            };

            // Loading indicator — a tiny spinning accent dot
            var loadDot = new Ellipse
            {
                Width             = 6,
                Height            = 6,
                Fill              = (SolidColorBrush)FindResource("AccentBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 5, 0),
                Visibility        = Visibility.Collapsed
            };

            // Keep in sync with model
            tab.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(BrowserTab.DisplayTitle):
                        titleTb.Text = tab.DisplayTitle;
                        break;
                    case nameof(BrowserTab.IsLoading):
                        loadDot.Visibility = tab.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                        globe.Visibility   = tab.IsLoading ? Visibility.Collapsed : Visibility.Visible;
                        break;
                }
            };

            // Close button
            var closeX = MakePath("IcoTabClose", 7, "#FF44485A");
            var closeBtn = new Button
            {
                Content           = closeX,
                Background        = Brushes.Transparent,
                BorderBrush       = Brushes.Transparent,
                BorderThickness   = new Thickness(0),
                Width             = 18, Height = 18,
                Cursor            = Cursors.Hand,
                FocusVisualStyle  = null,
                Padding           = new Thickness(2),
                Margin            = new Thickness(4, 0, -3, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            closeBtn.SetValue(WindowChrome.IsHitTestVisibleInChromeProperty, true);
            closeBtn.Click += (s, e) => { e.Handled = true; CloseTab(tab); };
            closeBtn.MouseEnter += (s, e) => closeX.Stroke = (Brush)FindResource("TextBrush");
            closeBtn.MouseLeave += (s, e) => closeX.Stroke = (Brush)FindResource("TextDimBrush");

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(loadDot);
            row.Children.Add(globe);
            row.Children.Add(titleTb);
            row.Children.Add(closeBtn);

            var btn = new Button
            {
                Style = (Style)FindResource("TabButtonStyle"),
                Content = row,
                Tag = tab
            };
            btn.SetValue(WindowChrome.IsHitTestVisibleInChromeProperty, true);
            btn.Click += (s, e) => ActivateTab(tab);

            // Middle-click to close
            btn.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Middle)
                { e.Handled = true; CloseTab(tab); }
            };

            tab.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BrowserTab.IsSelected))
                    ApplyChipStyle(btn, tab.IsSelected, titleTb, closeX);
            };

            return btn;
        }

        private static void ApplyChipStyle(Button btn, bool active, TextBlock tb, Path closeX)
        {
            if (active)
            {
                btn.Background      = new SolidColorBrush(Color.FromRgb(0x1A, 0x1C, 0x22));
                btn.BorderBrush     = new SolidColorBrush(Color.FromRgb(0x4D, 0x9E, 0xFF));
                btn.BorderThickness = new Thickness(0, 0, 0, 2);
                tb.Foreground       = new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF8));
            }
            else
            {
                btn.Background      = new SolidColorBrush(Color.FromRgb(0x10, 0x12, 0x17));
                btn.BorderBrush     = Brushes.Transparent;
                btn.BorderThickness = new Thickness(0);
                tb.Foreground       = new SolidColorBrush(Color.FromRgb(0x88, 0x90, 0xA4));
            }
        }

        private void ActivateTab(BrowserTab tab)
        {
            if (_active != null)
            {
                _active.IsSelected          = false;
                _active.WebView.Visibility  = Visibility.Collapsed;
            }
            _active                    = tab;
            tab.IsSelected             = true;
            tab.WebView.Visibility     = Visibility.Visible;

            BtnBack.IsEnabled    = tab.CanGoBack;
            BtnForward.IsEnabled = tab.CanGoForward;

            var disp = UrlHelper.IsNewTab(tab.Url) ? string.Empty : tab.Url;
            AddressBox.Text = disp;
            UpdateSecurityIcon(tab.IsSecure);
            UpdateZoomLabel(tab.Id);
            Title = string.IsNullOrEmpty(tab.Title) ? "RuneS" : tab.Title + " – RuneS";

            // Sync find bar
            if (FindBar.Visibility == Visibility.Visible)
                tab.WebView.CoreWebView2?.ExecuteScriptAsync("window.getSelection()?.removeAllRanges()");
        }

        private void CloseTab(BrowserTab tab)
        {
            if (_tabs.Count <= 1) { Close(); return; }

            int idx = _tabs.IndexOf(tab);
            _tabs.Remove(tab);
            _zoomLevels.Remove(tab.Id);

            WebViewContainer.Children.Remove(tab.WebView);
            if (tab.WebView.Tag is Button chip) TabsPanel.Children.Remove(chip);

            try { tab.WebView.Dispose(); } catch { }

            ActivateTab(_tabs[Math.Min(idx, _tabs.Count - 1)]);
        }

        // ══════════════════════════════════════════════════════════════════════
        // WebView2 event handlers
        // ──────────────────────────────────────────────────────────────────────
        // WPF NavigationStarting/Completed → already on UI thread, no dispatch.
        // CoreWebView2 COM events (SourceChanged etc.) → MUST BeginInvoke.
        // ══════════════════════════════════════════════════════════════════════

        private void OnNavStarting(BrowserTab tab, CoreWebView2NavigationStartingEventArgs e)
        {
            if (_closing || !_tabs.Contains(tab)) return;

            tab.IsLoading = true;
            tab.Url       = e.Uri ?? string.Empty;

            if (tab == _active)
            {
                LoadProgressBar.Visibility = Visibility.Visible;
                if (!UrlHelper.IsNewTab(e.Uri))
                    AddressBox.Text = e.Uri;
                RefreshIcon.Data = (Geometry)FindResource("IcoStop");
            }
        }

        private void OnNavCompleted(BrowserTab tab, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_closing || !_tabs.Contains(tab)) return;

            var core = tab.WebView.CoreWebView2;
            var url  = core?.Source ?? tab.Url;

            tab.IsLoading    = false;
            tab.CanGoBack    = core?.CanGoBack    ?? false;
            tab.CanGoForward = core?.CanGoForward ?? false;
            tab.Url          = url;
            tab.IsSecure     = UrlHelper.IsSecure(url);

            if (tab == _active)
            {
                LoadProgressBar.Visibility = Visibility.Collapsed;
                BtnBack.IsEnabled          = tab.CanGoBack;
                BtnForward.IsEnabled       = tab.CanGoForward;
                RefreshIcon.Data           = (Geometry)FindResource("IcoRefresh");
                AddressBox.Text            = UrlHelper.IsNewTab(url) ? string.Empty : url;
                UpdateSecurityIcon(tab.IsSecure);
            }

            // Add to history
            if (!UrlHelper.IsNewTab(url) && e.IsSuccess)
            {
                var title = tab.Title ?? url;
                _history.RemoveAll(h => h.Url == url);
                _history.Insert(0, (title, url));
                if (_history.Count > 200) _history.RemoveAt(_history.Count - 1);
            }

            // Re-apply zoom on navigation complete
            if (_zoomLevels.TryGetValue(tab.Id, out double z))
                tab.WebView.ZoomFactor = z;
        }

        // CoreWebView2 events — MUST BeginInvoke

        private void OnSourceChanged(BrowserTab tab)
        {
            var url = tab.WebView.CoreWebView2?.Source ?? string.Empty;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_closing || !_tabs.Contains(tab)) return;
                tab.Url      = url;
                tab.IsSecure = UrlHelper.IsSecure(url);
                if (tab == _active)
                {
                    AddressBox.Text = UrlHelper.IsNewTab(url) ? string.Empty : url;
                    UpdateSecurityIcon(tab.IsSecure);
                }
            }));
        }

        private void OnTitleChanged(BrowserTab tab)
        {
            var title = tab.WebView.CoreWebView2?.DocumentTitle ?? string.Empty;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_closing || !_tabs.Contains(tab)) return;
                if (!string.IsNullOrEmpty(title)) tab.Title = title;
                if (tab == _active)
                    Title = (string.IsNullOrEmpty(tab.Title) ? "New Tab" : tab.Title) + " – RuneS";
            }));
        }

        private void OnFaviconChanged(BrowserTab tab) { /* future: load favicon */ }

        private void OnProcessFailed(BrowserTab tab, CoreWebView2ProcessFailedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_closing) return;
                StatusText.Text = "Renderer crashed – will reload shortly";
                var timer = new System.Windows.Threading.DispatcherTimer
                            { Interval = TimeSpan.FromSeconds(1.5) };
                timer.Tick += (ts, te) =>
                {
                    timer.Stop();
                    if (!_closing && _tabs.Contains(tab))
                        NavigateTo(tab, tab.Url);
                };
                timer.Start();
            }));
        }

        private void OnContextMenuRequested(BrowserTab tab,
            CoreWebView2ContextMenuRequestedEventArgs e)
        {
            // Let WebView2 use its built-in context menu – nothing to override.
        }

        // ══════════════════════════════════════════════════════════════════════
        // Navigation
        // ══════════════════════════════════════════════════════════════════════

        private void NavigateTo(BrowserTab tab, string raw)
        {
            var core = tab.WebView?.CoreWebView2;
            if (core == null) return;

            if (UrlHelper.IsNewTab(raw))
            {
                core.NavigateToString(NewTabPageBuilder.Build());
                tab.Title = "New Tab";
                tab.Url   = HomePage;
                if (tab == _active) { AddressBox.Text = string.Empty; Title = "New Tab – RuneS"; }
                return;
            }

            core.Navigate(UrlHelper.ProcessInput(raw));
        }

        private void UpdateSecurityIcon(bool secure)
        {
            LockIcon.Visibility     = secure ? Visibility.Visible   : Visibility.Collapsed;
            InsecureIcon.Visibility = secure ? Visibility.Collapsed : Visibility.Visible;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Zoom
        // ══════════════════════════════════════════════════════════════════════

        private void AdjustZoom(int direction)
        {
            if (_active == null) return;

            var current = _zoomLevels.TryGetValue(_active.Id, out double z) ? z : 1.0;
            int idx = Array.FindIndex(_zoomSteps, s => Math.Abs(s - current) < 0.001);
            if (idx < 0) idx = Array.FindIndex(_zoomSteps, s => s >= current);
            if (idx < 0) idx = 6; // default to 1.0 index

            idx = Math.Max(0, Math.Min(_zoomSteps.Length - 1, idx + direction));
            var newZoom = _zoomSteps[idx];

            _zoomLevels[_active.Id] = newZoom;
            if (_active.WebView != null)
                _active.WebView.ZoomFactor = newZoom;
            UpdateZoomLabel(_active.Id);
        }

        private void ResetZoom()
        {
            if (_active == null) return;
            _zoomLevels[_active.Id] = 1.0;
            if (_active.WebView != null)
                _active.WebView.ZoomFactor = 1.0;
            UpdateZoomLabel(_active.Id);
        }

        private void UpdateZoomLabel(Guid tabId)
        {
            var zoom = _zoomLevels.TryGetValue(tabId, out double z) ? z : 1.0;
            ZoomLabel.Text = ((int)Math.Round(zoom * 100)) + "%";
            ZoomLabel.Foreground = Math.Abs(zoom - 1.0) > 0.001
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("TextSubBrush");
        }

        // ══════════════════════════════════════════════════════════════════════
        // Find in page
        // ══════════════════════════════════════════════════════════════════════

        private void ToggleFindBar()
        {
            if (FindBar.Visibility == Visibility.Collapsed)
            {
                FindBar.Visibility = Visibility.Visible;
                FindBox.Text       = string.Empty;
                FindBox.Focus();
                FindResultText.Text = string.Empty;
            }
            else
            {
                CloseFindBar();
            }
        }

        private void CloseFindBar()
        {
            FindBar.Visibility = Visibility.Collapsed;
            _active?.WebView?.CoreWebView2?.ExecuteScriptAsync(
                "window.find('', false, false, false, false, true, true)");
            FindResultText.Text = string.Empty;
        }

        private async void ExecuteFind(bool forward = true)
        {
            var query = FindBox.Text;
            if (string.IsNullOrEmpty(query) || _active?.WebView?.CoreWebView2 == null) return;

            // Use WebView2's built-in FindOnPage
            try
            {
                // Wrap in JS find — simple but effective
                var escaped = query.Replace("'", "\\'");
                var js = $"window.find('{escaped}', false, {(!forward ? "true" : "false")}, true, false, true, false)";
                await _active.WebView.CoreWebView2.ExecuteScriptAsync(js);
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Button handlers
        // ══════════════════════════════════════════════════════════════════════

        private void BtnBack_Click(object sender, RoutedEventArgs e)
            => _active?.WebView?.CoreWebView2?.GoBack();

        private void BtnForward_Click(object sender, RoutedEventArgs e)
            => _active?.WebView?.CoreWebView2?.GoForward();

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_active == null) return;
            if (_active.IsLoading) _active.WebView.CoreWebView2?.Stop();
            else                   _active.WebView.CoreWebView2?.Reload();
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            if (_active != null) NavigateTo(_active, HomePage);
        }

        private async void BtnNewTab_Click(object sender, RoutedEventArgs e)
        {
            try { await CreateTabAsync(HomePage); }
            catch (Exception ex) { StatusText.Text = "Tab error: " + ex.Message; }
        }

        private void BtnBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (_active == null || UrlHelper.IsNewTab(_active.Url)) return;
            var url   = _active.Url;
            var title = _active.Title ?? url;

            if (!_bookmarks.Any(b => b.Url == url))
            {
                _bookmarks.Add((title, url));
                BookmarkIcon.Fill   = (Brush)FindResource("AccentBrush");
                BookmarkIcon.Stroke = (Brush)FindResource("AccentBrush");
                BookmarkIcon.Data   = (Geometry)FindResource("IcoStarFilled");
                StatusText.Text     = "Bookmarked";
            }
            else
            {
                _bookmarks.RemoveAll(b => b.Url == url);
                BookmarkIcon.Fill   = Brushes.Transparent;
                BookmarkIcon.Stroke = (Brush)FindResource("TextSubBrush");
                BookmarkIcon.Data   = (Geometry)FindResource("IcoStarOutline");
                StatusText.Text     = "Bookmark removed";
            }
            RenderBookmarks();
        }

        private void BtnFind_Click(object sender, RoutedEventArgs e) => ToggleFindBar();

        private void BtnDownloads_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dl = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                System.Diagnostics.Process.Start("explorer.exe", dl);
            }
            catch { }
        }

        private async void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try { await CreateTabAsync("https://search.brave.com/settings"); }
            catch { }
        }

        private void BtnFindClose_Click(object sender, RoutedEventArgs e) => CloseFindBar();
        private void BtnFindNext_Click(object sender, RoutedEventArgs e)  => ExecuteFind(true);
        private void BtnFindPrev_Click(object sender, RoutedEventArgs e)  => ExecuteFind(false);

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)  => AdjustZoom(+1);
        private void BtnZoomOut_Click(object sender, RoutedEventArgs e) => AdjustZoom(-1);

        // Double-click on zoom label resets to 100%
        private void ZoomLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) ResetZoom();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Find bar input
        // ══════════════════════════════════════════════════════════════════════

        private void FindBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FindBar.Visibility == Visibility.Visible && FindBox.Text.Length > 0)
                ExecuteFind(true);
        }

        private void FindBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0)
            { ExecuteFind(false); e.Handled = true; }
            else if (e.Key == Key.Return)
            { ExecuteFind(true); e.Handled = true; }
            else if (e.Key == Key.Escape)
            { CloseFindBar(); e.Handled = true; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Address bar
        // ══════════════════════════════════════════════════════════════════════

        private void AddressBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AddrBarBorder.BorderBrush = (Brush)FindResource("AccentBrush");
            if (_active != null && !UrlHelper.IsNewTab(_active.Url))
                AddressBox.Text = _active.Url;
            AddressBox.SelectAll();
        }

        private void AddressBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AddrBarBorder.BorderBrush = (Brush)FindResource("BorderAddrBrush");
        }

        private void AddressBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _active != null)
            {
                NavigateTo(_active, AddressBox.Text);
                Keyboard.ClearFocus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                AddressBox.Text = _active?.Url ?? string.Empty;
                Keyboard.ClearFocus();
            }
        }

        private void AddressBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!AddressBox.IsKeyboardFocused)
            {
                AddressBox.Focus();
                AddressBox.SelectAll();
                e.Handled = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Bookmarks bar
        // ══════════════════════════════════════════════════════════════════════

        private void RenderBookmarks()
        {
            BookmarkItemsPanel.Children.Clear();
            foreach (var bm in _bookmarks)
            {
                var url   = bm.Url;
                var title = bm.Title;
                var btn   = new Button
                {
                    Style   = (Style)FindResource("BookmarkBarButtonStyle"),
                    Content = title,
                    ToolTip = url
                };
                btn.Click += (s, e) => { if (_active != null) NavigateTo(_active, url); };
                BookmarkItemsPanel.Children.Add(btn);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Keyboard commands
        // ══════════════════════════════════════════════════════════════════════

        private void RegisterCommands()
        {
            CommandBindings.Add(new CommandBinding(CmdNewTab,
                async (s, e) =>
                {
                    try { await CreateTabAsync(HomePage); }
                    catch { }
                }));

            CommandBindings.Add(new CommandBinding(CmdCloseTab,
                (s, e) => { if (_active != null) CloseTab(_active); }));

            CommandBindings.Add(new CommandBinding(CmdFocusBar,
                (s, e) => { AddressBox.Focus(); AddressBox.SelectAll(); }));

            CommandBindings.Add(new CommandBinding(CmdRefresh,
                (s, e) => _active?.WebView?.CoreWebView2?.Reload()));

            CommandBindings.Add(new CommandBinding(CmdHardRefresh,
                (s, e) =>
                {
                    // Ctrl+Shift+R — bypass cache
                    _active?.WebView?.CoreWebView2?.CallDevToolsProtocolMethodAsync(
                        "Page.reload", "{\"ignoreCache\":true}");
                }));

            CommandBindings.Add(new CommandBinding(CmdFindToggle,
                (s, e) => ToggleFindBar()));

            CommandBindings.Add(new CommandBinding(CmdZoomIn,
                (s, e) => AdjustZoom(+1)));

            CommandBindings.Add(new CommandBinding(CmdZoomOut,
                (s, e) => AdjustZoom(-1)));

            CommandBindings.Add(new CommandBinding(CmdZoomReset,
                (s, e) => ResetZoom()));

            CommandBindings.Add(new CommandBinding(CmdBookmark,
                (s, e) => BtnBookmark_Click(null, null)));

            CommandBindings.Add(new CommandBinding(CmdDuplicate,
                async (s, e) =>
                {
                    if (_active != null)
                        try { await CreateTabAsync(_active.Url); }
                        catch { }
                }));

            // Bindings
            InputBindings.Add(new KeyBinding(CmdNewTab,      Key.T, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdCloseTab,    Key.W, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdFocusBar,    Key.L, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdRefresh,     Key.F5, ModifierKeys.None));
            InputBindings.Add(new KeyBinding(CmdHardRefresh, Key.F5, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdFindToggle,  Key.F, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdZoomIn,      Key.OemPlus, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdZoomIn,      Key.Add, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdZoomOut,     Key.OemMinus, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdZoomOut,     Key.Subtract, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdZoomReset,   Key.D0, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdBookmark,    Key.D, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CmdDuplicate,   Key.K, ModifierKeys.Control | ModifierKeys.Shift));

            // Alt+Left/Right, Ctrl+Tab — handled in PreviewKeyDown
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Left && e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
                { BtnBack_Click(null, null); e.Handled = true; }
                else if (e.Key == Key.Right && e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
                { BtnForward_Click(null, null); e.Handled = true; }
                else if (e.Key == Key.Tab && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    if (_tabs.Count > 1)
                        ActivateTab(_tabs[(_tabs.IndexOf(_active) + 1) % _tabs.Count]);
                    e.Handled = true;
                }
                else if (e.Key == Key.Tab &&
                         e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    if (_tabs.Count > 1)
                    {
                        int i = (_tabs.IndexOf(_active) - 1 + _tabs.Count) % _tabs.Count;
                        ActivateTab(_tabs[i]);
                    }
                    e.Handled = true;
                }
                // Ctrl+1..8 select tab by index, Ctrl+9 → last tab
                else if (e.Key >= Key.D1 && e.Key <= Key.D9 &&
                         e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    int idx = e.Key == Key.D9
                        ? _tabs.Count - 1
                        : (e.Key - Key.D1);
                    if (idx >= 0 && idx < _tabs.Count)
                        ActivateTab(_tabs[idx]);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape && FindBar.Visibility == Visibility.Visible)
                {
                    CloseFindBar();
                    e.Handled = true;
                }
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        // Window chrome
        // ══════════════════════════════════════════════════════════════════════

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) BtnMaximize_Click(null, null);
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MaxWidth  = SystemParameters.WorkArea.Width;
                MaxHeight = SystemParameters.WorkArea.Height;
                OuterBorder.BorderThickness = new Thickness(0);
                MaximizeIcon.Data = (Geometry)FindResource("IcoRestore");
            }
            else
            {
                MaxWidth  = double.PositiveInfinity;
                MaxHeight = double.PositiveInfinity;
                OuterBorder.BorderThickness = new Thickness(1);
                MaximizeIcon.Data = (Geometry)FindResource("IcoMaximize");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Icon helper
        // ══════════════════════════════════════════════════════════════════════

        private Path MakePath(string geoKey, double size, string hexStroke)
        {
            return new Path
            {
                Data               = (Geometry)FindResource(geoKey),
                Width              = size, Height = size,
                Stretch            = Stretch.Uniform,
                Fill               = Brushes.Transparent,
                Stroke             = (Brush)new BrushConverter().ConvertFromString(hexStroke),
                StrokeThickness    = 1.8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap   = PenLineCap.Round,
                StrokeLineJoin     = PenLineJoin.Round,
                VerticalAlignment  = VerticalAlignment.Center
            };
        }
    }
}
