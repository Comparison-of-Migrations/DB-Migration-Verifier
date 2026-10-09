"""Isolated Trial source/new native/headless evidence; OS input requires Home lease."""
from pathlib import Path
import argparse,base64,fcntl,hashlib,json,subprocess,time,uuid
from driver import ps,q,encoded_script,CLI,VM
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source','baseline-source','headless','headless-baseline','headless-table','headless-hover','auto','baseline','table','launch','inspect','inspect-view','inspect-raw','batch','resume-consent','hover','paint','close','cleanup']);p.add_argument('--manifest',type=Path,required=True);p.add_argument('--source',action='store_true');p.add_argument('--language',choices=['ko','en'],default='ko');p.add_argument('--hover',action='store_true');p.add_argument('--reduced',action='store_true');a=p.parse_args();m=json.loads(a.manifest.read_text());stage=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='home' and m['component']=='trialpage' and m['runtime']=='win-x64' and stage['selfContained'];lease=json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text());assert a.action.startswith('headless') or lease['resources']['windows']['owner']=='home'
base="$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';$path=Join-Path $app 'ReplicaProbe.exe';$A=[System.Windows.Automation.AutomationElement];$T=[System.Windows.Automation.TreeScope];"
sourceTitle='TrialPage Input · original runtime' if a.source else 'TrialPage inline baseline · original' if a.action=='baseline-source' else 'TrialPage · original isolation'
source="$w=@($A::RootElement.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(sourceTitle)+")-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('\\Microsoft\\Edge\\Application\\msedge.exe')});"
native="$w=@($A::RootElement.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq'Replica TrialPage verification'-and(Get-Process -Id $_.Current.ProcessId).Path-eq$path});"
guard=(source if a.source else native)+"if($w.Count-ne1){throw 'Exact owned Trial window required'};$w=$w[0];"
activate=guard+"Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;public static class TrialFrame{[DllImport(\"user32.dll\")]public static extern bool SetForegroundWindow(IntPtr h);[DllImport(\"user32.dll\")]public static extern IntPtr GetForegroundWindow();}';$trialHwnd=$w.Current.NativeWindowHandle;[TrialFrame]::SetForegroundWindow([IntPtr]$trialHwnd)|Out-Null;Start-Sleep -Milliseconds 250;if([TrialFrame]::GetForegroundWindow().ToInt64()-ne$trialHwnd){throw 'Exact Trial foreground required before provider read'};"+guard
out=a.manifest.parent/'actual-windows';out.mkdir(exist_ok=True)
def download(file):
 nonce=uuid.uuid4().hex;payload=(r/'artifacts/replica/windows-download'/file).read_bytes();served='home-trialpage-'+nonce+'-'+file;(r/'artifacts/replica/windows-download'/served).write_bytes(payload)
 return "$f=Join-Path $app "+q(file)+";Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(hashlib.sha256(payload).hexdigest().upper())+"){throw 'Owned fixture hash'};"
def fetch(file,folder=out):
 text=ps(base+"Get-Content -Encoding UTF8 -Raw (Join-Path $app "+q(file)+")",retry_read=True);json.loads(text);(folder/file).write_text(text+'\n');return json.loads(text)
def tracked_native_transport(code):
 encoded,staged=encoded_script(code);record={'commandSha256':hashlib.sha256(code.encode()).hexdigest(),'startedUtc':time.time(),'action':a.action,'scriptStaged':staged.name if staged else None}
 lock=r/'artifacts/replica/windows-command-transport.lock'
 try:
  with lock.open('a') as transport:
   fcntl.flock(transport,fcntl.LOCK_EX)
   try:
    result=subprocess.run([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],capture_output=True,timeout=60)
    record.update(returncode=result.returncode,stdout=result.stdout.decode('utf-8-sig',errors='replace'),stderr=result.stderr.decode('utf-8-sig',errors='replace'))
   except subprocess.TimeoutExpired as error:
    record.update(timeout=True,stdout=(error.stdout or b'').decode('utf-8-sig',errors='replace'),stderr=(error.stderr or b'').decode('utf-8-sig',errors='replace'));raise
   finally:
    record['finishedUtc']=time.time();(out/('native-transport-'+uuid.uuid4().hex+'.json')).write_text(json.dumps(record,ensure_ascii=False,indent=2));fcntl.flock(transport,fcntl.LOCK_UN)
  if result.returncode:raise RuntimeError('Owned native transport returncode '+str(result.returncode)+'; raw output preserved; no replay')
  return record['stdout']
 finally:
  if staged:staged.unlink(missing_ok=True)
