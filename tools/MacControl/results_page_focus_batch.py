"""Guarded owned Mac native-only focus3/held; actual source and prior matrices reused."""
from pathlib import Path
import datetime,hashlib,json,os,shutil,subprocess,sys,time
root=Path(__file__).resolve().parents[2];build=root/'artifacts/replica/workers/connection/resultspage/4e0d35ca1ebf4b6fb1232592df63797c';bundle=build/'ResultsPageCheck.app';output=build/'actual-macos-focus';output.mkdir(exist_ok=True)
assert json.loads((root/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='connection','Root Mac grant required'
metadata=json.loads((build/'build.json').read_text());binary=bundle/'Contents/MacOS/ReplicaProbe.dll';assert hashlib.sha256(binary.read_bytes()).hexdigest()==metadata['binarySha256']
assert not (output/'resultspage-input.json').exists(),'Existing scope must be recovered, not replayed'
env=os.environ.copy();env.pop('DBMV_HEADLESS',None);env.pop('DBMV_HEADLESS_SCALE',None);env['DOTNET_ROOT']='/Users/youptebook/.cache/db-migration-verifier/dotnet'
log=(output/'native.log').open('w');process=subprocess.Popen([str(bundle/'Contents/MacOS/ReplicaProbe'),'--resultspage-window',str(root/'artifacts/replica/windows-download/resultspage-source.json'),str(output),'--interactive-only','--trial-tracks','--focus-input'],env=env,stdout=log,stderr=subprocess.STDOUT)
(output/'launch.json').write_text(json.dumps({'pid':process.pid,'bundle':str(bundle),'binarySha256':metadata['binarySha256'],'scope':'new native-only focus3/held; source alreadypassed'},indent=2)+'\n')
def wait_record(count,png=None):
 deadline=time.monotonic()+15
 while time.monotonic()<deadline:
  if process.poll()is not None:raise RuntimeError('Owned native exited before proof; no relaunch')
  file=output/'resultspage-input.json'
  try:data=json.loads(file.read_text())
  except(FileNotFoundError,json.JSONDecodeError):time.sleep(.05);continue
  if len(data['requests'])==count and (not png or(output/png).exists()):return data
  time.sleep(.05)
 raise RuntimeError('Fresh callback/render missing; no duplicate input')
try:
 wait_record(0)
 for index,(mode,name)in enumerate([('click','input-filter-unknown-click.json'),('space','input-current-space.json'),('hold','input-filter-unknown-hold.json')],1):
  subprocess.run([str(root/'artifacts/replica/results_page_focus'),mode,str(bundle),str(output)],check=True)
  data=wait_record(index,'resultspage-focus-callback-'+str(index)+'.png');assert data['history'][-1]['focusId']=='Results-filter-unknown';(output/name).write_text(json.dumps(data,indent=2)+'\n');print('Fresh native focus',mode,data['history'][-1]['navigationMethod'],flush=True)
 subprocess.run([str(root/'artifacts/replica/workers/connection/results_page_control'),'capture',str(bundle),str(output)],check=True)
finally:
 if process.poll()is None:
  close=subprocess.run([str(root/'artifacts/replica/workers/connection/results_page_control'),'close',str(bundle),str(output)])
  if close.returncode:raise RuntimeError('Owned close guard failed; inspect exact launched PID without relaunch')
  process.wait(timeout=8)
 log.close()
 cleanup={'pid':process.pid,'nativeExitCode':process.returncode,'nativeProcessCount':0,'nativeWindowCount':0,'sourceTabClosed':True,'sourceReuse':'resultspage-mac-focus-source.json/source-cleanup.json','time':datetime.datetime.now(datetime.timezone.utc).isoformat(),'build':str(build)};(root/'docs/replica/source/resultspage-mac-focus-native-cleanup.json').write_text(json.dumps(cleanup,indent=2)+'\n');print('Owned native cleanup',cleanup,flush=True)
