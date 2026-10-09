"""Fixed project-owned TableMappingRow fixtures, approved CLI/UIA control."""
from pathlib import Path
import argparse,json,time
from driver import ps
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','motion-open','motion-record','timing','prepare','auto','launch','read','close']);a=p.parse_args();base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-TableMappingRow-20261007') 'app';"
if a.action=='source-open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/tablemappingrow.html');[Console]::WriteLine('Owned rows source requested');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-tablemappingrow.json';prior=out.stat().st_mtime_ns if out.exists()else 0
 print(ps("$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('TableMappingRow · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Unique owned original missing'};$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'table-row-record'));if($null-eq $b){throw 'Owned record button missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned rows recorded');"));deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists()and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh original record')
 data=json.loads(out.read_text());print(data['dpr'],len(data['rows']));(r/'docs/replica/source/tablemappingrow-windows-source.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');(r/'artifacts/replica/windows-download/tablemappingrow-windows-source.json').write_text(json.dumps(data,ensure_ascii=False)+'\n')
elif a.action=='motion-open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/tablemappingrow-motion.html');[Console]::WriteLine('Owned row motion source requested');"))
elif a.action=='motion-record':
 out=r/'docs/replica/source/windows-source-tablerow-motion.json';prior=out.stat().st_mtime_ns if out.exists()else 0
 print(ps("$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('TableMappingRow motion · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Unique owned original missing'};$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'table-row-motion-record'));if($null-eq $b){throw 'Owned record button missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned row motion recorded');"));deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists()and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh original motion record')
 data=json.loads(out.read_text());assert len(data['rows'])==96;(r/'docs/replica/source/tablemappingrow-windows-source-timing.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print(data['dpr'],len(data['rows']))
elif a.action=='timing':
 print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--tablerow-timing' ($app+'\\tablemappingrow-timing.json');if($LASTEXITCODE-ne 0){throw 'Row timing failed'};"));data=ps(base+"Get-Content -Raw ($app+'\\tablemappingrow-timing.json')",retry_read=True);(r/'docs/replica/source/tablemappingrow-windows-timing.json').write_text(data+'\n')
elif a.action=='prepare':print(ps(base+"$d=Split-Path $app;$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory -Force $d|Out-Null;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($d+'\\check.zip');Expand-Archive ($d+'\\check.zip') $app -Force;foreach($f in @('tablemappingrow-cases.json','tablemappingrow-windows-source.json')){Invoke-WebRequest -UseBasicParsing ('http://10.211.55.2:8768/'+$f) -OutFile ($app+'\\'+$f)};[Console]::WriteLine('Owned rows prepared');",timeout=180))
elif a.action=='auto':
 print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--tablerow' ($app+'\\tablemappingrow-cases.json') ($app+'\\tablemappingrow-windows-source.json') $app;if($LASTEXITCODE-ne 0){throw 'Atomic rows failed'};",timeout=90));data=ps(base+"Get-Content -Raw ($app+'\\tablemappingrow-native.json')",retry_read=True);o=r/'docs/replica/source/tablemappingrow-windows-native';o.mkdir(exist_ok=True);(o/'tablemappingrow-native.json').write_text(data+'\n');print(len(json.loads(data)['rows']))
elif a.action=='launch':print(ps(base+"$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--tablerow-window',($app+'\\tablemappingrow-cases.json'),($app+'\\tablemappingrow-windows-source.json'),$app) -PassThru;[Console]::WriteLine($p.Id);"))
elif a.action=='read':print(ps(base+"Get-Content -Raw ($app+'\\tablemappingrow-input.json')",retry_read=True))
else:print(ps(base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica TableMappingRow verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});if($w.Count-ne 1){throw 'Unique owned rows missing'};$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned rows closed');"))
