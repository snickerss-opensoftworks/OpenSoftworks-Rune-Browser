using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace RuneS.Helpers
{
    public static class HistoryPageBuilder
    {
        public static string Build(List<HistoryEntry> entries)
        {
            var sb = new StringBuilder();
            var css = ThemeManager.GetCssVars();

            // SAFE JSON SERIALIZATION
            var json = JsonSerializer.Serialize(
                entries.Select(e => new
                {
                    t = e.Title,
                    u = e.Url,
                    ts = e.Time.Ticks,
                    d = e.Time.ToString("yyyy-MM-dd"),
                    tf = e.Time.ToString("HH:mm")
                })
            );

            sb.Append("<!DOCTYPE html><html lang='en'><head>\n");
            sb.Append("<meta charset='UTF-8'>\n");
            sb.Append("<meta name='viewport' content='width=device-width,initial-scale=1'>\n");
            sb.Append("<title>History</title>\n");
            sb.Append("<style>\n");
            sb.Append(css).Append("\n");

            sb.Append(@"
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
html,body{
  height:100%;
  background:var(--bg);
  color:var(--text);
  font-family:'Segoe UI',system-ui,sans-serif;
  font-size:13px;
  -webkit-font-smoothing:antialiased;
}
.toolbar{
  position:sticky;
  top:0;
  z-index:10;
  background:var(--bg2);
  border-bottom:1px solid var(--border);
  padding:14px 24px;
  display:flex;
  align-items:center;
  gap:14px;
}
.toolbar h1{
  font-size:18px;
  font-weight:300;
}
.search-wrap{
  flex:1;
  max-width:420px;
  position:relative;
}
.search-wrap svg{
  position:absolute;
  left:10px;
  top:50%;
  transform:translateY(-50%);
  width:14px;
  height:14px;
  stroke:var(--dim);
  fill:none;
  pointer-events:none;
}
#search{
  width:100%;
  background:var(--bg4);
  color:var(--text);
  border:1px solid var(--border2);
  border-radius:6px;
  padding:8px 10px 8px 32px;
  font-size:12px;
  outline:none;
}
#search:focus{border-color:var(--accent)}
.spacer{flex:1}
.btn-clear{
  background:transparent;
  color:var(--sub);
  border:1px solid var(--border2);
  border-radius:6px;
  padding:7px 14px;
  font-size:11.5px;
  cursor:pointer;
  transition:all .1s;
}
.btn-clear:hover{
  color:var(--red);
  border-color:var(--red);
}
.content{
  padding:0 24px 24px;
  max-width:860px;
}
.day-group{margin-top:20px}
.day-label{
  font-size:11px;
  text-transform:uppercase;
  letter-spacing:.1em;
  color:var(--dim);
  padding:10px 0 8px;
}
.entry{
  display:flex;
  align-items:center;
  gap:12px;
  padding:9px 12px;
  border-radius:7px;
  cursor:pointer;
  transition:background .1s;
}
.entry:hover{background:var(--bg3)}
.entry-favicon{
  width:16px;
  height:16px;
  flex-shrink:0;
  border-radius:3px;
  background:var(--bg4);
  display:flex;
  align-items:center;
  justify-content:center;
  font-size:9px;
  font-weight:600;
  color:var(--dim);
  overflow:hidden;
}
.entry-favicon img{
  width:100%;
  height:100%;
  object-fit:cover;
}
.entry-info{
  flex:1;
  min-width:0;
}
.entry-title{
  font-size:12.5px;
  white-space:nowrap;
  overflow:hidden;
  text-overflow:ellipsis;
}
.entry-url{
  font-size:11px;
  color:var(--dim);
  white-space:nowrap;
  overflow:hidden;
  text-overflow:ellipsis;
  margin-top:1px;
}
.entry-time{
  font-size:10.5px;
  color:var(--dim);
  flex-shrink:0;
  min-width:36px;
  text-align:right;
}
.entry-del{
  opacity:0;
  background:transparent;
  border:none;
  cursor:pointer;
  color:var(--dim);
  padding:2px 6px;
  border-radius:4px;
  font-size:14px;
  transition:all .1s;
}
.entry:hover .entry-del{opacity:1}
.entry-del:hover{
  background:var(--bg);
  color:var(--red);
}
.empty{
  text-align:center;
  padding:60px 20px;
  color:var(--dim);
}
</style></head><body>
");

            sb.Append(@"
<div class='toolbar'>
  <h1>History</h1>
  <span id='count'></span>
  <div class='search-wrap'>
    <svg viewBox='0 0 24 24' stroke-width='2'>
      <circle cx='11' cy='11' r='8'/>
      <path d='M21 21l-4.35-4.35'/>
    </svg>
    <input id='search' placeholder='Search history...' oninput='filter(this.value)' autofocus>
  </div>
  <div class='spacer'></div>
  <button class='btn-clear' onclick='clearAll()'>Clear All</button>
</div>

<div class='content' id='list'></div>

<script>
var ALL = " + json + @";

function esc(s){
  return (s||'')
    .replace(/&/g,'&amp;')
    .replace(/</g,'&lt;')
    .replace(/>/g,'&gt;')
    .replace(/""/g,'&quot;');
}

function getFavLetter(url){
  try{
    return new URL(url).hostname.replace('www.','').charAt(0).toUpperCase();
  }catch(e){return '?';}
}

function getFavUrl(url){
  try{
    var u=new URL(url);
    return u.origin + '/favicon.ico';
  }catch(e){return '';}
}

function render(data){
  var list=document.getElementById('list');
  document.getElementById('count').textContent =
    data.length + ' item' + (data.length===1?'':'s');

  if(!data.length){
    list.innerHTML='<div class=""empty""><p>No history found</p></div>';
    return;
  }

  var groups={},order=[];
  data.forEach(function(e){
    if(!groups[e.d]){
      groups[e.d]=[];
      order.push(e.d);
    }
    groups[e.d].push(e);
  });

  var html='';

  order.forEach(function(day){
    html+='<div class=""day-group""><div class=""day-label"">'+day+'</div>';
    groups[day].forEach(function(e){
      var letter=getFavLetter(e.u);
      var favUrl=getFavUrl(e.u);

      html+='<div class=""entry"" onclick=""nav(\''+e.u.replace(/'/g,'\\\'')+'\')"">'+
        '<div class=""entry-favicon""><img src=""'+favUrl+'"" onerror=""this.style.display=\'none\'"">'+letter+'</div>'+
        '<div class=""entry-info"">'+
          '<div class=""entry-title"">'+esc(e.t||e.u)+'</div>'+
          '<div class=""entry-url"">'+esc(e.u)+'</div>'+
        '</div>'+
        '<div class=""entry-time"">'+e.tf+'</div>'+
        '<button class=""entry-del"" onclick=""event.stopPropagation();del(\''+e.ts+'\')"">&#x2715;</button>'+
      '</div>';
    });
    html+='</div>';
  });

  list.innerHTML=html;
}

function filter(q){
  if(!q){ render(ALL); return; }
  q=q.toLowerCase();
  render(ALL.filter(function(e){
    return (e.t||'').toLowerCase().includes(q)
        || (e.u||'').toLowerCase().includes(q);
  }));
}

function nav(url){
  window.chrome.webview.postMessage(JSON.stringify({type:'navigate',value:url}));
}

function del(ts){
  window.chrome.webview.postMessage(JSON.stringify({type:'historyDelete',value:ts}));
}

function clearAll(){
  if(confirm('Clear all browsing history?')){
    window.chrome.webview.postMessage(JSON.stringify({type:'historyClear'}));
  }
}

render(ALL);
</script>
</body></html>
");

            return sb.ToString();
        }
    }
}