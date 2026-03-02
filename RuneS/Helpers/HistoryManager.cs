using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RuneS.Helpers
{
    public class HistoryEntry
    {
        public DateTime Time  { get; set; }
        public string   Url   { get; set; }
        public string   Title { get; set; }
    }

    public static class HistoryManager
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RuneS", "history.txt");

        private const int MaxEntries = 10000;

        private static List<HistoryEntry> _cache;
        private static readonly object _lock = new object();

        public static void Add(string url, string title)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (url.StartsWith("rune://", StringComparison.OrdinalIgnoreCase)) return;
            if (url.Equals("about:blank", StringComparison.OrdinalIgnoreCase)) return;

            var entry = new HistoryEntry
            {
                Time  = DateTime.Now,
                Url   = url,
                Title = string.IsNullOrEmpty(title) ? url : title
            };

            lock (_lock)
            {
                Load();
                _cache.Insert(0, entry);
                if (_cache.Count > MaxEntries)
                    _cache = _cache.Take(5000).ToList();
            }

            AppendToFile(entry);
        }

        public static List<HistoryEntry> GetAll()
        {
            lock (_lock) { Load(); return new List<HistoryEntry>(_cache); }
        }

        public static List<HistoryEntry> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return GetAll();
            lock (_lock)
            {
                Load();
                var q = query.ToLowerInvariant();
                return _cache.Where(e =>
                    (e.Url   ?? "").ToLowerInvariant().Contains(q) ||
                    (e.Title ?? "").ToLowerInvariant().Contains(q))
                    .ToList();
            }
        }

        /// <summary>Get unique URLs matching prefix — used for address bar autocomplete.</summary>
        public static List<HistoryEntry> Autocomplete(string prefix, int max = 8)
        {
            if (string.IsNullOrEmpty(prefix)) return new List<HistoryEntry>();
            lock (_lock)
            {
                Load();
                var p = prefix.ToLowerInvariant();
                return _cache
                    .Where(e => !string.IsNullOrEmpty(e.Url) &&
                                (e.Url.ToLowerInvariant().Contains(p) ||
                                 (e.Title ?? "").ToLowerInvariant().Contains(p)))
                    .GroupBy(e => e.Url)
                    .Select(g => g.First())
                    .Take(max)
                    .ToList();
            }
        }

        public static void DeleteEntry(string url, DateTime time)
        {
            lock (_lock)
            {
                Load();
                _cache.RemoveAll(e => e.Url == url &&
                    Math.Abs((e.Time - time).TotalSeconds) < 1);
            }
            Rewrite();
        }

        public static void ClearAll()
        {
            lock (_lock) { _cache = new List<HistoryEntry>(); }
            Rewrite();
        }

        private static void Load()
        {
            if (_cache != null) return;
            _cache = new List<HistoryEntry>();
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var parts = line.Split('\x01');
                    if (parts.Length < 3) continue;
                    if (!long.TryParse(parts[0], out long ticks)) continue;
                    _cache.Add(new HistoryEntry
                    {
                        Time  = new DateTime(ticks),
                        Url   = parts[1],
                        Title = parts[2]
                    });
                }
            }
            catch { }
        }

        private static void AppendToFile(HistoryEntry e)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var line = e.Time.Ticks + "\x01" +
                           (e.Url   ?? "").Replace("\x01", "") + "\x01" +
                           (e.Title ?? "").Replace("\x01", "") + "\n";
                File.AppendAllText(FilePath, line, Encoding.UTF8);
            }
            catch { }
        }

        private static void Rewrite()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var lines = new List<string>();
                lock (_lock)
                {
                    Load();
                    foreach (var e in _cache)
                        lines.Add(e.Time.Ticks + "\x01" +
                                  (e.Url ?? "").Replace("\x01", "") + "\x01" +
                                  (e.Title ?? "").Replace("\x01", ""));
                }
                File.WriteAllLines(FilePath, lines, Encoding.UTF8);
            }
            catch { }
        }
    }
}
