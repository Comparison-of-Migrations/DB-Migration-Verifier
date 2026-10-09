from pathlib import Path
import json,re,argparse
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();s=json.loads((r/f'docs/replica/source/field-{a.platform}-source.json').read_text());n=json.loads((r/f'docs/replica/source/field-{a.platform}-native/field-native.json').read_text());assert s['dpr']==n['scale']==2;assert len(s['rows'])==len(n['rows'])==len(n['updates'])==44;errors=[]
def color(s):
 if s.startswith('#'):return [int(s[i:i+2],16) for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return list(map(round,v[:3]))+[round(v[3]*255) if len(v)>3 else 255]
for phase in ['rows','updates']:
 for x,y in zip(s['rows'],n[phase]):
  def check(k,e,v):
   if (abs(e-v)>1e-8 if isinstance(e,(int,float)) and not isinstance(e,bool) else e!=v):errors.append(dict(phase=phase,id=x['id'],property=k,expected=e,actual=v))
  for k in ['id','disabled','name']:check(k,x[k],y[k])
  for k in ['rect','label','input','hint']:
   if x[k] is None:check(k,None,y[k]);continue
   for key,v in x[k].items():check(k+'.'+key,v,y[k][key])
  for k in ['marginTop','labelSize','labelWeight']:check(k,float(x[k].removesuffix('px')),y[k])
  check('labelColor',color(x['labelColor']),color(y['labelColor']));check('hintColor',None if x['hintColor'] is None else color(x['hintColor']),None if y['hintColor'] is None else color(y['hintColor']));check('labelHeight',int(float(x['labelHeight'].removesuffix('px'))*128)/128,y['labelHeight'])
report=dict(platform=a.platform,states=44,updates=44,failed=len(errors),scope='Original Field label/input/help geometry at DPR2, KO/EN three widths, disabled empty example password, port; same Field label/help/input-slot replacements.',errors=errors);(r/f'docs/replica/source/field-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items() if k!='errors'});print(errors[:16]);assert not errors
