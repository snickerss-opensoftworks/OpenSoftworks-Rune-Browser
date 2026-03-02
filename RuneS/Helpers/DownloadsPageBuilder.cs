using System.Collections.Generic;
using System.Text;

namespace RuneS.Helpers
{
    public static class DownloadsPageBuilder
    {
        public static string Build(IReadOnlyList<DownloadItem> downloads)
        {
            var sb  = new StringBuilder();
            var css = ThemeManager.GetCssVars();

            sb.Append("<!DOCTYPE html><html lang='en'><head>\n");
            sb.Append("<meta charset='UTF-8'><title>Downloads</title>\n");
            sb.Append("<style>\n").Append(css).Append("\n");
            sb.Append("*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}\n");
            sb.Append("html,body{min-height:100%;background:var(--bg);color:var(--text);\n");
            sb.Append("  font-family:'Segoe UI',system-ui,sans-serif;font-size:13px;\n");
            sb.Append("  -webkit-font-smoothing:antialiased}\n");
            sb.Append(".toolbar{position:sticky;top:0;z-index:10;background:var(--bg2);\n");
            sb.Append("  border-bottom:1px solid var(--border);padding:14px 24px;\n");
            sb.Append("  display:flex;align-items:center;gap:14px}\n");
            sb.Append(".toolbar h1{font-size:18px;font-weight:300;flex:1}\n");
            sb.Append(".btn{background:var(--bg4);color:var(--sub);border:1px solid var(--border2);\n");
            sb.Append("  border-radius:6px;padding:7px 14px;font-size:12px;font-family:inherit;\n");
            sb.Append("  cursor:pointer;transition:all .1s}\n");
            sb.Append(".btn:hover{background:var(--border2);color:var(--text)}\n");
            sb.Append(".content{padding:20px 24px;max-width:820px}\n");
            sb.Append(".item{display:flex;align-items:center;gap:14px;\n");
            sb.Append("  background:var(--bg2);border:1px solid var(--border);border-radius:9px;\n");
            sb.Append("  padding:13px 16px;margin-bottom:7px;transition:border-color .1s}\n");
            sb.Append(".item:hover{border-color:var(--border2)}\n");
            sb.Append(".ico{width:36px;height:36px;flex-shrink:0;background:var(--bg4);\n");
            sb.Append("  border:1px solid var(--border2);border-radius:8px;\n");
            sb.Append("  display:flex;align-items:center;justify-content:center;font-size:14px}\n");
            sb.Append(".info{flex:1;min-width:0}\n");
            sb.Append(".fname{font-size:13px;font-weight:500;white-space:nowrap;overflow:hidden;\n");
            sb.Append("  text-overflow:ellipsis;color:var(--text);cursor:pointer}\n");
            sb.Append(".fname:hover{color:var(--accent)}\n");
            sb.Append(".meta{font-size:11px;color:var(--dim);margin-top:2px}\n");
            sb.Append(".progress-wrap{width:100%;height:3px;background:var(--border2);\n");
            sb.Append("  border-radius:2px;margin-top:6px;overflow:hidden}\n");
            sb.Append(".progress-bar{height:100%;background:var(--accent);transition:width .3s}\n");
            sb.Append(".status{flex-shrink:0;font-size:11px;padding:3px 8px;border-radius:4px;font-weight:500}\n");
            sb.Append(".s-done{background:rgba(78,201,78,.12);color:var(--green)}\n");
            sb.Append(".s-prog{background:rgba(77,158,255,.12);color:var(--accent)}\n");
            sb.Append(".s-fail{background:rgba(232,34,58,.12);color:var(--red)}\n");
            sb.Append(".s-canc{background:var(--bg4);color:var(--dim)}\n");
            sb.Append(".actions{display:flex;gap:6px;flex-shrink:0}\n");
            sb.Append(".act{background:transparent;border:none;cursor:pointer;\n");
            sb.Append("  color:var(--dim);padding:4px 7px;border-radius:5px;\n");
            sb.Append("  font-size:12px;transition:all .1s}\n");
            sb.Append(".act:hover{background:var(--bg3);color:var(--text)}\n");
            sb.Append(".empty{text-align:center;padding:60px 20px;color:var(--dim)}\n");
            sb.Append(".empty p{font-size:14px;color:var(--sub);margin-bottom:6px}\n");
            sb.Append("</style></head><body>\n");

            sb.Append("<div class='toolbar'>\n");
            sb.Append("  <h1>Downloads</h1>\n");
            sb.Append("  <button class='btn' onclick='send(\"openDownloadsFolder\",\"\")'>Open Folder</button>\n");
            sb.Append("  <button class='btn' onclick='send(\"clearDownloads\",\"\")'>Clear Completed</button>\n");
            sb.Append("</div>\n");
            sb.Append("<div class='content'>\n");

            if (downloads == null || downloads.Count == 0)
            {
                sb.Append("<div class='empty'><p>No downloads yet</p><small>Files you download will appear here</small></div>\n");
            }
            else
            {
                foreach (var d in downloads)
                {
                    var fname = d.FileName.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
                    var fpath = d.FilePath.Replace("\\", "\\\\").Replace("'", "\\'");
                    var src   = (d.SourceUrl ?? "").Replace("\\", "\\\\").Replace("'", "\\'");
                    var ext   = System.IO.Path.GetExtension(d.FileName).ToLower().TrimStart('.');
                    var emoji = ext switch
                    {
                        "pdf"  => "📄", "zip" or "rar" or "7z" => "📦",
                        "mp3" or "wav" or "flac" => "🎵",
                        "mp4" or "mkv" or "avi"  => "🎬",
                        "jpg" or "jpeg" or "png" or "gif" or "webp" => "🖼",
                        "exe" or "msi" => "⚙", "txt" or "md" => "📝",
                        _ => "📁"
                    };
                    var statusClass = d.Status switch
                    {
                        DownloadStatus.Completed  => "s-done",
                        DownloadStatus.InProgress => "s-prog",
                        DownloadStatus.Failed     => "s-fail",
                        _                         => "s-canc"
                    };
                    var statusText = d.Status switch
                    {
                        DownloadStatus.Completed  => "Done",
                        DownloadStatus.InProgress => d.Progress + "%",
                        DownloadStatus.Failed     => "Failed",
                        _                         => "Cancelled"
                    };

                    sb.Append("<div class='item' id='dl-").Append(d.Id).Append("'>\n");
                    sb.Append("  <div class='ico'>").Append(emoji).Append("</div>\n");
                    sb.Append("  <div class='info'>\n");
                    sb.Append("    <div class='fname' onclick='open(\"").Append(fpath).Append("\")'>\n");
                    sb.Append("      ").Append(fname).Append("\n    </div>\n");
                    sb.Append("    <div class='meta'>").Append(d.SizeLabel);
                    sb.Append(" &middot; ").Append(d.StartTime.ToString("MMM d, h:mm tt")).Append("</div>\n");
                    if (d.Status == DownloadStatus.InProgress)
                    {
                        sb.Append("    <div class='progress-wrap'><div class='progress-bar' style='width:").Append(d.Progress).Append("%'></div></div>\n");
                    }
                    sb.Append("  </div>\n");
                    sb.Append("  <div class='status ").Append(statusClass).Append("'>").Append(statusText).Append("</div>\n");
                    sb.Append("  <div class='actions'>\n");
                    sb.Append("    <button class='act' onclick='openFile(\"").Append(fpath).Append("\")' title='Open'>&#x1F4C2;</button>\n");
                    sb.Append("    <button class='act' onclick='delDl(\"").Append(d.Id).Append("\")' title='Remove'>&#x2715;</button>\n");
                    sb.Append("  </div>\n");
                    sb.Append("</div>\n");
                }
            }

            sb.Append("</div>\n");
            sb.Append("<script>\n");
            sb.Append("function send(t,v){window.chrome.webview.postMessage(JSON.stringify({type:t,value:v}));}\n");
            sb.Append("function openFile(p){send('openFile',p);}\n");
            sb.Append("function delDl(id){send('removeDl',id);var el=document.getElementById('dl-'+id);if(el)el.remove();}\n");
            sb.Append("</script></body></html>\n");

            return sb.ToString();
        }
    }
}
