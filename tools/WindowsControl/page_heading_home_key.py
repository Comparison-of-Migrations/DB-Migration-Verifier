"""Owned PageHeading fixture keyboard input via approved CLI and official UI Automation."""
from pathlib import Path
import argparse,json,subprocess,time,uuid
from driver import ps,q,CLI,VM,encoded_script
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['read','key','close']);p.add_argument('--target',choices=['title','focus'],default='title');p.add_argument('--key',choices=['space','enter','tab','left','right','escape','end'],default='right');p.add_argument('--source',action='store_true');p.add_argument('--manifest',type=Path);a=p.parse_args()
base="$d=Join-Path $env:TEMP 'DBMV-PageHeadingHome-20261008';$probeApp=Join-Path $d 'app';"
if a.manifest:
 stage=json.loads((a.manifest.parent/'windows-stage.json').read_text());lease=json.loads((root/'docs/replica/ENVIRONMENT_LEASE.json').read_text());assert lease['resources']['windows']['owner']=='root'
 base="$d=Join-Path $env:TEMP "+q(stage['guestFolder'])+";$probeApp=Join-Path $d 'app';$out=Join-Path $d 'actual-home';"
owned=base+"$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name-eq 'Replica PageHeading verification' -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'ReplicaProbe'});if($w.Count-ne 1){throw 'Unique owned PageHeading input window missing'};$w=$w[0];if((Get-Process -Id $w.Current.ProcessId).Path-ne ($probeApp+'\\ReplicaProbe.exe')){throw 'Wrong process path'};"
if a.source:
 owned="$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name.StartsWith('Home heading · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Unique owned source required'};$w=$w[0];if(!(Get-Process -Id $w.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')){throw 'Wrong browser path'};"
 read="$pre=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('{\"focused\":')});if($pre.Count-ne 1){throw 'Unique source state missing'};[Console]::WriteLine($pre[0].Current.Name);"
else:read="Get-Content -Raw ("+("$out" if a.manifest else "$probeApp")+"+'\\pageheading-input.json')"
if a.action=='read':print(ps(owned+read,retry_read=True))
elif a.action=='close':print(ps(owned+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned PageHeading fixture closed');"))
else:
 nonce=str(uuid.uuid4());code=owned+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(('heading-home-ko-326' if a.source else 'screen-title') if a.target=='title' else 'focus-title')+"));if($null-eq $c){throw 'Owned control missing'};$c.SetFocus();$focus=[System.Windows.Automation.AutomationElement]::FocusedElement;if($focus.Current.ProcessId-ne $w.Current.ProcessId -or $focus.Current.AutomationId-ne $c.Current.AutomationId){throw 'Exact control focus missing'};Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/root-pageheading-windows-control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps({'nonce':nonce}))+"|Out-Null;Start-Sleep -Seconds 3;"
 encoded,_=encoded_script(code);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE);ready=root/'docs/replica/source/root-pageheading-windows-control-ready.json';deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
  if control.poll() is not None:raise RuntimeError(control.communicate())
  time.sleep(.05)
 else:control.terminate();raise RuntimeError('No verified focus')
 value={'space':57,'enter':28,'tab':15,'left':100,'right':102,'escape':1,'end':105}[a.key];option='--key' if a.key in ['left','right','end'] else '--scancode'
 try:subprocess.run([CLI,'send-key-event',VM,option,str(value),'--event','press'],check=True,capture_output=True,timeout=10)
 finally:subprocess.run([CLI,'send-key-event',VM,option,str(value),'--event','release'],check=True,capture_output=True,timeout=10)
 stdout,stderr=control.communicate(timeout=10)
 if control.returncode:raise RuntimeError((stdout+stderr).decode(errors='replace'))
 data=ps(owned+read,retry_read=True);out=(a.manifest.parent/'actual-windows-home-input') if a.manifest else root/'docs/replica/source/pageheadinghome-windows-input';out.mkdir(exist_ok=True);(out/f'{"source-" if a.source else ""}key-{a.key}-{a.target}.json').write_text(data);print(json.loads(data))
