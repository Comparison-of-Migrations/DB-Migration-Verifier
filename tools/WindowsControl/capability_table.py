"""Owned full-table source batch and isolated native parent verification."""
from pathlib import Path
import argparse,json,time,re,uuid,hashlib,base64
from driver import ps,q
r=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=['source-batch','source-input-open','source-input-close','source-input-read','source-input-invoke','source-input-ax','source-input-detached','auto','launch','read','invoke','ax','detached','close','cleanup']);p.add_argument('--stage',type=Path);p.add_argument('--output',type=Path,required=True);p.add_argument('--target',choices=['next','mount','record','focus'],default='record');p.add_argument('--label',default='initial');a=p.parse_args()
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
a.output.mkdir(parents=True,exist_ok=True)
base='';v=None
if a.stage:
 v=json.loads(a.stage.read_text());assert v['owner']=='preflight'and v['component']=='capabilitytable'and v['selfContained']and re.fullmatch(r'DBMV-Isolated-preflight-capabilitytable-[0-9a-f]{32}',v['guestFolder']);base="$base=Join-Path $env:TEMP "+q(v['guestFolder'])+";$app=Join-Path $base 'app';"
def find(title,source=False):
 guard="(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')"if source else"(Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"
 return (''if source else base)+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+")-and "+guard+"});"
