"""Numeric boundary regression in fixed owned typography fixtures."""
from pathlib import Path
import argparse,json,time
from driver import ps
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['open','record','native']);a=p.parse_args();folder=root/'docs/replica/source'
if a.action=='open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/notice-typography.html');[Console]::WriteLine('Owned typography boundary source requested');"))
elif a.action=='record':
 out=folder/'windows-source-typography-notice.json';prior=out.stat().st_mtime_ns if out.exists() else 0
 print(ps("$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name.StartsWith('Notice typography boundaries') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Expected unique owned typography boundary source'};$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'notice-typography-record'));if($null-eq $b){throw 'Owned record button missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned boundary source recorded');"));end=time.monotonic()+8
 while time.monotonic()<end:
  if out.exists() and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source evidence')
 (folder/'typography-notice-windows-boundary-web.json').write_bytes(out.read_bytes());print('Fresh 48 boundary source rows')
elif a.action=='native':
 base="$d=Join-Path $env:TEMP 'DBMV-Notice-20261007';$probeApp=Join-Path $d 'app';$ProgressPreference='SilentlyContinue';"
 for cases,output in [('typography-notice-boundary-cases.json','typography-notice-windows-boundary-native'),('typography-windows-extended-cases.json','typography-notice-windows66-native')]:
  print(ps(base+"Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/"+cases+"' -OutFile ($probeApp+'\\"+cases+"');& ($probeApp+'\\ReplicaProbe.exe') '--text-states' ($probeApp+'\\"+cases+"') $probeApp;if($LASTEXITCODE-ne 0){throw 'Probe failed'};Get-Content -Raw ($probeApp+'\\typography-state-native.json');",timeout=100)[:140])
  data=json.loads(ps(base+"Get-Content -Raw ($probeApp+'\\typography-state-native.json');",retry_read=True));out=folder/output;out.mkdir(exist_ok=True);(out/'typography-state-native.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print(output,len(data))
