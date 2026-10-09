"""Exact original bytes; new MainContent blank-padding hit diagnostic only."""
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[2];out=root/'artifacts/replica/windows-download';prefix='preflight-mainpadding';out.mkdir(parents=True,exist_ok=True)
hashes={}
for ext in ('js','css'):
 data=(root/f'docs/replica/source/original.{ext}').read_bytes();hashes[ext]=hashlib.sha256(data).hexdigest();(out/f'{prefix}-original.{ext}').write_bytes(data)
(out/f'{prefix}-inner.html').write_text(f'''<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="{prefix}-original.css"><a class="skip" href="#main">본문으로 건너뛰기</a><div id="app"></div><input id="file-open" type="file" accept=".json,application/json"><div id="dialog-host"></div><div id="toast-host" role="status" aria-live="polite"></div><div id="announcer" class="sr" role="status" aria-live="polite"></div><script src="{prefix}-original.js"></script>''')
js=r'''
const q=new URLSearchParams(location.search);document.title='Main padding original '+q.get('recording');const frame=document.querySelector('iframe'),rows=[],events=[],wait=ms=>new Promise(r=>setTimeout(r,ms));
await new Promise(r=>frame.onload=r);const w=frame.contentWindow,d=frame.contentDocument;
const box=e=>{const r=e.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height}},identity=e=>e?{tag:e.tagName,id:e.id,className:e.className,tabIndex:e.tabIndex}:null;
const ancestry=e=>{let r=[];while(e){r.push(identity(e));e=e.parentElement}return r};
function read(id){const main=d.getElementById('main'),s=w.getComputedStyle(main),b=box(main),page=main.querySelector('.page');const points=[{id:'left-padding',x:b.x+8,y:b.y+main.clientHeight/2},{id:'top-padding',x:b.x+main.clientWidth/2,y:b.y+8}];return{id,width:w.innerWidth,height:w.innerHeight,dpr:w.devicePixelRatio,mainBox:b,pageBox:box(page),main:{clientWidth:main.clientWidth,clientHeight:main.clientHeight,scrollWidth:main.scrollWidth,scrollHeight:main.scrollHeight,scrollTop:main.scrollTop,overflowX:s.overflowX,overflowY:s.overflowY,padding:s.padding,background:s.backgroundColor,tabIndex:main.tabIndex},focus:identity(d.activeElement),points:points.map(p=>{const e=d.elementFromPoint(p.x,p.y);return{...p,target:identity(e),ancestry:ancestry(e),mainAncestor:e===main||main.contains(e)}})} }
async function setup(en,width){frame.width=width;w.eval('clearInterval(timer);state=initial();state.connected=true;state.selected.add("trial");lang='+JSON.stringify(en?'en':'ko')+';go("trial");');await d.fonts.ready;await wait(400);return read((en?'en':'ko')+'-'+width)}
async function post(kind,data){const response=await fetch('/evidence/preflight-mainpadding-'+(q.get('os')||'mac')+'-'+kind+'.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(data)});if(!response.ok)throw Error('Owned POST '+response.status);return response.status}
if(q.get('interactive')==='1'){
 const row=await setup(q.get('lang')==='en',Number(q.get('width')||1280));if(q.has('resume-offset')){const value=Number(q.get('resume-offset'));if(value!==100)throw Error('Finite saved wheel resume only');d.getElementById('main').scrollTop=value;await wait(150);row.resumeSetup={scrollTop:d.getElementById('main').scrollTop,physicalWheel:false};}row.embedding={frameBox:box(frame),outerWidth:innerWidth,outerHeight:innerHeight};rows.push(row);
 const publish=()=>post('input',{phase:'ORIGINAL_TRUSTED_INPUT_OBSERVATION',recording:q.get('recording'),originalHashes:HASHES,rows,events});window.addEventListener('blur',()=>{events.push({type:'outer-window-blur',time:performance.now()});publish()});for(const type of ['pointerdown','pointerup','click','contextmenu'])document.addEventListener(type,e=>{events.push({type:'outer-'+type,isTrusted:e.isTrusted,target:identity(e.target),button:e.button,x:e.clientX,y:e.clientY});publish()},true);
 const safeRead=id=>{try{return read(id)}catch(error){return{readError:String(error),id}}};w.addEventListener('error',e=>{events.push({type:'inner-error',message:e.message});publish()});window.addEventListener('error',e=>{events.push({type:'outer-error',message:e.message});publish()});
 for(const type of ['wheel','pointerdown','pointerup','click','contextmenu'])d.addEventListener(type,e=>{const before=safeRead('before-'+type),event={type,button:e.button,isTrusted:e.isTrusted,target:identity(e.target),ancestry:ancestry(e.target),x:e.clientX,y:e.clientY,deltaY:e.deltaY,before};events.push(event);publish();setTimeout(async()=>{event.after=safeRead('after-'+type);await post('input',{phase:'ORIGINAL_TRUSTED_INPUT_OBSERVATION',recording:q.get('recording'),originalHashes:HASHES,rows,events})},650)},true);
 await post('control-ready',{phase:'ORIGINAL_READY',recording:q.get('recording'),originalHashes:HASHES,rows});document.getElementById('result').textContent='Ready '+row.id;
 window.paddingState=()=>read('current');
}else{
 for(const en of [false,true])for(const width of [960,1280])rows.push(await setup(en,width));
 await post('source',{phase:'ORIGINAL_DOM_ONLY',scope:'No dispatched wheel/click; no trusted input claim',recording:q.get('recording'),originalHashes:HASHES,rows});frame.remove();document.getElementById('result').textContent='Recorded '+rows.length;
}
'''.replace('HASHES',json.dumps(hashes))
html=f'''<!doctype html><meta charset="utf-8"><title>Main padding original · preflight</title><p id="result">Measuring</p><iframe width="1280" height="900" style="border:0" src="{prefix}-inner.html"></iframe><script>(async()=>{{try{{{js}}}catch(e){{document.getElementById('result').textContent='FAILED '+e.message;throw e}}}})();</script>'''
(out/f'{prefix}.html').write_text(html)
(root/'docs/replica/source/preflight-mainpadding-fixture.json').write_text(json.dumps({'fixture':prefix+'.html','originalHashes':hashes,'cases':['ko-960','ko-1280','en-960','en-1280'],'height':900,'scope':'Blank left/top padding only; source bytes unchanged; DOM does not prove trusted input'},indent=2)+'\n')
print('Prepared '+prefix+'.html')
