"""Exactly one actual original bulk Toggle action, scoped nonce/path/title, immediate close0."""
from pathlib import Path
import argparse,json,time,shutil
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('plan',type=Path);a=p.parse_args();d=json.loads(a.plan.read_text());o=a.plan.parent;assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
def find():return "$own=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(d['title'])+")-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});"
endpoint=r/'docs/replica/source/preflight-selectionpage-windows-input.json'
def read(test):
 limit=time.monotonic()+8
 while time.monotonic()<limit:
  try:
   value=json.loads(endpoint.read_text())
   if value.get('batch')==d['nonce']and test(value):return value
  except(FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.08)
 raise RuntimeError('Fresh source state absent; no action replay')
try:
 data=ps(find()+"if($own.Count){throw 'Exact own source already exists'};Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized','--force-device-scale-factor=2',"+q(d['url'])+");[Console]::WriteLine('Owned source requested');exit 0",timeout=15);(o/'launch.txt').write_text(data)
 before=read(lambda s:s['query']=='orders'and s['tables']==['demo.customers','demo.line_items']and s['shown']==['demo.orders']);(o/'before.json').write_text(json.dumps(before,ensure_ascii=False,indent=2))
 code=find()+"if($own.Count-ne1){throw 'Unique original source required'};$w=$own[0];$cond=[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::CheckBox),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'검색 결과 전체 선택'));$targets=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,$cond);if($targets.Count-ne1){throw 'Unique source bulk required'};$target=$targets[0];$b=$target.Current.BoundingRectangle;if(!$target.Current.IsEnabled-or$target.Current.IsOffscreen-or$b.Width-le0-or$b.Height-le0-or!$w.Current.BoundingRectangle.Contains($b)){throw 'Fresh source checkbox geometry/enabled/visible guard failed'};$toggle=$target.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern);$before=$toggle.Current.ToggleState.ToString();$toggle.Toggle();@{pid=$w.Current.ProcessId;path=(Get-Process -Id $w.Current.ProcessId).Path;title=$w.Current.Name;name=$target.Current.Name;type=$target.Current.ControlType.ProgrammaticName;before=$before;after=$toggle.Current.ToggleState.ToString();actions=1;box=@{x=$b.X;y=$b.Y;width=$b.Width;height=$b.Height}}|ConvertTo-Json;exit 0"
 result=ps(code,timeout=15);json.loads(result);(o/'toggle-identity.json').write_text(result)
 after=read(lambda s:s['sequence']>before['sequence']and s['tables']==['demo.customers','demo.line_items','demo.orders']);assert after['announcement']=='검색 결과의 선택을 변경했습니다.'and after['bulk']['checked'];(o/'after.json').write_text(json.dumps(after,ensure_ascii=False,indent=2));(o/'comparison.json').write_text(json.dumps({'passed':True,'scope':'single original bulk Toggle updates filtered selected table and exact source announcement','actions':1,'nonce':d['nonce']}))
finally:
 result=ps(find()+"if($own.Count-gt1){throw 'Ambiguous own source close'};if($own.Count){$own[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()};Start-Sleep -Milliseconds 250;"+find()+"@{sourceWindowCount=$own.Count}|ConvertTo-Json;exit 0",timeout=15);assert json.loads(result)['sourceWindowCount']==0;(o/'cleanup.json').write_text(result)
print('Original one Toggle passed, exact source0')
