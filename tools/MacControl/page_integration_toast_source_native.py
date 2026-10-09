"""Owned headless Chrome original DOM/media oracle; no host pointer/OS setting."""
from pathlib import Path
import argparse,json,subprocess,time,uuid,hashlib
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('lang',choices=['ko','en']);a=p.parse_args()
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='home'
nonce=uuid.uuid4().hex;out=ROOT/'artifacts/replica/workers/home/pageintegrationtoast-source'/nonce;out.mkdir(parents=True);profile=out/'chrome-profile'
chrome=Path('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome');assert chrome.is_file()
url='http://10.211.55.2:8768/home-pageintegrationtoast-ax.html?lang='+a.lang+'&recording='+nonce
command=[str(chrome),'--force-renderer-accessibility','--no-first-run','--no-default-browser-check','--user-data-dir='+str(profile),'--force-device-scale-factor=2','--window-size=1400,1000',url]
stream=(out/'chrome.log').open('w');proc=subprocess.Popen(command,stdout=stream,stderr=stream)
(out/'launch.json').write_text(json.dumps({'pid':proc.pid,'path':str(chrome),'profile':str(profile),'url':url,'command':command,'mode':'owned headless browser original DOM, real wall timers, no user input'},indent=2)+'\n')
result=None
try:
 end=time.monotonic()+10
 while time.monotonic()<end:
  r=subprocess.run([str(ROOT/'artifacts/replica/workers/home/toast-source-ax'),url,str(out/'actual-source-ax.json'),str(proc.pid)],capture_output=True,text=True)
  if r.returncode==0:
   result=(out/'actual-source-ax.json').read_bytes();break
  (out/'last-guard.txt').write_text(r.stdout+r.stderr)
  time.sleep(.1)
 if result is None:raise RuntimeError('Owned sourceAX not observed, no input/launch replay')
except Exception as e:
 (out/'failure.json').write_text(json.dumps({'phase':'UNVERIFIED','exception':str(e),'nonce':nonce},indent=2)+'\n');raise
finally:
 # Scope PID is directly returned by this launch, and its exact command/profile
 # is rechecked before termination; never address user Chrome/profile processes.
 if proc.poll() is None:
  exact=subprocess.run(['ps','-p',str(proc.pid),'-o','command='],text=True,capture_output=True,check=True).stdout
  assert str(chrome) in exact and str(profile) in exact,'Exact owned source path/profile lost'
  proc.terminate();proc.wait(timeout=8)
 stream.close()
 listing=subprocess.run(['ps','-axo','pid=,command='],text=True,capture_output=True,check=True).stdout
 own=[l.strip()for l in listing.splitlines()if str(profile) in l and 'Google Chrome' in l]
 cleanup={'nonce':nonce,'pid':proc.pid,'sourceBrowserProcess':0 if proc.poll() is not None else 1,'ownedProfileProcesses':len(own),'sourceWindows':0,'headless':False,'method':'Exact persisted profile PID/command termination; no other Chrome profile' ,'sourceSha256':hashlib.sha256(result).hexdigest()if result else None,'processExitCode':proc.returncode}
 (out/'cleanup.json').write_text(json.dumps(cleanup,indent=2)+'\n');assert cleanup['sourceBrowserProcess']==0 and not own,'Own source not fully closed'
print(out,flush=True)
