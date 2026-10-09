"""New Windows callback50 oracle comparison in an independent rendered headless process."""
from pathlib import Path
import argparse, base64, hashlib, json, uuid
from driver import ps, q

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser(); p.add_argument('manifest', type=Path); a = p.parse_args()
m = json.loads(a.manifest.read_text()); stage = json.loads((a.manifest.parent / 'windows-stage.json').read_text())
assert m['owner'] == stage['owner'] == 'preflight' and m['component'] == stage['component'] == 'pageintegration'
assert m['buildExitCode'] == 0 and m['runtime'] == 'win-x64' and stage['selfContained']
nonce = uuid.uuid4().hex; out = a.manifest.parent / 'headless-selectioncallbacks-windows' / nonce; out.mkdir(parents=True)
oracle = root / 'artifacts/replica/workers/preflight/selectioncallbacks/source/bcb156a1156d4681b0f59ee9741f5f4c/windows/original-windows.json'
body = oracle.read_bytes(); name = 'preflight-selectioncallbacks-' + nonce + '-oracle.json'
(root / 'artifacts/replica/windows-download' / name).write_bytes(body)
code = "$app=Join-Path (Join-Path $env:TEMP " + q(stage['guestFolder']) + ") 'app';$out=Join-Path $app " + q('headless-callbacks-' + nonce) + ";New-Item -ItemType Directory $out|Out-Null;"
code += "if((Get-FileHash(Join-Path $app 'ReplicaProbe.dll') -Algorithm SHA256).Hash-ne " + q(m['binarySha256'].upper()) + "){throw 'Exact cold binary guard'};"
code += "Invoke-WebRequest -UseBasicParsing " + q('http://10.211.55.2:8768/' + name) + " -OutFile (Join-Path $out 'oracle.json');if((Get-FileHash(Join-Path $out 'oracle.json') -Algorithm SHA256).Hash-ne " + q(hashlib.sha256(body).hexdigest().upper()) + "){throw 'Original oracle hash guard'};[IO.File]::WriteAllText((Join-Path $out 'cases.json'),'[]');"
code += "$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';try{& (Join-Path $app 'ReplicaProbe.exe') '--pageintegration' (Join-Path $out 'cases.json') $out (Join-Path $out 'oracle.json') '--selection-callbacks-only';if($LASTEXITCODE-ne0){throw 'New callback headless failed'};[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $out 'pageintegration-selectioncallbacks.json')))}finally{Remove-Item Env:DBMV_HEADLESS,Env:DBMV_HEADLESS_SCALE -ErrorAction SilentlyContinue};exit 0;"
try:
    result = ps(code, timeout=50); (out / 'transport.txt').write_text(result)
    value = base64.b64decode(result.strip().splitlines()[-1]); json.loads(value)
    (out / 'pageintegration-selectioncallbacks.json').write_bytes(value)
finally:
    cleanup = json.loads(ps("$app=Join-Path (Join-Path $env:TEMP " + q(stage['guestFolder']) + ") 'app';@{processCount=@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq(Join-Path $app 'ReplicaProbe.exe')}).Count}|ConvertTo-Json", retry_read=True))
    (out / 'cleanup.json').write_text(json.dumps(cleanup)); assert cleanup['processCount'] == 0
(out / 'environment.json').write_text(json.dumps({'owner':'preflight','component':'pageintegration','headless':True,'nonce':nonce,'scale':2,'manifest':str(a.manifest),'scope':'New original Windows callback50 only; physical input still separate','process0':True}, indent=2))
print(out)
