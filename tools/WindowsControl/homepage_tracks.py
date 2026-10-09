"""Scoped WelcomeSteps revalidation inside Home: own source22 and cold native22."""
from pathlib import Path
import argparse,base64,hashlib,json,time
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source','auto','cleanup']);p.add_argument('--manifest',type=Path);a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home'
source="$A=[System.Windows.Automation.AutomationElement];$T=[System.Windows.Automation.TreeScope];$w=@($A::RootElement.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('HomePage tracks · original isolation')-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('\\Microsoft\\Edge\\Application\\msedge.exe')});"
if a.action=='source':
 out=r/'docs/replica/source/home-homepage-windows-control-ready.json';prior=out.stat().st_mtime_ns if out.exists() else 0
 print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/homepage-tracks.html');"))
 try:
  code=source+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{"+source+"if($w.Count-eq1){$b=$w[0].FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'homepage-tracks-record'));if($null-ne$b){break}};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$deadline);if($w.Count-ne1-or$null-eq$b){throw 'Own source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"
  print(ps(code));deadline=time.monotonic()+8
  while time.monotonic()<deadline:
   if out.exists() and out.stat().st_mtime_ns>prior:break
   time.sleep(.05)
  else:raise RuntimeError('Fresh source22 missing')
  v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==22;(r/'docs/replica/source/homepage-tracks-windows-source.json').write_bytes(out.read_bytes());print('Original22 recorded')
 finally:print(ps(source+"if($w.Count-eq1){$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()}elseif($w.Count){throw 'Own source uniqueness required'};"))
else:
 assert a.manifest is not None;m=json.loads(a.manifest.read_text());s=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='home' and m['component']=='welcomesteps' and m['runtime']=='win-x64' and m['buildExitCode']==0 and s['selfContained'];base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';$path=Join-Path $app 'ReplicaProbe.exe';"
 if a.action=='auto':
  f=r/'artifacts/replica/windows-download/homepage-tracks-cases.json';served='home-homepage-tracks-'+a.manifest.parent.name+'-cases.json';(f.parent/served).write_bytes(f.read_bytes());digest=hashlib.sha256(f.read_bytes()).hexdigest().upper()
  code=base+"$ProgressPreference='SilentlyContinue';$f=Join-Path $app 'homepage-tracks-cases.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest)+"-or(Get-FileHash (Join-Path $app 'ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(m['binarySha256'].upper())+"){throw 'Own cases/binary mismatch'};Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;$out=Join-Path $app 'welcomesteps-native.json';$prior=if(Test-Path $out){(Get-Item $out).LastWriteTimeUtc.Ticks}else{0};& $path '--welcomesteps' $f $app;if($LASTEXITCODE-ne0-or!(Test-Path $out)-or(Get-Item $out).LastWriteTimeUtc.Ticks-le$prior){throw 'Fresh own native22 missing'};"
  print(ps(code,timeout=60));out=r/'docs/replica/source/homepage-tracks-windows-native';out.mkdir(exist_ok=True);v=ps(base+"Get-Content -Raw (Join-Path $app 'welcomesteps-native.json')",retry_read=True);assert len(json.loads(v)['rows'])==22;(out/'welcomesteps-native.json').write_text(v+'\n')
  for n in ['welcomesteps-ko-824.png','welcomesteps-en-824.png']:(out/n).write_bytes(base64.b64decode(ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $app "+q(n)+")))",retry_read=True)))
  (out/'environment.json').write_text(json.dumps({'phase':'ACTUAL_WINDOWS','manifest':str(a.manifest.resolve()),'binarySha256':m['binarySha256'],'stage':s},indent=2)+'\n')
 value=json.loads(ps(base+source+"$native=@($A::RootElement.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq'Replica WelcomeSteps verification'-and(Get-Process -Id $_.Current.ProcessId).Path-eq$path});$p=@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$path});[pscustomobject]@{nativeProcess=$p.Count;nativeWindow=$native.Count;sourceWindow=$w.Count;path=$path}|ConvertTo-Json",retry_read=True));assert value['nativeProcess']==value['nativeWindow']==value['sourceWindow']==0;(r/'docs/replica/source/homepage-tracks-windows-cleanup.json').write_text(json.dumps(value,indent=2)+'\n');print('Exact cleanup0')
