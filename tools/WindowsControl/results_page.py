"""Cold owned ResultsPage rendering/source/passive peer; approved official guest UIA only."""
from pathlib import Path
import argparse,json,re,hashlib,time,base64
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','source-peer','launch','read','peer','capture','close','input-batch','input-first','source-input-batch','source-filter-focus','focus-step','source-focus-step','focus-read','track-read','trial-show','tracks-record','reduced-mode','source-reduced']);p.add_argument('--stage',type=Path);p.add_argument('--lifecycle',action='store_true');p.add_argument('--tracks',action='store_true');p.add_argument('--geometry-only',action='store_true');p.add_argument('--interactive-only',action='store_true');p.add_argument('--reduced',action='store_true');p.add_argument('--skip-hover',action='store_true');p.add_argument('--focus-index',type=int,choices=[1,2,3]);p.add_argument('--focus-input',action='store_true');a=p.parse_args()
if a.action!='focus-read' and json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']!='connection':raise RuntimeError('Connection Windows resource required')
base='';native=not (a.action.startswith('source-')or a.action in ['trial-show','tracks-record','source-reduced'])
if native or a.stage:
 if not a.stage:raise RuntimeError('Owned full stage required')
 s=json.loads(a.stage.read_text())
 if s['owner']!='connection'or s['component']!='resultspage'or not s['selfContained']or not re.fullmatch(r'DBMV-Isolated-connection-resultspage-[0-9a-f]{32}',s['guestFolder']):raise RuntimeError('Owned isolated ResultsPage stage required')
 base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';"
title='ResultsPage tracks · original isolation'if a.tracks else'ResultsPage input · original isolation'if a.lifecycle else'ResultsPage · original isolation'
owned=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{"+("$_.Current.Name-eq 'Replica ResultsPage verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"if native else"([string]$_.Current.Name).StartsWith("+q(title)+") -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')")+"});if($w.Count-ne 1){throw 'Unique owned ResultsPage window required'};$w=$w[0];"
shared_read="function ReadResultsShared([string]$file){$stream=[IO.File]::Open($file,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete));$reader=[IO.StreamReader]::new($stream,[Text.Encoding]::UTF8);try{return $reader.ReadToEnd()}finally{$reader.Dispose();$stream.Dispose()}};"
peer_code="""
function Node($e){$hp=@($e.GetSupportedProperties()|Where-Object{$_.ProgrammaticName-like '*HeadingLevel*'});[pscustomobject]@{name=$e.Current.Name;id=$e.Current.AutomationId;type=$e.Current.ControlType.ProgrammaticName;content=$e.Current.IsContentElement;control=$e.Current.IsControlElement;headingProperty=if($hp.Count){$hp[0].ProgrammaticName}else{$null};headingLevel=if($hp.Count){$e.GetCurrentPropertyValue($hp[0]).ToString()}else{$null}}}
function ContentNodes($e,$depth){if($depth-gt20){throw 'Owned content peer depth guard'};$walker=[System.Windows.Automation.TreeWalker]::ContentViewWalker;$c=$walker.GetFirstChild($e);while($null-ne$c){Node $c;ContentNodes $c ($depth+1);$c=$walker.GetNextSibling($c)}}
$all=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition));$buttons=@($all|Where-Object{$_.Current.ControlType-eq[System.Windows.Automation.ControlType]::Button-and($_.Current.AutomationId-like 'Results-*'-or$_.Current.Name-like '*시험 기대*'-or$_.Current.Name-in@('연구 연결','전체','불일치','확인 불가·미검증'))}|ForEach-Object{$e=$_;[pscustomobject]@{peer=(Node $e);rawChildren=@($e.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|ForEach-Object{Node $_});contentDescendants=@(ContentNodes $e 0)}});$headings=@($all|Where-Object{$_.Current.Name-in@('별도 DB 동작 시험','확인할 차이와 미검증 이유','Behavior test · Separate DB','Differences and reasons for gaps')}|ForEach-Object{Node $_});[pscustomobject]@{buttons=$buttons;headings=$headings}|ConvertTo-Json -Depth 15;
"""
if a.action=='source-open':
 guard="$existing=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{([string]$_.Current.Name).StartsWith("+q(title)+") -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});if($existing.Count){throw 'Owned source already open; inspect rather than duplicate launch'};"
 print(ps(guard+"Start-Process msedge.exe -ArgumentList @('--new-window',"+q('http://10.211.55.2:8768/resultspage'+('-tracks'if a.tracks else'-input'if a.lifecycle else'')+'.html?os=windows')+");"))
