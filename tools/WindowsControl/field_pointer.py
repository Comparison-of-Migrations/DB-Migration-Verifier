"""Explicit 2026-10-07 project verification control approval: owned Field fixture."""
import argparse,hashlib,json
from pathlib import Path
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--target',type=int,choices=range(3),default=0);a=p.parse_args();source=r/'tools/WindowsControl/owned_field_mouse.cs';(r/'artifacts/replica/windows-download/owned-field-mouse.cs').write_bytes(source.read_bytes());digest=hashlib.sha256(source.read_bytes()).hexdigest();dll='OwnedFieldMouse-'+digest[:12]+'.dll'
code="$app=Join-Path (Join-Path $env:TEMP 'DBMV-Field-20261007') 'app';$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'owned-field-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/owned-field-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Helper hash mismatch'};$r=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$r.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedFieldFixture]::Run("+str(a.target)+");"
data=ps(code);record=json.loads(data);out=r/'docs/replica/source/field-windows-input';out.mkdir(exist_ok=True);(out/f'pointer-{a.target}.json').write_text(data);print(record)

events=ps("$app=Join-Path (Join-Path $env:TEMP 'DBMV-Field-20261007') 'app';Get-Content -Raw ($app+'\\field-pointer-events-"+str(a.target)+".json')",retry_read=True);(out/f'pointer-{a.target}-events.json').write_text(events)
