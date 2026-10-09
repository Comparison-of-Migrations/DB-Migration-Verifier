"""One fresh actual source/native focus scope, no repeat of prior passed matrices."""
from pathlib import Path
import argparse,json,shutil,subprocess,sys,time
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('stage',type=Path);p.add_argument('--source-only',action='store_true');p.add_argument('--native-only',action='store_true');a=p.parse_args();evidence=root/'docs/replica/source';log=evidence/'resultspage-windows-focus-batch.log'
def run(action,*args):
 result=subprocess.run([sys.executable,str(root/'tools/WindowsControl/results_page.py'),action,'--stage',str(a.stage),*args],cwd=root,capture_output=True,text=True)
 with log.open('a')as f:f.write(json.dumps([action,*args])+'\n'+result.stdout+result.stderr+'\n')
 print(action,result.returncode,result.stdout.strip(),flush=True)
 if result.returncode:raise RuntimeError('Scoped action stopped without duplicate: '+action)
if not a.native_only:
 run('source-open','--tracks')
 try:
  run('trial-show','--tracks')
  bridge=evidence/'connection-resultspage-windows-input.json'
  for index in [1,2,3]:
   before=bridge.stat().st_mtime_ns if bridge.exists()else 0
   run('source-focus-step','--tracks','--focus-index',str(index));deadline=time.monotonic()+6
   while not bridge.exists()or bridge.stat().st_mtime_ns<=before:
    if time.monotonic()>deadline:raise RuntimeError('Fresh source focus missing; no duplicate input')
    time.sleep(.05)
   data=json.loads(bridge.read_text());shutil.copyfile(bridge,evidence/('resultspage-windows-focus-source-'+str(index)+'.json'))
  shutil.copyfile(bridge,evidence/'resultspage-windows-focus-source.json');print('Original actual focus states',[(x['label'],x['focusVisible'],x['active'])for x in data['rows']],flush=True)
 finally:run('source-close','--tracks')
if not a.source_only:
 run('launch','--interactive-only','--focus-input')
 try:
  for index in [1,2,3]:run('focus-step','--focus-index',str(index))
  run('capture','--focus-input')
 finally:run('close')
 run('focus-read')
 print('New native focus scope recorded; strict comparison/cleanup proof required',flush=True)
