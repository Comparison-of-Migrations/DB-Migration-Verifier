"""Actual pointer to fixed InfoDialog buttons only; scoped cold runtime/source and fresh evidence."""
from pathlib import Path
import argparse,hashlib,json,re,time
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('--source',action='store_true');p.add_argument('--event-record-only',action='store_true');p.add_argument('--target',choices=['open','close','footer-close'],required=True);p.add_argument('--label',required=True);a=p.parse_args()
assert re.fullmatch('[a-z0-9-]+',a.label)
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='root'
m=json.loads(a.manifest.read_text());s=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='root'and m['component']=='infodialog'and s['selfContained']
f=r/'tools/WindowsControl/info_dialog_mouse.cs';served=r/'artifacts/replica/windows-download/root-infodialog-mouse.cs';served.write_bytes(f.read_bytes());digest=hashlib.sha256(f.read_bytes()).hexdigest();base="$base=Join-Path $env:TEMP "+q(s['guestFolder'])+";$app=Join-Path $base 'app';$out=Join-Path $base 'actual';";dll='OwnedInfoDialogMouse-'+digest[:12]+'.dll'
code=base+"$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'root-infodialog-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/root-infodialog-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Helper hash mismatch'};$rect=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$rect.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedInfoDialogMouse]::Run(($app+'\\ReplicaProbe.exe'),"+('$true'if a.source else'$false')+","+q(a.target)+");"
path=r/'docs/replica/source/root-infodialog-windows-input.json';prior=path.stat().st_mtime_ns if path.exists()else 0
ps(code)
condition="$_.Current.Name.StartsWith('InfoDialog input · original isolation') -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')" if a.source else "$_.Current.Name-eq 'Replica InfoDialog verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"
owned=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{"+condition+"});if($w.Count-ne 1){throw 'Unique owned window required'};$w=$w[0];"
record='InfoDialog-record'
if not a.source and not a.event_record_only:ps(owned+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(record)+"));if($null-eq $b){throw 'Owned recorder missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();")
if a.source:
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if path.exists()and path.stat().st_mtime_ns>prior:break
  time.sleep(.03)
 else:raise RuntimeError('Stale original evidence')
 data=path.read_text()
else:data=ps(owned+"Get-Content -Raw ($out+'\\infodialog-input.json')",retry_read=True)
folder=a.manifest.parent/'actual-windows'/('source-input'if a.source else'input');folder.mkdir(parents=True,exist_ok=True);json.loads(data);(folder/(a.label+'.json')).write_text(data);print(data)
