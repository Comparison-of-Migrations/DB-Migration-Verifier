"""New Results application parent oracle; literal original JS/CSS remain unchanged."""
from pathlib import Path
import hashlib,json

root=Path(__file__).resolve().parents[2]
out=root/'artifacts/replica/windows-download'
out.mkdir(parents=True,exist_ok=True)
prefix='connection-pageintegrationresults'
cases=[dict(id=f'{"en" if english else "ko"}-{width}',english=english,width=width,height=900,screen="results") for english in [False,True] for width in [1000,1280]]
hashes={}
for ext in ['js','css']:
    data=(root/f'docs/replica/source/original.{ext}').read_bytes()
    (out/f'{prefix}-original.{ext}').write_bytes(data)
    hashes[ext]=hashlib.sha256(data).hexdigest()
(root/'docs/replica/source/pageintegrationresults-cases.json').write_text(json.dumps(cases,indent=2)+'\n')
inner='''<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="PREFIX-original.css"><a class="skip" href="#main">본문으로 건너뛰기</a><div id="app"></div><input id="file-open" type="file" accept=".json,application/json"><div id="dialog-host"></div><div id="toast-host" role="status" aria-live="polite"></div><div id="announcer" class="sr" role="status" aria-live="polite"></div><script src="PREFIX-original.js"></script><script>
const input=new URLSearchParams(location.search),rows=[],exports=[];
// The output consumer intercepts sample download bytes; no file request/disk-success claim.
downloadFile=(name,body,mime)=>exports.push({name,body,mime});
const settle=async()=>{await document.fonts.ready;await new Promise(r=>setTimeout(r,360));await Promise.all(document.getAnimations().filter(a=>a.effect.getTiming().iterations!==Infinity).map(a=>a.finished.catch(()=>{})))};
const box=e=>{if(!e)return null;const r=e.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height}};
function normalizedReport(){const r=report();r.run.id='UI-FLOWRESULTS';return r}
function snapshot(label){const focused=document.activeElement,d=document.querySelector('dialog'),main=document.getElementById('main');rows.push({label,root:{innerWidth,innerHeight,clientWidth:document.documentElement.clientWidth,clientHeight:document.documentElement.clientHeight,scrollWidth:document.documentElement.scrollWidth,scrollHeight:document.documentElement.scrollHeight,scrollX,scrollY,announcer:{className:document.getElementById('announcer').className,role:document.getElementById('announcer').getAttribute('role'),position:getComputedStyle(document.getElementById('announcer')).position,box:box(document.getElementById('announcer'))}},screen:state.screen,english:lang==='en',filter:state.filter,focus:focused?.id||focused?.dataset.action||null,scroll:main?.scrollTop||0,visited:[...state.visited],title:document.getElementById('screen-title')?.textContent,announcement:document.getElementById('announcer').textContent,articleIds:[...document.querySelectorAll('.finding-top>.mono')].map(e=>e.textContent),settled:document.querySelector('.page')?.classList.contains('settled'),pageOpacity:Number(getComputedStyle(document.querySelector('.page')).opacity),modal:d?{type:modal.type,payload:modal.payload,title:d.querySelector('#dialog-title').textContent,body:d.querySelector('.dialog-body').textContent,checks:[...d.querySelectorAll('.dialog-body>ul>li')].map(e=>e.textContent),notices:[...d.querySelectorAll('.dialog-body>.notice')].map(e=>({title:e.querySelector('strong').textContent,text:e.querySelector('p').textContent})),opener:modal.opener?.id||modal.opener?.dataset.action||null,openerConnected:!!modal.opener?.isConnected,open:d.open}:null,report:state.run?normalizedReport():null,boxes:{content:box(main),page:box(document.querySelector('.page')),footer:box(document.querySelector('.footer')),heading:box(document.querySelector('.page-heading')),filter:box(document.querySelector('.filter-row')),findings:box(document.querySelector('.finding-list'))},footer:[...document.querySelectorAll('.footer [data-action]')].map(e=>({action:e.dataset.action,text:e.textContent,disabled:e.disabled,box:box(e)}))})}
async function action(key,startLabel){const b=document.querySelector('[data-action="'+key+'"]');if(!b||b.disabled)throw Error('Exact original action unavailable: '+key);b.focus({preventScroll:true});b.click();if(startLabel)snapshot(startLabel);await settle()}
window.recordResultsWrapperGeometry=async()=>{
rows.length=exports.length=0;state=initial();lang=input.get('lang');state.selected=new Set(categoryDefs.map(c=>c.key));render();go('connect');go('home');state.run=makeRun(true);state.run.groups.forEach(completeGroup);state.run.complete=true;go('results');await settle();snapshot('sample-go');
return{id:input.get('id'),reduced:matchMedia('(prefers-reduced-motion: reduce)').matches,viewport:{width:innerWidth,height:innerHeight},rows,scope:'Corrected original wrapper geometry only; existing68 logical states/report/export evidence reused'}
};
window.recordResultsIntegration=async()=>{
rows.length=exports.length=0;state=initial();lang=input.get('lang');state.selected=new Set(categoryDefs.map(c=>c.key));render();go('connect');go('home');await settle();
await action('sample','sample-go-start');snapshot('sample-go');
document.getElementById('main').scrollTop=100;await action('filter:unknown');snapshot('filter-unknown');
render();await settle();snapshot('ordinary-filter-render');
await action('language');snapshot('language-filtered');
document.getElementById('main').scrollTop=0;await action('detail:data');snapshot('detail-data-open');
closeModal();await settle();snapshot('detail-data-close');
await action('detail:trial');snapshot('detail-trial-open');
closeModal();await settle();snapshot('detail-trial-close');
await action('research');snapshot('research-open');
closeModal();await settle();snapshot('research-close');
await action('detail:data');lang=lang==='ko'?'en':'ko';render();const old=modal;modal=null;document.getElementById('dialog-host').replaceChildren();openModal(old.type,old.payload);await settle();snapshot('modal-language-recreated');
closeModal();await settle();snapshot('modal-language-close');
await action('save');snapshot('save-go');await action('back');snapshot('save-back-results');
await action('filter:unknown');await action('save');state.formats=new Set(['json','csv','html']);download();await settle();snapshot('download-requested');
const normalized=exports.map(x=>({...x,name:x.name.replace(state.run.id,'UI-FLOWRESULTS'),body:x.body.replaceAll(state.run.id,'UI-FLOWRESULTS')}));
const saved=normalizedReport();await openFile(new File([JSON.stringify(saved)],'sample.json',{type:'application/json'}));await settle();snapshot('reopen-results');
return{id:input.get('id'),reduced:matchMedia('(prefers-reduced-motion: reduce)').matches,viewport:{width:innerWidth,height:innerHeight},rows,exports:normalized,scope:'Programmatic original action/focus runtime oracle; not actualOS trusted input'}
};
</script>'''.replace('PREFIX',prefix)
(out/f'{prefix}-inner.html').write_text(inner)
frames=''.join(f'<iframe title="{c["id"]}" width="{c["width"]}" height="{c["height"]}" src="{prefix}-inner.html?id={c["id"]}&lang={"en" if c["english"] else "ko"}" style="border:0;position:absolute;left:0;top:50px;visibility:hidden"></iframe>' for c in cases)
script='''const query=new URLSearchParams(location.search),wrapperOnly=query.get('scope')==='wrapper';document.getElementById('record').onclick=async()=>{const rows=[];for(const f of document.querySelectorAll('iframe')){f.style.visibility='visible';rows.push(await (wrapperOnly?f.contentWindow.recordResultsWrapperGeometry():f.contentWindow.recordResultsIntegration()));f.style.visibility='hidden'}document.querySelector('iframe').style.visibility='visible';const os=query.get('os')==='windows'?'windows':'mac';const response=await fetch('/evidence/connection-pageintegrationresults-'+os+'-'+(wrapperOnly?'control-ready':'source')+'.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({recording:query.get('recording'),dpr:devicePixelRatio,originalHashes:HASHES,scope:wrapperOnly?'corrected-wrapper-geometry-only':'original-full-flow',rows})});if(!response.ok)throw Error('Owned Results integration record failed');document.getElementById('record').textContent='Results integration recorded'};if(query.get('auto')==='1')window.addEventListener('load',()=>document.getElementById('record').click());'''.replace('HASHES',json.dumps(hashes))
(out/f'{prefix}.html').write_text('<!doctype html><meta charset="utf-8"><title>Results integration · original isolation</title><button id="record">Record Results integration</button>'+frames+'<script>'+script+'</script>')


