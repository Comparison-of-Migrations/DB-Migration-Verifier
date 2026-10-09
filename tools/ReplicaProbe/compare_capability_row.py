"""Independent original and candidate row geometry/state/palette; no tolerances added for wrapping."""
from pathlib import Path
import json,sys,math,argparse
r=Path(__file__).resolve().parents[2];parser=argparse.ArgumentParser();parser.add_argument('platform',choices=['mac','windows']);parser.add_argument('--candidate',type=Path);parser.add_argument('--report',type=Path);args=parser.parse_args();os=args.platform;s=json.loads((r/f'docs/replica/source/capabilityrow-{os}-source.json').read_text());n=json.loads((args.candidate or r/f'docs/replica/source/capabilityrow-{os}-native/capabilityrow-native.json').read_text());assert len(s['rows'])==len(n['rows'])==48
assert len(n['peers'])==48
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
for a,b,peer in zip(s['rows'],n['rows'],n['peers']):
 compare(a,b,a['id'])
 assert peer['id']==a['id'] and peer['role']=='Row' and len(peer['cells'])==4
 assert all(c['role']=='Cell' for c in peer['cells'])
 assert peer['cells'][0]['children']==2 and peer['cells'][3]['children']==1 and peer['cells'][3]['contentChildren']==[2 if a['button'] else 1]
 expected_lines=round(a['detailText']['height']/(math.floor(12*1.6*s['dpr']*64)/(s['dpr']*64)))
 if peer['detailLineCount']!=expected_lines:errors.append(dict(path=a['id']+'.detailLineCount',source=expected_lines,native=peer['detailLineCount']))
v=dict(cases=48,errors=errors);(args.report or r/f'docs/replica/source/capabilityrow-{os}-comparison.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:20],ensure_ascii=False,indent=2));sys.exit(bool(errors))
