"""Owned ActionButton fixture only. Official CLI + read-only/scoped UIA patterns."""
import argparse,json,subprocess,time,uuid
from pathlib import Path
from driver import ps,q,FIND,APP_FOLDER,CLI,VM,encoded_script
ROOT=Path(__file__).resolve().parents[2]
common="$probeRoot=Join-Path $env:TEMP "+q(APP_FOLDER)+"; $probeApp=Join-Path $probeRoot 'app';"
source="""$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$window=@($wins|Where-Object{$_.Current.Name -like 'ActionButton · original isolation*' -and (Get-Process -Id $_.Current.ProcessId).ProcessName -eq 'msedge'});if($window.Count -ne 1){throw 'Expected unique owned source fixture'};$window=$window[0];"""
p=argparse.ArgumentParser();p.add_argument('action',choices=['open-source','reload-source','record-source','record-states','layout','launch','invoke','read-click','key-primary','invoke-disabled','capture-native','capture-source','launch-busy','read-busy','close-busy','close']);p.add_argument('--state',choices=['initial','clicked','reduced'],default='initial');p.add_argument('--key',choices=['space','m']);a=p.parse_args()
expected='Replica ActionButton verification'+(' · '+a.state if a.state!='initial' else '')
owned=common+'$expected='+q(expected)+';'+FIND
if a.action=='open-source':
 print(ps("Start-Process 'msedge.exe' -ArgumentList '--new-window http://10.211.55.2:8768/action-button.html';[Console]::WriteLine('Owned ActionButton source requested')"))
elif a.action=='reload-source':
 print(ps(source+"$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'새로 고침');$button=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);if($null -eq $button){throw 'Observed source refresh button missing'};$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned source refreshed')"))
elif a.action in ('record-source','record-states'):
 name='Record ActionButton measurements' if a.action=='record-source' else 'Record ActionButton states'
 print(ps(source+"$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,"+q(name)+");$button=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);if($null -eq $button){throw 'Owned record button missing'};$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Source record invoked')"))
