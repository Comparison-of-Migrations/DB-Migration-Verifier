"""Finite new parent resize/source48. External native sizing and exact close0."""
from pathlib import Path
import argparse
import datetime
import hashlib
import json
import uuid
from driver import ps, q

ROOT = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('manifest', type=Path)
p.add_argument('action', choices=['prepare', 'launch', 'batch', 'close', 'fetch', 'headless', 'measure-source', 'compile'])
p.add_argument('--diagnostic', action='store_true')
p.add_argument('--history-only', action='store_true')
p.add_argument('--history-start', type=int, choices=[0,3], default=0)
p.add_argument('--emit-only', action='store_true')
p.add_argument('--header-precision', action='store_true')
p.add_argument('--header-allocation', action='store_true')
p.add_argument('--preflight-only', action='store_true')
p.add_argument('--review-only', action='store_true')
a = p.parse_args()
m = a.manifest.resolve()
meta = json.loads(m.read_text())
assert meta['owner'] == 'connection' and meta['component'] == 'pageintegration'
assert meta['runtime'] == 'win-x64' and meta['buildExitCode'] == 0
out = m.parent/('actual-viewport-history-windows' if a.history_only else 'actual-parent-resize-windows')
out.mkdir(exist_ok=True)
served = ROOT/'artifacts/replica/windows-download'
nonce = uuid.uuid4().hex
if a.action == 'prepare':
    data = (ROOT/'tools/WindowsControl/page_integration_results_mouse.cs').read_bytes()+b'\n'+(ROOT/'tools/WindowsControl/page_integration_parent_resize.cs').read_bytes()
    f = served/('connection-parentresize-'+nonce+'-helper.txt')
    f.write_bytes(data)
    (out/'helper.json').write_text(json.dumps(dict(file=f.name, sha256=hashlib.sha256(data).hexdigest()), indent=2)+'\n')
    print(f)
    raise SystemExit(0)
resources = json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']
assert (a.emit_only and a.action == 'batch') or (a.action in ('fetch', 'headless', 'compile') and 'connection' in resources['headless']['owners']) or resources['windows']['owner'] == 'connection'
stage = json.loads((m.parent/'windows-stage.json').read_text())
assert stage['selfContained'] and stage['guestFolder'] == 'DBMV-Isolated-connection-pageintegration-'+m.parent.name
app = "$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';$exe=Join-Path $app 'ReplicaProbe.exe';$out=Join-Path $app "+q(out.name)+";New-Item -ItemType Directory $out -Force|Out-Null;$ProgressPreference='SilentlyContinue';"
app += "if((Get-FileHash (Join-Path $app 'ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(meta['binarySha256'].upper())+"){throw 'Exact frozen binary hash changed'};"
read = "function ReadShared($f){$fs=[IO.FileStream]::new($f,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite-bor[IO.FileShare]::Delete));$rd=[IO.StreamReader]::new($fs,[Text.Encoding]::UTF8);try{return $rd.ReadToEnd()}finally{$rd.Dispose()}};"

def transfer(path, name):
    data = path.read_bytes()
    file = served/('connection-parentresize-'+nonce+'-'+name+'.json')
    file.write_bytes(data)
    return "$"+name+"=Join-Path $out "+q(name+'.json')+";Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+file.name)+" -OutFile $"+name+";if((Get-FileHash $"+name+" -Algorithm SHA256).Hash-ne "+q(hashlib.sha256(data).hexdigest().upper())+"){throw 'Exact frozen input hash changed'};"

