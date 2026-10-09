"""Finite physical App flow. A failed fresh observation never repeats input."""
from pathlib import Path
import argparse, hashlib, json, plistlib, shutil, subprocess, time
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('action',choices=['prepare','launch','batch','inspect','close']);p.add_argument('--english',action='store_true');p.add_argument('--session',default='');a=p.parse_args();assert not a.session or (a.session.isalnum() and len(a.session)<40)
m=a.manifest.resolve();meta=json.loads(m.read_text());assert meta['owner']=='root' and meta['component']=='pageintegration' and meta['runtime']=='osx-arm64' and meta['buildExitCode']==0
base=m.parent;out=base/('actual-fullflow-en' if a.english else 'actual-fullflow-ko');out=out if not a.session else out.parent/(out.name+'-'+a.session);out.mkdir(exist_ok=True);bundle=base/'PageIntegrationFullFlowCheck.app';helper=ROOT/'artifacts/replica/workers/root/pageintegration/fullflow-input';launch=ROOT/'artifacts/replica/workers/root/pageintegration/fullflow-launch'
if a.action=='prepare':
 assert not bundle.exists(),'Fresh bundle required';shutil.copytree(Path(meta['binary']).parent,bundle/'Contents/MacOS');assert hashlib.sha256((bundle/'Contents/MacOS/ReplicaProbe.dll').read_bytes()).hexdigest()==meta['binarySha256'];(bundle/'Contents/Info.plist').write_bytes(plistlib.dumps(dict(CFBundleIdentifier='com.dbmigration.replica.root.fullflow.'+base.name,CFBundleName='Full flow verification',CFBundleExecutable='ReplicaProbe',CFBundlePackageType='APPL',NSHighResolutionCapable=True)));shutil.copyfile(ROOT/'docs/replica/source/root-modal-bounds-cases.json',out/'cases.json');print(bundle);raise SystemExit
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='root'
if a.action=='launch':
 assert not (out/'current-native.json').exists(),'Fresh tracked launch required';shutil.copyfile(ROOT/'docs/replica/source/root-modal-bounds-cases.json',out/'cases.json');r=subprocess.run([str(launch),str(m),str(out),'en' if a.english else 'ko'],capture_output=True,text=True,timeout=15);(out/'launch.log').write_text(r.stdout+r.stderr);assert r.returncode==0,r.stderr;print(r.stdout);raise SystemExit
v=json.loads((out/'current-native.json').read_text());binary=bundle/'Contents/MacOS/ReplicaProbe.dll';pid=v['pid'];checks=[]
def raw():
 files=sorted(out.glob('flow-state-*.json'));assert files,'Fresh observer required';return json.loads(files[-1].read_text())
def wait(predicate,after=-1,label='state'):
 end=time.monotonic()+12
 while time.monotonic()<end:
  try:
   s=raw()
   if s['sequence']>after and predicate(s['state']):return s
  except (AssertionError,FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.03)
 raise RuntimeError('Fresh predicate failed: '+label+'; do not replay input')
def run(mode,id,label,value=''):
 r=subprocess.run([str(helper),str(pid),str(binary),mode,id,value,str(out),label],capture_output=True,text=True,timeout=10);(out/(label+'.actor.log')).write_text(r.stdout+r.stderr);assert r.returncode==0,r.stderr
 return r
if a.action=='inspect':run('inspect','start','current');print(out/'current-ax.json');raise SystemExit
if a.action=='close':
 run('close','start','final-close');end=time.monotonic()+5
 while time.monotonic()<end and subprocess.run(['kill','-0',str(pid)],capture_output=True).returncode==0:time.sleep(.05)
 assert subprocess.run(['kill','-0',str(pid)],capture_output=True).returncode!=0;(out/'cleanup.json').write_text(json.dumps(dict(pid=pid,nativeProcess=0,nativeWindow=0),indent=2));raise SystemExit

def record(label,s):
 checks.append(dict(id=label,state=s['state'],sequence=s['sequence'],passed=True));(out/'physical-flow-checks.json').write_text(json.dumps(dict(scope='Fresh physical input and whole-flow milestones; timer intermediate counts are live, not normalized to handler timing',checks=checks),indent=2,ensure_ascii=False)+'\n');print(label,s['state']['screen'],flush=True)
def step(label,id,predicate,mode='click',value=''):
 before=raw()['sequence'];run(mode,id,label,value);s=wait(predicate,before,label);record(label,s);return s

