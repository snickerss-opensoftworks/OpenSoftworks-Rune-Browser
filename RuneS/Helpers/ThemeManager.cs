using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace RuneS.Helpers
{
    public class BrowserTheme
    {
        public string Id      { get; set; }
        public string Name    { get; set; }
        public string Preview { get; set; } // hex accent color for swatch

        // Window chrome colors
        public string BgWindow    { get; set; }
        public string BgTabBar    { get; set; }
        public string BgTabActive { get; set; }
        public string BgTabInact  { get; set; }
        public string BgNav       { get; set; }
        public string BgAddr      { get; set; }
        public string Accent      { get; set; }
        public string AccentDim   { get; set; }
        public string Text        { get; set; }
        public string TextSub     { get; set; }
        public string TextDim     { get; set; }
        public string Border      { get; set; }
        public string Border2     { get; set; }
        public string BorderAddr  { get; set; }
        public string Sep         { get; set; }
        public string Green       { get; set; }
        public string Red         { get; set; }
    }

    public static class ThemeManager
    {
        private static readonly string CustomDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RuneS", "themes");

        public static readonly List<BrowserTheme> BuiltIn = new List<BrowserTheme>
        {
            new BrowserTheme {
                Id="carbon", Name="Carbon Blackout", Preview="#4D9EFF",
                BgWindow="#080A0E", BgTabBar="#0B0D12", BgTabActive="#1A1C22",
                BgTabInact="#0B0D12", BgNav="#13151B", BgAddr="#1C1E26",
                Accent="#4D9EFF", AccentDim="#2A6ECC", Text="#F0F2F8",
                TextSub="#8890A4", TextDim="#44485A", Border="#1E2130",
                Border2="#2E3245", BorderAddr="#282C3A", Sep="#13151B",
                Green="#4EC94E", Red="#E8223A"
            },
            new BrowserTheme {
                Id="morpheon", Name="Morpheon Dark", Preview="#7B8FFF",
                BgWindow="#090B10", BgTabBar="#0C0F17", BgTabActive="#181D2A",
                BgTabInact="#0C0F17", BgNav="#111420", BgAddr="#191C29",
                Accent="#7B8FFF", AccentDim="#4A5ACC", Text="#E8EBF5",
                TextSub="#7880A0", TextDim="#404464", Border="#1B1E2E",
                Border2="#2A2E45", BorderAddr="#252838", Sep="#0F1219",
                Green="#4ECC7A", Red="#F04060"
            },
            new BrowserTheme {
                Id="purple", Name="Midnight Purple", Preview="#A855F7",
                BgWindow="#09080F", BgTabBar="#0D0B15", BgTabActive="#1A1728",
                BgTabInact="#0D0B15", BgNav="#121020", BgAddr="#1A1828",
                Accent="#A855F7", AccentDim="#7C3AED", Text="#F0EAFF",
                TextSub="#8876AA", TextDim="#443A64", Border="#1E1830",
                Border2="#2E2845", BorderAddr="#28223C", Sep="#100E1A",
                Green="#4EC94E", Red="#E8223A"
            },
            new BrowserTheme {
                Id="ocean", Name="Deep Ocean", Preview="#06B6D4",
                BgWindow="#080E10", BgTabBar="#0A1216", BgTabActive="#141E24",
                BgTabInact="#0A1216", BgNav="#0E1820", BgAddr="#141E28",
                Accent="#06B6D4", AccentDim="#0284C7", Text="#E0F4F8",
                TextSub="#6090A8", TextDim="#304858", Border="#142028",
                Border2="#1E3040", BorderAddr="#1A2838", Sep="#0A1216",
                Green="#4ECC9A", Red="#F04A4A"
            },
            new BrowserTheme {
                Id="dracula", Name="Dracula", Preview="#BD93F9",
                BgWindow="#191A21", BgTabBar="#21222C", BgTabActive="#282A36",
                BgTabInact="#21222C", BgNav="#21222C", BgAddr="#2D2F3F",
                Accent="#BD93F9", AccentDim="#9067D8", Text="#F8F8F2",
                TextSub="#9090B0", TextDim="#5C5C7A", Border="#343547",
                Border2="#44475A", BorderAddr="#383A50", Sep="#181920",
                Green="#50FA7B", Red="#FF5555"
            },
            new BrowserTheme {
                Id="nord", Name="Nord", Preview="#88C0D0",
                BgWindow="#2E3440", BgTabBar="#242933", BgTabActive="#3B4252",
                BgTabInact="#242933", BgNav="#2E3440", BgAddr="#3B4252",
                Accent="#88C0D0", AccentDim="#5E81AC", Text="#ECEFF4",
                TextSub="#A0AABB", TextDim="#606878", Border="#3B4252",
                Border2="#4C566A", BorderAddr="#434C5E", Sep="#2E3440",
                Green="#A3BE8C", Red="#BF616A"
            },
            new BrowserTheme {
                Id="catppuccin", Name="Catppuccin Mocha", Preview="#CBA6F7",
                BgWindow="#1E1E2E", BgTabBar="#181825", BgTabActive="#313244",
                BgTabInact="#181825", BgNav="#1E1E2E", BgAddr="#313244",
                Accent="#CBA6F7", AccentDim="#9055C0", Text="#CDD6F4",
                TextSub="#A6ADC8", TextDim="#5A5F78", Border="#2A2A3E",
                Border2="#45475A", BorderAddr="#3A3A50", Sep="#181825",
                Green="#A6E3A1", Red="#F38BA8"
            },
            new BrowserTheme {
                Id="light", Name="Light Chrome", Preview="#1A73E8",
                BgWindow="#F2F2F2", BgTabBar="#DEE1E6", BgTabActive="#FFFFFF",
                BgTabInact="#DEE1E6", BgNav="#F2F2F2", BgAddr="#FFFFFF",
                Accent="#1A73E8", AccentDim="#1557C4", Text="#1A1A1A",
                TextSub="#5F6368", TextDim="#9AA0A6", Border="#D0D0D0",
                Border2="#C0C0C0", BorderAddr="#DADCE0", Sep="#E8E8E8",
                Green="#188038", Red="#D93025"
            }
        };

        private static BrowserTheme _current;

        public static BrowserTheme Current =>
            _current ?? BuiltIn[0];

        public static string CurrentId
        {
            get => AppSettings.Get("themeId", "carbon");
        }

        public static void Load()
        {
            var id = CurrentId;
            var theme = BuiltIn.Find(t => t.Id == id);
            if (theme == null)
            {
                // Try custom
                theme = LoadCustom(id) ?? BuiltIn[0];
            }
            Apply(theme);
        }

        public static void Apply(BrowserTheme t)
        {
            _current = t;
            AppSettings.Set("themeId", t.Id);

            var res = Application.Current.Resources;

            SetBrush(res, "BgWindow",    t.BgWindow);
            SetBrush(res, "BgTitle",     t.BgTabBar);
            SetBrush(res, "BgTabBar",    t.BgTabBar);
            SetBrush(res, "BgTabActive", t.BgTabActive);
            SetBrush(res, "BgTabInactive", t.BgTabInact);
            SetBrush(res, "BgTabHover",  Lighten(t.BgTabInact, 10));
            SetBrush(res, "BgNav",       t.BgNav);
            SetBrush(res, "BgAddr",      t.BgAddr);
            SetBrush(res, "Accent",      t.Accent);
            SetBrush(res, "AccentDim",   t.AccentDim);
            SetBrush(res, "Text",        t.Text);
            SetBrush(res, "TextSub",     t.TextSub);
            SetBrush(res, "TextDim",     t.TextDim);
            SetBrush(res, "Border",      t.Border);
            SetBrush(res, "Border2",     t.Border2);
            SetBrush(res, "BorderAddr",  t.BorderAddr);
            SetBrush(res, "Sep",         t.Sep);
            SetBrush(res, "Green",       t.Green);
            SetBrush(res, "Red",         t.Red);

            // Update gradient brushes
            try
            {
                var gloss = (LinearGradientBrush)res["Gloss"];
                // Keep gloss as-is — it's always white overlay, works on any theme
            }
            catch { }
        }

        public static string GetCssVars(BrowserTheme t = null)
        {
            t = t ?? Current;
            return $@":root{{
  --bg:{t.BgWindow};--bg2:{t.BgTabBar};--bg3:{t.BgNav};--bg4:{t.BgAddr};
  --border:{t.Border};--border2:{t.Border2};
  --accent:{t.Accent};--accentDim:rgba(77,158,255,.12);
  --text:{t.Text};--sub:{t.TextSub};--dim:{t.TextDim};
  --green:{t.Green};--red:{t.Red};
}}";
        }

        public static void SaveCustomTheme(string id, string name, BrowserTheme theme)
        {
            try
            {
                Directory.CreateDirectory(CustomDir);
                var lines = new List<string>
                {
                    "id=" + id, "name=" + name,
                    "BgWindow=" + theme.BgWindow, "BgTabBar=" + theme.BgTabBar,
                    "BgTabActive=" + theme.BgTabActive, "BgTabInact=" + theme.BgTabInact,
                    "BgNav=" + theme.BgNav, "BgAddr=" + theme.BgAddr,
                    "Accent=" + theme.Accent, "AccentDim=" + theme.AccentDim,
                    "Text=" + theme.Text, "TextSub=" + theme.TextSub,
                    "TextDim=" + theme.TextDim, "Border=" + theme.Border,
                    "Border2=" + theme.Border2, "BorderAddr=" + theme.BorderAddr,
                    "Sep=" + theme.Sep, "Green=" + theme.Green, "Red=" + theme.Red,
                    "preview=" + theme.Preview
                };
                File.WriteAllLines(Path.Combine(CustomDir, id + ".theme"), lines);
            }
            catch { }
        }

        public static List<BrowserTheme> GetCustomThemes()
        {
            var list = new List<BrowserTheme>();
            try
            {
                if (!Directory.Exists(CustomDir)) return list;
                foreach (var f in Directory.GetFiles(CustomDir, "*.theme"))
                    try { var t = LoadCustomFile(f); if (t != null) list.Add(t); }
                    catch { }
            }
            catch { }
            return list;
        }

        private static BrowserTheme LoadCustom(string id)
        {
            try
            {
                var f = Path.Combine(CustomDir, id + ".theme");
                return File.Exists(f) ? LoadCustomFile(f) : null;
            }
            catch { return null; }
        }

        private static BrowserTheme LoadCustomFile(string file)
        {
            var d = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(file))
            {
                var eq = line.IndexOf('=');
                if (eq > 0) d[line.Substring(0, eq)] = line.Substring(eq + 1).Trim();
            }
            string G(string k, string def = "#000000") =>
                d.TryGetValue(k, out var v) ? v : def;

            return new BrowserTheme
            {
                Id = G("id"), Name = G("name"), Preview = G("preview", "#4D9EFF"),
                BgWindow = G("BgWindow"), BgTabBar = G("BgTabBar"),
                BgTabActive = G("BgTabActive"), BgTabInact = G("BgTabInact"),
                BgNav = G("BgNav"), BgAddr = G("BgAddr"),
                Accent = G("Accent"), AccentDim = G("AccentDim"),
                Text = G("Text"), TextSub = G("TextSub"), TextDim = G("TextDim"),
                Border = G("Border"), Border2 = G("Border2"),
                BorderAddr = G("BorderAddr"), Sep = G("Sep"),
                Green = G("Green"), Red = G("Red")
            };
        }

        private static void SetBrush(ResourceDictionary res, string key, string hex)
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(hex);
                if (res[key] is SolidColorBrush b) { b.Color = c; return; }
                res[key] = new SolidColorBrush(c);
            }
            catch { }
        }

        private static string Lighten(string hex, int amount)
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(hex);
                return $"#{Math.Min(255, c.R + amount):X2}" +
                       $"{Math.Min(255, c.G + amount):X2}" +
                       $"{Math.Min(255, c.B + amount):X2}";
            }
            catch { return hex; }
        }
    }
}