if a.action == 'compile':
    support = json.loads((out/'helper.json').read_text())
    code = app+"$helper=Join-Path $out 'helper.txt';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+support['file'])+" -OutFile $helper;if((Get-FileHash $helper -Algorithm SHA256).Hash-ne "+q(support['sha256'].upper())+"){throw 'Owned helper hash changed'};Add-Type -TypeDefinition ([IO.File]::ReadAllText($helper,[Text.Encoding]::UTF8)) -ReferencedAssemblies UIAutomationClient,UIAutomationTypes,WindowsBase,System.Drawing;"
    if a.history_only:
        batch = json.loads((out/'prepared-batch.json').read_text())
        code += "$batch=Join-Path $out 'batch-parse.txt';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+batch['file'])+" -OutFile $batch;if((Get-FileHash $batch -Algorithm SHA256).Hash-ne "+q(batch['sha256'])+"){throw 'Exact finite history actor hash'};$tokens=$null;$errors=$null;$null=[System.Management.Automation.Language.Parser]::ParseInput([IO.File]::ReadAllText($batch,[Text.Encoding]::UTF8),[ref]$tokens,[ref]$errors);if($errors.Count){throw($errors|Out-String)};"
    code += "'Scoped helper compiled/finite actor parsed; no query/window/input/runtime';"
elif a.action == 'headless':
    code = app+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$exe}).Count){throw 'Own prior runtime active'};$headless=Join-Path $out "+q('headless-'+nonce)+";New-Item -ItemType Directory $headless|Out-Null;"
    code += transfer(ROOT/'docs/replica/source/pageintegrationparentresize-cases.json', 'cases')
    code += transfer(ROOT/('docs/replica/source/pageintegrationparentresize-windows-history-source.json' if a.history_only else 'docs/replica/source/pageintegrationparentresize-windows-source.json'), 'oracle')
    extra = " '--parent-resize-target=review-ko'" if a.review_only else " '--parent-resize-history-only'" if a.history_only else " '--parent-resize-headerallocation-only'" if a.header_allocation else " '--parent-resize-headerprecision-only'" if a.header_precision else (" '--parent-resize-target=capabilities-ko' '--parent-resize-layout-diagnostic'" if a.diagnostic else (" '--parent-resize-target=capabilities-ko,capabilities-en'" if a.preflight_only else ''))
    code += "$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';& $exe '--pageintegration' $cases $headless $oracle '--parent-resize-only'"+extra+";$exit=$LASTEXITCODE;$remaining=@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$exe}).Count;[pscustomobject]@{phase='HEADLESS_RECORDED_NOT_PASSED';output=$headless;exitCode=$exit;ownedRuntime=$remaining;binary="+q(meta['binarySha256'])+"}|ConvertTo-Json -Compress|Set-Content -Encoding UTF8(Join-Path $out 'headless-run.json');if($remaining-ne0-or$exit-ne0){throw 'Finite headless scope failed or runtime remains'};'Finite headless exited0; strict comparison pending';"
elif a.action == 'launch':
    for name, expected in meta['sourceHashes'].items():
        if name.startswith('src/Verifier.Desktop/') or Path(name).name in ('PageIntegrationParentResizeProbe.cs','PageIntegrationViewportHistoryProbe.cs'):
            assert hashlib.sha256((ROOT/name).read_bytes()).hexdigest() == expected, 'Fresh product/probe snapshot required: '+name
    code = app+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$exe}).Count){throw 'Own previous process live'};if(Test-Path (Join-Path $out 'launch.json')){throw 'Fresh launch only; audit uncertain launch before replay'};"
    code += transfer(ROOT/'docs/replica/source/pageintegrationparentresize-cases.json', 'cases')
    code += transfer(ROOT/('docs/replica/source/pageintegrationparentresize-windows-history-source.json' if a.history_only else 'docs/replica/source/pageintegrationparentresize-windows-source.json'), 'oracle')
    code += "Remove-Item Env:DBMV_HEADLESS,Env:DBMV_HEADLESS_SCALE -ErrorAction SilentlyContinue;$proc=Start-Process $exe -ArgumentList @('--pageintegration-window',$cases,$out,$oracle,'--parent-resize-only'"+(",'--parent-resize-history-only','--parent-resize-history-start=3'" if a.history_only and a.history_start == 3 else ",'--parent-resize-history-only'" if a.history_only else '')+") -WorkingDirectory $app -PassThru;[pscustomobject]@{pid=$proc.Id;path=$exe;binary="+q(meta['binarySha256'])+";nonce="+q(nonce)+"}|ConvertTo-Json|Set-Content -Encoding UTF8(Join-Path $out 'launch.json');'Fresh own native launched; verify ready before input';"
