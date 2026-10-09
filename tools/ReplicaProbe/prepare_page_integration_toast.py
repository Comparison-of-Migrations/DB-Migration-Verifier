"""Original bytes, one actual browser media mode per nonce; parent toast only."""
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[2];out=root/'artifacts/replica/windows-download';prefix='home-pageintegrationtoast';hashes={}
for ext in ('js','css'):
 data=(root/f'docs/replica/source/original.{ext}').read_bytes();(out/f'{prefix}-original.{ext}').write_bytes(data);hashes[ext]=hashlib.sha256(data).hexdigest()
(out/f'{prefix}-inner.html').write_text(f'''<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="{prefix}-original.css"><a class="skip" href="#main">본문으로 건너뛰기</a><div id="app"></div><input id="file-open" type="file" accept=".json,application/json"><div id="dialog-host"></div><div id="toast-host" role="status" aria-live="polite"></div><div id="announcer" class="sr" role="status" aria-live="polite"></div><script src="{prefix}-original.js"></script>''')
js=r'''
const q=new URLSearchParams(location.search),frame=document.querySelector('iframe'),rows=[],lifetimes=[];
const reduced=q.get('motion')==='reduced',wait=ms=>new Promise(r=>setTimeout(r,ms));
const geometryOnly=q.get('geometry-only')==='1',settle=async()=>{const t=d.querySelector('.toast');await wait(240);await Promise.all(t.getAnimations().map(a=>a.finished))};
const until=async(t,ms)=>{const rest=ms-(performance.now()-t);if(rest>0)await wait(rest)};
const box=e=>{const r=e.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height}};
const short=en=>en?'Sample trial setup confirmed. No actual DB was verified.':'예시 시험 준비를 확인했습니다. 실제 DB 검증은 아닙니다.';
const textLines=(d,e)=>{const groups=[],walker=d.createTreeWalker(e,NodeFilter.SHOW_TEXT);let n;while(n=walker.nextNode())for(let i=0;i<n.length;i++){const r=d.createRange();r.setStart(n,i);r.setEnd(n,i+1);const b=r.getBoundingClientRect();if(!b.height)continue;let l=groups.find(v=>Math.abs(v.y-b.y)<.1);if(!l){l={y:b.y,text:''};groups.push(l)}l.text+=n.data[i]}return groups.map(l=>l.text.trimEnd())};
await new Promise(r=>frame.onload=r);
let w=frame.contentWindow,d=frame.contentDocument;
if(w.matchMedia('(prefers-reduced-motion: reduce)').matches!==reduced)throw Error('Actual browser media differs from requested '+q.get('motion'));
function read(id){const t=d.querySelector('.toast'),s=w.getComputedStyle(t);return{id,width:w.innerWidth,height:w.innerHeight,dpr:w.devicePixelRatio,text:t.textContent,box:box(t),lines:textLines(d,t),parent:{innerWidth:w.innerWidth,innerHeight:w.innerHeight,documentClientWidth:d.documentElement.clientWidth,documentClientHeight:d.documentElement.clientHeight,documentScrollWidth:d.documentElement.scrollWidth,documentScrollHeight:d.documentElement.scrollHeight},style:{background:s.backgroundColor,borderColor:s.borderTopColor,borderWidth:s.borderTopWidth,radius:s.borderTopLeftRadius,padding:s.padding,shadow:s.boxShadow,fontSize:parseFloat(s.fontSize),lineHeight:parseFloat(s.lineHeight),color:s.color,maxWidth:s.maxWidth,hitTest:s.pointerEvents!=='none'},animation:{name:s.animationName,duration:s.animationDuration,reduced:w.matchMedia('(prefers-reduced-motion: reduce)').matches}}}
for(const en of [false,true])for(const width of [960,1280]){
 const id=(en?'en':'ko')+'-'+width+'-'+(reduced?'reduced':'normal');if(q.get('target')&&q.get('target')!==id)continue;
 frame.width=width;w.eval('clearInterval(timer);lang='+JSON.stringify(en?'en':'ko')+';state=initial();go("home");');await d.fonts.ready;await wait(240);
 w.toast(short(en));await settle();rows.push(read(id+'-short'));
 w.toast(Array(4).fill(short(en)).join(' '));let oldTime=performance.now(),old=d.querySelector('.toast');await settle();rows.push(read(id+'-long'));if(width!==960||geometryOnly)continue;
 await until(oldTime,1200);const focus=d.activeElement;w.toast(short(en));const newTime=performance.now(),replacement=d.querySelector('.toast');
 const replaced=old!==replacement&&!old.isConnected&&replacement.textContent===short(en);
 await until(oldTime,4300);const survivesOldDeadline=d.querySelector('.toast')===replacement,oldDeadlineMs=performance.now()-oldTime;
 await until(newTime,4100);const beforeNewDeadline=d.querySelector('.toast')===replacement,beforeMs=performance.now()-newTime;
 await until(newTime,4400);const removed=!d.querySelector('.toast'),afterMs=performance.now()-newTime;
 lifetimes.push({id,replaced,survivesOldDeadline,beforeNewDeadline,removed,focusSame:d.activeElement===focus,oldDeadlineMs,beforeMs,afterMs,attachedOld:old.isConnected});
}
// Removing the original browsing context cancels its timers; retain the old
// DOM solely to observe that its pending timeout no longer mutates it.
let disposal=null;if(!geometryOnly){w.toast(short(true));const oldHost=d.getElementById('toast-host'),disposedTime=performance.now();frame.remove();await until(disposedTime,4400);disposal={browsingContextRemoved:!frame.isConnected,oldTimerDidNotMutateDiscardedDocument:oldHost.children.length===1,elapsedMs:performance.now()-disposedTime};}else frame.remove();
const result={phase:'ORIGINAL_PARENT_TOAST_RECORDED',scope:'one actual media mode,8geometry+2lifetimes+discarded-context timer; width-independent timer branch',recording:q.get('recording'),motion:q.get('motion'),originalHashes:HASHES,rows,lifetimes,disposal};
const response=await fetch('/evidence/home-pageintegrationtoast-'+(q.get('os')||'mac')+'-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(result)});if(!response.ok)throw Error('Owned POST '+response.status);document.getElementById('result').textContent='Recorded '+rows.length+' '+q.get('motion');
'''.replace('HASHES',json.dumps(hashes))
html=f'''<!doctype html><meta charset="utf-8"><title>Toast parent oracle · home</title><p id="result">Measuring</p><iframe width="1280" height="900" style="border:0" src="{prefix}-inner.html"></iframe><script>(async()=>{{try{{{js}}}catch(e){{document.getElementById('result').textContent='FAILED '+e.message;throw e}}}})();</script>'''
(out/f'{prefix}.html').write_text(html)
(root/'docs/replica/source/home-pageintegrationtoast-fixture.json').write_text(json.dumps({'scope':'MainContent parent toast; no product/source byte edits','originalHashes':hashes,'fixture':prefix+'.html','geometryCases':16,'lifetimeContexts':4,'media':'Actual browser prefers-reduced-motion required, no injected CSS'},indent=2)+'\n')
script=root/'artifacts/replica/workers/home/toast-fixture-script.js';script.parent.mkdir(parents=True,exist_ok=True);script.write_text(html.split('<script>',1)[1].split('</script>',1)[0])
print('Prepared exact original Toast fixture; syntax script '+str(script))
