"""Guarded finite Table parent actions via unique immutable runtime and literal source."""
from pathlib import Path
import argparse,json,hashlib,time,re
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('mode',choices=['pointer','key','passive']);p.add_argument('--stage',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--source',action='store_true');a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
v=json.loads(a.stage.read_text());assert v['owner']=='preflight'and v['component']=='capabilitytable'and v['selfContained']and re.fullmatch(r'DBMV-Isolated-preflight-capabilitytable-[0-9a-f]{32}',v['guestFolder']);base="$base=Join-Path $env:TEMP "+q(v['guestFolder'])+";$app=Join-Path $base 'app';"
f=r/'tools/WindowsControl/owned_capability_table_input.cs';served='preflight-capabilitytable-input.cs';(r/'artifacts/replica/windows-download'/served).write_bytes(f.read_bytes());digest=hashlib.sha256(f.read_bytes()).hexdigest().upper();dll='OwnedCapabilityTableInput-'+digest[:12]+'.dll';endpoint=r/'docs/replica/source/preflight-capabilitytable-windows-input.json';prior=endpoint.stat().st_mtime_ns if endpoint.exists()else 0
code=base+"$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'owned-capability-table-input.cs';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Table helper hash mismatch'};$r=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$r.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedCapabilityTableInput]::Run($"+str(a.source).lower()+",$app,"+q(a.mode)+");"
data=ps(code,timeout=30)
if a.source:
 print(data);end=time.monotonic()+8
 while time.monotonic()<end:
  if endpoint.exists()and endpoint.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh owned original Table input')
 data=endpoint.read_text()
json.loads(data);a.output.mkdir(parents=True,exist_ok=True);(a.output/(('source-'if a.source else'native-')+a.mode+'.json')).write_text(data+'\n');print(data)
