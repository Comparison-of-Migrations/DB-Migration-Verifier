"""Whole original flex parent/child geometry, original paint/margins and explicit lifecycle."""
from pathlib import Path
import argparse,json
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('os',choices=['headless','mac','windows']);p.add_argument('source',type=Path);p.add_argument('native',type=Path);a=p.parse_args();s=json.loads(a.source.read_text());n=json.loads(a.native.read_text());errors=[]
def box(id,key,x,y):
 for dim in ['x','y','width','height']:
  if abs(x[dim]-y[dim])>.021:errors.append([id,key,dim,x[dim],y[dim]])
assert len(s['rows'])==len(n['rows'])==72
for x,y in zip(s['rows'],n['rows']):
 if x['id']!=y['id']:errors.append([x['id'],'identity',y['id']]);continue
 box(x['id'],'root',x['root'],y['root'])
 if y['gap']!=float(x['css']['gap'].removesuffix('px'))or y['marginBottom']!=float(x['css']['marginBottom'].removesuffix('px')):errors.append([x['id'],'gap/margin',x['css'],y['gap'],y['marginBottom']])
 if len(x['children'])!=len(y['children'])or len(y['children'])!=4:errors.append([x['id'],'4 children']);continue
 for i,(c,d) in enumerate(zip(x['children'],y['children'])):
  for k in ['root','label','value']:box(x['id'],f'{i}:{k}',c[k],d[k])
  for k in ['labelText','valueText']:
   if c[k]!=d[k]:errors.append([x['id'],i,k,c[k],d[k]])
 if y['peer']['type']!='None'or y['peer']['name']or y['peer']['focusable']:errors.append([x['id'],'passive peer',y['peer']])
states=json.loads((a.native.parent/'metrics-lifecycle.json').read_text())['rows'];original=json.loads((r/f"docs/replica/source/connection-metrics-{'mac'if a.os=='headless'else a.os}-input.json").read_text())['rows'];assert len(states)==len(original)==8
for x,y in zip(original,states):
 for k in ['label','attached','sameItems','focusable','values']:
  if x[k]!=y[k]:errors.append([x['label'],k,x[k],y[k]])
 if x['attached']:
  for dim in ['width','height']:
   if abs(x['root'][dim]-y['root'][dim])>.021:errors.append([x['label'],'root',dim,x['root'][dim],y['root'][dim]])
  for i,(c,d) in enumerate(zip(x['effects'],y['effects'])):
   for k in ['opacity','y']:
    if abs(c[k]-d[k])>.0001:errors.append([x['label'],i,k,c[k],d[k]])
if a.os=='windows':
 pc=json.loads((r/'docs/replica/source/metrics-windows-peer.json').read_text());sc=json.loads((r/'docs/replica/source/metrics-windows-source-peer.json').read_text())
 if len(pc)!=8 or pc!=sc or any(not x['control']or not x['content']for x in pc):errors.append(['actual eight Text peers',sc,pc])
result={'os':a.os,'phase':('HEADLESS_PASSED'if not errors else'HEADLESS_FAILED')if a.os=='headless'else('PASSED'if not errors else'FAILED'),'cases':72,'lifecycle':8,'errors':errors};(r/f'docs/replica/source/metrics-{a.os}-comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2));raise SystemExit(bool(errors))
