from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
output=root/'artifacts/replica/windows-download'
output.mkdir(parents=True,exist_ok=True)
cases=json.loads((root/'docs/replica/source/root-pageintegration-cases.json').read_text())
for ext in ['css','js']:
    (output/f'pageintegration-original.{ext}').write_bytes((root/f'docs/replica/source/original.{ext}').read_bytes())
inner='''<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="pageintegration-original.css"><div id="app"></div><div id="dialog-host"></div><div id="toast-host" role="status" aria-live="polite"></div><div id="announcer" class="sr" role="status" aria-live="polite"></div><input id="file-open" hidden type="file"><script src="pageintegration-original.js"></script><script>
const input=new URLSearchParams(location.search);lang=input.get('lang');state.screen=input.get('screen');render();
function read(){const b=e=>{const r=e.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height}};return {id:input.get('id'),viewport:{width:innerWidth,height:innerHeight},topbar:b(document.querySelector('.topbar')),sidebar:b(document.querySelector('.rail')),strip:b(document.querySelector('.sample-strip')),content:b(document.querySelector('.content')),page:b(document.querySelector('.page')),footer:b(document.querySelector('.footer'))}}
</script>'''
(output/'pageintegration-inner.html').write_text(inner)
frames=''.join(f'<iframe title="{c["id"]}" width="{c["width"]}" height="{c["height"]}" src="pageintegration-inner.html?id={c["id"]}&screen={c["screen"]}&lang={"en" if c["english"] else "ko"}" style="border:0;position:absolute;left:0;top:50px;'+('visibility:hidden' if i else '')+'"></iframe>' for i,c in enumerate(cases))
script='''document.getElementById('record').onclick=async()=>{const frames=[...document.querySelectorAll('iframe')];for(const f of frames){await f.contentDocument.fonts.ready;await Promise.all(f.contentDocument.getAnimations().filter(a=>a.effect.getTiming().iterations!==Infinity).map(a=>a.finished.catch(()=>{})))}const rows=frames.map(f=>f.contentWindow.read());const os=new URLSearchParams(location.search).get('os')==='windows'?'windows':'mac';const response=await fetch('/evidence/root-pageintegration-'+os+'-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows})});if(!response.ok)throw Error('Record failed');document.getElementById('record').textContent='Page integration recorded'}'''
(output/'pageintegration.html').write_text('<!doctype html><meta charset="utf-8"><title>Page integration · original isolation</title><button id="record">Record page integration</button>'+frames+'<script>'+script+'</script>')
print(f'{len(cases)} original full-shell page contexts')
