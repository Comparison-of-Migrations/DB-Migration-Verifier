"""Fixed project drawer/source; approved Parallels CLI/UIA with path/title guards."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','prepare','auto','launch','record','open','cancel','apply','reduced','collision','read','close','search','choose']);p.add_argument('--label',default='current');p.add_argument('--text',default='');p.add_argument('--index',type=int,choices=range(4),default=0);a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-MatchingDialog-20261008') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('MatchingDialog · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica MatchingDialog verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned MatchingDialog required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/matchingdialog.html');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
 print(ps(source+unique+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'matchingdialog-record'));if($null-ne $b){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($null-eq $b){throw 'Source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",retry_read=True))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh drawer source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==48;(r/'docs/replica/source/matchingdialog-windows-source.json').write_bytes(out.read_bytes());print('Fresh original48')
elif a.action=='prepare':
 code=base+"$old=Join-Path (Join-Path $env:TEMP 'DBMV-Sidebar-20261007') 'app';foreach($dir in @($app,$old)){if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($dir+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'}};if(!(Test-Path $app)){Copy-Item $old $app -Recurse};$ProgressPreference='SilentlyContinue';"
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll','matchingdialog-cases.json']:
  file=r/('artifacts/replica/windows-download'if name.endswith('.json')else'artifacts/replica/windows-arm64-check')/name;served='matchingdialog-update-'+name;(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper();code+="$f=Join-Path $app "+q(name)+";$next=$f+'.update';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $next;if((Get-FileHash $next -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned update hash mismatch'};if(Test-Path $f){Copy-Item $f ($f+'.previous') -Force};Move-Item $next $f -Force;"
 print(ps(code+"[Console]::WriteLine('Closed drawer runtime hash verified');"))
elif a.action=='auto':
 print(ps(base+"$f=Join-Path $app 'matchingdialog-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--matchingdialog' ($app+'\\matchingdialog-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned drawer failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native drawer'};",timeout=60));value=ps(base+"Get-Content -Raw ($app+'\\matchingdialog-native.json')",retry_read=True);v=json.loads(value);assert len(v['rows'])==48;out=r/'docs/replica/source/matchingdialog-windows-native';out.mkdir(exist_ok=True);(out/'matchingdialog-native.json').write_text(value+'\n');print('Fresh native48')
elif a.action=='launch':print(ps(find+"if($w.Count){throw 'Owned drawer already open'};$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--matchingdialog-window',($app+'\\matchingdialog-cases.json'),$app) -PassThru;[Console]::WriteLine($p.Id);"))
elif a.action in ['record','open','cancel','apply','reduced','collision','search','choose']:
 target='MatchingDialog-'+('choice-'+str(a.index)if a.action=='choose'else a.action);pattern='Value'if a.action=='search'else'SelectionItem'if a.action=='choose'else'Invoke';method='SetValue('+q(a.text)+')'if a.action=='search'else'Select()'if a.action=='choose'else'Invoke()';print(ps(find+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(target)+"));if($null-eq $b-or !$b.Current.IsEnabled){throw 'Owned enabled target required'};$b.GetCurrentPattern([System.Windows.Automation."+pattern+"Pattern]::Pattern)."+method+";"))
elif a.action=='read':
 value=ps(find+unique+"Get-Content -Raw ($app+'\\matchingdialog-input.json')",retry_read=True);v=json.loads(value);out=r/'docs/replica/source/matchingdialog-windows-input';out.mkdir(exist_ok=True);(out/(a.label+'.json')).write_text(value+'\n');print(dict(visible=v['visible'],closing=v['closing'],focus=v['focus'],events=v['events']))
else:print(ps((source if a.action=='source-close'else find)+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
