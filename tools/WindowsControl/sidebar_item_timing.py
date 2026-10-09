"""Fixed source transition fixture and pure native clocks via approved CLI/UIA."""
from pathlib import Path
import argparse,json,time
from driver import ps
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['open','record','native']);a=p.parse_args()
if a.action=='open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/sidebaritem-motion.html');[Console]::WriteLine('Owned SidebarItem motion source requested');"))
elif a.action=='record':
 out=root/'docs/replica/source/windows-source-sidebaritem-motion.json';prior=out.stat().st_mtime_ns if out.exists() else 0
 print(ps("$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name.StartsWith('SidebarItem motion · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Expected unique owned SidebarItem source'};$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'sidebar-motion-record'));if($null-eq $b){throw 'Owned record button missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned SidebarItem source recorded');"));end=time.monotonic()+8
 while time.monotonic()<end:
  if out.exists() and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source evidence')
 data=json.loads(out.read_text());(root/'docs/replica/source/sidebaritem-windows-source-timing.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print({'scale':data['dpr'],'rows':len(data['rows'])})
elif a.action=='native':
 print(ps("$d=Join-Path $env:TEMP 'DBMV-SidebarItem-Timing-20261007';$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory -Force $d|Out-Null;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($d+'\\check.zip');Expand-Archive ($d+'\\check.zip') ($d+'\\app') -Force;& ($d+'\\app\\ReplicaProbe.exe') '--sidebaritem-timing' ($d+'\\timing.json');if($LASTEXITCODE-ne 0){throw 'Timing probe failed'};[Console]::WriteLine('Owned pure native clock samples recorded');",timeout=180))
 data=json.loads(ps("$d=Join-Path $env:TEMP 'DBMV-SidebarItem-Timing-20261007';Get-Content -Raw ($d+'\\timing.json')",retry_read=True));(root/'docs/replica/source/sidebaritem-windows-timing.json').write_text(json.dumps(data,indent=2)+'\n');print(len(data['rows']),'native timing samples')
