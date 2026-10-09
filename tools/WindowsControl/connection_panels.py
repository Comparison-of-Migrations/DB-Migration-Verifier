"""Fixed project ConnectionPanel windows only, official Parallels CLI/UIA, guarded runtime updates."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','prepare','auto','launch','edit','read','close','source-close']);p.add_argument('--label',default='current');a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-ConnectionPanel-20261007') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('ConnectionPanel · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica ConnectionPanel verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned ConnectionPanel required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/connectionpanel.html');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
 print(ps(source+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'panel-record'));if($null-eq $b){throw 'Owned source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh ConnectionPanel source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==40;(r/'docs/replica/source/connectionpanel-windows-source.json').write_bytes(out.read_bytes());print('Fresh original40')
elif a.action=='prepare':
 code=base+"$old=Join-Path (Join-Path $env:TEMP 'DBMV-Sidebar-20261007') 'app';foreach($dir in @($app,$old)){if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($dir+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'}};if(!(Test-Path $app)){Copy-Item $old $app -Recurse};$ProgressPreference='SilentlyContinue';"
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll','connectionpanel-cases.json']:
  file=r/('artifacts/replica/windows-download'if name.endswith('.json')else'artifacts/replica/windows-arm64-check')/name;served='connectionpanel-update-'+name;(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper()
  code+="$f=Join-Path $app "+q(name)+";$next=$f+'.update';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $next;if((Get-FileHash $next -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned update hash mismatch'};if(Test-Path $f){Copy-Item $f ($f+'.previous') -Force};Move-Item $next $f -Force;"
 print(ps(code+"[Console]::WriteLine('Closed ConnectionPanel runtime prepared and hash verified');"))
elif a.action=='auto':
 print(ps(base+"$f=Join-Path $app 'connectionpanel-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--connectionpanel' ($app+'\\connectionpanel-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned ConnectionPanel failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native ConnectionPanel'};",timeout=60));v=ps(base+"Get-Content -Raw ($app+'\\connectionpanel-native.json')",retry_read=True);out=r/'docs/replica/source/connectionpanel-windows-native';out.mkdir(exist_ok=True);(out/'connectionpanel-native.json').write_text(v+'\n');print('Fresh native40')
elif a.action=='launch':print(ps(find+"if($w.Count){throw 'Owned ConnectionPanel already open'};$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--connectionpanel-window',($app+'\\connectionpanel-cases.json'),$app) -PassThru;$deadline=[DateTime]::UtcNow.AddSeconds(6);do{$window=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Replica ConnectionPanel verification'));if($null-ne $window -and $window.Current.ProcessId-eq $p.Id){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow-lt $deadline);if($null-eq $window-or $window.Current.ProcessId-ne $p.Id){throw 'Owned ConnectionPanel not ready'};[Console]::WriteLine($p.Id);"))
elif a.action=='edit':
 code=find+unique
 for key,value in [('host','fixture.example.invalid'),('port','5432'),('db','fixture_sample'),('user','fixture_reader')]:
  condition=("[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'포트')" if key=='port' else "[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('ConnectionPanel-'+key)+")")
  code+="$targets=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,"+condition+")|Where-Object{$_.Current.IsEnabled-and $_.Current.ControlType-eq "+("[System.Windows.Automation.ControlType]::Spinner" if key=='port' else "[System.Windows.Automation.ControlType]::Edit")+"});if($targets.Count-ne 1){throw 'Owned editable field must be unique'};$targets[0].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("+q(value)+");"

 print(ps(code))
elif a.action=='read':
 value=ps(find+unique+"Get-Content -Raw ($app+'\\connectionpanel-input.json')",retry_read=True);v=json.loads(value);out=r/'docs/replica/source/connectionpanel-windows-input';out.mkdir(exist_ok=True);(out/(a.label+'.json')).write_text(value+'\n');print(v['events'])
else:print(ps((source if a.action=='source-close'else find)+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
