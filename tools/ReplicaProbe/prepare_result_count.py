"""Direct result-count DOM/CSS and original WAAPI; no changes to the protected original."""
from pathlib import Path
import json,re
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/windows-download'
css=r/'docs/replica/source/original.css';js=(r/'docs/replica/source/original.js').read_text()
animate=re.search(r'function animateResult\(\)\{.*?\}\)',js)
# Exact single-line original function, not a hand-recreated timing definition.
animate=next(line for line in js.splitlines() if line.startswith('function animateResult()'))
(o/'resultcount-original.css').write_bytes(css.read_bytes())
cases=[];parts=['<!doctype html><meta charset="utf-8"><title>ResultCount · original isolation</title><link rel="stylesheet" href="resultcount-original.css"><style>body{min-width:900px}.fixture{position:absolute;left:30px}</style>']
for width in [90,137.375,190.90625]:
 for kind in ['different','unknown','equal']:
  for count,maximum in [(0,20),(1,20),(11,20),(20,20),(1,10000),(123456789,123456790)]:
   id=str(len(cases));cases.append(dict(id=id,width=width,kind=kind,count=count,maximum=maximum))
   parts.append(f'<span class="fixture result-count {kind} {"zero"if count==0 else""}" data-id="{id}" style="top:{30+len(cases)*70}px;width:{width}px"><b class="num">{count}</b><span class="mini-track" aria-hidden="true"><span class="mini-bar" style="width:{count/maximum*100}%"></span></span></span>')
parts.append('<button id="result-count-record" style="position:fixed;right:20px;top:20px">Record ResultCount</button><button id="result-count-entrance" style="position:fixed;right:20px;top:60px">Play original entrance</button><output id="result-count-status" style="position:fixed;right:20px;top:100px"></output><script>')
parts.append(animate)
parts.append('''
function box(e,root){const b=e.getBoundingClientRect(),r=root.getBoundingClientRect();return{x:b.x-r.x,y:b.y-r.y,width:b.width,height:b.height}}
function read(e){const b=e.querySelector('b'),track=e.querySelector('.mini-track'),bar=e.querySelector('.mini-bar'),s=getComputedStyle(b),bs=getComputedStyle(bar);const range=document.createRange();range.selectNodeContents(b);return{id:e.dataset.id,width:e.getBoundingClientRect().width,height:e.getBoundingClientRect().height,number:box(b,e),track:box(track,e),bar:box(bar,e),glyphWidth:range.getBoundingClientRect().width,text:b.textContent,color:s.color,fontSize:s.fontSize,fontWeight:s.fontWeight,lineHeight:s.lineHeight,letterSpacing:s.letterSpacing,trackColor:getComputedStyle(track).backgroundColor,barColor:bs.backgroundColor,gradient:bs.backgroundImage,radius:bs.borderRadius,overflow:getComputedStyle(track).overflow,origin:bs.transformOrigin}}
document.getElementById('result-count-entrance').onclick=()=>{animateResult();document.getElementById('result-count-status').textContent='Entrance requested';};
document.getElementById('result-count-record').onclick=async()=>{
 const rows=[...document.querySelectorAll('.fixture')].map(read),timing=[];
 const originalReduce=matchMedia('(prefers-reduced-motion: reduce)').matches;
 // The normal branch invokes the exact original animateResult; reduced mode is its no-effect branch.
 if(!originalReduce){animateResult();for(const a of document.getAnimations())a.pause();}
 const e=document.querySelector('.fixture[data-id="8"]');
 for(const reduced of [false,true])for(const kind of ['number','bar']){
  const target=e.querySelector(kind==='number'?'b':'.mini-bar'),a=target.getAnimations()[0];
  for(const fraction of [0,.125,.25,.375,.5,.625,.75,.875,.999,1,1.001,1.25]){
   if(reduced){if(a)a.cancel();}else{if(!a)throw Error('Normal source motion unavailable');a.currentTime=fraction*(kind==='number'?220:320);}
   const style=getComputedStyle(target);timing.push({reduced,kind,fraction,opacity:Number(style.opacity),matrix:style.transform==='none'?[1,0,0,1,0,0]:style.transform.slice(7,-1).split(',').map(Number)});
  }
 }
 for(const a of document.getAnimations())a.cancel();
 const result=await fetch('/evidence/source-resultcount.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows,timing,originalReduce})});if(!result.ok)throw Error('Record failed');document.getElementById('result-count-record').textContent='Counts recorded';};
</script>''')
(o/'resultcount.html').write_text('\n'.join(parts));(r/'docs/replica/source/resultcount-cases.json').write_text(json.dumps(cases,indent=2)+'\n');(o/'resultcount-cases.json').write_text(json.dumps(cases))
p=r/'tools/WindowsControl/fixture_server.py';s=p.read_text();s=s.replace("allowed={'source-tablerow-motion.json'","allowed={'source-resultcount.json','source-tablerow-motion.json'");p.write_text(s)
print(len(cases),'result counts prepared')
