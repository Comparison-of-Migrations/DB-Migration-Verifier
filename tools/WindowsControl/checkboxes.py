"""Only the owned Checkbox source fixture and published verification executable."""
import argparse,base64,json,subprocess,time,uuid
from pathlib import Path
from driver import ps,q,FIND,APP_FOLDER,CLI,VM,encoded_script
ROOT=Path(__file__).resolve().parents[2]
common="$probeRoot=Join-Path $env:TEMP "+q(APP_FOLDER)+";$probeApp=Join-Path $probeRoot 'app';"
source="$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$window=@($wins|Where-Object{$_.Current.Name -like 'Checkbox · original isolation*' -and (Get-Process -Id $_.Current.ProcessId).ProcessName -eq 'msedge'});if($window.Count -ne 1){throw 'Expected owned Checkbox source'};$window=$window[0];"
p=argparse.ArgumentParser();p.add_argument('action',choices=['open-source','record','render','capture','launch','read-window','close','key','toggle','disabled']);p.add_argument('--count',type=int,default=0);p.add_argument('--target',type=int,choices=range(5),default=2);p.add_argument('--key',choices=['space','enter','tab'],default='space');a=p.parse_args()
if a.action=='open-source':
 print(ps("Start-Process 'msedge.exe' -ArgumentList '--new-window http://10.211.55.2:8768/checkbox.html';[Console]::WriteLine('Owned Checkbox source requested')"))
elif a.action=='record':
 print(ps(source+"$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Windows checkbox record');$b=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);if($null -eq $b){throw 'Owned recorder missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Checkbox record invoked')"))
elif a.action=='render':
 result=ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--checkbox',($probeApp+'\\checkbox-cases.json'),$probeApp) -WorkingDirectory $probeApp -PassThru -Wait;if($p.ExitCode -ne 0){throw 'Checkbox render failed'};[IO.File]::ReadAllText($probeApp+'\\checkbox-native.json')",timeout=90)
 (ROOT/'docs/replica/source/checkbox-windows-native.json').write_text(result);print('Windows native state record received')
 data=ps(common+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($probeApp+'\\checkbox-native-2x.png'))",retry_read=True)
 (ROOT/'docs/replica/screenshots/checkbox-windows-native-2x.png').write_bytes(base64.b64decode(data))
elif a.action=='launch':
 print(ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--checkbox-window',$probeApp) -WorkingDirectory $probeApp -PassThru;[Console]::WriteLine($p.Id)"))
elif a.action in ('read-window','close'):
 owned=common+'$expected='+q('Replica Checkbox verification · input '+str(a.count))+';'+FIND
 if a.action=='read-window':
  data=ps(owned+"[IO.File]::ReadAllText($probeApp+'\\checkbox-input.json')",retry_read=True)
  (ROOT/f'docs/replica/source/checkbox-windows-input-{a.count}.json').write_text(data);print('Fresh owned checkbox input record saved')
 else:print(ps(owned+"$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned Checkbox window closed')"))
elif a.action=='capture':
 nonce=str(uuid.uuid4())
 code=source+"$b=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Checkbox recorded'));if($null -eq $b){throw 'Owned recorded control missing'};$b.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Wrong focus'};$docs=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Document));$docs=@($docs|Where-Object{$_.Current.Name -eq 'Checkbox · original isolation'});if($docs.Count -ne 1){throw 'Expected source document'};$r=$docs[0].Current.BoundingRectangle;$d=ConvertTo-Json -InputObject ([pscustomobject]@{nonce="+q(nonce)+";x=$r.X;y=$r.Y;width=$r.Width;height=$r.Height});Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($d))|Out-Null;Start-Sleep -Seconds 8;"
 encoded,_=encoded_script(code);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
 while time.monotonic()<deadline:
  if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
  if control.poll() is not None:raise RuntimeError(control.communicate())
  time.sleep(.05)
 else:control.terminate();raise RuntimeError('No source focus handshake')
 (ROOT/'docs/replica/source/windows-checkbox-capture.json').write_text(ready.read_text())
 subprocess.run([CLI,'capture',VM,'--file',str(ROOT/'artifacts/replica/windows-checkbox-source-full.png')],check=True,capture_output=True,timeout=20)
 out,err=control.communicate(timeout=15)
 if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
 print('Owned Checkbox source captured')

elif a.action in ('key','toggle','disabled'):
 owned=common+'$expected='+q('Replica Checkbox verification · input '+str(a.count))+';'+FIND
 target="$c=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('ReplicaCheckbox'+str(a.target))+"));if($null -eq $c){throw 'Owned checkbox missing'};"
 before=ps(owned+"[Console]::WriteLine((Get-Item ($probeApp+'\\checkbox-input.json')).LastWriteTimeUtc.Ticks)",retry_read=True)
 if a.action=='disabled':
  if a.target!=3:raise RuntimeError('Disabled check requires owned disabled input')
  data=ps(owned+target+"if($c.Current.IsEnabled){throw 'Expected disabled input'};$refused=$false;try{$c.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()}catch{$refused=$true};if(!$refused){throw 'Disabled not refused'};[Console]::WriteLine('Disabled Toggle refused')")
  print(data)
 else:
  if a.action=='toggle':print(ps(owned+target+"$c.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle();[Console]::WriteLine('Owned Toggle requested')"))
  else:
   nonce=str(uuid.uuid4())
   code=owned+target+"$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Wrong focus'};$d="+q(json.dumps({'nonce':nonce}))+";Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body $d|Out-Null;Start-Sleep -Seconds 6;"
   encoded,_=encoded_script(code);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
   ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
   while time.monotonic()<deadline:
    if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
    if control.poll() is not None:raise RuntimeError(control.communicate())
    time.sleep(.05)
   else:control.terminate();raise RuntimeError('No verified focus; key blocked')
   scancode={'space':57,'enter':28,'tab':15}[a.key]
   try:subprocess.run([CLI,'send-key-event',VM,'--scancode',str(scancode),'--event','press'],check=True,capture_output=True,timeout=10)
   finally:subprocess.run([CLI,'send-key-event',VM,'--scancode',str(scancode),'--event','release'],check=True,capture_output=True,timeout=10)
   out,err=control.communicate(timeout=10)
   if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
  code=common+"$f=$probeApp+'\\checkbox-input.json';$deadline=[DateTime]::UtcNow.AddSeconds(5);do{if((Get-Item $f).LastWriteTimeUtc.Ticks -gt "+before+"){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $deadline);if((Get-Item $f).LastWriteTimeUtc.Ticks -le "+before+"){throw 'No fresh input record'};[IO.File]::ReadAllText($f)"
  data=ps(code,retry_read=True);record=json.loads(data)
  if a.action=='key' and a.key=='enter':assert record['changes']==a.count
  if a.action=='key' and a.key=='space' or a.action=='toggle':assert record['changes']==a.count+1
  (ROOT/f'docs/replica/source/checkbox-windows-{a.action}-{a.key}-{a.count}.json').write_text(data)
  print('Actual Windows input verified:',record['reason'],record['changes'])