elif a.action == 'fetch':
    code = app+read+"foreach($file in Get-ChildItem $out -Recurse -File|Where-Object{$_.Extension-in'.json','.txt'}){$relative=$file.FullName.Substring((Get-Item $out).FullName.Length+1);[pscustomobject]@{name=$relative;content=(ReadShared $file.FullName)}|ConvertTo-Json -Compress};"
elif a.action == 'measure-source':
    endpoint = ROOT/'docs/replica/source/connection-pageintegrationparentresize-windows-source.json'
    before = endpoint.stat().st_mtime_ns if endpoint.exists() else -1
    alias = served/'connection-pageintegrationparentresize-windows-source-latest.json'
    if not alias.exists() and not alias.is_symlink():
        alias.symlink_to(endpoint)
    expected_contexts = 5 if a.header_precision else (1 if a.diagnostic else 12)
    source_scope = 'parent-resize-headerprecision5' if a.header_precision else 'parent-resize-only6'
    url = 'http://10.211.55.2:8768/connection-pageintegrationparentresize.html?auto=1&os=windows&recording='+nonce+('&debug=headerprecision' if a.header_precision else ('&context=capabilities-ko&debug=table' if a.diagnostic else (" '--parent-resize-target=capabilities-ko,capabilities-en'" if a.preflight_only else '')))
    code = app+"Start-Process msedge.exe -ArgumentList @('--new-window',"+q(url)+");$end=[DateTime]::UtcNow.AddSeconds(18);$window=$null;do{$windows=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('Parent resize · original isolation')-or$_.Current.Name.StartsWith('Parent resize recorded · original isolation')});$windows=@($windows|Where-Object{(Get-Process -Id $_.Current.ProcessId).Path-like '*\\Microsoft\\Edge\\Application\\msedge.exe'});if($windows.Count-eq1){$window=$windows[0];break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$end);if($null-eq$window){throw 'Unique owned source absent; no duplicate launch'};try{$urlCount=0;foreach($edit in $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)){$value=$null;if($edit.Current.ControlType-eq[System.Windows.Automation.ControlType]::Edit-and$edit.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$value)-and$value.Current.Value-in@("+q(url)+","+q(url.removeprefix('http://'))+")){$urlCount++}};if($urlCount-ne1){throw 'Exact fresh source URL guard'};$recorded=$false;do{try{$reply=Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+alias.name)+";$state=[Text.Encoding]::UTF8.GetString($reply.RawContentStream.ToArray())|ConvertFrom-Json;if($state.recording-eq"+q(nonce)+"-and$state.scope-eq"+q(source_scope)+"-and$state.rows.Count-eq"+str(expected_contexts)+"){$recorded=$true;break}}catch{};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$end);if(!$recorded){throw 'Fresh source48 not ready'}}finally{$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()};Start-Sleep -Milliseconds 200;$remaining=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('Parent resize · original isolation')-or$_.Current.Name.StartsWith('Parent resize recorded · original isolation')}).Count;if($remaining-ne0){throw 'Owned source remains'};'Owned source48 saved/sourceWindow0';"
