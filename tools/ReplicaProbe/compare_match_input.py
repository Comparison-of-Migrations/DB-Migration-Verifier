"""Actual card requests and attached layout; source CSS states were independently measured."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[2]/'docs/replica/source';cases=json.loads((r/'matchoption-cases.json').read_text());checks=[]
for platform,states in [('mac',[('body-selected','archive.orders'),('disabled-body','archive.orders'),('keyboard-left','demo.customers'),('reduced-body-selected','archive.orders')]),('windows',[('body-selected','archive.orders'),('disabled-body','archive.orders'),('cancelled-press','archive.orders'),('keyboard-right','archive.orders'),('reduced-keyboard-left','demo.customers')])]:
 source=json.loads((r/f'matchoption-{platform}-source.json').read_text())['rows']
 for label,expected in states:
  n=json.loads((r/f'matchoption-{platform}-input/{label}.json').read_text());assert n['dpr']==2;assert [c['nameText']for c in n['rows']if c['selected']]==[expected]
  for row in n['rows']:
   index=next(i for i,c in enumerate(cases)if c['lang']=='ko'and c['width']==442 and c['name']==row['nameText']and c['selected']==row['selected']and not c['hover']and not c['focus']);s=source[index]
   # Actual Record may sample an in-flight hover fade. Independently sampled timing covers those colors.
   def same(a,b):
    if isinstance(a,dict):
     for k,v in a.items():same(v,b[k])
    elif isinstance(a,(int,float))and not isinstance(a,bool):assert abs(a-b)<.001,(platform,label,a,b)
    else:assert a==b,(platform,label,a,b)
   for k in ['width','height','radio','body','name','note','badge','nameText','noteText','badgeText','selected','disabled']:same(s[k],row[k])
  assert all(e['action']=='select'and e['name']!='demo.orders'for e in n['events']);checks.append(platform+'-'+label)
 for label in ['disabled-body']+(['cancelled-press','keyboard-right']if platform=='windows'else[]):
  baseline=json.loads((r/f'matchoption-{platform}-input/body-selected.json').read_text())['events'];current=json.loads((r/f'matchoption-{platform}-input/{label}.json').read_text())['events'];assert baseline==current
(r/'matchoption-input-comparison.json').write_text(json.dumps({'checks':checks,'errors':[],'scope':'Actual body click, disabled rejection, Windows cancelled press; keyboard group and reduced motion. Dialog-level focus belongs to MatchingDialog.'},ensure_ascii=False,indent=2)+'\n');print(len(checks),'actual card comparisons passed')
