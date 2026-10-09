"""Only external native window resize8; immutable fresh observer state, immediate close0."""
from pathlib import Path
import argparse,json,subprocess,time
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('action',choices=['launch','batch','close']);a=p.parse_args();m=a.manifest.resolve();meta=json.loads(m.read_text());assert meta['owner']=='home' and meta['component']=='pageintegration' and meta['runtime']=='osx-arm64'
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='home'
out=m.parent/'actual-connection-resize-macos';out.mkdir(exist_ok=True);bundle=m.parent/'PageIntegrationConnectionResizeCheck.app';helper=ROOT/'artifacts/replica/workers/home/pageintegration/page-integration-connection-resize'
def run(mode,value='',label='scope'):
 r=subprocess.run([str(helper),mode,str(bundle),str(out),str(value),label],text=True,capture_output=True,timeout=12);(out/(label+'.actor.log')).write_text(r.stdout+r.stderr)
 if r.returncode:raise RuntimeError(r.stdout+r.stderr)
def current():
 error=out/'pageintegration-connection-resize-observer-error.json'
 if error.exists():raise RuntimeError('Owned observer failed '+error.read_text())
 files=sorted(out.glob('pageintegration-connection-resize-state-*.json'))
 if not files:raise FileNotFoundError('Observer not ready')
 return json.loads(files[-1].read_text())
def wait(predicate):
 end=time.monotonic()+5
 while time.monotonic()<end:
  try:
   v=current()
   if predicate(v):return v
  except (FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.04)
 raise RuntimeError('Fresh actual native predicate failed; no input replay')
def close():
 run('close',label='native-close');time.sleep(.4);run('cleanup',label='native-cleanup');print('Exact own cleanup0',flush=True)
if a.action=='launch':run('launch',label='native-launch');v=wait(lambda v:v['setupReady'] and v['language']=='ko');print('Ready snapshot',v['snapshot'],flush=True)
elif a.action=='close':close()
else:
 rows=[]
 try:
  default=[];expanded=[]
  for phase in range(4):
   lang='en' if phase%2 else 'ko';is_expanded=phase>=2
   if phase:
    before=current()
    if abs(before['width']-1280)>.01:run('resize',1280,'phase-'+str(phase)+'-baseline-size');wait(lambda v:abs(v['width']-1280)<.01 and v['snapshot']>before['snapshot'])
    run('next',label='phase-'+str(phase)+'-baseline-seed')
   wait(lambda v:v['setupReady'] and v['phaseIndex']==phase)
   for index,width in enumerate([1280,1100] if is_expanded else [1280,1100,960,1280]):
    label=lang+('-advanced-' if is_expanded else '-')+str(index);before=current();run('resize',width,label);v=wait(lambda v:v['snapshot']>before['snapshot']+1 and abs(v['width']-width)<.01);v.update(id=label,actualWidth=v['width']);rows.append(v);(expanded if is_expanded else default).append(v)
    suffix='-expanded' if is_expanded else ''
    (out/('pageintegration-connection-resize'+suffix+'.json')).write_text(json.dumps({'phase':'ACTUAL_OS_RECORDED','scope':'Actual macOS external AXSize; F8 language/expanded baseline seed only','rows':expanded if is_expanded else default},ensure_ascii=False,indent=2)+'\n');print(v['id'],v['width'],v['pageRetained'],v['selectionStart'],v['selectionEnd'],flush=True)
 except Exception as e:
  (out/'batch-failure.json').write_text(json.dumps({'phase':'UNVERIFIED','exception':str(e),'cases':len(rows),'replayedInput':False},indent=2)+'\n');raise
 finally:close()
