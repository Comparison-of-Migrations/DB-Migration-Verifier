"""Owned Edge InfoDialog literal input fixture diagnostics; resource/freshness guards."""
from pathlib import Path
import argparse,hashlib,json,re,time
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['open','invoke','close','peers']);p.add_argument('--target',choices=['record','small','permissions','restore'],default='record');p.add_argument('--label',default='record');a=p.parse_args();assert re.fullmatch('[a-z0-9-]+',a.label)
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='root'
owned="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('InfoDialog input · original isolation') -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned Edge source required'};$w=$w[0];"
if a.action=='open':
 version=hashlib.sha256((r/'artifacts/replica/windows-download/infodialog-input.html').read_bytes()).hexdigest()[:16]
 print(ps(owned+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window',"+q('http://10.211.55.2:8768/infodialog-input.html?os=windows&fixture='+version)+");"))
elif a.action=='close':print(ps(owned+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='peers':
 data=ps(owned+unique+"$all=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition);@($all|ForEach-Object{[pscustomobject]@{name=$_.Current.Name;id=$_.Current.AutomationId;type=$_.Current.ControlType.ProgrammaticName;focus=$_.Current.HasKeyboardFocus;control=$_.Current.IsControlElement;content=$_.Current.IsContentElement}})|ConvertTo-Json -Depth 4",retry_read=True);p=r/'docs/replica/source'/('infodialog-windows-source-peers-'+a.label+'.json');json.loads(data);p.write_text(data);print(data)
else:
 path=r/'docs/replica/source/root-infodialog-windows-input.json';prior=path.stat().st_mtime_ns if path.exists()else 0
 print(ps(owned+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('InfoDialog-source-'+a.target)+"));if($null-eq $b){throw 'Diagnostic target absent'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 if a.target=='record':
  deadline=time.monotonic()+8
  while time.monotonic()<deadline:
   if path.exists()and path.stat().st_mtime_ns>prior:break
   time.sleep(.03)
  else:raise RuntimeError('Fresh source record missing')
  value=json.loads(path.read_text());(r/'docs/replica/source'/('infodialog-windows-source-input-'+a.label+'.json')).write_text(json.dumps(value,indent=2)+'\n');print(value)
