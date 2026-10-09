"""Own parent input batch; existing passed Trial child conditions are never replayed."""
from pathlib import Path
import argparse,json,subprocess,time,os,hashlib
ROOT=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('action',choices=['launch','batch','targeted','boundary','inspect','close']);p.add_argument('--source',action='store_true');p.add_argument('--validation',action='store_true');p.add_argument('--from-index',type=int,default=0);p.add_argument('--phase',default='');a=p.parse_args();m=a.manifest.resolve();base=m.parent;assert not a.phase or a.phase.replace('-','').isalnum()
out=base/'actual-macos'/a.phase if a.phase else base/'actual-macos';out.mkdir(parents=True,exist_ok=True)
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='home'
helper=ROOT/'artifacts/replica/workers/home/pageintegration/page-integration-trial-input';bundle=base/'PageIntegrationTrialCheck.app';statefile=ROOT/'docs/replica/source/home-pageintegrationtrial-mac-input.json' if a.source else out/'pageintegration-trial-actual-state.json'
def raw():return json.loads(statefile.read_text())
def wait(predicate,after=-1):
 end=time.monotonic()+6
 while time.monotonic()<end:
  try:
   v=raw()
   if v['snapshot']>after and predicate(v):return v
  except (FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.04)
 raise RuntimeError('Fresh predicate failed; no input replay')
def run(mode,identity,label):
 cmd=[str(helper),mode,'source' if a.source else 'native',str(bundle),str(out),identity,label];r=subprocess.run(cmd,capture_output=True,text=True,timeout=12);(out/(label+'.actor.log')).write_text(r.stdout+r.stderr)
 if r.returncode:raise RuntimeError(r.stderr+r.stdout)
 return r
if a.action=='launch':
 before=statefile.stat().st_mtime_ns if statefile.exists() else 0
 if a.source:
  suffix='?validation=1' if a.validation else ''
  script='tell application "Google Chrome"\nset w to make new window\nset bounds of w to {80, 100, 1360, 1080}\nset URL of active tab of w to "http://10.211.55.2:8768/home-pageintegrationtrial-input.html'+suffix+'"\nreturn id of w\nend tell'
  r=subprocess.run(['osascript','-e',script],capture_output=True,text=True,timeout=10);(out/'source-launch.json').write_text(json.dumps({'returncode':r.returncode,'windowId':r.stdout.strip(),'stderr':r.stderr},indent=2)+'\n');assert r.returncode==0,r.stderr;wait(lambda v:v['index']==0 and v['caseId']==('ko-bom-valid' if a.validation else 'ko-disabled') and statefile.stat().st_mtime_ns>before);print('Source ready')
 else:
  run('launch','validation' if a.validation else 'primary','native-launch');wait(lambda v:v['index']==0 and v['caseId']==('ko-bom-valid' if a.validation else 'ko-disabled') and statefile.stat().st_mtime_ns>before);print('Native ready',json.loads((out/'current-native.json').read_text())['pid'])
elif a.action=='inspect':run('inspect','primary',('source' if a.source else 'native')+'-inspect')
elif a.action=='close':
 run('close','primary',('source' if a.source else 'native')+'-close');time.sleep(.4);run('cleanup','primary',('source' if a.source else 'native')+'-cleanup');print('Exact cleanup0')
elif a.action=='boundary':
 assert a.validation,'Boundary scope flag required'
 names=['bom-valid','nel-valid','bom-host','nel-host'];kind='source' if a.source else 'native'
 for index in range(a.from_index,8):
  current=raw();assert current['index']==index,(current['index'],index)
  assert current['caseId']==('ko-' if index<4 else 'en-')+names[index%4]
  label=('ko-' if index<4 else 'en-')+names[index%4]+'-'+kind;prior=current['snapshot'];run('pointer','primary',label)
  fresh=wait(lambda v:v['index']==index,prior);assert fresh['caseId']==label.removesuffix('-'+kind)
  (out/(label+'.json')).write_text(json.dumps(fresh,indent=2,ensure_ascii=False)+'\n');print(label,fresh['state']['ready'],flush=True)
  if index<7:
   run('next','primary','boundary-setup-'+str(index+1)+'-'+kind);wait(lambda v:v['index']==index+1)
 run('close','primary',kind+'-boundary-close');time.sleep(.4);run('cleanup','primary',kind+'-boundary-cleanup');print('Boundary complete/exactcleanup0')
