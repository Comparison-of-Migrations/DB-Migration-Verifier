"""Strict parent geometry and literal state/data comparison; child locks remain separate."""
import json,sys
from pathlib import Path
lifecycle=sys.argv[1]=='--lifecycle';offset=1 if lifecycle else 0;s=json.loads(Path(sys.argv[1+offset]).read_text());n=json.loads((Path(sys.argv[2+offset])/('resultspage-lifecycle.json' if lifecycle else 'resultspage-native.json')).read_text());errors=[]
def eq(a,b,path):
 if isinstance(a,(int,float)) and isinstance(b,(int,float)):
  if abs(a-b)>0.011:errors.append({'path':path,'source':a,'native':b})
 elif isinstance(a,dict) and isinstance(b,dict):
  for k in b:
   if k in a:eq(a[k],b[k],path+'.'+k)
 elif isinstance(a,list) and isinstance(b,list):
  if len(a)!=len(b):errors.append({'path':path,'sourceCount':len(a),'nativeCount':len(b)})
  for i,(x,y) in enumerate(zip(a,b)):eq(x,y,path+f'[{i}]')
 elif a!=b:errors.append({'path':path,'source':a,'native':b})
identity='label' if lifecycle else 'id';source={x[identity]:x for x in s['rows']};native={x[identity]:x for x in n['rows']}
if len(s['rows'])!=len(n['rows']) or set(source)!=set(native):errors.append({'path':'coverage','source':list(source),'native':list(native)})
for row in n['rows']:
 if row[identity] in source:eq(source[row[identity]],row,row[identity])
r={'status':'PASSED' if not errors else 'FAILED','sourceCases':len(s['rows']),'nativeCases':len(n['rows']),'errors':errors}
Path(sys.argv[3+offset]).write_text(json.dumps(r,ensure_ascii=False,indent=2));print(json.dumps({'status':r['status'],'errors':len(errors),'first':errors[:12]},ensure_ascii=False));sys.exit(bool(errors))
