using System;
using System.Collections.Generic;
using System.IO;

namespace RuneS.Helpers
{
    public class SessionEntry
    {
        public string Url   { get; set; }
        public string Title { get; set; }
        public bool   Active { get; set; }
    }

    public static class SessionManager
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RuneS", "session.txt");

        public static void Save(IEnumerable<SessionEntry> tabs)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var lines = new List<string>();
                foreach (var t in tabs)
                {
                    if (string.IsNullOrEmpty(t.Url) ||
                        t.Url.Equals("rune://home", StringComparison.OrdinalIgnoreCase))
                        continue;
                    lines.Add((t.Active ? "1" : "0") + "\x01" +
                               t.Url.Replace("\x01", "") + "\x01" +
                               (t.Title ?? "").Replace("\x01", ""));
                }
                File.WriteAllLines(FilePath, lines);
            }
            catch { }
        }

        public static List<SessionEntry> Load()
        {
            var list = new List<SessionEntry>();
            try
            {
                if (!File.Exists(FilePath)) return list;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var p = line.Split('\x01');
                    if (p.Length < 2) continue;
                    list.Add(new SessionEntry
                    {
                        Active = p[0] == "1",
                        Url    = p[1],
                        Title  = p.Length > 2 ? p[2] : p[1]
                    });
                }
            }
            catch { }
            return list;
        }

        public static bool HasSession() =>
            File.Exists(FilePath) && new FileInfo(FilePath).Length > 0;

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }
    }
}
