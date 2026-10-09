"""Approved official CLI/UIA, fixed project ResultChart measurement window only."""
from pathlib import Path
import argparse,json,time
from driver import ps
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','prepare','auto','source-close','launch','close','update','lifecycle','invoke','read']);a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-ResultChart-20261007') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('ResultChart · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Unique owned Count original missing'};"
if a.action=='source-open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/resultchart.html');[Console]::WriteLine('Owned ResultChart source requested');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-resultchart.json';prior=out.stat().st_mtime_ns if out.exists()else 0
 print(ps(source+"$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'result-chart-record'));if($null-eq $b){throw 'Owned record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned ResultChart recorded');"))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists()and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source counts')
 v=json.loads(out.read_text());assert len(v['rows'])==len(json.loads((r/'docs/replica/source/resultchart-cases.json').read_text()));(r/'docs/replica/source/resultchart-windows-source.json').write_bytes(out.read_bytes());print(v['dpr'],len(v['rows']))
elif a.action=='lifecycle':
 print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--resultchart-lifecycle' ($app+'\\resultchart-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned chart lifecycle failed'};",timeout=90));(r/'docs/replica/source/resultchart-windows-lifecycle.json').write_text(ps(base+"Get-Content -Raw ($app+'\\resultchart-lifecycle.json')",retry_read=True)+'\n')
elif a.action=='invoke':
 find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica ResultChart verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});if($w.Count-ne 1){throw 'Unique owned Chart missing'};"
 print(ps(find+"foreach($key in @('data','schema','access','objects')){$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,('ChartRow-'+$key)));if($null-eq $b){throw 'Owned Chart row missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();};"))
elif a.action=='read':
 (r/'docs/replica/source/resultchart-windows-input.json').write_text(ps(base+"Get-Content -Raw ($app+'\\resultchart-input.json')",retry_read=True)+'\n');print('Owned chart input read')
elif a.action=='update':
 import hashlib
 updates=[]
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll']:
  src=r/'artifacts/replica/windows-arm64-check'/name
  (r/'artifacts/replica/windows-download'/('resultchart-update-'+name)).write_bytes(src.read_bytes())
  updates.append((name,hashlib.sha256(src.read_bytes()).hexdigest().upper()))
 code=base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Close owned row before update'};$ProgressPreference='SilentlyContinue';"
 for name,digest in updates:
  code+="$f=Join-Path $app '"+name+"';$tmp=$f+'.update';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/resultchart-update-"+name+"' -OutFile $tmp;if((Get-FileHash $tmp -Algorithm SHA256).Hash-ne '"+digest+"'){throw 'Owned update hash mismatch'};Copy-Item $f ($f+'.previous') -Force;Move-Item $tmp $f -Force;"
 print(ps(code+"[Console]::WriteLine('Closed owned row assemblies updated and hash verified');"))
elif a.action=='prepare':
 print(ps(base+"$d=Split-Path $app;$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory -Force $d|Out-Null;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($d+'\\check.zip');Expand-Archive ($d+'\\check.zip') $app -Force;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/resultchart-cases.json' -OutFile ($app+'\\resultchart-cases.json');[Console]::WriteLine('Owned ResultChart prepared');",timeout=180))
elif a.action=='auto':
 print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--resultchart' ($app+'\\resultchart-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Count verification failed'};",timeout=90));v=ps(base+"Get-Content -Raw ($app+'\\resultchart-native.json')",retry_read=True);out=r/'docs/replica/source/resultchart-windows-native';out.mkdir(exist_ok=True);(out/'resultchart-native.json').write_text(v+'\n')
elif a.action=='launch':print(ps(base+"$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--resultchart-window',($app+'\\resultchart-cases.json'),$app) -PassThru;[Console]::WriteLine($p.Id);"))
elif a.action=='close':print(ps(base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica ResultChart verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});if($w.Count-ne 1){throw 'Owned ResultChart not unique'};$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
else:print(ps(source+"$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned ResultChart source closed');"))
