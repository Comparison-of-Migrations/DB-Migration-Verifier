from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];source=Path(sys.argv[2])if len(sys.argv)>2 else r/f'docs/replica/source/connectionpage-{os}-source.json';native=Path(sys.argv[3])if len(sys.argv)>3 else r/f'docs/replica/source/connectionpage-{os}-native/connectionpage-native.json';s=json.loads(source.read_text());n=json.loads(native.read_text());assert len(s['rows'])==len(n['rows'])==96
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
for a,b in zip(s['rows'],n['rows']):compare(a,b,a['id'])
for a,b in zip(s['validation'],n['validation']):compare({'id':a['id'],'error':a['error']},b,a['id'])
v=dict(phase='HEADLESS_FAILED'if errors else 'HEADLESS_PASSED',cases=96,validation=len(s['validation']),errors=errors)if os=='headless'else dict(cases=96,validation=len(s['validation']),errors=errors);(r/f'docs/replica/source/connectionpage-{os}-comparison.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:30],ensure_ascii=False,indent=2));sys.exit(bool(errors))
