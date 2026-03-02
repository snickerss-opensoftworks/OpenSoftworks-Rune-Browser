using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using RuneS.Helpers;
using RuneS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace RuneS
{
    public partial class MainWindow : Window
    {
        // ── Collections ────────────────────────────────────────────────────────
        private readonly List<BrowserTab> _tabs = new List<BrowserTab>();
        private BrowserTab _active;
        private bool _closing;
        private readonly List<(string Title, string Url)> _bookmarks = new List<(string, string)>();

        // ── SINGLE WEBVIEW2 INSTANCE ───────────────────────────────────────────
        private WebView2 _sharedWebView;
        private bool _isWebViewReady = false;

        // ── Paths ──────────────────────────────────────────────────────────────
        private static readonly string BookmarksFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RuneS", "bookmarks.txt");

        // ── Zoom ───────────────────────────────────────────────────────────────
        private readonly double[] _zoomSteps =
            { 0.25, 0.33, 0.5, 0.67, 0.75, 0.9, 1.0, 1.1, 1.25, 1.5, 1.75, 2.0, 3.0 };
        private readonly Dictionary<Guid, double> _zoomMap = new Dictionary<Guid, double>();

        // ── Autocomplete popup ─────────────────────────────────────────────────
        private ListBox _acPopupList;
        private Popup _acPopup;
        private bool _suppressAc;

        // ── Context menu ───────────────────────────────────────────────────────
        private ContextMenu _tabCtx;
        private BrowserTab _ctxTab;

        // ── DWM ────────────────────────────────────────────────────────────────
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

        public MainWindow()
        {
            AppSettings.Load();
            ThemeManager.Load();
            InitializeComponent();
            BuildTabContextMenu();
            BuildAutocomplete();
            LoadBookmarks();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int v = 1; DwmSetWindowAttribute(hwnd, 20, ref v, sizeof(int));

            BookmarksBar.Visibility = AppSettings.ShowBookmarksBar
                ? Visibility.Visible : Visibility.Collapsed;
            RenderBookmarks();

            // Create SINGLE shared WebView2
            await InitializeSharedWebViewAsync();

            // Restore session or open home
            bool restored = false;
            if (AppSettings.RestoreLastSession && SessionManager.HasSession())
            {
                var sess = SessionManager.Load();
                if (sess.Count > 0)
                {
                    restored = true;
                    BrowserTab activeTab = null;
                    foreach (var s in sess)
                    {
                        await CreateTabAsync(s.Url, false, s.Active);
                        if (s.Active) activeTab = _tabs.Last();
                    }
                    if (activeTab != null)
                        await ActivateTabAsync(activeTab);
                    else if (_tabs.Count > 0)
                        await ActivateTabAsync(_tabs[0]);
                }
            }

            if (!restored)
            {
                var tab = await CreateTabAsync(AppSettings.StartupPage, true, true);
            }
        }

        private async Task InitializeSharedWebViewAsync()
        {
            _sharedWebView = new WebView2
            {
                DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 8, 10, 14),
                Visibility = Visibility.Collapsed
            };

            WebHost.Children.Add(_sharedWebView);

            try
            {
                await _sharedWebView.EnsureCoreWebView2Async();
                _isWebViewReady = true;
                HookCoreEvents();
            }
            catch (Exception ex)
            {
                if (!_closing) StatusText.Text = "WebView2: " + ex.Message;
            }
        }

        private void HookCoreEvents()
        {
            var c = _sharedWebView.CoreWebView2;
            if (c == null) return;

            c.Settings.IsStatusBarEnabled = false;
            c.Settings.IsZoomControlEnabled = false;
            c.Settings.AreBrowserAcceleratorKeysEnabled = true;
            c.Settings.IsPinchZoomEnabled = false;

            // Intercept rune:// URLs
            c.NavigationStarting += (s, ev) =>
            {
                if (ev.Uri != null && ev.Uri.StartsWith("rune://", StringComparison.OrdinalIgnoreCase))
                {
                    ev.Cancel = true;
                    var u = ev.Uri;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_active != null) NavigateTo(_active, u);
                    }));
                }
            };

            c.SourceChanged += (s, ev) =>
            {
                if (_active != null) Dispatcher.BeginInvoke(new Action(() => OnSourceChanged(_active)));
            };

            c.DocumentTitleChanged += (s, ev) =>
            {
                if (_active != null) Dispatcher.BeginInvoke(new Action(() => OnTitleChanged(_active)));
            };

            c.FaviconChanged += (s, ev) =>
            {
                if (_active != null) Dispatcher.BeginInvoke(new Action(() => OnFaviconChanged(_active)));
            };

            c.ProcessFailed += (s, ev) =>
            {
                if (_active != null) Dispatcher.BeginInvoke(new Action(() => OnProcessFailed(_active)));
            };

            c.StatusBarTextChanged += (s, ev) =>
            {
                var txt = c.StatusBarText ?? string.Empty;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_closing && _active != null) StatusText.Text = txt;
                }));
            };

            c.DownloadStarting += (s, ev) =>
            {
                var op = ev.DownloadOperation;
                ev.Handled = true;

                var item = DownloadManager.Add(
                    System.IO.Path.GetFileName(op.ResultFilePath),
                    op.ResultFilePath,
                    op.Uri);

                op.BytesReceivedChanged += (ds, de) =>
                {
                    item.ReceivedBytes = op.BytesReceived;
                    item.TotalBytes = (long)op.TotalBytesToReceive;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!_closing && _active != null)
                            StatusText.Text = "Downloading: " +
                                System.IO.Path.GetFileName(op.ResultFilePath) +
                                " — " + item.Progress + "%";
                    }));
                };

                op.StateChanged += (ds, de) =>
                {
                    switch (op.State)
                    {
                        case CoreWebView2DownloadState.Completed:
                            DownloadManager.Complete(item);
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (!_closing) StatusText.Text = "Downloaded: " + item.FileName;
                            }));
                            break;
                        case CoreWebView2DownloadState.Interrupted:
                            DownloadManager.Cancel(item);
                            break;
                    }
                };
            };

            c.WebMessageReceived += (s, ev) =>
            {
                var json = ev.TryGetWebMessageAsString();
                if (!string.IsNullOrEmpty(json) && _active != null)
                    Dispatcher.BeginInvoke(new Action(() => HandleWebMessage(_active, json)));
            };

            // Inject adblock if enabled
            if (AppSettings.AdblockEnabled)
            {
                InjectAdblock();
            }
        }

        private void InjectAdblock()
        {
            // Basic adblock - block common ad domains
            _sharedWebView.CoreWebView2.WebResourceRequested += (s, ev) =>
            {
                var url = ev.Request.Uri.ToLower();
                var blockedDomains = new[] { "googleadservices", "googlesyndication", "doubleclick",
                    "facebook.com/tr", "analytics", "tracker", "adsystem", "amazon-adsystem" };

                if (blockedDomains.Any(d => url.Contains(d)))
                {
                    ev.Response = _sharedWebView.CoreWebView2.Environment.CreateWebResourceResponse(
                        new System.IO.MemoryStream(), 403, "Blocked", "Content-Type: text/plain");
                }
            };
        }

        // ════════════════════════════════════════════════════════════════════════
        // TAB CREATION - Visual only, no WebView2 per tab
        // ════════════════════════════════════════════════════════════════════════
        private async Task<BrowserTab> CreateTabAsync(string url = null, bool activate = false, bool isActiveTab = false)
        {
            url = string.IsNullOrEmpty(url) ? "rune://home" : url;
            var tab = new BrowserTab { Url = url };
            _tabs.Add(tab);
            _zoomMap[tab.Id] = AppSettings.DefaultZoom;

            var chip = BuildChip(tab);
            TabsPanel.Children.Add(chip);
            tab.ChipElement = chip;

            if (activate || isActiveTab)
                await ActivateTabAsync(tab);

            return tab;
        }

        // ════════════════════════════════════════════════════════════════════════
        // TAB ACTIVATION - This is where the magic happens
        // ════════════════════════════════════════════════════════════════════════
        private async Task ActivateTabAsync(BrowserTab tab)
        {
            if (_active == tab) return;

            // Save current tab state before switching
            if (_active != null && _isWebViewReady)
            {
                _active.Url = _sharedWebView.Source?.ToString() ?? _active.Url;
                _active.Title = _sharedWebView.CoreWebView2?.DocumentTitle ?? _active.Title;
                _active.IsSelected = false;
                _active.IsActive = false;

                // Take screenshot for thumbnail (optional, can be disabled for performance)
                if (AppSettings.SaveTabThumbnails)
                {
                    // _active.Thumbnail = await CaptureThumbnailAsync();
                }
            }

            _active = tab;
            tab.IsSelected = true;
            tab.IsActive = true;

            // Update UI
            BtnBack.IsEnabled = false;
            BtnForward.IsEnabled = false;
            AddressBox.Text = UrlHelper.GetDisplayUrl(tab.Url);
            UpdateSecurityIcon(tab.Url);
            UpdateBookmarkStar(tab.Url);
            SyncZoomLabel(tab.Id);
            Title = (string.IsNullOrEmpty(tab.Title) ? "New Tab" : tab.Title) + " — RuneS";

            // Navigate shared WebView2 to this tab's URL
            if (_isWebViewReady)
            {
                _sharedWebView.Visibility = Visibility.Visible;
                NavigateTo(tab, tab.Url);

                if (_zoomMap.TryGetValue(tab.Id, out double z))
                    _sharedWebView.ZoomFactor = z;
            }

            // Update tab visual states
            foreach (var t in _tabs)
                UpdateTabVisualState(t);
        }

        private void UpdateTabVisualState(BrowserTab tab)
        {
            if (tab.ChipElement is Button chip)
            {
                ApplyChipStyle(chip, tab.IsSelected, chip.Content as StackPanel);
            }
        }

        private void NavigateTo(BrowserTab tab, string url)
        {
            if (!_isWebViewReady || _sharedWebView?.CoreWebView2 == null) return;

            if (UrlHelper.IsHomePage(url))
            {
                tab.Favicon = null; tab.Title = "New Tab"; tab.Url = "rune://home";
                _sharedWebView.CoreWebView2.NavigateToString(HomePageBuilder.Build());
                if (tab == _active) { AddressBox.Text = ""; Title = "New Tab — RuneS"; }
                return;
            }

            if (UrlHelper.IsSettingsPage(url))
            {
                tab.Favicon = null; tab.Title = "Settings"; tab.Url = "rune://settings";
                _sharedWebView.CoreWebView2.NavigateToString(SettingsPageBuilder.Build(AppSettings.ToJson()));
                if (tab == _active) { AddressBox.Text = "rune://settings"; Title = "Settings — RuneS"; }
                return;
            }

            if (UrlHelper.IsHistoryPage(url))
            {
                tab.Favicon = null; tab.Title = "History"; tab.Url = "rune://history";
                _sharedWebView.CoreWebView2.NavigateToString(HistoryPageBuilder.Build(HistoryManager.GetAll()));
                if (tab == _active) { AddressBox.Text = "rune://history"; Title = "History — RuneS"; }
                return;
            }

            if (UrlHelper.IsDownloadsPage(url))
            {
                tab.Favicon = null; tab.Title = "Downloads"; tab.Url = "rune://downloads";
                _sharedWebView.CoreWebView2.NavigateToString(DownloadsPageBuilder.Build(DownloadManager.All));
                if (tab == _active) { AddressBox.Text = "rune://downloads"; Title = "Downloads — RuneS"; }
                return;
            }

            if (UrlHelper.IsThemesPage(url))
            {
                tab.Favicon = null; tab.Title = "Themes"; tab.Url = "rune://themes";
                _sharedWebView.CoreWebView2.NavigateToString(ThemesPageBuilder.Build(
                    AppSettings.ThemeId, ThemeManager.GetCustomThemes()));
                if (tab == _active) { AddressBox.Text = "rune://themes"; Title = "Themes — RuneS"; }
                return;
            }

            if (UrlHelper.IsBookmarksPage(url))
            {
                tab.Favicon = null; tab.Title = "Bookmarks"; tab.Url = "rune://bookmarks";
                _sharedWebView.CoreWebView2.NavigateToString(BookmarksPageBuilder.Build(_bookmarks));
                if (tab == _active) { AddressBox.Text = "rune://bookmarks"; Title = "Bookmarks — RuneS"; }
                return;
            }

            _sharedWebView.CoreWebView2.Navigate(UrlHelper.ProcessInput(url));
        }

        // ════════════════════════════════════════════════════════════════════════
        // NAV EVENTS - Now use _sharedWebView instead of tab.WebView
        // ════════════════════════════════════════════════════════════════════════
        private void OnNavStarting(BrowserTab tab, CoreWebView2NavigationStartingEventArgs ev)
        {
            if (_closing || tab != _active) return;
            tab.IsLoading = true;
            LoadBar.Visibility = Visibility.Visible;
            if (!UrlHelper.IsInternalPage(ev.Uri)) AddressBox.Text = ev.Uri;
            RefreshIcon.Data = (Geometry)FindResource("IcoStop");
        }

        private void OnNavCompleted(BrowserTab tab, CoreWebView2NavigationCompletedEventArgs ev)
        {
            if (_closing || tab != _active) return;
            var core = _sharedWebView.CoreWebView2;
            var url = core?.Source ?? tab.Url;

            tab.IsLoading = false;
            tab.CanGoBack = core?.CanGoBack ?? false;
            tab.CanGoForward = core?.CanGoForward ?? false;
            tab.Url = url;

            LoadBar.Visibility = Visibility.Collapsed;
            BtnBack.IsEnabled = tab.CanGoBack;
            BtnForward.IsEnabled = tab.CanGoForward;
            RefreshIcon.Data = (Geometry)FindResource("IcoRefresh");
            AddressBox.Text = UrlHelper.GetDisplayUrl(url);
            UpdateSecurityIcon(url);
            UpdateBookmarkStar(url);

            if (_zoomMap.TryGetValue(tab.Id, out double z))
                _sharedWebView.ZoomFactor = z;
        }

        private void OnSourceChanged(BrowserTab tab)
        {
            var url = _sharedWebView?.CoreWebView2?.Source ?? string.Empty;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_closing || tab != _active) return;
                tab.Url = url;
                AddressBox.Text = UrlHelper.GetDisplayUrl(url);
                UpdateSecurityIcon(url);
                UpdateBookmarkStar(url);
                if (!UrlHelper.IsInternalPage(url))
                    HistoryManager.Add(url, tab.Title);
            }));
        }

        private void OnTitleChanged(BrowserTab tab)
        {
            var title = _sharedWebView?.CoreWebView2?.DocumentTitle ?? string.Empty;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_closing || tab != _active) return;
                if (!string.IsNullOrEmpty(title)) tab.Title = title;
                Title = tab.Title + " — RuneS";
                if (!UrlHelper.IsInternalPage(tab.Url))
                    HistoryManager.Add(tab.Url, tab.Title);
            }));
        }

        private void OnFaviconChanged(BrowserTab tab)
        {
            var faviconUri = _sharedWebView?.CoreWebView2?.FaviconUri;
            if (string.IsNullOrEmpty(faviconUri)) return;

            Task.Run(async () =>
            {
                try
                {
                    var stream = await _sharedWebView.CoreWebView2
                        .GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
                    if (stream == null) return;
                    var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    ms.Position = 0;
                    if (ms.Length == 0) return;
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = ms;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 32;
                    bmp.EndInit();
                    bmp.Freeze();

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!_closing && tab == _active)
                            tab.Favicon = bmp;
                    }));
                }
                catch { }
            });
        }

        private void OnProcessFailed(BrowserTab tab)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_closing) return;
                StatusText.Text = "Tab crashed — reloading...";
                var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                t.Tick += (s, e) => { t.Stop(); if (!_closing) NavigateTo(tab, tab.Url); };
                t.Start();
            }));
        }

        // ════════════════════════════════════════════════════════════════════════
        // WEBMESSAGE HANDLER
        // ════════════════════════════════════════════════════════════════════════
        private async void HandleWebMessage(BrowserTab tab, string json)
        {
            var type = ExtractStr(json, "type");
            var value = ExtractStr(json, "value");

            switch (type)
            {
                case "setting":
                    var eq = value.IndexOf('=');
                    if (eq > 0)
                    {
                        var k = value.Substring(0, eq);
                        var v = value.Substring(eq + 1);
                        AppSettings.ApplyFromJson(k, v);
                        if (k == "showBookmarksBar")
                            BookmarksBar.Visibility = AppSettings.ShowBookmarksBar
                                ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;

                case "applyTheme":
                    var theme = ThemeManager.BuiltIn.FirstOrDefault(t => t.Id == value)
                             ?? ThemeManager.GetCustomThemes().FirstOrDefault(t => t.Id == value);
                    if (theme != null)
                    {
                        ThemeManager.Apply(theme);
                        AppSettings.ThemeId = value;
                        AppSettings.Save();
                        foreach (var t in _tabs.Where(t => UrlHelper.IsInternalPage(t.Url)))
                            NavigateTo(t, t.Url);
                    }
                    break;

                case "importTheme":
                    var dlg = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = "Import Theme",
                        Filter = "Theme files (*.theme)|*.theme",
                        Multiselect = false
                    };
                    if (dlg.ShowDialog() == true)
                    {
                        if (tab.Url == "rune://themes")
                            NavigateTo(tab, "rune://themes");
                    }
                    break;

                case "openThemes":
                    await OpenOrCreateInternalTab("rune://themes");
                    break;

                case "navigate":
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (tab == _active) NavigateTo(tab, value);
                        else { await ActivateTabAsync(tab); NavigateTo(tab, value); }
                    }
                    break;

                case "historyDelete":
                    if (long.TryParse(value, out long ticks))
                    {
                        var all = HistoryManager.GetAll();
                        var match = all.FirstOrDefault(h => h.Time.Ticks == ticks);
                        if (match != null) HistoryManager.DeleteEntry(match.Url, match.Time);
                    }
                    break;

                case "historyClear":
                    HistoryManager.ClearAll();
                    if (tab.Url == "rune://history") NavigateTo(tab, "rune://history");
                    if (tab.Url == "rune://settings") NavigateTo(tab, "rune://settings");
                    break;

                case "bookmarkDelete":
                    _bookmarks.RemoveAll(b => b.Url == value);
                    SaveBookmarks();
                    RenderBookmarks();
                    if (tab.Url == "rune://bookmarks") NavigateTo(tab, "rune://bookmarks");
                    break;

                case "clearData":
                    try
                    {
                        _sharedWebView?.CoreWebView2?.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");
                        _sharedWebView?.CoreWebView2?.CallDevToolsProtocolMethodAsync("Network.clearBrowserCookies", "{}");
                    }
                    catch { }
                    StatusText.Text = "Cache and cookies cleared";
                    break;

                case "openDownloads":
                case "openDownloadsFolder":
                    try { System.Diagnostics.Process.Start("explorer.exe", AppSettings.DownloadsFolder); } catch { }
                    break;

                case "openFile":
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        { FileName = value, UseShellExecute = true });
                    }
                    catch { }
                    break;

                case "removeDl":
                    DownloadManager.Remove(value);
                    break;

                case "clearDownloads":
                    DownloadManager.ClearCompleted();
                    NavigateTo(tab, "rune://downloads");
                    break;

                case "getStats":
                    var histCount = HistoryManager.GetAll().Count;
                    var bmCount = _bookmarks.Count;
                    var dlCount = DownloadManager.All.Count;
                    var statsMsg = "{\"type\":\"stats\",\"history\":" + histCount +
                                    ",\"bookmarks\":" + bmCount +
                                    ",\"downloads\":" + dlCount + "}";
                    try { _sharedWebView?.CoreWebView2?.PostWebMessageAsString(statsMsg); } catch { }
                    break;
            }
        }

        private static string ExtractStr(string json, string key)
        {
            var search = "\"" + key + "\":\"";
            var start = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return string.Empty;
            start += search.Length;
            var end = json.IndexOf('"', start);
            return end < 0 ? string.Empty
                : json.Substring(start, end - start)
                      .Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\/", "/");
        }

        // ════════════════════════════════════════════════════════════════════════
        // TAB CHIP BUILDER - Simplified, no WebView2 per tab
        // ════════════════════════════════════════════════════════════════════════
        private Button BuildChip(BrowserTab tab)
        {
            var img = new Image
            {
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                Visibility = Visibility.Collapsed
            };
            var globe = MakePath("IcoGlobe", 11, "#FF44485A");
            globe.Margin = new Thickness(0, 0, 6, 0);
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = (Brush)FindResource("Accent"),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            var muteIco = new TextBlock
            {
                Text = "🔇",
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                Visibility = Visibility.Collapsed
            };
            var pinIco = new TextBlock
            {
                Text = "📌",
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 3, 0),
                Visibility = Visibility.Collapsed
            };
            var titleTb = new TextBlock
            {
                Text = tab.DisplayTitle,
                MaxWidth = 120,
                FontSize = 11.5,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = (Brush)FindResource("TextSub"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };

            tab.PropertyChanged += (s, ev) =>
            {
                switch (ev.PropertyName)
                {
                    case nameof(BrowserTab.DisplayTitle): titleTb.Text = tab.DisplayTitle; break;
                    case nameof(BrowserTab.IsLoading):
                        dot.Visibility = tab.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                        globe.Visibility = (!tab.IsLoading && !tab.HasFavicon) ? Visibility.Visible : Visibility.Collapsed;
                        img.Visibility = (!tab.IsLoading && tab.HasFavicon) ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case nameof(BrowserTab.Favicon):
                        img.Source = tab.Favicon;
                        img.Visibility = tab.HasFavicon ? Visibility.Visible : Visibility.Collapsed;
                        globe.Visibility = tab.HasFavicon ? Visibility.Collapsed : Visibility.Visible;
                        break;
                    case nameof(BrowserTab.IsMuted):
                        muteIco.Visibility = tab.IsMuted ? Visibility.Visible : Visibility.Collapsed; break;
                    case nameof(BrowserTab.IsPinned):
                        pinIco.Visibility = tab.IsPinned ? Visibility.Visible : Visibility.Collapsed;
                        titleTb.Visibility = tab.IsPinned ? Visibility.Collapsed : Visibility.Visible;
                        break;
                }
            };

            var xPath = MakePath("IcoTabClose", 7, "#FF44485A");
            var closeBtn = new Button
            {
                Content = xPath,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Width = 18,
                Height = 18,
                Padding = new Thickness(2),
                Margin = new Thickness(4, 0, -2, 0),
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeBtn.SetValue(WindowChrome.IsHitTestVisibleInChromeProperty, true);
            closeBtn.Click += (s, ev) => { ev.Handled = true; CloseTab(tab); };
            closeBtn.MouseEnter += (s, ev) => xPath.Stroke = (Brush)FindResource("Text");
            closeBtn.MouseLeave += (s, ev) => xPath.Stroke = new SolidColorBrush(Color.FromRgb(0x44, 0x48, 0x5A));

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(pinIco); row.Children.Add(dot); row.Children.Add(img);
            row.Children.Add(globe); row.Children.Add(muteIco); row.Children.Add(titleTb);
            row.Children.Add(closeBtn);

            var chip = new Button { Style = (Style)FindResource("TabBtn"), Content = row, Tag = tab };
            chip.SetValue(WindowChrome.IsHitTestVisibleInChromeProperty, true);

            chip.ToolTipOpening += (s, ev) =>
            {
                var mem = "Shared";
                chip.ToolTip = tab.Title + "\n" + tab.Url + "\nMemory: " + mem;
            };

            chip.Click += async (s, ev) => await ActivateTabAsync(tab);

            chip.MouseDown += (s, ev) =>
            {
                if (ev.ChangedButton == MouseButton.Middle)
                { ev.Handled = true; CloseTab(tab); }
                else if (ev.ChangedButton == MouseButton.Right)
                { ev.Handled = true; ShowTabContextMenu(tab); }
            };

            return chip;
        }

        private void ApplyChipStyle(Button chip, bool active, StackPanel content)
        {
            chip.Background = new SolidColorBrush(active
                ? Color.FromRgb(0x1A, 0x1C, 0x22) : Color.FromRgb(0x0B, 0x0D, 0x12));
            chip.BorderBrush = active
                ? new SolidColorBrush(Color.FromRgb(0x4D, 0x9E, 0xFF)) : Brushes.Transparent;
            chip.BorderThickness = active ? new Thickness(0, 0, 0, 2) : new Thickness(0);

            var titleTb = content?.Children.OfType<TextBlock>().FirstOrDefault();
            if (titleTb != null)
                titleTb.Foreground = new SolidColorBrush(active
                    ? Color.FromRgb(0xF0, 0xF2, 0xF8) : Color.FromRgb(0x88, 0x90, 0xA4));
        }

        private void CloseTab(BrowserTab tab)
        {
            if (_tabs.Count <= 1) { Close(); return; }

            int idx = _tabs.IndexOf(tab);
            _tabs.Remove(tab);
            _zoomMap.Remove(tab.Id);

            if (tab.ChipElement is Button chip)
                TabsPanel.Children.Remove(chip);

            if (_active == tab)
            {
                var newTab = _tabs[Math.Min(idx, _tabs.Count - 1)];
                _ = ActivateTabAsync(newTab);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CONTEXT MENU
        // ════════════════════════════════════════════════════════════════════════
        private void BuildTabContextMenu()
        {
            _tabCtx = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(0x13, 0x15, 0x1B)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x32, 0x45)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0, 4, 0, 4)
            };

            AddCtxItem("New Tab", () => _ = CreateTabAsync("rune://home"));
            AddCtxItem("Duplicate Tab", () => { if (_ctxTab != null) _ = CreateTabAsync(_ctxTab.Url); });
            _tabCtx.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x30)), Margin = new Thickness(0, 2, 0, 2) });
            AddCtxItem("Reload Tab", () => { if (_active == _ctxTab) _sharedWebView?.CoreWebView2?.Reload(); });
            AddCtxItem("Mute / Unmute Tab", () => { if (_ctxTab != null) ToggleMute(_ctxTab); });
            AddCtxItem("Pin / Unpin Tab", () => { if (_ctxTab != null) TogglePin(_ctxTab); });
            _tabCtx.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x30)), Margin = new Thickness(0, 2, 0, 2) });
            AddCtxItem("Copy URL", () => { if (_ctxTab != null) try { Clipboard.SetText(_ctxTab.Url); } catch { } });
            AddCtxItem("Open in New Tab", () => { if (_ctxTab != null) _ = CreateTabAsync(_ctxTab.Url); });
            _tabCtx.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x30)), Margin = new Thickness(0, 2, 0, 2) });
            AddCtxItem("Close Tab", () => { if (_ctxTab != null) CloseTab(_ctxTab); }, danger: true);
            AddCtxItem("Close Other Tabs", () =>
            {
                foreach (var t in _tabs.Where(t => t != _ctxTab).ToList()) CloseTab(t);
            }, danger: true);
        }

        private void AddCtxItem(string text, Action action, bool danger = false)
        {
            var item = new MenuItem
            {
                Header = text,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(danger ? Color.FromRgb(0xE8, 0x22, 0x3A) : Color.FromRgb(0x88, 0x90, 0xA4)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 6, 20, 6),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };
            item.Click += (s, e) => action();
            _tabCtx.Items.Add(item);
        }

        private void ShowTabContextMenu(BrowserTab tab)
        {
            _ctxTab = tab;
            _tabCtx.IsOpen = true;
        }

        private void ToggleMute(BrowserTab tab)
        {
            tab.IsMuted = !tab.IsMuted;
            if (_active == tab && _sharedWebView?.CoreWebView2 != null)
            {
                _sharedWebView.CoreWebView2.ExecuteScriptAsync(
                    tab.IsMuted
                        ? "document.querySelectorAll('video,audio').forEach(e=>e.muted=true)"
                        : "document.querySelectorAll('video,audio').forEach(e=>e.muted=false)");
            }
        }

        private void TogglePin(BrowserTab tab)
        {
            tab.IsPinned = !tab.IsPinned;
            if (tab.ChipElement is Button chip)
            {
                chip.MaxWidth = tab.IsPinned ? 40 : 220;
                chip.MinWidth = tab.IsPinned ? 36 : 100;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // AUTOCOMPLETE
        // ════════════════════════════════════════════════════════════════════════
        private void BuildAutocomplete()
        {
            _acPopupList = new ListBox
            {
                Background = new SolidColorBrush(Color.FromRgb(0x13, 0x15, 0x1B)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x32, 0x45)),
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                MaxHeight = 260,
                Focusable = false
            };
            _acPopupList.MouseLeftButtonUp += AcList_MouseLeftButtonUp;

            _acPopup = new Popup
            {
                Child = _acPopupList,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.None,
                Placement = PlacementMode.Bottom,
                PlacementTarget = AddressBox
            };
        }

        private void ShowAutocomplete(string query)
        {
            if (_suppressAc || string.IsNullOrEmpty(query)) { HideAutocomplete(); return; }

            var results = HistoryManager.Autocomplete(query, 8);
            var bmMatches = _bookmarks
                .Where(b => b.Url.ToLower().Contains(query.ToLower()) ||
                            b.Title.ToLower().Contains(query.ToLower()))
                .Take(3)
                .Select(b => new HistoryEntry { Url = b.Url, Title = b.Title, Time = DateTime.Now });
            var combined = bmMatches.Concat(results).GroupBy(e => e.Url).Select(g => g.First()).Take(8).ToList();

            if (combined.Count == 0) { HideAutocomplete(); return; }

            _acPopupList.Items.Clear();
            foreach (var e in combined)
            {
                var panel = new StackPanel { Margin = new Thickness(10, 5, 10, 5) };
                panel.Children.Add(new TextBlock
                {
                    Text = e.Title,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF8)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                panel.Children.Add(new TextBlock
                {
                    Text = e.Url,
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x48, 0x5A)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                var item = new ListBoxItem
                {
                    Content = panel,
                    Tag = e.Url,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF8))
                };
                _acPopupList.Items.Add(item);
            }

            _acPopup.Width = AddrBorder.ActualWidth;
            _acPopup.IsOpen = true;
        }

        private void HideAutocomplete() => _acPopup.IsOpen = false;

        private void AcList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_acPopupList.SelectedItem is ListBoxItem item && item.Tag is string url)
            {
                HideAutocomplete();
                AddressBox.Text = url;
                if (_active != null) NavigateTo(_active, url);
                Keyboard.ClearFocus();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // SECURITY + BOOKMARK ICONS
        // ════════════════════════════════════════════════════════════════════════
        private void UpdateSecurityIcon(string url)
        {
            bool secure = UrlHelper.IsSecure(url);
            bool intern = UrlHelper.IsInternalPage(url);
            LockIcon.Visibility = (secure && !intern) ? Visibility.Visible : Visibility.Collapsed;
            GlobeIcon.Visibility = (!secure || intern) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateBookmarkStar(string url)
        {
            bool have = _bookmarks.Any(b => b.Url == url);
            BookmarkIcon.Data = (Geometry)FindResource(have ? "IcoStarFilled" : "IcoStarOutline");
            BookmarkIcon.Fill = have ? (Brush)FindResource("Accent") : Brushes.Transparent;
            BookmarkIcon.Stroke = have ? (Brush)FindResource("Accent") : (Brush)FindResource("TextSub");
        }

        // ════════════════════════════════════════════════════════════════════════
        // BOOKMARKS
        // ════════════════════════════════════════════════════════════════════════
        private void LoadBookmarks()
        {
            try
            {
                if (!File.Exists(BookmarksFile)) { SeedBookmarks(); return; }
                foreach (var line in File.ReadAllLines(BookmarksFile))
                {
                    int sep = line.IndexOf('|');
                    if (sep > 0) _bookmarks.Add((line.Substring(0, sep), line.Substring(sep + 1)));
                }
            }
            catch { SeedBookmarks(); }
        }

        private void SeedBookmarks()
        {
            _bookmarks.Add(("Brave", "https://search.brave.com"));
            _bookmarks.Add(("GitHub", "https://github.com"));
            _bookmarks.Add(("YouTube", "https://www.youtube.com"));
            _bookmarks.Add(("Reddit", "https://www.reddit.com"));
            SaveBookmarks();
        }

        private void SaveBookmarks()
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BookmarksFile));
                File.WriteAllLines(BookmarksFile, _bookmarks.Select(b => b.Title + "|" + b.Url));
            }
            catch { }
        }

        private void RenderBookmarks()
        {
            BookmarkItems.Children.Clear();
            foreach (var bm in _bookmarks.Take(24))
            {
                var url = bm.Url; var label = bm.Title;
                var btn = new Button { Style = (Style)FindResource("BookmarkBtn"), Content = label, ToolTip = url };
                btn.Click += (s, e) => { if (_active != null) NavigateTo(_active, url); };
                BookmarkItems.Children.Add(btn);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ZOOM
        // ════════════════════════════════════════════════════════════════════════
        private void AdjustZoom(int dir)
        {
            if (_active == null || !_isWebViewReady) return;
            var cur = _zoomMap.TryGetValue(_active.Id, out double z) ? z : 1.0;
            int idx = Array.FindIndex(_zoomSteps, s => Math.Abs(s - cur) < 0.001);
            if (idx < 0) idx = 6;
            idx = Math.Max(0, Math.Min(_zoomSteps.Length - 1, idx + dir));
            ApplyZoom(_active, _zoomSteps[idx]);
        }

        private void ApplyZoom(BrowserTab tab, double zoom)
        {
            _zoomMap[tab.Id] = zoom;
            if (_isWebViewReady) _sharedWebView.ZoomFactor = zoom;
            if (tab == _active) SyncZoomLabel(tab.Id);
        }

        private void SyncZoomLabel(Guid id)
        {
            var z = _zoomMap.TryGetValue(id, out double v) ? v : 1.0;
            var pct = (int)Math.Round(z * 100);
            ZoomLabel.Text = pct + "%";
            ZoomLabel.Foreground = Math.Abs(z - 1.0) > 0.001
                ? (Brush)FindResource("Accent") : (Brush)FindResource("TextDim");
        }

        // ════════════════════════════════════════════════════════════════════════
        // FIND IN PAGE
        // ════════════════════════════════════════════════════════════════════════
        private void ToggleFind()
        {
            if (FindBarPanel.Visibility == Visibility.Collapsed)
            {
                FindBarPanel.Visibility = Visibility.Visible;
                FindBox.Text = string.Empty; FindBox.Focus();
            }
            else CloseFind();
        }

        private void CloseFind()
        {
            FindBarPanel.Visibility = Visibility.Collapsed;
            FindResult.Text = string.Empty;
            if (_isWebViewReady)
                _sharedWebView?.CoreWebView2?.ExecuteScriptAsync("window.find('',false,false,false,false,true,true)");
        }

        private async void DoFind(bool forward)
        {
            var q = FindBox.Text;
            if (string.IsNullOrEmpty(q) || !_isWebViewReady) return;
            try
            {
                var esc = q.Replace("\\", "\\\\").Replace("'", "\\'");
                await _sharedWebView.CoreWebView2.ExecuteScriptAsync(
                    "window.find('" + esc + "',false," + (!forward ? "true" : "false") + ",true,false,true,false)");
            }
            catch { }
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════════════
        private async Task OpenOrCreateInternalTab(string url)
        {
            var existing = _tabs.FirstOrDefault(t => t.Url == url);
            if (existing != null) { await ActivateTabAsync(existing); return; }
            await CreateTabAsync(url, true, true);
        }

        private System.Windows.Shapes.Path MakePath(string geoKey, double size, string hex)
        {
            return new System.Windows.Shapes.Path
            {
                Data = (Geometry)FindResource(geoKey),
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                Fill = Brushes.Transparent,
                Stroke = (Brush)new BrushConverter().ConvertFromString(hex),
                StrokeThickness = 1.8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        // ════════════════════════════════════════════════════════════════════════
        // BUTTON HANDLERS
        // ════════════════════════════════════════════════════════════════════════
        private void BtnBack_Click(object s, RoutedEventArgs e)
        {
            if (_isWebViewReady) _sharedWebView?.CoreWebView2?.GoBack();
        }

        private void BtnForward_Click(object s, RoutedEventArgs e)
        {
            if (_isWebViewReady) _sharedWebView?.CoreWebView2?.GoForward();
        }

        private void BtnRefresh_Click(object s, RoutedEventArgs e)
        {
            if (!_isWebViewReady) return;
            if (_active?.IsLoading == true) _sharedWebView.CoreWebView2?.Stop();
            else _sharedWebView.CoreWebView2?.Reload();
        }

        private async void BtnNewTab_Click(object s, RoutedEventArgs e)
        {
            try { await CreateTabAsync("rune://home", true, true); } catch { }
        }

        private void BtnBookmark_Click(object s, RoutedEventArgs e)
        {
            if (_active == null || UrlHelper.IsInternalPage(_active.Url)) return;
            var url = _active.Url; var title = _active.Title ?? url;
            if (_bookmarks.Any(b => b.Url == url)) _bookmarks.RemoveAll(b => b.Url == url);
            else _bookmarks.Add((title, url));
            SaveBookmarks(); RenderBookmarks(); UpdateBookmarkStar(url);
        }

        private void BtnFind_Click(object s, RoutedEventArgs e) => ToggleFind();

        private void BtnDownloads_Click(object s, RoutedEventArgs e)
            => _ = OpenOrCreateInternalTab("rune://downloads");

        private void BtnSettings_Click(object s, RoutedEventArgs e)
            => _ = OpenOrCreateInternalTab("rune://settings");

        private void BtnFindClose_Click(object s, RoutedEventArgs e) => CloseFind();
        private void BtnFindNext_Click(object s, RoutedEventArgs e) => DoFind(true);
        private void BtnFindPrev_Click(object s, RoutedEventArgs e) => DoFind(false);
        private void BtnZoomIn_Click(object s, RoutedEventArgs e) => AdjustZoom(+1);
        private void BtnZoomOut_Click(object s, RoutedEventArgs e) => AdjustZoom(-1);

        private void ZoomLabel_MouseDown(object s, MouseButtonEventArgs e)
        { if (e.ClickCount == 2 && _active != null) ApplyZoom(_active, 1.0); }

        // ════════════════════════════════════════════════════════════════════════
        // ADDRESS BAR
        // ════════════════════════════════════════════════════════════════════════
        private void AddressBox_GotFocus(object s, RoutedEventArgs e)
        {
            AddrBorder.BorderBrush = (Brush)FindResource("Accent");
            if (!UrlHelper.IsInternalPage(_active?.Url ?? "")) AddressBox.Text = _active?.Url ?? "";
            AddressBox.SelectAll();
        }

        private void AddressBox_LostFocus(object s, RoutedEventArgs e)
        {
            AddrBorder.BorderBrush = (Brush)FindResource("BorderAddr");
            HideAutocomplete();
        }

        private void AddressBox_TextChanged(object s, TextChangedEventArgs e)
        {
            if (AddressBox.IsKeyboardFocused && !_suppressAc)
                ShowAutocomplete(AddressBox.Text);
        }

        private void AddressBox_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Down && _acPopup.IsOpen)
            {
                if (_acPopupList.Items.Count > 0)
                {
                    _acPopupList.Focus();
                    _acPopupList.SelectedIndex = 0;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && _active != null)
            {
                _suppressAc = true;
                HideAutocomplete();
                var text = AddressBox.Text;
                NavigateTo(_active, text);
                Keyboard.ClearFocus();
                _suppressAc = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HideAutocomplete();
                AddressBox.Text = UrlHelper.GetDisplayUrl(_active?.Url ?? "");
                Keyboard.ClearFocus();
            }
        }

        private void AddressBox_PreviewMouseDown(object s, MouseButtonEventArgs e)
        {
            if (!AddressBox.IsKeyboardFocused) { AddressBox.Focus(); AddressBox.SelectAll(); e.Handled = true; }
        }

        // ════════════════════════════════════════════════════════════════════════
        // FIND BAR
        // ════════════════════════════════════════════════════════════════════════
        private void FindBox_TextChanged(object s, TextChangedEventArgs e)
        { if (FindBarPanel.Visibility == Visibility.Visible && FindBox.Text.Length > 0) DoFind(true); }

        private void FindBox_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Return && (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0)
            { DoFind(false); e.Handled = true; }
            else if (e.Key == Key.Return) { DoFind(true); e.Handled = true; }
            else if (e.Key == Key.Escape) { CloseFind(); e.Handled = true; }
        }

        // ════════════════════════════════════════════════════════════════════════
        // WINDOW CHROME
        // ════════════════════════════════════════════════════════════════════════
        private void BtnMinimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object s, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object s, RoutedEventArgs e) => Close();

        private void Window_StateChanged(object s, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MaxWidth = SystemParameters.WorkArea.Width;
                MaxHeight = SystemParameters.WorkArea.Height;
                if (MaxIcon != null) MaxIcon.Data = (Geometry)FindResource("IcoRestore");
            }
            else
            {
                MaxWidth = MaxHeight = double.PositiveInfinity;
                if (MaxIcon != null) MaxIcon.Data = (Geometry)FindResource("IcoMaximize");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _closing = true;

            SessionManager.Save(_tabs.Select(t => new SessionEntry
            {
                Url = t.Url,
                Title = t.Title,
                Active = t == _active
            }));

            try { _sharedWebView?.Dispose(); } catch { }

            base.OnClosed(e);
        }

        // Add these methods inside the MainWindow class

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            if (_active != null)
                NavigateTo(_active, "rune://home");
        }

        private void BtnExtensions_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(0x13, 0x15, 0x1B)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x32, 0x45)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0, 4, 0, 4)
            };

            var adblockItem = new MenuItem
            {
                Header = "uBlock Origin (Built-in)",
                IsCheckable = true,
                IsChecked = AppSettings.AdblockEnabled,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x90, 0xA4)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 6, 20, 6),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };
            adblockItem.Click += (s, ev) =>
            {
                AppSettings.AdblockEnabled = adblockItem.IsChecked;
                if (_isWebViewReady) _sharedWebView?.CoreWebView2?.Reload();
            };
            menu.Items.Add(adblockItem);

            var darkItem = new MenuItem
            {
                Header = "Dark Reader (Built-in)",
                IsCheckable = true,
                IsChecked = AppSettings.ForceDarkMode,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x90, 0xA4)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 6, 20, 6),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };
            darkItem.Click += (s, ev) =>
            {
                AppSettings.ForceDarkMode = darkItem.IsChecked;
                if (_isWebViewReady && _sharedWebView?.CoreWebView2 != null)
                {
                    if (AppSettings.ForceDarkMode)
                    {
                        _sharedWebView.CoreWebView2.ExecuteScriptAsync(@"
                    (function(){
                        if(!document.getElementById('rune-dark-mode')){
                            var s=document.createElement('style');
                            s.id='rune-dark-mode';
                            s.textContent='html{filter:invert(1) hue-rotate(180deg) !important} img,video,iframe{filter:invert(1) hue-rotate(180deg) !important}';
                            document.head.appendChild(s);
                        }
                    })()
                ");
                    }
                    else
                    {
                        _sharedWebView.CoreWebView2.ExecuteScriptAsync(@"
                    var s=document.getElementById('rune-dark-mode');
                    if(s) s.remove();
                ");
                    }
                }
            };
            menu.Items.Add(darkItem);

            menu.Items.Add(new Separator
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x30)),
                Margin = new Thickness(0, 2, 0, 2)
            });

            var manageItem = new MenuItem
            {
                Header = "Manage Extensions...",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x90, 0xA4)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 6, 20, 6),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };
            manageItem.Click += (s, ev) => _ = OpenOrCreateInternalTab("rune://extensions");
            menu.Items.Add(manageItem);

            menu.PlacementTarget = BtnExtensions;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(0x13, 0x15, 0x1B)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x32, 0x45)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0, 4, 0, 4)
            };

            void AddItem(string header, Action click, bool danger = false)
            {
                var item = new MenuItem
                {
                    Header = header,
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(danger ? Color.FromRgb(0xE8, 0x22, 0x3A) : Color.FromRgb(0x88, 0x90, 0xA4)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(14, 6, 20, 6),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12
                };
                item.Click += (s, ev) => click();
                menu.Items.Add(item);
            }

            AddItem("New Tab", () => _ = CreateTabAsync("rune://home", true, true));
            AddItem("New Window", () =>
            {
                var win = new MainWindow();
                win.Show();
            });
            menu.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x30)), Margin = new Thickness(0, 2, 0, 2) });

            AddItem("History", () => _ = OpenOrCreateInternalTab("rune://history"));
            AddItem("Downloads", () => _ = OpenOrCreateInternalTab("rune://downloads"));
            AddItem("Bookmarks", () => _ = OpenOrCreateInternalTab("rune://bookmarks"));
            menu.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x30)), Margin = new Thickness(0, 2, 0, 2) });

            AddItem("Find...", () => ToggleFind());
            menu.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x30)), Margin = new Thickness(0, 2, 0, 2) });

            AddItem("Settings", () => _ = OpenOrCreateInternalTab("rune://settings"));
            AddItem("Themes", () => _ = OpenOrCreateInternalTab("rune://themes"));
            menu.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x30)), Margin = new Thickness(0, 2, 0, 2) });

            AddItem("Exit", () => Close(), danger: true);

            menu.PlacementTarget = BtnMenu;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // KEYBOARD SHORTCUTS
        // ════════════════════════════════════════════════════════════════════════
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            bool ctrl = (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0;
            bool shift = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0;
            bool alt = (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) != 0;

            if (e.Key == Key.Escape && _acPopup.IsOpen) { HideAutocomplete(); e.Handled = true; return; }
            if (e.Key == Key.Escape && FindBarPanel.Visibility == Visibility.Visible) { CloseFind(); e.Handled = true; return; }

            if (!ctrl) goto altKeys;

            switch (e.Key)
            {
                case Key.T: _ = CreateTabAsync("rune://home", true, true); e.Handled = true; return;
                case Key.W: if (_active != null) CloseTab(_active); e.Handled = true; return;
                case Key.L: AddressBox.Focus(); AddressBox.SelectAll(); e.Handled = true; return;
                case Key.F: ToggleFind(); e.Handled = true; return;
                case Key.D: BtnBookmark_Click(null, null); e.Handled = true; return;
                case Key.H: _ = OpenOrCreateInternalTab("rune://history"); e.Handled = true; return;
                case Key.J: _ = OpenOrCreateInternalTab("rune://downloads"); e.Handled = true; return;
                case Key.B: _ = OpenOrCreateInternalTab("rune://bookmarks"); e.Handled = true; return;
                case Key.OemComma: _ = OpenOrCreateInternalTab("rune://settings"); e.Handled = true; return;
                case Key.F5:
                    if (_isWebViewReady)
                        _sharedWebView?.CoreWebView2?.CallDevToolsProtocolMethodAsync("Page.reload", "{\"ignoreCache\":true}");
                    e.Handled = true; return;
                case Key.OemPlus: case Key.Add: AdjustZoom(+1); e.Handled = true; return;
                case Key.OemMinus: case Key.Subtract: AdjustZoom(-1); e.Handled = true; return;
                case Key.D0: if (_active != null) ApplyZoom(_active, 1.0); e.Handled = true; return;
                case Key.Tab:
                    if (_tabs.Count > 1)
                    {
                        int i = _tabs.IndexOf(_active);
                        var nextTab = _tabs[shift ? (i - 1 + _tabs.Count) % _tabs.Count : (i + 1) % _tabs.Count];
                        _ = ActivateTabAsync(nextTab);
                    }
                    e.Handled = true; return;
            }

            if (ctrl && shift && e.Key == Key.K && _active != null)
            { _ = CreateTabAsync(_active.Url, true, true); e.Handled = true; return; }

            if (ctrl && e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                int idx = e.Key == Key.D9 ? _tabs.Count - 1 : (e.Key - Key.D1);
                if (idx >= 0 && idx < _tabs.Count) _ = ActivateTabAsync(_tabs[idx]);
                e.Handled = true; return;
            }

        altKeys:
            if (!alt) { if (e.Key == Key.F5 && _isWebViewReady) { _sharedWebView?.CoreWebView2?.Reload(); e.Handled = true; } return; }
            if (e.Key == Key.Left) { BtnBack_Click(null, null); e.Handled = true; }
            if (e.Key == Key.Right) { BtnForward_Click(null, null); e.Handled = true; }
        }
    }
}