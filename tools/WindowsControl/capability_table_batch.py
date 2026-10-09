"""One authorized Windows Table parent batch, each fresh state copied before next action."""
from pathlib import Path
import subprocess,json,sys,time,hashlib
r=Path(__file__).resolve().parents[2];stage=Path(sys.argv[1]).resolve();v=json.loads(stage.read_text());assert v['owner']=='preflight'and v['component']=='capabilitytable';assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
out=stage.parent/'actual-windows/input';out.mkdir(parents=True,exist_ok=True);driver=r/'tools/WindowsControl/capability_table.py';inputdriver=r/'tools/WindowsControl/capability_table_input.py'
def call(command,label):
 result=subprocess.run([sys.executable,*map(str,command)],cwd=r,capture_output=True,text=True);(out/(label+'-process.json')).write_text(json.dumps({'command':list(map(str,command)),'exitCode':result.returncode,'stdout':result.stdout,'stderr':result.stderr},indent=2)+'\n')
 if result.returncode:raise RuntimeError(result.stderr.strip()or result.stdout.strip())
 print(label+' passed',flush=True)
def invoke(action,label,source=False,target=None):call([driver,action,'--stage',stage,'--output',out,'--label',label]+(['--target',target]if target else[]),('source-'if source else'native-')+label)
def run(source):
 prefix='source-input-'if source else'';
 if source or '--resume-native' not in sys.argv:invoke('source-input-open'if source else'launch','open',source)
 invoke(prefix+'read','initial',source);invoke(prefix+'ax','initial',source)
 for mode in ['pointer','key','passive']:
  # Passive read provides exact before-counter without changing OS focus.
  invoke(prefix+'read',mode+'-before',source);call([inputdriver,mode,'--stage',stage,'--output',out]+(['--source']if source else[]),('source-'if source else'native-')+mode)
 for label in ['en-narrow','ko-narrow','en-wide']:
  invoke(prefix+'invoke',label,source,'next');invoke(prefix+'read',label,source)
 invoke(prefix+'invoke','detach',source,'mount');invoke(prefix+'read','detach',source);invoke(prefix+'detached','detached',source)
 invoke(prefix+'invoke','remount',source,'mount');invoke(prefix+'read','remount',source);invoke(prefix+'ax','remount',source)
 if not source:call([r/'tools/WindowsControl/capability_table_capture.py','--stage',stage,'--output',out/'native-window-final.png'],'native-capture')
 invoke('source-input-close'if source else'close','close',source)
 if not source:invoke('cleanup','cleanup',source)
(out/'actor-environment.json').write_text(json.dumps({'stage':str(stage),'helperSha256':hashlib.sha256((r/'tools/WindowsControl/owned_capability_table_input.cs').read_bytes()).hexdigest(),'sourceFixtureSha256':hashlib.sha256((r/'artifacts/replica/windows-download/capabilitytable-input.html').read_bytes()).hexdigest(),'scope':'first Windows Table parent9 / actual inner semanticAX'},indent=2)+'\n')
if '--resume-native' not in sys.argv:run(True)
run(False)
