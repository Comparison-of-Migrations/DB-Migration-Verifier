"""Affected metadata-only native eight callbacks KO/EN; close before reporting."""
from pathlib import Path
import json,subprocess,sys,time,shutil
r=Path(__file__).resolve().parents[2];m=Path(sys.argv[1]).resolve();o=m.parent/'actual-macos/input';actor=r/'artifacts/replica/workers/preflight/selectionpage/selection_page_native_control';record=o/'selectionpage-input.json'
def call(mode,label):
 p=subprocess.run([str(actor),str(m),mode],cwd=r,text=True,capture_output=True,timeout=12);d=o/label;d.mkdir();(d/'process.json').write_text(json.dumps({'mode':mode,'exitCode':p.returncode,'stdout':p.stdout,'stderr':p.stderr}))
 for f in o.glob(mode+'-*.json'):shutil.copyfile(f,d/f.name)
 assert p.returncode==0,p.stderr
 return json.loads(p.stdout)if p.stdout.startswith('{')else None
call('guard','00-guard');started=time.time();subprocess.run(['/usr/bin/open','-n',str(m.parent/'SelectionPage-input.app')],check=True)
try:
 limit=time.monotonic()+8
 while time.monotonic()<limit:
  try:
   state=json.loads(record.read_text())
   if record.stat().st_mtime>started and state['attached']:break
  except(FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.08)
 else:raise RuntimeError('Fresh state absent; no relaunch')
 checks=[];i=1
 for english in [False,True]:
  if english:call('next',f'{i:02d}-next');i+=1
  call('scroll-table',f'{i:02d}-scroll-table');i+=1
  for mode,reason in [('bulk-label','bulk'),('bulk','bulk'),('deselect','clear-tables')]:
   before=len(state['changes']);state=call(mode,f'{i:02d}-{mode}');i+=1;assert state['english']==english and state['lastChangeReason']==reason and len(state['changes'])==before+1 and state['changes'][-1]['reason']==reason;checks.append({'english':english,'mode':mode,'reason':reason,'passed':True})
  call('scroll-top',f'{i:02d}-scroll-top');i+=1;before=len(state['changes']);state=call('category-data',f'{i:02d}-category-data');i+=1;assert state['lastChangeReason']=='category:data'and len(state['changes'])==before+1;checks.append({'english':english,'mode':'category-data','reason':'category:data','passed':True})
 (o/'reason-comparison.json').write_text(json.dumps(checks,indent=2));assert len(checks)==8
finally:
 try:call('close','90-close')
 finally:time.sleep(.4);call('cleanup','91-cleanup')
print('Native metadata callbacks8; completed window/process0')
