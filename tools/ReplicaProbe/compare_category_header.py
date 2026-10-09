"""Compare actual original DOM with same-instance native header states."""
from pathlib import Path
import argparse,json,re,math
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();o=r/'docs/replica/source';s=json.loads((o/f'categoryheader-{a.platform}-source.json').read_text());n=json.loads((o/f'categoryheader-{a.platform}-native/categoryheader-native.json').read_text());assert s['dpr']==n['scale']==2;assert len(s['rows'])==len(n['rows'])==192;errors=[]
def color(s):
 if s.startswith('#'):return [int(s[i:i+2],16)for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return list(map(round,v[:3]))+[round(v[3]*255)if len(v)>3 else 255]
for x,y in zip(s['rows'],n['rows']):
 def check(key,expected,actual):
  if(abs(expected-actual)>1e-7 if isinstance(expected,(int,float))and not isinstance(expected,bool)else expected!=actual):errors.append(dict(id=x['id'],property=key,expected=expected,actual=actual))
 for key in ['id','name','selectionName','selected','expanded']:check(key,x[key],y[key])
 for key in ['rect','button','choice','checkbox','icon','svg','title','description','pill','chevron']:
  for field,value in x[key].items():check(key+'.'+field,value,y[key][field])
 for key in ['minHeight','gap','iconRadius','titleSize','titleWeight','titleHeight','descriptionSize','descriptionHeight']:check('style.'+key,float(x['style'][key].removesuffix('px')),y['style'][key])
 for key in ['bg','iconBg','titleColor','descriptionColor']:check('style.'+key,color(x['style'][key]),color(y['style'][key]))
 check('chevronAngle',90 if x['expanded']else 0,y['style']['chevronAngle'])
report=dict(platform=a.platform,states=192,failed=len(errors),scope='DPR2, original KO/EN five categories plus prepared trial; widths300/610/858/1058, selected×expanded, same native instance replacements and transformed AABB. Width300 is a wrapping stress case, not mobile support.',errors=errors);(o/f'categoryheader-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items()if k!='errors'});print(errors[:20]);assert not errors