elif a.action=='source-record':
 file=r/('docs/replica/source/connection-resultspage-windows-'+('input'if a.lifecycle else'source')+'.json');before=file.stat().st_mtime_ns if file.exists()else 0
 try:
  print(ps(owned+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('resultspage-finite'if a.lifecycle else'resultspage-record')+"));if($null-eq $c-or !$c.Current.IsEnabled-or $c.Current.Name-ne "+q('Run ResultsPage finite'if a.lifecycle else'Record ResultsPage')+"){throw 'Fresh fixed record target missing; inspect existing evidence'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 except RuntimeError:
  if not file.exists()or file.stat().st_mtime_ns<=before:raise
  print('Ambiguous CLI return resolved by fresh owned source evidence; no duplicate invocation')
 end=time.monotonic()+6
 while time.monotonic()<end:
  if file.exists()and file.stat().st_mtime_ns>before:break
  time.sleep(.05)
 else:raise RuntimeError('Fresh source record missing')
 data=json.loads(file.read_text());(r/('docs/replica/source/resultspage-windows-'+('lifecycle-source'if a.lifecycle else'source')+'.json')).write_text(json.dumps(data,indent=2)+'\n')
 if not a.lifecycle:(r/'artifacts/replica/windows-download/resultspage-source.json').write_bytes(file.read_bytes())
 print('Recorded',len(data['rows']))
elif a.action=='source-peer':
 code=owned+peer_code
 data=ps(code,retry_read=True);(r/'docs/replica/source/resultspage-windows-source-peer.json').write_text(data);print(data)
elif a.action=='launch':
 f=r/'artifacts/replica/windows-download/resultspage-source.json';digest=hashlib.sha256(f.read_bytes()).hexdigest()
 print(ps(base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime already active; inspect instead of relaunch'};$f=Join-Path $app 'resultspage-source.json';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/resultspage-source.json' -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Case hash mismatch'};Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;Remove-Item Env:DBMV_HEADLESS_SCALE -ErrorAction SilentlyContinue;$started=[DateTime]::UtcNow;Start-Process -FilePath ($app+'\\ReplicaProbe.exe') -ArgumentList @('--resultspage-window',$f,$app"+(",'--interactive-only'"if a.interactive_only else""if a.geometry_only else",'--finite'")+",'--trial-tracks'"+(",'--focus-input'"if a.focus_input else"")+") -WorkingDirectory $app;$deadline=[DateTime]::UtcNow.AddSeconds(25);do{$ws=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica ResultsPage verification'-and(Get-Process -Id $_.Current.ProcessId).Path-eq($app+'\\ReplicaProbe.exe')});$record=Join-Path $app 'resultspage-input.json';if($ws.Count-eq1-and(Test-Path $record)-and(Get-Item $record).LastWriteTimeUtc-ge$started){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow-lt$deadline);if($ws.Count-ne1-or!(Test-Path $record)){throw 'Owned ResultsPage startup incomplete; inspect without relaunch'};[Console]::WriteLine('Fresh owned ResultsPage requested native scope ready');"))
elif a.action=='read':
 out=r/'docs/replica/source/resultspage-windows-native';out.mkdir(exist_ok=True)
 for name in (['resultspage-native.json','resultspage-input.json']if a.geometry_only else['resultspage-native.json','resultspage-lifecycle.json','resultspage-input.json']):
  data=ps(owned+"Get-Content -Raw (Join-Path $app "+q(name)+");",retry_read=True);json.loads(data);(out/name).write_text(data)
 for name in ['resultspage-en-1000-326-trial-pending.png','resultspage-en-1280-656-sample-unknown.png','resultspage-ko-1280-904-trial-mixed.png']:
  data=ps(owned+"$file=Join-Path $app "+q(name)+";if(Test-Path $file){[Convert]::ToBase64String([IO.File]::ReadAllBytes($file))}else{[Console]::WriteLine('Missing scoped PNG')}",retry_read=True)
  if a.geometry_only and data.strip()=='Missing scoped PNG':continue
  (out/name).write_bytes(base64.b64decode(data))
 print('Requested native geometry/evidence saved')
elif a.action=='peer':
 code=owned+peer_code
 data=ps(code,retry_read=True);(r/'docs/replica/source/resultspage-windows-peer.json').write_text(data);print(data)

elif a.action=='capture':
 code=owned+"Add-Type -AssemblyName System.Drawing;Add-Type 'using System;using System.Runtime.InteropServices;public static class ResultsPageCaptureGuard{[DllImport(\"user32.dll\")]public static extern bool SetForegroundWindow(IntPtr h);[DllImport(\"user32.dll\")]public static extern IntPtr GetForegroundWindow();[DllImport(\"user32.dll\")]public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr c);}';$dpi=[ResultsPageCaptureGuard]::SetThreadDpiAwarenessContext([IntPtr](-4));try{[ResultsPageCaptureGuard]::SetForegroundWindow([IntPtr]$w.Current.NativeWindowHandle)|Out-Null;if([ResultsPageCaptureGuard]::GetForegroundWindow().ToInt64()-ne$w.Current.NativeWindowHandle){throw 'Owned capture foreground mismatch'};$r=$w.Current.BoundingRectangle;if($r.Width-lt500-or$r.Width-gt4000-or$r.Height-lt400-or$r.Height-gt2400){throw 'Owned capture bounds mismatch'};$b=[Drawing.Bitmap]::new([int]$r.Width,[int]$r.Height);$g=[Drawing.Graphics]::FromImage($b);$g.CopyFromScreen([int]$r.Left,[int]$r.Top,0,0,$b.Size);$m=[IO.MemoryStream]::new();$b.Save($m,[Drawing.Imaging.ImageFormat]::Png);[Console]::WriteLine([Convert]::ToBase64String($m.ToArray()));$g.Dispose();$b.Dispose();$m.Dispose();}finally{if($dpi-ne[IntPtr]::Zero){[ResultsPageCaptureGuard]::SetThreadDpiAwarenessContext($dpi)|Out-Null}}"
 out=r/('docs/replica/source/resultspage-windows-focus'if a.focus_input else'docs/replica/source/resultspage-windows-native');out.mkdir(exist_ok=True);(out/'native-frame.png').write_bytes(base64.b64decode(ps(code)));print('Owned actual native capture saved')

elif a.action in ['trial-show','tracks-record','reduced-mode','source-reduced']:
 target='Results-trial-reduced'if a.action=='reduced-mode'else'resultspage-trial-reduced'if a.action=='source-reduced'else'resultspage-trial-scroll'if a.action=='trial-show'else'resultspage-tracks-record'
 print(ps(owned+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(target)+"));if($null-eq$c-or!$c.Current.IsEnabled){throw 'Exact source diagnostic missing'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
elif a.action in ['input-batch','input-first','source-input-batch','source-filter-focus','focus-step','source-focus-step']:
 encoded=base64.b64encode((r/'tools/WindowsControl/results_page_mouse.cs').read_bytes()).decode()
 helper="Add-Type -TypeDefinition ([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String("+q(encoded)+"))) -ReferencedAssemblies @('UIAutomationClient','UIAutomationTypes','WindowsBase');"
 code=owned+helper+shared_read
 if a.action in ['focus-step','source-focus-step'] and a.focus_index==2:code+="$current=[System.Windows.Automation.AutomationElement]::FocusedElement;if($null-eq$current-or$current.Current.ProcessId-ne$w.Current.ProcessId-or$current.Current.Name-ne '확인 불가·미검증'){throw 'Exact current filter focus required; do not focus diagnostics'};"
 request_count=(a.focus_index-1)if a.action in ['focus-step','source-focus-step']else 0
 for target,mode in ([{1:('filter-unknown','click'),2:('current','space'),3:('filter-unknown','hold')}[a.focus_index]]if a.action in ['focus-step','source-focus-step']else[('filter-unknown','click')] if a.action in ['input-first','source-filter-focus'] else [('trial','hover'),('trial','cancel'),('trial','hold')]if a.reduced else[('trial','hover'),('trial','leave'),('trial','reverse'),('trial','cancel'),('trial','hold'),('current','escape')]if a.action=='source-input-batch'else [('filter-unknown','click'),('current','space'),('current','enter'),('filter-different','click'),('research','enter'),('data','space'),('trial','space'),('trial','hover'),('trial','leave'),('trial','reverse'),('trial','cancel'),('trial','hold')]):
  if a.skip_hover and mode=='hover':continue
  code+="[OwnedResultsPageInput]::Run(($app+'\\ReplicaProbe.exe'),"+('$true'if a.action.startswith('source-')else'$false')+","+q(target)+","+q(mode)+");"
  if a.action.startswith('source-'):continue
  if mode in ['click','space','enter','hold']:
   request_count+=1
   label='filter:unknown'if target in ['filter-unknown','current']else'filter:different'if target=='filter-different'else'research'if target=='research'else'detail:'+target
   code+="$deadline=[DateTime]::UtcNow.AddSeconds(10);$published=$false;do{try{$record=ReadResultsShared ($app+'\\resultspage-input.json')|ConvertFrom-Json}catch{$record=$null};if($null-ne$record-and@($record.requests).Count-eq "+str(request_count)+"-and$record.history[-1].label-eq "+q(label)+"){$published=$true;break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$deadline);if(!$published){throw 'Fresh callback state publication missing; stop without repeat'};"
  code+="[IO.File]::WriteAllText(($app+"+q('/'+('reduced-'if a.reduced else'')+'input-'+target+'-'+mode+'.json')+"),(ReadResultsShared ($app+'\\resultspage-input.json')));if(Test-Path($app+'\\resultspage-tracks.json')){[IO.File]::WriteAllText(($app+"+q('/'+('reduced-'if a.reduced else'')+'tracks-'+target+'-'+mode+'.json')+"),(ReadResultsShared ($app+'\\resultspage-tracks.json')))};"
 if a.action=='source-input-batch' and a.reduced:code+="[OwnedResultsPageInput]::Run(($app+'\\ReplicaProbe.exe'),$true,'current','escape');"
 print(ps(code,timeout=40))
elif a.action=='focus-read':
 # Read durable owned files after immediate native close; no UIA/window/input operation.
 out=r/'docs/replica/source/resultspage-windows-focus';out.mkdir(exist_ok=True)
 names=['input-filter-unknown-click.json','input-current-space.json','input-filter-unknown-hold.json','resultspage-focus-held-2.json','resultspage-focus-callback-1.png','resultspage-focus-callback-2.png','resultspage-focus-callback-3.png','resultspage-focus-held-2.png']
 code=base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq($app+'\\ReplicaProbe.exe')}).Count){throw 'Completed candidate must already be closed'};if((Get-FileHash($app+'\\ReplicaProbe.dll')-Algorithm SHA256).Hash-ne "+q(s['binarySha256'].upper())+"){throw 'Exact cold runtime required'};@("+','.join(q(n)for n in names)+")|ForEach-Object{[pscustomobject]@{name=$_;bytes=[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $app $_)))}}|ConvertTo-Json -Depth 4 -Compress;"
 for item in json.loads(ps(code,retry_read=True)):
  data=base64.b64decode(item['bytes']);assert item['name']in names
  if item['name'].endswith('.json'):json.loads(data)
  (out/item['name']).write_bytes(data)
 print('Completed closed runtime focus files copied; no window held during transfer')
elif a.action=='track-read':
 out=r/'docs/replica/source/resultspage-windows-native';out.mkdir(exist_ok=True)
 data=json.loads(ps(owned+shared_read+"@(Get-ChildItem $app -Filter '*-*.json'|Where-Object{$_.Name-like 'input-*'-or$_.Name-like 'tracks-*'-or$_.Name-like 'reduced-*'-or$_.Name-eq'resultspage-input.json'-or$_.Name-eq'resultspage-tracks.json'}|ForEach-Object{[pscustomobject]@{name=$_.Name;data=(ReadResultsShared $_.FullName)}})|ConvertTo-Json -Depth 4 -Compress;",retry_read=True))
 for item in data:json.loads(item['data']);(out/item['name']).write_text(item['data'])
 print('Fresh per-input native files',len(data))
else:
 print(ps(owned+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 if native:print(ps(base+"[Console]::WriteLine(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count)",retry_read=True))
