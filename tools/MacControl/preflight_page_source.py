"""Automatic original parent geometry, immediately closes exact own source window."""
from pathlib import Path
import json,subprocess,time,uuid
r=Path(__file__).resolve().parents[2];assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight'
nonce=uuid.uuid4().hex;o=r/'artifacts/replica/workers/preflight/preflightpage/source-macos'/nonce;o.mkdir(parents=True)
helper=r/'artifacts/replica/workers/preflight/preflightpage/preflight_page_source';file=r/'docs/replica/source/preflight-preflightpage-mac-source.json';before=file.stat().st_mtime_ns if file.exists()else 0
subprocess.run([str(helper),'count',str(o/'before.json')],check=True);assert json.loads((o/'before.json').read_text())['sourceWindowCount']==0
url='http://10.211.55.2:8768/preflightpage.html?os=mac&record=1'
subprocess.Popen(['/Applications/Google Chrome.app/Contents/MacOS/Google Chrome','--new-window',url],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
try:
 end=time.monotonic()+20
 while time.monotonic()<end:
  if file.exists()and file.stat().st_mtime_ns>before:
   try:
    raw=file.read_bytes();v=json.loads(raw)
    if len(v.get('rows',[]))==16 and v['dpr']==2:break
   except json.JSONDecodeError:pass
  time.sleep(.05)
 else:raise RuntimeError('Fresh original parent16 absent; no automatic relaunch')
 (o/'preflightpage-source.json').write_bytes(raw);(r/'docs/replica/source/preflightpage-mac-source.json').write_bytes(raw)
finally:subprocess.run([str(helper),'close',str(o/'cleanup.json')],check=True)
print(o);print('Original parent16 preserved, exact sourceWindow0')
