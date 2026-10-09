"""Compare actual original CSS transitions with production tracks and independent native natural height."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];assert os in ['mac','windows'];s=json.loads((r/f'docs/replica/source/categorysection-{os}-motion-source.json').read_text());n=json.loads((r/f'docs/replica/source/categorysection-{os}-native/categorysection-motion-native.json').read_text());assert len(s['rows'])==len(n['rows'])==72;errors=[]
for a,b in zip(s['rows'],n['rows']):
 for k,v in a.items():
  if isinstance(v,(int,float)):
   if abs(v-b[k])>.001:errors.append(dict(scenario=a['scenario'],ms=a['ms'],property=k,source=v,native=b[k]))
  elif v!=b[k]:errors.append(dict(scenario=a['scenario'],ms=a['ms'],property=k,source=v,native=b[k]))
for a,b in zip(s['durations'],n['durations']):
 fields=[('grid-template-rows','open'),('visibility','visibility')]if a['scenario'].startswith('expand')or a['scenario']=='collapse'else[('border-top-color','selected')]
 for prop,key in fields:
  duration=next(x['duration']for x in a['animations']if x['property']==prop)
  if abs(duration-b[key])>.001:errors.append(dict(scenario=a['scenario'],property=prop,source=duration,native=b[key]))
p=r/f'docs/replica/source/categorysection-{os}-motion-comparison.json';p.write_text(json.dumps(dict(platform=os,samples=72,errors=errors),indent=2)+'\n');print(os,'72 timing samples',len(errors),'differences');sys.exit(bool(errors))
