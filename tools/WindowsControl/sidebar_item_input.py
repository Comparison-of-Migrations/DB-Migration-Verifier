"""User-authorized official CLI/UIA for the fixed, owned SidebarItem fixture."""
from pathlib import Path
import argparse,json,subprocess,time,uuid
from driver import ps,q,encoded_script,CLI,VM
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['prepare-launch','invoke','disabled','key','close','read']);p.add_argument('--target',default='ko-normal-0-idle',choices=['ko-normal-0-idle','ko-active-0-idle','en-done-0-idle']);p.add_argument('--key',choices=['enter','space'],default='space');p.add_argument('--goal',type=int);a=p.parse_args()
base="$d=Join-Path $env:TEMP 'DBMV-SidebarItem-20261007';$probeApp=Join-Path $d 'app';"
owned=base+"""$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name-eq 'Replica SidebarItem verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($probeApp+'\\ReplicaProbe.exe')});if($w.Count-ne 1){throw 'Expected unique owned SidebarItem window'};$w=$w[0];"""
def target(id):return "$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('SidebarItem-'+id)+"));if($null-eq $b){throw 'Owned button missing'};"
if a.action=='prepare-launch':
 print(ps(base+"$ProgressPreference='SilentlyContinue';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($d+'\\check.zip');Expand-Archive ($d+'\\check.zip') $probeApp -Force;Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--sidebaritem-window',($probeApp+'\\sidebaritem-cases.json'),$probeApp)|Out-Null;[Console]::WriteLine('Owned interactive fixture requested');",timeout=180))
elif a.action=='close':print(ps(owned+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned fixture closed');"))
elif a.action=='invoke':
 print(ps(owned+target(a.target)+"$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned button invoked');"))
elif a.action=='disabled':
 data=ps(owned+target('ko-normal-1-idle')+"$before=(Get-Content -Raw ($probeApp+'\\sidebaritem-input.json')|ConvertFrom-Json).clicks;$refused=$false;try{$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()}catch{$refused=$true};$after=(Get-Content -Raw ($probeApp+'\\sidebaritem-input.json')|ConvertFrom-Json).clicks;if(!$refused -or $before-ne $after){throw 'Disabled input not refused'};[pscustomobject]@{disabled=$true;refused=$refused;before=$before;after=$after}|ConvertTo-Json;")
 (root/'docs/replica/source/sidebaritem-windows-disabled.json').write_text(data+'\n');print(data)
elif a.action=='read':
 data=json.loads(ps(base+"Get-Content -Raw ($probeApp+'\\sidebaritem-input.json')",retry_read=True));(root/'docs/replica/source/sidebaritem-windows-actual.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print({'clicks':data['clicks'],'events':len(data['events'])})
elif a.action=='key':
 if a.goal is None:p.error('--goal required')
 nonce=str(uuid.uuid4());ready=root/'docs/replica/source/windows-control-ready.json'
 code=owned+target(a.target)+"$initial=(Get-Content -Raw ($probeApp+'\\sidebaritem-input.json')|ConvertFrom-Json).clicks;if($initial-ne "+str(a.goal-1)+"){throw 'Unexpected previous click count; key blocked'};$b.SetFocus();$focus=[System.Windows.Automation.AutomationElement]::FocusedElement;if($focus.Current.ProcessId-ne $w.Current.ProcessId -or $focus.Current.AutomationId-ne $b.Current.AutomationId){throw 'Focus mismatch'};Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps(dict(nonce=nonce,component='SidebarItem',target=a.target)))+"|Out-Null;$deadline=[DateTime]::UtcNow.AddSeconds(10);do{$count=(Get-Content -Raw ($probeApp+'\\sidebaritem-input.json')|ConvertFrom-Json).clicks;if($count-eq "+str(a.goal)+"){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($count-ne "+str(a.goal)+"){throw 'Expected fresh click not reached'};[Console]::WriteLine($count);"
 encoded,_=encoded_script(code);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 deadline=time.monotonic()+10
 while time.monotonic()<deadline:
  try:confirmed=json.loads(ready.read_text()).get('nonce')==nonce
  except (FileNotFoundError,json.JSONDecodeError):confirmed=False
  if confirmed:break
  if control.poll() is not None:raise RuntimeError(control.communicate())
  time.sleep(.05)
 else:control.terminate();control.communicate();raise RuntimeError('No owned focus handshake; no key sent')
 scan=28 if a.key=='enter' else 57
 try:
  subprocess.run([CLI,'send-key-event',VM,'--scancode',str(scan),'--event','press'],capture_output=True,check=True,timeout=10)
  time.sleep(.15)
 finally:subprocess.run([CLI,'send-key-event',VM,'--scancode',str(scan),'--event','release'],capture_output=True,check=True,timeout=10)
 out,err=control.communicate(timeout=15)
 if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
 print('Owned key verified:',a.key,out.decode('utf8').strip())
