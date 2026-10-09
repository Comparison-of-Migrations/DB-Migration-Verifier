"""Original step-index and direct step parent, example-only layout fixture."""
from pathlib import Path
import json
root=Path(__file__).resolve().parents[2];out=root/'artifacts/replica/windows-download'
(out/'stepbadge-original.css').write_text((root/'docs/replica/source/original.css').read_text())
cases=[]
for lang in ['ko','en']:
 for state in ['normal','active','done']:
  for disabled in [False,True]:
   for number in (range(1,8) if state!='done' else [1]):
    cases.append(dict(id=f'{lang}-{state}-{int(disabled)}-{number}',lang=lang,state=state,disabled=disabled,number=number))
for lang in ['ko','en']:
 for state in ['normal','active','done']:
  for j,color in enumerate(['#b5bac1','#bec3cb','#cdd0d6']):cases.append(dict(id=f'{lang}-{state}-inherit-{j}',lang=lang,state=state,disabled=False,number=1,inheritedColor=color))
for dest in [root/'docs/replica/source/stepbadge-cases.json',out/'stepbadge-cases.json']:dest.write_text(json.dumps(cases,indent=2)+'\n')
parts=['<!doctype html><meta charset="utf-8"><title>StepBadge · original isolation</title><link rel="stylesheet" href="stepbadge-original.css"><style>body{width:720px;height:4100px}.fixture{position:absolute}.fixture>.step{width:196px}</style>']
icon='<svg class="svg" viewBox="0 0 24 24" aria-hidden="true"><path d="m5 12 4 4L19 6"></path></svg>'
for i,c in enumerate(cases):
 child=icon if c['state']=='done' else f'<span data-digit>{c["number"]}</span>'
 parts.append(f'<div class="fixture" style="left:30px;top:{30+i*60}px" lang="{c["lang"]}"><button type="button" style="color:{c.get('inheritedColor','')}" class="step {c["state"] if c["state"]!="normal" else ""}" {"disabled" if c["disabled"] else ""}><span class="step-index" data-id="{c["id"]}">{child}</span><span>DB</span></button></div>')
parts.append('''<button type="button" id="stepbadge-record" style="position:fixed;right:20px;top:20px">Record StepBadge</button><script>
function measurements(){return [...document.querySelectorAll('.step-index')].map(e=>{const r=e.getBoundingClientRect(),s=getComputedStyle(e),p=e.parentElement.getBoundingClientRect(),c=e.firstElementChild,q=c.getBoundingClientRect();return{id:e.dataset.id,rect:{width:r.width,height:r.height},parent:{x:r.x-p.x,y:r.y-p.y,width:p.width,height:p.height},child:{x:q.x-r.x,y:q.y-r.y,width:q.width,height:q.height},kind:c.hasAttribute('data-digit')?'digit':'check',text:c.textContent,style:{bg:s.backgroundColor,color:s.color,border:s.borderColor,radius:s.borderRadius,borderWidth:s.borderWidth,fontSize:s.fontSize,fontWeight:s.fontWeight,lineHeight:s.lineHeight,transitionDuration:s.transitionDuration}}})}
document.getElementById('stepbadge-record').onclick=async()=>{const response=await fetch('/evidence/source-stepbadge.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows:measurements()})});if(!response.ok)throw Error('Example evidence failed');document.getElementById('stepbadge-record').textContent='StepBadge recorded';};
</script>''')
(out/'stepbadge.html').write_text('\n'.join(parts));print(len(cases),'StepBadge cases')
