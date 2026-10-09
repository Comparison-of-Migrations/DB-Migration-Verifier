"""Fixed project CategorySection windows only, official Parallels CLI/UIA, guarded runtime updates."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','prepare','auto','launch','invoke-expand','toggle-select','record','reduced','read','source-motion','close','source-close']);p.add_argument('--label',default='current');a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-CategorySection-20261007') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('CategorySection · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica CategorySection verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned CategorySection required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/categorysection.html');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
 print(ps(source+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'section-record'));if($null-eq $b){throw 'Owned source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh CategorySection source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==192;(r/'docs/replica/source/categorysection-windows-source.json').write_bytes(out.read_bytes());print('Fresh original192')
elif a.action=='prepare':
 code=base+"$old=Join-Path (Join-Path $env:TEMP 'DBMV-Sidebar-20261007') 'app';foreach($dir in @($app,$old)){if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($dir+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'}};if(!(Test-Path $app)){Copy-Item $old $app -Recurse};$ProgressPreference='SilentlyContinue';"
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll','categorysection-cases.json']:
  file=r/('artifacts/replica/windows-download'if name.endswith('.json')else'artifacts/replica/windows-arm64-check')/name;served='categorysection-update-'+name;(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper()
  code+="$f=Join-Path $app "+q(name)+";$next=$f+'.update';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $next;if((Get-FileHash $next -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned update hash mismatch'};if(Test-Path $f){Copy-Item $f ($f+'.previous') -Force};Move-Item $next $f -Force;"
 print(ps(code+"[Console]::WriteLine('Closed CategorySection runtime prepared and hash verified');"))
elif a.action=='auto':
 print(ps(base+"$f=Join-Path $app 'categorysection-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--categorysection' ($app+'\\categorysection-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned CategorySection failed'};& ($app+'\\ReplicaProbe.exe') '--categorysection-timing' ($app+'\\categorysection-native.json') ($app+'\\categorysection-motion-native.json');if($LASTEXITCODE-ne 0){throw 'Owned CategorySection timing failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native CategorySection'};",timeout=60));v=ps(base+"Get-Content -Raw ($app+'\\categorysection-native.json')",retry_read=True);out=r/'docs/replica/source/categorysection-windows-native';out.mkdir(exist_ok=True);(out/'categorysection-native.json').write_text(v+'\n');(out/'categorysection-motion-native.json').write_text(ps(base+"Get-Content -Raw ($app+'\\categorysection-motion-native.json')",retry_read=True)+'\n');print('Fresh native192 and timing72')
elif a.action=='launch':print(ps(find+"if($w.Count){throw 'Owned CategorySection already open'};$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--categorysection-window',($app+'\\categorysection-cases.json'),$app) -PassThru;$deadline=[DateTime]::UtcNow.AddSeconds(6);do{$window=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Replica CategorySection verification'));if($null-ne $window -and $window.Current.ProcessId-eq $p.Id){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow-lt $deadline);if($null-eq $window-or $window.Current.ProcessId-ne $p.Id){throw 'Owned CategorySection not ready'};[Console]::WriteLine($p.Id);"))
elif a.action in ['invoke-expand','toggle-select','record','reduced']:
 id={'invoke-expand':'CategorySection-expand','toggle-select':'CategorySection-select','record':'CategorySection-record','reduced':'CategorySection-reduced'}[a.action]
 pattern='Toggle' if a.action=='toggle-select' else 'Invoke'
 operation='Toggle' if a.action=='toggle-select' else 'Invoke'
 print(ps(find+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(id)+"));if($null-eq $b-or !$b.Current.IsEnabled){throw 'Owned section target missing'};$b.GetCurrentPattern([System.Windows.Automation."+pattern+"Pattern]::Pattern)."+operation+"();"))
elif a.action=='source-motion':
 out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
 print(ps(source+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'section-motion'));if($null-eq $b){throw 'Owned motion record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh section motion source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==72;(r/'docs/replica/source/categorysection-windows-motion-source.json').write_bytes(out.read_bytes());print('Fresh original72 motion')
elif a.action=='read':
 value=ps(find+unique+"$v=Get-Content -Raw ($app+'\\categorysection-input.json')|ConvertFrom-Json;$texts=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Text))|Where-Object{$_.Current.Name.StartsWith('DATA-')});$v|Add-Member -NotePropertyName automation -NotePropertyValue ([pscustomobject]@{detailTextCount=$texts.Count;details=@($texts|ForEach-Object{$_.Current.Name})});$v|ConvertTo-Json -Depth 30",retry_read=True);v=json.loads(value);out=r/'docs/replica/source/categorysection-windows-input';out.mkdir(exist_ok=True);(out/(a.label+'.json')).write_text(value+'\n');print(v['events'],v['progress'])
else:print(ps((source if a.action=='source-close'else find)+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
