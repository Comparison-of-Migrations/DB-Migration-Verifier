"""Finite native parent input/peer slice; save then close exact owned window immediately."""
from pathlib import Path
import argparse, base64, json, subprocess, sys
from driver import ps, q

r = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--stage', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
p.add_argument('--remaining-only', action='store_true')
a = p.parse_args()
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
v = json.loads(a.stage.read_text())
assert v['owner']=='preflight' and v['component']=='preflightpage'
a.output.mkdir(parents=True, exist_ok=True)

def run(script, *args):
    result = subprocess.run([sys.executable, str(r/'tools/WindowsControl'/script), *args], cwd=r, text=True, capture_output=True)
    label = script.removesuffix('.py') + '-' + str(len(list(a.output.glob('process-*.json'))))
    (a.output/('process-'+label+'.json')).write_text(json.dumps({'args':args,'exitCode':result.returncode,'stdout':result.stdout,'stderr':result.stderr},ensure_ascii=False,indent=2))
    print(label+': '+str(result.returncode), flush=True)
    if result.returncode: raise RuntimeError(result.stderr or result.stdout)

common = ['--stage',str(a.stage),'--output',str(a.output)]
try:
    run('preflight_page.py','launch',*common)
    run('preflight_page_native_batch.py',*common,*(['--remaining-only'] if a.remaining_only else []))
    run('preflight_page_semantics.py',*common,'--label','en')
    run('preflight_page.py','invoke',*common,'--target','language')
    run('preflight_page.py','read',*common,'--label','final-ko')
    run('preflight_page_semantics.py',*common,'--label','ko')
finally:
    # Close even after a recorded diagnostic failure; absence is separately proven by cleanup.
    try: run('preflight_page.py','close',*common)
    finally: run('preflight_page.py','cleanup',*common)
base="$app=Join-Path (Join-Path $env:TEMP "+q(v['guestFolder'])+") 'app';"
encoded=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($app+'\\preflightpage-motion.json'))",retry_read=True)
motion=base64.b64decode(encoded.strip()).decode('utf-8')
(a.output/'native-motion.json').write_text(motion)
json.loads(motion)
print('Finite native parent slice saved and exact native window/process0',flush=True)
