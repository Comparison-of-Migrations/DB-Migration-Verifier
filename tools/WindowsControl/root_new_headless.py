"""Frozen parent/new-scope headless only; unique runtime/output, never OS input."""
from pathlib import Path
import argparse,base64,hashlib,io,json,uuid,zipfile
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('scope',choices=['home','progress','progress-diagnostic','progress-hover']);a=p.parse_args();m=json.loads(a.manifest.read_text());s=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='root' and m['component']==('pageintegration' if a.scope=='home' else 'progresspage') and m['buildExitCode']==0 and s['selfContained'];assert 'root' in json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['headless']['owners'];assert hashlib.sha256(Path(m['binary']).read_bytes()).hexdigest()==m['binarySha256']
nonce=uuid.uuid4().hex;out=a.manifest.parent/'windows-new-headless'/nonce;out.mkdir(parents=True);cases=r/('docs/replica/source/root-pageintegration-home-cases.json' if a.scope=='home' else 'docs/replica/source/root-progresspage-cases.json');data=cases.read_bytes();served='root-'+a.scope+'-'+nonce+'-cases.json';(r/'artifacts/replica/windows-download'/served).write_bytes(data);sha=hashlib.sha256(data).hexdigest().upper()
base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';$out=Join-Path $app "+q('new-headless-'+nonce)+";"
code=base+"$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory $out -Force|Out-Null;$cases=Join-Path $out 'cases.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $cases;if((Get-FileHash $cases -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Cases hash mismatch'};if((Get-FileHash ($app+'\\ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(m['binarySha256'].upper())+"){throw 'Binary hash mismatch'};$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';& ($app+'\\ReplicaProbe.exe') "+q('--pageintegration' if a.scope=='home' else '--progresspage')+" $cases $out "+('--home-only --home-actions-only' if a.scope=='home' else '--hover-diagnostic-only' if a.scope=='progress-diagnostic' else '--hover-only' if a.scope=='progress-hover' else '--interaction-only')+";if($LASTEXITCODE-ne0){throw 'New headless scope failed'};Add-Type -AssemblyName System.IO.Compression.FileSystem;$zip=$out+'.zip';[IO.Compression.ZipFile]::CreateFromDirectory($out,$zip);[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip));"
raw=base64.b64decode(ps(code,timeout=60))
with zipfile.ZipFile(io.BytesIO(raw)) as z:
 for name in z.namelist():
  assert Path(name).name==name
  if name.endswith('.json'):(out/name).write_text(z.read(name).decode('utf-8-sig'))
  else:(out/name).write_bytes(z.read(name))
if a.scope=='home':
 checks=json.loads((out/'pageintegration-home-checks.json').read_text());assert len(checks)==15 and all(x['pass'] for x in checks)
elif a.scope=='progress-diagnostic':
 checks=json.loads((out/'progresspage-hover-diagnostic.json').read_text());assert len(checks)==4
else:
 checks=json.loads((out/'progresspage-headless-input.json').read_text())['states'];assert len(checks)==(7 if a.scope=='progress-hover' else 18) and all(x['passed'] for x in checks);assert len(json.loads((out/'progresspage-timing.json').read_text())['rows'])==48
(out/'environment.json').write_text(json.dumps({'phase':'HEADLESS_RECORDED' if a.scope=='progress-diagnostic' else 'HEADLESS_PASSED','manifest':str(a.manifest.resolve()),'binarySha256':m['binarySha256'],'scope':a.scope,'headless':True,'rendering':'realSkia','casesSha256':sha,'newChecks':len(checks),'actualOSStillRequired':True},indent=2)+'\n');print(out)