unique="if($w.Count-ne 1){throw 'Unique owned CapabilityTable window required'};$w=$w[0];"
native=find('Replica CapabilityTable verification')
if a.action=='source-batch':
 endpoint=r/'docs/replica/source/preflight-capabilitytable-windows-source.json'
 for page,title,button,dest,count in [('capabilitytable.html','CapabilityTable · original isolation','capabilitytable-record','capabilitytable-windows-source.json',12),('capabilitytable-intrinsics.html','CapabilityTable cell contributions · original isolation','record-cell-intrinsics','capabilitytable-windows-cell-intrinsics.json',80)]:
  guard=find(title,True)
  print(ps(guard+"if($w.Count){throw 'Owned Table source already exists'};Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized','--force-device-scale-factor=2',"+q('http://10.211.55.2:8768/'+page+'?os=windows')+");"))
  prior=endpoint.stat().st_mtime_ns if endpoint.exists()else 0
  # One guarded guest call waits for the exact new window and its record control.
  code="$limit=(Get-Date).AddSeconds(8);do{"+guard+"if($w.Count-eq 1){$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(button)+"));if($null-ne $b){break}};Start-Sleep -Milliseconds 100}while((Get-Date)-lt $limit);"+unique+"if($null-eq $b){throw 'Fresh source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"
  print(ps(code));limit=time.monotonic()+8
  while time.monotonic()<limit:
   if endpoint.exists()and endpoint.stat().st_mtime_ns>prior:break
   time.sleep(.05)
  else:raise RuntimeError('Fresh owned Table source record absent')
  data=json.loads(endpoint.read_text());assert data['dpr']==2 and len(data.get('tables',data.get('samples',[])))==count
  (a.output/dest).write_bytes(endpoint.read_bytes());(r/'docs/replica/source'/dest).write_bytes(endpoint.read_bytes())
  print(ps(guard+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 count=json.loads(ps("$n=0;"+find('CapabilityTable · original isolation',True)+"$n+=$w.Count;"+find('CapabilityTable cell contributions · original isolation',True)+"$n+=$w.Count;@{sourceWindowCount=$n}|ConvertTo-Json",retry_read=True));assert count['sourceWindowCount']==0;(a.output/'source-cleanup.json').write_text(json.dumps(count)+'\n');print('Original Table12/intrinsic4/cell80 fresh; cleanup0')
elif a.action.startswith('source-input-'):
 guard=find('CapabilityTable Input · original isolation',True)
 if a.action=='source-input-open':print(ps(guard+"if($w.Count){throw 'Owned Table source input already exists'};Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized','--force-device-scale-factor=2','http://10.211.55.2:8768/capabilitytable-input.html?os=windows');"))
 elif a.action=='source-input-close':print(ps(guard+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 elif a.action in ['source-input-read','source-input-invoke']:
  endpoint=r/'docs/replica/source/preflight-capabilitytable-windows-input.json';prior=endpoint.stat().st_mtime_ns if endpoint.exists()else 0;identifier='CapabilityTable-'+(a.target if a.action=='source-input-invoke'else'record')
  if a.action=='source-input-invoke':print(ps(guard+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(identifier)+"));if($null-eq $b){throw 'Exact Table utility absent'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
  limit=time.monotonic()+8
  while time.monotonic()<limit:
   if endpoint.exists()and endpoint.stat().st_mtime_ns>prior:break
   time.sleep(.05)
  else:raise RuntimeError('No fresh original Table input')
  data=endpoint.read_text();json.loads(data);(a.output/('source-'+a.label+'.json')).write_text(data+'\n');print(data)
 elif a.action=='source-input-detached':
  data=ps(guard+unique+"$t=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CapabilityTable-source'));$b=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CapabilityRow-permissions'));@{tablePeers=$t.Count;permissionPeers=$b.Count}|ConvertTo-Json",retry_read=True);value=json.loads(data);assert value['tablePeers']==value['permissionPeers']==0;(a.output/'source-detached-ax.json').write_text(data+'\n');print(data)
 else:
  code=guard+unique+"function Tree($e,$depth){if($depth-gt 8){return $null};$c=@($e.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition));@{id=$e.Current.AutomationId;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;focusable=$e.Current.IsKeyboardFocusable;box=@{x=$e.Current.BoundingRectangle.X;y=$e.Current.BoundingRectangle.Y;width=$e.Current.BoundingRectangle.Width;height=$e.Current.BoundingRectangle.Height};children=@($c|ForEach-Object{Tree $_ ($depth+1)})}};$t=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CapabilityTable-source'));if($null-eq $t){throw 'Original Table peer missing'};Tree $t 0|ConvertTo-Json -Depth 24"
  data=ps(code,retry_read=True);json.loads(data);(a.output/('source-ax-'+a.label+'.json')).write_text(data+'\n');print(data)
else:
 assert v is not None,'Unique Table stage required'
 if a.action in ['auto','launch']:
  cases=r/'docs/replica/source'/('capabilitytable-cases.json'if a.action=='auto'else'capabilitytable-input-cases.json');name='preflight-capabilitytable-'+uuid.uuid4().hex+'-cases.json';(r/'artifacts/replica/windows-download'/name).write_bytes(cases.read_bytes());digest=hashlib.sha256(cases.read_bytes()).hexdigest().upper()
  print(ps(base+"Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+name)+" -OutFile ($app+'\\capabilitytable-cases.json');if((Get-FileHash ($app+'\\capabilitytable-cases.json') -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Cases hash mismatch'};"))
 if a.action=='auto':
  print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--capabilitytable' ($app+'\\capabilitytable-cases.json') $app '--geometry-only';if($LASTEXITCODE-ne 0){throw 'Owned Table native failed'};",timeout=60));data=ps(base+"Get-Content -Raw ($app+'\\capabilitytable-native.json')",retry_read=True);assert len(json.loads(data)['tables'])==12;(a.output/'capabilitytable-native.json').write_text(data+'\n')
  for name in ['capabilitytable-ko-478.png','capabilitytable-en-478.png','capabilitytable-ko-904.png']:(a.output/name).write_bytes(base64.b64decode(ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($app+'\\"+name+"'))",retry_read=True)))
 elif a.action=='launch':print(ps(native+"if($w.Count){throw 'Owned Table native already exists'};Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--capabilitytable-window',($app+'\\capabilitytable-cases.json'),$app);"))
 elif a.action=='invoke':print(ps(native+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('CapabilityTable-'+a.target)+"));if($null-eq $b){throw 'Exact Table utility missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 elif a.action=='read':
  data=ps("$until=(Get-Date).AddSeconds(8);do{"+native+"if($w.Count-eq 1){break};Start-Sleep -Milliseconds 80}while((Get-Date)-lt $until);"+unique+"Get-Content -Raw ($app+'\\capabilitytable-input.json')",retry_read=True);json.loads(data);(a.output/('native-'+a.label+'.json')).write_text(data+'\n');print(data)
 elif a.action=='close':print(ps(native+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 elif a.action=='cleanup':
  data=json.loads(ps(native+"$p=@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')});@{nativeWindowCount=$w.Count;nativeProcessCount=$p.Count}|ConvertTo-Json",retry_read=True));assert data['nativeWindowCount']==data['nativeProcessCount']==0;(a.output/'native-cleanup.json').write_text(json.dumps(data)+'\n');print(data)
 elif a.action=='detached':
  data=ps(native+unique+"$t=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CapabilityTable-root'));$b=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CapabilityRow-permissions'));@{tablePeers=$t.Count;permissionPeers=$b.Count}|ConvertTo-Json",retry_read=True);value=json.loads(data);assert value['tablePeers']==value['permissionPeers']==0;(a.output/'native-detached-ax.json').write_text(data+'\n');print(data)
 elif a.action=='ax':
  code=native+unique+"function Tree($e,$depth){if($depth-gt 8){return $null};$c=@($e.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition));@{id=$e.Current.AutomationId;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;focusable=$e.Current.IsKeyboardFocusable;box=@{x=$e.Current.BoundingRectangle.X;y=$e.Current.BoundingRectangle.Y;width=$e.Current.BoundingRectangle.Width;height=$e.Current.BoundingRectangle.Height};children=@($c|ForEach-Object{Tree $_ ($depth+1)})}};$t=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CapabilityTable-root'));if($null-eq $t){throw 'Table parent peer missing'};Tree $t 0|ConvertTo-Json -Depth 24"
  data=ps(code,retry_read=True);json.loads(data);(a.output/('native-ax-'+a.label+'.json')).write_text(data+'\n');print(data)
