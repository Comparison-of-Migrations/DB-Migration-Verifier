from pathlib import Path
import json
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/windows-download';rows=[]
for variant,prefix in enumerate(['migration_sample → migration_sample','migration_sample_with_a_long_database_name → migration_sample_with_a_long_database_name','','migration_sample → migration_sample']):
 for lang,suffix in [('ko','원본과 대상은 서로 다른 예시 서버입니다.'),('en','Source and target are separate sample servers.')]:
  for width in [412,466,660,816]:rows.append(dict(id=f'{variant}-{lang}-{width}',prefix=prefix,suffix=suffix if variant!=3 else '',width=width))
assert len(rows)==32
for p in [o/'inlinetext-cases.json',r/'docs/replica/source/inlinetext-cases.json']:p.write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n')
(o/'inlinetext-original.css').write_bytes((r/'docs/replica/source/original.css').read_bytes())
s='const cases='+json.dumps(rows,ensure_ascii=False)+''';for(const c of cases){const f=document.createElement('div');f.className='fixture';f.dataset.id=c.id;f.style.width=c.width+'px';const e=document.createElement('div');e.className='mono';e.append(c.prefix);const small=document.createElement('small');small.textContent=c.suffix;e.append(small);f.append(e);document.getElementById('fixtures').append(f)}
function read(){return [...document.querySelectorAll('.fixture')].map(f=>{const e=f.firstChild,r=e.getBoundingClientRect(),small=e.querySelector('small');const a=document.createRange();a.selectNodeContents(small);const rects=[...a.getClientRects()];return{id:f.dataset.id,width:r.width,height:r.height,text:e.textContent,regions:rects.filter(q=>q.width>0).map(q=>({x:q.x-r.x,y:Math.floor((q.y-r.y)/22)*22})),color:getComputedStyle(e).color,suffixColor:getComputedStyle(small).color}})}
document.getElementById('inlinetext-record').onclick=async()=>{await document.fonts.ready;const res=await fetch('/evidence/source-sidebar.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows:read()})});if(!res.ok)throw Error('Record failed');document.getElementById('inlinetext-record').textContent='InlineText recorded'};'''
(o/'inlinetext.html').write_text('<!doctype html><meta charset="utf-8"><title>InlineText · original isolation</title><link rel="stylesheet" href="inlinetext-original.css"><style>body{min-width:1100px}.fixture{position:absolute;left:30px;top:100px}.fixture:not(:first-child){visibility:hidden}.fixture>.mono{line-height:22px}#inlinetext-record{position:fixed;left:30px;top:20px}</style><button id="inlinetext-record">Record InlineText</button><div id="fixtures"></div><script>'+s+'</script>')
print('32 same-font inline cases')
