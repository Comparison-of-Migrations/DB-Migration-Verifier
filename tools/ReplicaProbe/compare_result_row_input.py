"""Actual guarded pointer/keyboard snapshots plus attached native playback lifecycle."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[2]/'docs/replica/source';errors=[];samples=0
aliases={'reduced-on':'key-m-target','reduced-enter':'key-enter-target','partial-update':'key-u-target','english-update':'key-e-target','disabled':'key-d-target'}
for platform in ['mac','windows']:
 folder=r/f'resultrow-{platform}-input'
 def state(name):
  global samples
  file=folder/(name+'.json')
  if not file.exists()and platform=='windows':file=folder/(aliases.get(name,name)+'.json')
  samples+=1;return json.loads(file.read_text())
 def check(name,fields):
  v=state(name)
  for key,expected in fields.items():
   actual=len(v['requests']) if key=='requestCount'else v[key]
   if actual!=expected:errors.append(dict(platform=platform,state=name,property=key,expected=expected,actual=actual))
  return v
 check('initial',dict(requestCount=0,enabled=True,counts=[1,0,11],english=False,changed=False,reduced=False))
 for i,name in enumerate(['title-click','icon-click','count-click','chevron-click'],1):check('actual-'+name,dict(requestCount=i,pressed=False,hover=True,focusVisible=False,scale=1,chevron=2))
 check('hold-inside',dict(requestCount=4,pressed=True,scale=.98,hover=True,focusVisible=False))
 check('actual-hold',dict(requestCount=5,pressed=False,scale=1,focusVisible=False))
 check('cancel-inside',dict(requestCount=5,pressed=True,scale=.98))
 check('cancel-outside',dict(requestCount=5,pressed=True,hover=False,scale=.98,chevron=0))
 check('actual-cancel',dict(requestCount=5,pressed=False,hover=False,scale=1,chevron=0))
 check('actual-hover',dict(requestCount=5,hover=True,chevron=2,background='#ff353740'))
 check('actual-leave',dict(requestCount=5,hover=False,chevron=0,background='#00000000'))
 check('key-enter-peer',dict(requestCount=6,requests=['data']*5+['objects'],focus=False))
 check('key-space-target',dict(requestCount=8,requests=['data']*5+['objects','data','data'],focus=True,focusVisible=True))
 check('reduced-on',dict(requestCount=8,reduced=True,scale=1,opacity=1))
 check('reduced-enter',dict(requestCount=9,reduced=True,scale=1,opacity=1))
 check('partial-update',dict(requestCount=9,counts=[7,5,0],changed=True,description='중단 · 미검증 포함 · 전체 12'))
 check('english-update',dict(requestCount=9,english=True,counts=[7,5,0],name='Data: Different 7, Unverified 5, Equal 0. View details'))
 check('disabled',dict(requestCount=9,enabled=False,pressed=False,focusVisible=False,background='#ff3b3d44'))
 check('disabled-count-click',dict(requestCount=9,enabled=False,pressed=False,counts=[7,5,0]))
 check('enabled-restored',dict(requestCount=9,enabled=True))
 check('korean-restored',dict(requestCount=9,english=False))
 check('counts-restored',dict(requestCount=9,counts=[1,0,11],changed=False))
 check('normal-restored',dict(requestCount=9,reduced=False))
 phases=json.loads((r/f'resultrow-{platform}-lifecycle/resultrow-lifecycle.json').read_text())['phases'];assert len(phases)==9
 for phase in phases:
  name=phase['phase'];samples+=1
  expectedAttached=not name.startswith('detached');playing=name in ['click-playing','second-playing'];reduced=name.startswith('reduced')
  if phase['attached']!=expectedAttached or phase['playing']!=playing or phase['reduced']!=reduced:errors.append(dict(platform=platform,phase=name,state=phase))
  if playing:
   if not(.86<=phase['opacity']<1 and .98<=phase['scale']<1):errors.append(dict(platform=platform,phase=name,animation=phase))
  elif phase['opacity']!=1 or phase['scale']!=1:errors.append(dict(platform=platform,phase=name,restoration=phase))
  expectedCount=0 if name=='attached'else 1 if name.startswith('click')else 3 if reduced else 2
  if len(phase['requests'])!=expectedCount:errors.append(dict(platform=platform,phase=name,requestCount=phase['requests']))
report=dict(actualSnapshots=48,attachedLifecyclePhases=18,samples=samples,failed=len(errors),scope='Each OS24 actual snapshots: entire row children, held/cancel, independent peer, Enter/Space, disabled/reduced, data/language/normal restoration. OS inputs serialized. Source original actions6 documented separately. Lifecycle uses production OnClick on attached window; it is not an extra input simulation claim. UIA focus on hover/leave is fixture focus modality, not a source hover focus change.',errors=errors)
(r/'resultrow-input-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items()if k not in ['errors','scope']});print(errors[:12]);assert not errors
