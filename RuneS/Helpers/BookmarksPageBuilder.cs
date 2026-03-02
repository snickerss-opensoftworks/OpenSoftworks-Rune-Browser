using System;
using System.Collections.Generic;
using System.Text;

namespace RuneS.Helpers
{
    public static class BookmarksPageBuilder
    {
        public static string Build(List<(string Title, string Url)> bookmarks)
        {
            var sb  = new StringBuilder();
            var css = ThemeManager.GetCssVars();

            sb.Append("<!DOCTYPE html><html lang='en'><head>\n");
            sb.Append("<meta charset='UTF-8'><title>Bookmarks</title>\n");
            sb.Append("<style>\n").Append(css).Append("\n");
            sb.Append("*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}\n");
            sb.Append("html,body{min-height:100%;background:var(--bg);color:var(--text);\n");
            sb.Append("  font-family:'Segoe UI',system-ui,sans-serif;font-size:13px;\n");
            sb.Append("  -webkit-font-smoothing:antialiased}\n");
            sb.Append(".toolbar{position:sticky;top:0;z-index:10;background:var(--bg2);\n");
            sb.Append("  border-bottom:1px solid var(--border);padding:14px 24px;\n");
            sb.Append("  display:flex;align-items:center;gap:14px}\n");
            sb.Append(".toolbar h1{font-size:18px;font-weight:300;flex:1}\n");
            sb.Append(".search-wrap{position:relative;max-width:340px;flex:1}\n");
            sb.Append(".search-wrap svg{position:absolute;left:9px;top:50%;transform:translateY(-50%);\n");
            sb.Append("  width:13px;height:13px;stroke:var(--dim);fill:none;pointer-events:none}\n");
            sb.Append("#search{width:100%;background:var(--bg4);color:var(--text);\n");
            sb.Append("  border:1px solid var(--border2);border-radius:6px;\n");
            sb.Append("  padding:7px 10px 7px 30px;font-size:12px;font-family:inherit;outline:none}\n");
            sb.Append("#search:focus{border-color:var(--accent)}\n");
            sb.Append(".content{padding:20px 24px;max-width:860px}\n");
            sb.Append(".grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:8px}\n");
            sb.Append(".card{background:var(--bg2);border:1px solid var(--border);border-radius:9px;\n");
            sb.Append("  padding:13px 14px;cursor:pointer;transition:background .1s,border-color .1s;\n");
            sb.Append("  display:flex;align-items:center;gap:10px;group:true;position:relative}\n");
            sb.Append(".card:hover{background:var(--bg3);border-color:var(--border2)}\n");
            sb.Append(".card:active{transform:scale(.98)}\n");
            sb.Append(".fav{width:20px;height:20px;flex-shrink:0;border-radius:4px;\n");
            sb.Append("  background:var(--bg4);display:flex;align-items:center;justify-content:center;\n");
            sb.Append("  font-size:10px;font-weight:600;color:var(--accent);overflow:hidden}\n");
            sb.Append(".fav img{width:100%;height:100%;object-fit:cover}\n");
            sb.Append(".info{flex:1;min-width:0}\n");
            sb.Append(".title{font-size:12.5px;font-weight:500;white-space:nowrap;\n");
            sb.Append("  overflow:hidden;text-overflow:ellipsis;color:var(--text)}\n");
            sb.Append(".url{font-size:10.5px;color:var(--dim);white-space:nowrap;\n");
            sb.Append("  overflow:hidden;text-overflow:ellipsis;margin-top:1px}\n");
            sb.Append(".del{position:absolute;top:6px;right:6px;opacity:0;\n");
            sb.Append("  background:transparent;border:none;cursor:pointer;\n");
            sb.Append("  color:var(--dim);font-size:13px;line-height:1;\n");
            sb.Append("  padding:2px 5px;border-radius:4px;transition:all .1s}\n");
            sb.Append(".card:hover .del{opacity:1}\n");
            sb.Append(".del:hover{background:var(--bg);color:var(--red)}\n");
            sb.Append(".empty{text-align:center;padding:60px 20px;color:var(--dim)}\n");
            sb.Append(".empty p{font-size:14px;color:var(--sub);margin-bottom:6px}\n");
            sb.Append(".count{font-size:11px;color:var(--dim)}\n");
            sb.Append("</style></head><body>\n");

            sb.Append("<div class='toolbar'>\n");
            sb.Append("  <h1>Bookmarks</h1>\n");
            sb.Append("  <span class='count' id='count'></span>\n");
            sb.Append("  <div class='search-wrap'>\n");
            sb.Append("    <svg viewBox='0 0 24 24' stroke-width='2' stroke-linecap='round'><circle cx='11' cy='11' r='8'/><path d='M21 21l-4.35-4.35'/></svg>\n");
            sb.Append("    <input id='search' placeholder='Search bookmarks...' oninput='filter(this.value)'>\n");
            sb.Append("  </div>\n");
            sb.Append("</div>\n");
            sb.Append("<div class='content'><div class='grid' id='grid'></div></div>\n");

            // Build JSON data
            var jsonSb = new StringBuilder("[");
            for (int i = 0; i < bookmarks.Count; i++)
            {
                var b = bookmarks[i];
                var t = b.Title.Replace("\\", "\\\\").Replace("\"", "\\\"");
                var u = b.Url.Replace("\\", "\\\\").Replace("\"", "\\\"");
                if (i > 0) jsonSb.Append(",");
                string host = "";
                try { host = new Uri(b.Url).Host.Replace("www.", ""); } catch { host = b.Title; }
                var letter = host.Length > 0 ? host.Substring(0, 1).ToUpper() : "?";
                jsonSb.Append("{\"t\":\"").Append(t)
                      .Append("\",\"u\":\"").Append(u)
                      .Append("\",\"l\":\"").Append(letter)
                      .Append("\"}");
            }
            jsonSb.Append("]");

            sb.Append("<script>\n");
            sb.Append("var ALL=").Append(jsonSb).Append(";\n");
            sb.Append("function esc(s){return(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/\"/g,'&quot;');}\n");
            sb.Append("function favUrl(u){try{return new URL(u).origin+'/favicon.ico';}catch(e){return '';}}\n");
            sb.Append("function render(data){\n");
            sb.Append("  var g=document.getElementById('grid');\n");
            sb.Append("  document.getElementById('count').textContent=data.length+' bookmark'+(data.length===1?'':'s');\n");
            sb.Append("  if(!data.length){g.innerHTML='<div class=\"empty\"><p>No bookmarks yet</p><small>Star pages with Ctrl+D</small></div>';return;}\n");
            sb.Append("  g.innerHTML=data.map(function(b,i){\n");
            sb.Append("    return '<div class=\"card\" onclick=\"nav(\\''+b.u.replace(/\\'/g,\"\\\\\\'\")+'\\')\">'+ \n");
            sb.Append("      '<div class=\"fav\"><img src=\"'+favUrl(b.u)+'\" onerror=\"this.style.display=\\'none\\'\" loading=\"lazy\">'+b.l+'</div>'+\n");
            sb.Append("      '<div class=\"info\"><div class=\"title\">'+esc(b.t)+'</div><div class=\"url\">'+esc(b.u)+'</div></div>'+\n");
            sb.Append("      '<button class=\"del\" onclick=\"event.stopPropagation();del(\\''+b.u.replace(/\\'/g,\"\\\\\\'\")+'\\')\" title=\"Remove\">&#x2715;</button>'+\n");
            sb.Append("      '</div>';\n");
            sb.Append("  }).join('');\n");
            sb.Append("}\n");
            sb.Append("function filter(q){\n");
            sb.Append("  if(!q){render(ALL);return;}\n");
            sb.Append("  q=q.toLowerCase();\n");
            sb.Append("  render(ALL.filter(function(b){return b.t.toLowerCase().includes(q)||b.u.toLowerCase().includes(q);}));\n");
            sb.Append("}\n");
            sb.Append("function nav(url){window.chrome.webview.postMessage(JSON.stringify({type:'navigate',value:url}));}\n");
            sb.Append("function del(url){window.chrome.webview.postMessage(JSON.stringify({type:'bookmarkDelete',value:url}));}\n");
            sb.Append("render(ALL);\n");
            sb.Append("</script></body></html>\n");

            return sb.ToString();
        }
    }
}
