"""Independent original and candidate card geometry/state/palette; no tolerances added for wrapping."""
from pathlib import Path
import json,sys,copy
r=Path(__file__).resolve().parents[2];os=sys.argv[1];s=json.loads((r/f'docs/replica/source/resultdetails-{os}-source.json').read_text());n=json.loads((r/f'docs/replica/source/resultdetails-{os}-native/resultdetails-native.json').read_text());assert len(s['rows'])==len(n['rows'])==40
errors=[]
def compare(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():compare(v,b.get(k),p+'.'+k)
 elif isinstance(a,list):
  if not isinstance(b,list) or len(a)!=len(b):errors.append(dict(path=p,sourceLength=len(a),nativeLength=len(b) if isinstance(b,list) else None))
  else:
   for i,(v,w) in enumerate(zip(a,b)):compare(v,w,p+f'[{i}]')
 elif isinstance(a,(float,int)):
  if b is None or abs(a-b)>.001:errors.append(dict(path=p,source=a,native=b))
 elif a!=b:errors.append(dict(path=p,source=a,native=b))
for a,b in zip(s['rows'],n['rows']):
 a={k:v for k,v in a.items() if k!='environment'};compare(a,b,a['id'])
rawErrors=errors;errors=[];allowed=[]
for a,b in zip(s["rows"],n["rows"]):
 a=copy.deepcopy(a);env=a.pop("environment",{})
 if os=="windows" and env:
  gx=env["innerWidth"]-env["clientWidth"];gy=env["innerHeight"]-env["clientHeight"]
  if gx or gy:
   assert env["minWidth"]=="960px" and env["minHeight"]=="720px"
   assert env["innerHeight"] in [400,600] and gx==15 and gy==(15 if env["innerWidth"]==960 else 0)
   assert abs(a["x"]+gx/2-b["x"])<.001 and abs(a["y"]+gy/2-b["y"])<.001
   a["x"]+=gx/2;a["y"]+=gy/2
   allowed.append(dict(id=a["id"],environment=env,xOffset=gx/2,yOffset=gy/2,reason="Measured default Windows root scrollbar center offsets"))
 compare(a,b,a["id"])
v=dict(cases=40,rawErrors=rawErrors,allowedOSOffsets=allowed,errors=errors);(r/f'docs/replica/source/resultdetails-{os}-comparison.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:20],ensure_ascii=False,indent=2));sys.exit(bool(errors))
