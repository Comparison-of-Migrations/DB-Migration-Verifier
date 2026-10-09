"""Owned headless Chrome original DOM/media oracle; no host pointer/OS setting."""
from pathlib import Path
import argparse,json,subprocess,time,uuid,hashlib
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('scale',type=int,choices=[1,2]);a=p.parse_args()
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='home'
nonce=uuid.uuid4().hex;out=ROOT/'artifacts/replica/workers/home/pageintegrationtoast-source'/nonce;out.mkdir(parents=True);profile=out/'chrome-profile'
chrome=Path('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome');assert chrome.is_file()
url='http://10.211.55.2:8768/home-pageintegrationtoast-metrics.html?recording='+nonce
command=[str(chrome),'--headless=new','--no-first-run','--no-default-browser-check','--user-data-dir='+str(profile),'--force-device-scale-factor='+str(a.scale),'--window-size=1400,1000','--disable-background-timer-throttling',url]
stream=(out/'chrome.log').open('w');proc=subprocess.Popen(command,stdout=stream,stderr=stream)
(out/'launch.json').write_text(json.dumps({'pid':proc.pid,'path':str(chrome),'profile':str(profile),'url':url,'command':command,'mode':'owned headless browser original DOM, real wall timers, no user input'},indent=2)+'\n')
source=ROOT/'docs/replica/source/home-pageintegrationtoast-mac-source.json';result=None
try:
 end=time.monotonic()+45
 while time.monotonic()<end:
  if proc.poll() is not None:raise RuntimeError('Owned source browser exited before recording: '+str(proc.returncode))
  try:
   d=json.loads(source.read_text())
   if d.get('recording')==nonce:
    assert len(d['rows'])==3 and all(r['dpr']==a.scale for r in d['rows'])
    result=source.read_bytes();(out/'source.json').write_bytes(result);break
  except (FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.1)
 if result is None:raise RuntimeError('Owned source did not record; no automatic retry/input')
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
 cleanup={'nonce':nonce,'pid':proc.pid,'sourceBrowserProcess':0 if proc.poll() is not None else 1,'ownedProfileProcesses':len(own),'sourceWindows':0,'headless':True,'sourceSha256':hashlib.sha256(result).hexdigest()if result else None,'processExitCode':proc.returncode}
 (out/'cleanup.json').write_text(json.dumps(cleanup,indent=2)+'\n');assert cleanup['sourceBrowserProcess']==0 and not own,'Own source not fully closed'
print(out,flush=True)
