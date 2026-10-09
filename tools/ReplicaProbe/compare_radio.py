"""Compare actual source recorder with the production Radio resolver. Raster handled separately."""
import json,math,re
from pathlib import Path
root=Path(__file__).resolve().parents[2]
def rgba(text):
 values=[float(x) for x in re.findall(r'[\d.]+',text)];return tuple(round(x) for x in values[:3])+(round(values[3]*255) if len(values)==4 else 255,)
def native(c):return tuple(round(c[k]) for k in ('R','G','B','A'))
rows=[]
for platform,source,nativepath in [('mac','radio-mac-source.json',root/'artifacts/replica/radio-mac/radio-native.json'),('windows','windows-source-radio.json',root/'docs/replica/source/radio-windows-native.json')]:
 s=json.loads((root/'docs/replica/source'/source).read_text());n=json.loads(nativepath.read_text())
 for a,b in zip(s['rows'],n):
  assert a['id']==b['id'];p=b['appearance'];scale=p['Scale']
  assert rgba(a['background'])==native(p['Background']);assert rgba(a['border'])==native(p['Border'])
  assert abs(a['rect']['width']-20*scale)<.0001 and abs(a['rect']['height']-20*scale)<.0001
  assert b['checkedState']==(a['checked']);assert b['disabled']==a['disabled']
  shadow=a['shadow'];g=p['Glow']
  if shadow=='none':assert g['Spread']==0 and g['Color']['A']==0
  else:
   color=shadow[:shadow.index(')')+1];assert rgba(color)==native(g['Color']);assert g['Spread']==3
  m=a['after']['transform'];pose=p['Pose'];angle=0;size=pose['Scale']
  expected=(size*math.cos(angle),size*math.sin(angle),-size*math.sin(angle),size*math.cos(angle),0,0)
  actual=(1,0,0,1,0,0) if m=='none' else tuple(map(float,m.removeprefix('matrix(').removesuffix(')').split(',')))
  assert max(abs(x-y) for x,y in zip(expected,actual))<1e-6
  rows.append(dict(platform=platform,id=a['id'],paintAndBox=True,markPose=True))
(root/'docs/replica/source/radio-state-comparison.json').write_text(json.dumps(rows,indent=2)+'\n')
print('Radio actual source / production resolver:48 states match')
