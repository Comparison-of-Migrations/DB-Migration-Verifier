"""Literal original event-handler flow oracle; actual OS input remains a separate gate."""
from pathlib import Path
import hashlib, json

r = Path(__file__).resolve().parents[2]
out = r / 'artifacts/replica/windows-download'
for ext in ['js', 'css']:
    data = (r / ('docs/replica/source/original.'+ext)).read_bytes()
    (out / ('root-appflow-original.'+ext)).write_bytes(data)
body = '''<!doctype html><meta charset="utf-8"><title>Root application flow · original isolation</title>
<button id="record-flow">Record application handler flows</button>
<iframe id="source" style="width:1280px;height:900px;border:0" srcdoc='<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="root-appflow-original.css"><div id="app"></div><div id="dialog-host"></div><div id="toast-host"></div><div id="announcer" class="sr" aria-live="polite"></div><input id="file-open" hidden type="file"><script src="root-appflow-original.js"></script>'></iframe>
<script>
document.getElementById('record-flow').onclick=async()=>{
 const frame=document.getElementById('source'),win=frame.contentWindow,doc=frame.contentDocument;
 const rows=[],wait=ms=>new Promise(r=>setTimeout(r,ms));
 async function until(fn,label){const end=performance.now()+12000;while(!fn()){if(performance.now()>end)throw Error(label);await wait(25)}}
 function current(){return win.eval('({screen:state.screen,english:lang==="en",connected:state.connected,busy:state.busy,error:state.error,categories:[...state.selected],tables:[...state.tables],maps:{...state.maps},expanded:[...state.expanded],trial:{...state.trial},stopped:state.stopped,visited:[...state.visited],filter:state.filter,formats:[...state.formats],saved:state.saved,run:state.run?report().run:null,modal:modal?.type||null,query:tableQuery})')}
 function record(id){const v=current(),f=doc.activeElement;rows.push({id,...v,title:doc.getElementById('screen-title')?.textContent||'',focus:f?.id||f?.dataset?.action||f?.dataset?.field||f?.dataset?.flag||f?.tagName||'',announcement:doc.getElementById('announcer').textContent,offset:doc.getElementById('main')?.scrollTop||0,footer:[...doc.querySelectorAll('.footer button[data-action]')].map(b=>({action:b.dataset.action,label:b.textContent.trim(),enabled:!b.disabled}))})}
 async function action(name){const b=doc.querySelector('button[data-action="'+name+'"]');if(!b||b.disabled)throw Error('Enabled source action absent: '+name);b.focus({preventScroll:true});b.click();if(name==='modal-close'||name==='mapping-apply')await until(()=>!current().modal,'Source modal close completion');await wait(35)}
 async function toggle(selector){const e=doc.querySelector(selector);if(!e||e.disabled)throw Error('Enabled source toggle absent: '+selector);e.focus({preventScroll:true});e.click();await wait(35)}
 async function field(selector,value){const e=doc.querySelector(selector);if(!e)throw Error('Source field absent');e.focus({preventScroll:true});e.value=value;e.dispatchEvent(new win.Event('input',{bubbles:true}));await wait(35)}
 async function path(prefix,english,interrupted){
  win.eval('clearInterval(timer);modal=null;state=initial();tableQuery="";lang='+JSON.stringify(english?'en':'ko')+';render(true)');await wait(35);record(prefix+'home');
  await action('start');record(prefix+'connect');
  await field('[data-field="source.host"]','');await action('connect');record(prefix+'invalid-connect');
  await field('[data-field="source.host"]','source.example.invalid');await action('connect');await until(()=>current().connected&&!current().busy,'Source connection timeout');record(prefix+'connected');
  await action('preflight');record(prefix+'preflight');await action('permissions');record(prefix+'permissions');await action('modal-close');record(prefix+'permissions-close');
  await action('choose');record(prefix+'selection');await action('clear-tables');record(prefix+'tables-empty');await toggle('[data-bulk]');record(prefix+'tables-restored');
  await action('mapping:demo.orders');await toggle('[data-modal-field="draft"][value="archive.orders"]');await action('mapping-apply');record(prefix+'mapped');
  await toggle('[data-category="trial"]');record(prefix+'trial-required');await action('trial');record(prefix+'trial');await toggle('[data-flag="trial.consent"]');await action('trial-check');record(prefix+'trial-ready');await action('trial-back');record(prefix+'trial-return');
  await action('review');record(prefix+'review');await toggle('[data-flag="stopped"]');record(prefix+'review-confirmed');await action('run');record(prefix+'running');
  if(interrupted){await until(()=>current().run.groups.some(g=>g.done>0),'Source first tick');await action('cancel');record(prefix+'interrupted')}
  else {await until(()=>current().run.complete,'Source completion');record(prefix+'completed')}
  await action('show-results');record(prefix+'results');await action('filter:unknown');record(prefix+'filtered');await action('detail:access');record(prefix+'detail');await action('modal-close');record(prefix+'detail-close');await action('save');record(prefix+'save');await toggle('[data-format="csv"]');await toggle('[data-format="html"]');record(prefix+'formats');await action('restart');record(prefix+'restart');
 }
 try{await path('ko-',false,false);await path('en-',true,true);const response=await fetch('/evidence/root-appflow-mac-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({scope:'Original handler full navigation oracle; programmatic focus/click, not native OS input',environment:{viewport:{width:1280,height:900},dpr:win.devicePixelRatio,language:'KO/EN',data:'synthetic initial/sample',motion:'browser default'},rows})});if(!response.ok)throw Error('Evidence save failed');document.getElementById('record-flow').textContent='Recorded '+rows.length+' states'}catch(error){document.getElementById('record-flow').textContent='FAILED: '+error.message;throw error}
};
</script>'''
(out / 'root-appflow.html').write_text(body)
manifest = {'fixture':str(out/'root-appflow.html'),'scope':'PageIntegration direct source event handlers; no protected source changes','sourceHashes':{ext:hashlib.sha256((out/('root-appflow-original.'+ext)).read_bytes()).hexdigest() for ext in ['js','css']}}
(r/'docs/replica/source/root-appflow-fixture.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(out/'root-appflow.html')
