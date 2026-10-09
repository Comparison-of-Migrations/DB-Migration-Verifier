"""Independent original and candidate card geometry/state/palette; no tolerances added for wrapping."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];s=json.loads((r/f'docs/replica/source/detailsbody-{os}-source.json').read_text());n=json.loads((r/f'docs/replica/source/detailsbody-{os}-native/detailsbody-native.json').read_text());assert len(s['rows'])==len(n['rows'])==80
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
for a,b in zip(s['rows'],n['rows']):compare(a,b,a['id'])
v=dict(cases=80,errors=errors);(r/f'docs/replica/source/detailsbody-{os}-comparison.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:20],ensure_ascii=False,indent=2));sys.exit(bool(errors))
