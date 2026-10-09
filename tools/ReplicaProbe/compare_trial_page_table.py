"""New table clip/peer correction scope only; prior page/input passes retained."""
import json,sys
from pathlib import Path
r=Path(__file__).resolve().parents[2];os=sys.argv[1];out=Path(sys.argv[2]);tag=sys.argv[3];s=json.loads((r/f'docs/replica/source/trialpage-{os}-source.json').read_text());n=json.loads((out/'trialpage-table-native.json').read_text());assert n['dpr']==s['dpr']==2 and len(n['rows'])==2
errors=[]
def walk(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():
   if k in ('computed','parent','fontSize','lineHeight','color','root') or (k=='text' and p in ('table.table','table.shell')):continue
   walk(v,b.get(k) if isinstance(b,dict) else None,p+'.'+k)
 elif isinstance(a,list):
  if not isinstance(b,list) or len(a)!=len(b):errors.append({'path':p,'source':len(a),'native':b});return
  for i,(x,y) in enumerate(zip(a,b)):walk(x,y,p+f'[{i}]')
 elif isinstance(a,(float,int)):
  if not isinstance(b,(float,int)) or abs(a-b)>.001:errors.append({'path':p,'source':a,'native':b})
 elif a!=b:errors.append({'path':p,'source':a,'native':b})
for row in n['rows']:walk(next(x for x in s['rows'] if x['id']==row['id'])['table'],row['table'],'table')
assert len(n['motion'])==2
for m in n['motion']:assert m['mounted'] and m['disposed'] and m['reattached'] and m['settled']==m['reattachOpacity']==1
result={'tableCases':2,'isolationMotionLifecycle':2,'errors':errors};(r/f'docs/replica/source/trialpage-{tag}-table-comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(result);sys.exit(bool(errors))
