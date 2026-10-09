"""Owned real-app Windows file dialogs; one observed action at a time, never replay input."""
from pathlib import Path
import argparse, base64, hashlib, io, json, uuid, zipfile
from driver import ps, q

ROOT = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('manifest', type=Path)
p.add_argument('action', choices=['prepare', 'launch', 'state', 'invoke', 'set-file', 'accept', 'cancel', 'fetch', 'close'])
p.add_argument('--id')
p.add_argument('--format', choices=['json', 'csv', 'html', 'invalid'])
a = p.parse_args()
m = json.loads(a.manifest.read_text())
assert m['owner'] == 'root' and m['component'] == 'pageintegration' and m['runtime'] == 'win-x64' and m['buildExitCode'] == 0
assert hashlib.sha256(Path(m['binary']).read_bytes()).hexdigest() == m['binarySha256']
out = a.manifest.parent / 'actual-file-windows'
out.mkdir(exist_ok=True)
if a.action == 'prepare':
    scope = {'nonce': uuid.uuid4().hex, 'manifest': str(a.manifest.resolve()), 'binary': m['binarySha256']}
    assert not (out / 'scope.json').exists(), 'Do not replace an existing file scope'
    (out / 'scope.json').write_text(json.dumps(scope, indent=2))
    print(out)
    raise SystemExit(0)
scope = json.loads((out / 'scope.json').read_text())
stage = json.loads((a.manifest.parent / 'windows-stage.json').read_text())
assert stage['selfContained']
if a.action != 'fetch':
    lease = json.loads((ROOT / 'docs/replica/ENVIRONMENT_LEASE.json').read_text())
    assert lease['resources']['windows']['owner'] == 'root', 'Root native lease required'
