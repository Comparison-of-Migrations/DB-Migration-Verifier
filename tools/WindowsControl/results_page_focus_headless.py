"""Independent owned cold Windows focus input/render scope; no actual input/window."""
from pathlib import Path
import argparse,base64,hashlib,json,sys,uuid
from driver import ps,q
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('stage',type=Path);a=p.parse_args();s=json.loads(a.stage.read_text());assert s['owner']=='connection'and s['component']=='resultspage'and s['selfContained']
assert 'connection'in json.loads((root/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['headless']['owners']
identifier=uuid.uuid4().hex;folder=a.stage.parent/'outputs'/identifier;folder.mkdir(parents=True)
cases=root/'artifacts/replica/windows-download/resultspage-source.json';digest=hashlib.sha256(cases.read_bytes()).hexdigest()
base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';$out=Join-Path $app "+q('focus-headless-'+identifier)+";"
code=base+"if((Get-FileHash($app+'\\ReplicaProbe.dll')-Algorithm SHA256).Hash-ne "+q(s['binarySha256'].upper())+"){throw 'Exact cold hash required'};New-Item -ItemType Directory $out|Out-Null;$f=Join-Path $out 'cases.json';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/resultspage-source.json' -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Case hash required'};$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';&($app+'\\ReplicaProbe.exe') '--resultspage' $f $out '--focus-only';if($LASTEXITCODE-ne0){throw 'Owned headless failed'};[Console]::WriteLine((Get-Content -Raw($out+'\\resultspage-focus-headless.json')));"
data=ps(code,timeout=40);j=json.loads(data);(folder/'resultspage-focus-headless.json').write_text(json.dumps(j,indent=2)+'\n')
for name in ['pointer','space','same-focused-held','same-focused-pointer']:
 filename='resultspage-focus-'+name+'.png';b=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $out "+q(filename)+")))",retry_read=True);(folder/filename).write_bytes(base64.b64decode(b))
(folder/'environment.json').write_text(json.dumps({'phase':'HEADLESS_RECORDED','stage':str(a.stage.resolve()),'runtime':'win-x64','render':'Skia','headlessDrawing':False,'scale':2,'caseSha256':digest,'binarySha256':s['binarySha256'],'output':str(folder.resolve()),'scope':'only pointer/Space/same-focused held/release; original comparison pending'},indent=2)+'\n')
print(folder.resolve())
