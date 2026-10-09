"""Scoped InfoDialogBody source/native evidence, official CLI/UIA and owned runtime guards."""
from pathlib import Path
import argparse,json,time,hashlib,base64
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','prepare','auto']);a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-InfoDialogBody-20261008') 'app';"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('InfoDialogBody · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
unique="if($w.Count-ne 1){throw 'Unique owned InfoDialogBody required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/infodialogbody.html');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
 print(ps(source+unique+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'infodialogbody-record'));if($null-ne $b){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($null-eq $b){throw 'Source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",retry_read=True))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh InfoDialogBody source')
 v=json.loads(out.read_text());assert len(v['rows'])==32;(r/'docs/replica/source/infodialogbody-windows-source.json').write_bytes(out.read_bytes());print('Fresh original32')
elif a.action=='source-close':print(ps(source+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='prepare':
 code=base+"$old=Join-Path (Join-Path $env:TEMP 'DBMV-Sidebar-20261007') 'app';foreach($dir in @($app,$old)){if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($dir+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'}};if(!(Test-Path $app)){Copy-Item $old $app -Recurse};$ProgressPreference='SilentlyContinue';"
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll','infodialogbody-cases.json']:
  file=r/('artifacts/replica/windows-download'if name.endswith('.json')else'artifacts/replica/windows-arm64-check')/name;served='infodialogbody-update-'+name;(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper();code+="$f=Join-Path $app "+q(name)+";$next=$f+'.update';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $next;if((Get-FileHash $next -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned update hash mismatch'};if(Test-Path $f){Copy-Item $f ($f+'.previous') -Force};Move-Item $next $f -Force;"
 print(ps(code+"[Console]::WriteLine('Closed InfoDialogBody runtime hash verified');"))
elif a.action=='auto':
 print(ps(base+"$f=Join-Path $app 'infodialogbody-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--infodialogbody' ($app+'\\infodialogbody-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned InfoDialogBody failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native InfoDialogBody'};",timeout=60));out=r/'docs/replica/source/infodialogbody-windows-native';out.mkdir(exist_ok=True)
 for filename in ['infodialogbody-native.json','infodialogbody-lifecycle.json']:
  value=ps(base+"Get-Content -Raw ($app+'\\"+filename+"')",retry_read=True);json.loads(value);(out/filename).write_text(value+'\n')
 for filename in ['infodialogbody-compact-ko-570-permissions.png','infodialogbody-wide-en-570-research.png']:
  png=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes(($app+'\\"+filename+"')))",retry_read=True);(out/filename).write_bytes(base64.b64decode(png))
 print('Fresh native32+lifecycle')
