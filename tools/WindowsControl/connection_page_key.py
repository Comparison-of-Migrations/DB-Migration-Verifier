"""Bounded keyboard input only to exact owned ConnectionPage fixture and fixed identities."""
from pathlib import Path
import argparse,json,subprocess,time,uuid,re
from driver import CLI,VM,q,encoded_script
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('target',choices=['native','source']);p.add_argument('--field',choices=['advanced','dbms-0','dbms-1','dbms-2','dbms-3','Footer-connect','Footer-preflight','source-host'],default='advanced');p.add_argument('--key',choices=['space','enter','right','tab'],default='space');p.add_argument('--stage',type=Path);a=p.parse_args()
if json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']!='connection':raise RuntimeError('Connection Windows resource required')
base=''
if a.target=='native':
 if not a.stage:raise RuntimeError('Exact owned isolated stage required')
 stage=json.loads(a.stage.read_text())
 if stage['owner']!='connection'or stage['component']!='connectionpage'or not stage['selfContained']or not re.fullmatch(r'DBMV-Isolated-connection-connectionpage-[0-9a-f]{32}',stage['guestFolder']):raise RuntimeError('Owned full Connection stage required')
 base="$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';"

find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{"+("$_.Current.Name-eq 'Replica ConnectionPage verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')" if a.target=='native'else"$_.Current.Name.StartsWith('ConnectionPage input · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'")+"});if($w.Count-ne 1){throw 'Unique owned ConnectionPage required'};$w=$w[0];"
if a.target=='source'and a.field in ['advanced','Footer-connect','Footer-preflight']:
 condition="[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,"+q({'advanced':'고급 연결 설정','Footer-connect':'두 연결 확인','Footer-preflight':'조회 범위 확인'}[a.field])+")"
else:condition="[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(a.field)+")"
nonce=str(uuid.uuid4());code=find+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,"+condition+");if($null-eq $c){throw 'Owned target missing'};$c.SetFocus();$focus=[System.Windows.Automation.AutomationElement]::FocusedElement;if($focus.Current.ProcessId-ne $w.Current.ProcessId){throw 'Owned focus guard failed'};Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/connection-connectionpage-windows-control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps({'nonce':nonce}))+"|Out-Null;Start-Sleep -Milliseconds 1500;"
encoded,_=encoded_script(code);process=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
ready=r/'docs/replica/source/connection-connectionpage-windows-control-ready.json';deadline=time.monotonic()+8
while time.monotonic()<deadline:
 try:
  if json.loads(ready.read_text()).get('nonce')==nonce:break
 except (FileNotFoundError,json.JSONDecodeError):pass
 if process.poll()is not None:raise RuntimeError(process.communicate())
 time.sleep(.02)
else:process.terminate();process.communicate();raise RuntimeError('No verified owned focus nonce; key blocked')
flag,value=('--key','102')if a.key=='right'else('--scancode',str({'space':57,'enter':28,'tab':15}[a.key]))
try:subprocess.run([CLI,'send-key-event',VM,flag,value,'--event','press'],check=True,capture_output=True,timeout=10)
finally:subprocess.run([CLI,'send-key-event',VM,flag,value,'--event','release'],check=True,capture_output=True,timeout=10)
out,err=process.communicate(timeout=10)
if process.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
print(a.target,a.field,a.key,'press/release delivered after owned focus nonce; inspect fresh state next')
