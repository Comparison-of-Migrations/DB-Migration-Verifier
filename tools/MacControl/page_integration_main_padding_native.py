"""Finite blank left-padding physical wheel/click; prepared original oracle required."""
from pathlib import Path
import argparse,json,shutil,plistlib,hashlib,subprocess,time
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('action',choices=['prepare','batch']);a=p.parse_args();m=a.manifest.resolve();v=json.loads(m.read_text());assert v['owner']=='preflight'and v['component']=='pageintegration'and v['runtime']=='osx-arm64'and v['buildExitCode']==0
base=m.parent;bundle=base/'MainPaddingCheck.app';out=base/'actual-main-padding-mac';actor=r/'artifacts/replica/workers/preflight/mainpadding/native-input';launcher=r/'artifacts/replica/workers/preflight/mainpadding/native-launch'
if a.action=='prepare':
 assert not bundle.exists();shutil.copytree(Path(v['binary']).parent,bundle/'Contents/MacOS');assert hashlib.sha256((bundle/'Contents/MacOS/ReplicaProbe.dll').read_bytes()).hexdigest()==v['binarySha256'];(bundle/'Contents/Info.plist').write_bytes(plistlib.dumps(dict(CFBundleIdentifier='local.dbmv.preflight.mainpadding.'+base.name,CFBundleName='Main padding verification',CFBundleExecutable='ReplicaProbe',CFBundlePackageType='APPL',NSHighResolutionCapable=True)));print(bundle);raise SystemExit
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight';assert not out.exists();out.mkdir();(out/'cases.json').write_text('[]\n');shutil.copyfile(r/'artifacts/replica/workers/preflight/mainpadding-source/463b51bd272f400f85316c4ee234ea42/source.json',out/'oracle.json')
launch=subprocess.run([str(launcher),str(m),str(out),'padding'],capture_output=True,text=True,timeout=15);(out/'launch.log').write_text(launch.stdout+launch.stderr);assert launch.returncode==0,launch.stderr
identity=out/'current-native.json';pid=json.loads(identity.read_text())['pid'];checks=[]
def raw():
 files=sorted(out.glob('main-padding-native-state-*.json'));assert files;return files[-1],json.loads(files[-1].read_text())
def wait(predicate,after=''):
 end=time.monotonic()+30
 while time.monotonic()<end:
  try:
   path,state=raw()
   if path.name>after and predicate(state):return path,state
  except(AssertionError,FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.04)
 raise RuntimeError('Fresh native padding state absent; no replay')
def run(mode,label,state):
 result=subprocess.run([str(actor),str(pid),'native',mode,str(identity),str(state),label,str(out)],capture_output=True,text=True,timeout=10);(out/(label+'.actor.log')).write_text(result.stdout+result.stderr);assert result.returncode==0,result.stderr
try:
 for index in range(4):
  path,before=wait(lambda s:s['ready']and s['index']==index);label=before['id'];assert before['hit']['viewportAncestor']and before['offset']==0
  run('wheel',label+'-wheel',path);wheel_path,wheel=wait(lambda s:s['offset']>0 and s['index']==index,path.name);assert abs(wheel['offset']-100)<.05
  run('click',label+'-click',wheel_path);click_path,click=wait(lambda s:s['focus']['type']=='WebScrollViewport'and s['index']==index,wheel_path.name)
  assert not click['isFocusVisible']and not click['isTabStop']
  checks.append({'id':label,'before':str(path),'wheel':str(wheel_path),'click':str(click_path),'offset':click['offset'],'focusAfterWheel':wheel['focus'],'focusAfterClick':click['focus'],'isFocusVisible':click['isFocusVisible'],'isTabStop':click['isTabStop'],'pass':True});(out/'comparison.json').write_text(json.dumps({'scope':'New blank leftpadding trusted native4; source comparison still separate','checks':checks},indent=2)+'\n')
  if index<3:run('next',label+'-fixture-next',click_path)
finally:
 try:
  try:path,_=raw()
  except (AssertionError,FileNotFoundError,json.JSONDecodeError):path=out/'oracle.json'
  run('close','final-close',path)
 finally:
  end=time.monotonic()+5
  while time.monotonic()<end and subprocess.run(['kill','-0',str(pid)],capture_output=True).returncode==0:time.sleep(.05)
  absent=subprocess.run(['kill','-0',str(pid)],capture_output=True).returncode!=0;(out/'cleanup.json').write_text(json.dumps({'pid':pid,'nativeProcess':0 if absent else 1,'nativeWindow':0 if absent else None,'checksCompleted':len(checks)},indent=2)+'\n');assert absent
print(out)
