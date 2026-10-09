"""One new native-Mac hitplane scope; reuse original passive and prior passing parent scope."""
from pathlib import Path
import hashlib,json,subprocess,sys,time
r=Path(__file__).resolve().parents[2];manifest=Path(sys.argv[1]).resolve();m=json.loads(manifest.read_text());assert m['owner']=='preflight'and m['component']=='capabilitytable'and m['runtime']=='osx-arm64'and m['buildExitCode']==0
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight'
base=manifest.parent;out=base/'actual-macos/input';bundle=base/'CapabilityTableCheck.app';actor=base/'capability_table_control'
(out/'actor-environment.json').write_text(json.dumps({'manifest':str(manifest),'actorSha256':hashlib.sha256(actor.read_bytes()).hexdigest(),'scope':'new fullrow hitplane/passivepadding1 only; no oldgeometry/input replay','productHash':m['sourceHashes']['src/Verifier.Desktop/WebCapabilityTable.cs']},indent=2)+'\n')
def action(mode):
 command=[str(actor),str(manifest),mode];v=subprocess.run(command,cwd=r,capture_output=True,text=True,timeout=10);(out/('native-'+mode+'-process.json')).write_text(json.dumps({'command':command,'exitCode':v.returncode,'stdout':v.stdout,'stderr':v.stderr},indent=2)+'\n')
 if v.returncode:raise RuntimeError(v.stderr)
 return v
start=time.time();subprocess.run(['/usr/bin/open','-n',str(bundle)],check=True)
try:
 end=time.monotonic()+8
 while time.monotonic()<end:
  try:
   file=out/'capabilitytable-input.json';initial=json.loads(file.read_text())
   if file.stat().st_mtime>start and initial['id']=='ko-904'and initial['permissions']==0:break
  except(FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.08)
 else:raise RuntimeError('Fresh exact zero-request initial Table missing')
 time.sleep(.4);action('passive')
finally:
 action('close');time.sleep(.4);action('cleanup')
before=json.loads((out/'native-passive-before.json').read_text());after=json.loads((out/'native-passive.json').read_text());cleanup=json.loads((out/'cleanup.json').read_text())
source=r/'artifacts/replica/workers/preflight/capabilitytable/2f1d87c39ecb422e8225c2097edc1e67/actual-macos/input/source-passive.json';s=json.loads(source.read_text())
errors=[]
if not before['buttonFocused']or after['buttonFocused']!=s['buttonFocused']or after['permissions']-before['permissions']!=0:errors.append('Passive focus/nonaction mismatch')
if cleanup!={'nativeProcessCount':0,'sourceWindowCount':0}:errors.append('Cleanup incomplete')
trace=json.loads((out/'capabilitytable-routed-input.json').read_text());
if len(trace)!=3 or any(e['sourceType']=='WebCapabilityTable'or e['x']!=91 or e['y']!=111 for e in trace):errors.append('Row full hitplane route absent')
report={'scope':'new transparent-row passive-padding1 actualMac','source':str(source.relative_to(r)),'sourceHash':hashlib.sha256(source.read_bytes()).hexdigest(),'candidate':str(out.relative_to(r)),'errors':errors,'passed':not errors}
(base/'actual-macos/passive-comparison.json').write_text(json.dumps(report,indent=2)+'\n');print(base/'actual-macos/passive-comparison.json');assert not errors,errors
