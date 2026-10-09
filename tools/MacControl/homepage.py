"""Actual Mac scoped Steps exception; own frozen bundle, no user window/input."""
from pathlib import Path
import argparse,hashlib,json,os,subprocess,time
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='home';m=json.loads(a.manifest.read_text());assert m['owner']=='home' and m['component']=='homepage' and m['runtime']=='osx-arm64' and m['buildExitCode']==0;bundle=a.manifest.resolve().parent/'HomePageCheck.app';exe=bundle/'Contents/MacOS/ReplicaProbe';assert hashlib.sha256((exe.parent/'ReplicaProbe.dll').read_bytes()).hexdigest()==m['binarySha256'];out=a.manifest.resolve().parent/'actual-macos';target=out/'homepage-native.json';assert not target.exists()
def count():
 lines=subprocess.check_output(['ps','-axo','pid=,command='],text=True).splitlines();return sum(l.strip().split(maxsplit=1)[1].split()[0]==str(exe) for l in lines if len(l.strip().split(maxsplit=1))>1)
assert count()==0;env=os.environ.copy();env.pop('DBMV_HEADLESS',None);env.pop('DBMV_HEADLESS_SCALE',None);subprocess.run(['open','-W','-n',str(bundle),'--args','--homepage',str(out/'cases.json'),str(out)],env=env,check=True,timeout=45);assert target.exists();v=json.loads(target.read_text());assert len(v['rows'])==16 and v['dpr']==2
deadline=time.monotonic()+3
while count()!=0 and time.monotonic()<deadline:time.sleep(.05)
assert count()==0;(out/'environment.json').write_text(json.dumps({'phase':'ACTUAL_MACOS','headless':False,'manifest':str(a.manifest.resolve()),'binarySha256':m['binarySha256'],'bundle':str(bundle),'dpr':v['dpr'],'cases':16,'lifecycle':3},indent=2)+'\n');(r/'docs/replica/source/homepage-mac-cleanup.json').write_text(json.dumps({'nativeProcess':0,'nativeWindow':0,'path':str(exe),'ownSourceDomTab':14,'sourceNativeWindow':0},indent=2)+'\n');print('Actual16/exact owned native0')
