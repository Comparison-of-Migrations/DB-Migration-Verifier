"""One guarded native30 summary scope; close exact own candidate immediately."""
from pathlib import Path
import argparse,json,subprocess,time
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('--peer-only',action='store_true');p.add_argument('action',choices=['launch','batch','close']);a=p.parse_args();m=a.manifest.resolve();meta=json.loads(m.read_text());assert meta['owner']=='home' and meta['component']=='pageintegration' and meta['runtime']=='osx-arm64'
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='home'
out=m.parent/('actual-summary-peer-macos' if a.peer_only else 'actual-summary-content-macos');out.mkdir(exist_ok=True);bundle=m.parent/'PageIntegrationSummaryContentCheck.app';helper=ROOT/'artifacts/replica/workers/home/pageintegration/page-integration-summary-content'
def run(mode,value='',label='scope'):
 r=subprocess.run([str(helper),mode,str(bundle),str(out),str(value),label],text=True,capture_output=True,timeout=12);(out/(label+'.actor.log')).write_text(r.stdout+r.stderr)
 if r.returncode:raise RuntimeError(r.stdout+r.stderr)
def current():
 error=out/'pageintegration-summary-content-observer-error.json'
 if error.exists():raise RuntimeError('Owned observer failed '+error.read_text())
 files=sorted(out.glob('pageintegration-summary-content-state-*.json'))
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
 raise RuntimeError('Fresh native predicate failed; no input replay')
def close():
 run('close',label='native-close');time.sleep(.4);run('cleanup',label='native-cleanup');print('Exact own cleanup0',flush=True)
if a.action=='launch':
 run('launch','peer-only' if a.peer_only else '',label='native-launch');v=wait(lambda v:v['setupReady'] and v['index']==0);print('Ready',v['snapshot'],flush=True)
elif a.action=='close':close()
else:
 rows=[]
 try:
  for index in range(2 if a.peer_only else 10):
   if index:run('next',label='context-'+str(index)+'-seed')
   wait(lambda v:v['setupReady'] and v['index']==index)
   for width in ([960,1280] if a.peer_only else [960,1180,1280]):
    before=current();run('resize',width,'context-'+str(index)+'-'+str(width));v=wait(lambda v:v['snapshot']>before['snapshot']+1 and abs(v['state']['width']-width)<.01);rows.append(v['state'])
    if width==960 or a.peer_only:run('ax',label='context-'+str(index)+'-'+str(width))
    (out/'pageintegration-summary-content.json').write_text(json.dumps({'phase':'ACTUAL_OS_RECORDED','scope':'summary-content30','rows':rows},ensure_ascii=False,indent=2)+'\n');print(v['state']['id'],flush=True)
 except Exception as e:
  (out/'batch-failure.json').write_text(json.dumps({'phase':'UNVERIFIED','exception':str(e),'cases':len(rows),'replayedInput':False},indent=2)+'\n');raise
 finally:close()
