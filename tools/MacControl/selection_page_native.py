"""Single finite native Selection parent slice, guarded raw input; immediately close on any result."""
from pathlib import Path
import json,subprocess,sys,time,shutil,hashlib
r=Path(__file__).resolve().parents[2];manifest=Path(sys.argv[1]).resolve();m=json.loads(manifest.read_text());assert m['owner']=='preflight'and m['component']=='selectionpage'and m['runtime']=='osx-arm64'and m['buildExitCode']==0
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight'
b=manifest.parent;o=b/'actual-macos/input';actor=r/'artifacts/replica/workers/preflight/selectionpage/selection_page_native_control';record=o/'selectionpage-input.json'
def call(mode,label):
 result=subprocess.run([str(actor),str(manifest),mode],cwd=r,text=True,capture_output=True,timeout=12);d=o/label;d.mkdir(exist_ok=True);(d/'process.json').write_text(json.dumps(dict(mode=mode,exitCode=result.returncode,stdout=result.stdout,stderr=result.stderr)))
 for path in o.glob(mode+'-*.json'):shutil.copyfile(path,d/path.name)
 if mode=='inspect':
  for path in o.glob('native-ax-*.json'):shutil.copyfile(path,d/path.name)
 assert result.returncode==0,result.stderr
 return json.loads(result.stdout)if result.stdout.startswith('{')else None
call('guard','00-guard');started=time.time();subprocess.run(['/usr/bin/open','-n',str(b/'SelectionPage-input.app')],check=True)
try:
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  try:
   initial=json.loads(record.read_text())
   if record.stat().st_mtime>started and initial['attached']:break
  except (FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.08)
 else:raise RuntimeError('Fresh native input state absent; no relaunch')
 (o/'initial.json').write_text(json.dumps(initial));checks=[];call('inspect','01-ax-ko-query');subprocess.run([str(r/'artifacts/replica/workers/preflight/selectionpage/selection_page_capture_control'),str(b/'SelectionPage-input.app'),str(o/'native-window-initial.png')],check=True)
 steps=[('focus-search',lambda s:s['searchFocused']),('scroll-bottom',None),('passive',lambda s:not s['searchFocused']and s['requests']==[]),('scroll-top',None),('category-trial',lambda s:'trial'in s['selection']['categories']and not s['footer']['enabled']),('scroll-bottom',None),('trial',lambda s:s['requests']==['trial']),('scroll-top',None),('category-schema',lambda s:'schema'not in s['selection']['categories']and'trial'in s['selection']['categories']),('focus-search',lambda s:s['searchFocused']),('language',lambda s:s['english']and s['searchFocused']and s['selection']['query']==''),('mount',lambda s:not s['attached']),('mount',lambda s:s['attached']and s['selection']['categories']==['access','objects','trial']),('motion',None),('language',lambda s:not s['english']and s['attached']),('back',lambda s:s['requests']==['trial','back'])]
 oldIdentity=None
 for index,(mode,expect) in enumerate(steps,start=2):
  state=call(mode,f'{index:02d}-{mode}')
  if expect:
   deadline=time.monotonic()+3
   while not expect(state) and time.monotonic()<deadline:
    time.sleep(.08)
    try:state=json.loads(record.read_text())
    except json.JSONDecodeError:pass
   passed=expect(state)
   if mode=='category-schema':passed=passed and state['preparationIdentity']!=oldIdentity
   checks.append(dict(label=f'{index:02d}-{mode}',passed=passed));(o/'input-comparison.json').write_text(json.dumps(checks,indent=2));assert passed,(mode,state)
  if state and state.get('preparationIdentity'):oldIdentity=state['preparationIdentity']
 print('Selection native parent finite scope captured',flush=True)
finally:
 try:call('close','90-close')
 finally:
  time.sleep(.4);call('cleanup','91-cleanup')
