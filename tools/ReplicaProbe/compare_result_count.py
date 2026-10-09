from pathlib import Path
import argparse,json,re,math
p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();r=Path(__file__).resolve().parents[2];o=r/'docs/replica/source'
s=json.loads((o/f'resultcount-{a.platform}-source.json').read_text());n=json.loads((o/f'resultcount-{a.platform}-native/resultcount-native.json').read_text());errors=[]
assert s['dpr']==n['scale']==2 and len(s['rows'])==len(n['rows'])==54 and len(s['timing'])==len(n['timing'])==48
def check(id,key,x,y,tolerance=1e-7):
 if abs(x-y)>tolerance if isinstance(x,(int,float)) else x!=y:errors.append(dict(id=id,property=key,expected=x,actual=y))
def color(v):
 if v is None:return(0,0,0,0)
 if v.startswith('#'):
  v=v.lstrip('#');return tuple(int(v[i:i+2],16)for i in range(0,len(v),2))if len(v)==8 else(255,)+tuple(int(v[i:i+2],16)for i in range(0,len(v),2))
 v=list(map(float,re.findall(r'[\d.]+',v)));return(round(v[3]*255)if len(v)>3 else 255,)+tuple(map(round,v[:3]))
for x,y in zip(s['rows'],n['rows']):
 id=x['id'];check(id,'id',id,y['id'])
 for key in ['width','height','text']:check(id,key,x[key],y[key])
 for key in ['number','track','bar']:
  for dim in ['x','y','width','height']:check(id,key+'.'+dim,x[key][dim],y[key][dim])
 # DOM Range width is an enclosing CSS LayoutUnit, not the unquantized native shaped advance.
 check(id,'glyph enclosing CSS width',x['glyphWidth'],math.ceil(y['glyphWidth']*128)/128)
 for key in ['color','trackColor','barColor']:check(id,key,color(x[key]),color(y[key]))
 for key in ['fontSize','fontWeight','letterSpacing','radius']:check(id,key,float(x[key].removesuffix('px')),y[key])
 check(id,'lineHeight CSS floor',math.floor(float(x['lineHeight'].removesuffix('px'))*128)/128,y['lineHeight'])
 check(id,'overflow',x['overflow'],'hidden')
 if y['gradient']:
  g=y['gradient'];check(id,'repeat',g['spread'],'Repeat');check(id,'endpoint x',5/math.sqrt(2),g['end']['X']);check(id,'endpoint y',5/math.sqrt(2),g['end']['Y']);check(id,'stops',[0,.6,.6,1],[z['Offset']for z in g['stops']]);check(id,'stop colors',[color(z)for z in ['#f0c47c','#f0c47c','#98753e','#98753e']],[color(z['color'])for z in g['stops']]);check(id,'source gradient',x['gradient'],'repeating-linear-gradient(135deg, rgb(240, 196, 124) 0px, rgb(240, 196, 124) 3px, rgb(152, 117, 62) 3px, rgb(152, 117, 62) 5px)')
 else:check(id,'gradient',x['gradient'],'none')
for x,y in zip(s['timing'],n['timing']):
 id=f"{x['kind']}-{x['reduced']}-{x['fraction']}";check(id,'kind',x['kind'],y['kind']);check(id,'fraction',x['fraction'],y['fraction']);check(id,'reduced',x['reduced'],y['reduced']);check(id,'opacity',x['opacity'],y['opacity'],1e-6)
 # getComputedStyle matrix() serializes six significant digits (e.g. 1.17568).
 for i,(u,v)in enumerate(zip(x['matrix'],y['matrix'])):check(id,'matrix'+str(i),u,v,5e-6)
l={v['phase']:v for v in n['lifecycle']};identity=[1,0,0,1,0,0]
for phase in ['mount','completed','reduced','replaced','replaced-settled','detached','reattached','replayed']:
 for key in ['number','bar']:check(phase,key,identity,l[phase][key])
 check(phase,'opacity',1,l[phase]['opacity'])
for phase in ['completed','reduced','replayed']:check(phase,'status','RanToCompletion',l[phase]['status'])
for phase in ['replaced-settled','detached']:check(phase,'status','Canceled',l[phase]['status'])
check('started','text','11',l['started']['text']);check('started','opacity',.6,l['started']['opacity']);check('started','bar',[0,0,0,1,0,0],l['started']['bar']);check('started','number',[1,0,0,1,0,5],l['started']['number']);check('replaced','text','0',l['replaced']['text'])
assert 0<l['running']['bar'][0]<1 and .6<l['running']['opacity']<1 and l['running']['text']=='11'
report=dict(platform=a.platform,states=54,timing=48,lifecycle=10,failed=len(errors),scope='Original DOM/CSS boxes and enclosing range widths, source WAAPI sampled timeline, same native instance updates, attached public entrance completion/reduced/replacement/detach/replay. Parent axis calculation, row input and whole chart are separate.',errors=errors)
(o/f'resultcount-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items()if k not in ('scope','errors')});print(errors[:12]);assert not errors