actual_inner_extra='''
let trustedTrace=[];
document.addEventListener('pointerdown',e=>trustedTrace.push({kind:'down',trusted:e.isTrusted,action:e.target.closest('[data-action]')?.dataset.action||null,point:{x:e.clientX,y:e.clientY},focus:document.activeElement?.id||document.activeElement?.dataset.action||null}),true);
document.addEventListener('pointerup',e=>trustedTrace.push({kind:'up',trusted:e.isTrusted,action:e.target.closest('[data-action]')?.dataset.action||null}),true);
document.addEventListener('keydown',e=>{if(e.key==='F12'){e.preventDefault();window.parent.recordActualState();return}if(e.key==='F8'){e.preventDefault();window.parent.nextActualContext();return}if(['F5','F6','F7'].includes(e.key)){e.preventDefault();if(state.screen==='results'){document.getElementById('main').scrollTop=e.key==='F5'?0:400;trustedTrace.push({kind:'setup-offset',key:e.key,trusted:e.isTrusted});window.parent.recordActualState()}return}trustedTrace.push({kind:'key',key:e.key,trusted:e.isTrusted,focus:document.activeElement?.id||document.activeElement?.dataset.action||null})},true);
window.prepareActualResults=async()=>{rows.length=0;trustedTrace=[];state=initial();lang=input.get('lang');state.selected=new Set(categoryDefs.map(c=>c.key));render();go('connect');go('home');if(input.get('id')==='ko-1280'&&new URLSearchParams(window.parent.location.search).get('resume')==='research'){state.run=makeRun(true);state.run.groups.forEach(completeGroup);state.run.complete=true;go('results');lang='en';state.filter='unknown';render();document.getElementById('main').scrollTop=400;document.querySelector('[data-action=\"detail:trial\"]').focus({preventScroll:true})}if(input.get('id')==='ko-1280'&&new URLSearchParams(window.parent.location.search).get('resume')==='after-data'){state.run=makeRun(true);state.run.groups.forEach(completeGroup);state.run.complete=true;go('results');lang='en';state.filter='unknown';render();document.getElementById('main').scrollTop=0;document.querySelector('[data-action=\"detail:data\"]').focus({preventScroll:true})}await settle()};
window.observeActualResults=()=>{rows.length=0;snapshot('actual');rows[0].modalBox=box(document.querySelector('dialog'));rows[0].focusVisible=!!document.activeElement?.matches(':focus-visible');rows[0].backgroundEnabled=!document.getElementById('app').inert&&!document.querySelector('dialog[open]');return{state:rows[0],controls:[...document.querySelectorAll('[data-action],#screen-title')].map(e=>({identity:e.id||e.dataset.action,action:e.dataset.action||null,text:e.textContent,name:e.getAttribute('aria-label')||e.innerText,box:box(e),visible:!!(e.getBoundingClientRect().width&&e.getBoundingClientRect().height),enabled:!e.disabled&&!e.closest('[inert]'),focused:e===document.activeElement})),trace:trustedTrace}};
'''
actual_inner=inner.rsplit('</script>',1)[0]+actual_inner_extra+'</script>'
(out/f'{prefix}-actual-inner.html').write_text(actual_inner)
actual_frames=''.join(f'<iframe title="{c["id"]}" width="{c["width"]}" height="{c["height"]}" src="{prefix}-actual-inner.html?id={c["id"]}&lang={"en" if c["english"] else "ko"}" style="border:0;position:absolute;left:0;top:0;visibility:hidden"></iframe>' for c in cases if c['width']==1280)
actual_script='''let index=-1,snapshot=0,revision=0,publishing=false;const q=new URLSearchParams(location.search),os=q.get('os')==='windows'?'windows':'mac';if(q.get('resume')==='english')index=0;
window.recordActualState=async()=>{const f=document.querySelectorAll('iframe')[index];if(!f||publishing)return;publishing=true;try{const state={recording:q.get('recording'),snapshot:++snapshot,revision,index,caseId:f.title,dpr:devicePixelRatio,headless:false,viewport:{width:f.contentWindow.innerWidth,height:f.contentWindow.innerHeight},...f.contentWindow.observeActualResults(),scope:'Literal original trustedOS actions observed; F8 context seed is setup; timer DOM observation changes no focus/input. F12 debugging key is outside pointer-state evidence'};const response=await fetch('/evidence/connection-pageintegrationresults-'+os+'-input.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(state)});if(!response.ok)throw Error('Owned actual source record failed')}finally{publishing=false}};
window.nextActualContext=async()=>{const fs=document.querySelectorAll('iframe');if(index>=fs.length-1)return;if(index>=0)fs[index].style.visibility='hidden';const f=fs[++index];++revision;f.style.visibility='visible';await f.contentWindow.prepareActualResults();await recordActualState();const response=await fetch('/evidence/connection-pageintegrationresults-'+os+'-control-ready.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({phase:'ACTUAL_SOURCE_READY',index,revision,caseId:f.title,dpr:devicePixelRatio,scope:'KOEN1280 new App callback boundary only; source geometry4 already proven; F8 seeds next, F12 readonly observe'})});if(!response.ok)throw Error('Owned actual source readiness failed');f.contentWindow.focus()};window.addEventListener('load',async()=>{await nextActualContext();setInterval(()=>recordActualState(),100)});'''
(out/f'{prefix}-actual.html').write_text('<!doctype html><meta charset="utf-8"><title>Results integration actual · original isolation</title>'+actual_frames+'<script>'+actual_script+'</script>')

