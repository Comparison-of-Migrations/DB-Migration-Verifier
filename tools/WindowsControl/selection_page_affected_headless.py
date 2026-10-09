"""Fresh independent Windows Selection projected providers/cache/empty drawing only."""
from pathlib import Path
import argparse, base64, hashlib, io, json, uuid, zipfile
from driver import ps, q

r = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser(); p.add_argument('manifest', type=Path); p.add_argument('--scopes', nargs='+', choices=['projected','peer','empty','reason'], default=['projected','peer','empty']); a = p.parse_args()
m = json.loads(a.manifest.read_text()); v = json.loads((a.manifest.parent/'windows-stage.json').read_text())
assert m['owner'] == v['owner'] == 'preflight' and m['component'] == v['component'] == 'selectionpage' and m['buildExitCode'] == 0
nonce = uuid.uuid4().hex; o = a.manifest.parent/'affected-windows-headless'/nonce; o.mkdir(parents=True)
body = (r/'docs/replica/source/selectionpage-windows-cases.json').read_bytes(); name = 'preflight-selection-affected-'+nonce+'.json'
(r/'artifacts/replica/windows-download'/name).write_bytes(body)
code = "$app=Join-Path (Join-Path $env:TEMP " + q(v['guestFolder']) + ") 'app';$out=Join-Path $app " + q('affected-'+nonce) + ";New-Item -ItemType Directory $out|Out-Null;Invoke-WebRequest -UseBasicParsing " + q('http://10.211.55.2:8768/'+name) + " -OutFile ($out+'\\cases.json');if((Get-FileHash ($out+'\\cases.json') -Algorithm SHA256).Hash-ne " + q(hashlib.sha256(body).hexdigest().upper()) + "){throw 'Cases mismatch'};$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';try{"
for scope in a.scopes:
    flag = '--'+scope+'-only'
    code += "& ($app+'\\ReplicaProbe.exe') '--selectionpage' ($out+'\\cases.json') $out " + q(flag) + ";if($LASTEXITCODE-ne0){throw " + q('Affected scope failed '+flag) + "};"
code += "Compress-Archive -Path ($out+'\\selectionpage-*') -DestinationPath ($out+'\\result.zip');[Convert]::ToBase64String([IO.File]::ReadAllBytes($out+'\\result.zip'))}finally{Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;Remove-Item Env:DBMV_HEADLESS_SCALE -ErrorAction SilentlyContinue};exit 0;"
try:
    result = ps(code, timeout=50); (o/'transport.txt').write_text(result)
    blob = base64.b64decode(result.strip().splitlines()[-1]); (o/'result.zip').write_bytes(blob)
    with zipfile.ZipFile(io.BytesIO(blob)) as z:
        for item in z.infolist():
            assert '/' not in item.filename and '\\' not in item.filename
            (o/item.filename).write_bytes(z.read(item))
    if 'projected' in a.scopes:
        d = json.loads((o/'selectionpage-projected.json').read_text()); assert len(d)==2
        assert all(x['parent'] and x['originalOwner'] and x['checkType']=='CheckBox' and x['buttonType']=='Button' and x['buttonChildren']==0 and x['notificationCount']>0 for x in d)
    if 'reason' in a.scopes:
        d=json.loads((o/'selectionpage-reasons.json').read_text()); assert len(d)==16
        expected=['bulk','bulk','clear-tables','category:data','expand:data','query','table:demo.orders','query']; assert [x['reason'] for x in d]==expected+expected
finally:
    cleanup=json.loads(ps("$app=Join-Path (Join-Path $env:TEMP "+q(v['guestFolder'])+") 'app';@{processCount=@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq($app+'\\ReplicaProbe.exe')}).Count}|ConvertTo-Json",retry_read=True)); (o/'cleanup.json').write_text(json.dumps(cleanup)); assert cleanup['processCount']==0
(o/'environment.json').write_text(json.dumps({'manifest':str(a.manifest),'nonce':nonce,'scale':2,'scope':a.scopes,'process0':True})); print(o)
