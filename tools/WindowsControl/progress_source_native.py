"""Only missing trusted Windows original hover/live; keep former80 source/layout."""
from pathlib import Path
import json,time,hashlib,uuid
from driver import ps,q
r=Path(__file__).resolve().parents[2];assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='root';out=r/'artifacts/replica/workers/root/progresspage/source-windows-input'/uuid.uuid4().hex;out.mkdir(parents=True);endpoint=r/'docs/replica/source/root-progresspage-windows-input.json';helper=(r/'tools/WindowsControl/progress_source_input.cs').read_text();guard="$wins=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('Progress input · original isolation')-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});"
def command(code):
 n='root-progress-source-command-'+uuid.uuid4().hex+'.txt';file=r/'artifacts/replica/windows-download'/n;file.write_text(code);sha=hashlib.sha256(file.read_bytes()).hexdigest().upper();return ps("$f=Join-Path $env:TEMP "+q(n)+";Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+n)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Own command hash mismatch'};try{&([scriptblock]::Create([IO.File]::ReadAllText($f)))}finally{Remove-Item $f -ErrorAction SilentlyContinue}",timeout=18)
def read(label,test,before=0):
 end=time.monotonic()+5
 while time.monotonic()<end:
  if endpoint.exists()and endpoint.stat().st_mtime_ns>before:
   v=json.loads(endpoint.read_text())
   if test(v):
    (out/(label+'.json')).write_text(json.dumps(v,indent=2)+'\n');return v
  time.sleep(.025)
 raise RuntimeError('Fresh source predicate failed '+label+'; do not repeat input')
checks=[];opened=False
try:
 before=endpoint.stat().st_mtime_ns if endpoint.exists()else 0
 result=command(guard+"if($wins.Count){throw 'Own original already open'};Start-Process msedge.exe -ArgumentList @('--new-window','--force-device-scale-factor=2',"+q('http://10.211.55.2:8768/root-progresspage-input-windows.html?os=windows&recording='+out.name)+");[Console]::WriteLine('OWNED_SOURCE_OPEN_REQUESTED');");opened=True;(out/'launch.txt').write_text(result);read('initial',lambda v:v['state']['phase']=='initial'and v['state']['dpr']==2,before)
 for mode,label,test in [('hover','hover',lambda v:v['state']['rows'][0]['hover']and v['state']['rows'][0]['background']=='rgb(58, 60, 68)'),('leave','leave',lambda v:not v['state']['rows'][0]['hover']and v['state']['rows'][0]['background']=='rgba(0, 0, 0, 0)'),('live','live',lambda v:v['state']['phase']=='live'and len(v['frames'])>5 and v['frames'][-1]['ms']>=650),('language','language',lambda v:v['state']['lang']=='en')]:
  before=endpoint.stat().st_mtime_ns;(out/('requested-'+label+'.json')).write_text(json.dumps({'mode':mode,'requested':True,'time':time.time()})+'\n');raw=command("Add-Type -TypeDefinition "+q(helper)+" -ReferencedAssemblies @('UIAutomationClient','UIAutomationTypes','WindowsBase');[ProgressSourceInput]::Run("+q(mode)+");");(out/('actor-'+label+'.txt')).write_text(raw);v=read(label,test,before);checks.append({'label':label,'passed':True})
  if label=='live':
   for i in range(4):
    states=[x['state']['rows'][i] for x in v['frames']];assert any(row['cells'][1]['opacity']<.999 and row['cells'][1]['transform']!='none' for row in states),i;assert all(row['pillOpacity']==1 for row in states);assert states[-1]['cells'][1]['opacity']==1
   checks.append({'label':'firstLiveCell4/PillOpacity1','passed':True,'frames':len(v['frames'])})
 (out/'comparison.json').write_text(json.dumps({'passed':True,'checks':checks,'scope':'Trusted source mouse hover/leave and real click firstlive/language, diagnostic timer never changes focus'},indent=2)+'\n');print(out)
except Exception as e:(out/'failure.txt').write_text(str(e));raise
finally:
 if opened:
  raw=command(guard+"if($wins.Count-ne1){throw 'Exact owned source close required'};$wins[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();Start-Sleep -Milliseconds 100;"+guard+"if($wins.Count){throw 'Owned source not closed'};[Console]::WriteLine('OWNED_SOURCE_WINDOW0');");(out/'cleanup.json').write_text(json.dumps({'sourceWindow':0,'actorExit':0,'result':raw,'browserProcess':'Shared Edge process left untouched'},indent=2)+'\n')
