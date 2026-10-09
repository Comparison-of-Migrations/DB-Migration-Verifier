"""Actual original label activation; exact owned Edge Field fixture only."""
from pathlib import Path
import argparse,hashlib,json
from driver import ps,q
r=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--target',type=int,choices=range(3),default=0);a=p.parse_args()
source=r/'tools/WindowsControl/owned_field_mouse.cs';(r/'artifacts/replica/windows-download/owned-field-mouse.cs').write_bytes(source.read_bytes());digest=hashlib.sha256(source.read_bytes()).hexdigest();dll='OwnedFieldMouse-'+digest[:12]+'.dll'
code="$app=Join-Path (Join-Path $env:TEMP 'DBMV-Field-20261007') 'app';$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'owned-field-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/owned-field-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Helper hash mismatch'};$r=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$r.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedFieldSource]::Run("+str(a.target)+");"
data=json.loads(ps(code));out=r/'docs/replica/source/field-windows-input';(out/f'source-pointer-{a.target}.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print(data);assert data['active']==['ko-180-0','','ko-port'][a.target]
