"""Compare attached card geometry/style with independent original label measurements."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2]/'docs/replica/source';platform=sys.argv[1];assert platform in ['mac','windows']
s=json.loads((r/f'matchoption-{platform}-source.json').read_text());n=json.loads((r/f'matchoption-{platform}-native/matchoption-native.json').read_text());assert len(s['rows'])==len(n['rows'])==136 and s['dpr']==n['dpr']==2;errors=[]
def compare(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():compare(v,b[k],p+'.'+k)
 elif isinstance(a,(float,int))and not isinstance(a,bool):
  if abs(a-b)>.001:errors.append(dict(path=p,source=a,native=b))
 elif a!=b:errors.append(dict(path=p,source=a,native=b))
for a,b in zip(s['rows'],n['rows']):compare(a,b,a['id'])
(r/f'matchoption-{platform}-comparison.json').write_text(json.dumps(dict(platform=platform,cases=136,errors=errors),ensure_ascii=False,indent=2)+'\n');print(platform,len(errors),'differences');print(json.dumps(errors[:12],ensure_ascii=False));sys.exit(bool(errors))
