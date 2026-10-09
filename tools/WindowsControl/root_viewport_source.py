from pathlib import Path
import json,time,uuid,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='root';nonce=uuid.uuid4().hex;out=r/'artifacts/replica/workers/root/pageintegration/root-viewport-source'/nonce;out.mkdir(parents=True);endpoint=r/'docs/replica/source/home-pageintegrationtrial-windows-control-ready.json';guard="$wins=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('Trial root viewport diagnostic · home')-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});"
code=guard+"if($wins.Count){throw 'Own source already active'};Start-Process msedge.exe -ArgumentList @('--new-window','--force-device-scale-factor=2',"+q('http://10.211.55.2:8768/home-pageintegrationtrial-rootviewport.html?recording='+nonce)+");[Console]::WriteLine('OWNED_ROOT_SOURCE_REQUESTED');"
f=r/'artifacts/replica/windows-download'/('root-viewport-source-'+nonce+'.txt');f.write_text(code);sha=hashlib.sha256(f.read_bytes()).hexdigest().upper();loader="$f=Join-Path $env:TEMP "+q(f.name)+";Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+f.name)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Own SHA changed'};&([scriptblock]::Create([IO.File]::ReadAllText($f)));"
try:
 raw=ps(loader,timeout=15);(out/'launch.txt').write_text(raw);end=time.monotonic()+8
 while time.monotonic()<end:
  if endpoint.exists():
   v=json.loads(endpoint.read_text())
   if v.get('recording')==nonce:
    (out/'source.json').write_text(json.dumps(v,indent=2)+'\n');assert len(v['rows'])==6 and v['dpr']==2;print(out);break
  time.sleep(.025)
 else:raise RuntimeError('Fresh root source missing; no repeat launch')
except Exception as e:(out/'failure.txt').write_text(str(e));raise
finally:
 raw=ps(guard+"if($wins.Count-ne1){throw 'Exact source close must be diagnosed, no broad close'};$wins[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();Start-Sleep -Milliseconds 120;"+guard+"if($wins.Count){throw 'Source close failure'};[Console]::WriteLine('OWNED_SOURCE_WINDOW0');",timeout=15);(out/'cleanup.json').write_text(json.dumps({'sourceWindow':0,'actorExit':0,'result':raw},indent=2)+'\n')
