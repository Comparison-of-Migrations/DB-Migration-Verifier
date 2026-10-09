from pathlib import Path
import json
root=Path(__file__).resolve().parents[2];out=root/'artifacts/replica/windows-download'
cases=[dict(id=f'home-{lang}-{width}',screen='home',english=lang=='en',width=width,height=900) for lang in ('ko','en') for width in (960,1180,1280,1440)]
(root/'docs/replica/source/root-pageintegration-home-cases.json').write_text(json.dumps(cases,indent=2)+'\n')
inner='''<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="pageintegration-original.css"><div id="app"></div><div id="dialog-host"></div><div id="toast-host"></div><div id="announcer"></div><input id="file-open" hidden type="file"><script src="pageintegration-original.js"></script><script>
const input=new URLSearchParams(location.search);lang=input.get('lang');render();
function read(){const b=e=>{const r=e.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height}};return{id:input.get('id'),viewport:{width:innerWidth,height:innerHeight},topbar:b(document.querySelector('.topbar')),sidebar:b(document.querySelector('.rail')),strip:b(document.querySelector('.sample-strip')),content:b(document.querySelector('.content')),page:b(document.querySelector('.page')),footer:b(document.querySelector('.footer'))}}
</script>'''
(out/'pageintegration-home-inner.html').write_text(inner)
frames=''.join(f'<iframe width="{c["width"]}" height="{c["height"]}" src="pageintegration-home-inner.html?id={c["id"]}&lang={"en" if c["english"] else "ko"}" style="border:0;position:absolute;left:0;top:50px;visibility:hidden"></iframe>' for c in cases)
script='''async function record(){const frames=[...document.querySelectorAll('iframe')];for(const f of frames){if(f.contentDocument.readyState!=='complete')await new Promise(r=>f.onload=r);await f.contentDocument.fonts.ready;await Promise.all(f.contentDocument.getAnimations().filter(a=>a.effect.getTiming().iterations!==Infinity).map(a=>a.finished.catch(()=>{})))}const rows=frames.map(f=>f.contentWindow.read());const os=/Windows/.test(navigator.userAgent)?'windows':'mac';const response=await fetch('/evidence/root-pageintegration-home-'+os+'-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows})});if(!response.ok)throw Error('Record failed');document.getElementById('state').textContent='Recorded home parent8'}window.onload=record;'''
(out/'pageintegration-home.html').write_text('<!doctype html><meta charset="utf-8"><title>Home application parent measurement</title><p id="state">Measuring home parent8</p>'+frames+'<script>'+script+'</script>')
print('Prepared 8 original Home application parent cases; original JS/CSS unchanged')
