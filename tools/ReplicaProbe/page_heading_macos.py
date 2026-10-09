"""Actual macOS heading evidence from the same frozen headless-tested binary."""
from pathlib import Path
import argparse, hashlib, json, os, platform, shutil, subprocess

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('manifest', type=Path)
p.add_argument('--home', action='store_true')
a = p.parse_args()
m = json.loads(a.manifest.read_text())
lease = json.loads((root / 'docs/replica/ENVIRONMENT_LEASE.json').read_text())
assert lease['resources']['macos']['owner'] == 'root', 'Root macOS resource required'
assert m['owner'] == 'root' and m['component'] == 'pageheading'
assert m['runtime'] == 'osx-arm64' and m['buildExitCode'] == 0
binary = Path(m['binary'])
assert hashlib.sha256(binary.read_bytes()).hexdigest() == m['binarySha256']
kind = 'home' if a.home else 'common'
output = a.manifest.parent / ('actual-macos-' + kind)
output.mkdir(exist_ok=True)
cases = root / 'docs/replica/source' / ('pageheadinghome-cases.json' if a.home else 'pageheading-cases.json')
owned_cases = output / 'cases.json'
shutil.copy2(cases, owned_cases)
env = os.environ.copy()
env.pop('DBMV_HEADLESS', None)
env.pop('DBMV_HEADLESS_SCALE', None)
env['TMPDIR'] = str(output / 'tmp') + '/'
Path(env['TMPDIR']).mkdir(exist_ok=True)
command = [str(Path('/Users/youptebook/.cache/db-migration-verifier/dotnet/dotnet')),
           str(binary), '--pageheading', str(owned_cases), str(output)]
record = output / 'pageheading-native.json'
prior = record.stat().st_mtime_ns if record.exists() else 0
result = subprocess.run(command, env=env, stdout=subprocess.PIPE,
                        stderr=subprocess.STDOUT, text=True, timeout=60)
(output / 'run.log').write_text(result.stdout)
assert result.returncode == 0, result.stdout
assert record.stat().st_mtime_ns > prior, 'Stale actual evidence'
value = json.loads(record.read_text())
assert len(value['rows']) == (12 if a.home else 84) and value['dpr'] == 2
(output / 'environment.json').write_text(json.dumps({
    'buildManifest': str(a.manifest.resolve()), 'binarySha256': m['binarySha256'],
    'headless': False, 'hostOS': platform.platform(), 'scale': value['dpr'],
    'command': command, 'casesSha256': hashlib.sha256(owned_cases.read_bytes()).hexdigest(),
    'processExitCode': result.returncode, 'note': 'Recorded only; compare source and actual input separately.'
}, indent=2) + '\n')
print(output)
print(result.stdout)
