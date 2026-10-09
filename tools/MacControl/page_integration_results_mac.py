"""Only fresh Results App parent flows; locked child matrices remain reused."""
from pathlib import Path
import argparse,hashlib,json,plistlib,shutil,subprocess,time
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('action',choices=['prepare','launch','batch','inspect','close','capture-batch']);p.add_argument('--source',action='store_true');p.add_argument('--resume-paint',action='store_true');p.add_argument('--resume-data',action='store_true');p.add_argument('--resume-research',action='store_true');p.add_argument('--capture-seed',action='store_true');p.add_argument('--from-capture',type=int,choices=[0,1],default=0);a=p.parse_args()
m=a.manifest.resolve();meta=json.loads(m.read_text());assert meta['owner']=='connection' and meta['component']=='pageintegration' and meta['runtime']=='osx-arm64' and meta['buildExitCode']==0
base=m.parent;out=base/'actual-results-macos';out.mkdir(exist_ok=True);bundle=base/'PageIntegrationResultsCheck.app';helper=ROOT/'artifacts/replica/workers/connection/pageintegration/page-integration-results-input';sourcefile=ROOT/'docs/replica/source/connection-pageintegrationresults-mac-input.json';statefile=sourcefile if a.source else out/'pageintegrationresults-actual-state.json';kind='source' if a.source else 'native'
if a.action=='prepare':
 assert not bundle.exists(),'Fresh frozen bundle required';shutil.copytree(Path(meta['binary']).parent,bundle/'Contents/MacOS');assert hashlib.sha256((bundle/'Contents/MacOS/ReplicaProbe.dll').read_bytes()).hexdigest()==meta['binarySha256'];(bundle/'Contents/Info.plist').write_bytes(plistlib.dumps({'CFBundleIdentifier':'com.dbmigration.replica.connection.results.'+base.name,'CFBundleName':'Results integration verification','CFBundleExecutable':'ReplicaProbe','CFBundlePackageType':'APPL','NSHighResolutionCapable':True}));print(bundle);raise SystemExit(0)
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='connection'
def raw():return json.loads(statefile.read_text())
def state(v):return v['state'] if a.source else v
def wait(predicate,after=-1):
 end=time.monotonic()+7
 while time.monotonic()<end:
  try:
   v=raw()
   if v['snapshot']>after and predicate(v):return v
  except (FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.04)
 raise RuntimeError('Fresh parent predicate failed; no input replay')
def run(mode,identity,label):
 r=subprocess.run([str(helper),mode,kind,str(bundle),str(out),identity,label],capture_output=True,text=True,timeout=12);(out/(label+'.actor.log')).write_text(r.stdout+r.stderr)
 if r.returncode:raise RuntimeError(r.stderr+r.stdout)
 return r
if a.action=='launch':
 assert not (out/('current-'+kind+'.json')).exists(),'Fresh launch required, inspect before any uncertain duplicate'
 if not a.source and a.capture_seed:
  (out/'pageintegrationresults-resume.json').write_text(json.dumps({'seed':'capture-caller','scope':'API setup new screenshotdiagnostic2 only; prior30 actual callbacks reused'}))
 if not a.source and a.resume_data:
  (out/'pageintegrationresults-resume.json').write_text(json.dumps({'seed':'post-language','scope':'API setup after proved initial5; passive4/initialinputs reused'}))
 if a.source:
  previous=sourcefile.stat().st_mtime_ns if sourcefile.exists() else -1
  resume='&resume=research' if a.resume_research else ''
  script='tell application "Google Chrome"\nset w to make new window\nset bounds of w to {80, 50, 1520, 1100}\nset URL of active tab of w to "http://10.211.55.2:8768/connection-pageintegrationresults-actual.html?os=mac'+resume+'"\nreturn id of w\nend tell'
  r=subprocess.run(['osascript','-e',script],capture_output=True,text=True,timeout=10);(out/'current-source.json').write_text(json.dumps({'returncode':r.returncode,'windowId':r.stdout.strip(),'stderr':r.stderr},indent=2)+'\n');assert r.returncode==0,r.stderr;wait(lambda v:v['index']==0 and sourcefile.stat().st_mtime_ns>previous)
 else:run('launch','sample','native-launch');wait(lambda v:v['index']==0)
 print('Own '+kind+' ready')
elif a.action=='capture-batch':
 assert not a.source,'Native remainingpaint only; originalpaint reused'
 checks=[]
 try:
  if a.from_capture==1:
   before=raw()['snapshot'];run('next','sample','capture-only-next');wait(lambda x:x['index']==1 and x['screen']=='results' and x['english']==False and x['modal'] is not None and x['focus']=='modal-close' and x['scroll']==400,after=before)
  for index in range(a.from_capture,2):
   v=wait(lambda x:x['index']==index and x['screen']=='results' and x['modal'] is not None and x['focus']=='modal-close' and x['scroll']==400);before=v['snapshot']
   run('escape','sample',f'capture-{index}-escape');v=wait(lambda x:x['index']==index,after=before);assert v['modal'] is None and v['focus']=='research' and v['scroll']==400
   checks.append({'id':f'capture-{index}','state':v,'scope':'New paint only; APIseed not actual research callback claim'})
   (out/'capture-checks.json').write_text(json.dumps(checks,indent=2,ensure_ascii=False)+'\n')
   run('capture','sample',f'capture-{index}-raw');run('capture','sample',f'capture-{index}-nominal');run('inspect','sample',f'capture-{index}-peer')
   if index==0:
    before=raw()['snapshot'];run('next','sample','capture-next');wait(lambda x:x['index']==1 and x['screen']=='results' and x['modal'] is not None and x['focus']=='modal-close' and x['scroll']==400,after=before)
 finally:
  run('close','sample',kind+'-close');time.sleep(.4);run('cleanup','sample',kind+'-cleanup')
 print('Targeted capture2 recorded/immediateclose0; pixelinspection required')
