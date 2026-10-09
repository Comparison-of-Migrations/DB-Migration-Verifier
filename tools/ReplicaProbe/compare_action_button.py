"""Compare production candidate diagnostics against actual original-browser fixture outputs."""
import json,re
from pathlib import Path
root=Path(__file__).resolve().parents[2]/'docs/replica/source'
def rgba(value):
 if value=='White':return [255,255,255,255]
 if value=='Transparent':return [0,0,0,0]
 if value.startswith('#'):result=[int(value[i:i+2],16) for i in (3,5,7)]+[int(value[1:3],16)]
 else:
  values=[float(x) for x in re.findall(r'[\d.]+',value)];result=values[:3]+[round(values[3]*255) if len(values)>3 else 255]
 return result if result[-1] else [0,0,0,0]
source=json.loads((root/'windows-source-action-button-states.json').read_text())
native=json.loads((root/'action-button-native-states.json').read_text())
lookup={(row['id'],row['mode']):row for row in native['rows']};errors=[]
for a in source['result']:
 b=lookup[a['id'],a['mode']]
 for key in ['bg','color','border']:
  if rgba(a[key])!=rgba(b[key]):errors.append([a['id'],a['mode'],key,a[key],b[key]])
 scale=float(re.search(r'matrix\(([^,]+)',a['transform'])[1]) if a['transform']!='none' else 1
 if abs(scale-b['scale'])>1e-12:errors.append([a['id'],a['mode'],'scale',scale,b['scale']])
 if (a['shadow']=='none')!=(len(b['shadow'])==0):errors.append([a['id'],a['mode'],'shadow-count'])
 if a['shadow']!='none' and b['shadow']:
  color,geometry=a['shadow'].split(') ',1);lengths=[float(v) for v in re.findall(r'(-?[\d.]+)px',geometry)]
  shadow=b['shadow'][0]
  if rgba(color+')')!=rgba(shadow['color']) or lengths[:3]!=[shadow['x'],shadow['y'],shadow['blur']]:errors.append([a['id'],a['mode'],'shadow-values',a['shadow'],b['shadow']])
 if b['focus']!=(a['mode']=='focus') or (b['focus'] and ('solid 3px' not in a['outline'] or a['offset']!='3px')):errors.append([a['id'],a['mode'],'focus'])
timing=json.loads((root/'action-button-native-timing.json').read_text());time_errors=[]
for kind in ['samples','reversed']:
 for a,b in zip(source['timing'][kind],timing[kind]):
  if a['time']!=b['time'] or rgba(a['color'])!=rgba(b['color']):time_errors.append([kind,a,b])
duration_delta=timing['reversedDuration']-source['timing']['reversedDuration']
result={'count':len(source['result']),'errors':errors,'timingCount':sum(len(timing[key]) for key in ['samples','reversed']),'timingErrors':time_errors,'reversedDurationDeltaMs':duration_delta,'mode':'Source-derived pseudo-state fixture; actual-input proofs separate'}
(root/'action-button-state-comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
print(json.dumps(result,ensure_ascii=False,indent=2))
assert not errors and not time_errors and abs(duration_delta)<1e-9

track_errors=[];duration_deltas=[];sample_count=0
for a,b in zip(source.get('tracks',[]),timing.get('tracks',[])):
 assert a['name']==b['name']
 for key,value in a['durations'].items():
  delta=b['durations'][key]-value;duration_deltas.append(abs(delta))
  if abs(delta)>1e-9:track_errors.append([a['name'],key,'duration',delta])
 for phase in ['samples','reversed']:
  for x,y in zip(a[phase],b[phase]):
   sample_count+=1
   for key in ['bg','color']:
    if rgba(x[key])!=rgba(y[key]):track_errors.append([a['name'],phase,x['time'],key,x[key],y[key]])
   scale=float(re.search(r'matrix\(([^,]+)',x['transform'])[1]) if x['transform']!='none' else 1
   if abs(scale-y['scale'])>1e-6:track_errors.append([a['name'],phase,x['time'],'scale',scale,y['scale']])
   source_shadow=x['shadow']
   if source_shadow!='none' and not y['shadow']:
    c,g=source_shadow.split(') ',1)
    if rgba(c+')')[-1]==0 and all(float(v)==0 for v in re.findall(r'(-?[\d.]+)px',g)):source_shadow='none'
   if (source_shadow=='none')!=(len(y['shadow'])==0):track_errors.append([a['name'],phase,x['time'],'shadow-count',x['shadow'],y['shadow']])
   elif source_shadow!='none':
    c,g=x['shadow'].split(') ',1);lengths=[float(v) for v in re.findall(r'(-?[\d.]+)px',g)];z=y['shadow'][0]
    if rgba(c+')')!=rgba(z['color']) or max(abs(i-j) for i,j in zip(lengths[:3],[z['x'],z['y'],z['blur']]))>1e-4:track_errors.append([a['name'],phase,x['time'],'shadow',x['shadow'],z])
track_result={'sampleCount':sample_count,'tracks':['background-color','color','box-shadow','transform'],'geometryTolerancePx':0.0001,'scaleTolerance':0.000001,'colorComparison':'Equal painted RGBA bytes; source CSS serialization has finite precision','shadowNormalization':'none equals an invisible zero-length shadow at transition time0','maxReverseDurationDeltaMs':max(duration_deltas,default=0),'errors':track_errors}
(root/'action-button-track-comparison.json').write_text(json.dumps(track_result,ensure_ascii=False,indent=2));print(json.dumps(track_result,ensure_ascii=False,indent=2));assert sample_count==39 and not track_errors
