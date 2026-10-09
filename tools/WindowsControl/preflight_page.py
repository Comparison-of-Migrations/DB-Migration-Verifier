"""Owned Preflight source16 and cold native scope; finite official CLI/UIA commands."""
from pathlib import Path
import argparse,json,time,re,uuid,hashlib,base64
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-batch','auto','launch','read','invoke','close','cleanup']);p.add_argument('--stage',type=Path);p.add_argument('--output',type=Path,required=True);p.add_argument('--target',choices=['next','language','mount','reduced','focus-title','focus-permission']);p.add_argument('--label',default='initial');a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight';a.output.mkdir(parents=True,exist_ok=True)
base='';v=None
if a.stage:
 v=json.loads(a.stage.read_text());assert v['owner']=='preflight'and v['component']=='preflightpage'and v['selfContained']and re.fullmatch(r'DBMV-Isolated-preflight-preflightpage-[0-9a-f]{32}',v['guestFolder']);base="$base=Join-Path $env:TEMP "+q(v['guestFolder'])+";$app=Join-Path $base 'app';"
def find(title,source=False):
 guard="(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')"if source else"(Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"
 return (''if source else base)+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+")-and "+guard+"});"
unique="if($w.Count-ne 1){throw 'Unique owned Preflight window required'};$w=$w[0];";native=find('Replica PreflightPage verification')
if a.action=='source-batch':
 guard=find('PreflightPage · original isolation',True);endpoint=r/'docs/replica/source/preflight-preflightpage-windows-source.json';prior=endpoint.stat().st_mtime_ns if endpoint.exists()else 0
 print(ps(guard+"if($w.Count){throw 'Owned source already exists'};Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized','--force-device-scale-factor=2','http://10.211.55.2:8768/preflightpage.html?os=windows&record=1');"))
 limit=time.monotonic()+25
 while time.monotonic()<limit:
  if endpoint.exists()and endpoint.stat().st_mtime_ns>prior:
   try:
    data=json.loads(endpoint.read_text())
    if data['dpr']==2 and len(data['rows'])==16:break
   except (KeyError,json.JSONDecodeError):pass
  time.sleep(.08)
 else:raise RuntimeError('Fresh source16 absent; scope preserved, do not retry input')
 (a.output/'preflightpage-source.json').write_bytes(endpoint.read_bytes());(r/'docs/replica/source/preflightpage-windows-source.json').write_bytes(endpoint.read_bytes());cases=[{'id':c['id'],'width':c['root']['box']['width'],'viewport':c['viewportWidth'],'english':c['english']}for c in data['rows']];(r/'docs/replica/source/preflightpage-windows-cases.json').write_text(json.dumps(cases,indent=2))
 print(ps(guard+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"));cleanup=json.loads(ps(guard+"@{sourceWindowCount=$w.Count}|ConvertTo-Json",retry_read=True));assert cleanup['sourceWindowCount']==0;(a.output/'source-cleanup.json').write_text(json.dumps(cleanup));print('Source16 fresh/closed0')
else:
 assert v is not None,'Cold owned Preflight stage required'
 if a.action in ['auto','launch']:
  cases=json.loads((r/'docs/replica/source/preflightpage-windows-cases.json').read_text())
  if a.action=='launch':cases=[c for c in cases if c['id']in ['ko-1180','en-1180']]
  body=json.dumps(cases).encode();name='preflight-preflightpage-'+uuid.uuid4().hex+'-cases.json';(r/'artifacts/replica/windows-download'/name).write_bytes(body);digest=hashlib.sha256(body).hexdigest().upper();print(ps(base+"Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+name)+" -OutFile ($app+'\\preflightpage-cases.json');if((Get-FileHash ($app+'\\preflightpage-cases.json') -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Cases mismatch'};"))
 if a.action=='auto':
  print(ps(base+"& ($app+'\\ReplicaProbe.exe') '--preflightpage' ($app+'\\preflightpage-cases.json') $app '--geometry-only';if($LASTEXITCODE-ne 0){throw 'Preflight native failed'};",timeout=45));data=ps(base+"Get-Content -Raw ($app+'\\preflightpage-native.json')",retry_read=True);assert len(json.loads(data)['rows'])==16;(a.output/'preflightpage-native.json').write_text(data)
  for name in ['preflightpage-ko-960.png','preflightpage-en-960.png','preflightpage-en-1440.png']:(a.output/name).write_bytes(base64.b64decode(ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($app+'\\"+name+"'))",retry_read=True)))
 elif a.action=='launch':
  launch=ps(native+"if($w.Count){throw 'Owned native already exists'};$proc=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--preflightpage-window',($app+'\\preflightpage-cases.json'),$app) -PassThru;$identity=@{pid=$proc.Id;path=($app+'\\ReplicaProbe.exe');started=[DateTime]::UtcNow.ToString('o')};$identity|ConvertTo-Json|Set-Content -Encoding UTF8 ($app+'\\preflightpage-launch.json');$limit=[DateTime]::UtcNow.AddSeconds(10);do{Start-Sleep -Milliseconds 100;"+native+"}while($w.Count-eq 0-and [DateTime]::UtcNow-lt $limit);if($w.Count-ne 1){throw 'Owned native startup window absent; launch identity preserved, do not relaunch'};$identity|ConvertTo-Json",timeout=20)
  json.loads(launch);(a.output/'native-launch.json').write_text(launch);print(launch)
 elif a.action=='read':
  data=ps(native+unique+"Get-Content -Raw ($app+'\\preflightpage-input.json')",retry_read=True);json.loads(data);(a.output/('native-'+a.label+'.json')).write_text(data);print(data)
 elif a.action=='invoke':
  assert a.target
  walk="function FindId($e,$id,$depth){if($depth-gt 18-or $script:visits++-gt 350){return $null};if($e.Current.AutomationId-eq $id){return $e};$walker=[System.Windows.Automation.TreeWalker]::ControlViewWalker;$child=$walker.GetFirstChild($e);$count=0;while($null-ne $child-and $count++-lt 80){$found=FindId $child $id ($depth+1);if($null-ne $found){return $found};$child=$walker.GetNextSibling($child)};return $null};$script:visits=0;"
  print(ps(native+unique+walk+"$b=FindId $w "+q('PreflightPage-'+a.target)+" 0;if($null-eq $b){throw 'Exact utility absent'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 elif a.action=='close':print(ps(native+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 elif a.action=='cleanup':
  data=json.loads(ps(native+"$p=@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')});@{nativeWindowCount=$w.Count;nativeProcessCount=$p.Count}|ConvertTo-Json",retry_read=True));assert data['nativeWindowCount']==data['nativeProcessCount']==0;(a.output/'native-cleanup.json').write_text(json.dumps(data));print(data)
