"""Capture only the owned source TextInput document; no arbitrary window target."""
import argparse,json,subprocess,time,uuid
from pathlib import Path
from driver import CLI,VM,encoded_script,q,FIND,APP_FOLDER
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--select',action='store_true');p.add_argument('--refresh-record',action='store_true');p.add_argument('--native',action='store_true');p.add_argument('--long',action='store_true');p.add_argument('--tab',action='store_true');a=p.parse_args();nonce=str(uuid.uuid4())
code="$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($wins|Where-Object{$_.Current.Name.StartsWith('TextInput · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName -eq 'msedge'});if($w.Count -ne 1){throw 'Expected unique owned source'};$w=$w[0];$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'"+('search-placeholder' if a.tab else 'field-long' if a.long else 'field-spaces' if a.select else 'record')+"'));if($null -eq $c){throw 'Source target missing'};$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $w.Current.ProcessId){throw 'Wrong focus'};$d=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'RootWebArea'));$r=$d.Current.BoundingRectangle;$body=ConvertTo-Json -InputObject ([pscustomobject]@{nonce="+q(nonce)+";x=$r.X;y=$r.Y;width=$r.Width;height=$r.Height});Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($body))|Out-Null;Start-Sleep -Seconds 8;"
if a.native:
 code="$probeRoot=Join-Path $env:TEMP "+q(APP_FOLDER)+";$probeApp=Join-Path $probeRoot 'app';$expected='Replica TextInput verification';"+FIND+"$w=$window;$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ReplicaTextInput0'));$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $w.Current.ProcessId){throw 'Wrong focus'};$r=$w.Current.BoundingRectangle;$body=ConvertTo-Json -InputObject ([pscustomobject]@{nonce="+q(nonce)+";x=$r.X;y=$r.Y;width=$r.Width;height=$r.Height});Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($body))|Out-Null;Start-Sleep -Seconds 8;"
if a.long:
 code=code.replace('$c.SetFocus();',"$c.SetFocus();$range=$c.GetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern).DocumentRange;$range.MoveEndpointByRange([System.Windows.Automation.Text.TextPatternRangeEndpoint]::Start,$range,[System.Windows.Automation.Text.TextPatternRangeEndpoint]::End);$range.Select();")
if a.select and not a.native:code=code.replace('$c.SetFocus();', '$c.SetFocus();$c.GetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern).DocumentRange.Select();')
if a.refresh_record:code+="$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'record')).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"
encoded,_=encoded_script(code);process=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
while time.monotonic()<deadline:
 if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
 if process.poll() is not None:raise RuntimeError(process.communicate())
 time.sleep(.05)
else:process.terminate();raise RuntimeError('No owned source focus handshake')
if a.long:
 for key in (100,102):
  for event in ('press','release'):subprocess.run([CLI,'send-key-event',VM,'--key',str(key),'--event',event],check=True,capture_output=True,timeout=10)
if a.tab:
 for event in ('press','release'):subprocess.run([CLI,'send-key-event',VM,'--scancode','15','--event',event],check=True,capture_output=True,timeout=10)
if a.native and a.select:
 def send(scan,event):subprocess.run([CLI,'send-key-event',VM,'--scancode',str(scan),'--event',event],check=True,capture_output=True,timeout=10)
 try:
  send(29,'press')
  try:send(30,'press')
  finally:send(30,'release')
 finally:send(29,'release')
if a.refresh_record:
 for event in ('press','release'):subprocess.run([CLI,'send-key-event',VM,'--scancode','63','--event',event],check=True,capture_output=True,timeout=10)
name=('textinput-windows-native' if a.native else 'textinput-windows-source')+('-tab' if a.tab else '-long' if a.long else '-selected' if a.select else '')
(ROOT/f'docs/replica/source/{name}-capture.json').write_text(ready.read_text())
subprocess.run([CLI,'capture',VM,'--file',str(ROOT/f'artifacts/replica/{name}-full.png')],check=True,capture_output=True,timeout=20)
out,err=process.communicate(timeout=15)
if process.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
print('Owned source captured:',name)