if a.action in ['source','baseline-source']:
 file=r/'docs/replica/source'/('home-trialpage-windows-control-ready.json' if a.action=='baseline-source' else 'home-trialpage-windows-source.json');prior=file.stat().st_mtime_ns if file.exists() else 0;url='trialpage-baseline.html' if a.action=='baseline-source' else 'trialpage.html?record=1'
 print(ps(base+source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window',"+q('http://10.211.55.2:8768/'+url)+");"))
 deadline=time.monotonic()+40
 while time.monotonic()<deadline:
  if file.exists() and file.stat().st_mtime_ns>prior:break
  time.sleep(.1)
 else:raise RuntimeError('Fresh source missing; preserve own window for diagnosis')
 v=json.loads(file.read_text());assert v['dpr']==2 and len(v['rows'])==(16 if a.action=='baseline-source' else 80);dest=r/'docs/replica/source'/('trialpage-windows-baseline.json' if a.action=='baseline-source' else 'trialpage-windows-source.json');dest.write_bytes(file.read_bytes());print(ps(base+source+"if($w.Count-ne1){throw 'Owned source close uniqueness'};$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"));print(dest)
elif a.action in ['auto','baseline','table','headless','headless-baseline','headless-table','headless-hover']:
 kind='baseline' if a.action in ['baseline','headless-baseline'] else 'table' if a.action in ['table','headless-table'] else 'hover' if a.action=='headless-hover' else '';file='trialpage-table-cases.json' if kind=='hover' else 'trialpage-'+kind+'-cases.json' if kind else 'trialpage-cases.json';name='trialpage-'+kind+'-native.json' if kind else 'trialpage-native.json';code=base+"$ProgressPreference='SilentlyContinue';"+download(file)+"if((Get-FileHash ($app+'\\ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(m['binarySha256'].upper())+"){throw 'Binary hash'};"
 if a.action.startswith('headless'):
  code+="$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';& $path '--trialpage' $f $app"+(' '+kind if kind else '')+";if($LASTEXITCODE-ne0){throw 'Trial headless runtime failed'};Remove-Item Env:DBMV_HEADLESS,Env:DBMV_HEADLESS_SCALE -ErrorAction SilentlyContinue;"
 else:
  scope=a.action+'-'+uuid.uuid4().hex;code+="Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;if(@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$path}).Count){throw 'Owned runtime must be closed'};$stdout=Join-Path $app "+q(scope+'-stdout.txt')+";$stderr=Join-Path $app "+q(scope+'-stderr.txt')+";$p=Start-Process $path -WorkingDirectory $app -ArgumentList @('--trialpage',$f,$app"+(','+q(kind) if kind else '')+") -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru;$identity=[pscustomobject]@{pid=$p.Id;requestedPath=$path;scope="+q(scope)+";binarySha256="+q(m['binarySha256'])+";stdoutPath=$stdout;stderrPath=$stderr;createdUtc=[DateTime]::UtcNow.ToString('o')};$identity|ConvertTo-Json|Set-Content -Encoding UTF8 (Join-Path $app 'trialpage-launch-identity.json');$identity|ConvertTo-Json;$processHandle=$p.Handle;$done=$p.WaitForExit(40000);if(!$done){throw 'Owned runtime still running; retain launch identity, no replay'};$p.Refresh();[pscustomobject]@{scope="+q(scope)+";pid=$p.Id;exitCode=$p.ExitCode;stdout=[IO.File]::ReadAllText($stdout);stderr=[IO.File]::ReadAllText($stderr)}|ConvertTo-Json -Depth 4;if($p.ExitCode-ne0){throw 'Owned runtime exited with failure; preserved stdout/stderr'};"
 print(ps(code,timeout=60) if a.action.startswith('headless') else tracked_native_transport(code));folder=a.manifest.parent/'windows-headless' if a.action.startswith('headless') else out;folder.mkdir(exist_ok=True);fetch(name,folder)
 for png in (['trialpage-table-ko-1280-default.png','trialpage-table-en-1280-default.png'] if kind=='table' else ['trialpage-hover-ko-1280-default.png','trialpage-hover-en-1280-default.png'] if kind=='hover' else ['trialpage-ko-1280-default.png','trialpage-en-960-both.png'] if not kind else []):(folder/png).write_bytes(base64.b64decode(ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $app "+q(png)+")))",retry_read=True)))
 (folder/('environment-'+a.action+'.json')).write_text(json.dumps({'phase':a.action,'manifest':str(a.manifest.resolve()),'stage':stage,'binarySha256':m['binarySha256']},indent=2));print(folder)