elif a.action=='targeted':
 kind='source' if a.source else 'native'
 for index in (0,14):
  while raw()['index']<index:
   next_index=raw()['index']+1;run('next','primary','target-setup-'+str(next_index)+'-'+kind);wait(lambda v:v['index']==next_index)
  current=raw();assert current['index']==index
  label=('ko-' if index==0 else 'en-')+'disabled-'+kind;prior=current['snapshot'];retained=out/('target-'+label+'.json');reuse=not a.source and index==0 and retained.exists()
  run('pointer','primary','target-'+label+('-paint-setup' if reuse else ''));fresh=wait(lambda v:v['index']==index,prior)
  (out/('target-'+label+('-paint-state' if reuse else '')+'.json')).write_text(json.dumps(fresh,indent=2,ensure_ascii=False)+'\n')
  for mode in (() if reuse else ('neutral-space','neutral-enter','activate')):
   prior=fresh['snapshot'];run(mode,'primary','target-'+label+'-'+mode);fresh=wait(lambda v:v['index']==index,prior);assert fresh['state']['screen']=='trial' and not fresh['state']['ready'] and not fresh['state']['consent'],'Neutral action changed model; do not replay';(out/('target-'+label+'-'+mode+'.json')).write_text(json.dumps(fresh,indent=2,ensure_ascii=False)+'\n')
  run('inspect','primary','target-'+label+'-ax');run('capture','primary','target-'+label+'-paint');print('Targeted',label,fresh['state']['focus'],flush=True)
 run('close','primary',kind+'-close');time.sleep(.4);run('cleanup','primary',kind+'-cleanup');print('Targeted complete/exactcleanup0')

else:
 names=['disabled','inputs-priority','invalid-empty','host-empty','db-empty','same-source','same-target','exact-whitespace','success','stale-success','edit-reset','consent-reset','return-reopen','language'];kind='source' if a.source else 'native'
 for index in range(a.from_index,28):
  current=raw();assert current['index']==index,(current['index'],index)
  name=names[index%14];label=('ko-' if index<14 else 'en-')+name+'-'+kind;prior=current['snapshot']
  if name=='edit-reset':run('edit','trial-validInput',label)
  elif name=='consent-reset':run('pointer','flag-trial.consent',label)
  elif name=='language':run('pointer','language',label)
  else:run('pointer','primary',label)
  fresh=wait(lambda v:v['index']==index,prior);(out/(label+'.json')).write_text(json.dumps(fresh,indent=2,ensure_ascii=False)+'\n');print(label,fresh['state']['screen'],fresh['state']['ready'],flush=True)
  if name=='disabled':
   prior=fresh['snapshot'];run('pointer','back',('ko-' if index<14 else 'en-')+'quiet-back-'+kind);fresh=wait(lambda v:v['state']['screen']=='select',prior);(out/(('ko-' if index<14 else 'en-')+'quiet-back-'+kind+'.json')).write_text(json.dumps(fresh,indent=2,ensure_ascii=False)+'\n')
  if name=='return-reopen':assert fresh['state']['screen']=='select','Return navigation failed; no replay'
  if index<27:
   run('next','primary','setup-'+str(index+1)+'-'+kind);wait(lambda v:v['index']==index+1)
 run('inspect','primary',kind+'-final-ax');run('close','primary',kind+'-close');time.sleep(.4);run('cleanup','primary',kind+'-cleanup');print('Batch complete and exact cleanup0')
