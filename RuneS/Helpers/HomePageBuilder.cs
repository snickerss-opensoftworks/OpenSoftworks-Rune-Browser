namespace RuneS.Helpers
{
    public static class HomePageBuilder
    {
        public static string Build()
        {
            return @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<title>New Tab</title>
<style>
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
:root{
  --bg:#080A0E;--bg2:#0D0F14;--bg3:#13151B;--bg4:#1A1C22;
  --border:#1E2130;--border2:#2E3245;
  --accent:#4D9EFF;--accentDim:rgba(77,158,255,.10);
  --text:#F0F2F8;--sub:#8890A4;--dim:#44485A;
  --green:#4EC94E;
}
html,body{height:100%;background:var(--bg);color:var(--text);
  font-family:'Segoe UI',system-ui,sans-serif;-webkit-font-smoothing:antialiased;
  overflow-x:hidden;user-select:none}
body{display:flex;flex-direction:column;align-items:center;
  justify-content:flex-start;padding:56px 20px 20px;min-height:100%}

/* Clock */
.clock-wrap{text-align:center;margin-bottom:40px}
.clock-time{font-size:80px;font-weight:100;letter-spacing:-5px;
  color:var(--text);line-height:1;font-variant-numeric:tabular-nums}
.clock-date{font-size:11px;color:var(--dim);margin-top:10px;
  letter-spacing:.16em;text-transform:uppercase}

/* Search */
.search-wrap{width:min(600px,100%);margin-bottom:40px}
.search-bar{display:flex;align-items:center;
  background:var(--bg3);border:1px solid var(--border2);
  border-radius:10px;padding:0 16px;height:50px;
  transition:border-color .15s,box-shadow .15s}
.search-bar:focus-within{border-color:var(--accent);
  box-shadow:0 0 0 3px rgba(77,158,255,.10)}
.s-ico{width:16px;height:16px;flex-shrink:0;margin-right:12px;
  opacity:.3;pointer-events:none}
.search-input{flex:1;background:transparent;border:none;outline:none;
  font-size:14px;font-family:inherit;color:var(--text);caret-color:var(--accent)}
.search-input::placeholder{color:var(--dim)}
.search-submit{background:var(--accent);color:white;border:none;
  border-radius:7px;width:32px;height:32px;cursor:pointer;flex-shrink:0;
  display:flex;align-items:center;justify-content:center;
  transition:background .12s;margin-left:8px}
.search-submit:hover{background:#3d8eef}
.search-submit svg{width:14px;height:14px}

/* Engine chips */
.engine-chips{display:flex;gap:6px;margin-top:10px;justify-content:center;flex-wrap:wrap}
.chip{font-size:10.5px;color:var(--dim);background:var(--bg3);
  border:1px solid var(--border);border-radius:20px;
  padding:4px 12px;cursor:pointer;transition:all .12s}
.chip:hover,.chip.active{background:var(--accentDim);
  border-color:var(--accent);color:var(--accent)}

/* Section */
.section-label{font-size:10px;text-transform:uppercase;letter-spacing:.14em;
  color:var(--dim);margin-bottom:12px;align-self:flex-start;
  width:min(600px,100%)}

/* Shortcuts grid */
.shortcuts{display:grid;grid-template-columns:repeat(4,1fr);
  gap:8px;width:min(600px,100%);margin-bottom:32px}
.shortcut{display:flex;flex-direction:column;align-items:center;gap:10px;
  padding:20px 8px 17px;background:var(--bg2);
  border:1px solid var(--border);border-radius:10px;
  cursor:pointer;text-decoration:none;color:inherit;
  transition:background .12s,border-color .12s,transform .1s}
.shortcut:hover{background:var(--bg3);border-color:var(--border2);
  transform:translateY(-2px);box-shadow:0 4px 20px rgba(0,0,0,.4)}
.shortcut:active{transform:scale(.97)}
.s-icon{width:40px;height:40px;background:var(--bg3);
  border:1px solid var(--border2);border-radius:10px;
  display:flex;align-items:center;justify-content:center}
.s-icon svg{width:20px;height:20px}
.s-lbl{font-size:11px;color:var(--sub);text-align:center;
  overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:100%}

/* Recently visited */
.recent-wrap{width:min(600px,100%)}
.recent-list{display:flex;flex-direction:column;gap:4px}
.recent-item{display:flex;align-items:center;gap:12px;
  padding:10px 14px;background:var(--bg2);border:1px solid var(--border);
  border-radius:8px;cursor:pointer;text-decoration:none;color:inherit;
  transition:background .1s}
.recent-item:hover{background:var(--bg3)}
.recent-favicon{width:16px;height:16px;border-radius:3px;opacity:.7}
.recent-title{font-size:12px;color:var(--text);flex:1;
  overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.recent-url{font-size:11px;color:var(--dim);
  overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:260px}

footer{position:fixed;bottom:10px;right:14px;font-size:10px;color:#222;
  letter-spacing:.04em}
</style>
</head>
<body>

<div class='clock-wrap'>
  <div class='clock-time' id='clock'>00:00</div>
  <div class='clock-date' id='datestr'></div>
</div>

<div class='search-wrap'>
  <div class='search-bar'>
    <svg class='s-ico' viewBox='0 0 24 24' fill='none' stroke='currentColor'
         stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
      <circle cx='11' cy='11' r='8'/><path d='M21 21l-4.35-4.35'/>
    </svg>
    <input class='search-input' id='si'
           placeholder='Search or go to a website...'
           autofocus onkeydown='handleKey(event)'/>
    <button class='search-submit' onclick='doSearch()'>
      <svg viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5'
           stroke-linecap='round' stroke-linejoin='round'>
        <path d='M5 12h14M12 5l7 7-7 7'/>
      </svg>
    </button>
  </div>
  <div class='engine-chips'>
    <div class='chip active' onclick='setEngine(""brave"",this)'>Brave</div>
    <div class='chip' onclick='setEngine(""google"",this)'>Google</div>
    <div class='chip' onclick='setEngine(""ddg"",this)'>DuckDuckGo</div>
    <div class='chip' onclick='setEngine(""bing"",this)'>Bing</div>
  </div>
</div>

<div class='section-label'>Quick Access</div>
<div class='shortcuts'>

  <a class='shortcut' href='https://github.com'>
    <div class='s-icon'>
      <svg viewBox='0 0 24 24' fill='none' stroke='var(--sub)' stroke-width='1.7'
           stroke-linecap='round' stroke-linejoin='round'>
        <path d='M9 19c-5 1.5-5-2.5-7-3m14 6v-3.87a3.37 3.37 0 00-.94-2.61
          c3.14-.35 6.44-1.54 6.44-7A5.44 5.44 0 0020 4.77
          A4.92 4.92 0 0019.91 1S18.73.65 16 2.48a13.38 13.38 0 00-7 0
          C6.27.65 5.09 1 5.09 1A4.92 4.92 0 005 4.77
          a5.44 5.44 0 00-1.5 3.78c0 5.42 3.3 6.61 6.44 7
          A3.37 3.37 0 009 18.13V22'/>
      </svg>
    </div>
    <span class='s-lbl'>GitHub</span>
  </a>

  <a class='shortcut' href='https://www.youtube.com'>
    <div class='s-icon'>
      <svg viewBox='0 0 24 24' fill='none' stroke='var(--sub)' stroke-width='1.7'
           stroke-linecap='round' stroke-linejoin='round'>
        <path d='M22.54 6.42a2.78 2.78 0 00-1.95-1.96C18.88 4 12 4 12 4
          s-6.88 0-8.59.46A2.78 2.78 0 001.46 6.42 29 29 0 001 12
          a29 29 0 00.46 5.58A2.78 2.78 0 003.41 19.6C5.12 20 12 20
          12 20s6.88 0 8.59-.46a2.78 2.78 0 001.95-1.95A29 29 0 0023
          12a29 29 0 00-.46-5.58z'/>
        <polygon points='9.75,15.02 15.5,12 9.75,8.98'
                 fill='var(--sub)' stroke='none'/>
      </svg>
    </div>
    <span class='s-lbl'>YouTube</span>
  </a>

  <a class='shortcut' href='https://www.reddit.com'>
    <div class='s-icon'>
      <svg viewBox='0 0 24 24' fill='none' stroke='var(--sub)' stroke-width='1.7'
           stroke-linecap='round' stroke-linejoin='round'>
        <circle cx='12' cy='13.5' r='6.5'/>
        <path d='M17.5 6l-3-1.5M20.5 8a1.5 1.5 0 110 3'/>
        <circle cx='9.5' cy='12.5' r='.6' fill='var(--sub)' stroke='none'/>
        <circle cx='14.5' cy='12.5' r='.6' fill='var(--sub)' stroke='none'/>
        <path d='M9.5 15.5s1 1.5 2.5 1.5 2.5-1.5 2.5-1.5'/>
      </svg>
    </div>
    <span class='s-lbl'>Reddit</span>
  </a>

  <a class='shortcut' href='https://news.ycombinator.com'>
    <div class='s-icon'>
      <svg viewBox='0 0 24 24' fill='none' stroke='var(--sub)' stroke-width='1.7'
           stroke-linecap='round' stroke-linejoin='round'>
        <path d='M4 4l8 8 8-8'/><path d='M12 12v8'/>
        <rect x='2' y='20' width='20' height='2' rx='1'
              fill='var(--sub)' stroke='none'/>
      </svg>
    </div>
    <span class='s-lbl'>Hacker News</span>
  </a>

  <a class='shortcut' href='https://www.twitch.tv'>
    <div class='s-icon'>
      <svg viewBox='0 0 24 24' fill='none' stroke='var(--sub)' stroke-width='1.7'
           stroke-linecap='round' stroke-linejoin='round'>
        <path d='M21 2H3v16h5v4l4-4h5l4-4V2z'/>
        <line x1='11' y1='7' x2='11' y2='11'/>
        <line x1='16' y1='7' x2='16' y2='11'/>
      </svg>
    </div>
    <span class='s-lbl'>Twitch</span>
  </a>

  <a class='shortcut' href='https://www.wikipedia.org'>
    <div class='s-icon'>
      <svg viewBox='0 0 24 24' fill='none' stroke='var(--sub)' stroke-width='1.7'
           stroke-linecap='round' stroke-linejoin='round'>
        <path d='M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z'/>
        <path d='M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z'/>
      </svg>
    </div>
    <span class='s-lbl'>Wikipedia</span>
  </a>

  <a class='shortcut' href='https://www.x.com'>
    <div class='s-icon'>
      <svg viewBox='0 0 24 24' fill='var(--sub)' stroke='none'>
        <path d='M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-4.714-6.231-5.401 6.231H2.748l7.73-8.835L1.254 2.25H8.08l4.259 5.63 5.905-5.63zm-1.161 17.52h1.833L7.084 4.126H5.117L17.083 19.77z'/>
      </svg>
    </div>
    <span class='s-lbl'>X</span>
  </a>

  <a class='shortcut' href='https://search.brave.com'>
    <div class='s-icon'>
      <svg viewBox='0 0 24 24' fill='none' stroke='var(--sub)' stroke-width='1.7'
           stroke-linecap='round' stroke-linejoin='round'>
        <path d='M12 2L4 6l.5 10L12 22l7.5-6L20 6z'/>
        <path d='M12 8v6M9 11l3 3 3-3'/>
      </svg>
    </div>
    <span class='s-lbl'>Brave Search</span>
  </a>
</div>

<footer>RuneS</footer>

<script>
var engines = {
  brave:  'https://search.brave.com/search?q=',
  google: 'https://www.google.com/search?q=',
  ddg:    'https://duckduckgo.com/?q=',
  bing:   'https://www.bing.com/search?q='
};
var activeEngine = 'brave';

function setEngine(key, el) {
  activeEngine = key;
  document.querySelectorAll('.chip').forEach(c => c.classList.remove('active'));
  el.classList.add('active');
}

function doSearch() {
  var q = document.getElementById('si').value.trim();
  if (!q) return;
  var url = /^https?:\/\//i.test(q) ? q
          : (q.indexOf('.')>-1 && q.indexOf(' ')<0 ? 'https://'+q
          : engines[activeEngine]+encodeURIComponent(q));
  window.location.href = url;
}
function handleKey(e) { if(e.key==='Enter') doSearch(); }

// Clock
(function tick() {
  var n=new Date(), h=n.getHours().toString().padStart(2,'0'),
      m=n.getMinutes().toString().padStart(2,'0');
  document.getElementById('clock').textContent = h+':'+m;
  var D=['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'],
      M=['January','February','March','April','May','June','July',
         'August','September','October','November','December'];
  document.getElementById('datestr').textContent =
    D[n.getDay()]+', '+M[n.getMonth()]+' '+n.getDate()+', '+n.getFullYear();
  setTimeout(tick, 10000);
})();
</script>
</body></html>";
        }
    }
}