elif a.action=='launch':
 if a.source:print(ps(base+source+"if($w.Count){throw 'Own Trial source already open'};Start-Process msedge.exe -ArgumentList @('--new-window',"+q('http://10.211.55.2:8768/trialpage-input.html'+('?reduced=1' if a.hover and a.reduced else ''))+");"))
 else:
  cases=r/'artifacts/replica/windows-download/trialpage-input-cases.json';cases.write_bytes((r/'artifacts/replica/workers/home/trialpage/ec53805086a04095ab4d7b7605a9c14d/actual-macos/input-cases.json').read_bytes());print(ps(base+native+"if($w.Count){throw 'Own Trial native already open'};$ProgressPreference='SilentlyContinue';"+download(cases.name)+"Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;$p=Start-Process $path -ArgumentList @('--trialpage-window',$f,$app,"+q('hover-reduced' if a.hover and a.reduced else 'hover-normal' if a.hover else 'input')+") -PassThru;$p.Id;"))
elif a.action in ['batch','resume-consent','hover','paint']:
 helper=r/'tools/WindowsControl/trialpage_input.cs';served='home-trialpage-input-'+uuid.uuid4().hex+'.txt';(r/'artifacts/replica/windows-download'/served).write_bytes(helper.read_bytes());load="$ProgressPreference='SilentlyContinue';$helper=Join-Path $app "+q(served)+";Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $helper;if((Get-FileHash $helper -Algorithm SHA256).Hash-ne "+q(hashlib.sha256(helper.read_bytes()).hexdigest().upper())+"){throw 'Owned actor hash'};Add-Type -TypeDefinition ([IO.File]::ReadAllText($helper)) -ReferencedAssemblies @('UIAutomationClient','UIAutomationTypes','WindowsBase','System.Drawing');"
 code=base+load+activate
 if a.action=='paint':
  label=('source-' if a.source else 'native-')+a.language+'-paint';print(ps(code+"[TrialPaint]::Capture($app,$"+str(a.source).lower()+","+q(label)+");"));(out/(label+'.png')).write_bytes(base64.b64decode(ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $app "+q(label+'.png')+")))",retry_read=True)));raise SystemExit(0)
 if a.action=='resume-consent':
  assert a.source and a.language=='en'
  code+=guard+"$b=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'TrialPage-next'));for($i=0;$i-lt4;$i++){$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep -Milliseconds 150;}"
 elif a.language=='en':code+=guard+"$b=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,"+q('TrialPage-next' if a.source else 'TrialPage-setup')+"));$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep -Milliseconds 200;"
 code+=guard+"$trialHost=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'trial-host'));if(!$w.Current.BoundingRectangle.Contains($trialHost.Current.BoundingRectangle)){$scroll=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'TrialPage-scroll'));$scroll.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep -Milliseconds 200;}"
 if a.action=='hover':
  for mode in ['hover','leave','cancel']:
   label=('source-' if a.source else 'native-')+a.language+('-reduced-' if a.reduced else '-normal-')+mode
   code+="[OwnedTrialInput]::Hover($app,$"+str(a.source).lower()+","+q(mode)+");"
   if a.source:code+="$state=Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/home-trialpage-windows-input-latest.json';[IO.File]::WriteAllText((Join-Path $app "+q(label+'.json')+"),([Text.Encoding]::UTF8.GetString($state.RawContentStream.ToArray())));"
   else:code+="[IO.File]::Copy((Join-Path $app 'trialpage-input.json'),(Join-Path $app "+q(label+'.json')+"),$true);"
  print(ps(code,timeout=60))
  for mode in ['hover','leave','cancel']:fetch(('source-' if a.source else 'native-')+a.language+('-reduced-' if a.reduced else '-normal-')+mode+'.json')
  raise SystemExit(0)
 for i,(target,mode) in enumerate([(x,'edit')for x in ['trial-host','trial-db','trial-validInput','trial-invalidInput']]+[('flag-trial.consent',x)for x in ['pointer','space','enter']]):
  if a.action=='resume-consent' and i<4:continue
  if a.source and mode=='pointer':code+=guard+"$b=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'TrialPage-consent-scroll'));$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep -Milliseconds 200;"
  label=('source-' if a.source else 'native-')+a.language+'-'+('field'+str(i) if i<4 else mode)
  if i==2:code+=guard+"$b=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'TrialPage-scroll'));$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep -Milliseconds 200;"
  code+="[OwnedTrialInput]::Run($app,$"+str(a.source).lower()+","+q(target)+","+q(mode)+");"
  if not a.source:code+="[IO.File]::Copy((Join-Path $app 'trialpage-input.json'),(Join-Path $app "+q(label+'.json')+"),$true);"
  else:code+="$state=Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/home-trialpage-windows-input-latest.json';[IO.File]::WriteAllText((Join-Path $app "+q(label+'.json')+"),([Text.Encoding]::UTF8.GetString($state.RawContentStream.ToArray())));"
 print(ps(code,timeout=60))
 for name in (['pointer','space','enter'] if a.action=='resume-consent' else ['field0','field1','field2','field3','pointer','space','enter']):fetch(('source-' if a.source else 'native-')+a.language+'-'+name+'.json')
