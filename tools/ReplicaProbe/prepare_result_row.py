"""Exact resultChart row DOM/CSS, wide/compact endpoints plus status/disabled boundaries."""
from pathlib import Path
import re,json
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/windows-download';js=(r/'docs/replica/source/original.js').read_text();icons=dict(re.findall(r"(\w+):'([^']*)'",re.search(r'const icons=\{(.*?)\};',js).group(1)));svg=lambda n:'<svg class="svg" viewBox="0 0 24 24" aria-hidden="true">'+icons[n]+'</svg>'
(o/'resultrow-original.css').write_bytes((r/'docs/replica/source/original.css').read_bytes());cases=[]
data=[('data',['데이터','Data'],'db',1,0,11),('schema',['구조·제약','Schema & constraints'],'grid',1,0,17),('access',['사용자·권한','Users & permissions'],'shield',1,2,5),('objects',['관련 객체','Related objects'],'layers',0,0,12)]
for compact in [False,True]:
 for lang in ['ko','en']:
  for width in [610,858,978,1058]:
   for state in ['rest','hover','focus']:
    for j,(key,names,icon,diff,unknown,equal) in enumerate(data):
     desc=('실행 완료 · 전체 'if lang=='ko'else'Completed · Total ')+str(diff+unknown+equal)
     cases.append(dict(lang=lang,width=width,key=key,name=names[lang=='en'],description=desc,icon=icon,different=diff,unknown=unknown,equal=equal,maximum=20,hover=state=='hover',focus=state=='focus',last=j==3,compact=compact,disabled=False))
# Source interrupted/unstarted descriptions; exact source totals and counts, narrowest wide/compact context.
for compact in [False,True]:
 for lang in ['ko','en']:
  for status in ['interrupted','unstarted']:
   for j,(key,names,icon,diff,unknown,equal) in enumerate(data):
    total=diff+unknown+equal
    desc=({'interrupted':['중단 · 미검증 포함','Interrupted · Gaps retained'],'unstarted':['미시작 · 미검증','Not started · Unverified']}[status][lang=='en'])+(' · 전체 'if lang=='ko'else' · Total ')+str(total)
    cases.append(dict(lang=lang,width=610,key=key,name=names[lang=='en'],description=desc,icon=icon,different=diff if status=='interrupted'else 0,unknown=total-equal-diff if status=='interrupted'else total,equal=equal if status=='interrupted'else 0,maximum=20,hover=False,focus=False,last=j==3,compact=compact,disabled=False))
for compact in [False,True]:
 for state in ['rest','hover','focus']:
  cases.append(dict(cases[0],compact=compact,disabled=True,hover=state=='hover',focus=state=='focus'))
parts=['<!doctype html><meta charset="utf-8"><title>ResultSummaryRow · original isolation</title><link rel="stylesheet" href="resultrow-original.css"><style>body{min-width:1300px}.fixture{position:absolute;left:30px;margin:0}.fixture *{transition:none!important;animation:none!important}.result-row.force-hover:not(:disabled){background:#353740}.result-row.force-hover .row-chevron{transform:translateX(2px)}.result-row.force-focus{outline:3px solid #a7b1ff;outline-offset:-3px}/* Exact original.css47 media declarations, isolated endpoint; live viewport checked separately. */.result-row.compact{grid-template-columns:minmax(155px,1.4fr) repeat(3,minmax(75px,1fr)) 16px;gap:16px;padding-inline:16px}.compact .result-category{gap:8px}</style>']
for i,c in enumerate(cases):
 c['id']=str(i);name=c['name'];nums=[c[k]for k in ['different','unknown','equal']];label=name+': '+', '.join((['불일치','미검증','일치']if c['lang']=='ko'else['Different','Unverified','Equal'])[j]+' '+str(n)for j,n in enumerate(nums))+('. 세부 보기'if c['lang']=='ko'else'. View details')
 # Last-child border is source parent relationship, not a style override.
 parts.append(f'<figure class="result-chart fixture" style="top:{30+i*150}px;width:{c["width"]+2}px"><div class="result-rows"><button type="button" class="result-row {"compact"if c["compact"]else""} {"force-hover"if c["hover"]else""} {"force-focus"if c["focus"]else""}" data-id="{i}" aria-label="{label}" {"disabled"if c["disabled"]else""}><span class="result-category"><span class="category-icon">{svg(c["icon"])}</span><span><strong>{name}</strong><small>{c["description"]}</small></span></span>'+''.join(f'<span class="result-count {kind} {"zero"if n==0 else""}"><b class="num">{n}</b><span class="mini-track" aria-hidden="true"><span class="mini-bar" style="width:{n/c["maximum"]*100}%"></span></span></span>'for kind,n in zip(['different','unknown','equal'],nums))+f'<span class="row-chevron">{svg("chevron")}<span class="sr">{"세부 보기"if c["lang"]=="ko"else"Details"}</span></span></button>'+(''if c['last']else'<span hidden></span>')+'</div></figure>')
parts.append('''<button id="result-row-record" style="position:fixed;right:20px;top:20px">Record ResultSummaryRow</button><script>
document.getElementById('result-row-record').onclick=async()=>{const rows=[...document.querySelectorAll('button[data-id]')].map(e=>{const r=e.getBoundingClientRect(),box=x=>{const b=x.getBoundingClientRect();return{x:b.x-r.x,y:b.y-r.y,width:b.width,height:b.height}};const cat=e.querySelector('.result-category'),s=getComputedStyle(e),chev=e.querySelector('.row-chevron');const marker=document.createElement('span'),ex=document.createElement('span');marker.style.cssText='display:inline-block;width:0;height:0;vertical-align:baseline';ex.style.cssText='position:absolute;width:0;height:1ex';chev.append(marker,ex);const fontProbe={baseline:marker.getBoundingClientRect().y-chev.getBoundingClientRect().y,xHeight:ex.getBoundingClientRect().height};marker.remove();ex.remove();return{fontProbe,id:e.dataset.id,width:r.width,height:r.height,children:[...e.children].map(box),category:box(cat),icon:box(cat.querySelector('.category-icon')),iconSvg:box(cat.querySelector('svg')),title:box(cat.querySelector('strong')),description:box(cat.querySelector('small')),chevronSvg:box(e.querySelector('.row-chevron svg')),name:e.getAttribute('aria-label'),style:{background:s.backgroundColor,borderBottom:s.borderBottomWidth,outline:s.outline,outlineOffset:s.outlineOffset,columns:s.gridTemplateColumns}}});const res=await fetch('/evidence/source-resultrow.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows})});if(!res.ok)throw Error('Record failed');document.getElementById('result-row-record').textContent='Rows recorded';};</script>''')
(o/'resultrow.html').write_text('\n'.join(parts));(r/'docs/replica/source/resultrow-cases.json').write_text(json.dumps(cases,ensure_ascii=False,indent=2)+'\n');(o/'resultrow-cases.json').write_text(json.dumps(cases,ensure_ascii=False));print(len(cases),'result rows prepared')
