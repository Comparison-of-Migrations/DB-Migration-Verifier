"""Four read-only actual Mac peer observations on one hash-guarded frozen window."""
from pathlib import Path
import argparse,hashlib,json,subprocess,time,uuid
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);a=p.parse_args();m=json.loads(a.manifest.read_text())
assert m['owner']=='root' and m['component'] in ['actionbuttonsemantic'] and m['buildExitCode']==0
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='root'
binary=Path(m['binary']);assert hashlib.sha256(binary.read_bytes()).hexdigest()==m['binarySha256']
flag='actionbuttonsemantic'
out=a.manifest.parent/'actual-mac'/uuid.uuid4().hex;out.mkdir(parents=True)
helper=r/'artifacts/replica/workers/root/actionbuttonsemantic/action-button-semantics-inspect'
log=(out/'native.log').open('w');process=subprocess.Popen(['/Users/youptebook/.cache/db-migration-verifier/dotnet/dotnet',str(binary),'--'+flag+'-window','unused',str(out)],stdout=log,stderr=log,cwd=r)
deadline=time.monotonic()+12
while not(out/(flag+'-native.json')).exists() and process.poll() is None and time.monotonic()<deadline:time.sleep(.1)
assert (out/(flag+'-native.json')).exists(),'Opened metadata missing; keep owned diagnostic window'
capture=out/'actionbuttonsemantic-actual-ax.json'
subprocess.run([str(helper),str(process.pid),'inspect',str(binary),str(capture)],check=True,cwd=r)
subprocess.run([str(helper),str(process.pid),'close',str(binary),str(out/'cleanup.json')],check=True,cwd=r);assert process.wait(timeout=5)==0;log.close()
v=json.loads(capture.read_text());rows={x['id'].removeprefix('ButtonSemantics-'):x for x in v['rows']}
assert len(rows)==8
for key,row in rows.items():
    name='Required permissions' if key.startswith('en-') else '필요한 권한 보기'
    assert row['role']=='AXButton' and row['children']==1,(key,row)
    assert name in [row['title'],row['description'],row['value']],(key,row)
(out/'comparison.json').write_text(json.dumps({'passed':True,'scope':'eight actual Mac unchanged default Button role/name/child1; regression scope, not new original descendant equivalence','processId':process.pid,'cleanup':0,'manifest':str(a.manifest.resolve()),'binarySha256':m['binarySha256']},indent=2)+'\n');print(out)
