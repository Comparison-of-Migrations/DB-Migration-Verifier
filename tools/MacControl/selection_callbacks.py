"""Finite new Selection parent physical callbacks; no replay of old full-flow matrices."""
from pathlib import Path
import argparse,json,hashlib,plistlib,shutil,subprocess,time
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('action',choices=['prepare','launch','batch','close']);p.add_argument('--english',action='store_true');p.add_argument('--resume',action='store_true');p.add_argument('--phase',default='');p.add_argument('--resume-after',choices=['query','mapping-draft'],default='query');a=p.parse_args();m=a.manifest.resolve();v=json.loads(m.read_text());assert v['owner']=='preflight'and v['component']=='pageintegration'and v['runtime']=='osx-arm64'and v['buildExitCode']==0
assert not a.phase or all(c.islower()or c.isdigit()or c=='-'for c in a.phase)
base=m.parent;out=base/(('actual-selectioncallbacks-en'if a.english else'actual-selectioncallbacks-ko')+('-resume'if a.resume else'')+('-'+a.phase if a.phase else''));out.mkdir(exist_ok=True);bundle=base/'SelectionCallbacksCheck.app';helper=r/'artifacts/replica/workers/preflight/selectioncallbacks/native-input';launch=r/'artifacts/replica/workers/preflight/selectioncallbacks/native-launch'
if a.action=='prepare':
 assert not bundle.exists();shutil.copytree(Path(v['binary']).parent,bundle/'Contents/MacOS');assert hashlib.sha256((bundle/'Contents/MacOS/ReplicaProbe.dll').read_bytes()).hexdigest()==v['binarySha256'];(bundle/'Contents/Info.plist').write_bytes(plistlib.dumps(dict(CFBundleIdentifier='local.dbmv.preflight.selectioncallbacks.'+base.name,CFBundleName='Selection callback verification',CFBundleExecutable='ReplicaProbe',CFBundlePackageType='APPL',NSHighResolutionCapable=True)));print(bundle);raise SystemExit
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight'
if a.action=='launch':
 assert not(out/'current-native.json').exists();
 if a.resume:(out/'selectioncallbacks-resume.json').write_text(json.dumps({'checkpoint':'after-mapping-draft-cancel'if a.resume_after=='mapping-draft'else'after-query-orders'}))
 (out/'cases.json').write_text('[]\n');shutil.copyfile(r/'artifacts/replica/workers/preflight/selectioncallbacks/source/bcb156a1156d4681b0f59ee9741f5f4c/original-mac.json',out/'oracle.json');s=subprocess.run([str(launch),str(m),str(out),'en'if a.english else'ko'],capture_output=True,text=True,timeout=15);(out/'launch.log').write_text(s.stdout+s.stderr);assert s.returncode==0,s.stderr;print(s.stdout);raise SystemExit
pid=json.loads((out/'current-native.json').read_text())['pid'];binary=bundle/'Contents/MacOS/ReplicaProbe.dll';checks=[]
def raw():
 files=sorted(out.glob('selectioncallback-state-*.json'));assert files;return json.loads(files[-1].read_text())
def wait(predicate,after=-1,label='state'):
 end=time.monotonic()+8
 while time.monotonic()<end:
  try:
   s=raw()
   if s['sequence']>after and predicate(s):return s
  except(AssertionError,FileNotFoundError,json.JSONDecodeError):pass
  time.sleep(.03)
 raise RuntimeError('Fresh new callback absent '+label+'; no replay')
def run(mode,id,label,value=''):
 s=subprocess.run([str(helper),str(pid),str(binary),mode,id,value,str(out),label],capture_output=True,text=True,timeout=10);(out/(label+'.actor.log')).write_text(s.stdout+s.stderr);assert s.returncode==0,s.stderr

def step(label,id,predicate,mode='click',value=''):
 
 if mode in ['click','edit']and not id.startswith(('Footer-','MatchingDialog-'))and any(c['id']==id for c in raw()['controls']):visible(id)
 before=raw()['sequence'];run(mode,id,label,value);s=wait(predicate,before,label);checks.append({'id':label,'sequence':s['sequence'],'state':s,'passed':True});(out/'comparison.json').write_text(json.dumps({'scope':'New connected Selection physical input and API/setup-labeled boundaries; prior full flow29 and child12506 reused','checks':checks},ensure_ascii=False,indent=2)+'\n');print(label,flush=True);return s

def visible(id):
 for n in range(4):
  s=raw();targets=[c for c in s['controls']if c['id']==id]
  if targets and targets[0]['box']['y']>=98 and targets[0]['box']['y']+targets[0]['box']['height']<=s['height']-84:return
  before=s['sequence'];run('scroll-up'if targets and targets[0]['box']['y']<98 else'scroll-down',id,'setup-scroll-'+str(len(checks))+'-'+str(n));wait(lambda s:True,before)
 raise RuntimeError('Target outside fresh main viewport '+id)

def cleanup():
 try:run('close','start','final-close')
 finally:
  end=time.monotonic()+5
  while time.monotonic()<end and subprocess.run(['kill','-0',str(pid)],capture_output=True).returncode==0:time.sleep(.05)
  absent=subprocess.run(['kill','-0',str(pid)],capture_output=True).returncode!=0;(out/'cleanup.json').write_text(json.dumps({'pid':pid,'nativeProcess':0 if absent else 1,'nativeWindow':0 if absent else None}));assert absent
