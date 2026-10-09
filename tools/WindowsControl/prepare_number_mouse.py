"""Build the explicitly authorized, fixed-scope UIA/SendInput test helper in user TEMP."""
import hashlib,sys
from pathlib import Path
from driver import ps,q,APP_FOLDER
root=Path(__file__).resolve().parents[2]
source=root/'tools/WindowsControl/owned_number_mouse.cs'
(root/'artifacts/replica/windows-download/owned-number-mouse.cs').write_bytes(source.read_bytes())
digest=hashlib.sha256(source.read_bytes()).hexdigest()
name='OwnedNumberMouse-'+digest[:12]+'.dll'
code="$app=Join-Path (Join-Path $env:TEMP "+q(APP_FOLDER)+") 'app';$dll=Join-Path $app "+q(name)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'owned-number-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/owned-number-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash -ne "+q(digest.upper())+"){throw 'Owned helper source hash mismatch'};$r=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$r.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};[Console]::WriteLine('Owned helper ready: '+[IO.Path]::GetFileName($dll));"
print(ps(code))
