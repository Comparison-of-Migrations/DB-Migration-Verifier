"""Strict drawer comparison; separately report measured OS root-scrollbar offsets."""
from pathlib import Path
import copy,json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1]
s=json.loads((r/f'docs/replica/source/matchingdialog-{os}-source.json').read_text())
n=json.loads((r/f'docs/replica/source/matchingdialog-{os}-native/matchingdialog-native.json').read_text())
assert len(s['rows'])==len(n['rows'])==48
errors=[]
def compare(a,b,path):
 if isinstance(a,dict):
  for k,v in a.items():
   if k=='environment':continue # Measurement context, not a candidate UI property.
   if k not in b:errors.append(dict(path=path+'.'+k,source=v,native='MISSING'));continue
   compare(v,b[k],path+'.'+k)
 elif isinstance(a,list):
  if len(a)!=len(b):errors.append(dict(path=path,source=len(a),native=len(b)));return
  for i,(x,y)in enumerate(zip(a,b)):compare(x,y,path+f'[{i}]')
 elif isinstance(a,(float,int))and not isinstance(a,bool):
  if abs(a-b)>.001:errors.append(dict(path=path,source=a,native=b))
 elif a!=b:errors.append(dict(path=path,source=a,native=b))
for a,b in zip(s['rows'],n['rows']):compare(a,b,a['id'])
raw_errors=errors;errors=[];allowed=[]
for a,b in zip(s['rows'],n['rows']):
 a=copy.deepcopy(a);env=a.get('environment',{})
 if os=='windows' and env:
  gx=env['innerWidth']-env['clientWidth'];gy=env['innerHeight']-env['clientHeight']
  if gx or gy:
   # Original html/body min-height creates the default classic root bars below
   # the supported 720px desktop height. No dimensions/text/state are exempted.
   assert env['minWidth']=='960px' and env['minHeight']=='720px'
   assert env['innerHeight']==600 and gx==15 and gy==(15 if env['innerWidth']==960 else 0)
   assert abs(a['parts']['dialog']['x']+gx-b['parts']['dialog']['x'])<.001
   assert abs(a['parts']['dialog']['y']+gy/2-b['parts']['dialog']['y'])<.001
   for box in list(a['parts'].values())+[c['box']for c in a['cards']]:box['x']+=gx;box['y']+=gy/2
   allowed.append(dict(id=a['id'],reason='Default Windows root scrollbars below original minimum desktop height',environment=env,xOffset=gx,yOffset=gy/2))
 compare(a,b,a['id'])
v=dict(cases=48,rawErrors=raw_errors,allowedOSOffsets=allowed,errors=errors)
(r/f'docs/replica/source/matchingdialog-{os}-comparison.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n')
print(len(errors),'unresolved differences;',len(raw_errors),'raw differences;',len(allowed),'measured OS-offset cases')
print(json.dumps(errors[:24],ensure_ascii=False,indent=2));sys.exit(bool(errors))
