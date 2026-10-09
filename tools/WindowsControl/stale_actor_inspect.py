"""Read-only identity lookup for the one documented timed-out project wrapper; no UIA walk."""
from pathlib import Path
import base64,fcntl,json,subprocess,uuid
r=Path(__file__).resolve().parents[2];out=r/'artifacts/replica/workers/root/guest-recovery'/uuid.uuid4().hex;out.mkdir(parents=True)
script=r'''[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false);$ErrorActionPreference='Stop';
$rows=@();foreach($p in Get-CimInstance Win32_Process -Filter "name='powershell.exe'" -Property ProcessId,ExecutablePath,CommandLine,CreationDate){
 $cmd=$p.CommandLine;if($cmd -notmatch '(?i)-EncodedCommand\s+([A-Za-z0-9+/=]+)'){continue};try{$decoded=[Text.Encoding]::Unicode.GetString([Convert]::FromBase64String($Matches[1]))}catch{continue};
 if(!$decoded.Contains('owned-windows-command-36944a9f66f94c12869c588aba16f2ec.txt')){continue};
 if(!$p.ExecutablePath.EndsWith('\WindowsPowerShell\v1.0\powershell.exe',[StringComparison]::OrdinalIgnoreCase)){throw 'Expected official actor executable missing'};
 $rows+=@{pid=$p.ProcessId;path=$p.ExecutablePath;creation=$p.CreationDate.ToString('o');ownedWrapper='owned-windows-command-36944a9f66f94c12869c588aba16f2ec.txt';readOnly=$true};
};ConvertTo-Json -InputObject @($rows) -Depth 5 -Compress;'''
encoded=base64.b64encode(script.encode('utf-16-le')).decode();lock=r/'artifacts/replica/windows-command-transport.lock'
with lock.open('a')as f:
 fcntl.flock(f,fcntl.LOCK_EX)
 try:
  v=subprocess.run(['/Applications/Parallels Desktop.app/Contents/MacOS/prlctl','exec','Windows 11','--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],capture_output=True,timeout=15)
  record={'scope':'specific stale project actor identity, read-only, no UIA tree, no OS settings','returncode':v.returncode,'stdout':v.stdout.decode('utf-8-sig',errors='replace'),'stderr':v.stderr.decode('utf-8-sig',errors='replace')};(out/'inspect.json').write_text(json.dumps(record,indent=2)+'\n');print(out,record,flush=True)
  if v.returncode==0:
   data=json.loads(record['stdout']);(out/'owned-actors.json').write_text(json.dumps(data,indent=2)+'\n')
 except subprocess.TimeoutExpired as e:
  (out/'timeout.json').write_text(json.dumps({'scope':'read-only stale specific actor inspect','timeout':15,'commandReplayed':False})+'\n');print('Read-only inspect timed out',out,flush=True)
 finally:fcntl.flock(f,fcntl.LOCK_UN)
