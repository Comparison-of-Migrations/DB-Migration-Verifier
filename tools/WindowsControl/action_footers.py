"""Approved fixed-scope ActionFooter fixture/UIA; no account/OS/security changes."""
from pathlib import Path
import argparse,json,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','prepare','auto','launch','invoke','read','close','source-action']);p.add_argument('--width',type=int,choices=[1000,1138,1280],default=1280);p.add_argument('--id');p.add_argument('--label');a=p.parse_args()
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('ActionFooter · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-ActionFooter-20261007') 'app';"
if a.action=='source-open':
 print(ps(source+"if($w.Count){throw 'Owned footer source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/actionfooter.html');[Console]::WriteLine('Owned footer source requested');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
 code=source+"if($w.Count-ne 1){throw 'Unique owned footer source required'};$w=$w[0];$window=$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern);$window.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Normal);Start-Sleep -Milliseconds 150;$docs=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Document))|Where-Object{$_.Current.Name-eq 'ActionFooter · original isolation'});if($docs.Count-ne 1){throw 'Owned footer document not unique'};$transform=$w.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern);if(!$transform.Current.CanResize){throw 'Owned source cannot resize'};$extra=$w.Current.BoundingRectangle.Width-$docs[0].Current.BoundingRectangle.Width;$height=$w.Current.BoundingRectangle.Height;$transform.Resize("+str(a.width*2)+"+$extra,$height);Start-Sleep -Milliseconds 200;$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'foot-record'));if($null-eq $b){throw 'Owned footer record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned footer recorded');"
 print(ps(code));deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh footer source')
 v=json.loads(out.read_text());assert v['dpr']==2 and v['viewport']==a.width and len(v['rows'])==44,(v['dpr'],v['viewport'],len(v['rows']))
 (r/f'docs/replica/source/actionfooter-windows-source-{a.width}.json').write_bytes(out.read_bytes());print('Fresh footer source',a.width,44)
elif a.action=='source-close':print(ps(source+"if($w.Count-ne 1){throw 'Unique owned footer source required'};$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='prepare':
 code=base+"$old=Join-Path (Join-Path $env:TEMP 'DBMV-ScrollViewport-20261007') 'app';foreach($f in @($app,$old)){if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($f+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'}};if(!(Test-Path $app)){if(!(Test-Path ($old+'\\ReplicaProbe.exe'))){throw 'Known runtime missing'};Copy-Item $old $app -Recurse};$ProgressPreference='SilentlyContinue';"
 for name in ['ReplicaProbe.dll','Verifier.Desktop.dll','actionfooter-native-cases.json']:
  file=r/('artifacts/replica/windows-download' if name.endswith('.json')else'artifacts/replica/windows-arm64-check')/name
  served='footer-update-'+name;(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper()
  code+="$f=Join-Path $app "+q(name)+";$next=$f+'.update';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $next;if((Get-FileHash $next -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned update hash mismatch'};if(Test-Path $f){Copy-Item $f ($f+'.previous') -Force};Move-Item $next $f -Force;"
 print(ps(code+"[Console]::WriteLine('Owned footer fixture prepared');"))
elif a.action in ['launch','invoke','read','close','source-action']:
 find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica ActionFooter verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
 if a.action=='launch':
  print(ps(find+"if($w.Count){throw 'Owned footer already open'};$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--actionfooter-window',($app+'\\actionfooter-native-cases.json'),$app) -PassThru;[Console]::WriteLine($p.Id);"))
 elif a.action=='read':
  value=ps(base+"Get-Content -Raw ($app+'\\actionfooter-input.json')",retry_read=True);data=json.loads(value)
  out=r/'docs/replica/source/actionfooter-windows-input';out.mkdir(exist_ok=True);(out/((a.label or 'current')+'.json')).write_text(value+'\n');print(data)
 else:
  if a.action=='source-action':find=source
  code=find+"if($w.Count-ne 1){throw 'Unique owned footer required'};$w=$w[0];"
  if a.action=='close':print(ps(code+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
  else:
   allowed={'FooterCase-connect','FooterCase-connect-busy','FooterCase-select-none','FooterCase-trial-ready','FooterLocale','FooterRecord','Footer-connect','Footer-back','Footer-trial-back','Footer-review','foot-connect','foot-busy','foot-none','foot-ready','foot-locale','foot-record','Reload'}
   if a.id not in allowed:raise RuntimeError('Fixed footer target required')
   condition= "[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(a.id)+")"
   if a.id=='Footer-trial-back':condition="[System.Windows.Automation.AndCondition]::new("+condition+",[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,"+q('Ready · Return' if a.label=='en' else '준비 확인 · 돌아가기')+"))"
   if a.action=='source-action' and a.id=='Footer-connect':condition="[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'두 연결 확인')"
   if a.action=='source-action' and a.id=='Footer-trial-back':condition="[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'준비 확인 · 돌아가기')"
   if a.id=='Reload':condition="[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'새로 고침')"
   code+="$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,"+condition+");if($null-eq $b){throw 'Owned footer button missing'};if(!$b.Current.IsEnabled){[Console]::WriteLine('Disabled: '+$b.Current.Name)}else{$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Invoked: '+$b.Current.Name)};"
   print(ps(code))
else:
 code=base+"$f=Join-Path $app 'actionfooter-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--actionfooter' ($app+'\\actionfooter-native-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned footer candidate failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh footer candidate'};"
 print(ps(code,timeout=60));v=ps(base+"Get-Content -Raw ($app+'\\actionfooter-native.json')",retry_read=True)
 out=r/'docs/replica/source/actionfooter-windows-native';out.mkdir(exist_ok=True);(out/'actionfooter-native.json').write_text(v+'\n');print('Fresh native cases',len(json.loads(v)['rows']))
