"""Finite official WMI inspection/cleanup of two recorded completed readonly actors only; no CLR/UIA/input."""
from pathlib import Path
import argparse,base64,fcntl,json,subprocess,uuid
r=Path(__file__).resolve().parents[2];a=argparse.ArgumentParser();a.add_argument('--close',action='store_true');v=a.parse_args()
records=json.loads((r/'docs/replica/source/resultspage-windows-timeout-actors.json').read_text())['actors']
known=[base64.b64encode(x['readonlyScript'].encode('utf-16-le')).decode() for x in records]
assert all(len(x)==rec['encodedLength'] for x,rec in zip(known,records))
out=r/'artifacts/replica/workers/root/guest-recovery'/uuid.uuid4().hex;out.mkdir(parents=True)
script='''var known=KNOWN,close=__CLOSE__,wmi=GetObject("winmgmts:"),e=new Enumerator(wmi.ExecQuery("SELECT ProcessId,ExecutablePath,CommandLine,CreationDate FROM Win32_Process WHERE Name='powershell.exe'"));var found=0;
for(;!e.atEnd();e.moveNext()){var p=e.item(),m=String(p.CommandLine).match(/-EncodedCommand\\s+([A-Za-z0-9+\\/=]+)/i);if(!m)continue;var i=-1;for(var k=0;k<known.length;k++){if(m[1]===known[k])i=k;}if(i<0)continue;
var path=String(p.ExecutablePath),creation=String(p.CreationDate);if(!/\\\\WindowsPowerShell\\\\v1\\.0\\\\powershell\\.exe$/i.test(path))throw Error("Official path guard");
// The recorded interval is UTC; WMI timestamp includes local offset.
function epoch(s){return Date.UTC(+s.slice(0,4),+s.slice(4,6)-1,+s.slice(6,8),+s.slice(8,10),+s.slice(10,12),+s.slice(12,14))-(s.charAt(21)==="-"?-1:1)*Number(s.slice(22))*60000;}
var utc=epoch(creation);if(utc<Date.UTC(2026,9,8,16,30,32)||utc>Date.UTC(2026,9,8,16,35,2))throw Error("Recorded creation interval guard");
WScript.Echo(["MATCH",i,p.ProcessId,path,creation].join("\\t"));found++;
if(close){var exact=wmi.Get("Win32_Process.Handle='"+p.ProcessId+"'");if(String(exact.CreationDate)!==creation||String(exact.CommandLine)!==String(p.CommandLine)||String(exact.ExecutablePath)!==path)throw Error("Fresh identity guard");var code=exact.Terminate(0);WScript.Echo("CLOSED\\t"+p.ProcessId+"\\t"+code);if(code!==0)throw Error("Terminate result");}}
WScript.Echo("FOUND\\t"+found);'''.replace('KNOWN',json.dumps(known)).replace('__CLOSE__','true' if v.close else 'false')
name='root-completed-results-actors-'+out.name+'.js';served=r/'artifacts/replica/windows-download'/name;served.write_text(script)
lock=r/'artifacts/replica/windows-command-transport.lock'
command='curl.exe --fail --silent --show-error http://10.211.55.2:8768/'+name+' -o "%TEMP%\\'+name+'" && cscript.exe //nologo "%TEMP%\\'+name+'"'
(out/'command.json').write_text(json.dumps({'scope':'two exact recorded completed readonly actors; no unrelated process','close':v.close,'script':str(served),'command':command},indent=2))
with lock.open('a') as f:
 fcntl.flock(f,fcntl.LOCK_EX)
 try:
  result=subprocess.run(['/Applications/Parallels Desktop.app/Contents/MacOS/prlctl','exec','Windows 11','--current-user','cmd.exe','/d','/s','/c',command],capture_output=True,timeout=15)
  record={'returncode':result.returncode,'stdout':result.stdout.decode('utf-8-sig',errors='replace'),'stderr':result.stderr.decode('utf-8-sig',errors='replace')};(out/'result.json').write_text(json.dumps(record,indent=2));print(out,record,flush=True)
 except subprocess.TimeoutExpired:
  (out/'timeout.json').write_text(json.dumps({'noReplay':True,'close':v.close}));raise
 finally:fcntl.flock(f,fcntl.LOCK_UN)
