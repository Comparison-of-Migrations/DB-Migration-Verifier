"""Strict independently measured research grid and attached lifecycle comparison."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];s=json.loads((r/f'docs/replica/source/welcomepaper-{os}-source.json').read_text());candidate=Path(sys.argv[2]);n=json.loads((candidate/'welcomepaper-native.json').read_text());assert len(s['rows'])==len(n['rows'])==12
errors=[]
def compare(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():compare(v,b.get(k) if isinstance(b,dict) else None,p+'.'+k)
 elif isinstance(a,list):
  if not isinstance(b,list) or len(a)!=len(b):errors.append(dict(path=p,source=a,native=b))
  else:
   for i,(x,y) in enumerate(zip(a,b)):compare(x,y,p+f'[{i}]')
 elif isinstance(a,(float,int)):
  if b is None or abs(a-b)>.001:errors.append(dict(path=p,source=a,native=b))
 elif a!=b:errors.append(dict(path=p,source=a,native=b))
def contract(v):
 if isinstance(v,dict):return{k:contract(x) for k,x in v.items() if k not in ['style','case','fontSize']}
 if isinstance(v,list):return[contract(x) for x in v]
 return v
for a,b in zip(s['rows'],n['rows']):compare(contract(a),b,a['id'])
for x in n['lifecycle']:
 assert x['attached'] and x['children']==4
 compare(contract(next(a for a in s['rows'] if a['id']==x['row']['id'])),x['row'],'lifecycle.'+x['row']['id'])
if n['input']:assert [x['requests'] for x in n['input']]==[1,2,3] and all(x['focused'] for x in n['input'])
v=dict(cases=12,lifecycle=3,input=n['input'],errors=errors);(r/f'docs/replica/source/welcomepaper-{sys.argv[3] if len(sys.argv)>3 else os}-comparison.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:20],ensure_ascii=False,indent=2));sys.exit(bool(errors))
