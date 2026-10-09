"""Only the owned Radio source fixture and published verification executable."""
import argparse,base64,json,subprocess,time,uuid
from pathlib import Path
from driver import ps,q,FIND,APP_FOLDER,CLI,VM,encoded_script
ROOT=Path(__file__).resolve().parents[2]
common="$probeRoot=Join-Path $env:TEMP "+q(APP_FOLDER)+";$probeApp=Join-Path $probeRoot 'app';"
source="$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$window=@($wins|Where-Object{$_.Current.Name -like 'Radio · original isolation*' -and (Get-Process -Id $_.Current.ProcessId).ProcessName -eq 'msedge'});if($window.Count -ne 1){throw 'Expected owned Radio source'};$window=$window[0];"
p=argparse.ArgumentParser();p.add_argument('action',choices=['open-source','record','render','capture','launch','read-window','close','key','toggle','disabled']);p.add_argument('--count',type=int,default=0);p.add_argument('--target',type=int,choices=range(5),default=2);p.add_argument('--key',choices=['space','enter','tab','down','up','left','right'],default='space');p.add_argument('--next',type=int);a=p.parse_args()
if a.action=='open-source':
 print(ps("Start-Process 'msedge.exe' -ArgumentList '--new-window http://10.211.55.2:8768/radio.html';[Console]::WriteLine('Owned Radio source requested')"))
elif a.action=='record':
 print(ps(source+"$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Windows radio record');$b=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);if($null -eq $b){throw 'Owned recorder missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Radio record invoked')"))
elif a.action=='render':
 result=ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--radio',($probeApp+'\\radio-cases.json'),$probeApp) -WorkingDirectory $probeApp -PassThru -Wait;if($p.ExitCode -ne 0){throw 'Radio render failed'};[IO.File]::ReadAllText($probeApp+'\\radio-native.json')",timeout=90)
 (ROOT/'docs/replica/source/radio-windows-native.json').write_text(result);print('Windows native state record received')
 data=ps(common+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($probeApp+'\\radio-native-2x.png'))",retry_read=True)
 (ROOT/'docs/replica/screenshots/radio-windows-native-2x.png').write_bytes(base64.b64decode(data))
elif a.action=='launch':
 print(ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--radio-window',$probeApp) -WorkingDirectory $probeApp -PassThru;[Console]::WriteLine($p.Id)"))
elif a.action in ('read-window','close'):
 owned=common+'$expected='+q('Replica Radio verification · input '+str(a.count))+';'+FIND
 if a.action=='read-window':
  data=ps(owned+"[IO.File]::ReadAllText($probeApp+'\\radio-input.json')",retry_read=True)
  (ROOT/f'docs/replica/source/radio-windows-input-{a.count}.json').write_text(data);print('Fresh owned radio input record saved')
 else:print(ps(owned+"$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned Radio window closed')"))
elif a.action=='capture':
 nonce=str(uuid.uuid4())
 code=source+"$b=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Radio recorded'));if($null -eq $b){throw 'Owned recorded control missing'};$b.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Wrong focus'};$docs=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Document));$docs=@($docs|Where-Object{$_.Current.Name -eq 'Radio · original isolation'});if($docs.Count -ne 1){throw 'Expected source document'};$r=$docs[0].Current.BoundingRectangle;$d=ConvertTo-Json -InputObject ([pscustomobject]@{nonce="+q(nonce)+";x=$r.X;y=$r.Y;width=$r.Width;height=$r.Height});Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($d))|Out-Null;Start-Sleep -Seconds 8;"
 encoded,_=encoded_script(code);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
 while time.monotonic()<deadline:
  if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
  if control.poll() is not None:raise RuntimeError(control.communicate())
  time.sleep(.05)
 else:control.terminate();raise RuntimeError('No source focus handshake')
 (ROOT/'docs/replica/source/windows-radio-capture.json').write_text(ready.read_text())
 subprocess.run([CLI,'capture',VM,'--file',str(ROOT/'artifacts/replica/windows-radio-source-full.png')],check=True,capture_output=True,timeout=20)
 out,err=control.communicate(timeout=15)
 if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
 print('Owned Radio source captured')

elif a.action in ('key','toggle','disabled'):
 owned=common+'$expected='+q('Replica Radio verification · input '+str(a.count))+';'+FIND
 target="$c=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('ReplicaRadio'+str(a.target))+"));if($null -eq $c){throw 'Owned radio missing'};"
 before=ps(owned+"[Console]::WriteLine((Get-Item ($probeApp+'\\radio-input.json')).LastWriteTimeUtc.Ticks)",retry_read=True)
 if a.action=='disabled':
  if a.target not in (1,2):raise RuntimeError('Disabled check requires owned disabled input')
  data=ps(owned+target+"if($c.Current.IsEnabled){throw 'Expected disabled input'};$refused=$false;try{$c.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()}catch{$refused=$true};if(!$refused){throw 'Disabled not refused'};[Console]::WriteLine('Disabled Selection refused')")
  print(data)
 else:
  if a.action=='toggle':print(ps(owned+target+"$c.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select();[Console]::WriteLine('Owned Selection requested')"))
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
   scancode={'space':57,'enter':28,'tab':15,'down':104,'up':98,'left':100,'right':102}[a.key]
   option='--key' if a.key in ('down','up','left','right') else '--scancode'
   try:subprocess.run([CLI,'send-key-event',VM,option,str(scancode),'--event','press'],check=True,capture_output=True,timeout=10)
   finally:subprocess.run([CLI,'send-key-event',VM,option,str(scancode),'--event','release'],check=True,capture_output=True,timeout=10)
   out,err=control.communicate(timeout=10)
   if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
  code=common+"$f=$probeApp+'\\radio-input.json';$deadline=[DateTime]::UtcNow.AddSeconds(5);do{if((Get-Item $f).LastWriteTimeUtc.Ticks -gt "+before+"){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $deadline);if((Get-Item $f).LastWriteTimeUtc.Ticks -le "+before+"){throw 'No fresh input record'};[IO.File]::ReadAllText($f)"
  data=ps(code,retry_read=True);record=json.loads(data)
  if a.next is not None:assert record['changes']==a.next
  if a.action=='key' and a.key=='enter':assert record['changes']==a.count
  # Different target controls can receive the same key at the same change count.
  (ROOT/f'docs/replica/source/radio-windows-{a.action}-{a.key}-{a.count}-target-{a.target}.json').write_text(data)
  print('Actual Windows input verified:',record['reason'],record['changes'])
