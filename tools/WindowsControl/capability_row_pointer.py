"""Project-scoped official SendInput: guarded owned CapabilityRow fixture only."""
import argparse,hashlib,json,time
from pathlib import Path
from driver import ps,q
from capability_row_runtime import load
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--target',type=int,choices=range(2),default=0);p.add_argument('--cancel',action='store_true');p.add_argument('--source',action='store_true');p.add_argument('--stage',type=Path,required=True);a=p.parse_args();stage,base=load(a.stage);source=r/'tools/WindowsControl/owned_capability_row_mouse.cs';(r/'artifacts/replica/windows-download/owned-capability_row-mouse.cs').write_bytes(source.read_bytes());digest=hashlib.sha256(source.read_bytes()).hexdigest();dll='OwnedCapabilityRowMouse-'+digest[:12]+'.dll'
evidence=r/'docs/replica/source/preflight-capabilityrow-windows-input.json';prior=evidence.stat().st_mtime_ns if evidence.exists() else 0
code=base+"$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'owned-capability_row-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/owned-capability_row-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Helper hash mismatch'};$r=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$r.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedCapabilityRowFixture]::Run("+str(a.target)+",$"+str(a.cancel).lower()+",($app+'\\ReplicaProbe.exe'));"
if a.source:code=code[:code.index('[OwnedCapabilityRowFixture]::Run')]+"[OwnedCapabilityRowSource]::Run("+str(a.target)+",$"+str(a.cancel).lower()+");"
data=ps(code)
if a.source:
 print(data);deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if evidence.exists() and evidence.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source pointer evidence')
 data=evidence.read_text()
record=json.loads(data);out=r/'docs/replica/source/capabilityrow-windows-input';out.mkdir(exist_ok=True);(out/(('source-' if a.source else '')+f'pointer-{a.target}-cancel-{a.cancel}.json')).write_text(data);print(record)
