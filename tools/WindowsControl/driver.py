"""Explicitly user-authorized Parallels CLI / Windows UI Automation fixture control.
No credentials, OS settings, other applications, or arbitrary command interface.
"""
import argparse, base64, fcntl, gzip, hashlib, json, subprocess, time, uuid
from pathlib import Path
CLI = '/Applications/Parallels Desktop.app/Contents/MacOS/prlctl'
VM = 'Windows 11'
APP_FOLDER = 'DBMV-Replica-Playback-20261006'
PREFIX = '''[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false);
$ErrorActionPreference='Stop';
Add-Type -AssemblyName UIAutomationClient;
Add-Type -AssemblyName UIAutomationTypes;
'''
FIND = '''$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$expected);
$windows=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,$condition);
$window=@($windows|Where-Object{(Get-Process -Id $_.Current.ProcessId).Path -eq ($probeApp+'\\ReplicaProbe.exe')});
if($window.Count -ne 1){throw 'Expected unique owned fixture window'};$window=$window[0];
'''
def q(s): return "'" + str(s).replace("'", "''") + "'"
def encoded_script(code):
    script=PREFIX+code
    encoded=base64.b64encode(script.encode('utf-16-le')).decode()
    staged=None
    if len(encoded)>3500:
        # This installed CLI rejects long guest argument strings (~4KB).
        # Compress the same authorized inline command; do not change execution
        # policy or run downloaded .ps1 files (the guest disallows those).
        compressed=base64.b64encode(gzip.compress(script.encode('utf-8'))).decode()
        wrapper="$m=[IO.MemoryStream]::new([Convert]::FromBase64String("+q(compressed)+"));$g=[IO.Compression.GZipStream]::new($m,[IO.Compression.CompressionMode]0);$r=[IO.StreamReader]::new($g);& ([scriptblock]::Create($r.ReadToEnd()));$r.Dispose();"
        encoded=base64.b64encode(wrapper.encode('utf-16-le')).decode()
    if len(encoded)>3500:
        # The compressed wrapper can still exceed the Parallels argument limit.
        # Transfer only this generated, hash-verified owned fixture command;
        # evaluate the same inline ScriptBlock without changing execution policy.
        folder=Path(__file__).resolve().parents[2]/'artifacts/replica/windows-download'
        staged=folder/('owned-windows-command-'+uuid.uuid4().hex+'.txt')
        payload=script.encode('utf-8');staged.write_bytes(payload)
        sha=hashlib.sha256(payload).hexdigest().upper()
        url='http://10.211.55.2:8768/'+staged.name
        wrapper="$ProgressPreference='SilentlyContinue';$f=Join-Path $env:TEMP "+q(staged.name)+";try{Invoke-WebRequest -UseBasicParsing "+q(url)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Generated fixture command hash mismatch'};& ([scriptblock]::Create([Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($f))))}finally{Remove-Item $f -ErrorAction SilentlyContinue};"
        encoded=base64.b64encode(wrapper.encode('utf-16-le')).decode()
    return encoded,staged
def ps(code, timeout=40, retry_read=False):
    # Parallels guest command transport is a serial resource, independent of
    # native input leases and isolated verification processes.
    lock_path=Path(__file__).resolve().parents[2]/'artifacts/replica/windows-command-transport.lock'
    lock_path.parent.mkdir(parents=True,exist_ok=True)
    with lock_path.open('a') as transport:
        fcntl.flock(transport,fcntl.LOCK_EX)
        try:return _ps_serial(code,timeout,retry_read)
        finally:fcntl.flock(transport,fcntl.LOCK_UN)
def _ps_serial(code, timeout=40, retry_read=False):
    encoded,staged=encoded_script(code)
    try:
        for attempt in range(3):
            result=subprocess.run([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],capture_output=True,timeout=timeout)
            if not result.returncode:break
            error=result.stderr.decode('utf-8',errors='replace')+result.stdout.decode('utf-8',errors='replace')
            session_not_started='PrlVmGuest_RunProgram: Unable to open new session' in error
            if attempt==2 or not(session_not_started or (retry_read and 'Invalid argument' in error)):raise RuntimeError(error)
            time.sleep(.25 * (attempt+1))
        return result.stdout.decode('utf-8-sig').strip()
    finally:
        if staged:staged.unlink(missing_ok=True)
