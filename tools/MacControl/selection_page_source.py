"""Source36 autoDOM measurement and exact owned Chrome close; no candidate/test input."""
from pathlib import Path
import json,subprocess,time,uuid,shutil,hashlib
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/workers/preflight/selectionpage/source-macos'/uuid.uuid4().hex;o.mkdir(parents=True);print(o,flush=True)
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight'
actor=r/'artifacts/replica/workers/preflight/selectionpage/selection_page_source_control'
def mode(m):
 p=subprocess.run([str(actor),m,str(o)],cwd=r,text=True,capture_output=True);(o/(m+'-process.json')).write_text(json.dumps({'exitCode':p.returncode,'stdout':p.stdout,'stderr':p.stderr}));assert p.returncode==0,p.stderr
mode('guard');started=time.time();url='http://10.211.55.2:8768/selectionpage.html?os=mac&record=1&batch='+o.name
try:
 subprocess.run(['/usr/bin/open','-na','/Applications/Google Chrome.app','--args','--new-window',url],check=True)
 time.sleep(.5);mode('activate');deadline=time.monotonic()+35
 chunks={}
 while time.monotonic()<deadline:
  for index,part in enumerate(['a','b']):
   endpoint=r/'docs/replica/source'/f'preflight-selectionpage-{part}-mac-source.json'
   if endpoint.exists() and endpoint.stat().st_mtime>started:
    try:
     d=json.loads(endpoint.read_text())
     if d['batch']==o.name and d['chunk']==index and d['dpr']==2 and len(d['rows'])==18:
      chunks[index]=d;shutil.copyfile(endpoint,o/('source-chunk-'+part+'.json'))
    except json.JSONDecodeError:pass
  if len(chunks)==2:break
  time.sleep(.1)
 else:
  for name in ['preflight-selectionpage-mac-source.json','preflight-selectionpage-mac-control-ready.json']:
   path=r/'docs/replica/source'/name
   if path.exists():shutil.copyfile(path,o/name)
  raise RuntimeError('Fresh source18+18 absent; no duplicate launch')
 combined={'dpr':2,'rows':chunks[0]['rows']+chunks[1]['rows'],'batch':o.name,'transport':'bounded POST18+18, same exact nonce/protocol/producer','chunks':[{ 'path':str(o/('source-chunk-'+part+'.json')),'sha256':hashlib.sha256((o/('source-chunk-'+part+'.json')).read_bytes()).hexdigest()} for part in ['a','b']]}
 assert len(combined['rows'])==36 and len(set(x['id'] for x in combined['rows']))==36
 (o/'selectionpage-source.json').write_text(json.dumps(combined,ensure_ascii=False,indent=2));shutil.copyfile(o/'selectionpage-source.json',r/'docs/replica/source/selectionpage-mac-source.json')
finally:
 try:mode('close')
 finally:time.sleep(.3);mode('cleanup')
print('Source36 fresh bounded18+18 exactnonce and ownWindow0')
