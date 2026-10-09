from pathlib import Path
import json,argparse,re
p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();r=Path(__file__).resolve().parents[2]/'docs/replica/source';s=json.loads((r/f'resultrow-{a.platform}-source.json').read_text());n=json.loads((r/f'resultrow-{a.platform}-native/resultrow-native.json').read_text());cases=json.loads((r/'resultrow-cases.json').read_text());assert s['dpr']==n['scale']==2 and len(s['rows'])==len(n['rows'])==len(cases);errors=[]
def check(id,key,x,y):
 if abs(x-y)>1e-7 if isinstance(x,(int,float))else x!=y:errors.append(dict(id=id,property=key,expected=x,actual=y))
def color(v):
 if v.startswith('#'):
  v=v.lstrip('#');return tuple(int(v[i:i+2],16)for i in range(0,len(v),2))if len(v)==8 else(255,)+tuple(int(v[i:i+2],16)for i in range(0,len(v),2))
 v=list(map(float,re.findall(r'[\d.]+',v)));return(round(v[3]*255)if len(v)>3 else 255,)+tuple(map(round,v[:3]))
for x,y,c in zip(s['rows'],n['rows'],cases):
 id=x['id'];check(id,'id',id,y['id'])
 for key in ['width','height','name']:check(id,key,x[key],y[key])
 for key in ['category','icon','iconSvg','title','description','chevronSvg']:
  for d in ['x','y','width','height']:check(id,key+'.'+d,x[key][d],y[key][d])
 for i,(u,v)in enumerate(zip(x['children'],y['children'])):
  for d in ['x','y','width','height']:check(id,'children'+str(i)+'.'+d,u[d],v[d])
 check(id,'background',color(x['style']['background']),color(y['style']['background']));check(id,'borderBottom',float(x['style']['borderBottom'].removesuffix('px')),y['style']['borderBottom']);check(id,'focus',c['focus'],y['style']['focus'])
report=dict(platform=a.platform,states=len(cases),failed=len(errors),scope='KO/EN ×610/858/978/1058×four original categories×rest/hover/focus wide/compact; interrupted/unstarted/disabled boundaries and same-instance replacements, source individual grid fr allocation and baseline middle-chevron. Timing/actual input are pending, not LOCK approval.',errors=errors);(r/f'resultrow-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items()if k not in ['scope','errors']});print(errors[:15]);assert not errors
