from pathlib import Path
import json
r=Path(__file__).resolve().parents[2];out=r/'artifacts/replica/windows-download'
cases=[{'id':'default','categories':['data','schema','access','objects'],'tables':['demo.customers','demo.orders','demo.line_items']},{'id':'cancel-before-first','categories':['data','schema'],'tables':['demo.orders'],'cancelAt':0},{'id':'cancel-three','categories':['data','schema'],'tables':['demo.orders'],'cancelAt':3},{'id':'schema-rename','categories':['schema'],'tables':['demo.orders'],'renamed':True},{'id':'without-tables','categories':['access','objects'],'tables':[]},{'id':'zero-total','categories':['data'],'tables':[]}]
(r/'docs/replica/source/root-pageintegration-run-cases.json').write_text(json.dumps(cases,indent=2)+'\n')
html="""<!doctype html><meta charset="utf-8"><title>Page integration run state · original isolation</title><link rel="stylesheet" href="pageintegration-original.css"><div id="app"></div><div id="dialog-host"></div><div id="toast-host"></div><div id="announcer"></div><input id="file-open" hidden type="file"><script src="pageintegration-original.js"></script><button id="run-record" style="position:fixed;z-index:500;top:0;left:0">Record original run state</button><script>
const runCases=CASES;
document.getElementById('run-record').onclick=async()=>{
const rows=[];for(const c of runCases){state=initial();state.selected=new Set(c.categories);state.tables=new Set(c.tables);if(c.renamed)state.maps['demo.orders']='archive.orders';state.stopped=true;startRun();const frames=[];let last='';const read=()=>JSON.parse(JSON.stringify({groups:state.run.groups,interrupted:state.run.interrupted,complete:state.run.complete}));
while(true){const v=read(),key=JSON.stringify(v);if(key!==last){frames.push(v);last=key;}const done=v.groups.reduce((sum,g)=>sum+g.done,0);if(c.cancelAt!==undefined&&done>=c.cancelAt&&!v.interrupted){document.querySelector('[data-action="cancel"]').click();frames.push(read());break;}if(v.complete)break;await new Promise(resolve=>setTimeout(resolve,4));}
rows.push({id:c.id,frames,final:read(),footer:document.querySelector('.footer').textContent,screen:state.screen,visited:[...state.visited]});}
const response=await fetch('/evidence/root-pageintegration-run-mac-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({intervalMs:140,rows})});if(!response.ok)throw Error('Record failed');document.getElementById('run-record').textContent='Run state recorded';};
</script>""".replace('CASES',json.dumps(cases))
(out/'pageintegration-run.html').write_text(html);print(len(cases))
