"""Actual non-headless owned Windows Edge reference via browser Input API; never OS-click proof."""
from pathlib import Path
import argparse,json,uuid,hashlib,threading
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);a=p.parse_args();m=a.manifest.resolve();meta=json.loads(m.read_text());assert meta['owner']=='preflight' and meta['component']=='pageintegration'
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
uid=uuid.uuid4().hex;out=m.parent/'actual-main-padding-windows';folder=out/'browser-reference'/uid;folder.mkdir(parents=True)
cs=(r/'tools/WindowsControl/page_integration_main_padding_native.cs').read_bytes();served=r/'artifacts/replica/windows-download'/('preflight-mainpadding-browser-'+uid+'.txt');served.write_bytes(cs)
saved={};
for case,relative in [('ko-960','source-first-diagnostic/4/ko-960/wheel.json'),('ko-1280','source-fifth-diagnostic/ko-1280/wheel.json')]:
 data=json.loads((out/relative).read_text());assert data['rows'][0]['id']==case;saved[case]=data
savedfile=r/'artifacts/replica/windows-download'/('preflight-mainpadding-reused-wheel-'+uid+'.json');savedfile.write_text(json.dumps(saved));(folder/'reused-wheel.json').write_text(json.dumps(saved,indent=2))
code="$ProgressPreference='SilentlyContinue';$base=Join-Path $env:TEMP "+q('DBMV-MainPadding-browser-'+uid)+";New-Item -ItemType Directory $base|Out-Null;$helper=Join-Path $base 'helper.txt';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served.name)+" -OutFile $helper;if((Get-FileHash $helper -Algorithm SHA256).Hash-ne "+q(hashlib.sha256(cs).hexdigest().upper())+"){throw 'Owned helper hash'};Add-Type -TypeDefinition ([IO.File]::ReadAllText($helper)) -ReferencedAssemblies UIAutomationClient,UIAutomationTypes,WindowsBase;"+r'''
function Post($kind){try{return Invoke-RestMethod -UseBasicParsing ('http://10.211.55.2:8768/preflight-mainpadding-windows-'+$kind+'.json')}catch{return $null}}
function Call($method,$parameters){
 $script:messageId++;$request=@{id=$script:messageId;method=$method;params=$parameters};$text=$request|ConvertTo-Json -Compress -Depth 100;$bytes=[Text.Encoding]::UTF8.GetBytes($text);$cts=[Threading.CancellationTokenSource]::new(5000)
 try{$script:socket.SendAsync([ArraySegment[byte]]::new($bytes),[Net.WebSockets.WebSocketMessageType]::Text,$true,$cts.Token).GetAwaiter().GetResult()|Out-Null
 do{$memory=[IO.MemoryStream]::new();do{$buffer=[byte[]]::new(32768);$part=$script:socket.ReceiveAsync([ArraySegment[byte]]::new($buffer),$cts.Token).GetAwaiter().GetResult();$memory.Write($buffer,0,$part.Count)}while(!$part.EndOfMessage);$response=[Text.Encoding]::UTF8.GetString($memory.ToArray())|ConvertFrom-Json;$memory.Dispose()}while($response.id-ne$script:messageId)
 $script:wire.Add([pscustomobject]@{request=$request;response=$response});if($response.error){throw ($response.error|ConvertTo-Json)};return $response.result
 }finally{$cts.Dispose()}
}
$savedWheels=Invoke-RestMethod -UseBasicParsing 'http://10.211.55.2:8768/SAVED_WHEEL_FILE';$edge='C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe';$completed=Invoke-RestMethod -UseBasicParsing 'http://10.211.55.2:8768/preflight-mainpadding-completed-ko960.json';$rows=@($completed.row);$checks=@($completed.check)
foreach($language in @('ko','en')){foreach($width in @(960,1280)){
 $id=$language+'-'+$width;if($id-eq'ko-960'){continue};$nonce=[Guid]::NewGuid().ToString('N');$profile=Join-Path $base $id;New-Item -ItemType Directory $profile|Out-Null;$url='http://10.211.55.2:8768/preflight-mainpadding.html?os=windows&interactive=1&lang='+$language+'&width='+$width+'&recording='+$nonce;if($language-eq'ko'){$url+='&resume-offset=100'};$title='Main padding original '+$nonce;$sourcePid=0;$ready=$null;$script:socket=$null;$script:wire=[Collections.Generic.List[object]]::new();$script:messageId=0
 $launch=Start-Process -FilePath $edge -ArgumentList @('--remote-debugging-port=0','--force-renderer-accessibility','--no-first-run','--no-default-browser-check',('--user-data-dir='+$profile),'--window-size=1400,1050',('--app='+$url)) -PassThru
 [pscustomobject]@{pid=$launch.Id;profile=$profile;url=$url;nonce=$nonce;path=$edge;headless=$false;method='Actual browser Input API, not OS SendInput'}|ConvertTo-Json|Set-Content -Encoding UTF8 (Join-Path $profile 'launch.json')
 try{
  $end=[DateTime]::UtcNow.AddSeconds(30);do{Start-Sleep -Milliseconds 100;$v=Post 'control-ready';if($v.recording-eq$nonce){$ready=$v}}while(!$ready-and[DateTime]::UtcNow-lt$end);if(!$ready){throw 'Fresh owned source ready absent'};$ready|ConvertTo-Json -Depth 100|Set-Content -Encoding UTF8 (Join-Path $profile 'ready.json')
  $windows=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith($title)-and(Get-Process -Id $_.Current.ProcessId).Path-eq$edge});if($windows.Count-ne1){throw 'Exact source window required'};$sourcePid=$windows[0].Current.ProcessId;$process=Get-CimInstance Win32_Process -Filter ('ProcessId='+$sourcePid);if(!$process.CommandLine.Contains($profile)-or!$process.CommandLine.Contains('--remote-debugging-port=0')-or$process.CommandLine.Contains('--headless')){throw 'Private nonheadless profile identity'};$process|Select-Object ProcessId,ExecutablePath,CommandLine,CreationDate|ConvertTo-Json|Set-Content -Encoding UTF8 (Join-Path $profile 'owned-identity.json')
  [MainPaddingInput]::TracePath=Join-Path $profile 'physical-capture-diagnostic.txt';[MainPaddingInput]::Init($sourcePid,$edge,$title,$true);$row=$ready.rows[0];$point=@($row.points|Where-Object{$_.id-eq'left-padding'})[0];if($point.target.id-ne'main'-or$row.dpr-ne2){throw 'Fresh original MAIN/DPR2'};$x=$row.embedding.frameBox.x+$point.x;$y=$row.embedding.frameBox.y+$point.y
  # No OS wheel/button is injected. Records physical/current cursor, source GUI capture/focus, buttonstate and same owned root-hit before browser API.
  [MainPaddingInput]::Send($x,$y,'inspect',$row.embedding.outerWidth)|Set-Content -Encoding UTF8 (Join-Path $profile 'owned-hit-inspect.json')
  $active=[IO.File]::ReadAllLines((Join-Path $profile 'DevToolsActivePort'));$port=[int]$active[0];if($port-le0){throw 'Owned browser port absent'};$version=Invoke-RestMethod -UseBasicParsing ('http://127.0.0.1:'+$port+'/json/version');if(!([Uri]$version.webSocketDebuggerUrl).AbsolutePath.Equals($active[1])){throw 'Owned active browser endpoint mismatch'};$targets=Invoke-RestMethod -UseBasicParsing ('http://127.0.0.1:'+$port+'/json/list');$target=@($targets|Where-Object{$_.type-eq'page'-and$_.url-eq$url});if($target.Count-ne1){throw 'Unique exact nonce target URL required'};$target=$target[0];$uri=[Uri]$target.webSocketDebuggerUrl;if($uri.Host-notin@('localhost','127.0.0.1')-or$uri.Port-ne$port-or!$uri.AbsolutePath.EndsWith('/'+$target.id)){throw 'Owned page endpoint guard'};[pscustomobject]@{version=$version;target=$target;activePort=$active}|ConvertTo-Json -Depth 30|Set-Content -Encoding UTF8 (Join-Path $profile 'browser-target.json')
  $script:socket=[Net.WebSockets.ClientWebSocket]::new();$cts=[Threading.CancellationTokenSource]::new(5000);try{$script:socket.ConnectAsync($uri,$cts.Token).GetAwaiter().GetResult()|Out-Null}finally{$cts.Dispose()}
  $fresh=Call 'Runtime.evaluate' @{expression='({url:location.href,title:document.title,dpr:devicePixelRatio,state:window.paddingState()})';returnByValue=$true};if($fresh.result.value.url-ne$url-or$fresh.result.value.title-ne$title-or$fresh.result.value.dpr-ne2-or$fresh.result.value.state.points[0].target.id-ne'main'){throw 'Fresh actual browser target/document/main guard'}
  if($language-eq'ko'){$wheel=$savedWheels.$id;$we=@($wheel.events|Where-Object{$_.type-eq'wheel'})[0];if($row.resumeSetup.scrollTop-ne100){throw 'Explicit saved-wheel setup absent'};$wheel|ConvertTo-Json -Depth 100|Set-Content -Encoding UTF8 (Join-Path $profile 'reused-wheel.json')}else{
  Call 'Input.dispatchMouseEvent' @{type='mouseMoved';x=$x;y=$y;button='none';buttons=0;pointerType='mouse'}|Out-Null
  Call 'Input.dispatchMouseEvent' @{type='mouseWheel';x=$x;y=$y;deltaX=0;deltaY=100;button='none';buttons=0;pointerType='mouse'}|Out-Null
  $end=[DateTime]::UtcNow.AddSeconds(10);$wheel=$null;do{Start-Sleep -Milliseconds 100;$v=Post 'input';if($v.recording-eq$nonce-and@($v.events|Where-Object{$_.type-eq'wheel'-and$_.after}).Count){$wheel=$v}}while(!$wheel-and[DateTime]::UtcNow-lt$end);if(!$wheel){throw 'Browser wheel observation absent'};$we=@($wheel.events|Where-Object{$_.type-eq'wheel'})[0];if(!$we.isTrusted-or$we.target.id-ne'main'-or$we.after.main.scrollTop-ne100-or$we.after.focus.id-ne'screen-title'){throw 'Browser wheel contract'};$wheel|ConvertTo-Json -Depth 100|Set-Content -Encoding UTF8 (Join-Path $profile 'wheel.json')
  }
  $fresh=Call 'Runtime.evaluate' @{expression='({url:location.href,state:window.paddingState()})';returnByValue=$true};if($fresh.result.value.url-ne$url-or$fresh.result.value.state.points[0].target.id-ne'main'){throw 'Fresh intended source MAIN before browser click'}
  Call 'Input.dispatchMouseEvent' @{type='mousePressed';x=$x;y=$y;button='left';buttons=1;clickCount=1;pointerType='mouse'}|Out-Null
  try{Call 'Input.dispatchMouseEvent' @{type='mouseReleased';x=$x;y=$y;button='left';buttons=0;clickCount=1;pointerType='mouse'}|Out-Null}catch{Call 'Input.dispatchMouseEvent' @{type='mouseReleased';x=$x;y=$y;button='left';buttons=0;clickCount=1;pointerType='mouse'}|Out-Null;throw}
  Start-Sleep -Milliseconds 750;$settled=Call 'Runtime.evaluate' @{expression='({url:location.href,state:window.paddingState()})';returnByValue=$true};$settled|ConvertTo-Json -Depth 100|Set-Content -Encoding UTF8 (Join-Path $profile 'settled-readonly.json');if($settled.result.value.url-ne$url-or$settled.result.value.state.focus.tag-ne'BODY'){throw 'Settled source click focus contract'};
  $end=[DateTime]::UtcNow.AddSeconds(10);$click=$null;do{Start-Sleep -Milliseconds 100;$v=Post 'input';if($v.recording-eq$nonce-and@($v.events|Where-Object{$_.type-eq'click'}).Count-and@($v.events|Where-Object{$_.type-eq'pointerup'}).Count){$click=$v}}while(!$click-and[DateTime]::UtcNow-lt$end);if(!$click){throw 'Browser trusted click observation absent'};$up=@($click.events|Where-Object{$_.type-eq'pointerup'})[0];$ce=@($click.events|Where-Object{$_.type-eq'click'})[0];if(!$up.isTrusted-or!$ce.isTrusted-or$up.target.id-ne'main'-or$ce.target.id-ne'main'-or$settled.result.value.state.focus.tag-ne'BODY'){throw 'Browser click focus contract'};$click|ConvertTo-Json -Depth 100|Set-Content -Encoding UTF8 (Join-Path $profile 'click.json');$rows+=$row;$checks+=[pscustomobject]@{id=$id;method='Actual Windows nonheadless Edge browser Input API; not OS SendInput';wheelOffset=$we.after.main.scrollTop;focusAfterWheel=$we.after.focus;focusAfterClick=$settled.result.value.state.focus;settledObserver='Runtime readonly state after750ms';wheelTrusted=$we.isTrusted;clickTrusted=$ce.isTrusted}
 }finally{
  $script:wire|ConvertTo-Json -Depth 100|Set-Content -Encoding UTF8 (Join-Path $profile 'browser-input-wire.json');if($script:socket){$script:socket.Dispose()};if($sourcePid){[MainPaddingInput]::Close();Start-Sleep -Milliseconds 150;$process=Get-CimInstance Win32_Process -Filter ('ProcessId='+$sourcePid) -ErrorAction SilentlyContinue;if($process){if($process.ExecutablePath-ne$edge-or!$process.CommandLine.Contains($profile)){throw 'Owned source cleanup identity'};Stop-Process -Id $sourcePid}}
  Start-Sleep -Milliseconds 250;$own=@(Get-CimInstance Win32_Process -Filter "Name='msedge.exe'"|Where-Object{$_.CommandLine-and$_.CommandLine.Contains($profile)});$windows=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith($title)});[pscustomobject]@{pid=$sourcePid;sourceWindow=$windows.Count;profileProcesses=$own.Count}|ConvertTo-Json|Set-Content -Encoding UTF8 (Join-Path $profile 'cleanup.json');if($own.Count-or$windows.Count){throw 'Exact source remains'}
 }
}}
[pscustomobject]@{phase='ORIGINAL_ACTUAL_BROWSER_INPUT_RECORDED';method='Actual Windows nonheadless Edge Input API, not OS SendInput';rows=$rows;checks=$checks}|ConvertTo-Json -Depth 100|Set-Content -Encoding UTF8 (Join-Path $base 'comparison.json')
foreach($folder in Get-ChildItem $base -Directory){foreach($f in Get-ChildItem $folder.FullName -File|Where-Object{$_.Extension-in@('.json','.txt')}){[pscustomobject]@{name=($folder.Name+'/'+$f.Name);content=[IO.File]::ReadAllText($f.FullName)}|ConvertTo-Json -Compress -Depth 100}};[pscustomobject]@{name='comparison.json';content=[IO.File]::ReadAllText((Join-Path $base 'comparison.json'))}|ConvertTo-Json -Compress -Depth 100
'''
code=code.replace('SAVED_WHEEL_FILE',savedfile.name)
(folder/'command.ps1').write_text(code);stop=threading.Event()
def mirror():
 seen={}
 while not stop.wait(.025):
  for kind in ('input','control-ready'):
   source=r/'docs/replica/source'/('preflight-mainpadding-windows-'+kind+'.json')
   if not source.exists():continue
   data=source.read_bytes();sha=hashlib.sha256(data).hexdigest()
   if seen.get(kind)==sha:continue
   target=r/'artifacts/replica/windows-download'/source.name;tmp=target.with_suffix('.json.'+uid+'.tmp');tmp.write_bytes(data);tmp.replace(target);seen[kind]=sha
thread=threading.Thread(target=mirror,daemon=True);thread.start()
try:result=ps(code,timeout=120)
except Exception as error:(folder/'failure.txt').write_text(str(error));raise
finally:stop.set();thread.join(timeout=2)
(folder/'transport.txt').write_text(result)
for line in result.splitlines():
 try:d=json.loads(line)
 except json.JSONDecodeError:continue
 if 'content' in d:
  dest=folder/d['name'];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(d['content'].lstrip('\ufeff'))
comparison=json.loads((folder/'comparison.json').read_text());assert len(comparison['rows'])==4
(out/'source').mkdir(exist_ok=True);(out/'source/comparison.json').write_text(json.dumps(comparison,ensure_ascii=False,indent=2));print(folder)
