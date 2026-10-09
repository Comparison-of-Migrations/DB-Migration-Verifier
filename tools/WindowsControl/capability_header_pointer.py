"""One guarded passive click in the owned source/native header; no generic window input."""
from pathlib import Path
import argparse,json,hashlib,re,time
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--stage',type=Path,required=True);p.add_argument('--source',action='store_true');a=p.parse_args();v=json.loads(a.stage.read_text());assert v['owner']=='preflight'and v['component']=='capabilityheader'and v['selfContained']and re.fullmatch(r'DBMV-Isolated-preflight-capabilityheader-[0-9a-f]{32}',v['guestFolder']);base="$base=Join-Path $env:TEMP "+q(v['guestFolder'])+";$app=Join-Path $base 'app';";file=r/'tools/WindowsControl/owned_capability_header_mouse.cs';served='preflight-capabilityheader-mouse.cs';(r/'artifacts/replica/windows-download'/served).write_bytes(file.read_bytes());digest=hashlib.sha256(file.read_bytes()).hexdigest();dll='OwnedCapabilityHeaderMouse-'+digest[:12]+'.dll';evidence=r/'docs/replica/source/preflight-capabilityheader-windows-input.json';prior=evidence.stat().st_mtime_ns if evidence.exists()else 0
code=base+"$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'owned-capability-header-mouse.cs';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Header helper hash mismatch'};$r=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$r.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedCapabilityHeaderFixture]::Click($"+str(a.source).lower()+",($app+'\\ReplicaProbe.exe'));"
data=ps(code)
if a.source:
 print(data);deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if evidence.exists()and evidence.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh header passive source evidence')
 data=evidence.read_text()
value=json.loads(data);out=r/'docs/replica/source/capabilityheader-windows-input';out.mkdir(exist_ok=True);(out/('source-passive.json'if a.source else'native-passive.json')).write_text(data+'\n');print(value)
