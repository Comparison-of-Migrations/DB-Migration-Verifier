"""Fixed project-owned CategoryHeader original/native fixtures."""
from pathlib import Path
import argparse,json,time,base64
from driver import ps
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','prepare','auto','launch','read','close']);a=p.parse_args()
base="$d=Join-Path $env:TEMP 'DBMV-CategoryHeader-20261007';$app=Join-Path $d 'app';"
find=base+"$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name-eq 'Replica CategoryHeader verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});if($w.Count-ne 1){throw 'Unique owned header fixture missing'};$w=$w[0];"
if a.action=='source-open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/categoryheader.html');[Console]::WriteLine('Owned header source requested');"))
elif a.action=='source-record':
 out=root/'docs/replica/source/windows-source-categoryheader.json';prior=out.stat().st_mtime_ns if out.exists()else 0
 print(ps("$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name.StartsWith('CategoryHeader · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Unique owned source missing'};$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'categoryheader-record'));if($null-eq $b){throw 'Owned record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned source recorded');"));end=time.monotonic()+8
 while time.monotonic()<end:
  if out.exists()and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source evidence')
 data=json.loads(out.read_text());(root/'docs/replica/source/categoryheader-windows-source.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print({'dpr':data['dpr'],'rows':len(data['rows'])})
elif a.action=='prepare':print(ps(base+"$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory -Force $d|Out-Null;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($d+'\\check.zip');Expand-Archive ($d+'\\check.zip') $app -Force;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/categoryheader-cases.json' -OutFile ($app+'\\categoryheader-cases.json');[Console]::WriteLine('Owned header prepared');",timeout=180))
elif a.action=='auto':
 print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--categoryheader' ($app+'\\categoryheader-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Probe failed'};[Console]::WriteLine((Get-Item ($app+'\\categoryheader-native.json')).LastWriteTimeUtc.ToString('o'));",timeout=100));data=json.loads(ps(base+"Get-Content -Raw ($app+'\\categoryheader-native.json')",retry_read=True));out=root/'docs/replica/source/categoryheader-windows-native';out.mkdir(exist_ok=True);(out/'categoryheader-native.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print({'scale':data['scale'],'rows':len(data['rows'])})
elif a.action=='launch':print(ps(base+"$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--categoryheader-window',($app+'\\categoryheader-cases.json'),$app) -PassThru;[Console]::WriteLine($p.Id);"))
elif a.action=='read':print(ps(base+"Get-Content -Raw ($app+'\\categoryheader-input.json')",retry_read=True))
elif a.action=='close':print(ps(find+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned header close requested');"))
