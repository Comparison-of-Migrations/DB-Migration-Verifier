"""Compare original DBMS label geometry and endpoint states; forced endpoints are not input evidence."""
import json,re,argparse
from pathlib import Path
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args()
s=json.loads((r/f'docs/replica/source/dbmsoption-{a.platform}-source.json').read_text());n=json.loads((r/f'docs/replica/source/dbmsoption-{a.platform}-native/dbmsoption-native.json').read_text());assert s['dpr']==n['scale']==2;assert len(s['rows'])==len(n['rows'])==len(n['updates'])==80;errors=[]
def color(s):
 if s.startswith('#'):return [int(s[i:i+2],16) for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return list(map(round,v[:3]))+[round(v[3]*255) if len(v)>3 else 255]
for phase in ['rows','updates']:
 for x,y in zip(s['rows'],n[phase]):
  def check(k,e,v,t=1e-8):
   if (abs(e-v)>t if isinstance(e,(int,float)) and not isinstance(e,bool) and isinstance(v,(int,float)) else e!=v):errors.append(dict(phase=phase,id=x['id'],property=k,expected=e,actual=v))
  check('id',x['id'],y['id'])
  for k in ['rect','body','icon','radio','title','description']:
   if x[k] is None:check(k,None,y[k]);continue
   for prop,v in x[k].items():check(k+'.'+prop,v,y[k][prop])
  for k in ['bg','border','iconColor','titleColor','descriptionColor']:check(k,color(x['style'][k]),color(y['style'][k]))
  for k in ['radius','padding','gap','titleSize','titleWeight','descriptionSize']:check(k,float(x['style'][k].removesuffix('px')),y['style'][k])
  for k in ['titleHeight','descriptionHeight']:check(k,int(float(x['style'][k].removesuffix('px'))*128)/128,y['style'][k])
  selected=x['style']['shadow']!='none';check('shadowSpread',1 if selected else 0,y['style']['shadowSpread']);check('shadowColor',color('rgb(139, 148, 255)') if selected else [0,0,0,0],color(y['style']['shadowColor']));check('outline',bool(re.search(r'\bsolid\s+3px\b',x['style']['outline'])),y['style']['outline'])
report=dict(platform=a.platform,states=80,updates=80,failed=len(errors),scope='DPR2, original full/compact geometry, KO/EN four engines, selected/hover/focus endpoints. Forced endpoint evidence only.',errors=errors)
(r/f'docs/replica/source/dbmsoption-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items() if k!='errors'});print(errors[:16]);assert not errors
