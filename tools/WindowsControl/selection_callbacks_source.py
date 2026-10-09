"""Fresh Windows DOM callback oracle50, exact own title/path and immediate source close0."""
from pathlib import Path
import argparse,json,time
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('plan',type=Path);a=p.parse_args();plan=json.loads(a.plan.read_text());assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight';out=a.plan.parent/'windows';out.mkdir(exist_ok=True);url=plan['url'].replace('&os=mac','&os=windows');title=plan['title'].split(' · ')[0]
find="$own=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+")-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});"
try:
 code=find+"if($own.Count){throw 'Owned source already exists'};Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized','--force-device-scale-factor=2',"+q(url)+");[Console]::WriteLine('Fresh owned callback source requested');exit 0"
 (out/'launch.txt').write_text(ps(code,timeout=15));limit=time.monotonic()+20
 while time.monotonic()<limit:
  try:
   value=json.loads((r/'docs/replica/source/preflight-selectioncallbacks-windows-source.json').read_text())
   if value.get('nonce')==plan['nonce']and len(value.get('records',[]))==2:break
  except(FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.1)
 else:raise RuntimeError('Fresh source50 absent; do not replay source actions')
 assert value['originalHashes']==plan['originalHashes']and value['dpr']==2;assert all(len(x['rows'])==25 for x in value['records']);(out/'original-windows.json').write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n')
finally:
 record=ps(find+"if($own.Count-gt1){throw 'Ambiguous own source'};if($own.Count){$own[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()};Start-Sleep -Milliseconds 250;"+find+"@{sourceWindowCount=$own.Count}|ConvertTo-Json;exit 0",timeout=15);assert json.loads(record)['sourceWindowCount']==0;(out/'cleanup.json').write_text(record)
print('Fresh Windows source50 saved, exact source0')
