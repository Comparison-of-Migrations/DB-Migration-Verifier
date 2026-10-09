"""Source-only automatic focus-safe diagnostics; literal modal CSS/JS remain unchanged."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[2];p=r/'artifacts/replica/windows-download/infodialog-input.html';html=p.read_text();start=html.index('f.srcdoc=')+len('f.srcdoc=');doc,used=json.JSONDecoder().raw_decode(html[start:]);end=start+used
# Replace our instrumentation only. Do not alter the copied source openModal/closeModal/press.
doc=doc.replace('return {visible:!!d,','return {capturedAtUtc:new Date().toISOString(),visible:!!d,')
if 'DBMV-infodialog-input'not in doc:
 insertion="const automaticEvidence=()=>setTimeout(()=>parent.postMessage({type:'DBMV-infodialog-input',data:read()},'*'),420);document.addEventListener('focusin',automaticEvidence);document.addEventListener('keyup',automaticEvidence);document.addEventListener('pointerup',automaticEvidence);"
 at=doc.rfind('</script>');doc=doc[:at]+insertion+doc[at:]
html=html[:start]+json.dumps(doc,ensure_ascii=False).replace('</','<\\/')+html[end:]
if "e.data?.type==='DBMV-infodialog-input'"not in html:
 at=html.rfind('</script>');html=html[:at]+"window.addEventListener('message',async e=>{if(e.source!==f.contentWindow||e.data?.type!=='DBMV-infodialog-input')return;const data=e.data.data;document.getElementById('record').textContent=JSON.stringify(data);const platform=new URLSearchParams(location.search).get('os')??'mac';await fetch('/evidence/root-infodialog-'+platform+'-input.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(data)});});"+html[at:]
p.write_text(html);print(p)
