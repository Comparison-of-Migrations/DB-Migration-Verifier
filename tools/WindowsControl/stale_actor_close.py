"""Close one proven stale completed validation actor, with fresh PID/time/path/wrapper guards."""
from pathlib import Path
import base64,fcntl,json,subprocess,uuid
r=Path(__file__).resolve().parents[2];proof=r/'artifacts/replica/workers/root/guest-recovery/eea84f9cd09043d1bd4512129276098e/owned-actors.json';rows=json.loads(proof.read_text());assert len(rows)==1 and rows[0]['pid']==1460 and rows[0]['ownedWrapper']=='owned-windows-command-36944a9f66f94c12869c588aba16f2ec.txt';out=proof.parent
script=r'''[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false);$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';
$p=Get-CimInstance Win32_Process -Filter 'ProcessId=1460' -Property ProcessId,ExecutablePath,CommandLine,CreationDate;
if($null-ne$p){
 if(!$p.ExecutablePath.EndsWith('\WindowsPowerShell\v1.0\powershell.exe',[StringComparison]::OrdinalIgnoreCase)){throw 'Exact official actor path guard'};
 if($p.CreationDate.ToString('o')-ne'2026-10-09T00:54:34.9777000+09:00'){throw 'PID creation identity guard'};
 if($p.CommandLine -notmatch '(?i)-EncodedCommand\s+([A-Za-z0-9+/=]+)'){throw 'Owned wrapper args guard'};
 $decoded=[Text.Encoding]::Unicode.GetString([Convert]::FromBase64String($Matches[1]));if(!$decoded.Contains('owned-windows-command-36944a9f66f94c12869c588aba16f2ec.txt')){throw 'Exact owned wrapper guard'};
 Stop-Process -Id 1460 -Force;
};Start-Sleep -Milliseconds 150;$remaining=@(Get-Process -Id 1460 -ErrorAction SilentlyContinue).Count;if($remaining-ne0){throw 'Exact completed actor still running'};@{pid=1460;remaining=0;scope='proved stale completed preflight validation actor only';nativeOldWindowProcess='previous own966 confirmed0';noOtherProcessStopped=$true}|ConvertTo-Json -Compress;'''
encoded=base64.b64encode(script.encode('utf-16-le')).decode();lock=r/'artifacts/replica/windows-command-transport.lock'
with lock.open('a')as f:
 fcntl.flock(f,fcntl.LOCK_EX)
 try:
  v=subprocess.run(['/Applications/Parallels Desktop.app/Contents/MacOS/prlctl','exec','Windows 11','--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],capture_output=True,timeout=15)
  record={'returncode':v.returncode,'stdout':v.stdout.decode('utf-8-sig',errors='replace'),'stderr':v.stderr.decode('utf-8-sig',errors='replace'),'proof':str(proof)};(out/'close-result.json').write_text(json.dumps(record,indent=2)+'\n');print(record)
  assert v.returncode==0;value=json.loads(record['stdout']);assert value['remaining']==0;(out/'cleanup.json').write_text(json.dumps(value,indent=2)+'\n')
 except subprocess.TimeoutExpired:
  (out/'close-timeout.json').write_text(json.dumps({'pid':1460,'mutationNotReplayed':True,'identityGuard':'freshpath/creation/decodedwrapper','requiresReadOnlyStatus':True})+'\n');raise
 finally:fcntl.flock(f,fcntl.LOCK_UN)
