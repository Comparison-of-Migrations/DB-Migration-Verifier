"""Compare native TopBar against independently recorded original DOM, without geometry injection."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];assert os in ['mac','windows']
a=json.loads((r/f'docs/replica/source/topbar-{os}-source.json').read_text());b=json.loads((r/f'docs/replica/source/topbar-{os}-native/topbar-native.json').read_text());assert a['dpr']==b['dpr']==2 and len(a['rows'])==len(b['rows'])==8
errors=[]
def compare(s,n,path):
 if isinstance(s,dict):
  for k,v in s.items():compare(v,n[k],path+'.'+k)
 elif isinstance(s,list):
  assert len(s)==len(n)
  for i,(x,y) in enumerate(zip(s,n)):compare(x,y,path+'.'+str(i))
 elif isinstance(s,(int,float)) and not isinstance(s,bool):
  if abs(s-n)>.001:errors.append({'path':path,'source':s,'native':n})
 elif s!=n:errors.append({'path':path,'source':s,'native':n})
for s,n in zip(a['rows'],b['rows']):compare(s,n,s['id'])
p=r/f'docs/replica/source/topbar-{os}-comparison.json';p.write_text(json.dumps({'platform':os,'cases':len(a['rows']),'errors':errors},ensure_ascii=False,indent=2)+'\n');print('TopBar',os,'8 cases',len(errors),'differences');print(json.dumps(errors[:8],ensure_ascii=False));sys.exit(bool(errors))
