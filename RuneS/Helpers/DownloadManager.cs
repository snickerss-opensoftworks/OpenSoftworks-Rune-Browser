using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RuneS.Helpers
{
    public enum DownloadStatus { InProgress, Completed, Cancelled, Failed }

    public class DownloadItem
    {
        public string         Id        { get; set; } = Guid.NewGuid().ToString("N");
        public string         FileName  { get; set; }
        public string         FilePath  { get; set; }
        public string         SourceUrl { get; set; }
        public DateTime       StartTime { get; set; }
        public long           TotalBytes   { get; set; }
        public long           ReceivedBytes { get; set; }
        public DownloadStatus Status    { get; set; } = DownloadStatus.InProgress;

        public int Progress => TotalBytes > 0
            ? (int)Math.Min(100, ReceivedBytes * 100 / TotalBytes) : 0;

        public string SizeLabel
        {
            get
            {
                var b = TotalBytes > 0 ? TotalBytes : ReceivedBytes;
                if (b == 0) return "—";
                if (b < 1024)      return b + " B";
                if (b < 1048576)   return (b / 1024.0).ToString("F1") + " KB";
                return             (b / 1048576.0).ToString("F1") + " MB";
            }
        }
    }

    public static class DownloadManager
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RuneS", "downloads.txt");

        private static readonly List<DownloadItem> _items = new List<DownloadItem>();
        private static bool _loaded;

        public static IReadOnlyList<DownloadItem> All
        {
            get { Load(); return _items; }
        }

        public static DownloadItem Add(string fileName, string filePath, string sourceUrl)
        {
            var item = new DownloadItem
            {
                FileName  = fileName,
                FilePath  = filePath,
                SourceUrl = sourceUrl,
                StartTime = DateTime.Now
            };
            lock (_items) { Load(); _items.Insert(0, item); }
            return item;
        }

        public static void Complete(DownloadItem item)
        {
            item.Status = DownloadStatus.Completed;
            Save();
        }

        public static void Cancel(DownloadItem item)
        {
            item.Status = DownloadStatus.Cancelled;
            Save();
        }

        public static void Remove(string id)
        {
            lock (_items) { _items.RemoveAll(i => i.Id == id); }
            Save();
        }

        public static void ClearCompleted()
        {
            lock (_items) { _items.RemoveAll(i => i.Status != DownloadStatus.InProgress); }
            Save();
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var p = line.Split('\x01');
                    if (p.Length < 6) continue;
                    _items.Add(new DownloadItem
                    {
                        Id        = p[0],
                        FileName  = p[1],
                        FilePath  = p[2],
                        SourceUrl = p[3],
                        StartTime = new DateTime(long.TryParse(p[4], out long t) ? t : 0),
                        TotalBytes = long.TryParse(p[5], out long sz) ? sz : 0,
                        Status    = (DownloadStatus)(int.TryParse(p[6], out int s) ? s : 1)
                    });
                }
            }
            catch { }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var lines = new List<string>();
                lock (_items)
                {
                    foreach (var i in _items.Take(500))
                        lines.Add(string.Join("\x01", i.Id, i.FileName, i.FilePath,
                                  i.SourceUrl, i.StartTime.Ticks,
                                  i.TotalBytes, (int)i.Status));
                }
                File.WriteAllLines(FilePath, lines);
            }
            catch { }
        }
    }
}
