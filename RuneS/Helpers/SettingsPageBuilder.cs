using System.Text;

namespace RuneS.Helpers
{
    public static class SettingsPageBuilder
    {
        public static string Build(string settingsJson)
        {
            var sb  = new StringBuilder();
            var css = ThemeManager.GetCssVars();

            sb.Append("<!DOCTYPE html><html lang='en'><head>\n");
            sb.Append("<meta charset='UTF-8'>\n");
            sb.Append("<meta name='viewport' content='width=device-width,initial-scale=1'>\n");
            sb.Append("<title>Settings</title>\n");
            sb.Append("<style>\n");
            sb.Append(css).Append("\n");
            sb.Append("*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}\n");
            sb.Append("html,body{height:100%;background:var(--bg);color:var(--text);\n");
            sb.Append("  font-family:'Segoe UI',system-ui,sans-serif;font-size:13px;\n");
            sb.Append("  -webkit-font-smoothing:antialiased;overflow-x:hidden}\n");
            sb.Append("body{display:flex;min-height:100%}\n");
            sb.Append(".sidebar{width:200px;background:var(--bg2);border-right:1px solid var(--border);\n");
            sb.Append("  flex-shrink:0;padding:18px 0;position:sticky;top:0;height:100vh;overflow-y:auto}\n");
            sb.Append(".brand{display:flex;align-items:center;gap:9px;padding:0 16px 18px;\n");
            sb.Append("  border-bottom:1px solid var(--border);margin-bottom:8px}\n");
            sb.Append(".logo{width:24px;height:24px;background:var(--bg4);border:1px solid var(--border2);\n");
            sb.Append("  border-radius:6px;display:flex;align-items:center;justify-content:center;\n");
            sb.Append("  font-size:12px;font-weight:700;color:var(--accent)}\n");
            sb.Append(".brand-name{font-size:13px;font-weight:600}\n");
            sb.Append(".ns{padding:3px 10px 2px;font-size:10px;text-transform:uppercase;\n");
            sb.Append("  letter-spacing:.1em;color:var(--dim);margin-top:6px}\n");
            sb.Append(".ni{display:flex;align-items:center;gap:9px;padding:8px 16px;\n");
            sb.Append("  cursor:pointer;color:var(--sub);font-size:12px;transition:all .1s}\n");
            sb.Append(".ni:hover{background:var(--bg3);color:var(--text)}\n");
            sb.Append(".ni.active{background:rgba(77,158,255,.1);color:var(--accent);font-weight:500}\n");
            sb.Append(".content{flex:1;padding:26px 32px;max-width:640px;overflow-y:auto}\n");
            sb.Append(".page{display:none}.page.active{display:block}\n");
            sb.Append(".pg-title{font-size:20px;font-weight:300;margin-bottom:4px}\n");
            sb.Append(".pg-sub{font-size:11.5px;color:var(--dim);margin-bottom:24px}\n");
            sb.Append(".grp{margin-bottom:20px}\n");
            sb.Append(".gl{font-size:10px;text-transform:uppercase;letter-spacing:.1em;\n");
            sb.Append("  color:var(--dim);margin-bottom:8px}\n");
            sb.Append(".s{display:flex;align-items:center;justify-content:space-between;\n");
            sb.Append("  background:var(--bg3);border:1px solid var(--border);border-radius:7px;\n");
            sb.Append("  padding:12px 14px;margin-bottom:5px}\n");
            sb.Append(".si{flex:1}\n");
            sb.Append(".st{font-size:12.5px;margin-bottom:2px}\n");
            sb.Append(".sd{font-size:11px;color:var(--dim);line-height:1.4}\n");
            // Toggle
            sb.Append(".tgl{position:relative;width:36px;height:20px;flex-shrink:0;cursor:pointer}\n");
            sb.Append(".tgl input{opacity:0;width:0;height:0;position:absolute}\n");
            sb.Append(".tk{position:absolute;inset:0;background:var(--border2);border-radius:10px;transition:background .2s}\n");
            sb.Append(".tgl input:checked+.tk{background:var(--accent)}\n");
            sb.Append(".tt{position:absolute;top:3px;left:3px;width:14px;height:14px;\n");
            sb.Append("  background:white;border-radius:50%;transition:transform .2s;box-shadow:0 1px 3px rgba(0,0,0,.4)}\n");
            sb.Append(".tgl input:checked~.tt{transform:translateX(16px)}\n");
            // Select
            sb.Append("select{background:var(--bg4);color:var(--text);border:1px solid var(--border2);\n");
            sb.Append("  border-radius:5px;padding:6px 24px 6px 9px;font-size:12px;font-family:inherit;\n");
            sb.Append("  cursor:pointer;outline:none;-webkit-appearance:none}\n");
            sb.Append("select:focus{border-color:var(--accent)}\n");
            // Input
            sb.Append("input[type=text]{background:var(--bg4);color:var(--text);\n");
            sb.Append("  border:1px solid var(--border2);border-radius:5px;\n");
            sb.Append("  padding:6px 9px;font-size:12px;font-family:inherit;outline:none;width:220px}\n");
            sb.Append("input[type=text]:focus{border-color:var(--accent)}\n");
            // Range
            sb.Append("input[type=range]{-webkit-appearance:none;width:140px;height:3px;\n");
            sb.Append("  background:var(--border2);border-radius:2px;outline:none;cursor:pointer}\n");
            sb.Append("input[type=range]::-webkit-slider-thumb{-webkit-appearance:none;\n");
            sb.Append("  width:13px;height:13px;border-radius:50%;background:var(--accent);cursor:pointer}\n");
            // Buttons
            sb.Append(".btn{background:var(--bg4);color:var(--sub);border:1px solid var(--border2);\n");
            sb.Append("  border-radius:5px;padding:6px 14px;font-size:12px;font-family:inherit;\n");
            sb.Append("  cursor:pointer;transition:all .1s}\n");
            sb.Append(".btn:hover{background:var(--border2);color:var(--text)}\n");
            sb.Append(".btn-danger{color:var(--red);border-color:rgba(232,34,58,.25)}\n");
            sb.Append(".btn-danger:hover{background:rgba(232,34,58,.1);border-color:var(--red)}\n");
            sb.Append(".btn-accent{background:var(--accent);color:white;border-color:transparent}\n");
            sb.Append(".btn-accent:hover{opacity:.88}\n");
            // About
            sb.Append(".about-card{background:var(--bg3);border:1px solid var(--border);\n");
            sb.Append("  border-radius:10px;padding:24px;text-align:center}\n");
            sb.Append(".alogo{width:56px;height:56px;background:var(--bg4);border:1px solid var(--border2);\n");
            sb.Append("  border-radius:14px;display:flex;align-items:center;justify-content:center;\n");
            sb.Append("  font-size:24px;font-weight:700;color:var(--accent);margin:0 auto 12px}\n");
            sb.Append(".about-card h2{font-size:18px;font-weight:300}\n");
            sb.Append(".about-card p{color:var(--dim);font-size:11.5px;line-height:1.7;margin-top:6px}\n");
            sb.Append(".stat-row{display:flex;gap:8px;justify-content:center;margin-top:14px;flex-wrap:wrap}\n");
            sb.Append(".stat{background:var(--bg4);border:1px solid var(--border);\n");
            sb.Append("  border-radius:6px;padding:8px 16px;text-align:center}\n");
            sb.Append(".stat-val{font-size:16px;font-weight:600;color:var(--accent)}\n");
            sb.Append(".stat-lbl{font-size:10px;color:var(--dim)}\n");
            sb.Append("</style></head><body>\n");

            // Sidebar
            sb.Append("<nav class='sidebar'>\n");
            sb.Append("  <div class='brand'><div class='logo'>R</div>\n");
            sb.Append("  <div class='brand-name'>RuneS</div></div>\n");
            sb.Append("  <div class='ns'>Browser</div>\n");
            sb.Append("  <div class='ni active' onclick='nav(\"search\",this)'>Search</div>\n");
            sb.Append("  <div class='ni' onclick='nav(\"appearance\",this)'>Appearance</div>\n");
            sb.Append("  <div class='ni' onclick='nav(\"startup\",this)'>Startup</div>\n");
            sb.Append("  <div class='ni' onclick='nav(\"downloads\",this)'>Downloads</div>\n");
            sb.Append("  <div class='ns'>Privacy</div>\n");
            sb.Append("  <div class='ni' onclick='nav(\"privacy\",this)'>Privacy &amp; Security</div>\n");
            sb.Append("  <div class='ni' onclick='nav(\"performance\",this)'>Performance</div>\n");
            sb.Append("  <div class='ns'>System</div>\n");
            sb.Append("  <div class='ni' onclick='nav(\"about\",this)'>About</div>\n");
            sb.Append("</nav>\n");

            sb.Append("<div class='content'>\n");

            // Search page
            sb.Append("<div class='page active' id='pg-search'>\n");
            sb.Append("<div class='pg-title'>Search</div>\n");
            sb.Append("<div class='pg-sub'>Choose your default search engine and address bar behaviour.</div>\n");
            sb.Append("<div class='grp'><div class='gl'>Default Search Engine</div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Search engine</div>\n");
            sb.Append("<div class='sd'>Used when typing in the address bar</div></div>\n");
            sb.Append("<select id='sel-se' onchange='apply(\"searchEngine\",this.value)'>\n");
            sb.Append("  <option value='https://search.brave.com/search?q='>Brave Search</option>\n");
            sb.Append("  <option value='https://www.google.com/search?q='>Google</option>\n");
            sb.Append("  <option value='https://duckduckgo.com/?q='>DuckDuckGo</option>\n");
            sb.Append("  <option value='https://www.bing.com/search?q='>Bing</option>\n");
            sb.Append("  <option value='https://search.yahoo.com/search?p='>Yahoo</option>\n");
            sb.Append("  <option value='https://www.ecosia.org/search?q='>Ecosia</option>\n");
            sb.Append("</select></div></div>\n");
            sb.Append("</div>\n");

            // Appearance page
            sb.Append("<div class='page' id='pg-appearance'>\n");
            sb.Append("<div class='pg-title'>Appearance</div>\n");
            sb.Append("<div class='pg-sub'>Customise the look and feel of the browser.</div>\n");
            sb.Append("<div class='grp'><div class='gl'>Interface</div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Show bookmarks bar</div>\n");
            sb.Append("<div class='sd'>Display pinned bookmarks below the address bar</div></div>\n");
            sb.Append("<label class='tgl'><input type='checkbox' id='chk-bm' onchange='apply(\"showBookmarksBar\",this.checked?\"true\":\"false\")'>\n");
            sb.Append("<div class='tk'></div><div class='tt'></div></label></div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Default zoom level</div>\n");
            sb.Append("<div class='sd'>Page zoom applied to new tabs</div></div>\n");
            sb.Append("<div style='display:flex;align-items:center;gap:9px'>\n");
            sb.Append("<input type='range' id='zoom-r' min='50' max='200' step='10' oninput='setZoom(this.value)'>\n");
            sb.Append("<span id='zoom-v' style='color:var(--accent);font-size:12px;width:34px'>100%</span>\n");
            sb.Append("</div></div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Themes</div>\n");
            sb.Append("<div class='sd'>Customise the browser colour scheme</div></div>\n");
            sb.Append("<button class='btn btn-accent' onclick='send(\"openThemes\",\"\")'>Open Themes</button></div>\n");
            sb.Append("</div></div>\n");

            // Startup page
            sb.Append("<div class='page' id='pg-startup'>\n");
            sb.Append("<div class='pg-title'>Startup</div>\n");
            sb.Append("<div class='pg-sub'>Control what happens when RuneS starts.</div>\n");
            sb.Append("<div class='grp'><div class='gl'>On Startup</div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Restore last session</div>\n");
            sb.Append("<div class='sd'>Reopen tabs from your previous session on launch</div></div>\n");
            sb.Append("<label class='tgl'><input type='checkbox' id='chk-sess' onchange='apply(\"restoreLastSession\",this.checked?\"true\":\"false\")'>\n");
            sb.Append("<div class='tk'></div><div class='tt'></div></label></div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Startup page</div>\n");
            sb.Append("<div class='sd'>URL opened in new tab on launch</div></div>\n");
            sb.Append("<input type='text' id='inp-startup' value='' onchange='apply(\"startupPage\",this.value)' placeholder='rune://home'>\n");
            sb.Append("</div></div></div>\n");

            // Downloads page
            sb.Append("<div class='page' id='pg-downloads'>\n");
            sb.Append("<div class='pg-title'>Downloads</div>\n");
            sb.Append("<div class='pg-sub'>Control where and how files are saved.</div>\n");
            sb.Append("<div class='grp'><div class='gl'>Save Location</div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Downloads folder</div>\n");
            sb.Append("<div class='sd' id='dl-path'>Loading...</div></div>\n");
            sb.Append("<button class='btn' onclick='send(\"openDownloads\",\"\")'>Open Folder</button></div>\n");
            sb.Append("</div></div>\n");

            // Privacy page
            sb.Append("<div class='page' id='pg-privacy'>\n");
            sb.Append("<div class='pg-title'>Privacy &amp; Security</div>\n");
            sb.Append("<div class='pg-sub'>Manage tracking, cookies, and data.</div>\n");
            sb.Append("<div class='grp'><div class='gl'>Tracking</div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Do Not Track</div>\n");
            sb.Append("<div class='sd'>Send a DNT header with every request</div></div>\n");
            sb.Append("<label class='tgl'><input type='checkbox' id='chk-dnt' onchange='apply(\"doNotTrack\",this.checked?\"true\":\"false\")'>\n");
            sb.Append("<div class='tk'></div><div class='tt'></div></label></div>\n");
            sb.Append("</div>\n");
            sb.Append("<div class='grp'><div class='gl'>Browsing Data</div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Clear history</div>\n");
            sb.Append("<div class='sd'>Removes all browsing history entries</div></div>\n");
            sb.Append("<button class='btn btn-danger' onclick='clearHistory()'>Clear History</button></div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Clear cache &amp; cookies</div>\n");
            sb.Append("<div class='sd'>Removes cached files and site cookies</div></div>\n");
            sb.Append("<button class='btn btn-danger' onclick='clearData()'>Clear Data</button></div>\n");
            sb.Append("</div></div>\n");

            // Performance page
            sb.Append("<div class='page' id='pg-performance'>\n");
            sb.Append("<div class='pg-title'>Performance</div>\n");
            sb.Append("<div class='pg-sub'>Optimise memory and CPU usage.</div>\n");
            sb.Append("<div class='grp'><div class='gl'>Memory Saver</div>\n");
            sb.Append("<div class='s'><div class='si'><div class='st'>Suspend background tabs</div>\n");
            sb.Append("<div class='sd'>Freeze inactive tabs after 60 seconds to save RAM. Resumes instantly when clicked.</div></div>\n");
            sb.Append("<label class='tgl'><input type='checkbox' id='chk-sus' onchange='apply(\"suspendBackgroundTabs\",this.checked?\"true\":\"false\")'>\n");
            sb.Append("<div class='tk'></div><div class='tt'></div></label></div>\n");
            sb.Append("</div></div>\n");

            // About page
            sb.Append("<div class='page' id='pg-about'>\n");
            sb.Append("<div class='pg-title'>About RuneS</div>\n");
            sb.Append("<div class='pg-sub'>Browser information and version details.</div>\n");
            sb.Append("<div class='about-card'>\n");
            sb.Append("<div class='alogo'>R</div>\n");
            sb.Append("<h2>RuneS Browser</h2>\n");
            sb.Append("<p>Version 1.0.0 &middot; Built with WPF + Chromium WebView2</p>\n");
            sb.Append("<div class='stat-row'>\n");
            sb.Append("  <div class='stat'><div class='stat-val' id='s-hist'>—</div><div class='stat-lbl'>History Items</div></div>\n");
            sb.Append("  <div class='stat'><div class='stat-val' id='s-bm'>—</div><div class='stat-lbl'>Bookmarks</div></div>\n");
            sb.Append("  <div class='stat'><div class='stat-val' id='s-dl'>—</div><div class='stat-lbl'>Downloads</div></div>\n");
            sb.Append("</div>\n");
            sb.Append("<div style='display:flex;gap:10px;justify-content:center;margin-top:20px'>\n");
            sb.Append("  <button class='btn' onclick='send(\"navigate\",\"rune://history\")'>View History</button>\n");
            sb.Append("  <button class='btn' onclick='send(\"navigate\",\"rune://downloads\")'>Downloads</button>\n");
            sb.Append("  <button class='btn' onclick='send(\"navigate\",\"rune://themes\")'>Themes</button>\n");
            sb.Append("</div></div></div>\n");

            sb.Append("</div>\n"); // close .content

            // JavaScript
            sb.Append("<script>\n");
            sb.Append("var S="); sb.Append(settingsJson); sb.Append(";\n");
            sb.Append("function nav(id,el){\n");
            sb.Append("  document.querySelectorAll('.page').forEach(function(p){p.classList.remove('active');});\n");
            sb.Append("  document.querySelectorAll('.ni').forEach(function(n){n.classList.remove('active');});\n");
            sb.Append("  document.getElementById('pg-'+id).classList.add('active');\n");
            sb.Append("  el.classList.add('active');\n");
            sb.Append("}\n");
            sb.Append("function apply(k,v){S[k]=v;send('setting',k+'='+v);}\n");
            sb.Append("function send(t,v){window.chrome.webview.postMessage(JSON.stringify({type:t,value:v}));}\n");
            sb.Append("function clearData(){if(confirm('Clear cache and cookies?'))send('clearData','');}\n");
            sb.Append("function clearHistory(){if(confirm('Clear all history?'))send('historyClear','');}\n");
            sb.Append("function setZoom(v){document.getElementById('zoom-v').textContent=v+'%';apply('defaultZoom',(v/100).toFixed(2));}\n");
            sb.Append("function syncUI(){\n");
            sb.Append("  var s=document.getElementById('sel-se');\n");
            sb.Append("  for(var i=0;i<s.options.length;i++){if(s.options[i].value===S.searchEngine){s.selectedIndex=i;break;}}\n");
            sb.Append("  document.getElementById('chk-bm').checked=S.showBookmarksBar;\n");
            sb.Append("  var z=Math.round((S.defaultZoom||1)*100);\n");
            sb.Append("  document.getElementById('zoom-r').value=z;\n");
            sb.Append("  document.getElementById('zoom-v').textContent=z+'%';\n");
            sb.Append("  var dlEl=document.getElementById('dl-path');\n");
            sb.Append("  if(dlEl)dlEl.textContent=S.downloadsFolder||'—';\n");
            sb.Append("  document.getElementById('chk-dnt').checked=S.doNotTrack;\n");
            sb.Append("  document.getElementById('chk-sus').checked=S.suspendBackgroundTabs;\n");
            sb.Append("  document.getElementById('chk-sess').checked=S.restoreLastSession;\n");
            sb.Append("  var sp=document.getElementById('inp-startup');\n");
            sb.Append("  if(sp)sp.value=S.startupPage||'rune://home';\n");
            sb.Append("}\n");
            sb.Append("syncUI();\n");
            sb.Append("// Request stats\n");
            sb.Append("send('getStats','');\n");
            sb.Append("window.chrome.webview.addEventListener('message',function(e){\n");
            sb.Append("  try{\n");
            sb.Append("    var m=JSON.parse(e.data);\n");
            sb.Append("    if(m.type==='stats'){\n");
            sb.Append("      document.getElementById('s-hist').textContent=m.history||0;\n");
            sb.Append("      document.getElementById('s-bm').textContent=m.bookmarks||0;\n");
            sb.Append("      document.getElementById('s-dl').textContent=m.downloads||0;\n");
            sb.Append("    }\n");
            sb.Append("  }catch(ex){}\n");
            sb.Append("});\n");
            sb.Append("</script></body></html>\n");

            return sb.ToString();
        }
    }
}
