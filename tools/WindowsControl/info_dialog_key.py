"""Exact owned InfoDialog source/native key press-release; resource/focus/nonce guard."""
from pathlib import Path
import argparse,json,re,time
from driver import ps,q
r=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('--source',action='store_true');p.add_argument('--target',choices=['current','open','close','footer-close'],default='current');p.add_argument('--key',choices=['tab','escape','end','enter','space'],required=True);p.add_argument('--label',required=True);a=p.parse_args()
assert re.fullmatch('[a-z0-9-]+',a.label)
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='root'
m=json.loads(a.manifest.read_text());s=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='root' and m['component']=='infodialog' and s['selfContained']
base="$base=Join-Path $env:TEMP "+q(s['guestFolder'])+";$app=Join-Path $base 'app';$out=Join-Path $base 'actual';"
condition="$_.Current.Name.StartsWith('InfoDialog input · original isolation') -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')" if a.source else "$_.Current.Name-eq 'Replica InfoDialog verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"
owned=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{"+condition+"});if($w.Count-ne 1){throw 'Unique owned InfoDialog required'};$w=$w[0];"
focus=''
if a.target!='current':
 if a.source:
  name={'open':'Open details','close':'닫기','footer-close':'닫기'}[a.target]
  focus="$c=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Button))|Where-Object{$_.Current.Name-eq "+q(name)+"});if($c.Count-ne "+('1' if a.target=='open' else '2')+"){throw 'Source button count mismatch'};$c=$c["+('1' if a.target=='footer-close' else '0')+"];$c.SetFocus();"
 else:focus="$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('InfoDialog-'+a.target)+"));if($null-eq $c){throw 'Owned target missing'};$c.SetFocus();"
source_file=r/'docs/replica/source/root-infodialog-windows-input.json';source_prior=source_file.stat().st_mtime_ns if source_file.exists()else 0
f=r/'tools/WindowsControl/info_dialog_mouse.cs';served=r/'artifacts/replica/windows-download/root-infodialog-mouse.cs';served.write_bytes(f.read_bytes());digest=__import__('hashlib').sha256(f.read_bytes()).hexdigest();dll='OwnedInfoDialogMouse-'+digest[:12]+'.dll'
code=base+"$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'root-infodialog-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/root-infodialog-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Helper hash mismatch'};$rect=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$rect.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;[OwnedInfoDialogMouse]::Key(($app+'\\ReplicaProbe.exe'),"+('$true'if a.source else'$false')+","+q(a.key)+","+q(a.target)+");"
ps(code)
if a.source:
 f=source_file;prior=source_prior
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if f.exists() and f.stat().st_mtime_ns>prior:break
  time.sleep(.03)
 else:raise RuntimeError('Source evidence stale')
 data=f.read_text()
else:
 ps(owned+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'InfoDialog-record'));if($null-eq $b){throw 'Owned recorder missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();")
 data=ps(owned+"Get-Content -Raw ($out+'\\infodialog-input.json')",retry_read=True)
folder=a.manifest.parent/'actual-windows'/('source-input'if a.source else'input');folder.mkdir(parents=True,exist_ok=True);json.loads(data);(folder/(a.label+'.json')).write_text(data);print(data)
