"""Strict horizontal source boxes/ranges; DOM integer client metrics kept distinct from real layout."""
import argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('native',type=Path);p.add_argument('output',type=Path);a=p.parse_args();s=json.loads(a.source.read_text());n=json.loads(a.native.read_text());issues=[]
def fail(path,x,y):issues.append({'path':path,'source':x,'native':y})
def check(path,x,y):
 if isinstance(x,(int,float))and isinstance(y,(int,float)):
  if abs(x-y)>0.0001:fail(path,x,y)
 elif x!=y:fail(path,x,y)
check('dpr',s['dpr'],n['dpr']);sources={(c['id'],c['label']):c for c in s['samples']};check('sample count',len(sources),len(n['samples']))
for c in n['samples']:
 key=(c['id'],c['label']);x=sources.get(key)
 if x is None:fail('missing state',None,key);continue
 for b in ['shell','content','extent']:
  for f in ['x','y','width','height']:check(str(key)+'.'+b+'.'+f,x[b][f],c[b][f])
 for f in ['offset','maximum','scrollWidth','scrollHeight']:check(str(key)+'.'+f,x[f],c[f])
 for f in ['clientWidth','clientHeight']:check(str(key)+'.'+f,x[f],math.floor(c[f]+.5))
result={'source':str(a.source),'native':str(a.native),'states':len(n['samples']),'differences':issues,'passed':not issues};a.output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result));raise SystemExit(bool(issues))
