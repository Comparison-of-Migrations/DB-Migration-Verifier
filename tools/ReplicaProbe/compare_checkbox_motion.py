import json,math,re,sys
from pathlib import Path
r=Path(__file__).resolve().parents[2]
platform=sys.argv[1] if len(sys.argv)>1 else 'mac'
s=json.loads((r/f'docs/replica/source/checkbox-{platform}-source-motion.json').read_text())['output'];n=json.loads((r/f'docs/replica/source/checkbox-{platform}-native-motion.json').read_text())
def rgb(s):return tuple(round(float(v)) for v in re.findall(r'[\d.]+',s)[:3])
def matrix(s):return (1,0,0,1,0,0) if s=='none' else tuple(map(float,s[7:-1].split(',')))
rows=[];fails=[]
for a,b in zip(s,n):
 assert a['name']==b['name']
 for d in a['durations']:
  property=d['property'];key='Background' if property=='background-color' else 'Border' if property.startswith('border-') else 'Glow' if property=='box-shadow' else 'Pose' if d['pseudo']=='::after' else 'Scale'
  if abs(d['duration']-b['durations'][key])>1e-5:fails.append(dict(name=a['name'],duration=d,native=b['durations'][key]))
 for phase in ['samples','reversed']:
  for x,y in zip(a[phase],b[phase]):
   p=y['appearance'];assert x['time']==y['time'];error=[]
   for src,key in [('bg','Background'),('border','Border')]:
    c=p[key]['Color'];v=tuple(c[k] for k in ['R','G','B'])
    if rgb(x[src])!=v:error.append((src,x[src],v))
   scale=matrix(x['scale']);size=p['Scale']
   if max(abs(z-w) for z,w in zip(scale,(size,0,0,size,0,0)))>1e-6:error.append(('scale',scale,size))
   mark=matrix(x['mark']);size=p['Pose']['Scale'];angle=p['Pose']['Angle']*math.pi/180
   expected=(size*math.cos(angle),size*math.sin(angle),-size*math.sin(angle),size*math.cos(angle),0,0)
   if max(abs(z-w) for z,w in zip(mark,expected))>1e-6:error.append(('mark',mark,expected))
   shadow=x['shadow'];g=p['Glow']
   if shadow=='none':
    if g['Spread']!=0 and g['Color']['A']!=0:error.append(('shadow-none',g))
   else:
    color=shadow[:shadow.index(')')+1];numbers=list(map(float,re.findall(r'[\d.]+',color)));ng=g['Color']
    if tuple(round(v) for v in numbers[:3])!=tuple(round(ng[k]) for k in ('R','G','B')) or round((numbers[3] if len(numbers)>3 else 1)*255)!=ng['Color']['A']:error.append(('glow-color',color,g))
    spread=float(shadow[shadow.index(')')+1:].strip().split()[-1].removesuffix('px'))
    if abs(spread-g['Spread'])>1e-5:error.append(('spread',spread,g['Spread']))
   rows.append(dict(name=a['name'],phase=phase,time=x['time'],equal=not error))
   if error:fails.append(dict(name=a['name'],phase=phase,time=x['time'],errors=error))
(r/f'docs/replica/source/checkbox-{platform}-motion-comparison.json').write_text(json.dumps(dict(rows=rows,failures=fails),indent=2)+'\n')
print('Timing rows',len(rows),'failures',len(fails));print(json.dumps(fails[:5],indent=2));assert not fails
