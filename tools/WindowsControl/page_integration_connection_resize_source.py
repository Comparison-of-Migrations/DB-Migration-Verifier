"""Fresh owned Edge original resize geometry8; automatic DOM oracle, scoped immediate close."""
from pathlib import Path
import json,time,uuid,argparse,hashlib
from driver import ps,q
ROOT=Path(__file__).resolve().parents[2]
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home'
p=argparse.ArgumentParser();p.add_argument('--expanded',action='store_true');a=p.parse_args();nonce=uuid.uuid4().hex;base=ROOT/'artifacts/replica/workers/home/pageintegrationconnection-resize-source'/nonce;base.mkdir(parents=True)
name='home-pageintegrationconnection-resize-'+nonce+'.html';source=ROOT/('artifacts/replica/windows-download/home-pageintegrationconnection-resize-expanded.html' if a.expanded else 'artifacts/replica/windows-download/home-pageintegrationconnection-resize.html');title=('Connection expanded resize oracle - home ' if a.expanded else 'Connection parent resize oracle - home ')+nonce
s=source.read_text().replace('Connection expanded resize oracle · home' if a.expanded else 'Connection parent resize oracle · home',title);(ROOT/'artifacts/replica/windows-download'/name).write_text(s)
url='http://10.211.55.2:8768/'+name+'?recording='+nonce+'&os=windows';record=ROOT/('docs/replica/source/home-pageintegrationconnection-resize-windows-control-ready.json' if a.expanded else 'docs/replica/source/home-pageintegrationconnection-resize-windows-source.json')
close="""$prefix=TITLE;$url=URL;$windows=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$owned=@();foreach($w in $windows){if(!$w.Current.Name.StartsWith($prefix)){continue};$process=Get-Process -Id $w.Current.ProcessId;if($process.ProcessName-ne'msedge'-or!$process.Path.EndsWith('\\msedge.exe',[StringComparison]::OrdinalIgnoreCase)){throw 'Own exact Edge path required'};$address=$false;foreach($n in $w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)){ $pat=$null;if($n.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$pat)){if($pat.Current.Value-eq$url-or$pat.Current.Value-eq$url.Replace('http://','')){$address=$true;break}}};if(!$address){throw 'Own exact URL required'};$owned+=@($w)};if($owned.Count-ne1){throw 'One exact nonce source window required'};$pidOwned=$owned[0].Current.ProcessId;$hwnd=$owned[0].Current.NativeWindowHandle;([System.Windows.Automation.WindowPattern]$owned[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)).Close();Start-Sleep -Milliseconds 250;$left=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith($prefix)});[pscustomobject]@{ownedSourceWindow=$left.Count;pid=$pidOwned;hwnd=$hwnd;titlePrefix=$prefix;url=$url}|ConvertTo-Json -Compress;if($left.Count){throw 'Own source remains'};""".replace('$prefix=TITLE;', '$prefix='+q(title)+';').replace('$url=URL;', '$url='+q(url)+';')
try:
 launch=ps("$ProgressPreference='SilentlyContinue';Start-Process msedge.exe -ArgumentList @('--new-window',"+q(url)+");[pscustomobject]@{nonce="+q(nonce)+";url="+q(url)+"}|ConvertTo-Json -Compress;",timeout=15);(base/'launch.log').write_text(launch)
 end=time.monotonic()+20;value=None
 while time.monotonic()<end:
  try:
   v=json.loads(record.read_text())
   if v.get('recording')==nonce and len(v.get('rows',[]))==(4 if a.expanded else 8):value=v;break
  except (FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.1)
 if value is None:raise RuntimeError('Fresh source nonce not recorded; no input replay')
 (base/'source.json').write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n');print('Fresh original source recorded',len(value['rows']),base,flush=True)
except Exception as e:
 (base/'failure.json').write_text(json.dumps({'phase':'UNVERIFIED','exception':str(e),'replayedInput':False},indent=2)+'\n');raise
finally:
 command=ROOT/'artifacts/replica/windows-download'/('home-connection-source-close-'+nonce+'.txt');command.write_text(close);sha=hashlib.sha256(command.read_bytes()).hexdigest().upper()
 loader="$ProgressPreference='SilentlyContinue';$f=Join-Path $env:TEMP "+q(command.name)+";try{Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+command.name)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Owned source close command hash mismatch'};& ([scriptblock]::Create([IO.File]::ReadAllText($f,[Text.Encoding]::UTF8)))}finally{Remove-Item $f -ErrorAction SilentlyContinue};"
 try:r=ps(loader,timeout=20)
 except Exception as e:
  (base/'cleanup-transport-failure.json').write_text(json.dumps({'phase':'CLOSE_UNVERIFIED','recordedRows':len(value['rows']) if value else 0,'exception':str(e),'commandSha256':sha,'next':'Fresh exactnonce readonly inventory before any guarded close; no measurement or unknown input replay'},indent=2)+'\n');raise
 (base/'cleanup.log').write_text(r)
 for line in r.splitlines():
  try:v=json.loads(line)
  except json.JSONDecodeError:continue
  if 'ownedSourceWindow'in v:(base/'cleanup.json').write_text(json.dumps(v,indent=2)+'\n');print('Exact source close0',v['ownedSourceWindow'],flush=True)
