"""Exact literal text, equivalent boxes, typography/palette and explicit metric entrance."""
from pathlib import Path
import argparse,json,re
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('os',choices=['headless','mac','windows']);p.add_argument('source',type=Path);p.add_argument('native',type=Path);p.add_argument('--natural',action='store_true');a=p.parse_args();s=json.loads(a.source.read_text());n=json.loads(a.native.read_text());errors=[]
assert len(s['rows'])==len(n['rows'])==72
if a.natural:
 differences=[]
 for x,y in zip(s['rows'],n['rows']):
  if x['id']!=y['id']:errors.append([x['id'],'identity',y['id']]);continue
  for key,dim in [('naturalWidth','width'),('naturalHeight','height')]:
   d=abs(x['root'][dim]-y[key]);differences.append(d)
   if d>.021:errors.append([x['id'],key,x['root'][dim],y[key]])
 result={'os':a.os,'phase':('HEADLESS_NATURAL_PASSED'if not errors else'HEADLESS_NATURAL_FAILED')if a.os=='headless'else('NATURAL_PASSED'if not errors else'NATURAL_FAILED'),'cases':72,'maximumDifference':max(differences),'errors':errors};(r/f'docs/replica/source/metricitem-{a.os}-natural-comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2));raise SystemExit(bool(errors))
for x,y,c in zip(s['rows'],n['rows'],s['cases']):
 if x['id']!=y['id']:errors.append([x['id'],'identity',y['id']]);continue
 for key in ['root','label','value']:
  for dim in ['x','y','width','height']:
   if abs(x[key][dim]-y[key][dim])>.021:errors.append([x['id'],key,dim,x[key][dim],y[key][dim]])
 for key in ['labelText','valueText']:
  if x[key]!=y[key]:errors.append([x['id'],key,x[key],y[key]])
 if 'naturalWidth'in y:
  for key,dim in [('naturalWidth','width'),('naturalHeight','height')]:
   if abs(x['root'][dim]-y[key])>.021:errors.append([x['id'],key,x['root'][dim],y[key]])
 elif a.os=='windows':errors.append([x['id'],'missing independently measured natural sizes'])
 source_spacing=x['valueCSS']['letterSpacing'];source_spacing=0 if source_spacing=='normal'else float(source_spacing.removesuffix('px'))
 for key,expected in {'valueSize':float(x['valueCSS']['fontSize'].removesuffix('px')),'valueWeight':int(x['valueCSS']['fontWeight']),'valueSpacing':source_spacing,'tabular':'tabular-nums'in x['valueCSS']['fontVariantNumeric'],'monospaced':'monospace'in x['valueCSS']['fontFamily']}.items():
  if y[key]!=expected:errors.append([x['id'],key,expected,y[key]])
 def color(css):return '#ff'+''.join(f'{int(v):02x}'for v in re.findall(r'\d+',css)[:3])
 if y['labelColor'].lower()!=color(x['labelCSS']['color'])or y['valueColor'].lower()!=color(x['valueCSS']['color']):errors.append([x['id'],'palette',y['labelColor'],y['valueColor']])
 if y['peer']['type']!='None'or y['peer']['name']or y['peer']['focusable']:errors.append([x['id'],'passive peer',y['peer']])
native_lifecycle=a.native.parent/'metricitem-lifecycle.json';source_lifecycle=r/('docs/replica/source/connection-metricitem-'+('mac'if a.os=='headless'else a.os)+'-input.json')
if not native_lifecycle.exists()or not source_lifecycle.exists():errors.append(['missing lifecycle evidence'])
else:
 states={x['label']:x for x in json.loads(native_lifecycle.read_text())['rows']};original=json.loads(source_lifecycle.read_text())['rows']
 if len(states)!=9 or len(original)!=7:errors.append(['lifecycle counts',len(states),len(original)])
 for x in original:
  y=states[x['label']]
  for key in ['attached','sameLabel','sameValue','focusable']:
   if x[key]!=y[key]:errors.append([x['label'],key,x[key],y[key]])
  if x['attached']:
   for key in ['opacity','y']:
    if abs(x[key]-y[key])>.0001:errors.append([x['label'],key,x[key],y[key]])
if a.os=='windows':
 peer=r/'docs/replica/source/metricitem-windows-peer.json';original=r/'docs/replica/source/metricitem-windows-source-peer.json'
 if not peer.exists()or not original.exists():errors.append(['missing actual Windows peer evidence'])
 else:
  pc=json.loads(peer.read_text());sc=json.loads(original.read_text())
  if len(pc)!=2 or pc!=sc or any(not x['control']or not x['content']for x in pc):errors.append(['actual2text peers',sc,pc])
result={'os':a.os,'phase':('HEADLESS_PASSED'if not errors else'HEADLESS_FAILED')if a.os=='headless'else('PASSED'if not errors else'FAILED'),'cases':72,'errors':errors};(r/f'docs/replica/source/metricitem-{a.os}-comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2));raise SystemExit(bool(errors))
