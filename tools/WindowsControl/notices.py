"""Fixed passive Notice project fixtures via official CLI/UI Automation."""
from pathlib import Path
import argparse,json,time,base64
from driver import ps
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','prepare','auto']);a=p.parse_args()
base="$d=Join-Path $env:TEMP 'DBMV-Notice-20261007';$probeApp=Join-Path $d 'app';"
if a.action=='source-open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/notice.html');[Console]::WriteLine('Owned Notice source requested');"))
elif a.action=='source-record':
 out=root/'docs/replica/source/windows-source-notice.json';prior=out.stat().st_mtime_ns if out.exists() else 0
 print(ps("$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name.StartsWith('Notice · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Expected unique owned Notice source'};$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'notice-record'));if($null-eq $b){throw 'Owned record button missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned Notice source recorded');"));end=time.monotonic()+8
 while time.monotonic()<end:
  if out.exists() and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source evidence')
 data=json.loads(out.read_text());(root/'docs/replica/source/notice-windows-source.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print({'scale':data['dpr'],'rows':len(data['rows'])})
elif a.action=='prepare':print(ps(base+"$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory -Force $d|Out-Null;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($d+'\\check.zip');Expand-Archive ($d+'\\check.zip') $probeApp -Force;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/notice-cases.json' -OutFile ($probeApp+'\\notice-cases.json');[Console]::WriteLine('Owned Notice prepared');",timeout=180))
elif a.action=='auto':
 print(ps(base+"& ($probeApp+'\\ReplicaProbe.exe') '--notice' ($probeApp+'\\notice-cases.json') $probeApp;if($LASTEXITCODE-ne 0){throw 'Probe failed'};[Console]::WriteLine((Get-Item ($probeApp+'\\notice-native.json')).LastWriteTimeUtc.ToString('o'));",timeout=100))
 out=root/'docs/replica/source/notice-windows-native';out.mkdir(exist_ok=True);data=json.loads(ps(base+"Get-Content -Raw ($probeApp+'\\notice-native.json')",retry_read=True));(out/'notice-native.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');(out/'notice-native.png').write_bytes(base64.b64decode(ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($probeApp+'\\notice-native.png'))",retry_read=True)));print({'scale':data['scale'],'rows':len(data['rows'])})
