"""Reproduce recorded component-only .step endpoint cases, original CSS untouched."""
import json,re
from pathlib import Path
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/windows-download';o.mkdir(exist_ok=True,parents=True)
cs=json.loads((r/'docs/replica/source/sidebaritem-cases.json').read_text());css=(r/'docs/replica/source/original.css').read_text();rules=''
for m in re.finditer(r'([^{}]+)\{([^}]*)\}',css):
 selector=m.group(1)
 if any(x in selector for x in [':hover',':active',':focus-visible']):rules+=re.sub(r':(hover|active|focus-visible)',lambda x:'.probe-'+x.group(1),selector)+'{'+m.group(2)+'}\n'
(o/'sidebaritem-original.css').write_text(css+'\n'+rules);(o/'sidebaritem-cases.json').write_text(json.dumps(cs,ensure_ascii=False))
parts=['<!doctype html><meta charset="utf-8"><title>SidebarItem · original isolation</title><link rel="stylesheet" href="sidebaritem-original.css"><style>body{width:800px;height:18000px}.fixture{position:absolute}.fixture>.step{transition:none!important;pointer-events:none}</style>']
icon='<svg class="svg" viewBox="0 0 24 24" aria-hidden="true"><path d="m5 12 4 4L19 6"></path></svg>'
for i,c in enumerate(cs):
 child=icon if c['state']=='done' else str(c['index']);pseudo={'hover':'probe-hover','pressed':'probe-active','focus-visible':'probe-focus-visible'}.get(c['interaction'],'')
 parts.append(f'<div class="fixture" style="left:30px;top:{30+i*110}px"><button type="button" id="{c["id"]}" class="step {c["state"] if c["state"]!="normal" else ""} {pseudo}" style="width:{c["width"]}px" {"disabled" if c["disabled"] else ""}><span class="step-index">{child}</span><span>{c["label"]}</span></button></div>')
parts.append('''<button type="button" id="sidebaritem-record" style="position:fixed;right:20px;top:20px">Record SidebarItem</button><script>function measurements(){return [...document.querySelectorAll('button.step')].map(e=>{const r=e.getBoundingClientRect(),s=getComputedStyle(e);function box(c){const t=c.getBoundingClientRect();return{x:t.x-r.x,y:t.y-r.y,width:t.width,height:t.height}}return{id:e.id,width:r.width,height:r.height,badge:box(e.firstElementChild),label:box(e.lastElementChild),style:{bg:s.backgroundColor,color:s.color,border:s.borderColor,radius:s.borderRadius,padding:s.padding,gap:s.gap,fontSize:s.fontSize,fontWeight:s.fontWeight,shadow:s.boxShadow,transform:s.transform,outline:s.outline,outlineOffset:s.outlineOffset}}})}
document.getElementById('sidebaritem-record').onclick=async()=>{const response=await fetch('/evidence/source-sidebaritem.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows:measurements()})});if(!response.ok)throw Error('Example evidence failed');document.getElementById('sidebaritem-record').textContent='SidebarItem recorded';};</script>''')
(o/'sidebaritem.html').write_text('\n'.join(parts));print(len(cs),'SidebarItem cases')
