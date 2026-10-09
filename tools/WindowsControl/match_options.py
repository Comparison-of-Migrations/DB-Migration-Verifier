"""Fixed project MatchOption windows only, official Parallels CLI/UIA, guarded runtime updates."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','motion-source-open','motion-source-record','prepare','auto','launch','choose','record','reduced','read','close','source-close','motion-source-close']);p.add_argument('--label',default='current');p.add_argument('--index',type=int,choices=range(4),default=0);a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-MatchOption-20261007') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('MatchOption · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica MatchOption verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned MatchOption required'};$w=$w[0];"
if a.action.startswith('motion-'):source=source.replace('MatchOption · original isolation','MatchOption motion · original isolation')
if a.action in ['source-open','motion-source-open']:print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/"+('matchoption-motion.html'if a.action.startswith('motion-')else'matchoption.html')+"');"))
elif a.action in ['source-record','motion-source-record']:
 out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
 print(ps(source+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('match-motion-record'if a.action.startswith('motion-')else'matchoption-record')+"));if($null-eq $b){throw 'Owned source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh MatchOption source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==(50 if a.action.startswith('motion-')else 136);(r/'docs/replica/source'/('matchoption-windows-source-timing.json'if a.action.startswith('motion-')else'matchoption-windows-source.json')).write_bytes(out.read_bytes());print('Fresh original'+str(len(v['rows'])))
elif a.action=='prepare':
 code=base+"$old=Join-Path (Join-Path $env:TEMP 'DBMV-Sidebar-20261007') 'app';foreach($dir in @($app,$old)){if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($dir+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'}};if(!(Test-Path $app)){Copy-Item $old $app -Recurse};$ProgressPreference='SilentlyContinue';"
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll','matchoption-cases.json']:
  file=r/('artifacts/replica/windows-download'if name.endswith('.json')else'artifacts/replica/windows-arm64-check')/name;served='matchoption-update-'+name;(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper()
  code+="$f=Join-Path $app "+q(name)+";$next=$f+'.update';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $next;if((Get-FileHash $next -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned update hash mismatch'};if(Test-Path $f){Copy-Item $f ($f+'.previous') -Force};Move-Item $next $f -Force;"
 print(ps(code+"[Console]::WriteLine('Closed MatchOption runtime prepared and hash verified');"))
elif a.action=='auto':
 print(ps(base+"$f=Join-Path $app 'matchoption-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--matchoption' ($app+'\\matchoption-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned MatchOption failed'};& ($app+'\\ReplicaProbe.exe') '--matchoption-timing' ($app+'\\matchoption-timing.json');if($LASTEXITCODE-ne 0){throw 'Owned timing failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native MatchOption'};",timeout=60));v=ps(base+"Get-Content -Raw ($app+'\\matchoption-native.json')",retry_read=True);out=r/'docs/replica/source/matchoption-windows-native';out.mkdir(exist_ok=True);(out/'matchoption-native.json').write_text(v+'\n');(r/'docs/replica/source/matchoption-windows-timing.json').write_text(ps(base+"Get-Content -Raw ($app+'\\matchoption-timing.json')",retry_read=True)+'\n');print('Fresh native136 and timing50')
elif a.action=='launch':print(ps(find+"if($w.Count){throw 'Owned MatchOption already open'};$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--matchoption-window',($app+'\\matchoption-cases.json'),$app) -PassThru;$deadline=[DateTime]::UtcNow.AddSeconds(6);do{$window=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Replica MatchOption verification'));if($null-ne $window-and $window.Current.ProcessId-eq $p.Id){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow-lt $deadline);if($null-eq $window-or $window.Current.ProcessId-ne $p.Id){throw 'Owned MatchOption not ready'};[Console]::WriteLine($p.Id);"))
elif a.action in ['choose','record','reduced']:
 target='MatchOption-choice-'+str(a.index)if a.action=='choose'else'MatchOption-'+a.action;pattern='SelectionItem'if a.action=='choose'else'Invoke';method='Select'if a.action=='choose'else'Invoke'
 print(ps(find+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(target)+"));if($null-eq $b){throw 'Owned target missing'};$b.GetCurrentPattern([System.Windows.Automation."+pattern+"Pattern]::Pattern)."+method+"();"))
elif a.action=='read':
 value=ps(find+unique+"Get-Content -Raw ($app+'\\matchoption-input.json')",retry_read=True);v=json.loads(value);out=r/'docs/replica/source/matchoption-windows-input';out.mkdir(exist_ok=True);(out/(a.label+'.json')).write_text(value+'\n');print(v['events'])
else:print(ps((source if a.action in ['source-close','motion-source-close']else find)+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
