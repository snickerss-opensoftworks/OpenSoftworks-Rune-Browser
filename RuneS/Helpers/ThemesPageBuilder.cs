using System.Collections.Generic;
using System.Text;

namespace RuneS.Helpers
{
    public static class ThemesPageBuilder
    {
        public static string Build(string currentId, List<BrowserTheme> custom)
        {
            var sb  = new StringBuilder();
            var css = ThemeManager.GetCssVars();

            sb.Append("<!DOCTYPE html><html lang='en'><head>\n");
            sb.Append("<meta charset='UTF-8'>\n");
            sb.Append("<title>Themes</title>\n");
            sb.Append("<style>\n");
            sb.Append(css).Append("\n");
            sb.Append("*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}\n");
            sb.Append("html,body{background:var(--bg);color:var(--text);\n");
            sb.Append("  font-family:'Segoe UI',system-ui,sans-serif;font-size:13px;\n");
            sb.Append("  -webkit-font-smoothing:antialiased;min-height:100%}\n");
            sb.Append(".toolbar{position:sticky;top:0;z-index:10;background:var(--bg2);\n");
            sb.Append("  border-bottom:1px solid var(--border);padding:14px 24px;\n");
            sb.Append("  display:flex;align-items:center;gap:14px}\n");
            sb.Append(".toolbar h1{font-size:18px;font-weight:300;flex:1}\n");
            sb.Append(".btn{background:var(--bg4);color:var(--sub);border:1px solid var(--border2);\n");
            sb.Append("  border-radius:6px;padding:7px 14px;font-size:12px;font-family:inherit;\n");
            sb.Append("  cursor:pointer;transition:all .1s}\n");
            sb.Append(".btn:hover{background:var(--border2);color:var(--text)}\n");
            sb.Append(".content{padding:24px;max-width:960px}\n");
            sb.Append(".section-label{font-size:11px;text-transform:uppercase;letter-spacing:.12em;\n");
            sb.Append("  color:var(--dim);margin-bottom:14px}\n");
            sb.Append(".grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));\n");
            sb.Append("  gap:12px;margin-bottom:32px}\n");
            sb.Append(".card{background:var(--bg2);border:2px solid var(--border);\n");
            sb.Append("  border-radius:10px;overflow:hidden;cursor:pointer;\n");
            sb.Append("  transition:border-color .15s,transform .1s,box-shadow .15s}\n");
            sb.Append(".card:hover{border-color:var(--border2);transform:translateY(-2px);\n");
            sb.Append("  box-shadow:0 6px 24px rgba(0,0,0,.4)}\n");
            sb.Append(".card.active{border-color:var(--accent)}\n");
            sb.Append(".card-preview{height:90px;position:relative;overflow:hidden}\n");
            sb.Append(".card-preview .bar{height:20px;display:flex;align-items:center;padding:0 8px;gap:4px}\n");
            sb.Append(".card-preview .dot{width:5px;height:5px;border-radius:50%}\n");
            sb.Append(".card-preview .tab{height:14px;width:60px;border-radius:3px 3px 0 0;margin-top:3px;margin-left:4px}\n");
            sb.Append(".card-preview .nav{height:22px;display:flex;align-items:center;padding:0 6px;gap:5px}\n");
            sb.Append(".card-preview .addr{flex:1;height:11px;border-radius:5px}\n");
            sb.Append(".card-preview .content-area{flex:1;display:flex;align-items:center;justify-content:center;font-size:10px;opacity:.4}\n");
            sb.Append(".card-name{padding:10px 12px;display:flex;align-items:center;justify-content:space-between}\n");
            sb.Append(".card-name span{font-size:12px;font-weight:500;color:var(--text)}\n");
            sb.Append(".check{width:16px;height:16px;border-radius:50%;border:1.5px solid var(--border2);\n");
            sb.Append("  display:flex;align-items:center;justify-content:center;flex-shrink:0}\n");
            sb.Append(".active .check{background:var(--accent);border-color:var(--accent)}\n");
            sb.Append(".check-ico{display:none;color:white;font-size:9px;font-weight:700}\n");
            sb.Append(".active .check-ico{display:block}\n");
            sb.Append(".custom-section{}\n");
            sb.Append(".import-drop{border:2px dashed var(--border2);border-radius:10px;\n");
            sb.Append("  padding:32px;text-align:center;cursor:pointer;transition:border-color .15s}\n");
            sb.Append(".import-drop:hover{border-color:var(--accent)}\n");
            sb.Append(".import-drop p{color:var(--sub);font-size:13px;margin-bottom:6px}\n");
            sb.Append(".import-drop small{color:var(--dim);font-size:11px}\n");
            sb.Append("</style></head><body>\n");

            sb.Append("<div class='toolbar'><h1>Themes</h1>\n");
            sb.Append("<button class='btn' onclick='send(\"importTheme\",\"\")'>Import Theme File</button>\n");
            sb.Append("</div>\n");
            sb.Append("<div class='content'>\n");
            sb.Append("<div class='section-label'>Built-in Themes</div>\n");
            sb.Append("<div class='grid' id='builtin'></div>\n");

            if (custom != null && custom.Count > 0)
            {
                sb.Append("<div class='section-label'>Custom Themes</div>\n");
                sb.Append("<div class='grid' id='custom'></div>\n");
            }

            sb.Append("<div class='section-label'>Create Theme</div>\n");
            sb.Append("<div class='import-drop' onclick='send(\"importTheme\",\"\")'>\n");
            sb.Append("  <p>Drop a .theme file here or click to browse</p>\n");
            sb.Append("  <small>Theme files set accent color, backgrounds, and more</small>\n");
            sb.Append("</div>\n");
            sb.Append("</div>\n"); // close content

            // Build themes JSON
            sb.Append("<script>\n");
            sb.Append("var CUR='").Append(currentId).Append("';\n");
            sb.Append("var BUILTIN=[\n");
            for (int i = 0; i < ThemeManager.BuiltIn.Count; i++)
            {
                var t = ThemeManager.BuiltIn[i];
                if (i > 0) sb.Append(",\n");
                sb.Append("{");
                sb.Append("id:\"").Append(t.Id).Append("\",");
                sb.Append("name:\"").Append(t.Name).Append("\",");
                sb.Append("bg:\"").Append(t.BgWindow).Append("\",");
                sb.Append("bar:\"").Append(t.BgTabBar).Append("\",");
                sb.Append("nav:\"").Append(t.BgNav).Append("\",");
                sb.Append("tab:\"").Append(t.BgTabActive).Append("\",");
                sb.Append("acc:\"").Append(t.Accent).Append("\",");
                sb.Append("addr:\"").Append(t.BgAddr).Append("\",");
                sb.Append("text:\"").Append(t.Text).Append("\",");
                sb.Append("sub:\"").Append(t.TextSub).Append("\"");
                sb.Append("}");
            }
            sb.Append("];\n");

            // Custom themes data
            sb.Append("var CUSTOM=[\n");
            if (custom != null)
            {
                for (int i = 0; i < custom.Count; i++)
                {
                    var t = custom[i];
                    if (i > 0) sb.Append(",\n");
                    sb.Append("{id:\"").Append(t.Id).Append("\",name:\"").Append(t.Name)
                      .Append("\",bg:\"").Append(t.BgWindow)
                      .Append("\",bar:\"").Append(t.BgTabBar)
                      .Append("\",nav:\"").Append(t.BgNav)
                      .Append("\",acc:\"").Append(t.Accent)
                      .Append("\",text:\"").Append(t.Text)
                      .Append("\",sub:\"").Append(t.TextSub).Append("\"}");
                }
            }
            sb.Append("];\n");

            sb.Append(@"
function renderTheme(t, container) {
  var active = t.id === CUR;
  var card = document.createElement('div');
  card.className = 'card' + (active ? ' active' : '');
  card.innerHTML =
    '<div class=""card-preview"" style=""background:'+t.bg+'"">'+
      '<div class=""bar"" style=""background:'+t.bar+'"">'+
        '<div class=""dot"" style=""background:#E8223A""></div>'+
        '<div class=""dot"" style=""background:#FFA040""></div>'+
        '<div class=""dot"" style=""background:#4EC94E""></div>'+
        '<div class=""tab"" style=""background:'+t.tab+';border-bottom:2px solid '+t.acc+'""></div>'+
      '</div>'+
      '<div class=""nav"" style=""background:'+t.nav+'"">'+
        '<div class=""addr"" style=""background:'+t.addr+';opacity:.6""></div>'+
      '</div>'+
      '<div class=""content-area"" style=""color:'+t.text+';"">RuneS</div>'+
    '</div>'+
    '<div class=""card-name"">'+
      '<span>'+t.name+'</span>'+
      '<div class=""check""><span class=""check-ico"">&#x2713;</span></div>'+
    '</div>';
  card.onclick = function(){ applyTheme(t.id); };
  container.appendChild(card);
}

function applyTheme(id){
  CUR = id;
  document.querySelectorAll('.card').forEach(function(c){ c.classList.remove('active'); });
  event.currentTarget.classList.add('active');
  send('applyTheme', id);
}

function send(type, value){
  window.chrome.webview.postMessage(JSON.stringify({type:type, value:value}));
}

var bi = document.getElementById('builtin');
BUILTIN.forEach(function(t){ renderTheme(t, bi); });
var ci = document.getElementById('custom');
if (ci) CUSTOM.forEach(function(t){ renderTheme(t, ci); });
");
            sb.Append("</script></body></html>\n");
            return sb.ToString();
        }
    }
}
