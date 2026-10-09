from pathlib import Path
import json,math,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];candidate=Path(sys.argv[2]);tag=sys.argv[3] if len(sys.argv)>3 else os
s=json.loads((r/f'docs/replica/source/trialpage-{os}-baseline.json').read_text());n=json.loads((candidate/'trialpage-baseline-native.json').read_text());assert len(s['rows'])==len(n['rows'])==16 and s['dpr']==n['dpr']==2
errors=[]
for a,b in zip(s['rows'],n['rows']):
 assert a['id']==b['id'] and len(a['notices'])==len(b['notices'])==3
 for x,y in zip(a['notices'],b['notices']):
  if x['title']!=y['title'] or abs(x['baseline']-y['baseline'])>.001:errors.append(dict(id=a['id'],source=x,native=y))
  assert len(x['titleWidths'])==len(y['titleWidths'])
  for u,v in zip(x['titleWidths'],y['titleWidths']):
   if abs(u-math.ceil(v*128)/128)>.001:errors.append(dict(id=a['id'],sourceWidth=u,nativeWidth=v))
(r/f'docs/replica/source/trialpage-{tag}-baseline-comparison.json').write_text(json.dumps(dict(cases=16,titles=48,errors=errors),ensure_ascii=False,indent=2)+'\n');print(len(errors),'baseline differences');print(errors[:3]);sys.exit(bool(errors))
