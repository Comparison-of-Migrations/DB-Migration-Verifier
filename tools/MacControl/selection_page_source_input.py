"""Finite actual original Selection parent input/AX; fresh nonce and immediate scoped close."""
from pathlib import Path
import json,subprocess,time,uuid,shutil
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/workers/preflight/selectionpage/source-input-macos'/uuid.uuid4().hex;o.mkdir(parents=True);print(o,flush=True)
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight'
actor=r/'artifacts/replica/workers/preflight/selectionpage/selection_page_source_input_control';endpoint=r/'docs/replica/source/preflight-selectionpage-mac-input.json'
def call(mode,label):
 result=subprocess.run([str(actor),mode,str(o)],cwd=r,text=True,capture_output=True,timeout=12)
 step=o/label;step.mkdir(exist_ok=True);(step/'process.json').write_text(json.dumps(dict(mode=mode,exitCode=result.returncode,stdout=result.stdout,stderr=result.stderr)))
 for file in o.glob(mode+'-*.json'):shutil.copyfile(file,step/file.name)
 if mode=='inspect':
  for file in o.glob('source-*.json'):shutil.copyfile(file,step/file.name)
 assert result.returncode==0,result.stderr
 return json.loads(result.stdout) if result.stdout.startswith('{') else None
call('guard','00-guard');url='http://10.211.55.2:8768/selectionpage-input.html?os=mac&batch='+o.name+'&resume=after-trial-navigation'
try:
 subprocess.run(['/usr/bin/open','-na','/Applications/Google Chrome.app','--args','--new-window',url],check=True)
 time.sleep(.6);call('activate','01-activate');deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  try:
   if json.loads(endpoint.read_text()).get('batch')==o.name:break
  except (FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.08)
 else:raise RuntimeError('Fresh exactnonce original state absent; no duplicate launch')
 # KO/EN source AX and language already preserved in prior exact0 scope.
 checks=[]
 steps=[('expand-data',lambda s:'data'in s['expanded'] and 'data'in s['categories']),('category-data',lambda s:'data'not in s['categories'] and 'data'in s['expanded']),('search-orders',lambda s:s['query']=='orders'and len(s['tables'])==3 and s['shown']==['demo.orders']),('deselect',lambda s:'demo.orders'not in s['tables']and len(s['tables'])==2),('bulk',lambda s:'demo.orders'in s['tables']and len(s['tables'])==3),('mapping',lambda s:s['modal']and s['modal']['type']=='mapping'and s['modal']['payload']=='demo.orders'),('escape',lambda s:s['modal']is None and s['query']=='orders'),('clear-search',lambda s:s['query']==''and s['focus']['id']=='table-search'),('search-none',lambda s:s['query']=='missing-table'and len(s['tables'])==3 and s['bulk']['disabled']and s['deselectDisabled']),('escape',lambda s:s['query']==''and s['focus']['id']=='table-search'),('passive',lambda s:s['focus']['id']!='table-search'and len(s['tables'])==3),('category-trial',lambda s:'trial'in s['categories']and s['reviewDisabled']),('trial',lambda s:s['screen']=='trial'),('back',lambda s:s['screen']=='select'and 'data'in s['expanded']and 'trial'in s['categories'])]
 for index,(mode,expect) in enumerate(steps[13:],start=13):
  if mode in ['mapping','passive','trial']:call('scroll-table','setup-scroll-'+mode)
  if mode=='category-trial':call('scroll-top','setup-scroll-top')
  label=f'{index+6:02d}-{mode}';state=call(mode,label);deadline=time.monotonic()+3
  while not expect(state) and time.monotonic()<deadline:
   time.sleep(.08);fresh=json.loads(endpoint.read_text())
   if fresh.get('batch')==o.name:state=fresh
  (o/label/'source-final.json').write_text(json.dumps(state,ensure_ascii=False,indent=2));passed=expect(state);checks.append(dict(label=label,passed=passed));(o/'input-comparison.json').write_text(json.dumps(checks,indent=2));assert passed,(label,state)
 print('Original finite14 input + actual KOEN AX captured',flush=True)
finally:
 try:call('close','90-close')
 finally:time.sleep(.3);call('cleanup','91-cleanup')
