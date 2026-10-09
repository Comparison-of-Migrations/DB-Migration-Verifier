"""Independent original and candidate card geometry/state/palette; no tolerances added for wrapping."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];s=json.loads((r/f'docs/replica/source/welcomepapercontent-{os}-source.json').read_text());candidate=Path(sys.argv[2]) if len(sys.argv)>2 else r/f'docs/replica/source/welcomepapercontent-{os}-native';n=json.loads((candidate/'welcomepapercontent-native.json').read_text());assert len(s['rows'])==len(n['rows'])==8
errors=[]
def compare(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():compare(v,b.get(k),p+'.'+k)
 elif isinstance(a,list):
  if not isinstance(b,list) or len(a)!=len(b):errors.append(dict(path=p,source=a,native=b))
  else:
   for i,(x,y) in enumerate(zip(a,b)):compare(x,y,p+f'[{i}]')
 elif isinstance(a,(float,int)):
  if b is None or abs(a-b)>.001:errors.append(dict(path=p,source=a,native=b))
 elif a!=b:errors.append(dict(path=p,source=a,native=b))
def geometry_contract(v):
 if isinstance(v,dict):return {k:geometry_contract(x) for k,x in v.items() if k not in ['fontSize','fontWeight','lineHeight','letterSpacing','marginTop','marginBottom','maxWidth']}
 if isinstance(v,list):return [geometry_contract(x) for x in v]
 return v
for a,b in zip(s['rows'],n['rows']):compare(geometry_contract(a),b,a['id'])
v=dict(cases=8,errors=errors);(r/f'docs/replica/source/welcomepapercontent-{sys.argv[3] if len(sys.argv)>3 else os}-comparison.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:20],ensure_ascii=False,indent=2));sys.exit(bool(errors))
