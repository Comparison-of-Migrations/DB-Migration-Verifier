"""Owned actual Windows Progress measurements; never retry uncertain input."""
from pathlib import Path
import argparse,base64,hashlib,io,json,time,uuid,zipfile
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('action',choices=['source','launch','input','read','ax','close','retrieve']);p.add_argument('--mode',choices=['hover','leave','F5','F6','F7','F8','F9','F10','F11'],default='F5');p.add_argument('--label',default='initial');a=p.parse_args();m=json.loads(a.manifest.read_text());s=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='root' and m['component']=='progresspage' and s['selfContained'];assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='root'
base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';"
find=base+"$own=@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')});$w=@($own|Where-Object{$_.MainWindowHandle-ne0}|ForEach-Object{[System.Windows.Automation.AutomationElement]::FromHandle($_.MainWindowHandle)}|Where-Object{$_.Current.Name-eq 'Replica ProgressPage verification'});"
unique="if($w.Count-ne1){throw 'Unique own Progress required'};$w=$w[0];"
read="function ReadShared($path){$f=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete);$s=[IO.StreamReader]::new($f);try{$s.ReadToEnd()}finally{$s.Dispose()}};"
out=a.manifest.parent/'actual-windows';out.mkdir(exist_ok=True);current=a.manifest.parent/'current-windows.json'
if a.action=='source':
 endpoint=r/'docs/replica/source/root-progresspage-windows-source.json';prior=endpoint.stat().st_mtime_ns if endpoint.exists()else 0
 guard="$sw=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('Progress page · original isolation')-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});"
 print(ps(guard+"if($sw.Count){throw 'Owned original already open'};Start-Process msedge.exe -ArgumentList @('--new-window','--force-device-scale-factor=2','http://10.211.55.2:8768/progresspage.html?os=windows');"))
 end=time.monotonic()+15
 while time.monotonic()<end:
  if endpoint.exists()and endpoint.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('Actual source80 not recorded; inspect own source without repeating')
 v=json.loads(endpoint.read_text());assert v['dpr']==2 and len(v['rows'])==80;(out/'source80.json').write_bytes(endpoint.read_bytes());tree=guard+"if($sw.Count-ne1){throw 'Original exact peer window missing'};$table=$sw[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Table));if($null-eq$table){throw 'Visible source Table missing'};function Tree($e,$d){if($d-gt8){return $null};@{name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;children=@($e.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|ForEach-Object{Tree $_ ($d+1)})}};Tree $table 0|ConvertTo-Json -Depth 30;";raw=ps(tree,retry_read=True);json.loads(raw);(out/'source-table-ax.json').write_text(raw+'\n');print(ps(guard+"if($sw.Count-ne1){throw 'Original exact close missing'};$sw[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 print('Actual Windows original80 recorded and scoped close')
elif a.action=='launch':
 nonce=uuid.uuid4().hex;current.write_text(json.dumps({'nonce':nonce,'output':str(out/nonce)},indent=2)+'\n');(out/nonce).mkdir();data=(r/'docs/replica/source/root-progresspage-cases.json').read_bytes();name='root-progresspage-actual-'+nonce+'-cases.json';(r/'artifacts/replica/windows-download'/name).write_bytes(data)
 code=find+"if($w.Count){throw 'Own Progress already open'};$out=Join-Path $app "+q('actual-'+nonce)+";New-Item -ItemType Directory $out -Force|Out-Null;$cases=Join-Path $out 'cases.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+name)+" -OutFile $cases;if((Get-FileHash $cases -Algorithm SHA256).Hash-ne "+q(hashlib.sha256(data).hexdigest().upper())+"){throw 'Cases changed'};if((Get-FileHash ($app+'\\ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(m['binarySha256'].upper())+"){throw 'Owned binary changed'};Remove-Item Env:DBMV_HEADLESS,Env:DBMV_HEADLESS_SCALE -ErrorAction SilentlyContinue;$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--progresspage-window',$cases,$out) -WorkingDirectory $app -RedirectStandardOutput ($out+'\\native.stdout.log') -RedirectStandardError ($out+'\\native.stderr.log') -PassThru;@{pid=$p.Id;requestedPath=$app+'\\ReplicaProbe.exe';scope='owned hash-guarded native launch';started=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -Encoding UTF8 ($out+'\\launch-identity.json');$p.Id;"
 pid=int(ps(code));v=json.loads(current.read_text());v['pid']=pid;v['requestedGuestPath']=s['guestFolder']+'\\app\\ReplicaProbe.exe';v['binarySha256']=m['binarySha256'];current.write_text(json.dumps(v,indent=2)+'\n');print({'pid':pid,'output':v['output']})
else:
 v=json.loads(current.read_text());output=Path(v['output']);guest="$out=Join-Path $app "+q('actual-'+v['nonce'])+";"
 if a.action=='input':
  helper=(r/'tools/WindowsControl/owned_progress_page_input.cs').read_text();print(ps(base+"Add-Type -TypeDefinition "+q(helper)+" -ReferencedAssemblies @('UIAutomationClient','UIAutomationTypes','WindowsBase');[OwnedProgressInput]::Run($app,"+q(a.mode)+");"))
 elif a.action=='read':
  raw=ps(find+unique+guest+read+"$end=[DateTime]::UtcNow.AddSeconds(8);do{if(Test-Path($out+'\\progresspage-actual-state.json')){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$end);ReadShared($out+'\\progresspage-actual-state.json');",retry_read=True);json.loads(raw);(output/('state-'+a.label+'.json')).write_text(raw+'\n');print(raw)
 elif a.action=='ax':
  code=find+unique+"function Tree($e,$d){if($d-gt10){return $null};@{id=$e.Current.AutomationId;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;box=@{x=$e.Current.BoundingRectangle.X;y=$e.Current.BoundingRectangle.Y;width=$e.Current.BoundingRectangle.Width;height=$e.Current.BoundingRectangle.Height};children=@($e.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|ForEach-Object{Tree $_ ($d+1)})}};Tree $w 0|ConvertTo-Json -Depth 35;"
  raw=ps(code,retry_read=True);json.loads(raw);(output/('ax-'+a.label+'.json')).write_text(raw+'\n');print('Saved actual AX')
 elif a.action=='close':print(ps(find+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 else:
  code=base+guest+"$n=@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq($app+'\\ReplicaProbe.exe')}).Count;if($n-ne0){throw 'Own native still running'};@{nativeProcess=0;nativeWindow=0}|ConvertTo-Json|Set-Content -Encoding UTF8($out+'\\cleanup.json');Add-Type -AssemblyName System.IO.Compression.FileSystem;$zip=$out+'.zip';[IO.Compression.ZipFile]::CreateFromDirectory($out,$zip);[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip));"
  raw=base64.b64decode(ps(code,timeout=60))
  with zipfile.ZipFile(io.BytesIO(raw))as z:
   for n in z.namelist():
    assert Path(n).name==n
    if n.endswith('.json'):(output/n).write_text(z.read(n).decode('utf-8-sig'))
    else:(output/n).write_bytes(z.read(n))
  print(output)