elif a.action=='inspect':run('inspect','sample',kind+'-inspect')
elif a.action=='close':
 run('close','sample',kind+'-close');time.sleep(.4);run('cleanup','sample',kind+'-cleanup');print('Exact cleanup0')
else:
 checks=json.loads((out/(kind+'-checks.json')).read_text()) if a.resume_paint or a.resume_data or a.resume_research else []
 if a.resume_data:checks=checks[:5]
 if a.resume_research:checks=checks[:10];assert len(checks)==10 and all(c['passed'] for c in checks)
 def step(mode,identity,label,predicate):
  before=raw()['snapshot'];run(mode,identity,label+'-'+kind);v=wait(lambda x:x['index']==index,after=before);s=state(v);passed=predicate(s);checks.append(dict(id=label,passed=passed,state=s));(out/(kind+'-checks.json')).write_text(json.dumps(checks,indent=2,ensure_ascii=False)+'\n');assert passed,'Fresh parent assertion failed: '+label
 try:
  for index in range(2):
   v=raw();assert v['index']==index;prefix='ko-' if index==0 else 'en-';assert state(v)['screen']==('results' if (a.resume_research or (a.resume_data and not a.source)) and index==0 else 'home')
   if not((a.resume_research or (a.resume_data and not a.source)) and index==0):
    if (a.resume_paint or a.resume_data) and index==0:
     assert [c['id'] for c in checks]==['ko-sample-go','ko-setup-filter','ko-filter-unknown']+(['ko-language-filtered','ko-setup-data'] if a.resume_data else []) and all(c['passed'] for c in checks),'Only proved completed initial3/5 reused'
     for mode,identity in [('pointer','sample'),('filter-offset','sample'),('pointer','filter:unknown')]:
      before=raw()['snapshot'];run(mode,identity,'resume-setup-'+mode+'-'+kind);wait(lambda v:v['index']==0,after=before)
    else:
     step('pointer','sample',prefix+'sample-go',lambda s:s['screen']=='results' and s['focus']=='screen-title' and s['scroll']==0)
     step('filter-offset','sample',prefix+'setup-filter',lambda s:s['scroll']==400)
     step('pointer','filter:unknown',prefix+'filter-unknown',lambda s:s['filter']=='unknown' and s['focus']=='filter:unknown' and s['scroll']==400 and s['articleIds']==['U-001'])
    if a.resume_data and index==0:
     for mode,identity in [('pointer','language'),('zero','sample')]:
      before=raw()['snapshot'];run(mode,identity,'resume-setup-'+mode+'-'+identity+'-'+kind);wait(lambda v:v['index']==0,after=before)
    else:
     run('capture','sample',prefix+kind+'-filter-pointer-paint')
     step('pointer','language',prefix+'language-filtered',lambda s:s['english']==(index==0) and s['filter']=='unknown' and s['focus']=='language' and s['scroll']==400)
     step('zero','sample',prefix+'setup-data',lambda s:s['scroll']==0)
   for identity,offset,mode in [('detail:data',0,None),('detail:trial',400,'filter-offset'),('research',400,'research-offset')]:
    if a.resume_research and index==0 and identity!='research':continue
    label=identity.replace(':','-')
    if mode:step(mode,'sample',prefix+'setup-'+label,lambda s:s['scroll']==offset)
    step('pointer',identity,prefix+label+'-open',lambda s:s['modal'] is not None and s['focus']=='modal-close' and not s['backgroundEnabled'])
    step('escape','sample',prefix+label+'-close',lambda s:s['modal'] is None and s['focus']==identity and s['scroll']==offset and s['backgroundEnabled'])
    if identity=='research':run('capture','sample',prefix+kind+'-research-caller-paint')
   step('pointer','save',prefix+'save-go',lambda s:s['screen']=='save' and s['focus']=='screen-title' and s['scroll']==0)
   step('pointer','back',prefix+'save-back',lambda s:s['screen']=='results' and s['filter']=='unknown' and s['focus']=='screen-title' and s['scroll']==0)
   run('capture','sample',prefix+kind+'-paint');run('inspect','sample',prefix+kind+'-ax')
   if index==0:
    before=raw()['snapshot'];run('next','sample',kind+'-setup-next');wait(lambda v:v['index']==1,after=before)
 finally:
  run('close','sample',kind+'-close');time.sleep(.4);run('cleanup','sample',kind+'-cleanup')
 print('Scoped parent callbacks recorded; source/native strict comparison still required; exactcleanup0')
