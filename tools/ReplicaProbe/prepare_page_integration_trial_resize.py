"""New parent resize/Stopped concern only; literal original handler oracle, no native input."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'artifacts/replica/windows-download';OUT.mkdir(parents=True,exist_ok=True)
for ext in ('js','css'):
 data=(ROOT/f'docs/replica/source/original.{ext}').read_bytes()
 (OUT/f'home-pageintegrationtrial-resize-original.{ext}').write_bytes(data)
inner='<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="home-pageintegrationtrial-resize-original.css"><div id="app"></div><div id="dialog-host"></div><div id="toast-host"></div><div id="announcer" class="sr" role="status" aria-live="polite"></div><input id="file-open" hidden type="file"><script src="home-pageintegrationtrial-resize-original.js"></script>'
(OUT/'home-pageintegrationtrial-resize-inner.html').write_text(inner)
common='''const frame=document.querySelector('iframe'),w=frame.contentWindow,d=frame.contentDocument;
const wait=ms=>new Promise(r=>setTimeout(r,ms));async function settle(){await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));await wait(400)}
async function post(component,value){let res=await fetch('/evidence/home-pageintegrationtrial-'+component+'-'+(new URLSearchParams(location.search).get('os')||'mac')+'-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(value)});if(!res.ok)throw Error('POST '+res.status);document.getElementById('result').textContent='Recorded '+value.rows.length;}
'''
resize='''const rows=[];for(const language of ['ko','en']){
 frame.width=1280;w.eval('clearInterval(timer);lang='+JSON.stringify(language)+';state=initial();go("trial")');await d.fonts.ready;await settle();
 const page=d.querySelector('.page'),editor=d.querySelector('[data-field="trial.host"]');editor.focus({preventScroll:true});editor.setSelectionRange(3,8);
 const initialAnimations=new Set(d.getAnimations());const expected=editor.value;function box(e){const b=e.getBoundingClientRect(),p=d.querySelector('.page').getBoundingClientRect();return {x:b.x-p.x,y:b.y-p.y,width:b.width,height:b.height}};function geometry(){const page=d.querySelector('.page'),splits=page.querySelectorAll('.split'),panels=page.querySelectorAll('.panel'),fields=page.querySelectorAll('.field');return {root:box(page),split:box(splits[0]),panels:[...panels].slice(0,2).map(box),fields:[...fields].map(box),inputs:[...page.querySelectorAll('input[data-field^="trial."]')].map(box),valueSplit:box(splits[1])}};
 for(const width of [1280,1100,960,1280]){frame.width=width;await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));
  const immediateAnimations=d.getAnimations().filter(a=>a.effect?.target===d.querySelector('.page'));
  const immediate={opacity:getComputedStyle(d.querySelector('.page')).opacity,transform:getComputedStyle(d.querySelector('.page')).transform,newPageAnimations:immediateAnimations.filter(a=>!initialAnimations.has(a)).length};
  await settle();const current=d.querySelector('[data-field="trial.host"]');rows.push({id:language+'-'+rows.filter(r=>r.language===language).length,language,width,actualWidth:w.innerWidth,pageRetained:page===d.querySelector('.page'),editorRetained:editor===current,focused:current===d.activeElement,value:current.value,expectedValue:expected,selectionStart:current.selectionStart,selectionEnd:current.selectionEnd,immediate,geometry:geometry(),document:{innerWidth:w.innerWidth,innerHeight:w.innerHeight,clientWidth:d.documentElement.clientWidth,clientHeight:d.documentElement.clientHeight,scrollHeight:d.documentElement.scrollHeight,contentClientWidth:d.querySelector('.content').clientWidth,contentScrollWidth:d.querySelector('.content').scrollWidth,contentClientHeight:d.querySelector('.content').clientHeight,contentScrollHeight:d.querySelector('.content').scrollHeight},newPageAnimations:d.getAnimations().filter(a=>a.effect?.target===d.querySelector('.page')&&!initialAnimations.has(a)).length});
 }
}await post('resize',{scope:'Literal original Trial resize handler/CSS; synthetic in-document focus, not actualOS input',recording:new URLSearchParams(location.search).get('recording'),dpr:w.devicePixelRatio,rows});'''
stopped='''const rows=[];for(const mode of ['endpoint','dbms']){
 w.eval('clearInterval(timer);lang="ko";state=initial();state.stopped=true;state.connected=true;state.trial.ready=true;state.trial.consent=true;go("connect")');await settle();
 const before=w.eval('({stopped:state.stopped,ready:state.trial.ready,consent:state.trial.consent,connected:state.connected})');
 if(mode==='endpoint'){const editor=d.querySelector('[data-field="source.host"]');editor.value='changed.example.invalid';editor.dispatchEvent(new w.Event('input',{bubbles:true}));}
 else{const picker=d.querySelector('[data-field="dbms"]');picker.value='PostgreSQL';picker.dispatchEvent(new w.Event('change',{bubbles:true}));}
 await settle();rows.push({id:mode,before,after:w.eval('({stopped:state.stopped,ready:state.trial.ready,consent:state.trial.consent,connected:state.connected,visited:[...state.visited]})')});
}await post('stopped',{scope:'Literal source Connection endpoint/dbms handler after prior Review confirmation; synthetic seed/events, not actualOS input',recording:new URLSearchParams(location.search).get('recording'),rows});'''
for name,body in [('resize',resize),('stopped',stopped)]:
 html='<!doctype html><meta charset="utf-8"><title>Trial parent '+name+' oracle · home</title><p id="result">Measuring</p><iframe width="1280" height="900" style="border:0" src="home-pageintegrationtrial-resize-inner.html"></iframe><script>window.onload=async()=>{try{'+common+body+'}catch(e){document.getElementById("result").textContent="FAILED "+e.message;throw e}};</script>'
 (OUT/f'home-pageintegrationtrial-{name}.html').write_text(html)
manifest={'scope':'New resize8 and stopped2 original literal concerns; old scopes reused','originalHashes':{ext:hashlib.sha256((ROOT/f'docs/replica/source/original.{ext}').read_bytes()).hexdigest() for ext in ('js','css')},'fixtures':[str(OUT/f'home-pageintegrationtrial-{n}.html') for n in ('resize','stopped')]}
(ROOT/'docs/replica/source/home-pageintegrationtrial-resize-fixture.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Prepared own resize8/stopped2 fixtures. No original/product changes.')
