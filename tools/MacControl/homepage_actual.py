"""Scoped frozen HomePage actual input; product cases proved by the separate auto snapshot."""
from pathlib import Path
import argparse,hashlib,json,os,subprocess,time
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['start','inputs','close','cleanup']);p.add_argument('manifest',type=Path);p.add_argument('--english-only',action='store_true');p.add_argument('--passive-only',action='store_true');a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='home';m=json.loads(a.manifest.read_text());assert m['owner']=='home' and m['component']=='homepage' and m['runtime']=='osx-arm64';bundle=a.manifest.resolve().parent/'HomePageCheck.app';out=a.manifest.resolve().parent/'actual-macos';exe=bundle/'Contents/MacOS/ReplicaProbe';assert hashlib.sha256((exe.parent/'ReplicaProbe.dll').read_bytes()).hexdigest()==m['binarySha256'];helper=r/'artifacts/replica/workers/home/homepage-input'
def count():
 lines=subprocess.check_output(['ps','-axo','pid=,command='],text=True).splitlines();return sum(l.strip().split(maxsplit=1)[1].split()[0]==str(exe) for l in lines if len(l.strip().split(maxsplit=1))>1)
def run(mode,target='start'):
 v=subprocess.run([str(helper),mode,str(bundle),str(out),target],text=True,capture_output=True);
 if v.returncode:raise RuntimeError(v.stderr+v.stdout)
 return v.stdout
if a.action=='start':
 assert count()==0 and not (out/'homepage-input.json').exists();env=os.environ.copy();env.pop('DBMV_HEADLESS',None);env.pop('DBMV_HEADLESS_SCALE',None);subprocess.run(['open','-n',str(bundle),'--args','--homepage-window',str(out/'cases.json'),str(out)],env=env,check=True);deadline=time.monotonic()+8
 while not (out/'homepage-input.json').exists() and time.monotonic()<deadline:time.sleep(.05)
 assert (out/'homepage-input.json').exists();print('Owned input fixture started')
elif a.action=='inputs':
 for language in (['en'] if a.english_only else ['ko','en']):
  state=json.loads((out/'homepage-input.json').read_text());assert state['english']==(language=='en');before=len(state['actions']);run('inspect');(out/(language+'-homepage-ax.json')).write_bytes((out/'homepage-ax.json').read_bytes())
  for target in ([] if a.passive_only else ['start','open','sample','research']):
   for mode in ['pointer','enter','space']:
    run(mode,target);state=json.loads((out/'homepage-input.json').read_text());before+=1;assert len(state['actions'])==before and state['actions'][-1]==target
  for mode in ['passive-help','passive-hint','passive-gap']:
   run(mode);state=json.loads((out/'homepage-input.json').read_text());assert len(state['actions'])==before;assert state['focused']=='home-body' if a.passive_only else True;print(language,mode,state['focused'])
  if a.passive_only:
   for mode in ['neutral-enter','neutral-space']:
    run(mode);state=json.loads((out/'homepage-input.json').read_text());assert len(state['actions'])==before and state['focused']=='home-body'
  if language=='ko':run('language')
 print('Actual neutral passive6/keys4 recorded' if a.passive_only else 'Actual pointer/Return/Space24 plus passive6 recorded')
elif a.action=='close':print(run('close'))
else:
 assert count()==0;(r/'docs/replica/source/homepage-mac-input-cleanup.json').write_text(json.dumps({'nativeProcess':0,'nativeWindow':0,'path':str(exe)},indent=2)+'\n');print('Exact native0')
