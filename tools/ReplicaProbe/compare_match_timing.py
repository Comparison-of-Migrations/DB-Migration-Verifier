from pathlib import Path
import json,re,argparse
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();s=json.loads((r/f'docs/replica/source/matchoption-{a.platform}-source-timing.json').read_text());n=json.loads((r/f'docs/replica/source/matchoption-{a.platform}-timing.json').read_text());assert not s['reduced'];assert len(s['rows'])==len(n['rows'])==50;err=[]
def rgba(s):
 if s.startswith('#'):return [int(s[i:i+2],16) for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return v[:3]+[v[3]*255 if len(v)>3 else 255]
for x,y in zip(s['rows'],n['rows']):
 def check(k,e,v,tol=1e-6):
  if (abs(e-v)>tol if isinstance(e,(int,float)) else e!=v):err.append(dict(scenario=x['scenario'],ms=x['ms'],property=k,expected=e,actual=v))
 for k in ['scenario','ms']:check(k,x[k],y[k])
 for k in ['bg','border']:
  for i,(e,v) in enumerate(zip(rgba(x[k]),rgba(y[k]))):check(k+str(i),e,v,.51)
 sh=x['shadow'];native=rgba(y['shadowColor']);
 if sh!='none':
  cm=re.search(r'rgba?\([^)]*\)',sh);c=rgba(cm.group(0));geom=list(map(float,re.findall(r'(-?[\d.]+)px',sh)))
  for i,(e,v) in enumerate(zip(c,native)):check('shadowColor'+str(i),e,v,1 if i==3 else .51)
  check('shadowSpread',geom[3],y['shadowSpread'],1e-5)
 else:check('shadowAlpha',0,native[3]);check('shadowSpread',0,y['shadowSpread'],1e-5)
 if x['reverseDuration'] is not None:check('reverseDuration',x['reverseDuration'],y['reverseDuration'],1e-5)
report=dict(platform=a.platform,samples=50,failed=len(err),scope='Actual original CSS160ms ease and production private injected clock; background/border/inset selection/reversing shortening. Serialized channels tolerance explicit; cubic solver reversal duration tolerance0.00001ms.',errors=err)
(r/f'docs/replica/source/matchoption-{a.platform}-timing-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items() if k!='errors'});print(err[:12]);assert not err
