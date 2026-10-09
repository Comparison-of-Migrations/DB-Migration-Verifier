"""Owned numeric source page, official CLI/UIA only; fixed synthetic inputs."""
import argparse,json,time,subprocess,uuid
from pathlib import Path
from driver import ps,q,CLI,VM,encoded_script
ROOT=Path(__file__).resolve().parents[2]
FIND="""$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('NumberInput · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName -eq 'msedge'});if($w.Count -ne 1){throw 'Expected unique owned numeric source'};$w=$w[0];"""
p=argparse.ArgumentParser();p.add_argument('action',choices=['open','inspect','record','key']);p.add_argument('--key',choices=['up','down','tab'],default='up');p.add_argument('--capture',action='store_true');p.add_argument('--value',choices=['3306','','1','65535','0','65536','1.5','1e3','0001','-1','1e+3','1e-3'],default='3306');a=p.parse_args()
if a.action=='open':
 print(ps("Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/numberinput.html');[Console]::WriteLine('Owned numeric source requested')"));raise SystemExit
if a.action=='inspect':
 code=FIND+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'number-default'));$items=@($c)+@($c.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition));$items|ForEach-Object{[pscustomobject]@{name=$_.Current.Name;id=$_.Current.AutomationId;type=$_.Current.ControlType.ProgrammaticName;patterns=@($_.GetSupportedPatterns()|ForEach-Object{$_.ProgrammaticName});rect=$_.Current.BoundingRectangle}}|ConvertTo-Json -Depth 5;"
 print(ps(code,retry_read=True));raise SystemExit
if a.action=='record':
 print(ps(FIND+"$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'record')).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned numeric record invoked')"));raise SystemExit
# The shared recorder can last contain a Mac observation. Establish a fresh
# record from this exact Edge window before using its event length as a guard.
data=ROOT/'docs/replica/source/windows-source-numberinput.json'
old_stamp=data.stat().st_mtime_ns if data.exists() else 0
ps(FIND+"$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'record')).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();")
deadline=time.monotonic()+5
while time.monotonic()<deadline:
 if data.exists() and data.stat().st_mtime_ns>old_stamp:break
 time.sleep(.05)
else:raise RuntimeError('No fresh owned Windows source record')
before=json.loads(data.read_text());nonce=str(uuid.uuid4());code=FIND+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'number-default'));$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("+q(a.value)+");$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $w.Current.ProcessId){throw 'Wrong focus'};Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps({'nonce':nonce}))+"|Out-Null;Start-Sleep -Seconds 8;"
encoded,_=encoded_script(code);process=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
while time.monotonic()<deadline:
 if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
 if process.poll() is not None:raise RuntimeError(process.communicate())
 time.sleep(.05)
else:process.terminate();raise RuntimeError('Owned numeric focus handshake missing; no key sent')
option='--scancode' if a.key=='tab' else '--key';value={'up':98,'down':104,'tab':15}[a.key]
for event in ('press','release'):subprocess.run([CLI,'send-key-event',VM,option,str(value),'--event',event],check=True,capture_output=True,timeout=10)
if a.capture:
 subprocess.run([CLI,'capture',VM,'--file',str(ROOT/'artifacts/replica/numberinput-windows-source-focused-full.png')],check=True,capture_output=True,timeout=20)
out,err=process.communicate(timeout=12)
if process.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
data=ROOT/'docs/replica/source/windows-source-numberinput.json';d=json.loads(data.read_text());r=d['rows'][0]
assert any(e['event']=='keyup' and e['key']=={'up':'ArrowUp','down':'ArrowDown','tab':'Tab'}[a.key] for e in d['events'][len(before['events']):])
path=ROOT/('docs/replica/source/numberinput-windows-step-'+(a.value.replace('-','minus').replace('.','dot').replace('+','plus') or 'empty')+'-'+a.key+'.json');path.write_text(data.read_text());print(r)
