"""Only new small modal application-parent checks in an isolated Windows process."""
from pathlib import Path
import argparse
import base64
import hashlib
import io
import json
import uuid
import zipfile
from driver import ps, q

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('manifest', type=Path)
parser.add_argument('--native-auto', action='store_true')
args = parser.parse_args()
manifest = json.loads(args.manifest.read_text())
stage = json.loads((args.manifest.parent / 'windows-stage.json').read_text())
resources = json.loads((root / 'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']
assert manifest['owner'] == 'root' and manifest['component'] == 'pageintegration'
assert manifest['runtime'] == 'win-x64' and manifest['buildExitCode'] == 0 and stage['selfContained']
assert hashlib.sha256(Path(manifest['binary']).read_bytes()).hexdigest() == manifest['binarySha256']
if args.native_auto:
    assert resources['windows']['owner'] == 'root'
else:
    assert 'root' in resources['headless']['owners']

nonce = uuid.uuid4().hex
output = args.manifest.parent / ('modal-bounds-native-auto' if args.native_auto else 'modal-bounds-headless-windows') / nonce
output.mkdir(parents=True)
files = {
    'cases.json': root / 'docs/replica/source/root-modal-bounds-cases.json',
    'oracle.json': root / 'docs/replica/source/root-modal-bounds-windows-source.json',
    # Callback semantics are platform independent; geometry always uses fresh Windows oracle.
    'root-appflow-mac-source.json': root / 'docs/replica/source/root-appflow-mac-source.json',
}
oracle = json.loads(files['oracle.json'].read_text())
assert len(oracle['rows']) == 4 and all(row['dpr'] == 2 for row in oracle['rows'])
command = "$app=Join-Path (Join-Path $env:TEMP " + q(stage['guestFolder']) + ") 'app';"
command += "$out=Join-Path $app " + q('root-modal-bounds-'+nonce) + ";"
command += "if(Test-Path $out){throw 'Scope already exists; no replay'};New-Item -ItemType Directory $out|Out-Null;"
command += "if((Get-FileHash ($app+'\\ReplicaProbe.dll')).Hash-ne " + q(manifest['binarySha256'].upper()) + "){throw 'Frozen binary mismatch'};"
input_hashes = {}
for name, path in files.items():
    payload = path.read_bytes()
    served = f'root-modal-bounds-{nonce}-{name}'
    (root / 'artifacts/replica/windows-download' / served).write_bytes(payload)
    sha = hashlib.sha256(payload).hexdigest()
    input_hashes[name] = sha
    command += "Invoke-WebRequest -UseBasicParsing " + q('http://10.211.55.2:8768/'+served) + " -OutFile (Join-Path $out " + q(name) + ");"
    command += "if((Get-FileHash (Join-Path $out " + q(name) + ")).Hash-ne " + q(sha.upper()) + "){throw 'Owned input hash mismatch'};"
command += "$env:DBMV_HEADLESS=" + q('0' if args.native_auto else '1') + ";$env:DBMV_HEADLESS_SCALE='2';"
command += "& ($app+'\\ReplicaProbe.exe') '--pageintegration' (Join-Path $out 'cases.json') $out (Join-Path $out 'oracle.json') '--modal-bounds-only';$exit=$LASTEXITCODE;"
command += "$remaining=@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq($app+'\\ReplicaProbe.exe')}).Count;"
command += "@{processCount=$remaining;exitCode=$exit;scope='New connected parent only'}|ConvertTo-Json|Set-Content -Encoding UTF8 (Join-Path $out 'cleanup.json');"
command += "Add-Type -AssemblyName System.IO.Compression.FileSystem;$zip=$out+'.zip';[IO.Compression.ZipFile]::CreateFromDirectory($out,$zip);[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip));"
(output / 'scope.json').write_text(json.dumps({'manifest': str(args.manifest.resolve()), 'binarySha256': manifest['binarySha256'], 'inputs': input_hashes, 'nativeAuto': args.native_auto, 'nonce': nonce}, indent=2) + '\n')
raw = ps(command, timeout=55)
with zipfile.ZipFile(io.BytesIO(base64.b64decode(raw))) as archive:
    for name in archive.namelist():
        target = (output / name).resolve()
        assert target.is_relative_to(output.resolve())
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(archive.read(name))
cleanup = json.loads((output / 'cleanup.json').read_text(encoding='utf-8-sig'))
assert cleanup['processCount'] == 0 and cleanup['exitCode'] == 0, output
checks = json.loads((output / 'pageintegration-modal-bounds.json').read_text())['checks']
assert len(checks) == 128 and all(check['passed'] for check in checks), output
(output / 'environment.json').write_text(json.dumps({
    'phase': 'ACTUAL_NATIVE_PARENT_PASSED' if args.native_auto else 'HEADLESS_PASSED',
    'headless': not args.native_auto, 'rendering': 'realSkia', 'dpr': 2,
    'checks': 128, 'actualPhysicalInputStillRequired': True,
    'source': str(files['oracle.json']), 'cleanup': cleanup,
}, indent=2) + '\n')
print(output)
