"""Scoped WelcomeSteps LOCK exception: new Home widths and preserved legacy widths."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];s=json.loads((r/f'docs/replica/source/homepage-tracks-{os}-source.json').read_text());n=json.loads((Path(sys.argv[2])/'welcomesteps-native.json').read_text());assert len(s['rows'])==len(n['rows'])==22 and s['dpr']==n['dpr']==2
errors=[]
def compare(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():
   if k!='style':compare(v,b.get(k),p+'.'+k)
 elif isinstance(a,list):
  assert len(a)==len(b)
  for i,(x,y) in enumerate(zip(a,b)):compare(x,y,p+f'[{i}]')
 elif isinstance(a,(int,float)):
  if b is None or abs(a-b)>.001:errors.append({'path':p,'source':a,'native':b})
 elif a!=b:errors.append({'path':p,'source':a,'native':b})
for a,b in zip(s['rows'],n['rows']):compare(a,b,a['id'])
assert len(n['lifecycle'])==3
for x in n['lifecycle']:assert x['attached'] and x['children']==3 and x['numbers']==['01','02','03']
p=r/f'docs/replica/source/homepage-tracks-{sys.argv[3] if len(sys.argv)>3 else os}-comparison.json';p.write_text(json.dumps({'cases':22,'lifecycle':3,'errors':errors},ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:12],ensure_ascii=False,indent=2));sys.exit(bool(errors))
