using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RuneS.Helpers
{
    public static class AppSettings
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RuneS", "settings.cfg");

        private static Dictionary<string, string> _data = new Dictionary<string, string>();
        private static bool _loaded;

        // ── Search ──────────────────────────────────────────────────────────
        public static string SearchEngine
        {
            get => Get("searchEngine", "https://search.brave.com/search?q=");
            set { Set("searchEngine", value); Save(); }
        }

        // ── UI ──────────────────────────────────────────────────────────────
        public static bool ShowBookmarksBar
        {
            get => Get("showBookmarks", "true") == "true";
            set { Set("showBookmarks", value ? "true" : "false"); Save(); }
        }

        // Add these to AppSettings.cs

        public static bool AdblockEnabled
        {
            get => Get("adblockEnabled", "true") == "true";
            set { Set("adblockEnabled", value ? "true" : "false"); Save(); }
        }

        public static bool ForceDarkMode
        {
            get => Get("forceDarkMode", "false") == "true";
            set { Set("forceDarkMode", value ? "true" : "false"); Save(); }
        }

        public static double DefaultZoom
        {
            get => double.TryParse(Get("defaultZoom", "1.0"), out double v) ? v : 1.0;
            set { Set("defaultZoom", value.ToString("F2")); Save(); }
        }

        // ── Downloads ───────────────────────────────────────────────────────
        public static string DownloadsFolder
        {
            get => Get("downloadsFolder",
                       Path.Combine(Environment.GetFolderPath(
                           Environment.SpecialFolder.UserProfile), "Downloads"));
            set { Set("downloadsFolder", value); Save(); }
        }

        // ── Privacy ─────────────────────────────────────────────────────────
        public static bool DoNotTrack
        {
            get => Get("doNotTrack", "false") == "true";
            set { Set("doNotTrack", value ? "true" : "false"); Save(); }
        }

        public static bool BlockThirdPartyCookies
        {
            get => Get("blockThirdParty", "false") == "true";
            set { Set("blockThirdParty", value ? "true" : "false"); Save(); }
        }

        // ── Performance ─────────────────────────────────────────────────────
        public static bool SuspendBackgroundTabs
        {
            get => Get("suspendTabs", "true") == "true";
            set { Set("suspendTabs", value ? "true" : "false"); Save(); }
        }

        // ── Session ─────────────────────────────────────────────────────────
        public static bool RestoreLastSession
        {
            get => Get("restoreSession", "true") == "true";
            set { Set("restoreSession", value ? "true" : "false"); Save(); }
        }

        // ── Theme ────────────────────────────────────────────────────────────
        public static string ThemeId
        {
            get => Get("themeId", "carbon");
            set { Set("themeId", value); Save(); }
        }

        // ── Fonts ────────────────────────────────────────────────────────────
        public static string DefaultFont
        {
            get => Get("defaultFont", "Segoe UI");
            set { Set("defaultFont", value); Save(); }
        }

        public static int DefaultFontSize
        {
            get => int.TryParse(Get("defaultFontSize", "16"), out int v) ? v : 16;
            set { Set("defaultFontSize", value.ToString()); Save(); }
        }

        // ── Startup ──────────────────────────────────────────────────────────
        public static string StartupPage
        {
            get => Get("startupPage", "rune://home");
            set { Set("startupPage", value); Save(); }
        }

        // ── Reader Mode ──────────────────────────────────────────────────────
        public static bool ReaderModeEnabled
        {
            get => Get("readerMode", "true") == "true";
            set { Set("readerMode", value ? "true" : "false"); Save(); }
        }

        // ── Internal accessors used by ThemeManager ─────────────────────────
        public static string Get(string key, string def = "")
        {
            Load();
            return _data.TryGetValue(key, out var v) ? v : def;
        }

        public static void Set(string key, string value)
        {
            Load();
            _data[key] = value;
        }

        // ── IO ──────────────────────────────────────────────────────────────
        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _data = new Dictionary<string, string>();
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var eq = line.IndexOf('=');
                    if (eq < 1) continue;
                    _data[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var lines = new List<string>();
                foreach (var kv in _data)
                    lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(FilePath, lines);
            }
            catch { }
        }

        /// <summary>JSON snapshot for the settings page.</summary>
        public static string ToJson()
        {
            Load();
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(J("searchEngine",           SearchEngine));
            sb.Append(",");
            sb.Append(J("showBookmarksBar",        ShowBookmarksBar ? "true" : "false", false));
            sb.Append(",");
            sb.Append(J("defaultZoom",             DefaultZoom.ToString("F2"), false));
            sb.Append(",");
            sb.Append(J("downloadsFolder",         DownloadsFolder));
            sb.Append(",");
            sb.Append(J("doNotTrack",              DoNotTrack ? "true" : "false", false));
            sb.Append(",");
            sb.Append(J("blockThirdPartyCookies",  BlockThirdPartyCookies ? "true" : "false", false));
            sb.Append(",");
            sb.Append(J("suspendBackgroundTabs",   SuspendBackgroundTabs ? "true" : "false", false));
            sb.Append(",");
            sb.Append(J("restoreLastSession",      RestoreLastSession ? "true" : "false", false));
            sb.Append(",");
            sb.Append(J("themeId",                 ThemeId));
            sb.Append(",");
            sb.Append(J("defaultFont",             DefaultFont));
            sb.Append(",");
            sb.Append(J("defaultFontSize",         DefaultFontSize.ToString(), false));
            sb.Append(",");
            sb.Append(J("startupPage",             StartupPage));
            sb.Append(",");
            sb.Append(J("readerModeEnabled",       ReaderModeEnabled ? "true" : "false", false));
            sb.Append(",");
            sb.Append(J("version",                 "1.0.0"));
            sb.Append("}");
            return sb.ToString();
        }

        private static string J(string key, string val, bool quote = true)
        {
            var v = val.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return quote
                ? "\"" + key + "\":\"" + v + "\""
                : "\"" + key + "\":" + val;
        }

        public static bool SaveTabThumbnails
        {
            get => Get("saveThumbnails", "false") == "true";
            set { Set("saveThumbnails", value ? "true" : "false"); Save(); }
        }

        public static void ApplyFromJson(string key, string value)
        {
            switch (key)
            {
                case "searchEngine":          SearchEngine            = value; break;
                case "showBookmarksBar":       ShowBookmarksBar        = value == "true"; break;
                case "defaultZoom":
                    if (double.TryParse(value, out double z)) DefaultZoom = z; break;
                case "downloadsFolder":        DownloadsFolder         = value; break;
                case "doNotTrack":             DoNotTrack              = value == "true"; break;
                case "blockThirdPartyCookies": BlockThirdPartyCookies  = value == "true"; break;
                case "suspendBackgroundTabs":  SuspendBackgroundTabs   = value == "true"; break;
                case "restoreLastSession":     RestoreLastSession      = value == "true"; break;
                case "defaultFont":            DefaultFont             = value; break;
                case "defaultFontSize":
                    if (int.TryParse(value, out int fs)) DefaultFontSize = fs; break;
                case "startupPage":            StartupPage             = value; break;
                case "readerModeEnabled":      ReaderModeEnabled       = value == "true"; break;
            }
        }
    }
}
