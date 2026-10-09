"""Owned MatchOption fixture keyboard input via approved CLI and official UI Automation."""
from pathlib import Path
import argparse,json,subprocess,time,uuid
from driver import ps,q,CLI,VM,encoded_script
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['read','key','close']);p.add_argument('--target',type=int,choices=range(4),default=0);p.add_argument('--key',choices=['space','enter','tab','left','right'],default='right');a=p.parse_args()
base="$d=Join-Path $env:TEMP 'DBMV-MatchOption-20261007';$probeApp=Join-Path $d 'app';"
owned=base+"$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name-eq 'Replica MatchOption verification' -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'ReplicaProbe'});if($w.Count-ne 1){throw 'Unique owned MatchOption input window missing'};$w=$w[0];if((Get-Process -Id $w.Current.ProcessId).Path-ne ($probeApp+'\\ReplicaProbe.exe')){throw 'Wrong process path'};"
if a.action=='read':print(ps(owned+"Get-Content -Raw ($probeApp+'\\matchoption-input.json')",retry_read=True))
elif a.action=='close':print(ps(owned+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned MatchOption fixture closed');"))
else:
 nonce=str(uuid.uuid4());code=owned+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('MatchOption-choice-'+str(a.target))+"));if($null-eq $c){throw 'Owned radio missing'};$c.SetFocus();$focus=[System.Windows.Automation.AutomationElement]::FocusedElement;if($focus.Current.ProcessId-ne $w.Current.ProcessId -or $focus.Current.AutomationId-ne $c.Current.AutomationId){throw 'Exact radio focus missing'};Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps({'nonce':nonce}))+"|Out-Null;Start-Sleep -Seconds 3;"
 encoded,_=encoded_script(code);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE);ready=root/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
  if control.poll() is not None:raise RuntimeError(control.communicate())
  time.sleep(.05)
 else:control.terminate();raise RuntimeError('No verified focus')
 value={'space':57,'enter':28,'tab':15,'left':100,'right':102}[a.key];option='--key' if a.key in ['left','right'] else '--scancode'
 try:subprocess.run([CLI,'send-key-event',VM,option,str(value),'--event','press'],check=True,capture_output=True,timeout=10)
 finally:subprocess.run([CLI,'send-key-event',VM,option,str(value),'--event','release'],check=True,capture_output=True,timeout=10)
 stdout,stderr=control.communicate(timeout=10)
 if control.returncode:raise RuntimeError((stdout+stderr).decode(errors='replace'))
 data=ps(owned+"Get-Content -Raw ($probeApp+'\\matchoption-input.json')",retry_read=True);out=root/'docs/replica/source/matchoption-windows-input';out.mkdir(exist_ok=True);(out/f'key-{a.key}-{a.target}.json').write_text(data);print(json.loads(data)['events'])
