"""Owned ConnectionPage guarded SendInput only; full isolated runtime required."""
from pathlib import Path
import argparse,hashlib,json,re
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--stage',type=Path,required=True);p.add_argument('--label',choices=['pointer-closed','pointer-open'],required=True);a=p.parse_args()
if json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']!='connection':raise RuntimeError('Windows resource required')
s=json.loads(a.stage.read_text())
if s['owner']!='connection'or s['component']!='connectionpage'or not s['selfContained']or not re.fullmatch(r'DBMV-Isolated-connection-connectionpage-[0-9a-f]{32}',s['guestFolder']):raise RuntimeError('Owned isolated stage required')
f=r/'tools/WindowsControl/connection_page_mouse.cs';(r/'artifacts/replica/windows-download/connection-page-mouse.cs').write_bytes(f.read_bytes());digest=hashlib.sha256(f.read_bytes()).hexdigest();dll='OwnedConnectionPageMouse-'+digest[:12]+'.dll'
code="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'connection-page-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/connection-page-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Helper hash mismatch'};$r=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$r.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedConnectionPageFixture]::Run(($app+'\\ReplicaProbe.exe'));"
data=ps(code);v=json.loads(data);o=r/'docs/replica/source/connectionpage-windows-input';(o/(a.label+'.json')).write_text(data);print({'view':v['view'],'focus':v['focus'],'requests':v['requests']})
