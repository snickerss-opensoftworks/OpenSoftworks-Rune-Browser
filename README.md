# ⚡ RuneS — RuneSearch Browser

A lightweight WPF browser combining the spirit of **Brave** and **Microsoft Edge**, built on the **WebView2 / Chromium** engine. Inspired by the clean nostalgia of Windows XP Luna, but modernised with flat UI, rounded corners, and a blue accent palette.

---

## ✨ Features

| Feature | Details |
|---|---|
| **Tabbed browsing** | Create, close, and switch tabs — Ctrl+T, Ctrl+W, Ctrl+Tab |
| **Address bar** | Smart input: URLs, domains, or search queries (Brave Search) |
| **Navigation** | Back, Forward, Refresh, Stop, Home |
| **Bookmarks bar** | Always-visible bar with quick-access sites |
| **New-tab speed dial** | Clock, search box, and quick-access shortcuts |
| **Security indicator** | 🔒 Lock icon for HTTPS, globe icon otherwise |
| **Loading indicator** | Slim 2px progress bar |
| **Keyboard shortcuts** | Full keyboard control (see table below) |
| **Low memory** | Shared WebView2 data folder, no unnecessary features |
| **Windows 11 DWM** | Rounded window corners + accent border via DWM API |

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+T` | New tab |
| `Ctrl+W` | Close current tab |
| `Ctrl+L` | Focus address bar |
| `Ctrl+D` | Bookmark page |
| `F5` | Refresh |
| `Alt+←` | Back |
| `Alt+→` | Forward |
| `Ctrl+Tab` | Switch to next tab |
| `Esc` | Cancel navigation / restore URL |

---

## 🛠 Requirements

### Development
- **Visual Studio 2022 Community** (any edition)
- **.NET SDK 4.8.1** (pre-installed with VS 2022)
- C# 7.3 language version (set in `.csproj`)
- NuGet package: `Microsoft.Web.WebView2` (auto-restored on first build)

### Runtime (end user)
- **Windows 10 1803+** or **Windows 11** (required for WebView2)
- **Microsoft Edge WebView2 Runtime** (Evergreen) — usually already installed on Windows 10/11

  If not installed, download from:
  https://developer.microsoft.com/microsoft-edge/webview2/

---

## 🏗 Building

1. Open **`RuneS.sln`** in Visual Studio 2022
2. Right-click the solution → **Restore NuGet Packages**
3. Select **Release | x64**
4. Press **F5** (Debug) or **Ctrl+Shift+B** (Build)

Or from the command line:
```bash
dotnet restore
dotnet build -c Release -r win-x64
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## 📁 Project Structure

```
RuneS/
├── RuneS.sln
└── RuneS/
    ├── RuneS.csproj
    ├── App.xaml / App.xaml.cs
    ├── MainWindow.xaml / MainWindow.xaml.cs
    ├── Models/
    │   └── BrowserTab.cs          ← Tab model (INotifyPropertyChanged)
    ├── Helpers/
    │   ├── UrlHelper.cs           ← URL/search processing
    │   ├── WindowEffectsHelper.cs ← DWM API (rounded corners, border colour)
    │   └── NewTabPageBuilder.cs   ← Speed-dial HTML page
    └── Resources/
        └── Styles.xaml            ← All WPF styles & colour palette
```

---

## 🎨 Design Language

- **Title bar**: XP Luna gradient (`#1A5DBE → #0B3D91`) + white Segoe MDL2 icons
- **Tab strip**: Light blue-gray (`#E4EAF8`) with active tabs highlighted in white + blue underline
- **Address bar**: Pill shape, rounded 14px, blue border on focus
- **Icons**: Segoe MDL2 Assets (built into Windows 10+, no external icon files needed)
- **Accent**: `#1A73E8` (Google Blue / Edge Blue)
- **Window**: `WindowStyle=None` + `WindowChrome` for resize/move + DWM rounded corners

---

## 🔧 Customisation Ideas

- **Search engine**: Edit `UrlHelper.SearchEngine` constant
- **Home page**: Change `MainWindow.HomePage` constant
- **Bookmarks**: Default bookmarks are set in `MainWindow._bookmarks`
- **Theme**: All colours are in `Resources/Styles.xaml` — just change the hex values

---

## ⚡ Performance Tips (Low-End Devices)

- All tabs share **one WebView2 browser process** via a shared `UserDataFolder`
- The status bar and loading animations are minimal
- `AllowsTransparency` is `False` — no software-rendered transparent window
- Hardware-accelerated WebView2 rendering (Chromium GPU compositing)
- Avoid keeping many tabs open simultaneously to conserve RAM

---

## 📄 License

MIT — do whatever you want with it.