else:
    support = json.loads((out/'helper.json').read_text())
    code = app+read+"$helper=Join-Path $out 'helper.txt';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+support['file'])+" -OutFile $helper;if((Get-FileHash $helper -Algorithm SHA256).Hash-ne "+q(support['sha256'].upper())+"){throw 'Owned helper changed'};Add-Type -TypeDefinition ([IO.File]::ReadAllText($helper,[Text.Encoding]::UTF8)) -ReferencedAssemblies UIAutomationClient,UIAutomationTypes,WindowsBase,System.Drawing;$launch=(ReadShared (Join-Path $out 'launch.json'))|ConvertFrom-Json;[ParentNativeResize]::Init($app,$launch.pid);"
    cleanup = "function CloseOwned{[ParentNativeResize]::Close();$end=[DateTime]::UtcNow.AddSeconds(4);do{$remaining=@(Get-Process -Id $launch.pid -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$exe}).Count;$windows=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ProcessId-eq$launch.pid-and$_.Current.Name-eq'Replica PageIntegration application verification'}).Count;if($remaining-eq0-and$windows-eq0){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$end);[pscustomobject]@{pid=$launch.pid;path=$exe;ownedNativeProcess=$remaining;ownedWindow=$windows}|ConvertTo-Json|Set-Content -Encoding UTF8(Join-Path $out 'cleanup.json');if($remaining-ne0-or$windows-ne0){throw 'Exact own close incomplete'}};"
    code += cleanup
    if a.action == 'close':
        code += "CloseOwned;'Exact native process/window0';"
    elif a.history_only:
        code += '$historyStart='+str(a.history_start)+';'+(ROOT/'tools/WindowsControl/page_integration_viewport_history_batch.ps.txt').read_text()
    else:
        code += "function Current{return ((ReadShared (Join-Path $out 'pageintegrationparentresize-actual-state.json'))|ConvertFrom-Json)};$records=[Collections.Generic.List[object]]::new();$transport=[Collections.Generic.List[object]]::new();try{for($context=0;$context-lt12;$context++){if($context-gt0){[OwnedResultsIntegrationInput]::Run($exe,$false,'current','next','','')};$end=[DateTime]::UtcNow.AddSeconds(5);do{Start-Sleep -Milliseconds 50;$baseline=Current}while((!$baseline.ready-or$baseline.index-ne$context)-and[DateTime]::UtcNow-lt$end);if(!$baseline.ready-or$baseline.index-ne$context){throw 'Fresh exact parent baseline missing; no replay'};$rows=[Collections.Generic.List[object]]::new();foreach($width in @(1280,1100,960,1280)){$before=Current;$detail=[ParentNativeResize]::Resize($width,900,$before.state.dpr)|ConvertFrom-Json;$transport.Add($detail);ConvertTo-Json -InputObject @($transport.ToArray()) -Depth 12|Set-Content -Encoding UTF8(Join-Path $out 'resize-transport.json');$end=[DateTime]::UtcNow.AddSeconds(4);do{Start-Sleep -Milliseconds 70;$fresh=Current}while(($fresh.index-ne$context-or!$fresh.ready-or$fresh.snapshot-le($before.snapshot+1)-or[Math]::Abs($fresh.state.width-$width)-gt0.01)-and[DateTime]::UtcNow-lt$end);if($fresh.index-ne$context-or!$fresh.ready-or$fresh.snapshot-le($before.snapshot+1)-or[Math]::Abs($fresh.state.width-$width)-gt0.01){$fresh|ConvertTo-Json -Depth 30|Set-Content -Encoding UTF8(Join-Path $out 'failed-state.json');throw 'Native client resize did not settle; no replay'};$rows.Add($fresh.state);$current=@{id=$baseline.id;rows=@($rows.ToArray())};@{dpr=$fresh.state.dpr;phase='ACTUAL_OS_RECORDED';rows=@($records.ToArray())+@($current)}|ConvertTo-Json -Depth 30|Set-Content -Encoding UTF8(Join-Path $out 'pageintegrationparentresize-records.json')};$records.Add($current)}}catch{$_.Exception.ToString()|Set-Content -Encoding UTF8(Join-Path $out 'primary-failure.txt');throw}finally{CloseOwned};'New parentresize48 recorded and immediateclose0; strict compare required';"

