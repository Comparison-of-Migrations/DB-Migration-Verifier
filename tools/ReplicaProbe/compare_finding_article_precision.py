"""Narrow Article LOCK exception: affected mono line allocation, no repeated lifecycle/input."""
from pathlib import Path
import json,sys
s=json.loads(Path(sys.argv[1]).read_text());n=json.loads(Path(sys.argv[2]).read_text());sm={x['id']:x for x in s['rows']};errors=[];deltas=[]
if len(s['rows'])!=42 or len(n['rows'])!=42 or set(sm)!=set(x['id']for x in n['rows']):errors.append({'path':'coverage'})
for b in n['rows']:
 a=sm[b['id']]
 for box,field in [('root','height'),('identifier','height'),('identifier','y'),('location','height'),('reason','y')]:
  delta=b[box][field]-a[box][field];deltas.append(abs(delta))
  if abs(delta)>.001:errors.append({'id':b['id'],'path':box+'.'+field,'source':a[box][field],'native':b[box][field]})
 if abs(b['locationBaseline']-a['locationBaseline'])>.001:errors.append({'id':b['id'],'path':'baseline','source':a['locationBaseline'],'native':b['locationBaseline']})
r={'status':'FAILED'if errors else'PASSED','sourceCases':len(s['rows']),'nativeCases':len(n['rows']),'maxAffectedDelta':max(deltas,default=0),'errors':errors,'scope':'Actual affected mono12 CSSfloor allocation only; prior unaffected geometry/lifecycle/input/peer/paint evidence reused'};Path(sys.argv[3]).write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'status':r['status'],'errors':len(errors),'maxAffectedDelta':r['maxAffectedDelta'],'first':errors[:6]},ensure_ascii=False));raise SystemExit(bool(errors))
