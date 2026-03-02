using System;

namespace RuneS.Helpers
{
    public static class UrlHelper
    {
        public static string ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "rune://home";
            input = input.Trim();

            if (input.StartsWith("rune://", StringComparison.OrdinalIgnoreCase)) return input;

            if (Uri.TryCreate(input, UriKind.Absolute, out Uri uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return input;

            if (!input.Contains(" ") && input.Contains(".") &&
                !input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return "https://" + input;

            return AppSettings.SearchEngine + Uri.EscapeDataString(input);
        }

        public static bool IsSecure(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        public static bool IsInternalPage(string url) =>
            string.IsNullOrWhiteSpace(url) ||
            url.Equals("about:blank", StringComparison.OrdinalIgnoreCase) ||
            IsRuneUrl(url);

        public static bool IsRuneUrl(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.StartsWith("rune://", StringComparison.OrdinalIgnoreCase);

        public static bool IsHomePage(string url) =>
            string.IsNullOrWhiteSpace(url) ||
            url.Equals("about:blank",   StringComparison.OrdinalIgnoreCase) ||
            url.Equals("rune://home",   StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://rune/", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("http://rune/",  StringComparison.OrdinalIgnoreCase);

        public static bool IsHistoryPage(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.Equals("rune://history", StringComparison.OrdinalIgnoreCase);

        public static bool IsSettingsPage(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.Equals("rune://settings", StringComparison.OrdinalIgnoreCase);

        public static bool IsDownloadsPage(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.Equals("rune://downloads", StringComparison.OrdinalIgnoreCase);

        public static bool IsThemesPage(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.Equals("rune://themes", StringComparison.OrdinalIgnoreCase);

        public static bool IsBookmarksPage(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.Equals("rune://bookmarks", StringComparison.OrdinalIgnoreCase);

        public static string GetDisplayUrl(string url)
        {
            if (IsHomePage(url)) return string.Empty;
            return url ?? string.Empty;
        }

        public static string GetPageTitle(string url)
        {
            if (IsHomePage(url))      return "New Tab";
            if (IsHistoryPage(url))   return "History — RuneS";
            if (IsSettingsPage(url))  return "Settings — RuneS";
            if (IsDownloadsPage(url)) return "Downloads — RuneS";
            if (IsThemesPage(url))    return "Themes — RuneS";
            if (IsBookmarksPage(url)) return "Bookmarks — RuneS";
            return null;
        }
    }
}
