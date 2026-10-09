"""Changed optional hosts only: native KO/EN hierarchy and passive hit, always immediate close."""
from pathlib import Path
import json, subprocess, sys, time, shutil, uuid
r=Path(__file__).resolve().parents[2]; manifest=Path(sys.argv[1]).resolve();m=json.loads(manifest.read_text())
assert m['owner']=='preflight' and m['component']=='preflightpage' and m['runtime']=='osx-arm64'
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight'
b=manifest.parent;o=b/'actual-macos/input';attempt=o/('optional-'+uuid.uuid4().hex);attempt.mkdir();print(attempt,flush=True);actor=r/'artifacts/replica/workers/preflight/preflightpage/preflight_page_control'
def action(mode,label):
 p=subprocess.run([str(actor),str(manifest),mode],cwd=r,text=True,capture_output=True)
 (attempt/(label+'-process.json')).write_text(json.dumps({'mode':mode,'exitCode':p.returncode,'stdout':p.stdout,'stderr':p.stderr}))
 if p.returncode:raise RuntimeError(p.stderr)
 f=o/('native-'+('ax' if mode=='inspect' else mode)+'.json')
 if f.exists():shutil.copyfile(f,attempt/(label+'.json'))
 if mode not in ['inspect','close','cleanup']:
  value=json.loads(p.stdout);(attempt/(label+'.json')).write_text(json.dumps(value,ensure_ascii=False));return value
started=time.time();subprocess.run(['/usr/bin/open','-n',str(b/'PreflightPage-input.app')],check=True)
try:
 deadline=time.monotonic()+8
 while not (o/'preflightpage-input.json').exists() or (o/'preflightpage-input.json').stat().st_mtime<started:
  if time.monotonic()>deadline:raise RuntimeError('Fresh initial state absent; no duplicate launch')
  time.sleep(.1)
 for lang in ['ko','en']:
  action('inspect',lang+'-ax')
  before=action('focus-title',lang+'-before');assert before['headingFocused']
  after=action('passive',lang+'-after');assert not after['headingFocused'] and after['requests']==[]
  if lang=='ko':after=action('language','en-language');assert after['english']
finally:
 try:action('close','close')
 finally:
  time.sleep(.4);action('cleanup','cleanup')
print('Optional KO/EN hierarchy/passive2 saved; exact native/source0')
