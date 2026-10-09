"""Owned Selection callback lifecycle. Startup observation is separate from physical input."""
from pathlib import Path
import argparse, datetime, hashlib, json, uuid
from driver import ps, q

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser(); p.add_argument('manifest', type=Path); p.add_argument('action', choices=['launch','startup','trace','observe','close']); p.add_argument('--english', action='store_true'); p.add_argument('--resume-after-query', action='store_true'); p.add_argument('--resume-after-removal-open', action='store_true'); p.add_argument('--resume-after-resize', action='store_true'); p.add_argument('--diagnose-startup', action='store_true'); p.add_argument('--closed-launch', type=Path); p.add_argument('--wait-ready', action='store_true'); a = p.parse_args()
m = a.manifest.resolve(); meta = json.loads(m.read_text()); stage = json.loads((m.parent/'windows-stage.json').read_text())
assert meta['owner'] == stage['owner'] == 'preflight' and meta['component'] == stage['component'] == 'pageintegration'
assert meta['runtime'] == 'win-x64' and meta['buildExitCode'] == 0 and stage['selfContained']
assert stage['guestFolder'] == 'DBMV-Isolated-preflight-pageintegration-'+m.parent.name
if a.action not in ['startup','trace','observe']:
    assert json.loads((root/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner'] == 'preflight'
out = m.parent/'actual-selectioncallbacks-windows'; out.mkdir(exist_ok=True); active = out/'active-launch.json'; nonce = uuid.uuid4().hex; served = root/'artifacts/replica/windows-download'
code = "$ProgressPreference='SilentlyContinue';$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';$exe=Join-Path $app 'ReplicaProbe.exe';$flowRoot=Join-Path $app 'actual-selectioncallbacks-windows';New-Item -ItemType Directory $flowRoot -Force|Out-Null;"
code += "if((Get-FileHash(Join-Path $app 'ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(meta['binarySha256'].upper())+"){throw 'Exact frozen callback DLL guard'};"
read = "function ReadShared($f){if(!(Test-Path $f)){return ''};$stream=[IO.FileStream]::new($f,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite-bor[IO.FileShare]::Delete));$reader=[IO.StreamReader]::new($stream,[Text.Encoding]::UTF8);try{return $reader.ReadToEnd()}finally{$reader.Dispose()}};"
if a.action == 'launch':
    assert not active.exists(), 'Prior launch active/uncertain; no replay'
    oracle = root/'artifacts/replica/workers/preflight/selectioncallbacks/source/bcb156a1156d4681b0f59ee9741f5f4c/windows/original-windows.json'
    body = oracle.read_bytes(); original = json.loads(body); assert len(original['records']) == 2 and all(len(r['rows']) == 25 for r in original['records'])
    name = 'preflight-selectioncallbacks-'+nonce+'-oracle.json'; (served/name).write_bytes(body)
    code += "if(@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$exe}).Count){throw 'Own previous runtime active'};$out=Join-Path $flowRoot "+q('phase-'+nonce)+";New-Item -ItemType Directory $out|Out-Null;$cases=Join-Path $out 'cases.json';[IO.File]::WriteAllText($cases,'[]');$oracle=Join-Path $out 'oracle.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+name)+" -OutFile $oracle;if((Get-FileHash $oracle -Algorithm SHA256).Hash-ne "+q(hashlib.sha256(body).hexdigest().upper())+"){throw 'Original callback oracle changed'};Remove-Item Env:DBMV_HEADLESS,Env:DBMV_HEADLESS_SCALE -ErrorAction SilentlyContinue;"
    if a.resume_after_query or a.resume_after_removal_open or a.resume_after_resize: code += "[IO.File]::WriteAllText((Join-Path $out 'selectioncallbacks-resume.json'),'{\"checkpoint\":\""+('after-resize1280' if a.resume_after_resize else 'after-mapping-open-removal' if a.resume_after_removal_open else 'after-query-orders')+"\"}');"
    redirect = "" if a.diagnose_startup else " -RedirectStandardOutput (Join-Path $out 'stdout.log') -RedirectStandardError (Join-Path $out 'stderr.log')"
    if a.diagnose_startup:
        code += "$env:DOTNET_HOST_TRACE='1';$env:DOTNET_HOST_TRACEFILE=Join-Path $out 'native-host-trace.log';$env:DOTNET_HOST_TRACE_VERBOSITY='4';"
    code += "$proc=Start-Process $exe -ArgumentList @('--pageintegration-window',$cases,$out,$oracle,'--selection-callbacks-only'"+(",'--selection-callbacks-english'" if a.english else '')+") -WorkingDirectory $app"+redirect+" -PassThru;$creation=$proc.StartTime.ToUniversalTime().ToString('o');$session=$proc.SessionId;"
    if a.diagnose_startup:
        code += "$end=[DateTime]::UtcNow.AddSeconds(12);do{Start-Sleep -Milliseconds 80;$proc.Refresh()}while(!$proc.HasExited-and!(Test-Path(Join-Path $out 'selectioncallbacks-native-ready.json'))-and[DateTime]::UtcNow-lt$end);"
    code += "[pscustomobject]@{pid=$proc.Id;path=$exe;output=$out;creation=$creation;session=$session;english="+('$true' if a.english else '$false')+";resumeAfterQuery="+('$true' if a.resume_after_query or a.resume_after_removal_open or a.resume_after_resize else '$false')+";resumeAfterRemovalOpen="+('$true' if a.resume_after_removal_open else '$false')+";resumeAfterResize="+('$true' if a.resume_after_resize else '$false')+";diagnosticStartup="+('$true' if a.diagnose_startup else '$false')+";hasExited=$proc.HasExited;exitCode=if($proc.HasExited){$proc.ExitCode}else{$null};ready=(Test-Path(Join-Path $out 'selectioncallbacks-native-ready.json'));nonce="+q(nonce)+";binary="+q(meta['binarySha256'])+"}|ConvertTo-Json -Compress|Tee-Object -FilePath(Join-Path $flowRoot 'active-launch.json');"
else:
    launch = json.loads((a.closed_launch if a.closed_launch and a.action=='trace' else active).read_text()); assert launch['binary'] == meta['binarySha256']; code += read+"$pidOwned="+str(launch['pid'])+";$out="+q(launch['output'])+";"
    code += "$p=Get-Process -Id $pidOwned -ErrorAction SilentlyContinue;if($p-and$p.Path-ne$exe){throw 'Exact current callback process path guard'};"
    if a.action == 'startup':
        if a.wait_ready: code += "$end=[DateTime]::UtcNow.AddSeconds(15);while(!(Test-Path(Join-Path $out 'selectioncallbacks-native-ready.json'))-and[DateTime]::UtcNow-lt$end){Start-Sleep -Milliseconds 80};"
        code += "@{pid=$pidOwned;process=if($p){1}else{0};path=if($p){$p.Path}else{$null};creation=if($p){$p.StartTime.ToUniversalTime().ToString('o')}else{$null};mainHandle=if($p){$p.MainWindowHandle.ToInt64()}else{0};title=if($p){$p.MainWindowTitle}else{''};ready=(Test-Path(Join-Path $out 'selectioncallbacks-native-ready.json'));files=@(Get-ChildItem $out -File|Select-Object Name,Length);stdout=(ReadShared(Join-Path $out 'stdout.log'));stderr=(ReadShared(Join-Path $out 'stderr.log'))}|ConvertTo-Json -Depth 8 -Compress;"
    elif a.action == 'trace':
        code += "$trace=Join-Path $out 'native-host-trace.log';@{pid=$pidOwned;path=$exe;trace=if(Test-Path $trace){[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes((ReadShared $trace)))}else{''};failure=(ReadShared(Join-Path $out 'failure.txt'));ready=(Test-Path(Join-Path $out 'selectioncallbacks-native-ready.json'))}|ConvertTo-Json -Compress;"
    elif a.action == 'observe':
        code += "$latest=Get-ChildItem $out -Filter 'selectioncallback-state-*.json'|Sort-Object Name|Select-Object -Last 1;if(!$latest){throw 'Fresh immutable native callback observation absent; no input'};@{name=$latest.Name;data=(ReadShared $latest.FullName)}|ConvertTo-Json -Compress;"
    else:
        code += "$windows=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ProcessId-eq$pidOwned-and$_.Current.Name-eq'Replica PageIntegration application verification'});if($p){if($windows.Count-ne1){throw 'Exact own callback close needs unique title window'};$windows[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()};$end=[DateTime]::UtcNow.AddSeconds(5);do{Start-Sleep -Milliseconds 50;$count=@(Get-Process -Id $pidOwned -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$exe}).Count}while($count-ne0-and[DateTime]::UtcNow-lt$end);$windowCount=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ProcessId-eq$pidOwned-and$_.Current.Name-eq'Replica PageIntegration application verification'}).Count;@{pid=$pidOwned;path=$exe;nativeProcess=$count;nativeWindow=$windowCount}|ConvertTo-Json -Compress;if($count-ne0-or$windowCount-ne0){throw 'Exact callback close0 incomplete'};"
command = served/('preflight-selectioncallbacks-'+nonce+'-command.txt'); command.write_text(code); sha = hashlib.sha256(command.read_bytes()).hexdigest().upper()
loader = "$file=Join-Path $env:TEMP "+q(command.name)+";try{Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+command.name)+" -OutFile $file;if((Get-FileHash $file -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Exact callback command guard'};&([scriptblock]::Create([IO.File]::ReadAllText($file,[Text.Encoding]::UTF8)))}finally{Remove-Item $file -ErrorAction SilentlyContinue};exit 0;"
(out/('command-'+nonce+'.json')).write_text(json.dumps({'action':a.action,'nonce':nonce,'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'commandSha256':sha,'manifest':str(m),'scope':'Startup/lifecycle only, no input success inferred'},indent=2))
try: result = ps(loader,timeout=25,retry_read=a.action in ['startup','trace','observe'])
except Exception as error:
    (out/(a.action+'-'+nonce+'-failure.json')).write_text(json.dumps({'error':str(error),'nonce':nonce,'status':'UNVERIFIED_NO_REPLAY'})); raise
(out/(a.action+'-'+nonce+'.log')).write_text(result)
if a.action == 'launch':
    metadata=json.loads(result.strip().splitlines()[-1]);assert metadata['nonce']==nonce and metadata['binary']==meta['binarySha256'];active.write_text(json.dumps(metadata,indent=2))
elif a.action == 'observe':
    v = json.loads(result); (out/v['name']).write_text(v['data'].lstrip('\ufeff'))
elif a.action == 'startup': (out/('startup-'+nonce+'.json')).write_text(result)
elif a.action == 'trace':
    import base64
    v=json.loads(result);(out/('trace-'+nonce+'.json')).write_text(json.dumps({k:value for k,value in v.items() if k!='trace'},indent=2)); (out/('native-host-trace-'+nonce+'.log')).write_bytes(base64.b64decode(v['trace']))
else:
    v = json.loads(result); assert v['nativeProcess'] == v['nativeWindow'] == 0; (out/('cleanup-'+nonce+'.json')).write_text(result); active.rename(out/('closed-launch-'+nonce+'.json'))
print(({k:value for k,value in json.loads(result).items() if k!='trace'} if a.action=='trace' else result[:2500]))
