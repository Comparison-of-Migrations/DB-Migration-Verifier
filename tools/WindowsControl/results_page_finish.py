"""One guarded Windows final scope; stop on first failure, retain each fresh record."""
from pathlib import Path
import argparse,hashlib,json,shutil,subprocess,sys,time
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('stage',type=Path);p.add_argument('--targeted-first',action='store_true');p.add_argument('--remaining-reduced',action='store_true');a=p.parse_args()
source=root/'docs/replica/source';folder=source/'resultspage-windows-native';log=source/'resultspage-windows-final-batch.log'
def run(*args):
 result=subprocess.run([sys.executable,str(root/'tools/WindowsControl/results_page.py'),*args],cwd=root,capture_output=True,text=True)
 with log.open('a')as f:f.write(json.dumps(list(args))+'\n'+result.stdout+result.stderr+'\n')
 print(args[0],result.returncode,result.stdout.strip(),flush=True)
 if result.returncode:raise RuntimeError('Guarded Results scope stopped: '+args[0])
def record(name):
 bridge=source/'connection-resultspage-windows-control-ready.json';before=bridge.stat().st_mtime_ns if bridge.exists() else 0
 try:run('tracks-record','--stage',str(a.stage),'--tracks')
 except RuntimeError:
  if not bridge.exists() or bridge.stat().st_mtime_ns<=before:raise
  print('Record transport result resolved by fresh owned evidence; no duplicate invocation',flush=True)
 deadline=time.monotonic()+6
 while not bridge.exists()or bridge.stat().st_mtime_ns<=before:
  if time.monotonic()>deadline:raise RuntimeError('Fresh owned source trace missing')
  time.sleep(.05)
 data=json.loads(bridge.read_text());assert data['frames'],'Source trace empty';shutil.copyfile(bridge,source/name)
def compare(tool,*args):
 result=subprocess.run([sys.executable,str(root/'tools/ReplicaProbe'/tool),*map(str,args)],cwd=root)
 if result.returncode:raise RuntimeError('Fresh scope comparison failed: '+tool)
run('launch','--stage',str(a.stage),'--interactive-only')
if not a.remaining_reduced:
 run('input-first' if a.targeted_first else 'input-batch','--stage',str(a.stage))
 run('track-read','--stage',str(a.stage))
 if a.targeted_first:
  prior=source/'resultspage-windows-first-final-input'
  retained=[f for f in prior.glob('input-*.json') if f.name!='input-filter-unknown-click.json']+[f for f in prior.glob('tracks-trial-*.json')]
  for f in retained:
   assert (folder/f.name).read_bytes()==f.read_bytes(),'Previously passed record changed: '+f.name
  provenance={'retainedHashes':{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in retained},'firstFilterPointer':'fresh targeted first1 on bb98; expected callback state publication awaited','remaining11':'prior bb98 first-final input retained; unchanged product/OS/conditions','normalTrialTracks':'prior bb98 captured5 retained; source strict comparison still required'}
  (source/'resultspage-windows-input-provenance.json').write_text(json.dumps(provenance,indent=2)+'\n')
 compare('compare_results_page_actual.py',folder,source/'resultspage-windows-actual-comparison.json')
 run('capture','--stage',str(a.stage))
run('reduced-mode','--stage',str(a.stage))
run('input-batch','--stage',str(a.stage),'--reduced',*(['--skip-hover'] if a.remaining_reduced else []))
run('track-read','--stage',str(a.stage))
run('close','--stage',str(a.stage))
run('source-open','--tracks')
run('trial-show','--tracks')
run('source-input-batch','--stage',str(a.stage),'--tracks')
record('resultspage-windows-tracks-source.json')
compare('compare_results_page_tracks.py',source/'resultspage-windows-tracks-source.json',folder,source/'resultspage-windows-tracks-comparison.json')
run('source-reduced','--tracks')
run('source-input-batch','--stage',str(a.stage),'--tracks','--reduced')
record('resultspage-windows-reduced-source.json')
compare('compare_results_page_reduced.py',folder,source/'resultspage-windows-reduced-comparison.json','--source',source/'resultspage-windows-reduced-source.json')
bridge=source/'connection-resultspage-windows-input.json';before=bridge.stat().st_mtime_ns if bridge.exists() else 0
run('source-filter-focus','--stage',str(a.stage),'--tracks')
deadline=time.monotonic()+6
while not bridge.exists() or bridge.stat().st_mtime_ns<=before:
 if time.monotonic()>deadline:raise RuntimeError('Fresh actual source filter focus missing')
 time.sleep(.05)
shutil.copyfile(bridge,source/'resultspage-windows-filter-focus-source.json')
print('Source pointer focus',json.loads(bridge.read_text())['rows'][-1],flush=True)
run('source-close','--tracks')
print('Required final Windows input scopes passed; exact cleanup and LOCK remain to be recorded.')
