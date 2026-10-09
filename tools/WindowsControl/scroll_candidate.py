"""Official SendInput/capture only in unique owned original scroll fixture, no settings changes."""
from pathlib import Path
import sys,hashlib,base64,json,time
from driver import ps,q
r=Path(__file__).resolve().parents[2];mode=sys.argv[1];assert mode in ['capture','wheel-down','wheel-up','hover-up','hover-down','hover-thumb','arrow-down','drag-bottom','pressed-thumb','pressed-arrow','pressed-up','arrow-up','track-below','hold-pause','hold-detach','hold-down','reset','body-click','arrow-down-key','page-down-key','home-key','end-key','page-tall','wheel-burst','wheel-reverse','space-key','shift-space-key','tab-back','child-tab','short-key','interrupt-shrink','detach-pending'];f=r/'tools/WindowsControl/owned_scroll_candidate.cs';digest=hashlib.sha256(f.read_bytes()).hexdigest();(r/'artifacts/replica/windows-download/owned-scroll-candidate.cs').write_bytes(f.read_bytes());dll='OwnedScrollCandidate-'+digest[:12]+'.dll';out=r/'docs/replica/source/windows-source-sidebar.json'
code="$folder=Join-Path $env:TEMP 'DBMV-ScrollViewport-20261007';New-Item -ItemType Directory -Force $folder|Out-Null;$dll=Join-Path $folder "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $folder 'owned-scroll-candidate.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/owned-scroll-candidate.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Owned source helper hash mismatch'};Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll','System.Drawing.dll',[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedScrollCandidate]::Run("+q(mode)+");"
print(ps(code,timeout=60))
data=ps("$folder=Join-Path $env:TEMP 'DBMV-ScrollViewport-20261007';Get-Content -Raw (Join-Path $folder "+q("scroll-input-"+mode+".json")+")",retry_read=True)
out=r/'docs/replica/source/scrollviewport-windows-input';out.mkdir(exist_ok=True)
(out/('actual-'+mode+'.json')).write_text(data+'\n')
d=json.loads(data);print({k:d[k]for k in ['offset','events']})

if mode!='page-tall':
 image=ps("$folder=Join-Path $env:TEMP 'DBMV-ScrollViewport-20261007';[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $folder "+q('scroll-candidate-'+mode+'.png')+")))",retry_read=True)
 (r/f'docs/replica/screenshots/scrollviewport-windows-candidate-{mode}.png').write_bytes(base64.b64decode(image))
