"""Exact owned header fixtures via approved CLI/UIA; isolated runtime only."""
from pathlib import Path
import argparse,json,time,hashlib,base64,re
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','input-open','input-record','input-close','input-invoke','cases','auto','launch','read','invoke','close','ax','input-ax']);p.add_argument('--stage',type=Path);p.add_argument('--target',choices=['language','width','mount','record'],default='record');p.add_argument('--label',choices=['initial','passive','language','narrow','detach','remount','restore'],default='initial');a=p.parse_args();base='';v=None
if a.stage:
 v=json.loads(a.stage.read_text());assert v['owner']=='preflight'and v['component']=='capabilityheader'and v['selfContained']and re.fullmatch(r'DBMV-Isolated-preflight-capabilityheader-[0-9a-f]{32}',v['guestFolder']);base="$base=Join-Path $env:TEMP "+q(v['guestFolder'])+";$app=Join-Path $base 'app';"
if a.action in ['cases','auto','launch','read','invoke','close','ax','input-ax']and not a.stage:p.error('Owned header stage required')
unique="if($w.Count-ne 1){throw 'Unique owned CapabilityHeader window required'};$w=$w[0];"
def find(title,source=False):return (''if source else base)+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+") -and "+("(Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'"if source else"(Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')")+"});"
source=find('CapabilityHeader · original isolation',True);inp=find('CapabilityHeader Input · original isolation',True);native=find('Replica CapabilityHeader verification');out=r/'docs/replica/source/capabilityheader-windows-input';out.mkdir(exist_ok=True)
def fresh(action,endpoint,dest):
 file=r/'docs/replica/source'/endpoint;prior=file.stat().st_mtime_ns if file.exists()else 0;print(ps(action));deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if file.exists()and file.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh owned Header evidence')
 dest.write_bytes(file.read_bytes());return json.loads(file.read_text())
if a.action in ['source-open','input-open']:
 f=source if a.action=='source-open'else inp;url='capabilityheader.html'if a.action=='source-open'else'capabilityheader-input.html';print(ps(f+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window',"+q('http://10.211.55.2:8768/'+url)+");"))
elif a.action in ['source-close','input-close','close']:print(ps((source if a.action=='source-close'else inp if a.action=='input-close'else native)+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='source-record':
 data=fresh(source+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'capabilityheader-record'));if($null-eq $b){throw 'Record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",'preflight-capabilityheader-windows-source.json',r/'docs/replica/source/capabilityheader-windows-source.json');assert data['dpr']==2 and len(data['headers'])==12;(r/'docs/replica/source/capabilityheader-windows-cases.json').write_text(json.dumps(data['cases'],ensure_ascii=False,indent=2)+'\n')
elif a.action=='cases':
 file=r/'docs/replica/source/capabilityheader-windows-cases.json';name='preflight-capabilityheader-'+v['guestFolder'].split('-')[-1]+'-cases.json';(r/'artifacts/replica/windows-download'/name).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest().upper();print(ps(base+"Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+name)+" -OutFile ($app+'\\capabilityheader-cases.json');if((Get-FileHash ($app+'\\capabilityheader-cases.json') -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Cases hash mismatch'};"))
elif a.action=='auto':
 print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--capabilityheader' ($app+'\\capabilityheader-cases.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned Header failed'};",timeout=60));dest=r/'docs/replica/source/capabilityheader-windows-native';dest.mkdir(exist_ok=True)
 data=ps(base+"Get-Content -Raw ($app+'\\capabilityheader-native.json')",retry_read=True);assert len(json.loads(data)['headers'])==12;(dest/'capabilityheader-native.json').write_text(data+'\n')
 for name in ['capabilityheader-ko-478.png','capabilityheader-en-904.png']:(dest/name).write_bytes(base64.b64decode(ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes(($app+'\\"+name+"')))",retry_read=True)))
elif a.action=='launch':print(ps(base+"Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--capabilityheader-window',($app+'\\capabilityheader-cases.json'),$app);"))
elif a.action=='read':
 data=ps(native+unique+"Get-Content -Raw ($app+'\\capabilityheader-input.json')",retry_read=True);json.loads(data);(out/('native-'+a.label+'.json')).write_text(data+'\n');print(data)
elif a.action in ['invoke','input-invoke','input-record']:
 identifier='capabilityheader-input-record'if a.action=='input-record'else'CapabilityHeader-'+a.target;command=(native if a.action=='invoke'else inp)+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(identifier)+"));if($null-eq $b){throw 'Exact utility missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"
 if a.action=='invoke':print(ps(command))
 else:print(fresh(command,'preflight-capabilityheader-windows-input.json',out/('source-'+a.label+'.json')))
elif a.action in ['ax','input-ax']:
 code=(native if a.action=='ax'else inp)+unique+"$items=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition);@($items|Where-Object{$_.Current.AutomationId.StartsWith('CapabilityHeader-cell-')-or $_.Current.ControlType-eq [System.Windows.Automation.ControlType]::Text}|ForEach-Object{@{id=$_.Current.AutomationId;name=$_.Current.Name;type=$_.Current.ControlType.ProgrammaticName}})|ConvertTo-Json -Depth 5";data=ps(code,retry_read=True);(out/('native-ax.json'if a.action=='ax'else'source-ax.json')).write_text(data+'\n');print(data)
