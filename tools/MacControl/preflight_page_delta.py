"""Only corrected passive1 and source utility-focus/inner-AX affected scopes."""
from pathlib import Path
import json,subprocess,sys,time,shutil
r=Path(__file__).resolve().parents[2];manifest=Path(sys.argv[1]).resolve();m=json.loads(manifest.read_text());assert m['owner']=='preflight'and m['component']=='preflightpage'and m['runtime']=='osx-arm64';assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight';base=manifest.parent;out=base/'actual-macos/input';actor=r/'artifacts/replica/workers/preflight/preflightpage/preflight_page_control';endpoint=r/'docs/replica/source/preflight-preflightpage-mac-input.json'
def action(mode,label,source=False):
 command=[str(actor),str(manifest),mode]+(['--source']if source else[]);v=subprocess.run(command,cwd=r,text=True,capture_output=True);prefix='source-'if source else'native-';(out/(prefix+label+'-process.json')).write_text(json.dumps({'command':command,'exitCode':v.returncode,'stdout':v.stdout,'stderr':v.stderr}));print(prefix+label,v.returncode,flush=True)
 if v.returncode:raise RuntimeError(v.stderr)
 f=out/(prefix+('ax'if mode=='inspect'else mode)+'.json')
 if f.exists():shutil.copyfile(f,out/(prefix+'delta-'+label+'.json'))
 if mode not in ['inspect','close','cleanup']:
  x=json.loads((out/(prefix+'delta-'+label+'.json')).read_text());return x
def fresh(file,started):
 end=time.monotonic()+6
 while time.monotonic()<end:
  if file.exists()and file.stat().st_mtime>started:
   try:return json.loads(file.read_text())
   except json.JSONDecodeError:pass
  time.sleep(.05)
 raise RuntimeError('Fresh startup absent; no repeated launch/input')
if '--source-only' not in sys.argv:
 started=time.time();subprocess.run(['/usr/bin/open','-n',str(base/'PreflightPage-input.app')],check=True);initial=fresh(out/'preflightpage-input.json',started);assert initial['attached']and initial['requests']==[];action('inspect','ko-ax');before=action('focus-title','passive-before');assert before['headingFocused'];after=action('passive','passive-after');assert not after['headingFocused']and after['requests']==[];action('close','close');time.sleep(.3);action('cleanup','cleanup')
if '--native-only' not in sys.argv:
 started=time.time();subprocess.run(['/Applications/Google Chrome.app/Contents/MacOS/Google Chrome','--new-window','http://10.211.55.2:8768/preflightpage-input.html?os=mac'],check=True);initial=fresh(endpoint,started);assert initial['dpr']==2 and initial['requests']==[];before=action('focus-permission','permission-before',True);assert before['permissionFocused'];after=action('language','permission-language',True);assert after['permissionFocused']and after['english'];action('inspect','en-ax',True);before=action('focus-title','title-before',True);assert before['headingFocused'];after=action('language','title-language',True);assert after['headingFocused']and not after['english'];action('inspect','ko-ax',True);action('close','close',True);time.sleep(.3);action('cleanup','cleanup',True)
print('Only affected parent scopes passed and scoped windows closed0')