nonce = uuid.uuid4().hex
code = "$app=Join-Path (Join-Path $env:TEMP " + q(stage['guestFolder']) + ") 'app';$exe=Join-Path $app 'ReplicaProbe.exe';$out=Join-Path $app " + q('root-file-'+scope['nonce']) + ";$files=Join-Path $out 'files';$ProgressPreference='SilentlyContinue';"
code += "if((Get-FileHash (Join-Path $app 'ReplicaProbe.dll')).Hash-ne " + q(m['binarySha256'].upper()) + "){throw 'Frozen binary mismatch'};"
code += 'Add-Type -TypeDefinition '+q((ROOT/'tools/WindowsControl/root_file_dialog_input.cs').read_text())+';\n'
code += r"""
function ReadShared($path){$s=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite-bor[IO.FileShare]::Delete));$r=[IO.StreamReader]::new($s);try{$r.ReadToEnd()}finally{$r.Dispose()}}
function ReadBytesShared($path){$s=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite-bor[IO.FileShare]::Delete));$mem=[IO.MemoryStream]::new();try{$s.CopyTo($mem);return ,$mem.ToArray()}finally{$s.Dispose();$mem.Dispose()}}
function WindowOwned{
 $launch=(ReadShared (Join-Path $out 'current-native.json'))|ConvertFrom-Json;
 $proc=Get-Process -Id $launch.pid -ErrorAction Stop;
 if($proc.Path-ne$exe-or$proc.StartTime.ToUniversalTime().Ticks-ne$launch.creation){throw 'Owned fresh process guard'};
 $pidCondition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,[int]$launch.pid);
 $windows=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,$pidCondition));
 $main=@($windows|Where-Object{$_.Current.Name-eq'Replica PageIntegration application verification'});
 if($main.Count-ne1){throw 'Unique owned main window required'};return $main[0]
}
function ElementOwned($window,$id){$matches=@($window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$id))|Where-Object{$_.Current.IsEnabled-and!$_.Current.IsOffscreen});if($matches.Count-ne1){throw 'Fresh unique enabled visible target required'};return $matches[0]}
function PopupOwned{
 $main=WindowOwned;$rootHandle=[IntPtr]$main.Current.NativeWindowHandle;$popup=[RootFileDialogInput]::GetWindow($rootHandle,6);
 if($popup-eq$rootHandle-or$popup-eq[IntPtr]::Zero){return $null};
 $title=[RootFileDialogInput]::Text($popup);if($title-notin@('Save sample result','예시 결과 저장','Open a sample result','예시 결과 열기')){throw 'Unexpected owned popup title'};
 [RootFileDialogInput]::Guard($rootHandle,$popup,[uint32]$main.Current.ProcessId,$title);
 $element=[System.Windows.Automation.AutomationElement]::FromHandle($popup);if($element.Current.ProcessId-ne$main.Current.ProcessId){throw 'Popup UIA process mismatch'};
 return [pscustomobject]@{main=$rootHandle;handle=$popup;pid=[uint32]$main.Current.ProcessId;title=$title;element=$element}
}
function FileControl($popup,$id){
 $matches=@($popup.element.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$id))|Where-Object{$_.Current.NativeWindowHandle-ne0-and$_.Current.IsEnabled-and!$_.Current.IsOffscreen-and($id-ne'1148'-or$_.Current.ControlType-eq[System.Windows.Automation.ControlType]::Edit)-and($id-ne'1001'-or[System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($_).Current.AutomationId-eq'FileNameControlHost')});
 if($matches.Count-ne1){throw 'Fresh native file control not unique'};return $matches[0]
}
function DialogOwned{$popup=PopupOwned;if($null-eq$popup){throw 'Owned popup missing'};return $popup.element}
function Record{
 $main=WindowOwned;$rows=[Collections.Generic.List[object]]::new();$pidCondition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,[int]$main.Current.ProcessId);
 $nodes=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,$pidCondition));$popup=PopupOwned;if($null-ne$popup){$nodes+=@($popup.element)};
 foreach($top in $nodes){$all=@($top)+@($top.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition));foreach($e in $all){if($e.Current.AutomationId.StartsWith('System.')-or$e.Current.ControlType-eq[System.Windows.Automation.ControlType]::ListItem){continue};$value=$null;$valuePattern=$null;if($e.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$valuePattern)){$value=$valuePattern.Current.Value};$rect=$e.Current.BoundingRectangle;$rows.Add([pscustomobject]@{top=$top.Current.Name;id=$e.Current.AutomationId;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;enabled=$e.Current.IsEnabled;offscreen=$e.Current.IsOffscreen;focus=$e.Current.HasKeyboardFocus;value=$value;box=@($rect.X,$rect.Y,$rect.Width,$rect.Height)})}}
 $state=(ReadShared (Join-Path $out 'pageintegration-actual-state.json'))|ConvertFrom-Json;
 [pscustomobject]@{state=$state;nodes=@($rows.ToArray());files=@(Get-ChildItem $files -File|Select-Object Name,Length)}|ConvertTo-Json -Depth 30
}
"""
if a.action == 'launch':
    code += "if(Test-Path $out){throw 'Fresh scope already exists; no duplicate launch'};if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$exe}).Count){throw 'Frozen app already running'};New-Item -ItemType Directory $files -Force|Out-Null;$cases=Join-Path $out 'cases.json';[IO.File]::WriteAllText($cases,'[]');[IO.File]::WriteAllText((Join-Path $files 'invalid-sample.json'),'{\"kind\":\"SYNTHETIC-NEVER-ECHO\",\"version\":2}');$env:DBMV_HEADLESS='0';Remove-Item Env:DBMV_HEADLESS_SCALE -ErrorAction SilentlyContinue;$proc=Start-Process $exe -ArgumentList @('--pageintegration-window',$cases,$out,'--file-window-only') -WorkingDirectory $app -PassThru;[pscustomobject]@{pid=$proc.Id;creation=$proc.StartTime.ToUniversalTime().Ticks;path=$exe}|ConvertTo-Json|Set-Content -Encoding UTF8 (Join-Path $out 'current-native.json');$end=[DateTime]::UtcNow.AddSeconds(10);do{if(Test-Path (Join-Path $out 'pageintegration-actual-ready.json')){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$end);if(!(Test-Path (Join-Path $out 'pageintegration-actual-ready.json'))){throw 'Fresh ready missing; do not relaunch'};Record;"
elif a.action == 'state':
    code += 'Record;'
elif a.action == 'invoke':
    assert a.id in ['format-csv', 'format-html', 'format-json', 'Footer-download', 'Save-open', 'language', 'Footer-save', 'Footer-back']
    code += '$w=WindowOwned;$e=ElementOwned $w '+q(a.id)+";$pattern=$null;if($e.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$pattern)){$pattern.Invoke()}elseif($e.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern,[ref]$pattern)){$pattern.Toggle()}else{throw 'Observed target has no permitted action'};Start-Sleep -Milliseconds 300;Record;"
elif a.action == 'set-file':
    assert a.id and a.format
    code += "$popup=PopupOwned;if($null-eq$popup){throw 'Owned file popup missing'};$e=FileControl $popup "+q(a.id)+";$s=(ReadShared (Join-Path $out 'pageintegration-actual-state.json'))|ConvertFrom-Json;$name="+ (q('invalid-sample.json') if a.format=='invalid' else "([string]$s.sampleRun.Id+'-sample."+a.format+"')")+";$path=Join-Path $files $name;[RootFileDialogInput]::FileName($popup.main,$popup.handle,[IntPtr]$e.Current.NativeWindowHandle,$popup.pid,$popup.title,$path);$fresh=FileControl (PopupOwned) "+q(a.id)+";$vp=$fresh.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern);if($vp.Current.Value-ne$path){throw 'Fresh UIA filename differs; do not accept or replay'};Record;"
