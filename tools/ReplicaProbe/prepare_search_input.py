"""Prepare the original search-field's direct context and example-only evidence."""
from pathlib import Path
import json,html
root=Path(__file__).resolve().parents[2];out=root/'artifacts/replica/windows-download'
css=(root/'docs/replica/source/original.css').read_text();(out/'searchinput-original.css').write_text(css)
cases=[]
for context in ['tables','matching']:
 for lang in ['ko','en']:
  for width in [160,240,380,452]:
   for state,value in [('empty',''),('value','orders'),('space',' ')]:
    label=({'ko':'테이블 검색','en':'Search tables'} if context=='tables' else {'ko':'대상 테이블 검색','en':'Search target tables'})[lang]
    placeholder=({'ko':'테이블 이름으로 검색','en':'Search by table name'} if context=='tables' else {'ko':'대상 테이블 이름 검색','en':'Search target table names'})[lang]
    clear={'ko':'테이블 검색 지우기','en':'Clear table search'}[lang]
    cases.append(dict(id=f'{context}-{lang}-{width}-{state}',context=context,lang=lang,width=width,value=value,label=label,placeholder=placeholder,clear=clear))
(root/'docs/replica/source/searchinput-cases.json').write_text(json.dumps(cases,ensure_ascii=False,indent=2)+'\n');(out/'searchinput-cases.json').write_text(json.dumps(cases,ensure_ascii=False))
parts=['<!doctype html><meta charset="utf-8"><title>SearchInput · original isolation</title><link rel="stylesheet" href="searchinput-original.css"><style>body{width:1000px;position:relative;height:5400px}.fixture-cell{position:absolute}.fixture-cell .table-toolbar{margin:0}.fixture-cell .match-browser{margin:0}</style>']
icon='<svg class="svg" viewBox="0 0 24 24" aria-hidden="true"><circle cx="10" cy="10" r="6"></circle><path d="m15 15 6 6"></path></svg>'
close='<svg class="svg" viewBox="0 0 24 24" aria-hidden="true"><path d="m6 6 12 12M6 18 18 6"></path></svg>'
for i,c in enumerate(cases):
 field=f'<div class="search-field" data-id="{c["id"]}">{icon}<label class="sr" for="{c["id"]}">{c["label"]}</label><input type="search" id="{c["id"]}" placeholder="{c["placeholder"]}" value="{html.escape(c["value"])}" autocomplete="off">'
 if c['context']=='tables':field+=f'<button type="button" id="searchclear-{c["id"]}" class="btn quiet icon-button" aria-label="{c["clear"]}" data-clear="{c["id"]}" {"disabled" if not c["value"] else ""}>{close}</button>'
 field+='</div>';parent='table-toolbar' if c['context']=='tables' else 'match-browser'
 parts.append(f'<div class="fixture-cell" style="left:30px;top:{30+i*76}px;width:{c["width"]}px"><div class="{parent}">{field}</div></div>')
parts.append('''<button type="button" id="searchinput-record" style="position:fixed;right:20px;top:20px">Record SearchInput</button><script>
function measurements(){return [...document.querySelectorAll('.search-field[data-id]')].map(f=>{const r=f.getBoundingClientRect(),i=f.querySelector('input'),q=f.querySelector('svg'),b=f.querySelector('button'),s=getComputedStyle(i);function rect(e){if(!e)return null;const t=e.getBoundingClientRect();return{x:t.x-r.x,y:t.y-r.y,width:t.width,height:t.height}}return{id:f.dataset.id,width:r.width,height:r.height,input:rect(i),icon:rect(q),clear:rect(b),value:i.value,placeholder:i.placeholder,label:f.querySelector('label').textContent,clearName:b?.getAttribute('aria-label'),clearDisabled:b?.disabled,style:{padding:s.padding,fontSize:s.fontSize,lineHeight:s.lineHeight,bg:s.backgroundColor,color:s.color,border:s.borderColor,radius:s.borderRadius}}})}
const events=[];function recordEvent(i,kind){const b=i.parentElement.querySelector('button');events.push({id:i.id,kind,value:i.value,focus:document.activeElement.id,clearDisabled:b?.disabled})}
document.addEventListener('input',e=>{if(e.target.type!=='search')return;const b=e.target.parentElement.querySelector('button');if(b)b.disabled=!e.target.value;recordEvent(e.target,'input')});
document.addEventListener('keyup',e=>{if(e.target.type==='search')recordEvent(e.target,'keyup-'+e.key)});
document.addEventListener('click',e=>{const b=e.target.closest('button[data-clear]');if(!b||b.disabled)return;const i=document.getElementById(b.dataset.clear);i.value='';b.disabled=true;i.focus();recordEvent(i,'clear')});
document.getElementById('searchinput-record').onclick=async()=>{const response=await fetch('/evidence/source-searchinput.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows:measurements(),events})});if(!response.ok)throw Error('Example evidence failed');document.getElementById('searchinput-record').textContent='SearchInput recorded';};
</script>''');(out/'searchinput.html').write_text('\n'.join(parts));print(len(cases),'search-field cases')
p=root/'tools/WindowsControl/fixture_server.py';s=p.read_text();s=s.replace("allowed={'source-iconbutton.json'","allowed={'source-searchinput.json','source-iconbutton.json'");p.write_text(s)