if a.history_only and a.action == 'batch':
    fallback = "function CloseHistoryFallback{$persisted=(ReadShared (Join-Path $out 'launch.json'))|ConvertFrom-Json;if($persisted.path-ne$exe){throw 'Exact launch path required for close'};$live=Get-Process -Id $persisted.pid -ErrorAction SilentlyContinue;if($live-and$live.Path-ne$exe){throw 'Close PID reused; no action'};$owned=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ProcessId-eq$persisted.pid-and$_.Current.Name-eq'Replica PageIntegration application verification'});if($owned.Count-gt1){throw 'Unique owned close window required'};if($owned.Count-eq1){$owned[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()};$end=[DateTime]::UtcNow.AddSeconds(4);do{$remaining=@(Get-Process -Id $persisted.pid -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$exe}).Count;$windows=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ProcessId-eq$persisted.pid-and$_.Current.Name-eq'Replica PageIntegration application verification'}).Count;if($remaining-eq0-and$windows-eq0){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$end);[pscustomobject]@{pid=$persisted.pid;path=$exe;ownedNativeProcess=$remaining;ownedWindow=$windows;fallback=$true}|ConvertTo-Json|Set-Content -Encoding UTF8(Join-Path $out 'cleanup.json');if($remaining-ne0-or$windows-ne0){throw 'Exact history close incomplete'}};"
    code = app+read+fallback+"try{"+code+"}finally{if(!(Test-Path(Join-Path $out 'cleanup.json'))){CloseHistoryFallback}};"

command = served/('connection-parentresize-'+nonce+'-command.txt')
command.write_text(code)
sha = hashlib.sha256(command.read_bytes()).hexdigest().upper()
if a.emit_only:
    assert a.action == 'batch'
    (out/'prepared-batch.json').write_text(json.dumps(dict(file=command.name,sha256=sha),indent=2)+'\n')
    print('File-only finite actor emitted; no guest command/UIA/window/input/runtime')
    raise SystemExit(0)
loader = "$ProgressPreference='SilentlyContinue';$file=Join-Path $env:TEMP "+q(command.name)+";try{Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+command.name)+" -OutFile $file;if((Get-FileHash $file -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Owned command changed'};& ([scriptblock]::Create([IO.File]::ReadAllText($file,[Text.Encoding]::UTF8)))}finally{Remove-Item $file -ErrorAction SilentlyContinue};"
(out/('command-'+nonce+'.json')).write_text(json.dumps(dict(action=a.action, startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(), nonce=nonce, commandSha256=sha, guestFolder=stage['guestFolder'], binarySha256=meta['binarySha256']), indent=2)+'\n')
try:
    result = ps(loader, timeout=55 if a.action in ('batch', 'headless') else 30, retry_read=a.action == 'fetch')
except Exception as error:
    (out/(a.action+'-'+nonce+'-failure.json')).write_text(json.dumps(dict(phase='UNVERIFIED', message=str(error), nonce=nonce, commandSha256=sha, replayedInput=False), indent=2)+'\n')
    raise
(out/(a.action+'-'+nonce+'.log')).write_text(result+'\n')
if a.action == 'fetch':
    for line in result.splitlines():
        try:
            value = json.loads(line)
        except json.JSONDecodeError:
            continue
        if 'name' in value and 'content' in value:
            relative = Path(value['name'].replace('\\', '/'))
            assert not relative.is_absolute() and '..' not in relative.parts
            target = out/relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(value['content'].lstrip('\ufeff')+'\n')
elif a.action == 'measure-source':
    assert endpoint.exists() and endpoint.stat().st_mtime_ns > before
    value = json.loads(endpoint.read_text())
    assert value['recording'] == nonce and len(value['rows']) == expected_contexts
    (ROOT/('docs/replica/source/pageintegrationparentresize-windows-'+('header-precision' if a.header_precision else ('table-diagnostic' if a.diagnostic else 'source'))+'.json')).write_bytes(endpoint.read_bytes())
print('Pure fetch saved' if a.action == 'fetch' else result[:2000])
