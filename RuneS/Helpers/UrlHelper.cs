using System;

namespace RuneS.Helpers
{
    public static class UrlHelper
    {
        private const string SearchEngine = "https://search.brave.com/search?q=";

        public static string ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "https://search.brave.com";
            input = input.Trim();

            if (Uri.TryCreate(input, UriKind.Absolute, out Uri uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return input;

            if (!input.Contains(" ") && input.Contains(".") &&
                !input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return "https://" + input;

            return SearchEngine + Uri.EscapeDataString(input);
        }

        public static bool IsSecure(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        public static bool IsNewTab(string url) =>
            string.IsNullOrWhiteSpace(url) ||
            url.Equals("about:blank", StringComparison.OrdinalIgnoreCase) ||
            url.Equals("runes://newtab", StringComparison.OrdinalIgnoreCase);
    }
}