print('Prepared',len(cases),'new literal full-shell Results contexts; original hashes',hashes)

# Only newly changed invisible status text layout; no prior callback flow replay.
marker_extra='''
window.recordMarkerLayout=async()=>{
state=initial();lang=input.get('lang');state.selected=new Set(categoryDefs.map(c=>c.key));render();state.run=makeRun(true);state.run.groups.forEach(completeGroup);state.run.complete=true;go('results');await settle();
const e=document.getElementById('announcer'),node=e.firstChild,style=getComputedStyle(e),lines=[];
for(let i=0;i<node.textContent.length;i++){const r=document.createRange();r.setStart(node,i);r.setEnd(node,i+1);const rect=r.getBoundingClientRect();if(rect.width<=0)continue;let line=lines.find(l=>Math.abs(l.box.y-rect.y)<.01);if(!line){line={text:'',box:{x:rect.x,y:rect.y,width:rect.width,height:rect.height}};lines.push(line)}line.text+=node.textContent[i];const right=Math.max(line.box.x+line.box.width,rect.right);line.box.x=Math.min(line.box.x,rect.x);line.box.width=right-line.box.x;line.box.height=Math.max(line.box.height,rect.height)}
return{id:input.get('id'),text:e.textContent,lineTexts:lines.map(l=>l.text.trim()),lineRects:lines.map(l=>l.box),parentBox:box(e),fontFamily:style.fontFamily,fontSize:Number.parseFloat(style.fontSize),lineHeight:Number.parseFloat(style.lineHeight),whiteSpace:style.whiteSpace,overflowWrap:style.overflowWrap,wordBreak:style.wordBreak,scope:'Only .sr text DOMRange layout; APIsample setup, no trusted callback replay'};
};
'''
marker_inner=inner.rsplit('</script>',1)[0]+marker_extra+'</script>'
(out/f'{prefix}-marker-inner.html').write_text(marker_inner)
marker_frames=''.join(f'<iframe title="{c["id"]}" width="1280" height="900" src="{prefix}-marker-inner.html?id={c["id"]}&lang={"en" if c["english"] else "ko"}" style="border:0;position:absolute;left:0;top:0;visibility:hidden"></iframe>' for c in cases if c['width']==1280)
marker_script='''const q=new URLSearchParams(location.search);window.addEventListener('load',async()=>{const rows=[];for(const f of document.querySelectorAll('iframe')){f.style.visibility='visible';rows.push(await f.contentWindow.recordMarkerLayout());f.style.visibility='hidden'}const os=q.get('os')==='windows'?'windows':'mac';const response=await fetch('/evidence/connection-pageintegrationresults-'+os+'-control-ready.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({recording:q.get('recording'),dpr:devicePixelRatio,originalHashes:HASHES,scope:'marker-text-layout-only',rows})});if(!response.ok)throw Error('Marker layout record failed');document.title='Results marker layout · original isolation'});'''.replace('HASHES',json.dumps(hashes))
(out/f'{prefix}-marker.html').write_text('<!doctype html><meta charset="utf-8"><title>Results marker layout · original isolation</title>'+marker_frames+'<script>'+marker_script+'</script>')
