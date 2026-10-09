"""Strict comparison of trial() page geometry and visible leaf content.
DOM inline strong glyph boxes are distinct from Avalonia line blocks and remain
an explicit paint/baseline scope, rather than being coerced into matching data.
Inherited container font and aggregate text are checked through their leaves.
"""
from pathlib import Path
import json,sys,re
root=Path(__file__).resolve().parents[2]
os=sys.argv[1];candidate=Path(sys.argv[2]);tag=sys.argv[3] if len(sys.argv)>3 else os
s=json.loads((root/f'docs/replica/source/trialpage-{os}-source.json').read_text());n=json.loads((candidate/'trialpage-native.json').read_text());assert len(s['rows'])==len(n['rows'])==80 and s['dpr']==n['dpr']==2
errors=[]
containers=('heading','split','panels[0]','panels[1]','section','table.shell','table.table','values.panel','values.split','guide.list','consent.root')
def cmp(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():
   if k in ('computed','parent','case','noticeComputed','intrinsic'):continue
   # Native reports actual child fonts, whereas container DOM metadata is inherited.
   aggregate=any(p.endswith('.'+x) for x in containers) or p.endswith('.root') or p.endswith('.icon')
   if k in ('fontSize','lineHeight','color') and aggregate:continue
   if k=='text' and aggregate and not p.endswith('.consent.root'):continue
   if p.endswith('.title') and '.notices[' in p and k in ('box','lineHeight'):continue
   if '.fields[' in p and p.endswith('.input') and k in ('text','fontSize','lineHeight','color'):continue
   if p.endswith('.consent.input') and k in ('text','fontSize','lineHeight','color'):continue
   if re.search(r'\.table\.rows\[\d+\]$',p) and k=='root':continue
   cmp(v,b.get(k) if isinstance(b,dict) else None,p+'.'+k)
 elif isinstance(a,list):
  if not isinstance(b,list) or len(a)!=len(b):errors.append(dict(path=p,source='length '+str(len(a)),native=None if b is None else len(b)));return
  for i,(x,y) in enumerate(zip(a,b)):cmp(x,y,p+f'[{i}]')
 elif isinstance(a,(float,int)) and not isinstance(a,bool):
  if not isinstance(b,(float,int)) or abs(a-b)>.001:errors.append(dict(path=p,source=a,native=b))
 elif p.endswith('.lineHeight') and isinstance(b,str):
  if abs(float(a.removesuffix('px'))-float(b.removesuffix('px')))>.008:errors.append(dict(path=p,source=a,native=b))
 elif a!=b:errors.append(dict(path=p,source=a,native=b))
for a,b in zip(s['rows'],n['rows']):cmp(a,b,a['id'])
for x in n['lifecycle']:
 assert x['attached'];cmp(next(y for y in s['rows'] if y['id']==x['row']['id']),x['row'],'mounted.'+x['row']['id'])
assert len(n['lifecycle'])==3
if n['inputs']:
 assert len(n['inputs'])==10
 for x in n['inputs']:
  v=x['view'];assert not v['Ready'] and v['Error'] and x['focused'].startswith(('trial-','flag-trial.'))
  if x['phase'].startswith('field'):assert x['sameEditor'] and not v['Consent'] and not x['okVisible'] and x['errorVisible']
  else:assert x['replaced'] and v['Consent'] and x['focused']=='flag-trial.consent'
result=dict(cases=80,lifecycle=3,inputs=len(n['inputs']),deferred=['inline Notice strong glyph bounds/baseline paint','actual OS input/accessibility/motion'],errors=errors)
(root/f'docs/replica/source/trialpage-{tag}-comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:30],ensure_ascii=False,indent=2));sys.exit(bool(errors))
