"""Endpoint layout/paint check, not an actual input or transition proof."""
from pathlib import Path
import json,re,argparse,math
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();s=json.loads((r/f'docs/replica/source/sidebaritem-{a.platform}-source.json').read_text());n=json.loads((r/f'docs/replica/source/sidebaritem-{a.platform}-native/sidebaritem-native.json').read_text());assert s['dpr']==n['scale']==2;cs=json.loads((r/'docs/replica/source/sidebaritem-cases.json').read_text());assert len(s['rows'])==len(n['rows'])==len(cs)==132;err=[]
def color(s):
 if s=='White':return [255]*4
 if s=='Transparent':return [0]*4
 if s.startswith('#'):return [int(s[i:i+2],16) for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return list(map(round,v[:3]))+[round(v[3]*255) if len(v)>3 else 255]
assert len(n['updates'])==132
for row,(c,x,y) in enumerate(zip(cs+cs,s['rows']+s['rows'],n['rows']+n['updates'])):
 row%=132
 def check(k,e,v,t=1e-8):
  if (abs(e-v)>t if isinstance(e,(int,float)) and not isinstance(e,bool) else e!=v):err.append(dict(id=x['id'],property=k,expected=e,actual=v))
 scale=.98 if c['interaction']=='pressed' and not c['disabled'] else 1
 # Transformed DOMRects use float32 viewport coordinates; subtracting two
 # large fixture Y coordinates then dividing by .98 has a bounded ULP loss.
 rectTol=2**(math.floor(math.log2(30+row*110+100))-23)*2/scale if scale!=1 else 1e-8
 check('id',x['id'],y['id']);check('scale',scale,y['style']['scale'])
 for k in ['width','height']:check(k,x[k]/scale,y[k],rectTol)
 for k in ['badge','label']:
  for prop,v in x[k].items():check(k+'.'+prop,v/scale,y[k][prop],max(rectTol,1/64) if k=='label' and prop=='width' else rectTol)
 for k in ['bg','color','border']:check(k,color(x['style'][k]),color(y['style'][k]))
 for k in ['radius','fontSize','fontWeight']:check(k,float(x['style'][k].removesuffix('px')),y['style'][k])
 check('padding','14,12,14,12',y['style']['padding'])
 shadow=x['style']['shadow'];has=shadow!='none';check('shadowX',3 if has else 0,y['style']['shadowX']);check('shadowColor',[139,148,255,255] if has else [0,0,0,0],color(y['style']['shadowColor']));check('shadowInset',has,y['style']['shadowInset'])
 check('itemStatus','step' if c['state']=='active' else '',y['itemStatus'])
 check('name',c['label'] if c['state']=='done' else f'{c["index"]} {c["label"]}',y['name'])
report=dict(platform=a.platform,endpoints=132,updates=132,failed=len(err),scope='SidebarItem endpoint layouts/paint; actual interaction/timing and full Sidebar not covered',errors=err)
(r/f'docs/replica/source/sidebaritem-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items() if k!='errors'});print(err[:12]);assert not err
