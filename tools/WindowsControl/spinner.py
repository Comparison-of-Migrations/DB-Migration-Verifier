"""Only the owned Spinner source fixture and published verification executable."""
import argparse,base64,json,subprocess,time,uuid
from pathlib import Path
from driver import ps,q,FIND,APP_FOLDER,CLI,VM,encoded_script
ROOT=Path(__file__).resolve().parents[2]
common="$probeRoot=Join-Path $env:TEMP "+q(APP_FOLDER)+";$probeApp=Join-Path $probeRoot 'app';"
source="$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$window=@($wins|Where-Object{$_.Current.Name -like 'Spinner · original isolation*' -and (Get-Process -Id $_.Current.ProcessId).ProcessName -eq 'msedge'});if($window.Count -ne 1){throw 'Expected owned Spinner source'};$window=$window[0];"
p=argparse.ArgumentParser();p.add_argument('action',choices=['open-source','record','render','capture','launch','read-window','close']);a=p.parse_args()
if a.action=='open-source':
 print(ps("Start-Process 'msedge.exe' -ArgumentList '--new-window http://10.211.55.2:8768/spinner.html';[Console]::WriteLine('Owned Spinner source requested')"))
elif a.action=='record':
 print(ps(source+"$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Record Spinner measurements');$b=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);if($null -eq $b){throw 'Owned recorder missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Spinner record invoked')"))
elif a.action=='render':
 print(ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--spinner',($probeApp+'\\spinner-cases.json'),$probeApp) -WorkingDirectory $probeApp -PassThru -Wait;if($p.ExitCode -ne 0){throw 'Spinner render failed'};[IO.File]::ReadAllText($probeApp+'\\spinner-native.json')",timeout=90))
 data=ps(common+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($probeApp+'\\spinner-native-2x.png'))",retry_read=True)
 (ROOT/'docs/replica/screenshots/spinner-windows-native-2x.png').write_bytes(base64.b64decode(data))
elif a.action=='launch':
 print(ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--spinner-window',$probeApp) -WorkingDirectory $probeApp -PassThru;[Console]::WriteLine($p.Id)"))
elif a.action in ('read-window','close'):
 owned=common+"$expected='Replica Spinner verification · completed';"+FIND
 if a.action=='read-window':print(ps(owned+"[IO.File]::ReadAllText($probeApp+'\\spinner-window.json')",retry_read=True))
 else:print(ps(owned+"$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned Spinner window closed')"))
elif a.action=='capture':
 nonce=str(uuid.uuid4())
 code=source+"$b=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Spinner recorded'));if($null -eq $b){throw 'Owned recorded control missing'};$b.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Wrong focus'};$docs=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Document));$docs=@($docs|Where-Object{$_.Current.Name -eq 'Spinner · original isolation'});if($docs.Count -ne 1){throw 'Expected source document'};$r=$docs[0].Current.BoundingRectangle;$d=ConvertTo-Json -InputObject ([pscustomobject]@{nonce="+q(nonce)+";x=$r.X;y=$r.Y;width=$r.Width;height=$r.Height});Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($d))|Out-Null;Start-Sleep -Seconds 8;"
 encoded,_=encoded_script(code);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
 while time.monotonic()<deadline:
  if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
  if control.poll() is not None:raise RuntimeError(control.communicate())
  time.sleep(.05)
 else:control.terminate();raise RuntimeError('No source focus handshake')
 (ROOT/'docs/replica/source/windows-spinner-capture.json').write_text(ready.read_text())
 subprocess.run([CLI,'capture',VM,'--file',str(ROOT/'artifacts/replica/windows-spinner-source-full.png')],check=True,capture_output=True,timeout=20)
 out,err=control.communicate(timeout=15)
 if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
 print('Owned Spinner source captured')
