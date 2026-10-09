"""Compare original parent-relative hint boxes/text/color and full mounted rows."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];s=json.loads((r/f'docs/replica/source/welcomeentryhint-{os}-source.json').read_text());candidate=Path(sys.argv[2]);n=json.loads((candidate/'welcomeentryhint-native.json').read_text());assert len(s['rows'])==len(n['rows'])==8 and s['dpr']==n['dpr']==2
errors=[]
def compare(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():
   if k!='style':compare(v,b.get(k),p+'.'+k)
 elif isinstance(a,(float,int)):
  if b is None or abs(a-b)>.001:errors.append(dict(path=p,source=a,native=b))
 elif a!=b:errors.append(dict(path=p,source=a,native=b))
for a,b in zip(s['rows'],n['rows']):compare(a,b,a['id'])
for x in n['lifecycle']:
 assert x['attached'] and x['children']==2;compare(next(y for y in s['rows'] if y['id']==x['row']['id']),x['row'],'mounted.'+x['row']['id'])
v=dict(cases=8,lifecycle=3,errors=errors);(r/f'docs/replica/source/welcomeentryhint-{sys.argv[3] if len(sys.argv)>3 else os}-comparison.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:20],ensure_ascii=False,indent=2));sys.exit(bool(errors))
