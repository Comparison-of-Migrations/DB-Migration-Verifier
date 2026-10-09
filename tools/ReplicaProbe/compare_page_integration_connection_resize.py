"""Compare only newly observed resize identity/caret/entry scope; never waive a mismatch."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
s=json.loads(a.source.read_text());c=json.loads(a.candidate.read_text());sr={r['id']:r for r in s['rows']};cr={r['id']:r for r in c['rows']}
fields=['language','width','actualWidth','pageRetained','editorRetained','fieldRetained','pickerRetained','focused','value','selectionStart','selectionEnd','newPageAnimations'];differences=[]
if set(sr)!=set(cr):differences.append({'field':'caseIds','source':sorted(sr),'candidate':sorted(cr)})
for key in sorted(set(sr)&set(cr)):
 for field in fields+([f for f in ['toggleRetained','explanationRetained','columnRetained'] if f in sr[key]]):
  if sr[key].get(field)!=cr[key].get(field):differences.append({'id':key,'field':field,'source':sr[key].get(field),'candidate':cr[key].get(field)})
geometry_checks=0
def geometry_check(expected,observed,identity):
 def geometry(left,right,path):
  global geometry_checks
  if isinstance(left,dict):
   for key,value in left.items():geometry(value,right.get(key) if isinstance(right,dict) else None,path+'.'+key)
  elif isinstance(left,list):
   if not isinstance(right,list) or len(left)!=len(right):differences.append({'id':identity,'field':path,'source':len(left),'candidate':None if not isinstance(right,list) else len(right)});return
   for i,(l,r) in enumerate(zip(left,right)):geometry(l,r,path+'['+str(i)+']')
  else:
   geometry_checks+=1
   if not isinstance(right,(int,float)) or abs(left-right)>.001:differences.append({'id':identity,'field':path,'source':left,'candidate':right})
 geometry(expected,observed,'geometry')
for key in sorted(set(sr)&set(cr)):
 if 'geometry' in sr[key]:geometry_check(sr[key]['geometry'],cr[key].get('geometry'),key)
actual=c.get('phase')=='ACTUAL_OS_RECORDED'
result={'phase':('ACTUAL_OS_DIFFERENCES' if differences else 'ACTUAL_OS_PASSED') if actual else ('HEADLESS_DIFFERENCES' if differences else 'HEADLESS_PASSED'),'scope':'New connected Connection KOEN resize identity/caret/entry8 only; source handler oracle; '+('candidate actual native external window resize' if actual else 'candidate isolated Skia; actualOS unverified'),'source':str(a.source.resolve()),'candidate':str(a.candidate.resolve()),'cases':len(sr),'geometryChecks':geometry_checks,'geometrySource':'Fresh connected source geometry per row','geometryReuse':'Fresh connected source geometry per row when included; optional legacy Connection80 oracle only where same viewport/client/gutter/intrinsicheight condition is explicitly proven.','differences':differences,'candidateAdditionalPublicFields':{r['id']:{k:r.get(k) for k in ['fieldRetained','caretIndex','immediate']} for r in c['rows']},'oldScopes':'Existing Connection geometry/input/state/AX/paint evidence reused; not rerun by this comparison'}
a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'phase':result['phase'],'cases':len(sr),'differences':len(differences)}))
