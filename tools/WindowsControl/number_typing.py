"""One bounded, trusted 1/e keyboard sequence in the owned numeric fixture."""
import argparse,json,subprocess,time,uuid
from pathlib import Path
from driver import ps,q,FIND,APP_FOLDER,CLI,VM,encoded_script
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('target',choices=['source','native']);a=p.parse_args()
common="$probeApp=Join-Path (Join-Path $env:TEMP "+q(APP_FOLDER)+") 'app';"
find=("$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('NumberInput · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName -eq 'msedge'});if($w.Count -ne 1){throw 'Expected one owned numeric source'};$window=$w[0];" if a.target=='source' else common+"$expected='Replica NumberInput verification';"+FIND)
field="$c=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'number-default'));if($null -eq $c){throw 'Owned numeric field missing'};"
nonce=str(uuid.uuid4())
code=find+field+"$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('');$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Wrong keyboard focus'};Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps({'nonce':nonce}))+"|Out-Null;Start-Sleep -Seconds 6;[pscustomobject]@{raw=$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value;type=$c.Current.ControlType.ProgrammaticName;focused=$c.Current.HasKeyboardFocus}|ConvertTo-Json;"
encoded,_=encoded_script(code);process=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
while time.monotonic()<deadline:
 if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
 if process.poll() is not None:raise RuntimeError(process.communicate())
 time.sleep(.05)
else:process.terminate();raise RuntimeError('No verified owned keyboard target')
for scan in (2,18):
 try:subprocess.run([CLI,'send-key-event',VM,'--scancode',str(scan),'--event','press'],check=True,capture_output=True,timeout=10)
 finally:subprocess.run([CLI,'send-key-event',VM,'--scancode',str(scan),'--event','release'],check=True,capture_output=True,timeout=10)
out,err=process.communicate(timeout=10)
if process.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
peer=json.loads(out.decode('utf-8-sig'))
if a.target=='source':
 data=json.loads((ROOT/'docs/replica/source/windows-source-numberinput.json').read_text())
 assert data['events'][-1]['event']=='keyup' and data['events'][-1]['key']=='e'
else:data=json.loads(ps(common+"Get-Content ($probeApp+'\\numberinput-keyup-E.json') -Raw;",retry_read=True))
assert peer['focused'],(peer,data['rows'][0])
row=data['rows'][0];assert row['value']=='' and row['badInput']
(ROOT/'docs/replica/source'/('numberinput-windows-'+a.target+'-incomplete.json')).write_text(json.dumps({'peer':peer,'data':data},ensure_ascii=False,indent=2)+'\n')
print(a.target,peer,row)
