"""Fixed WelcomeStep project probe; title/path guards, approved official CLI/UIA."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','auto']);p.add_argument('--manifest',type=Path);a=p.parse_args()
base=""
if a.action=='auto':
 if a.manifest is None:raise RuntimeError('Own isolated win manifest required')
 m=json.loads(a.manifest.read_text());stage=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='home' and m['component']=='welcomestep' and m['runtime']=='win-x64' and m['buildExitCode']==0 and stage['selfContained'];assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home';base="$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';"

source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('WelcomeStep · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
unique="if($w.Count-ne 1){throw 'Unique owned WelcomeStep required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/welcomestep.html');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/home-welcomestep-windows-source.json';prior=out.stat().st_mtime_ns if out.exists() else 0
 print(ps(source+unique+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'welcomestep-record'));if($null-ne $b){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($null-eq $b){throw 'Source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",retry_read=True))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists() and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh WelcomeStep source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==24;(r/'docs/replica/source/welcomestep-windows-source.json').write_bytes(out.read_bytes());print('Fresh original24')
elif a.action=='auto':
 cases=r/'artifacts/replica/windows-download/welcomestep-cases.json';served='home-welcomestep-'+a.manifest.parent.name+'-cases.json';(r/'artifacts/replica/windows-download'/served).write_bytes(cases.read_bytes());digest=hashlib.sha256(cases.read_bytes()).hexdigest().upper()
 code=base+"$ProgressPreference='SilentlyContinue';$f=Join-Path $app 'welcomestep-cases.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned cases hash mismatch'};if((Get-FileHash ($app+'\\ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(m['binarySha256'].upper())+"){throw 'Owned binary hash mismatch'};Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;"
 code+="$f=Join-Path $app 'welcomestep-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--welcomestep' ($app+'\\welcomestep-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned WelcomeStep failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native WelcomeStep'};"
 print(ps(code,timeout=60));value=ps(base+"Get-Content -Raw ($app+'\\welcomestep-native.json')",retry_read=True);v=json.loads(value);assert len(v['rows'])==24;out=r/'docs/replica/source/welcomestep-windows-native';out.mkdir(exist_ok=True);(out/'welcomestep-native.json').write_text(value+'\n')
 import base64
 for filename in ['welcomestep.png','welcomestep-ko-684.png','welcomestep-en-980.png']:
  png=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes(($app+'\\"+filename+"')))",retry_read=True);(out/filename).write_bytes(base64.b64decode(png))
 proof={'phase':'WINDOWS_OS_RECORDED','headless':False,'manifest':str(a.manifest.resolve()),'stage':stage,'binarySha256':m['binarySha256'],'casesSha256':digest,'output':str(out.resolve()),'rows':24,'lifecycle':v['lifecycle']}
 (r/'docs/replica/source/welcomestep-windows-run.json').write_text(json.dumps(proof,indent=2)+'\n');print('Fresh actualOS native24')
elif a.action=='source-close':print(ps(source+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
