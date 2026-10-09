"""Strict original/native CapabilityTable parent geometry and intrinsic shared columns."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);a=p.parse_args();s=json.loads(a.source.read_text());n=json.loads(a.candidate.read_text());diff=[]
def check(path,left,right):
 if isinstance(left,dict):
  for key in left:check(path+'.'+key,left[key],right.get(key))
 elif isinstance(left,list):
  if right is None or len(left)!=len(right):diff.append({'path':path,'source':left,'candidate':right});return
  for i,(x,y)in enumerate(zip(left,right)):check(path+f'[{i}]',x,y)
 elif isinstance(left,(float,int)):
  if right is None or abs(left-right)>.0001:diff.append({'path':path,'source':left,'candidate':right})
 elif left!=right:diff.append({'path':path,'source':left,'candidate':right})
check('dpr',s['dpr'],n['dpr']);native={t['id']:t for t in n['tables']}
for table in s['tables']:
 target=native[table['id']]
 for key in ('shell','table','headers','rows','columns','clientWidth','clientHeight','scrollWidth','scrollHeight','scrollLeft'):check(table['id']+'.'+key,table[key],target[key])
 language=table['id'].split('-')[0]
 for kind,field in (('min-content','minColumns'),('max-content','maxColumns')):check(table['id']+'.'+field,next(t['columns']for t in s['intrinsics']if t['id']==language+'-'+kind),target[field])
a.output.write_text(json.dumps({'source':str(a.source),'candidate':str(a.candidate),'tables':len(s['tables']),'differences':diff,'passed':not diff},indent=2)+'\n');print(len(s['tables']),'tables;',len(diff),'differences');raise SystemExit(bool(diff))
