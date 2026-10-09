"""Fixed project TrialPreparation source; approved Parallels CLI/UIA with path/title guards."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','prepare','auto','launch','invoke','read','close']);p.add_argument('--target',choices=['unprepared','ready','hidden','english','reduced','record','prepare'],default='record');p.add_argument('--label',default='current');a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-TrialPreparation-20261008') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('TrialPreparation · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica TrialPreparation verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned TrialPreparation required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/trialpreparation.html');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
 print(ps(source+unique+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'trialpreparation-record'));if($null-ne $b){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($null-eq $b){throw 'Source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",retry_read=True))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh TrialPreparation source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==40;(r/'docs/replica/source/trialpreparation-windows-source.json').write_bytes(out.read_bytes());print('Fresh original40')
elif a.action=='prepare':
 code=base+"$old=Join-Path (Join-Path $env:TEMP 'DBMV-Sidebar-20261007') 'app';foreach($dir in @($app,$old)){if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($dir+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'}};if(!(Test-Path $app)){Copy-Item $old $app -Recurse};$ProgressPreference='SilentlyContinue';"
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll','trialpreparation-cases.json']:
  file=r/('artifacts/replica/windows-download'if name.endswith('.json')else'artifacts/replica/windows-arm64-check')/name;served='trialpreparation-update-'+name;(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper();code+="$f=Join-Path $app "+q(name)+";$next=$f+'.update';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $next;if((Get-FileHash $next -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned update hash mismatch'};if(Test-Path $f){Copy-Item $f ($f+'.previous') -Force};Move-Item $next $f -Force;"
 print(ps(code+"[Console]::WriteLine('Closed TrialPreparation runtime hash verified');"))
elif a.action=='auto':
 print(ps(base+"$f=Join-Path $app 'trialpreparation-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--trialpreparation' ($app+'\\trialpreparation-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned TrialPreparation failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native TrialPreparation'};",timeout=60));value=ps(base+"Get-Content -Raw ($app+'\\trialpreparation-native.json')",retry_read=True);v=json.loads(value);assert len(v['rows'])==40;out=r/'docs/replica/source/trialpreparation-windows-native';out.mkdir(exist_ok=True);(out/'trialpreparation-native.json').write_text(value+'\n');
 for filename in ['trialpreparation-lifecycle.json']:
  payload=ps(base+"Get-Content -Raw ($app+'\\"+filename+"')",retry_read=True);json.loads(payload);(out/filename).write_text(payload+'\n')
 import base64
 png=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes(($app+'\\trialpreparation.png')))",retry_read=True);(out/'trialpreparation.png').write_bytes(base64.b64decode(png));print('Fresh native40 and lifecycle7')
elif a.action=='launch':print(ps(find+"if($w.Count){throw 'Owned parent already open'};$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--trialpreparation-window',($app+'\\trialpreparation-cases.json'),$app) -PassThru;[Console]::WriteLine($p.Id);"))
elif a.action=='invoke':
 print(ps(find+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('TrialPreparation-'+a.target)+"));if($null-eq $b-or !$b.Current.IsEnabled){throw 'Owned enabled target required'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
elif a.action=='read':
 value=ps(find+unique+"Get-Content -Raw ($app+'\\trialpreparation-input.json')",retry_read=True);json.loads(value);out=r/'docs/replica/source/trialpreparation-windows-input';out.mkdir(exist_ok=True);assert a.label.replace('-','').isalnum();(out/(a.label+'.json')).write_text(value+'\n');print(value)
else:print(ps((source if a.action=='source-close'else find)+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