elif a.action=='layout':
 print(ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--action-buttons',($probeApp+'\\action-button-cases.json'),$probeApp) -WorkingDirectory $probeApp -PassThru -Wait;if($p.ExitCode -ne 0){throw 'Fixture failed'};[IO.File]::ReadAllText($probeApp+'\\action-button-native-layout.json')",timeout=90))
elif a.action=='launch':
 print(ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--action-window',($probeApp+'\\action-button-cases.json'),$probeApp) -WorkingDirectory $probeApp -PassThru;[Console]::WriteLine($p.Id)"))
elif a.action=='invoke':
 print(ps(owned+"$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ActionButton-ko-primary-normal');$button=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);if($null -eq $button){throw 'Owned primary button missing'};$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();$deadline=[DateTime]::UtcNow.AddSeconds(5);do{if($window.Current.Name -eq 'Replica ActionButton verification · clicked'){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $deadline);if($window.Current.Name -ne 'Replica ActionButton verification · clicked'){throw 'Action did not dispatch'};[Console]::WriteLine($window.Current.Name)"))
elif a.action=='invoke-disabled':
 code=owned+"$f=Join-Path $probeApp 'action-button-click.json';$before=(Get-Content $f -Raw|ConvertFrom-Json).clicked;$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ActionButton-ko-primary-disabled');$button=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);if($null -eq $button -or $button.Current.IsEnabled){throw 'Expected disabled owned button'};$refused=$false;$kind='';try{$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()}catch{$refused=$true;$kind=$_.Exception.GetType().FullName};Start-Sleep -Milliseconds 400;$after=(Get-Content $f -Raw|ConvertFrom-Json).clicked;if(!$refused -or $before -ne $after){throw 'Disabled invocation was not safely refused'};ConvertTo-Json -InputObject ([pscustomobject]@{enabled=$button.Current.IsEnabled;refused=$refused;exceptionType=$kind;before=$before;after=$after;title=$window.Current.Name})"
 result=ps(code);(ROOT/'docs/replica/source/windows-action-button-disabled-invoke.json').write_text(result);print(result)
elif a.action=='key-primary':
 if not a.key:p.error('--key required')
 if a.key=='space' and a.state!='initial':p.error('Space verification requires a fresh initial window')
 nonce=str(uuid.uuid4());target='Replica ActionButton verification · '+('clicked' if a.key=='space' else 'reduced')
 focus=owned+"$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ActionButton-ko-primary-normal');$button=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);if($null -eq $button){throw 'Owned primary button missing'};$button.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Wrong keyboard process'};"
 live=focus+"Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps({'nonce':nonce,'expected':expected}))+" | Out-Null;$deadline=[DateTime]::UtcNow.AddSeconds(12);do{if($window.Current.Name -eq "+q(target)+"){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $deadline);if($window.Current.Name -ne "+q(target)+"){throw 'Expected owned state not reached'};[Console]::WriteLine($window.Current.Name);"
 encoded,_=encoded_script(live)
 control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
 while time.monotonic()<deadline:
  if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
  if control.poll() is not None:raise RuntimeError(control.communicate())
  time.sleep(.05)
 else:control.terminate();raise RuntimeError('No owned focus handshake')
 evidence=ROOT/('docs/replica/source/windows-action-button-click.json' if a.key=='space' else 'docs/replica/source/windows-action-button-reduced.json')
 prior=evidence.stat().st_mtime_ns if evidence.exists() else 0
 code=57 if a.key=='space' else 50
 input_path=ROOT/'docs/replica/source/windows-action-button-input-state.json';before=input_path.stat().st_mtime_ns if input_path.exists() else 0
 try:
  subprocess.run([CLI,'send-key-event',VM,'--scancode',str(code),'--event','press'],check=True,capture_output=True,timeout=10)
  if a.key=='space':
   deadline=time.monotonic()+5
   while time.monotonic()<deadline:
    if input_path.exists() and input_path.stat().st_mtime_ns>before:
     data=json.loads(input_path.read_text())
     if data.get('pressed'):
      (ROOT/'docs/replica/source/windows-action-button-key-held.json').write_text(json.dumps(data,ensure_ascii=False,indent=2));break
    time.sleep(.05)
   else:raise RuntimeError('No fresh held-key state')
 finally:subprocess.run([CLI,'send-key-event',VM,'--scancode',str(code),'--event','release'],check=True,capture_output=True,timeout=10)
 out,err=control.communicate(timeout=15)
 if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
 deadline=time.monotonic()+5
 while time.monotonic()<deadline:
  if evidence.exists() and evidence.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh action evidence after key')
 if a.key=='space':
  data=json.loads(evidence.read_text())
  if data.get('clicked')!=1 or data.get('reduced'):raise RuntimeError('Unexpected fresh-window click evidence')
  (ROOT/'docs/replica/source/windows-action-button-final-keyboard-click.json').write_text(evidence.read_text())
 print(out.decode('utf8',errors='replace'))
elif a.action=='read-click':
 print(ps(common+"$expected='Replica ActionButton verification · clicked';"+FIND+"[IO.File]::ReadAllText($probeApp+'\\action-button-click.json')",retry_read=True))
elif a.action in ('capture-native','capture-source'):
 native=a.action=='capture-native';nonce=str(uuid.uuid4())
 select=owned if native else source
 condition="[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ActionButton-ko-quiet-normal')" if native else "[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Record ActionButton measurements')"
 baseCondition="[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ActionButton-ko-base-normal')" if native else "[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'두 연결 확인')"
 code=select+"$f=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,"+condition+");if($null -eq $f){throw 'Capture focus missing'};$f.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Capture focus mismatch'};$b=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,"+baseCondition+");if($null -eq $b){throw 'Owned base missing'};$r=$b.Current.BoundingRectangle;$d=ConvertTo-Json -InputObject ([pscustomobject]@{nonce="+q(nonce)+";title=$window.Current.Name;x=$r.X;y=$r.Y;width=$r.Width;height=$r.Height});Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($d))|Out-Null;Start-Sleep -Seconds 8;"
 encoded,_=encoded_script(code);print('Scoped capture argument length:',len(encoded),flush=True);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
 while time.monotonic()<deadline:
  if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
  if control.poll() is not None:raise RuntimeError(control.communicate())
  time.sleep(.05)
 else:control.terminate();raise RuntimeError('No owned capture handshake')
 tag='native' if native else 'source';metadata=ROOT/f'docs/replica/source/windows-action-button-{tag}-capture.json';metadata.write_text(ready.read_text())
 target=ROOT/f'artifacts/replica/windows-action-button-{tag}-full.png'
 subprocess.run([CLI,'capture',VM,'--file',str(target)],capture_output=True,check=True,timeout=20)
 out,err=control.communicate(timeout=15)
 if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
 print('Owned '+tag+' capture and rectangle evidence saved')
elif a.action=='launch-busy':
 print(ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--action-busy-window',($probeApp+'\\action-button-cases.json'),$probeApp) -WorkingDirectory $probeApp -PassThru;[Console]::WriteLine($p.Id)"))
elif a.action in ('read-busy','close-busy'):
 scoped=common+"$expected='Replica ActionButton busy verification · completed';"+FIND
 if a.action=='read-busy':print(ps(scoped+"$f=$probeApp+'\\action-button-busy-window.json';if((Get-Item $f).LastWriteTimeUtc -lt (Get-Process -Id $window.Current.ProcessId).StartTime.ToUniversalTime()){throw 'Stale busy evidence'};[IO.File]::ReadAllText($f)",retry_read=True))
 else:print(ps(scoped+"$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned busy window closed')"))
elif a.action=='close':
 print(ps(owned+"$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned ActionButton window closed')"))
