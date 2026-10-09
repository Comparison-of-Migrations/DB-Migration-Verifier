"""Only new full parent summary contracts; renderer leaf paint proof is explicitly reused."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
s=json.loads(a.source.read_text());c=json.loads(a.candidate.read_text());sr={r['id']:r for r in s['rows']};cr={r['id']:r for r in c['rows']};diff=[];checks=0
def check(identity,field,expected,actual):
 global checks
 checks+=1
 if expected!=actual:diff.append({'id':identity,'field':field,'source':expected,'candidate':actual})
def geometry(identity,field,expected,actual):
 global checks
 if isinstance(expected,dict):
  for k,v in expected.items():geometry(identity,field+'.'+k,v,actual.get(k) if isinstance(actual,dict) else None)
 else:
  checks+=1
  if not isinstance(actual,(int,float)) or abs(expected-actual)>.001:diff.append({'id':identity,'field':field,'source':expected,'candidate':actual})
check('cases','ids',sorted(sr),sorted(cr))
for identity in sorted(set(sr)&set(cr)):
 expected,actual=sr[identity],cr[identity]
 for field in ['mode','language','width','actualWidth','height','run']:check(identity,field,expected[field],actual.get(field))
 geometry(identity,'summary',expected['summary'],actual.get('summary'))
 check(identity,'items.length',len(expected['items']),len(actual['items']))
 for index,(left,right) in enumerate(zip(expected['items'],actual['items'])):
  for field in ['label','value','strong','mono','labelLines','valueLines','labelFont','valueFont']:check(identity,f'items[{index}].'+field,left[field],right.get(field))
  if left['mono']:
   # The literal suffix is part of exact whole value. Public muted clips establish
   # parent requested inline grouping; protected InlineText literal/paint proof is reused.
   check(identity,f'items[{index}].suffixPainted',bool(left['suffix']),right.get('suffixPainted'))
  else:check(identity,f'items[{index}].logicalLines',left['logicalLines'],right.get('logicalLines'))
  geometry(identity,f'items[{index}].boxes',left['boxes'],right.get('boxes'))
actual_os=c.get('phase')=='ACTUAL_OS_RECORDED'
result={'phase':('ACTUAL_OS_PASSED' if actual_os else 'HEADLESS_PASSED') if not diff else ('ACTUAL_OS_DIFFERENCES' if actual_os else 'HEADLESS_DIFFERENCES'),'scope':f'Only static Review/Save summary content/order/strong/mono/logical lines/actual wrapped lines/relative placement for {len(sr)} source cases; subset when below30, not a full30 claim','source':str(a.source.resolve()),'candidate':str(a.candidate.resolve()),'cases':len(sr),'checks':checks,'differences':diff,'reuse':'Protected SummaryItem/SummaryList/InlineText typography/paint/clips unchanged; root Mac InlineText namedText peer correction has separate affected4 actual proof; literal suffix compared as full combined value plus positive public muted clips. Exact source HTML tag vs native control class is not asserted. Existing first-row parent48 passes retained; full items placement is newly assessed. ActualOS semantic public names/AX and paint inspection remain separate.'}
a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'phase':result['phase'],'cases':len(sr),'checks':checks,'differences':len(diff)}))
