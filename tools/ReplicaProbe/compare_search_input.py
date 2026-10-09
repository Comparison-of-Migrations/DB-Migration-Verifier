"""Search-field composition geometry, labels and immediate value/clear state."""
import json,re,argparse
from pathlib import Path
root=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();s=json.loads((root/f'docs/replica/source/searchinput-{a.platform}-source.json').read_text());n=json.loads((root/f'docs/replica/source/searchinput-{a.platform}-native/searchinput-native.json').read_text());assert s['dpr']==n['scale']==2;assert len(s['rows'])==len(n['rows'])==48;errors=[]
margins={x['id']:x['margin'] for x in json.loads((root/'docs/replica/source/searchinput-source-margins.json').read_text())}
def color(s):
 if s.startswith('#'):return [int(s[i:i+2],16) for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return list(map(round,v[:3]))+[round(v[3]*255) if len(v)>3 else 255]
for x,y in zip(s['rows'],n['rows']):
 def check(k,expected,actual,tol=0):
  same=abs(expected-actual)<=tol if isinstance(expected,(float,int)) and not isinstance(expected,bool) else expected==actual
  if not same:errors.append(dict(id=x['id'],property=k,expected=expected,actual=actual))
 for k in ['id','width','height','value','placeholder','label','clearName','clearDisabled']:check(k,x.get(k),y[k])
 for side in ['left','top','right','bottom']:check('margin.'+side,float(margins[x['id']][side].removesuffix('px')),y['margin'][side])
 for k in ['input','icon','clear']:
  if x[k] is None:check(k,None,y[k]);continue
  for prop in ['x','y','width','height']:check(k+'.'+prop,x[k][prop],y[k][prop],1e-8)
 for k in ['bg','color','border']:check(k,color(x['style'][k]),color(y['style'][k]))
 for k in ['fontSize','lineHeight','radius']:check(k,float(x['style'][k].removesuffix('px')),y['style'][k],1e-8)
 check('padding','40,9,44,9',y['style']['padding'])
assert len(n['updates'])==7
for u in n['updates']:
 if u['disabled']!=(not u['value']):errors.append(dict(property='update-clear',actual=u))
report=dict(scope='SearchInput composed geometry across two contexts, KO/EN, widths160/240/380/452 and empty/value/space; locked editor/button/SVG behavior reused',platform=a.platform,layouts=48,updates=7,failed=len(errors),errors=errors)
(root/f'docs/replica/source/searchinput-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='errors'},ensure_ascii=False,indent=2));print(errors[:8]);assert not errors
