"""Icon-only composition contract, source snapshots versus actual attached layout."""
import argparse,json,re
from pathlib import Path
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args()
s=json.loads((root/f'docs/replica/source/iconbutton-{a.platform}-source.json').read_text());n=json.loads((root/f'docs/replica/source/iconbutton-{a.platform}-native/iconbutton-native.json').read_text());assert s['dpr']==n['scale']==2
cases={c['id']:c for c in json.loads((root/'docs/replica/source/iconbutton-cases.json').read_text())};errors=[]
def color(s):
 if s.startswith('#'):return [int(s[i:i+2],16) for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return list(map(round,v[:3]))+[round(v[3]*255) if len(v)>3 else 255]
assert len(s['rows'])==len(n['rows'])==44
for x,y in zip(s['rows'],n['rows']):
 def check(key,expected,actual,tol=0):
  same=abs(expected-actual)<=tol if isinstance(expected,(int,float)) and not isinstance(expected,bool) else expected==actual
  if not same:errors.append(dict(id=x['id'],mode=x['mode'],property=key,expected=expected,actual=actual))
 for k in ['id','mode','label','width','height','disabled']:check(k,x[k],y[k])
 for k in ['bg','color','border']:check(k,color(x[k]),color(y[k]))
 for k in ['radius','borderWidth','fontSize']:check(k,float(x[k].removesuffix('px')),y[k])
 check('padding','0,0,0,0',y['padding']);check('text','',y['text']);check('reduced',cases[x['id']]['reduced'],y['reduced'])
 check('source.children',1,x['children']);scale=1 if x['transform']=='none' else float(x['transform'].split('(')[1].split(',')[0]);check('scale',scale,y['scale'])
 for k in ['x','y','width','height']:check('icon.'+k,x['icon'][k]/scale,y['icon'][k],1/64)
 check('focus',x['mode']=='focus',y['focus'])
 if y['focus']:assert x['outline']=='rgb(167, 177, 255) solid 3px' and x['offset']=='3px'
 if x['shadow']=='none':check('shadow',[],y['shadow'])
 else:
  check('shadow.count',1,len(y['shadow']));t=y['shadow'][0];check('shadow.color',color(x['shadow'].split(')')[0]+')'),color(t['color']))
  for k,v in [('x',0),('y',2),('blur',5)]:check('shadow.'+k,v,t[k])
assert len(n['changes'])==12
for c in n['changes']:
 if c['expected']!=c['actual']:errors.append(dict(property='name-update',**c))
report=dict(scope='Icon-only source .btn.quiet.icon-button endpoints, explicit parent fonts and name updates; locked action/motion/SVG contracts reused',platform=a.platform,states=44,nameUpdates=12,failed=len(errors),errors=errors)
(root/f'docs/replica/source/iconbutton-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='errors'},ensure_ascii=False,indent=2));print(errors[:4]);assert not errors
