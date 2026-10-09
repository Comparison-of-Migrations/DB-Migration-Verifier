"""One frozen owned Progress window. Resume after failed observation, never repeat input."""
from pathlib import Path
import argparse,json,subprocess,time
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('--from-step',default='initial',choices=['initial','normal-hover','normal-leave','live','reduced','reduced-hover','reduced-leave','language','detach','remount','interruption','cleanup']);a=p.parse_args()
m=json.loads(a.manifest.read_text());v=json.loads((a.manifest.parent/'current-native.json').read_text());out=Path(v['output']);assert m['component']=='progresspage' and m['owner']=='root';assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='root'
helper=r/'artifacts/replica/workers/root/progresspage/progress-page-input';capture=r/'artifacts/replica/workers/root/progresspage/progress-page-capture'
def raw():return json.loads((out/'progresspage-actual-state.json').read_text())
def run(mode,identity='Progress-row-data'):
 x=subprocess.run([str(helper),str(v['pid']),mode,v['binary'],identity,str(out/'progress-ax.json')],capture_output=True,text=True);(out/('helper-'+mode+'-'+identity+'.log')).write_text(x.stdout+x.stderr);assert x.returncode==0,x.stderr+x.stdout

def save(step,test,prior=-1):
 end=time.monotonic()+4
 while time.monotonic()<end:
  x=raw()
  if x['snapshot']>prior and test(x):
   (out/('actual-'+step+'.json')).write_text(json.dumps(x,indent=2)+'\n');print(step,flush=True);return x
  time.sleep(.02)
 raise RuntimeError('Observation failed '+step+'; do not repeat input; '+json.dumps(raw()))
def act(step,mode,identity,test):
 old=raw()['snapshot'];run(mode,identity);return save(step,test,old)
def snap(name):
 x=subprocess.run([str(capture),str(v['pid']),v['binary'],str(out/(name+'.png'))],capture_output=True,text=True);assert x.returncode==0,x.stderr+x.stdout
steps=['initial','normal-hover','normal-leave','live','reduced','reduced-hover','reduced-leave','language','detach','remount','interruption','cleanup']
for step in steps[steps.index(a.from_step):]:
 if step=='initial':
  save(step,lambda s:not s['headless'] and s['dpr']==2 and s['attached']);run('inspect');(out/'actual-ko-ax.json').write_bytes((out/'progress-ax.json').read_bytes());snap('actual-ko')
 elif step=='normal-hover':act(step,'move-row','Progress-row-data',lambda s:s['rows'][0]['pointer'] and s['rows'][0]['background']=='#ff3a3c44');snap('actual-hover')
 elif step=='normal-leave':act(step,'move-out','outside',lambda s:not s['rows'][0]['pointer'] and s['rows'][0]['background']=='#00000000')
 elif step=='live':
  act(step,'key','F10',lambda s:s['phase']=='live' and s['run']['Groups'][0]['Done']==1);time.sleep(.75);frames=json.loads((out/'progresspage-actual-motion.json').read_text());assert len(frames)>5
  for index in range(4):
   cells=[f['state']['rows'][index]['cells'][1] for f in frames];assert any(c['opacity']<.999 and c['y']>0 for c in cells),'Initial live Cell must animate';assert cells[-1]['opacity']==1 and cells[-1]['y']==0
   assert all(f['state']['rows'][index]['pillOpacity']==1 for f in frames),'Pill must not own animation'
  (out/'actual-live-check.json').write_text(json.dumps({'frames':len(frames),'firstLiveCells':4,'owner':'Cell','passed':True})+'\n')
 elif step=='reduced':act(step,'key','F6',lambda s:s['reduced'])
 elif step=='reduced-hover':act(step,'move-row','Progress-row-data',lambda s:s['rows'][0]['pointer'] and s['rows'][0]['background']=='#ff3a3c44')
 elif step=='reduced-leave':act(step,'move-out','outside',lambda s:not s['rows'][0]['pointer'] and s['rows'][0]['background']=='#00000000')
 elif step=='language':
  act(step,'key','F8',lambda s:s['english']);run('inspect');(out/'actual-en-ax.json').write_bytes((out/'progress-ax.json').read_bytes());snap('actual-en')
 elif step=='detach':act(step,'key','F7',lambda s:not s['attached'] and len(s['rows'])==0)
 elif step=='remount':act(step,'key','F7',lambda s:s['attached'] and len(s['rows'])==4)
 elif step=='interruption':act(step,'key','cancel',lambda s:s['run']['Interrupted'])
 else:
  run('close');end=time.monotonic()+5
  while time.monotonic()<end and subprocess.run(['kill','-0',str(v['pid'])],capture_output=True).returncode==0:time.sleep(.05)
  assert subprocess.run(['kill','-0',str(v['pid'])],capture_output=True).returncode!=0
  (out/'cleanup.json').write_text(json.dumps({'nativeProcess':0,'nativeWindow':0,'pid':v['pid']})+'\n')
