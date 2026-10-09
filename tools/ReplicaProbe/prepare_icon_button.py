"""Example-only icon button source fixture; keeps the original web untouched."""
from pathlib import Path
import json,re
root=Path(__file__).resolve().parents[2];out=root/'artifacts/replica/windows-download'
cases=[]
for context in ['search','dialog']:
 for lang in ['ko','en']:
  label=({'ko':'테이블 검색 지우기','en':'Clear table search'} if context=='search' else {'ko':'닫기','en':'Close'})[lang]
  for state in ['enabled','disabled','reduced']:
   cases.append(dict(id=f'{context}-{lang}-{state}',context=context,label=label,disabled=state=='disabled',reduced=state=='reduced'))
(root/'docs/replica/source/iconbutton-cases.json').write_text(json.dumps(cases,ensure_ascii=False,indent=2)+'\n');(out/'iconbutton-cases.json').write_text(json.dumps(cases,ensure_ascii=False))
css=(root/'docs/replica/source/original.css').read_text();(out/'iconbutton-original.css').write_text(css)
# State classes duplicate only selectors containing pseudo states. The source
# declarations/specificity/order are retained; never add these to the product web.
statecss='\n'.join(sel.replace(':hover','.fixture-over').replace(':active','.fixture-active').replace(':focus-visible','.fixture-focus')+'{'+body+'}' for sel,body in re.findall(r'([^{}]+)\{([^{}]*)\}',css) if any(x in sel for x in [':hover',':active',':focus-visible']))
html=['<!doctype html><meta charset="utf-8"><title>IconButton · original isolation</title><link rel="stylesheet" href="iconbutton-original.css"><style>body{position:relative;width:760px;height:820px}.cell{position:absolute;width:300px;height:60px}.dialog-head{padding:0;border:0}.dialog-head h2{font-size:13px}.fixture-reduced button{transition:none !important}</style>', '<style>'+statecss+'</style>']
for i,c in enumerate(cases):
 button=f'<button type="button" id="{c["id"]}" class="btn quiet icon-button" data-action="fixture" aria-label="{c["label"]}" {"disabled" if c["disabled"] else ""}><svg class="svg" viewBox="0 0 24 24" aria-hidden="true"><path d="m6 6 12 12M6 18 18 6"></path></svg></button>'
 inner=('<div class="table-toolbar"><div class="search-field" style="height:44px">'+button+'</div></div>') if c['context']=='search' else '<div class="dialog-head"><h2>Example heading</h2>'+button+'</div>'
 html.append(f'<div class="cell {"fixture-reduced" if c["reduced"] else ""}" data-id="{c["id"]}" style="left:30px;top:{30+i*64}px">'+inner+'</div>')
html.append('''<button id="iconbutton-record" type="button" style="position:fixed;left:470px;top:20px">Record IconButton</button><textarea id="iconbutton-evidence" readonly aria-label="IconButton evidence" style="position:fixed;left:470px;top:80px;width:270px;height:300px"></textarea><script>
function press(b){if(!b.closest('.fixture-reduced')&&!matchMedia('(prefers-reduced-motion: reduce)').matches)b.animate([{transform:'scale(1)',opacity:1},{transform:'scale(.98)',opacity:.86},{transform:'scale(1)',opacity:1}],{duration:160,easing:'ease-out'})}
document.addEventListener('click',e=>{const b=e.target.closest('[data-action=fixture]');if(!b||b.disabled)return;press(b);b.dataset.clicks=Number(b.dataset.clicks||0)+1});
function read(b,id,mode){const s=getComputedStyle(b),svg=b.querySelector('svg'),r=b.getBoundingClientRect(),q=svg.getBoundingClientRect();return{id,mode,label:b.getAttribute('aria-label'),width:parseFloat(s.width),height:parseFloat(s.height),padding:s.padding,radius:s.borderRadius,borderWidth:s.borderWidth,bg:s.backgroundColor,color:s.color,border:s.borderColor,shadow:s.boxShadow,transform:s.transform,outline:s.outline,offset:s.outlineOffset,icon:{x:q.x-r.x,y:q.y-r.y,width:q.width,height:q.height},svg:svg.outerHTML,fontSize:s.fontSize,children:b.children.length,disabled:b.disabled}}
document.getElementById('iconbutton-record').onclick=async()=>{const rows=[];for(const cell of document.querySelectorAll('.cell[data-id]'))for(const mode of ['normal','hover','pressed','focus']){const clone=cell.cloneNode(true),b=clone.querySelector('button');if(b.disabled&&mode==='focus')continue;clone.style.left='390px';clone.style.top='430px';document.body.append(clone);b.id='state-helper';if(mode==='hover'||mode==='pressed')b.classList.add('fixture-over');if(mode==='pressed')b.classList.add('fixture-active');if(mode==='focus')b.classList.add('fixture-focus');getComputedStyle(b).backgroundColor;for(const a of b.getAnimations()){if(a instanceof CSSTransition)a.finish()};rows.push(read(b,cell.dataset.id,mode));clone.remove()};const result={dpr:devicePixelRatio,rows};const response=await fetch('/evidence/source-iconbutton.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(result)});if(!response.ok)throw Error('Example evidence failed');document.getElementById('iconbutton-evidence').value=JSON.stringify(result);document.getElementById('iconbutton-record').textContent='IconButton recorded';};
</script>''');(out/'iconbutton.html').write_text('\n'.join(html))
print(f'{len(cases)} context/language/disabled/reduced cases prepared')
