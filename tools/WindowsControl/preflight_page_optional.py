"""Affected optional Windows namedleaf KO/EN only; immediately close exact owned window."""
from pathlib import Path
import argparse,json,subprocess,sys,time
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--stage',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
def run(script,*args):
 x=subprocess.run([sys.executable,str(r/'tools/WindowsControl'/script),'--stage',str(a.stage),'--output',str(a.output),*args],cwd=r,text=True,capture_output=True)
 (a.output/('process-'+str(len(list(a.output.glob('process-*'))))+'.json')).write_text(json.dumps({'script':script,'args':args,'exitCode':x.returncode,'stdout':x.stdout,'stderr':x.stderr}))
 print(script,x.returncode,flush=True)
 if x.returncode:raise RuntimeError(x.stderr or x.stdout)
try:
 run('preflight_page.py','launch')
 run('preflight_page_semantics.py','--label','ko')
 run('preflight_page.py','invoke','--target','language');time.sleep(.5)
 run('preflight_page_semantics.py','--label','en')
finally:
 try:run('preflight_page.py','close')
 finally:run('preflight_page.py','cleanup')
for lang in ['ko','en']:
 d=json.loads((a.output/f'native-{lang}-semantics.json').read_text());v=d['views'][0]
 assert not v['truncated'] and len(v['nodes'])==3
 assert all(x['node']['type']=='ControlType.Text' and len(x['children'])==0 for x in v['nodes'])
print('Affected optional Windows ControlView namedleaf KOEN2 exact3Text/0children and cleanup0')
