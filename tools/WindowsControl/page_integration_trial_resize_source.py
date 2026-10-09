"""Fresh owned Edge original resize geometry8; automatic DOM oracle, scoped immediate close."""
from pathlib import Path
import json,time,uuid
from driver import ps,q
ROOT=Path(__file__).resolve().parents[2]
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home'
nonce=uuid.uuid4().hex;base=ROOT/'artifacts/replica/workers/home/pageintegrationtrial-resize-source'/nonce;base.mkdir(parents=True)
name='home-pageintegrationtrial-resize-'+nonce+'.html';source=ROOT/'artifacts/replica/windows-download/home-pageintegrationtrial-resize.html';title='Trial parent resize oracle - home '+nonce
s=source.read_text().replace('Trial parent resize oracle · home',title);(ROOT/'artifacts/replica/windows-download'/name).write_text(s)
url='http://10.211.55.2:8768/'+name+'?recording='+nonce+'&os=windows';record=ROOT/'docs/replica/source/home-pageintegrationtrial-resize-windows-source.json'
close="""$prefix=TITLE;$url=URL;$windows=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$owned=@();foreach($w in $windows){if(!$w.Current.Name.StartsWith($prefix)){continue};$process=Get-Process -Id $w.Current.ProcessId;if($process.ProcessName-ne'msedge'-or!$process.Path.EndsWith('\\msedge.exe',[StringComparison]::OrdinalIgnoreCase)){throw 'Own exact Edge path required'};$address=$false;foreach($n in $w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)){ $pat=$null;if($n.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$pat)){if($pat.Current.Value-eq$url-or$pat.Current.Value-eq$url.Replace('http://','')){$address=$true;break}}};if(!$address){throw 'Own exact URL required'};$owned+=@($w)};if($owned.Count-ne1){throw 'One exact nonce source window required'};$pidOwned=$owned[0].Current.ProcessId;$hwnd=$owned[0].Current.NativeWindowHandle;([System.Windows.Automation.WindowPattern]$owned[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)).Close();Start-Sleep -Milliseconds 250;$left=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith($prefix)});[pscustomobject]@{ownedSourceWindow=$left.Count;pid=$pidOwned;hwnd=$hwnd;titlePrefix=$prefix;url=$url}|ConvertTo-Json -Compress;if($left.Count){throw 'Own source remains'};""".replace('$prefix=TITLE;', '$prefix='+q(title)+';').replace('$url=URL;', '$url='+q(url)+';')
try:
 launch=ps("$ProgressPreference='SilentlyContinue';Start-Process msedge.exe -ArgumentList @('--new-window',"+q(url)+");[pscustomobject]@{nonce="+q(nonce)+";url="+q(url)+"}|ConvertTo-Json -Compress;",timeout=15);(base/'launch.log').write_text(launch)
 end=time.monotonic()+20;value=None
 while time.monotonic()<end:
  try:
   v=json.loads(record.read_text())
   if v.get('recording')==nonce and len(v.get('rows',[]))==8:value=v;break
  except (FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.1)
 if value is None:raise RuntimeError('Fresh source nonce8 not recorded; no input replay')
 (base/'source.json').write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n');print('Fresh original source8 recorded',base,flush=True)
except Exception as e:
 (base/'failure.json').write_text(json.dumps({'phase':'UNVERIFIED','exception':str(e),'replayedInput':False},indent=2)+'\n');raise
finally:
 r=ps(close,timeout=20);(base/'cleanup.log').write_text(r)
 for line in r.splitlines():
  try:v=json.loads(line)
  except json.JSONDecodeError:continue
  if 'ownedSourceWindow'in v:(base/'cleanup.json').write_text(json.dumps(v,indent=2)+'\n');print('Exact source close0',v['ownedSourceWindow'],flush=True)
