"""Keep one owned native child alive until scoped close, with frozen binary/hash guard."""
from pathlib import Path
import argparse,hashlib,json,os,subprocess,uuid
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('--input-only',action='store_true');a=p.parse_args();a.manifest=a.manifest.resolve();m=json.loads(a.manifest.read_text());assert m['owner']=='root' and m['component']=='progresspage' and m['buildExitCode']==0 and m['runtime']=='osx-arm64'
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='root'
binary=Path(m['binary']);assert hashlib.sha256(binary.read_bytes()).hexdigest()==m['binarySha256'];out=a.manifest.parent/'actual-macos'/uuid.uuid4().hex;out.mkdir(parents=True)
env=os.environ.copy();env.pop('DBMV_HEADLESS',None);env.pop('DBMV_HEADLESS_SCALE',None)
dotnet='/Users/youptebook/.cache/db-migration-verifier/dotnet/dotnet';args=[dotnet,str(binary),'--progresspage-window',str(r/'docs/replica/source/root-progresspage-cases.json'),str(out)]
if a.input_only:args.append('--native-input-only')
with (out/'native.log').open('w')as log:
 child=subprocess.Popen(args,cwd=str(binary.parent),env=env,stdout=log,stderr=log)
 v={'pid':child.pid,'binary':str(binary),'output':str(out),'manifest':str(a.manifest.resolve()),'binarySha256':m['binarySha256'],'headless':False}
 (a.manifest.parent/'current-native.json').write_text(json.dumps(v,indent=2)+'\n');print(json.dumps(v),flush=True)
 try:code=child.wait()
 except BaseException:
  child.terminate();child.wait(timeout=5);raise
print('Owned native exited',code,flush=True)
assert code==0
