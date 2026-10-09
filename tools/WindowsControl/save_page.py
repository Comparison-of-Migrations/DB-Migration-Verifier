"""Fixed project SavePage source; approved Parallels CLI/UIA with path/title guards."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','prepare','auto','launch','read','invoke','close','request']);p.add_argument('--target',choices=['language','mount','reduced','record','open','restart','download'],default='record');a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-SavePage-20261008') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('SavePage · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica SavePage verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned SavePage required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/savepage.html');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
 print(ps(source+unique+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'savepage-record'));if($null-ne $b){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($null-eq $b){throw 'Source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",retry_read=True))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh SavePage source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==48;(r/'docs/replica/source/savepage-windows-source.json').write_bytes(out.read_bytes());print('Fresh original48')
elif a.action=='prepare':
 code=base+"$old=Join-Path (Join-Path $env:TEMP 'DBMV-Sidebar-20261007') 'app';foreach($dir in @($app,$old)){if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($dir+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'}};if(!(Test-Path $app)){Copy-Item $old $app -Recurse};$ProgressPreference='SilentlyContinue';"
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll','savepage-cases.json']:
  file=r/('artifacts/replica/windows-download'if name.endswith('.json')else'artifacts/replica/windows-arm64-check')/name;served='savepage-update-'+name;(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper();code+="$f=Join-Path $app "+q(name)+";$next=$f+'.update';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $next;if((Get-FileHash $next -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned update hash mismatch'};if(Test-Path $f){Copy-Item $f ($f+'.previous') -Force};Move-Item $next $f -Force;"
 print(ps(code+"[Console]::WriteLine('Closed SavePage runtime hash verified');"))
elif a.action=='auto':
 print(ps(base+"$f=Join-Path $app 'savepage-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--savepage' ($app+'\\savepage-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned SavePage failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native SavePage'};",timeout=60));value=ps(base+"Get-Content -Raw ($app+'\\savepage-native.json')",retry_read=True);v=json.loads(value);assert len(v['rows'])==48;out=r/'docs/replica/source/savepage-windows-native';out.mkdir(exist_ok=True);(out/'savepage-native.json').write_text(value+'\n');
 for filename in []:
  payload=ps(base+"Get-Content -Raw ($app+'\\"+filename+"')",retry_read=True);json.loads(payload);(out/filename).write_text(payload+'\n')
 import base64
 for filename in ['savepage.png','savepage-compact-ko-true-0-656.png','savepage-wide-en-true-3-904.png']:
  png=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes(($app+'\\"+filename+"')))",retry_read=True);(out/filename).write_bytes(base64.b64decode(png))
 print('Fresh native48')
elif a.action=='source-close':print(ps(source+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))

elif a.action=='launch':
 print(ps(base+"Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--savepage-window',($app+'\\savepage-cases.json'),$app);"))
 print(ps("$deadline=[DateTime]::UtcNow.AddSeconds(8);do{"+find+"if($w.Count-eq 1){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);"+unique+"[Console]::WriteLine('Owned window ready');",retry_read=True))
elif a.action=='read':
 data=ps(find+unique+"Get-Content -Raw ($app+'\\savepage-input.json')",retry_read=True);json.loads(data);out=r/'docs/replica/source/savepage-windows-input';out.mkdir(exist_ok=True);(out/'read-final.json').write_text(data+'\n');print(data)
elif a.action=='close':print(ps(find+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='invoke':print(ps(find+unique+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('Save-'+a.target if a.target in ['open','restart'] else 'Footer-download' if a.target=='download' else 'SavePage-'+a.target)+"));if($null-eq $c){throw 'Owned button missing'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))

elif a.action=='request':
 data=ps(find+unique+"Get-Content -Raw ($app+'\\savepage-request.json')",retry_read=True);json.loads(data);(r/'docs/replica/source/savepage-windows-input/request-final.json').write_text(data+'\n');print(data)
