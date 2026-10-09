"""Scoped official SendInput for unique research source/native windows."""
import argparse,hashlib,json,time
from pathlib import Path
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--source',action='store_true');p.add_argument('--target',choices=['research','language','body','gap'],default='research');p.add_argument('--label',default='pointer');a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home';m=json.loads(a.manifest.read_text());stage=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='home' and m['component']=='welcomepaper';source=r/'tools/WindowsControl/owned_welcome_paper_mouse.cs';served=r/'artifacts/replica/windows-download/owned-welcome-paper-mouse.cs';served.write_bytes(source.read_bytes());digest=hashlib.sha256(source.read_bytes()).hexdigest();dll='OwnedWelcomePaperMouse-'+digest[:12]+'.dll';base="$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';"
prior=r/'docs/replica/source/home-welcomepaper-windows-input.json';stamp=prior.stat().st_mtime_ns if prior.exists() else 0
code=base+"$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'owned-welcome-paper-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/owned-welcome-paper-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Helper hash mismatch'};$r=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$r.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedWelcomePaperFixture]::Run($app,$"+str(a.source).lower()+","+str({'research':0,'language':1,'body':2,'gap':3}[a.target])+");"
data=ps(code)
if a.source:
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if prior.exists() and prior.stat().st_mtime_ns>stamp:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source pointer evidence')
 data=prior.read_text()
out=r/'docs/replica/source/welcomepaper-windows-input';out.mkdir(exist_ok=True);(out/f'{"source-" if a.source else ""}{a.label}.json').write_text(data);print(json.loads(data))