def ensure_visible(id):
 # Read-only AX inspection; scrolling is sent only to the guarded own content.
 for attempt in range(4):
  run('inspect',id,'visible-'+str(len(checks))+'-'+str(attempt));nodes=json.loads((out/('visible-'+str(len(checks))+'-'+str(attempt)+'-ax.json')).read_text());found=[n for n in nodes if n.get('id')==id]
  if found and found[0].get('box',{}).get('height',0)>0:
   # Native helper enforces full frame containment. Do not assume offscreen visibility.
   snapshot=raw();controls=[c for c in snapshot['controls'] if c['id']==id]
   if controls and 98<=controls[0]['box']['y'] and controls[0]['box']['y']+controls[0]['box']['height']<=snapshot['state'].get('viewportHeight',900)-84:return
  prior=raw()['sequence'];run('scroll-down',id,'scroll-'+str(len(checks))+'-'+str(attempt));wait(lambda s:True,prior,'scroll')
 raise RuntimeError('Target not in own viewport after bounded scroll: '+id)

try:
 s=wait(lambda s:s['screen']=='home');record('home',s)
 step('connect','start',lambda s:s['screen']=='connect' and s['focus']=='screen-title')
 step('host-empty','source-host',lambda s:not s['connected'],mode='edit',value='')
 step('invalid-connect','Footer-connect',lambda s:bool(s['error']) and not s['connected'])
 step('host-restored','source-host',lambda s:not s['connected'] and not s['busy'],mode='edit',value='source.example.invalid')
 step('connected','Footer-connect',lambda s:s['connected'] and not s['busy'])
 step('preflight','Footer-preflight',lambda s:s['screen']=='capabilities' and s['focus']=='screen-title')
 # The physical permission modal/caller already has isolated bothOS proof; whole route includes it.
 step('permissions','CapabilityRow-permissions',lambda s:s['modal']=='permissions')
 step('permissions-close','InfoDialog-close',lambda s:s['modal'] is None)
 step('selection','Footer-choose',lambda s:s['screen']=='select' and s['focus']=='screen-title')
 ensure_visible('Selection-clear-tables');step('tables-empty','Selection-clear-tables',lambda s:s['tables']==[])
 step('tables-restored','table-all',lambda s:len(s['tables'])==3)
 ensure_visible('Selection-mapping-demo.orders');step('mapping-open','Selection-mapping-demo.orders',lambda s:s['modal']=='mapping')
 step('mapping-draft','MatchingDialog-choice-3',lambda s:s['modal']=='mapping')
 step('mapped','MatchingDialog-apply',lambda s:s['modal'] is None and s['maps']['demo.orders']=='archive.orders')
 # Return to top for the original fifth category after mapping.
 prior=raw()['sequence'];run('scroll-up','start','selection-up');wait(lambda s:s['offset']==0,prior,'selection-up')
 step('trial-required','category-trial',lambda s:'trial' in s['categories'] and not s['trial']['ready'])
 ensure_visible('TrialPreparation-prepare');step('trial','TrialPreparation-prepare',lambda s:s['screen']=='trial' and s['focus']=='screen-title')
 ensure_visible('flag-trial.consent');step('consent','flag-trial.consent',lambda s:s['trial']['consent'])
 step('trial-ready','Footer-trial-check',lambda s:s['trial']['ready'])
 step('trial-return','Footer-trial-back',lambda s:s['screen']=='select' and s['trial']['ready'])
 step('review','Footer-review',lambda s:s['screen']=='review')
 ensure_visible('flag-stopped');step('review-confirmed','flag-stopped',lambda s:s['stopped'])
 step('running','Footer-run',lambda s:s['screen']=='running' and s['run'] is not None)
 if a.english:step('interrupted','Footer-cancel',lambda s:s['run']['interrupted'] and not s['run']['complete'])
 else:record('completed',wait(lambda s:s['run']['complete'],label='complete'))
 step('results','Footer-show-results',lambda s:s['screen']=='results')
 # Filter and detail bothOS physical proofs reused; whole-page persistence still checked here.
 step('save','Footer-save',lambda s:s['screen']=='save')
 step('csv','format-csv',lambda s:'csv' in s['formats']);step('html','format-html',lambda s:'html' in s['formats'])
 ensure_visible('Save-restart');step('restart','Save-restart',lambda s:s['screen']=='connect' and not s['connected'] and s['run'] is None and not s['stopped'])
finally:
 # Even a diagnostic failure closes the finished component scope immediately.
 try:run('close','start','final-close')
 finally:
  end=time.monotonic()+5
  while time.monotonic()<end and subprocess.run(['kill','-0',str(pid)],capture_output=True).returncode==0:time.sleep(.05)
  absent=subprocess.run(['kill','-0',str(pid)],capture_output=True).returncode!=0
  (out/'cleanup.json').write_text(json.dumps(dict(pid=pid,nativeProcess=0 if absent else 1,nativeWindow=0 if absent else None),indent=2));assert absent,'Owned process retained'
