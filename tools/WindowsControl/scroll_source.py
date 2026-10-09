"""Official SendInput/capture only in unique owned original scroll fixture, no settings changes."""
from pathlib import Path
import sys,hashlib,base64,json,time
from driver import ps,q
r=Path(__file__).resolve().parents[2];mode=sys.argv[1];assert mode in ['capture','wheel-down','wheel-up','hover-up','hover-down','hover-thumb','arrow-down','drag-bottom','pressed-thumb','pressed-arrow','pressed-up','arrow-up','track-below','hold-pause','hold-detach','hold-down','reset','reload','body-click','arrow-down-key','page-down-key','home-key','end-key','page-tall','wheel-burst','wheel-reverse','space-key','shift-space-key','tab-back','child-tab','short-key','interrupt-shrink','detach-pending'];f=r/'tools/WindowsControl/owned_scroll_source.cs';digest=hashlib.sha256(f.read_bytes()).hexdigest();(r/'artifacts/replica/windows-download/owned-scroll-source.cs').write_bytes(f.read_bytes());dll='OwnedScrollSource-'+digest[:12]+'.dll';out=r/'docs/replica/source/windows-source-sidebar.json';before=out.stat().st_mtime_ns
code="$folder=Join-Path $env:TEMP 'DBMV-ScrollViewport-20261007';New-Item -ItemType Directory -Force $folder|Out-Null;$dll=Join-Path $folder "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $folder 'owned-scroll-source.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/owned-scroll-source.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Owned source helper hash mismatch'};Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll','System.Drawing.dll',[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedScrollSource]::Run("+q(mode)+");"
print(ps(code,timeout=60))
if mode=='reload':sys.exit(0)
for i in range(80):
 if out.stat().st_mtime_ns>before:break
 time.sleep(.05)
else:raise RuntimeError('No fresh scroll record')
data=json.loads(out.read_text());(r/f'docs/replica/source/scrollviewport-windows-source-{mode}.json').write_bytes(out.read_bytes());
if mode=='page-tall':print('Fresh tall source offset',data['offset']);sys.exit(0)
v=ps("$f=Join-Path (Join-Path $env:TEMP 'DBMV-ScrollViewport-20261007') "+q('scroll-source-'+mode+'.png')+";[Convert]::ToBase64String([IO.File]::ReadAllBytes($f))",retry_read=True);(r/f'docs/replica/screenshots/scrollviewport-windows-source-{mode}.png').write_bytes(base64.b64decode(v));print({k:data[k]for k in ['width','height','clientWidth','scrollHeight','offset','wheels']})
