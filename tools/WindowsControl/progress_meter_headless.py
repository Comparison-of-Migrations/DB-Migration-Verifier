"""Independent real-rendering ProgressMeter Windows process; no native input."""
from pathlib import Path
import argparse,base64,hashlib,io,json,uuid,zipfile
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);a=p.parse_args();m=json.loads(a.manifest.read_text());stage=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='root' and m['component']=='progressmeter' and m['runtime']=='win-x64' and stage['selfContained'];assert 'root' in json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['headless']['owners']
nonce=uuid.uuid4().hex;out=a.manifest.parent/'windows-headless'/nonce;out.mkdir(parents=True);data=(r/'docs/replica/source/progressmeter-cases.json').read_bytes();served='root-progressmeter-'+nonce+'-headless-cases.json';(r/'artifacts/replica/windows-download'/served).write_bytes(data);sha=hashlib.sha256(data).hexdigest().upper()
code="$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';$out=Join-Path $app "+q('headless-'+nonce)+";$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory $out -Force|Out-Null;$cases=Join-Path $out 'cases.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $cases;if((Get-FileHash $cases -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Cases hash mismatch'};if((Get-FileHash ($app+'\\ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(m['binarySha256'].upper())+"){throw 'Binary mismatch'};$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';& ($app+'\\ReplicaProbe.exe') '--progressmeter' $cases $out|Out-Null;if($LASTEXITCODE-ne 0){throw 'Headless failed'};Remove-Item Env:DBMV_HEADLESS,Env:DBMV_HEADLESS_SCALE;Add-Type -AssemblyName System.IO.Compression.FileSystem;$zip=Join-Path $app "+q('headless-'+nonce+'.zip')+";[IO.Compression.ZipFile]::CreateFromDirectory($out,$zip);[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip));"
b=base64.b64decode(ps(code,timeout=60));
with zipfile.ZipFile(io.BytesIO(b)) as z:
 for n in z.namelist():
  assert Path(n).name==n
  (out/n).write_bytes(z.read(n))
v=json.loads((out/'progressmeter-native.json').read_text());assert len(v['rows'])==30 and len(json.loads((out/'progressmeter-lifecycle.json').read_text()))==7 and len(json.loads((out/'progressmeter-timing.json').read_text()))==18
(out/'environment.json').write_text(json.dumps({'phase':'WINDOWS_HEADLESS_RECORDED','headless':True,'rendering':'realSkia','dpr':v['dpr'],'manifest':str(a.manifest.resolve()),'binarySha256':m['binarySha256'],'casesSha256':sha,'stage':stage,'remaining':'fresh Windows source comparison, native AX/paint/lifecycle'},indent=2)+'\n');print(out)
