"""Isolate original running/updateProgress for trusted OS input, without rewriting CSS/JS."""
from pathlib import Path
r=Path(__file__).resolve().parents[2];f=r/'artifacts/replica/windows-download'
s='''<!doctype html><meta charset="utf-8"><title>Progress input · original isolation</title>
<button id="progress-tick">Record first live update</button><button id="progress-language">Language</button><button id="progress-record">Record current state</button>
<iframe id="progress-frame" width="1280" height="900" style="position:absolute;left:0;top:50px;border:0" src="progresspage-inner.html?index=16"></iframe><script>
const frame=document.getElementById('progress-frame');let phase='initial',revision=0,frames=[];
function view(){const w=frame.contentWindow,d=w.document,run=w.ProgressInputBridge.run(),rows=[...d.querySelectorAll('tbody tr')];rows.forEach((row,i)=>row.id='Progress-source-row-'+run.groups[i].key);const b=e=>{const r=e.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height}};return {phase,dpr:devicePixelRatio,lang:w.ProgressInputBridge.language(),run,rows:rows.map(row=>({id:row.id,box:b(row),hover:row.matches(':hover'),background:getComputedStyle(row).backgroundColor,cells:[...row.cells].map(cell=>({box:b(cell),opacity:Number(getComputedStyle(cell).opacity),transform:getComputedStyle(cell).transform,enter:cell.classList.contains('enter')})),pillOpacity:Number(getComputedStyle(row.querySelector('.pill')).opacity)}))};}
async function record(){const os=new URLSearchParams(location.search).get('os')==='windows'?'windows':'mac';const v={state:view(),frames};const response=await fetch('/evidence/root-progresspage-'+os+'-input.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(v)});if(!response.ok)throw Error('Owned progress record rejected');document.getElementById('progress-record').textContent='Progress input recorded';}
document.getElementById('progress-record').onclick=record;
document.getElementById('progress-language').onclick=()=>{const w=frame.contentWindow;w.ProgressInputBridge.toggleLanguage();w.document.querySelector('.page').style.width='980px';phase='language';record();};
document.getElementById('progress-tick').onclick=()=>{const w=frame.contentWindow,bridge=w.ProgressInputBridge;bridge.tick();phase='live';frames=[];const rev=++revision,t=performance.now();function measure(now){if(rev!==revision)return;frames.push({ms:now-t,state:view()});if(now-t<650)requestAnimationFrame(measure);else record();}requestAnimationFrame(measure);};
frame.onload=async()=>{const script=frame.contentDocument.createElement('script');script.textContent="window.ProgressInputBridge={run:()=>state.run,language:()=>lang,toggleLanguage:()=>{lang=lang==='ko'?'en':'ko';render();},tick:()=>{const g=state.run.groups[0];g.done=1;g.status='running';updateProgress();}}";frame.contentDocument.body.append(script);if(new URLSearchParams(location.search).get('lang')==='en')frame.contentWindow.ProgressInputBridge.toggleLanguage();await frame.contentDocument.fonts.ready;await new Promise(r=>setTimeout(r,250));record();};
</script>'''
(f/'progresspage-input.html').write_text(s)
print(f/'progresspage-input.html')
