"""Fixed WelcomeEntryHint project probe; title/path guards, approved official CLI/UIA."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','auto']);p.add_argument('--manifest',type=Path);a=p.parse_args()
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home'
base=""
if a.action=='auto':
 if a.manifest is None:raise RuntimeError('Own isolated win manifest required')
 m=json.loads(a.manifest.read_text());stage=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='home' and m['component']=='welcomeentryhint' and m['runtime']=='win-x64' and m['buildExitCode']==0 and stage['selfContained'];assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home';base="$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';"

source=r"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('WelcomeEntryHint · original isolation') -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('\Microsoft\Edge\Application\msedge.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned WelcomeEntryHint required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/welcomeentryhint.html');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/home-welcomeentryhint-windows-source.json';prior=out.stat().st_mtime_ns if out.exists() else 0
 print(ps(source+unique+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'welcomeentryhint-record'));if($null-ne $b){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($null-eq $b){throw 'Source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",retry_read=True))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists() and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh WelcomeEntryHint source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==8;(r/'docs/replica/source/welcomeentryhint-windows-source.json').write_bytes(out.read_bytes());print('Fresh original8')
elif a.action=='auto':
 cases=r/'artifacts/replica/windows-download/welcomeentryhint-cases.json';served='home-welcomeentryhint-'+a.manifest.parent.name+'-cases.json';(r/'artifacts/replica/windows-download'/served).write_bytes(cases.read_bytes());digest=hashlib.sha256(cases.read_bytes()).hexdigest().upper()
 code=base+"$ProgressPreference='SilentlyContinue';$f=Join-Path $app 'welcomeentryhint-cases.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned cases hash mismatch'};if((Get-FileHash ($app+'\\ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(m['binarySha256'].upper())+"){throw 'Owned binary hash mismatch'};Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;"
 code+="$f=Join-Path $app 'welcomeentryhint-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};$proc=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--welcomeentryhint-window',($app+'\\welcomeentryhint-cases.json'),$app) -PassThru;$deadline=[DateTime]::UtcNow.AddSeconds(20);do{if((Test-Path $f)-and(Get-Item $f).LastWriteTimeUtc.Ticks-gt$prior){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$deadline);if(!(Test-Path $f)-or(Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native WelcomeEntryHint'};"
 print(ps(code,timeout=60));value=ps(base+"Get-Content -Raw ($app+'\\welcomeentryhint-native.json')",retry_read=True);v=json.loads(value);assert len(v['rows'])==8;out=r/'docs/replica/source/welcomeentryhint-windows-native';out.mkdir(exist_ok=True);(out/'welcomeentryhint-native.json').write_text(value+'\n')
 import base64
 for filename in ['welcomeentryhint.png','welcomeentryhint-ko-438.png','welcomeentryhint-en-438.png']:
  png=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes(($app+'\\"+filename+"')))",retry_read=True);(out/filename).write_bytes(base64.b64decode(png))
 proof={'phase':'WINDOWS_OS_RECORDED','headless':False,'manifest':str(a.manifest.resolve()),'stage':stage,'binarySha256':m['binarySha256'],'casesSha256':digest,'output':str(out.resolve()),'rows':8,'lifecycle':v['lifecycle']}
 (r/'docs/replica/source/welcomeentryhint-windows-run.json').write_text(json.dumps(proof,indent=2)+'\n');print('Fresh actualOS native8')
elif a.action=='source-close':print(ps(source+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))

if a.action=='auto':
 owned=base+"$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name-eq'Replica WelcomeEntryHint verification'-and(Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});if($w.Count-ne1){throw 'Unique owned hint window required'};$w=$w[0];"
 inspect=owned+"$A=[System.Windows.Automation.AutomationElement];$T=[System.Windows.Automation.TreeScope];$toggle=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'WelcomeEntryHint-toggle'));if($null-eq$toggle){throw 'Own language utility missing'};$names=@('저장한 결과 JSON은 DB 연결이나 새 검사 없이 바로 열어 확인할 수 있습니다.','Open a saved result JSON without connecting to a DB or running a new check.','저장한 결과 JSON은 DB 연결이나 새 검사 없이 바로 열어 확인할 수 있습니다.');$i=0;$rows=@(foreach($name in $names){$text=$w.FindAll($T::Descendants,[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new($A::ControlTypeProperty,[System.Windows.Automation.ControlType]::Text),[System.Windows.Automation.PropertyCondition]::new($A::NameProperty,$name)));if($text.Count-ne1){throw 'Actual text peer mismatch'};[pscustomobject]@{textPeerCount=$text.Count;name=$text[0].Current.Name;type=$text[0].Current.ControlType.ProgrammaticName};if($i-lt2){$toggle.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep -Milliseconds 200};$i++});ConvertTo-Json -InputObject $rows"

 value=ps(inspect);(r/'docs/replica/source/welcomeentryhint-windows-ax.json').write_text(value+'\n');print(value);print(ps(owned+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
