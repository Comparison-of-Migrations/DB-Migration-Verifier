"""Hash-verified complete owned Windows runtime, never a shared two-DLL update."""
from pathlib import Path
import argparse, hashlib, json, zipfile
from driver import ps, q

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('action', choices=['pack', 'stage'])
p.add_argument('manifest', type=Path)
p.add_argument('--headless-only', action='store_true', help='Deploy an isolated runtime without acquiring native input/focus access')
a = p.parse_args()
m = json.loads(a.manifest.read_text())
if m['buildExitCode'] != 0 or m['runtime'] != 'win-x64':
    raise RuntimeError('Successful owned win-x64 build required')
binary = Path(m['binary'])
if hashlib.sha256(binary.read_bytes()).hexdigest() != m['binarySha256']:
    raise RuntimeError('Owned binary changed')
name = f"{m['owner']}-{m['component']}-{a.manifest.parent.name}-runtime.zip"
archive = root / 'artifacts/replica/windows-download' / name
record = a.manifest.parent / 'windows-stage.json'
if a.action == 'pack':
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
        for f in binary.parent.rglob('*'):
            if f.is_file():
                z.write(f, f.relative_to(binary.parent))
    value = {'owner': m['owner'], 'component': m['component'], 'buildManifest': str(a.manifest.resolve()),
             'archive': str(archive), 'archiveSha256': hashlib.sha256(archive.read_bytes()).hexdigest(),
             'servedURL': 'http://10.211.55.2:8768/' + name,
             'guestFolder': 'DBMV-Isolated-' + name.removesuffix('-runtime.zip'),
             'binarySha256': m['binarySha256'], 'selfContained': (binary.parent / 'coreclr.dll').exists()}
    record.write_text(json.dumps(value, indent=2) + '\n')
    print(record)
else:
    v = json.loads(record.read_text())
    resources = json.loads((root / 'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']
    if a.headless_only and m['owner'] not in resources['headless']['owners']:
        raise RuntimeError('Independent headless owner required')
    if not a.headless_only and resources['windows']['owner'] != m['owner']:
        raise RuntimeError('Windows resource must be granted to this owner before deployment')
    if not v['selfContained']:
        raise RuntimeError('Use isolated build --self-contained to obtain a cold executable runtime')
    code = "$base=Join-Path $env:TEMP " + q(v['guestFolder']) + ";$app=Join-Path $base 'app';"
    code += "if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime is active'};"
    code += "New-Item -ItemType Directory -Path $base -Force|Out-Null;$zip=Join-Path $base 'runtime.zip';"
    code += "Invoke-WebRequest -UseBasicParsing " + q(v['servedURL']) + " -OutFile $zip;"
    code += "if((Get-FileHash $zip -Algorithm SHA256).Hash-ne " + q(v['archiveSha256'].upper()) + "){throw 'Owned archive hash mismatch'};"
    code += "Expand-Archive -Path $zip -DestinationPath $app -Force;"
    code += "if((Get-FileHash ($app+'\\ReplicaProbe.dll') -Algorithm SHA256).Hash-ne " + q(v['binarySha256'].upper()) + "){throw 'Owned binary hash mismatch'};"
    code += "if(!(Test-Path ($app+'\\coreclr.dll'))-or !(Test-Path ($app+'\\Avalonia.Headless.dll'))){throw 'Complete runtime missing'};[Console]::WriteLine($app);"
    print(ps(code, timeout=60))
