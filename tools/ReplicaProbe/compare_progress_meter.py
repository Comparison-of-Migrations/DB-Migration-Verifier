"""Compare literal meter geometry/state and actual CSS timing; no product mutation."""
from pathlib import Path
import argparse,json
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('candidate',type=Path);p.add_argument('--os',choices=['mac','windows'],default='mac');a=p.parse_args()
source=json.loads((r/f'docs/replica/source/root-progressmeter-{a.os}-source.json').read_text());native=json.loads((a.candidate/'progressmeter-native.json').read_text());diff=[]
assert source['dpr']==native['dpr']==2 and len(source['rows'])==len(native['rows'])==30
for x,y in zip(source['rows'],native['rows']):
 for k in ['id','width','height','background','barBackground','radius','overflow','origin','label','minimum','maximum','value','focusable']:
  if x[k]!=y[k]:diff.append([x['id'],k,x[k],y[k]])
 # Source DOMRect of a CSS float transform rounds through float32. No layout tolerance is applied to dimensions.
 if abs(x['barWidth']-y['barWidth'])>0.000008:diff.append([x['id'],'float32 transform extent',x['barWidth'],y['barWidth']])
timing=json.loads((r/f'docs/replica/source/root-progressmeter-{a.os}-input.json').read_text())['rows'];candidate=json.loads((a.candidate/'progressmeter-timing.json').read_text());assert len(timing)==len(candidate)==18
for x,y in zip(timing,candidate):
 for k in ['label','time','duration']:
  if x[k]!=y[k]:diff.append([x['label'],k,x[k],y[k]])
 if abs(x['fraction']-y['fraction'])>1e-6:diff.append([x['label'],'CSS serialized matrix fraction',x['fraction'],y['fraction']])
life=json.loads((r/f'docs/replica/source/root-progressmeter-{a.os}-control-ready.json').read_text())['lifecycle'];candidate=json.loads((a.candidate/'progressmeter-lifecycle.json').read_text());assert len(life)==len(candidate)==7
for x,y in zip(life,candidate):
 for k in ['label','processed','total','name','attached','focusable']:
  if x[k]!=y[k]:diff.append([x['label'],k,x[k],y[k]])
 if x['attached']:
  if x['width']!=y['width']:diff.append([x['label'],'width',x['width'],y['width']])
  if x['label']=='normal-start':
   if not(0<=y['fraction']<1):diff.append([x['label'],'actual still transitioning',y['fraction']])
  elif x['fraction']!=y['fraction']:diff.append([x['label'],'fraction',x['fraction'],y['fraction']])
report={'os':a.os,'endpointCases':30,'timingCases':18,'lifecycleCases':7,'differences':diff,'boundary':'CSS float32 transform DOMRect/serialized matrix only; no layout, color, count, name or timing tolerance.'};(a.candidate/'compare.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report));assert not diff