elif a.action in ('accept', 'cancel'):
    assert a.id
    if a.action == 'accept':
        code += "$popup=PopupOwned;$editId=if($popup.title-in@('예시 결과 열기','Open a sample result')){'1148'}else{'1001'};$edit=FileControl $popup $editId;$v=$edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value;if(!$v.StartsWith($files+'\\')-or[IO.Path]::GetDirectoryName($v)-ne$files-or[IO.Path]::GetExtension($v)-notin@('.json','.csv','.html')){throw 'Fresh own synthetic path guard before accept'};"
    code += "$popup=PopupOwned;if($null-eq$popup){throw 'Owned file popup missing'};$e=FileControl $popup "+q(a.id)+";[RootFileDialogInput]::Click($popup.main,$popup.handle,[IntPtr]$e.Current.NativeWindowHandle,$popup.pid,$popup.title);Start-Sleep -Milliseconds 300;Record;"
elif a.action == 'close':
    code += "$w=WindowOwned;$pidOwned=$w.Current.ProcessId;$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();$end=[DateTime]::UtcNow.AddSeconds(4);do{$proc=Get-Process -Id $pidOwned -ErrorAction SilentlyContinue;if(!$proc){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt$end);$remaining=if($proc-and$proc.Path-eq$exe){1}else{0};$cleanup=[pscustomobject]@{pid=$pidOwned;processCount=$remaining;closedVia='Fresh owned WindowPattern.Close';scope='root sample files only'};$cleanup|ConvertTo-Json|Set-Content -Encoding UTF8 (Join-Path $out 'cleanup.json');if($remaining){throw 'Owned process remains'};$cleanup|ConvertTo-Json;"
elif a.action == 'fetch':
    code += "Add-Type -AssemblyName System.IO.Compression;$mem=[IO.MemoryStream]::new();$zip=[IO.Compression.ZipArchive]::new($mem,[IO.Compression.ZipArchiveMode]::Create,$true);foreach($f in Get-ChildItem $out -Recurse -File){$name=$f.FullName.Substring($out.Length+1).Replace('\\','/');$entry=$zip.CreateEntry($name);$stream=$entry.Open();try{$bytes=ReadBytesShared $f.FullName;$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}};$zip.Dispose();[Convert]::ToBase64String($mem.ToArray());"

# Always transfer a short SHA-verified generated command. Preserve exact action
# nonce before execution; a failed/uncertain mutation is inspected, never retried.
served = ROOT / 'artifacts/replica/windows-download' / ('root-file-command-'+nonce+'.txt')
served.write_text(code)
sha = hashlib.sha256(served.read_bytes()).hexdigest().upper()
(out / (nonce+'-command.json')).write_text(json.dumps({'action': a.action, 'id': a.id, 'format': a.format, 'sha256': sha, 'served': str(served)}, indent=2))
loader = "$f=Join-Path $env:TEMP "+q(served.name)+";Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served.name)+" -OutFile $f;if((Get-FileHash $f).Hash-ne "+q(sha)+"){throw 'Own command hash'};try{&([scriptblock]::Create([IO.File]::ReadAllText($f)))}finally{Remove-Item $f};"
result = ps(loader, timeout=35)
if a.action == 'fetch':
    with zipfile.ZipFile(io.BytesIO(base64.b64decode(result))) as z:
        for name in z.namelist():
            target = (out / 'guest-output' / name).resolve()
            assert target.is_relative_to((out / 'guest-output').resolve())
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(z.read(name))
    print(out / 'guest-output')
else:
    (out / (nonce+'-'+a.action+'.json')).write_text(result)
    data = json.loads(result)
    if 'state' in data:
        print(json.dumps({'state': {k:data['state'].get(k) for k in ['Screen','English','Saved','focus','formats']}, 'nodes': [{k:n[k] for k in ['top','id','name','type','value','enabled','focus']} for n in data['nodes'] if (n['type']=='ControlType.Window' or (n['type'] in ['ControlType.Edit','ControlType.Button'] and n['id'] in ['1001','1148','1','2','Save-open','Footer-download','Footer-save','language'])) and not n['offscreen']], 'files':data['files']}, ensure_ascii=False))
    else:
        print(result)
