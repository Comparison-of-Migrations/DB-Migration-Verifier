"""Fixed project SearchInput fixture, official CLI plus Windows UI Automation only."""
import argparse,sys,json,time,subprocess,uuid,base64
from pathlib import Path
from driver import ps,q,encoded_script,CLI,VM
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['prepare','auto','launch','set-value','clear','escape','source-escape','source-record','read','close']);p.add_argument('--target',choices=['tables-ko-380-empty','tables-ko-380-value','tables-ko-380-space','matching-ko-452-value'],default='tables-ko-380-empty');p.add_argument('--value',choices=['orders','archive',' ',''],default='orders');a=p.parse_args()
base="$d=Join-Path $env:TEMP 'DBMV-SearchInput-20261007';$probeApp=Join-Path $d 'app';"
owned=base+"""$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name-eq 'Replica SearchInput verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($probeApp+'\\ReplicaProbe.exe')});if($w.Count-ne 1){throw 'Expected unique owned SearchInput window'};$w=$w[0];"""
source="""$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name.StartsWith('SearchInput · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Expected unique owned SearchInput source'};$w=$w[0];"""
def target(id):return "$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(id)+"));if($null-eq $b){throw 'Owned target missing'};"
if a.action=='prepare':
 print(ps(base+"$ProgressPreference='SilentlyContinue';New-Item -ItemType Directory -Force $d|Out-Null;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($d+'\\check.zip');Expand-Archive ($d+'\\check.zip') $probeApp -Force;Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/searchinput-cases.json' -OutFile ($probeApp+'\\searchinput-cases.json');[Console]::WriteLine('Owned SearchInput fixture prepared');",timeout=180))
elif a.action=='auto':
 print(ps(base+"& ($probeApp+'\\ReplicaProbe.exe') '--searchinput' ($probeApp+'\\searchinput-cases.json') $probeApp;if($LASTEXITCODE-ne 0){throw 'Probe failed'};[Console]::WriteLine((Get-Item ($probeApp+'\\searchinput-native.json')).LastWriteTimeUtc.ToString('o'));",timeout=100))
 out=root/'docs/replica/source/searchinput-windows-native';out.mkdir(exist_ok=True);data=json.loads(ps(base+"Get-Content -Raw ($probeApp+'\\searchinput-native.json')",retry_read=True));(out/'searchinput-native.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');(out/'searchinput-native.png').write_bytes(base64.b64decode(ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($probeApp+'\\searchinput-native.png'))",retry_read=True)));print({'scale':data['scale'],'rows':len(data['rows'])})
elif a.action=='launch':print(ps(base+"Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--searchinput-window',($probeApp+'\\searchinput-cases.json'),$probeApp)|Out-Null;[Console]::WriteLine('Owned interactive SearchInput requested');"))
elif a.action=='set-value':
 print(ps(owned+target('SearchInput-'+a.target)+"$b.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("+q(a.value)+");[Console]::WriteLine($b.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value);"))
elif a.action=='clear':print(ps(owned+target('SearchClear-'+a.target)+"$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned clear invoked');"))
elif a.action in ['escape','source-escape']:
 src=a.action=='source-escape';id=a.target if src else 'SearchInput-'+a.target;nonce=str(uuid.uuid4());ready=root/'docs/replica/source/windows-control-ready.json'
 code=(source if src else owned)+target(id)+"$b.SetFocus();$focus=[System.Windows.Automation.AutomationElement]::FocusedElement;if($focus.Current.ProcessId-ne $w.Current.ProcessId -or $focus.Current.AutomationId-ne $b.Current.AutomationId){throw 'Exact focus mismatch; key blocked'};$ProgressPreference='SilentlyContinue';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps(dict(nonce=nonce,component='SearchInput',target=id)))+"|Out-Null;Start-Sleep -Seconds 2;[Console]::WriteLine($b.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value);"
 encoded,_=encoded_script(code);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE);end=time.monotonic()+10
 while time.monotonic()<end:
  # The evidence server writes this small handshake in place. A transient
  # empty/partial JSON is not focus confirmation and must never send a key.
  try:confirmed=json.loads(ready.read_text()).get('nonce')==nonce
  except (FileNotFoundError,json.JSONDecodeError):confirmed=False
  if confirmed:break
  if control.poll() is not None:raise RuntimeError(control.communicate())
  time.sleep(.05)
 else:control.terminate();control.communicate();raise RuntimeError('No focus handshake; key blocked')
 try:
  subprocess.run([CLI,'send-key-event',VM,'--scancode','1','--event','press'],check=True,capture_output=True,timeout=10);time.sleep(.15)
 finally:subprocess.run([CLI,'send-key-event',VM,'--scancode','1','--event','release'],check=True,capture_output=True,timeout=10)
 out,err=control.communicate(timeout=8)
 if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
 print('Owned Escape delivered; final value:',repr(out.decode('utf8').strip()))
elif a.action=='source-record':
 out=root/'docs/replica/source/windows-source-searchinput.json';prior=out.stat().st_mtime_ns if out.exists() else 0
 print(ps(source+target('searchinput-record')+"$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned source record invoked');"));end=time.monotonic()+8
 while time.monotonic()<end:
  if out.exists() and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source record')
 data=json.loads(out.read_text());(root/'docs/replica/source/searchinput-windows-source-input.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print({'events':data['events']})
elif a.action=='read':
 data=json.loads(ps(base+"Get-Content -Raw ($probeApp+'\\searchinput-input.json')",retry_read=True));(root/'docs/replica/source/searchinput-windows-actual.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print({'events':len(data['events']),'tail':data['events'][-4:]})
elif a.action=='close':print(ps(owned+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned SearchInput closed');"))
