from pathlib import Path
import sys,json,time
from driver import ps
r=Path(__file__).resolve().parents[2];out=r/'docs/replica/source/windows-source-sidebar.json';prior=out.stat().st_mtime_ns
print(ps("$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($wins|Where-Object{$_.Current.Name.StartsWith('ScrollViewport · original isolation')-and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});if($w.Count-ne 1){throw 'Owned original window must be unique'};$b=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'scroll-boundaries'));if($null-eq $b){throw 'Owned original boundaries missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()"))
for i in range(60):
 if out.stat().st_mtime_ns>prior:break
 time.sleep(.05)
else:raise RuntimeError('No fresh original boundary record')
d=json.loads(out.read_text());assert len(d['rows'])==14;(r/'docs/replica/source/scrollviewport-windows-source-boundaries.json').write_bytes(out.read_bytes());print([(x['id'],x['offset'])for x in d['rows']])
