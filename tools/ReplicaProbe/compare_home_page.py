"""Compare the actual home() subtree; parent media/padding remain source metadata."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];s=json.loads((r/f'docs/replica/source/homepage-{os}-source.json').read_text());candidate=Path(sys.argv[2]);n=json.loads((candidate/'homepage-native.json').read_text());assert len(s['rows'])==len(n['rows'])==16 and s['dpr']==n['dpr']==2
errors=[]
def compare(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():
   if k in ('parent','case') or k=='box' and p.endswith('.report') and not a['visible']:continue
   compare(v,b.get(k),p+'.'+k)
 elif isinstance(a,list):
  assert len(a)==len(b)
  for i,(x,y) in enumerate(zip(a,b)):compare(x,y,p+f'[{i}]')
 elif isinstance(a,(int,float)) and not isinstance(a,bool):
  if b is None or abs(a-b)>.001:errors.append(dict(path=p,source=a,native=b))
 elif a!=b:errors.append(dict(path=p,source=a,native=b))
for a,b in zip(s['rows'],n['rows']):compare(a,b,a['id'])
for x in n['lifecycle']:
 assert x['attached'] and x['children']==5;compare(next(y for y in s['rows'] if y['id']==x['row']['id']),x['row'],'mounted.'+x['row']['id'])
if n['inputs']:
 assert len(n['inputs'])==12
 for i,x in enumerate(n['inputs']):assert x['count']==i+1 and x['action']==['start','open','sample','research'][i//3] and x['method']==['pointer','Return','Space'][i%3]
v=dict(cases=16,lifecycle=3,inputs=len(n['inputs']),errors=errors);(r/f'docs/replica/source/homepage-{sys.argv[3] if len(sys.argv)>3 else os}-comparison.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:25],ensure_ascii=False,indent=2));sys.exit(bool(errors))
