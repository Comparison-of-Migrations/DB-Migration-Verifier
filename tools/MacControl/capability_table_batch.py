"""One owned actual-Mac Table parent batch, fresh passive records; stops on guarded failure."""
from pathlib import Path
import json,subprocess,sys,time,shutil,hashlib
r=Path(__file__).resolve().parents[2];manifest=Path(sys.argv[1]).resolve();m=json.loads(manifest.read_text());assert m['owner']=='preflight'and m['component']=='capabilitytable'and m['runtime']=='osx-arm64'and m['buildExitCode']==0
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight'
base=manifest.parent;out=base/'actual-macos/input';bundle=base/'CapabilityTableCheck.app';actor=r/'artifacts/replica/capability-table-control';endpoint=r/'docs/replica/source/preflight-capabilitytable-mac-input.json';out.mkdir(parents=True,exist_ok=True)
(out/'actor-environment.json').write_text(json.dumps({'manifest':str(manifest),'binarySha256':m['binarySha256'],'actorSha256':hashlib.sha256(actor.read_bytes()).hexdigest(),'sourceFixtureSha256':hashlib.sha256((r/'artifacts/replica/windows-download/capabilitytable-input.html').read_bytes()).hexdigest(),'scope':'new inner semanticTable AX and passive-observer parent pointer/key/lifecycle; no old12 geometry replay'},indent=2)+'\n')
def action(mode,source=False,label=None):
 command=[str(actor),str(manifest),mode]+(['--source']if source else[])
 result=subprocess.run(command,cwd=r,text=True,capture_output=True);(out/(('source-'if source else'native-')+(label or mode)+'-process.json')).write_text(json.dumps({'command':command,'exitCode':result.returncode,'stdout':result.stdout,'stderr':result.stderr},indent=2)+'\n')
 if result.returncode:raise RuntimeError(result.stderr.strip())
 if label and mode=='inspect':shutil.copyfile(out/(('source-'if source else'native-')+'ax.json'),out/(('source-'if source else'native-')+label+'-ax.json'))
 if label and label!=mode and mode not in ['inspect','close','cleanup','detached']:shutil.copyfile(out/(('source-'if source else'native-')+mode+'.json'),out/(('source-'if source else'native-')+label+'.json'))
 print(('source 'if source else'native ')+(label or mode)+' passed',flush=True)
def waitfile(file,after):
 end=time.monotonic()+8
 while time.monotonic()<end:
  if file.exists()and file.stat().st_mtime>after:
   try:return json.loads(file.read_text())
   except json.JSONDecodeError:pass
  time.sleep(.05)
 raise RuntimeError('Fresh owned passive record absent: '+str(file))
def run(source):
 resume=source and '--resume-source' in sys.argv
 if resume:
  assert json.loads((out/'source-pointer.json').read_text())['permissions']==1
  assert json.loads(endpoint.read_text())['id']=='ko-904'and json.loads(endpoint.read_text())['permissions']==1
 else:
  start=time.time()
  subprocess.run(['/Applications/Google Chrome.app/Contents/MacOS/Google Chrome','--new-window','http://10.211.55.2:8768/capabilitytable-input.html?os=mac']if source else['/usr/bin/open','-n',str(bundle)],check=True)
  file=endpoint if source else out/'capabilitytable-input.json';initial=waitfile(file,start);assert initial['id']=='ko-904'and initial['permissions']==0 and initial['attached'];assert not source or initial['dpr']==2;(out/(('source-'if source else'native-')+'initial.json')).write_text(json.dumps(initial)+'\n');
  if not source:time.sleep(.4)
  if source or '--native-only' not in sys.argv:action('inspect',source,'initial')
 for mode,label in [('pointer','pointer'),('key','key'),('passive','passive'),('next','en-narrow'),('next','ko-narrow'),('next','en-wide'),('mount','detach')]:
  if not(resume and mode=='pointer'):action(mode,source,label)
 action('detached',source);action('mount',source,'remount');action('inspect',source,'remount')
 if not source:subprocess.run([str(r/'artifacts/replica/capability-table-capture'),str(bundle),str(base/'actual-macos/native-window-final.png')],check=True,cwd=r)
 action('close',source);time.sleep(.3);action('cleanup')
if '--native-only' not in sys.argv:run(True)
run(False)
