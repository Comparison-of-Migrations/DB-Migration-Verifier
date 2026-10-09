from pathlib import Path
import argparse,json,re
p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();r=Path(__file__).resolve().parents[2]/'docs/replica/source';s=json.loads((r/f'tablemappingrow-{a.platform}-source.json').read_text());n=json.loads((r/f'tablemappingrow-{a.platform}-native/tablemappingrow-native.json').read_text());cases=json.loads((r/'tablemappingrow-cases.json').read_text());assert len(s['rows'])==len(n['rows'])==48;errors=[]
def color(v):
 if v.startswith('#'):
  v=v.lstrip('#');return tuple(int(v[i:i+2],16)for i in range(0,len(v),2))if len(v)==8 else(255,)+tuple(int(v[i:i+2],16)for i in range(0,len(v),2))
 v=list(map(float,re.findall(r'[\d.]+',v)));return (round(v[3]*255)if len(v)>3 else 255,)+tuple(map(round,v[:3]))
for x,y,c in zip(s['rows'],n['rows'],cases):
 assert x['id']==y['id']==c['id']
 for prop in ['width','height']:
  if abs(x[prop]-y[prop])>1e-7:errors.append(dict(id=x['id'],property=prop,expected=x[prop],actual=y[prop]))
 for prop in ['choice','checkbox','source','target','method','methodIcon','methodText','button','buttonText','buttonIcon']:
  for key in ['x','y','width','height']:
   if abs(x[prop][key]-y['targetBox'if prop=='target'else prop][key])>1e-7:errors.append(dict(id=x['id'],property=prop+'.'+key,expected=x[prop][key],actual=y['targetBox'if prop=='target'else prop][key]))
 for prop in ['background','methodColor']:
  if color(x['style'][prop])!=color(y['style'][prop]):errors.append(dict(id=x['id'],property=prop,expected=x['style'][prop],actual=y['style'][prop]))
 assert c['selected']==y['selected'] and c['manual']==y['manual'] and c['source']==y['selectionName'] and c['button']==y['buttonName']
report=dict(platform=a.platform,states=48,failed=len(errors),scope='Atomic row uses actual original table parent column allocation. Internal boxes/name/states/selection/manual/last-border and same-instance replacement compared; parent automatic column layout remains separate.',errors=errors);(r/f'tablemappingrow-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items()if k!='errors'});print(errors[:18]);assert not errors