if a.action=='close':cleanup();raise SystemExit
try:
 wait(lambda s:s['ready']and s['state']['screen']=='select'and (not s['state']['stopped']and s['modal']and s['modal']['draft']=='archive.orders'if a.resume and a.resume_after=='mapping-draft'else s['state']['stopped']))
 category_announcement='3 categories selected'if a.english else'3 개 분류 선택';expanded_announcement='Data details expanded'if a.english else'데이터 세부 검사 펼침'
 collapsed_announcement='Data details collapsed'if a.english else'데이터 세부 검사 닫힘'
 if not a.resume:
  step('category-data','category-data',lambda s:s['reason']=='category:data'and s['state']['categories']==['schema','access','objects']and not s['state']['stopped']and s['state']['announcement']==category_announcement)
  step('stopped-fixture-setup','start',lambda s:s['state']['stopped'],mode='seed-stopped')
  step('expand-data','expand-data',lambda s:s['reason']=='expand:data'and s['state']['expanded']==['data']and s['state']['stopped']and s['state']['announcement']==expanded_announcement)
  step('collapse-data','expand-data',lambda s:s['reason']=='expand:data'and s['state']['expanded']==[]and s['state']['stopped']and s['state']['announcement']==collapsed_announcement)
  visible('table-search');step('query-orders','table-search',lambda s:s['state']['query']=='orders'and s['shown']==['demo.orders']and s['state']['stopped']and s['state']['announcement']==collapsed_announcement,mode='edit',value='orders')
 if not(a.resume and a.resume_after=='mapping-draft'):
  step('table-orders','Selection-table-demo.orders',lambda s:s['reason']=='table:demo.orders'and s['state']['tables']==['demo.customers','demo.line_items']and not s['state']['stopped']and s['state']['announcement']==collapsed_announcement)
  step('filtered-bulk','table-all',lambda s:s['reason']=='bulk'and s['state']['tables']==['demo.customers','demo.line_items','demo.orders']and s['state']['announcement']==('Selection updated for matching tables.'if a.english else'검색 결과의 선택을 변경했습니다.'))
  step('filtered-deselect','Selection-clear-tables',lambda s:s['reason']=='clear-tables'and s['state']['tables']==['demo.customers','demo.line_items']and s['state']['announcement']==('Matching tables were deselected.'if a.english else'검색 결과의 선택을 해제했습니다.'))
  visible('Selection-mapping-demo.orders');step('mapping-open-cancel','Selection-mapping-demo.orders',lambda s:s['modal']is not None)
  step('mapping-draft-cancel','MatchingDialog-choice-3',lambda s:s['modal']and s['modal']['draft']=='archive.orders')
 step('mapping-cancel','MatchingDialog-close',lambda s:s['modal']is None and s['state']['maps']['demo.orders']=='demo.orders'and s['state']['focus']=='mapping:demo.orders')
 run('inspect','start','post-mapping-cancel')
 step('mapping-open-context','Selection-mapping-demo.orders',lambda s:s['modal']is not None);step('mapping-target-context','MatchingDialog-choice-3',lambda s:s['modal']and s['modal']['draft']=='archive.orders');step('mapping-query-preserved','MatchingDialog-apply',lambda s:s['modal']is None and s['state']['maps']['demo.orders']=='archive.orders'and s['state']['query']=='orders')
 visible('table-search');step('query-target','table-search',lambda s:s['state']['query']=='archive'and s['shown']==['demo.orders'],mode='edit',value='archive')
 step('mapping-open-removal','Selection-mapping-demo.orders',lambda s:s['modal']is not None);step('mapping-target-removal','MatchingDialog-choice-1',lambda s:s['modal']and s['modal']['draft']=='demo.orders');step('mapping-opener-removed','MatchingDialog-apply',lambda s:s['modal']is None and not s['shown']and s['state']['maps']['demo.orders']=='demo.orders'and s['state']['focus']=='BODY')
 visible('table-search');step('query-clear','table-search',lambda s:s['state']['query']==''and len(s['shown'])==3,mode='edit',value='')
 visible('Selection-mapping-demo.orders');step('modal-language-open','Selection-mapping-demo.orders',lambda s:s['modal']is not None)
 step('modal-language-query','MatchingDialog-search',lambda s:s['modal']and s['modal']['query']=='archive',mode='edit',value='archive')
 step('modal-language-draft','MatchingDialog-choice-0',lambda s:s['modal']and s['modal']['draft']=='archive.orders')
 step('modal-language-handler-api','start',lambda s:s['state']['english']!=a.english and s['modal']and s['modal']['draft']=='demo.orders'and s['modal']['query']=='',mode='language-api')
 step('modal-language-cancel','MatchingDialog-close',lambda s:s['modal']is None and s['state']['focus']=='BODY')
 step('language-fixture-reset','start',lambda s:s['state']['english']==a.english,mode='language-api')
 step('resize-fixture-seed','start',lambda s:all(s['identity'].values()),mode='seed-resize')
 for width in[960,1180,1280]:step('resize-'+str(width),'start',lambda s,w=width:s['width']==w and all(s['identity'].values())and s['state']['query']==''and s['state']['tables']==['demo.customers','demo.line_items'],mode='resize')
 step('error-navigation-preflight','Footer-back',lambda s:s['state']['screen']=='capabilities')
 step('error-navigation-connect','Footer-back',lambda s:s['state']['screen']=='connect')
 step('error-host-empty','source-host',lambda s:not s['state']['connected'],mode='edit',value='')
 step('error-invalid-connect','Footer-connect',lambda s:bool(s['state']['error']))
 step('error-back-home','Footer-back',lambda s:s['state']['screen']=='home'and not s['state']['error'])
 step('error-start-reset','start',lambda s:s['state']['screen']=='connect'and not s['state']['error']and s['state']['tables']==['demo.customers','demo.orders','demo.line_items'])
finally:cleanup()
print('Finite new parent callbacks saved and exactclose0')
