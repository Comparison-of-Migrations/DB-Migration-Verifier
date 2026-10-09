"""New Table parent requests/language/width/remount; child mechanics retain their LOCK evidence."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('output',type=Path);a=p.parse_args();diff=[];count=0
labels=['initial','pointer','key','passive','en-narrow','ko-narrow','en-wide','detach','remount']
def check(path,x,y):
 if isinstance(x,dict):
  for k,v in x.items():check(path+'.'+k,v,y.get(k)if isinstance(y,dict)else None)
 elif isinstance(x,list):
  if not isinstance(y,list)or len(x)!=len(y):diff.append({'path':path,'source':x,'candidate':y});return
  for i,(l,r)in enumerate(zip(x,y)):check(path+f'[{i}]',l,r)
 elif isinstance(x,(int,float)):
  if y is None or abs(x-y)>.0001:diff.append({'path':path,'source':x,'candidate':y})
 elif x!=y:diff.append({'path':path,'source':x,'candidate':y})
for label in labels:
 s=json.loads((a.folder/('source-'+label+'.json')).read_text());n=json.loads((a.folder/('native-'+label+'.json')).read_text());count+=1
 for key in ['id','english','attached','scrollLeft']:check(label+'.'+key,s[key],n[key])
 if s['attached']:
  for key in ['shell','table','headers','rows','columns','clientWidth','clientHeight','scrollWidth','scrollHeight','gutter','permissionsButton']:check(label+'.'+key,s[key],n[key])
  # Focus semantics of component actions, rather than diagnostic utilities.
  if label in ['pointer','key','passive']:check(label+'.buttonFocused',s['buttonFocused'],n['buttonFocused'])
# Original key actor may require a fresh fixture epoch after guarded pre-Enter failure.
# Validate exact request deltas against explicit before proof, retaining absolute counts in evidence.
for platform in ['source','native']:
 initial=json.loads((a.folder/(platform+'-initial.json')).read_text());check(platform+'.initial.expectedRequests',0,initial['permissions'])
 previous=initial
 for label in labels[1:]:
  data=json.loads((a.folder/(platform+'-'+label+'.json')).read_text())
  proof=a.folder/(platform+'-'+label+'-before.json')
  before=json.loads(proof.read_text())if proof.exists()else previous
  expected=1 if label in ['pointer','key']else 0
  check(platform+'.'+label+'.requestDelta',expected,data['permissions']-before['permissions'])
  previous=data
a.output.write_text(json.dumps({'states':count,'differences':diff,'passed':not diff},indent=2)+'\n');print(count,'parent states;',len(diff),'differences');raise SystemExit(bool(diff))
