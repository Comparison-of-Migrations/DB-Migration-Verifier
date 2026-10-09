"""Literal full Review/Save summary content oracle; bytes and text remain original."""
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[2];out=root/'artifacts/replica/windows-download';out.mkdir(exist_ok=True)
prefix='home-pageintegrationsummarycontent'
hashes={}
for ext in ('js','css'):
 data=(root/f'docs/replica/source/original.{ext}').read_bytes();(out/f'{prefix}-original.{ext}').write_bytes(data);hashes[ext]=hashlib.sha256(data).hexdigest()
inner=f'''<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="{prefix}-original.css"><a class="skip" href="#main">본문으로 건너뛰기</a><div id="app"></div><input id="file-open" type="file" accept=".json,application/json"><div id="dialog-host"></div><div id="toast-host" role="status" aria-live="polite"></div><div id="announcer" class="sr" role="status" aria-live="polite"></div><script src="{prefix}-original.js"></script>'''
(out/f'{prefix}-inner.html').write_text(inner)
js=r'''
const frame=document.querySelector('iframe'),w=frame.contentWindow,d=frame.contentDocument,q=new URLSearchParams(location.search),rows=[];
const wait=ms=>new Promise(r=>setTimeout(r,ms));
const box=e=>{const b=e.getBoundingClientRect(),p=d.querySelector('.page').getBoundingClientRect();return{x:b.x-p.x,y:b.y-p.y,width:b.width,height:b.height}};
function textLines(e){const groups=[],walker=d.createTreeWalker(e,NodeFilter.SHOW_TEXT);let node;
 while(node=walker.nextNode())for(let i=0;i<node.length;i++){const r=d.createRange();r.setStart(node,i);r.setEnd(node,i+1);const b=r.getBoundingClientRect();if(!b.height)continue;let line=groups.find(v=>Math.abs(v.y-b.y)<.1);if(!line){line={y:b.y,text:'',x:b.x,right:b.right};groups.push(line)}line.text+=node.data[i];line.x=Math.min(line.x,b.x);line.right=Math.max(line.right,b.right)}
 return groups.map(l=>l.text.trimEnd());
}
function observe(mode,language,width){const summary=d.querySelector('.summary-list'),main=d.getElementById('main'),footer=d.querySelector('.footer'),page=d.querySelector('.page'),style=w.getComputedStyle(main);return{id:mode+'-'+language+'-'+width,mode,language,width,actualWidth:w.innerWidth,height:w.innerHeight,document:{clientWidth:d.documentElement.clientWidth,clientHeight:d.documentElement.clientHeight,scrollWidth:d.documentElement.scrollWidth,scrollHeight:d.documentElement.scrollHeight},parentDiagnostic:{content:box(main),clientWidth:main.clientWidth,clientHeight:main.clientHeight,scrollWidth:main.scrollWidth,scrollHeight:main.scrollHeight,offsetWidth:main.offsetWidth,offsetHeight:main.offsetHeight,padding:style.padding,page:box(page),footer:box(footer),footerText:footer.textContent,bodyClientWidth:d.body.clientWidth,bodyScrollHeight:d.body.scrollHeight},run:w.eval('state.run?({complete:state.run.complete,interrupted:state.run.interrupted,groups:state.run.groups.map(g=>({key:g.key,status:g.status}))}):null'),summary:box(summary),items:[...summary.children].map(row=>{const label=row.children[0],value=row.children[1],suffix=value.querySelector('small'),font=w.getComputedStyle(value);return{label:label.textContent,value:value.textContent,logicalLines:[...value.childNodes].filter(n=>n.nodeType!==1||n.tagName!=='BR').map(n=>n.textContent),strong:value.tagName==='STRONG',mono:value.classList.contains('mono'),suffix:suffix?.textContent||'',labelLines:textLines(label),valueLines:textLines(value),labelFont:{size:parseFloat(w.getComputedStyle(label).fontSize),weight:Number(w.getComputedStyle(label).fontWeight)},valueFont:{size:parseFloat(font.fontSize),weight:Number(font.fontWeight)},boxes:{row:box(row),label:box(label),value:box(value)}}})};}
for(const language of ['ko','en'])for(const mode of ['review-default','review-trial','save-completed','save-interrupted','save-pending']){
 if(q.get('target')&&!q.get('target').startsWith(mode+'-'+language+'-'))continue;
 frame.width=1280;w.eval('clearInterval(timer);lang='+JSON.stringify(language)+';state=initial();');
 if(mode==='review-trial')w.eval('state.selected.add("trial");state.trial.ready=true;state.trial.consent=true;');
 if(mode.startsWith('save-'))w.eval('state.run=makeRun(true);state.run.id="UI-SUMMARY";'+(mode==='save-completed'?'state.run.groups.forEach(completeGroup);state.run.complete=true;':mode==='save-interrupted'?'state.run.interrupted=true;':'')+'state.visited=new Set(["results","save"]);go("results");go("save");');
 else w.eval('state.connected=true;go("review");');
 await d.fonts.ready;await wait(400);
 for(const width of [960,1180,1280]){if(q.get('target')&&q.get('target')!==mode+'-'+language+'-'+width)continue;frame.width=width;await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));await wait(80);rows.push(observe(mode,language,width));}
}
const result={scope:'summary-content30',recording:q.get('recording'),dpr:w.devicePixelRatio,originalHashes:HASHES,rows};const response=await fetch('/evidence/home-pageintegrationsummarycontent-'+(q.get('os')||'mac')+'-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(result)});if(!response.ok)throw Error('Owned POST '+response.status);document.getElementById('result').textContent='Recorded '+rows.length;
'''.replace('HASHES',json.dumps(hashes))
html=f'''<!doctype html><meta charset="utf-8"><title>Summary content oracle · home</title><p id="result">Measuring</p><iframe width="1280" height="900" style="border:0" src="{prefix}-inner.html"></iframe><script>window.onload=async()=>{{try{{{js}}}catch(e){{document.getElementById('result').textContent='FAILED '+e.message;throw e}}}};</script>'''
(out/f'{prefix}.html').write_text(html)
axjs=r'''
const frame=document.querySelector('iframe'),q=new URLSearchParams(location.search),language=q.get('lang'),width=Number(q.get('width'));
if(!['ko','en'].includes(language)||![960,1280].includes(width))throw Error('Finite source AX context required');
frame.width=width;const w=frame.contentWindow,d=frame.contentDocument;w.eval('clearInterval(timer);lang='+JSON.stringify(language)+';state=initial();go("review");');await d.fonts.ready;await new Promise(r=>setTimeout(r,400));
const mono=d.querySelector('.summary-item .mono'),small=mono.querySelector('small');
window.sourceSummaryAxLiteral={recording:q.get('recording'),language,width,fullText:mono.textContent,prefix:mono.firstChild.textContent,suffix:small.textContent,childNodes:[...mono.childNodes].map(n=>({nodeType:n.nodeType,tag:n.nodeType===1?n.tagName:null,text:n.textContent})),fullCombinedOccurrence:[...mono.childNodes].filter(n=>n.textContent===mono.textContent).length};
document.getElementById('result').textContent='Ready original summary AX '+language+' '+width;
'''
(out/f'{prefix}-ax.html').write_text(f'''<!doctype html><meta charset="utf-8"><title>Original summary AX oracle · home</title><p id="result">Preparing</p><iframe title="Original Review content" width="1280" height="900" style="border:0" src="{prefix}-inner.html"></iframe><script>window.onload=async()=>{{try{{{axjs}}}catch(e){{document.getElementById('result').textContent='FAILED '+e.message;throw e}}}};</script>''')
(root/'docs/replica/source/home-pageintegrationsummarycontent-fixture.json').write_text(json.dumps({'scope':'summary-content30','originalHashes':hashes,'fixture':prefix+'.html','cases':30},indent=2)+'\n')
print('Prepared literal summary content30')