elif a.action in ['inspect','inspect-view','inspect-raw']:
 code=base+activate+"$nodes=[Collections.Generic.List[object]]::new();function Walk($e,$d){if($d-gt40){return};$c=$e.Current;$b=$c.BoundingRectangle;$children=@($e.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition));$nodes.Add([pscustomobject]@{name=$c.Name;id=$c.AutomationId;type=$c.ControlType.ProgrammaticName;className=$c.ClassName;localized=$c.LocalizedControlType;isControl=$c.IsControlElement;isContent=$c.IsContentElement;children=$children.Count;box=@{x=$b.X;y=$b.Y;width=$b.Width;height=$b.Height}});foreach($child in $children){Walk $child ($d+1)}};Walk $w 0;$nodes|ConvertTo-Json -Depth 8;";
 if a.action in ['inspect-view','inspect-raw']:code=code.replace("$children=@($e.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition));","$children=@();$walker=[System.Windows.Automation.TreeWalker]::ControlViewWalker;$child=$walker.GetFirstChild($e);while($null-ne$child){$children+=,$child;$child=$walker.GetNextSibling($child);}")
 if a.action=='inspect-raw':code=code.replace('::ControlViewWalker','::RawViewWalker')
 v=ps(code,retry_read=True);(out/(('source-' if a.source else 'native-')+a.language+('-controlview-ax.json' if a.action=='inspect-view' else '-rawview-ax.json' if a.action=='inspect-raw' else '-ax.json'))).write_text(v+'\n')
elif a.action=='close':print(ps(base+guard+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
else:
 code=base+native+"$p=@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$path});$source=@($A::RootElement.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('TrialPage')-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('\\Microsoft\\Edge\\Application\\msedge.exe')});[pscustomobject]@{nativeProcess=$p.Count;nativeWindow=$w.Count;sourceWindow=$source.Count;path=$path}|ConvertTo-Json;";v=json.loads(ps(code,retry_read=True));(out/'cleanup.json').write_text(json.dumps(v,indent=2));assert v['nativeProcess']==v['nativeWindow']==v['sourceWindow']==0;print(v)
