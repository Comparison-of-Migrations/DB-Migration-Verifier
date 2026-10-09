"""Equivalent article/line boxes and literal text; inline fragment geometry is retained separately."""
from pathlib import Path
import argparse,json,math
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('os',choices=['headless','mac','windows']);p.add_argument('source',type=Path);p.add_argument('native',type=Path);a=p.parse_args();s=json.loads(a.source.read_text());n=json.loads(a.native.read_text());errors=[]
assert len(s['rows'])==len(n['rows'])==42
for x,y in zip(s['rows'],n['rows']):
 if x['id']!=y['id']:errors.append([x['id'],'identity',y['id']]);continue
 for key in ['root','top','title','identifier','location','reason']:
  for dim in ['x','y','width','height']:
   if abs(x[key][dim]-y[key][dim])>.021:errors.append([x['id'],key,dim,x[key][dim],y[key][dim]])
 if abs(x['locationBaseline']-y['locationBaseline'])>.021:errors.append([x['id'],'location baseline',x['locationBaseline'],y['locationBaseline']])
 for key in ['titleText','identifierText','locationText','reasonText']:
  if x[key]!=y[key]:errors.append([x['id'],key,x[key],y[key]])
 if y['peer']['type']!='Group'or y['peer']['focusable']:errors.append([x['id'],'passive peer',y['peer']])
lifecycle=a.native.parent/'findingarticle-lifecycle.json'
if not lifecycle.exists():errors.append(['missing native lifecycle evidence'])
if lifecycle.exists():
 rows=json.loads(lifecycle.read_text())['rows'];
 if len(rows)!=8:errors.append(['native lifecycle count',len(rows)])
 states={x['label']:x for x in rows}
 for name in ['normal-start','reattached']:
  if abs(states[name]['opacity']-.35)>.00001:errors.append([name,'entry opacity',states[name]['opacity']])
 for name in ['normal-end','reattached-end','reduced']:
  if abs(states[name]['opacity']-1)>.00001:errors.append([name,'settled/reduced opacity',states[name]['opacity']])
 if states['detached']['attached']or not states['reattached']['attached']:errors.append(['attached lifecycle'])
 for x in rows:
  if not x['sameTitle']or not x['sameLocation']or x['focusable']or x['peer']!='Group':errors.append([x['label'],'reuse/passive'])
source_lifecycle=r/('docs/replica/source/connection-findingarticle-'+('mac'if a.os=='headless'else a.os)+'-input.json')
if not source_lifecycle.exists():errors.append(['missing source lifecycle evidence'])
if lifecycle.exists() and source_lifecycle.exists():
 source_states=json.loads(source_lifecycle.read_text())['rows']
 if len(source_states)!=6:errors.append(['source lifecycle count',len(source_states)])
 for x in source_states:
  y=states[x['label']]
  for k in ['attached','focusable','sameTitle','sameLocation']:
   if x[k]!=y[k]:errors.append([x['label'],k,x[k],y[k]])
  if x['attached']:
   for k in ['opacity','y']:
    if abs(x[k]-y[k])>.0001:errors.append([x['label'],k,x[k],y[k]])
if a.os=='windows':
 peer=r/'docs/replica/source/findingarticle-windows-peer.json';original=r/'docs/replica/source/findingarticle-windows-source-peer.json'
 if not peer.exists()or not original.exists():errors.append(['missing actual Windows peer evidence'])
 if peer.exists() and original.exists():
  pc=json.loads(peer.read_text());sc=json.loads(original.read_text());actual=pc['children'];expected=[{'name':x['name'],'type':x['type']}for x in sc]
  if pc['type']!='ControlType.Group'or pc['focusable']or actual!=expected:errors.append(['actual Windows content/control peers',expected,pc])
  parents=r/'docs/replica/source/findingarticle-windows-source-parents.json'
  if not parents.exists():errors.append(['missing source article group peer'])
  else:
   group=json.loads(parents.read_text())[0]
   if group!=pc:errors.append(['source/native article group',group,pc])
result={'os':a.os,'phase':('HEADLESS_PASSED'if not errors else'HEADLESS_FAILED')if a.os=='headless'else('PASSED'if not errors else'FAILED'),'cases':42,'errors':errors};(r/f'docs/replica/source/findingarticle-{a.os}-comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2));raise SystemExit(bool(errors))
