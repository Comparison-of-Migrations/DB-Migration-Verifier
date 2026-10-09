"""Approved official CLI/UIA, fixed project ResultCount measurement window only."""
from pathlib import Path
import argparse,json,time
from driver import ps
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-record','prepare','auto','source-close']);a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-ResultCount-20261007') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('ResultCount · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Unique owned Count original missing'};"
if a.action=='source-record':
 out=r/'docs/replica/source/windows-source-resultcount.json';prior=out.stat().st_mtime_ns if out.exists()else 0
 print(ps(source+"$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'result-count-record'));if($null-eq $b){throw 'Owned record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned counts recorded');"))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists()and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source counts')
 v=json.loads(out.read_text());assert len(v['rows'])==54 and len(v['timing'])==48 and not v['originalReduce'];(r/'docs/replica/source/resultcount-windows-source.json').write_bytes(out.read_bytes());print(v['dpr'],len(v['rows']),len(v['timing']))
elif a.action=='prepare':
 print(ps(base+"$d=Split-Path $app;$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory -Force $d|Out-Null;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($d+'\\check.zip');Expand-Archive ($d+'\\check.zip') $app -Force;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/resultcount-cases.json' -OutFile ($app+'\\resultcount-cases.json');[Console]::WriteLine('Owned counts prepared');",timeout=180))
elif a.action=='auto':
 print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--resultcount' ($app+'\\resultcount-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Count verification failed'};",timeout=90));v=ps(base+"Get-Content -Raw ($app+'\\resultcount-native.json')",retry_read=True);out=r/'docs/replica/source/resultcount-windows-native';out.mkdir(exist_ok=True);(out/'resultcount-native.json').write_text(v+'\n')
else:print(ps(source+"$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned Count source closed');"))
