from pathlib import Path
import json,re,argparse
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();s=json.loads((r/f'docs/replica/source/sidebaritem-{a.platform}-source-timing.json').read_text());n=json.loads((r/f'docs/replica/source/sidebaritem-{a.platform}-timing.json').read_text());assert not s['reduced'];assert len(s['rows'])==len(n['rows'])==50;err=[]
def rgba(s):
 if s=='White':return [255]*4
 if s.startswith('#'):return [int(s[i:i+2],16) for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return v[:3]+[v[3]*255 if len(v)>3 else 255]
for x,y in zip(s['rows'],n['rows']):
 def check(k,e,v,tol=1e-6):
  if (abs(e-v)>tol if isinstance(e,(int,float)) else e!=v):err.append(dict(scenario=x['scenario'],ms=x['ms'],property=k,expected=e,actual=v))
 for k in ['scenario','ms']:check(k,x[k],y[k])
 for k in ['bg','color','badgeColor']:
  for channel,(e,v) in enumerate(zip(rgba(x[k]),rgba(y[k]))):check(k+str(channel),e,v,1 if channel==3 else .51)
 sh=x['shadow'];present=sh!='none';native=rgba(y['shadowColor']);v=list(map(float,re.findall(r'[\d.]+',sh))) if present else []
 if present:
  # Computed rgba precedes inset geometry; RGB strings have three channels.
  cm=re.search(r'rgba?\([^)]*\)',sh);c=rgba(cm.group(0));geom=list(map(float,re.findall(r'(-?[\d.]+)px',sh)))
  for i,(e,b) in enumerate(zip(c,native)):check('shadowColor'+str(i),e,b,1 if i==3 else .51)
  check('shadowX',geom[0],y['shadowX'],1e-5)
 else:check('shadowAlpha',0,native[3]);check('shadowX',0,y['shadowX'],1e-5)
 transform=x['transform'];scale=1 if transform=='none' else float(re.findall(r'-?[\d.]+',transform)[0]);check('scale',scale,y['scale'],1e-6)
 if x['reverseDuration'] is not None:check('reverseDuration',x['reverseDuration'],y['reverseDuration'],1e-6)
report=dict(platform=a.platform,samples=50,failed=len(err),scope='Actual source CSS transitions and private candidate clocks: hover bg/inset-shadow, inherited disabled color, pressed scale, interrupted reversing-shortening; CSS serialized channel rounding is explicit',errors=err)
(r/f'docs/replica/source/sidebaritem-{a.platform}-timing-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items() if k!='errors'});print(err[:12]);assert not err