def main():
    parser=argparse.ArgumentParser();parser.add_argument('action',choices=['windows','prepare','launch-motion','focus-motion','key-motion','read-motion','close-motion','launch-typography','focus-typography','key-typography','read-typography','close-typography','source-controls','invoke-source-record','focus-source','open-source-extended','invoke-source-extended']);parser.add_argument('--state',choices=['completed','cancelled','reduced','initial','narrow','restored'],default='completed');parser.add_argument('--key',choices=['c','m','r','n']);parser.add_argument('--output',type=Path);a=parser.parse_args()
    common="$probeRoot=Join-Path $env:TEMP "+q(APP_FOLDER)+"; $probeApp=Join-Path $probeRoot 'app';"
    if a.action=='prepare':
        print(ps(common+"$ProgressPreference='SilentlyContinue'; New-Item -ItemType Directory -Force $probeRoot | Out-Null; Invoke-WebRequest 'http://10.211.55.2:8768/replica-check.zip' -OutFile ($probeRoot+'\\check.zip'); Expand-Archive ($probeRoot+'\\check.zip') $probeApp -Force; [Console]::WriteLine('User fixture prepared')",timeout=180))
    elif a.action=='windows':
        print(ps("$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition); $rows=@(foreach($w in $wins){if($w.Current.Name -like '*Typography*' -or $w.Current.Name -like 'Replica Motion*'){[pscustomobject]@{title=$w.Current.Name;processId=$w.Current.ProcessId;handle=$w.Current.NativeWindowHandle}}}); ConvertTo-Json -InputObject $rows"))
    elif a.action in ('launch-motion','launch-typography'):
        arguments="@('--motion-window',$probeApp)" if a.action=='launch-motion' else "@('--text-window',($probeApp+'\\typography-cases.json'),$probeApp)"
        print(ps(common+"$p=Start-Process -FilePath ($probeApp+'\\ReplicaProbe.exe') -ArgumentList "+arguments+" -WorkingDirectory $probeApp -PassThru; [Console]::WriteLine($p.Id)"))
    elif a.action in ('focus-motion','key-motion','close-motion','focus-typography','key-typography','close-typography'):
        typography=a.action.endswith('typography')
        title='Replica Typography verification' if typography else 'Replica Motion verification'
        expected=title+(' · '+a.state if a.state!='initial' else '')
        if a.action.startswith('close-'):
            print(ps(common+'$expected='+q(expected)+';'+FIND+"$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close(); [Console]::WriteLine('Fixture close requested')"));return
        target_state=({'n':'narrow','r':'restored'} if typography else {'c':'cancelled','m':'reduced','r':'completed'}).get(a.key)
        record=('typography-live-' if typography else 'motion-window-')+str(target_state)+'.json'
        before=None
        if a.action.startswith('key-'):
            if not target_state:parser.error('Key is not supported by this fixture')
            before=ps(common+"$f=Join-Path $probeApp "+q(record)+"; if(Test-Path $f){[Console]::WriteLine((Get-Item $f).LastWriteTimeUtc.Ticks)}else{[Console]::WriteLine(0)}",retry_read=True)
        condition="[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ReplicaKeyboardTarget')" if typography else "[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::IsKeyboardFocusableProperty,$true)"
        focus=common+'$expected='+q(expected)+';'+FIND+"$condition="+condition+";$targets=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$condition);if($targets.Count -ne 1){throw 'Expected one fixture keyboard target'};$targets[0].SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Owned fixture is not keyboard focused'};"
        if a.action.startswith('focus-'):
            print(ps(focus+"[Console]::WriteLine($window.Current.Name)"));return
        if a.action.startswith('key-'):
            # Keep the UIA host alive while sending the key. Its exit can otherwise
            # restore a console window's focus before the host-side key arrives.
            nonce=str(uuid.uuid4())
            target_title=title+' · '+target_state
            live=focus+"$ProgressPreference='SilentlyContinue'; Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps({'nonce':nonce,'expected':expected}))+" | Out-Null; $deadline=[DateTime]::UtcNow.AddSeconds(10);do{if($window.Current.Name -eq "+q(target_title)+"){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $deadline);if($window.Current.Name -ne "+q(target_title)+"){throw 'Key did not reach owned fixture'};[Console]::WriteLine($window.Current.Name);"
            encoded,staged=encoded_script(live)
            control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
            ready_path=Path(__file__).resolve().parents[2]/'docs/replica/source/windows-control-ready.json'
            deadline=time.monotonic()+10
            while time.monotonic()<deadline:
                try:
                    if json.loads(ready_path.read_text()).get('nonce')==nonce:break
                except (FileNotFoundError,json.JSONDecodeError):pass
                if control.poll() is not None:
                    out,err=control.communicate()
                    if staged:staged.unlink(missing_ok=True)
                    raise RuntimeError((out+err).decode('utf8',errors='replace'))
                time.sleep(.05)
            else:
                control.terminate();control.communicate()
                if staged:staged.unlink(missing_ok=True)
                raise RuntimeError('No verified UIA focus handshake; key blocked')
            code={'c':46,'m':50,'r':19,'n':49}[a.key]
            # Explicit press AND release, including release after failed press.
            try:subprocess.run([CLI,'send-key-event',VM,'--scancode',str(code),'--event','press'],check=True,capture_output=True,timeout=10)
            finally:subprocess.run([CLI,'send-key-event',VM,'--scancode',str(code),'--event','release'],check=True,capture_output=True,timeout=10)
            out,err=control.communicate(timeout=15)
            if staged:staged.unlink(missing_ok=True)
            if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
            # Verify the owned window AND a fresh record, never reuse an older success JSON.
            verification=common+'$expected='+q(target_title)+';'+"$deadline=[DateTime]::UtcNow.AddSeconds(5);$condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$expected);do{$window=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,$condition);if($null -ne $window){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow -lt $deadline);"+FIND+"$f=Join-Path $probeApp "+q(record)+";if((Get-Item $f).LastWriteTimeUtc.Ticks -le "+before+"){throw 'No fresh fixture record after key'};[Console]::WriteLine($window.Current.Name);"
            print(ps(verification,retry_read=True))
            print('Key verified: '+a.key)
    elif a.action=='open-source-extended':
        print(ps("Start-Process msedge.exe 'http://10.211.55.2:8768/typography-extended.html';[Console]::WriteLine('Owned extended source requested')"))
    elif a.action in ('source-controls','invoke-source-record','focus-source','invoke-source-extended'):
        code="""$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);
$window=@($wins | Where-Object {$_.Current.Name.StartsWith('Typography baseline49 · source isolation')}) ;
if($window.Count -ne 1){throw 'Exact source fixture window must be unique'}; $window=$window[0];
if((Get-Process -Id $window.Current.ProcessId).ProcessName -ne 'msedge'){throw 'Expected Edge source fixture'};
$condition=[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Button),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'기록 완료'));
$button=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);
if($null -eq $button){throw 'Expected source record button not found'};
"""
        if a.action=='invoke-source-extended':
            code=code.replace('Typography baseline49 · source isolation','Typography extended cases · source isolation').replace("'기록 완료'","'Windows 경계 측정 기록'")
        if a.action=='focus-source':code+="$button.SetFocus(); [Console]::WriteLine('Source fixture record button focused');"
        if a.action in ('invoke-source-record','invoke-source-extended'):code+="$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"
        if a.action!='focus-source':code+="[pscustomobject]@{button=$button.Current.Name;enabled=$button.Current.IsEnabled;patterns=@($button.GetSupportedPatterns() | ForEach-Object {$_.ProgrammaticName})}|ConvertTo-Json"
        print(ps(code))
    elif a.action=='read-motion':
        data=ps(common+"Get-Content -Raw ($probeApp+'\\motion-window-"+a.state+".json')",retry_read=True)
        parsed=json.loads(data)
        if a.output:a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(parsed,ensure_ascii=False,indent=2)+'\n')
        print(json.dumps(dict(state=parsed['state'],engine=parsed.get('engine'),status=parsed['status'],scale=parsed['scale']),ensure_ascii=False))
    elif a.action=='read-typography':
        if a.state not in ('narrow','restored'):parser.error('Typography record state must be narrow or restored')
        data=ps(common+"Get-Content -Raw ($probeApp+'\\typography-live-"+a.state+".json')",retry_read=True)
        parsed=json.loads(data)
        if a.output:a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(parsed,ensure_ascii=False,indent=2)+'\n')
        print(json.dumps(parsed,ensure_ascii=False))
if __name__=='__main__': main()
