"""Owned NumberInput native window; official CLI + Windows UI Automation only."""
import argparse,json,subprocess,time,uuid
from pathlib import Path
from driver import ps,q,CLI,VM,APP_FOLDER,FIND,encoded_script
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=['open','inspect','key','close']);p.add_argument('--key',choices=['up','down','tab'],default='up');p.add_argument('--value',choices=['3306','','1','65535','0','65536','1.5','1e3','0001','-1','1e+3','1e-3'],default='3306');a=p.parse_args()
common="$probeApp=Join-Path (Join-Path $env:TEMP "+q(APP_FOLDER)+") 'app';$expected='Replica NumberInput verification';"
if a.action=='open':
 print(ps(common+"Start-Process -FilePath ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--number-window',$probeApp) -WorkingDirectory $probeApp;"));raise SystemExit
find=common+FIND
field="$c=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'number-default'));if($null -eq $c){throw 'Owned number field missing'};"
if a.action=='close':
 print(ps(find+"$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"));raise SystemExit
if a.action=='inspect':
 print(ps(find+field+"[pscustomobject]@{type=$c.Current.ControlType.ProgrammaticName;patterns=@($c.GetSupportedPatterns()|ForEach-Object{$_.ProgrammaticName});rect=$c.Current.BoundingRectangle;value=$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value}|ConvertTo-Json -Depth 5;",retry_read=True));raise SystemExit
key={'up':'Up','down':'Down','tab':'Tab'}[a.key];file='numberinput-keyup-'+key+'.json'
stamp=ps(common+"$f=Join-Path $probeApp "+q(file)+";if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};",retry_read=True)
nonce=str(uuid.uuid4())
code=find+field+"$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("+q(a.value)+");$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Wrong native number focus'};Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps({'nonce':nonce}))+"|Out-Null;$deadline=[DateTime]::UtcNow.AddSeconds(10);do{Start-Sleep -Milliseconds 50;$f=Join-Path $probeApp "+q(file)+"}while((!(Test-Path $f) -or (Get-Item $f).LastWriteTimeUtc.Ticks -le "+str(int(stamp))+") -and [DateTime]::UtcNow -lt $deadline);if(!(Test-Path $f) -or (Get-Item $f).LastWriteTimeUtc.Ticks -le "+str(int(stamp))+"){throw 'No fresh owned numeric key state'};"
encoded,_=encoded_script(code);process=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
while time.monotonic()<deadline:
 if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
 if process.poll() is not None:raise RuntimeError(process.communicate())
 time.sleep(.05)
else:process.terminate();raise RuntimeError('Native numeric focus handshake missing; no key sent')
option='--scancode' if a.key=='tab' else '--key';value={'up':98,'down':104,'tab':15}[a.key]
try:subprocess.run([CLI,'send-key-event',VM,option,str(value),'--event','press'],check=True,capture_output=True,timeout=10)
finally:subprocess.run([CLI,'send-key-event',VM,option,str(value),'--event','release'],check=True,capture_output=True,timeout=10)
out,err=process.communicate(timeout=15)
if process.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
record=ps(common+"Get-Content (Join-Path $probeApp "+q(file)+") -Raw;",retry_read=True)
data=json.loads(record)
assert any(e.get('type')=='keyup' and e.get('key')==key for e in data['events'])
name='numberinput-windows-native-step-'+(a.value.replace('-','minus').replace('.','dot').replace('+','plus') or 'empty')+'-'+a.key+'.json'
(ROOT/'docs/replica/source'/name).write_text(record);print(data['rows'][0])
