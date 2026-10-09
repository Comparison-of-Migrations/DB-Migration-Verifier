"""Owned Edge headless original DOM/media; real Windows fonts/gutters/wall timers."""
from pathlib import Path
import argparse,json,time,uuid
from driver import ps,q
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('motion',choices=['normal','reduced']);a=p.parse_args()
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home'
nonce=uuid.uuid4().hex;out=ROOT/'artifacts/replica/workers/home/pageintegrationtoast-windows-source'/nonce;out.mkdir(parents=True)
url='http://10.211.55.2:8768/home-pageintegrationtoast.html?os=windows&motion='+a.motion+'&recording='+nonce
profile='DBMV-Toast-source-'+nonce
code="$ProgressPreference='SilentlyContinue';$profile=Join-Path $env:TEMP "+q(profile)+";$edge='C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';if(!(Test-Path $edge)){throw 'Exact Edge executable absent'};New-Item -ItemType Directory $profile|Out-Null;$args=@('--headless=new','--no-first-run','--no-default-browser-check',('--user-data-dir='+$profile),'--force-device-scale-factor=2','--window-size=1400,1000','--disable-background-timer-throttling'"
if a.motion=='reduced':code+=",'--force-prefers-reduced-motion=reduce'"
code+=","+q(url)+");$p=Start-Process -FilePath $edge -ArgumentList $args -PassThru;[pscustomobject]@{pid=$p.Id;profile=$profile;path=$edge;nonce="+q(nonce)+";url="+q(url)+"}|ConvertTo-Json -Compress;"
launch=None;source=None
try:
 text=ps(code,timeout=15);(out/'launch.log').write_text(text)
 for line in text.splitlines():
  try:v=json.loads(line)
  except json.JSONDecodeError:continue
  if v.get('nonce')==nonce:launch=v
 if launch is None:raise RuntimeError('Persisted own launch identity not returned; no relaunch')
 (out/'launch.json').write_text(json.dumps(launch,indent=2)+'\n')
 end=time.monotonic()+45;record=ROOT/'docs/replica/source/home-pageintegrationtoast-windows-source.json'
 while time.monotonic()<end:
  try:
   v=json.loads(record.read_text())
   if v.get('recording')==nonce:
    assert v['motion']==a.motion and len(v['rows'])==8 and len(v['lifetimes'])==2
    source=record.read_bytes();(out/'source.json').write_bytes(source);break
  except (FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.1)
 if source is None:raise RuntimeError('Owned source nonce not recorded; no retry/input')
except Exception as e:
 (out/'failure.json').write_text(json.dumps({'phase':'UNVERIFIED','exception':str(e),'nonce':nonce,'replayedInput':False},indent=2)+'\n');raise
finally:
 if launch is not None:
  cleanup="$profile="+q(launch['profile'])+";$edge="+q(launch['path'])+";$owned=@(Get-CimInstance Win32_Process -Filter \"Name='msedge.exe'\"|Where-Object{$_.CommandLine-and$_.CommandLine.Contains($profile)});foreach($p in $owned){if($p.ExecutablePath-ne$edge){throw 'Exact profile executable guard'};Stop-Process -Id $p.ProcessId -ErrorAction Stop};Start-Sleep -Milliseconds 300;$left=@(Get-CimInstance Win32_Process -Filter \"Name='msedge.exe'\"|Where-Object{$_.CommandLine-and$_.CommandLine.Contains($profile)});[pscustomobject]@{ownedProfileProcesses=$left.Count;sourceWindows=0;headless=$true;nonce="+q(nonce)+"}|ConvertTo-Json -Compress;if($left.Count){throw 'Own headless source remains'};"
  result=ps(cleanup,timeout=15);(out/'cleanup.log').write_text(result)
  for line in result.splitlines():
   try:v=json.loads(line)
   except json.JSONDecodeError:continue
   if v.get('nonce')==nonce:(out/'cleanup.json').write_text(json.dumps(v,indent=2)+'\n')
print(out,flush=True)
