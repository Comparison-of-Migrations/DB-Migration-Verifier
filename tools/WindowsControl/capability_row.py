"""Fixed project CapabilityRow source; approved Parallels CLI/UIA with path/title guards."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
from capability_row_runtime import load
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','cases','auto','launch','read','invoke','close','input-open','input-invoke','input-record','input-close','motion-open','motion-record','motion-close']);p.add_argument('--target',choices=['language','mount','reduced','record','data','permissions'],default='record');p.add_argument('--stage',type=Path);p.add_argument('--label',choices=['language','hide','restore','reduced','detach','remount','final'],default='final');a=p.parse_args()
if a.action in ['cases','auto','launch','read','invoke','close'] and a.stage is None:p.error('Owned isolated --stage required')
stage,base=load(a.stage) if a.stage else (None,'')
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('CapabilityRow · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica CapabilityRow verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned CapabilityRow required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/capabilityrow.html');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/preflight-capabilityrow-windows-source.json';prior=out.stat().st_mtime_ns if out.exists() else 0
 print(ps(source+unique+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'capabilityrow-record'));if($null-ne $b){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($null-eq $b){throw 'Source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",retry_read=True))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists() and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh CapabilityRow source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==48;(r/'docs/replica/source/capabilityrow-windows-source.json').write_bytes(out.read_bytes());(r/'docs/replica/source/capabilityrow-windows-cases.json').write_text(json.dumps(v['cases'],ensure_ascii=False,indent=2)+'\n');print('Fresh original48')
elif a.action=='cases':
 file=r/'docs/replica/source/capabilityrow-windows-cases.json';served='preflight-capabilityrow-'+stage['guestFolder'].split('-')[-1]+'-cases.json';(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper()
 print(ps(base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'};$f=Join-Path $app 'capabilityrow-cases.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned cases hash mismatch'};[Console]::WriteLine('Own cases verified');"))
elif a.action=='auto':
 print(ps(base+"$f=Join-Path $app 'capabilityrow-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--capabilityrow' ($app+'\\capabilityrow-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned CapabilityRow failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native CapabilityRow'};",timeout=60));value=ps(base+"Get-Content -Raw ($app+'\\capabilityrow-native.json')",retry_read=True);v=json.loads(value);assert len(v['rows'])==48;out=r/'docs/replica/source/capabilityrow-windows-native';out.mkdir(exist_ok=True);(out/'capabilityrow-native.json').write_text(value+'\n');
 for filename in ['capabilityrow-timing.json']:
  payload=ps(base+"Get-Content -Raw ($app+'\\"+filename+"')",retry_read=True);json.loads(payload);(out/filename).write_text(payload+'\n')
 import base64
 for filename in ['capabilityrow-ko-478-2.png','capabilityrow-ko-656-2.png','capabilityrow-en-904-2.png']:
  png=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes(($app+'\\"+filename+"')))",retry_read=True);(out/filename).write_bytes(base64.b64decode(png))
 print('Fresh native48')
elif a.action=='source-close':print(ps(source+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))

elif a.action=='launch':
 print(ps(base+"Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--capabilityrow-window',($app+'\\capabilityrow-cases.json'),$app);"))
 print(ps("$deadline=[DateTime]::UtcNow.AddSeconds(8);do{"+find+"if($w.Count-eq 1){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);"+unique+"[Console]::WriteLine('Owned window ready');",retry_read=True))
elif a.action=='read':
 data=ps(find+unique+"Get-Content -Raw ($app+'\\capabilityrow-input.json')",retry_read=True);json.loads(data);out=r/'docs/replica/source/capabilityrow-windows-input';out.mkdir(exist_ok=True);(out/('read-'+a.label+'.json')).write_text(data+'\n');print(data)
elif a.action=='close':print(ps(find+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='invoke':print(ps(find+unique+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('CapabilityRow-'+a.target)+"));if($null-eq $c){throw 'Owned button missing'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))

if a.action.startswith('input-'):
 sourceInput="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('CapabilityRow Input · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
 if a.action=='input-open':print(ps(sourceInput+"if($w.Count){throw 'Owned input source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/capabilityrow-input.html');"))
 elif a.action=='input-close':print(ps(sourceInput+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 elif a.action=='input-invoke':
  evidence=r/'docs/replica/source/preflight-capabilityrow-windows-input.json';prior=evidence.stat().st_mtime_ns if evidence.exists() else 0
  print(ps(sourceInput+unique+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('CapabilityRow-'+a.target)+"));if($null-eq $c){throw 'Owned source input control missing'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
  deadline=time.monotonic()+8
  while time.monotonic()<deadline:
   if evidence.exists() and evidence.stat().st_mtime_ns>prior:break
   time.sleep(.05)
  else:raise RuntimeError('No fresh source input action evidence')
  dest=r/'docs/replica/source/capabilityrow-windows-input';dest.mkdir(exist_ok=True);(dest/('source-invoke-'+a.target+'-'+a.label+'.json')).write_bytes(evidence.read_bytes());print(json.loads(evidence.read_text()))
 elif a.action=='input-record':
  out=r/'docs/replica/source/preflight-capabilityrow-windows-input.json';prior=out.stat().st_mtime_ns if out.exists() else 0
  print(ps(sourceInput+unique+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'capabilityrow-input-record'));if($null-eq $c){throw 'Source input record missing'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
  deadline=time.monotonic()+8
  while time.monotonic()<deadline:
   if out.exists() and out.stat().st_mtime_ns>prior:break
   time.sleep(.05)
  else:raise RuntimeError('No fresh source input record')
  v=json.loads(out.read_text());dest=r/'docs/replica/source/capabilityrow-windows-input';dest.mkdir(exist_ok=True);(dest/'source-latest.json').write_bytes(out.read_bytes());print(v)

if a.action.startswith('motion-'):
 motion="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('CapabilityRow motion · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
 if a.action=='motion-open':print(ps(motion+"if($w.Count){throw 'Owned motion source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/capabilityrow-motion.html');"))
 elif a.action=='motion-close':print(ps(motion+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 else:
  out=r/'docs/replica/source/preflight-capabilityrow-windows-source.json';prior=out.stat().st_mtime_ns if out.exists() else 0
  print(ps(motion+unique+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'capabilityrow-motion-record'));if($null-ne $b){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($null-eq $b){throw 'Owned motion record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
  deadline=time.monotonic()+8
  while time.monotonic()<deadline:
   if out.exists() and out.stat().st_mtime_ns>prior:break
   time.sleep(.05)
  else:raise RuntimeError('No fresh motion source')
  value=json.loads(out.read_text());assert len(value['rows'])==48
  (r/'docs/replica/source/capabilityrow-windows-source-timing.json').write_bytes(out.read_bytes());print('Fresh original48 timing')
