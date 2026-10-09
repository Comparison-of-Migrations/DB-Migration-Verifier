"""Approved official CLI/UIA, fixed project ResultSummaryRow measurement window only."""
from pathlib import Path
import argparse,json,time
from driver import ps
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','prepare','auto','source-close','launch','close','motion-open','motion-record','timing','update']);a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-ResultRow-20261007') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('ResultSummaryRow · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Unique owned Count original missing'};"
if a.action=='source-open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/resultrow.html');[Console]::WriteLine('Owned ResultSummaryRow source requested');"))
elif a.action=='motion-open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/resultrow-motion.html');[Console]::WriteLine('Owned motion requested');"))
elif a.action=='motion-record':
 print(ps(source.replace('ResultSummaryRow · original isolation','ResultSummaryRow motion · original isolation')+"$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'result-row-motion-record'));if($null-eq $b){throw 'Owned motion record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 time.sleep(.2);(r/'docs/replica/source/resultrow-windows-source-timing.json').write_bytes((r/'docs/replica/source/windows-source-resultrow-motion.json').read_bytes())
elif a.action=='timing':
 print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--resultrow-timing' ($app+'\\resultrow-timing.json');if($LASTEXITCODE-ne 0){throw 'Owned timing failed'};"));(r/'docs/replica/source/resultrow-windows-timing.json').write_text(ps(base+"Get-Content -Raw ($app+'\\resultrow-timing.json')",retry_read=True)+'\n')
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-resultrow.json';prior=out.stat().st_mtime_ns if out.exists()else 0
 print(ps(source+"$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'result-row-record'));if($null-eq $b){throw 'Owned record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned ResultSummaryRow recorded');"))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists()and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source counts')
 v=json.loads(out.read_text());assert len(v['rows'])==len(json.loads((r/'docs/replica/source/resultrow-cases.json').read_text()));(r/'docs/replica/source/resultrow-windows-source.json').write_bytes(out.read_bytes());print(v['dpr'],len(v['rows']))
elif a.action=='update':
 import hashlib
 updates=[]
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll']:
  src=r/'artifacts/replica/windows-arm64-check'/name
  (r/'artifacts/replica/windows-download'/('resultrow-update-'+name)).write_bytes(src.read_bytes())
  updates.append((name,hashlib.sha256(src.read_bytes()).hexdigest().upper()))
 code=base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Close owned row before update'};$ProgressPreference='SilentlyContinue';"
 for name,digest in updates:
  code+="$f=Join-Path $app '"+name+"';$tmp=$f+'.update';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/resultrow-update-"+name+"' -OutFile $tmp;if((Get-FileHash $tmp -Algorithm SHA256).Hash-ne '"+digest+"'){throw 'Owned update hash mismatch'};Copy-Item $f ($f+'.previous') -Force;Move-Item $tmp $f -Force;"
 print(ps(code+"[Console]::WriteLine('Closed owned row assemblies updated and hash verified');"))
elif a.action=='prepare':
 print(ps(base+"$d=Split-Path $app;$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory -Force $d|Out-Null;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($d+'\\check.zip');Expand-Archive ($d+'\\check.zip') $app -Force;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/resultrow-cases.json' -OutFile ($app+'\\resultrow-cases.json');[Console]::WriteLine('Owned ResultSummaryRow prepared');",timeout=180))
elif a.action=='auto':
 print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--resultrow' ($app+'\\resultrow-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Count verification failed'};",timeout=90));v=ps(base+"Get-Content -Raw ($app+'\\resultrow-native.json')",retry_read=True);out=r/'docs/replica/source/resultrow-windows-native';out.mkdir(exist_ok=True);(out/'resultrow-native.json').write_text(v+'\n')
elif a.action=='launch':print(ps(base+"$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--resultrow-window',($app+'\\resultrow-cases.json'),$app) -PassThru;[Console]::WriteLine($p.Id);"))
elif a.action=='close':print(ps(base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica ResultSummaryRow verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});if($w.Count-ne 1){throw 'Owned ResultSummaryRow not unique'};$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
else:print(ps(source+"$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned ResultSummaryRow source closed');"))
