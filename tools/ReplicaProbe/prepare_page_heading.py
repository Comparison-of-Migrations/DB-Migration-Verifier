"""Original heading() and its actual resource strings, evaluated with untouched CSS."""
from pathlib import Path
import re,ast,json
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/windows-download';src=(r/'docs/replica/source/original.js').read_text().splitlines()
resources=[]
for line in src:
 if not line.startswith(tuple('function '+x+'(' for x in ['connect','capabilities','select','trial','review','running','results','save'])):continue
 key=line.split('function ')[1].split('(')[0];start=line.index('heading(');end=start+len('heading(');depth=1;quote=None;escape=False
 while depth:
  c=line[end];end+=1
  if quote:
   if escape:escape=False
   elif c=='\\':escape=True
   elif c==quote:quote=None
  elif c in ['"',"'"]:quote=c
  elif c=='(':depth+=1
  elif c==')':depth-=1
 text=line[:end] if key=='results' else line[start:end]
 pairs=[tuple(ast.literal_eval(v) for v in m)for m in re.findall(r"t\(('(?:\\.|[^'\\])*'),('(?:\\.|[^'\\])*')\)",text)]
 for i,title in enumerate(pairs[:-1]):resources.append(dict(screen=key,variant=i,title=title,description=pairs[-1]))
cases=[dict(id=f"{x['screen']}-{x['variant']}-{lang}-{width}",lang=lang,width=width,title=x['title'][lang=='en'],description=x['description'][lang=='en'])for x in resources for lang in ['ko','en']for width in [656,980,1060]]
assert len(resources)==14 and len(cases)==84
(o/'pageheading-original.css').write_bytes((r/'docs/replica/source/original.css').read_bytes());heading=next(x for x in src if x.startswith('const heading='))
script=heading+'\nconst cases='+json.dumps(cases,ensure_ascii=False)+""";for(const c of cases){const f=document.createElement('div');f.className='fixture';f.dataset.id=c.id;f.style.width=c.width+'px';f.innerHTML=heading(c.title,c.description);f.querySelector('h1').id='heading-'+c.id;document.getElementById('fixtures').append(f)}
function read(){return [...document.querySelectorAll('.fixture')].map(f=>{const e=f.firstChild,r=e.getBoundingClientRect(),b=x=>{const q=x.getBoundingClientRect();return{x:q.x-r.x,y:q.y-r.y,width:q.width,height:q.height}},h=e.querySelector('h1'),p=e.querySelector('p');return{id:f.dataset.id,width:r.width,height:r.height,title:b(h),description:b(p),titleText:h.textContent,descriptionText:p.textContent,marginBottom:getComputedStyle(e).marginBottom,titleColor:getComputedStyle(h).color,descriptionColor:getComputedStyle(p).color}})}
const title=document.querySelector('.fixture h1');const live=document.getElementById('live');function state(){live.textContent=JSON.stringify({focused:document.activeElement===title,focusVisible:title.matches(':focus-visible'),focusId:document.activeElement.id,outline:getComputedStyle(title).outline,headingLevel:Number(title.tagName.slice(1))})}document.getElementById('focus-title').onclick=()=>{title.focus({preventScroll:true});state()};for(const type of ['focusin','focusout','keydown','keyup','pointerdown'])document.addEventListener(type,()=>setTimeout(state,0));state();
document.getElementById('pageheading-record').onclick=async()=>{await document.fonts.ready;const res=await fetch('/evidence/source-sidebar.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows:read()})});if(!res.ok)throw Error('Evidence failed');document.getElementById('pageheading-record').textContent='PageHeading recorded'};"""
(o/'pageheading.html').write_text('<!doctype html><meta charset="utf-8"><title>PageHeading · original isolation</title><link rel="stylesheet" href="pageheading-original.css"><style>body{min-width:1100px}.fixture{position:absolute;left:30px;top:100px}.fixture:not(:first-child){visibility:hidden;pointer-events:none}#pageheading-record{position:fixed;left:30px;top:20px}#focus-title{position:fixed;left:500px;top:20px}#after-title{position:fixed;left:30px;top:280px}#live{position:absolute;left:30px;top:360px;white-space:pre-wrap}</style><button id="pageheading-record">Record PageHeading</button><button id="focus-title">Focus title</button><div id="fixtures"></div><button id="after-title">Next button</button><pre id="live"></pre><script>'+script+'</script>')
for p in [o/'pageheading-cases.json',r/'docs/replica/source/pageheading-cases.json']:p.write_text(json.dumps(cases,ensure_ascii=False,indent=2)+'\n')
print(len(cases),'original heading resource/width cases')
