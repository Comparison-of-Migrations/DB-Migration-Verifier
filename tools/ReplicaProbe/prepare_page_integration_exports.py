from pathlib import Path
import json
r=Path(__file__).resolve().parents[2];out=r/'artifacts/replica/windows-download'
html="""<!doctype html><meta charset="utf-8"><title>Page integration exports · original isolation</title><link rel="stylesheet" href="pageintegration-original.css"><div id="app"></div><div id="dialog-host"></div><div id="toast-host"></div><div id="announcer"></div><input id="file-open" hidden type="file"><script src="pageintegration-original.js"></script><button id="exports-record" style="position:fixed;z-index:500;top:0;left:0">Record original exports</button><script>
// Observe the actual original download arguments; forward every call unchanged.
const actualDownloadFile=downloadFile;let exportCapture=[];
downloadFile=(name,body,mime)=>{exportCapture.push({name,body,mime});return actualDownloadFile(name,body,mime)};
document.getElementById('exports-record').onclick=async()=>{
const rows=[];for(const english of [false,true])for(const partial of [false,true]){state=initial();lang=english?'en':'ko';state.run=makeRun(true);state.run.id='UI-EXPORT1';if(partial){state.run.groups[0].status='interrupted';state.run.groups[0].done=3;state.run.interrupted=true;}else{state.run.groups.forEach(completeGroup);state.run.complete=true;}state.screen='save';state.formats=new Set(['json','csv','html']);render();exportCapture=[];download();rows.push({id:(english?'en':'ko')+(partial?'-partial':'-complete'),english,report:report(),exports:exportCapture});}
const cells=EXPORT_CELLS;const csv=cells.map(csvCell),escaped=cells.map(esc);
const response=await fetch('/evidence/root-pageintegration-export-mac-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({rows,cells,csv,escaped,scope:'Original download forwarded unchanged; synthetic sample only'})});if(!response.ok)throw Error('Record failed');document.getElementById('exports-record').textContent='Exports recorded';};
</script>"""
(out/'pageintegration-exports.html').write_text(html.replace('EXPORT_CELLS',json.dumps(['=1+1','+1','@value','-1',chr(9)+'value',chr(13)+'value','a"b','<&>"'+chr(39),'한국어'],ensure_ascii=False)));print('Original KO/EN complete/partial export observer')
