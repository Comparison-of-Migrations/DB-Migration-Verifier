"""Strict new Selection parent callback50 comparison, no previous geometry matrix."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);a=p.parse_args();s=json.loads(a.source.read_text());c=json.loads(a.candidate.read_text());assert s['nonce']==c['nonce'];assert len(s['records'])==len(c['records'])==2
keys=['english','screen','error','categories','tables','maps','expanded','query','stopped','announcement','focus','modal','shown','bulk','footer','width'];resizekeys=['pageSame','headingSame','searchSame','categorySame','tableCheckboxSame','mainScroll'];diffs=[];checks=0
for sc,cc in zip(s['records'],c['records']):
 assert sc['language']==cc['language'];assert len(sc['rows'])==len(cc['rows'])==25
 for sr,cr in zip(sc['rows'],cc['rows']):
  assert sr['id']==cr['id'];id=sc['language']+'-'+sr['id']
  for key in keys+(resizekeys if sr['id'].startswith('resize-')else[]):
   expected=sr[key];actual=cr[key];checks+=1
   if isinstance(expected,(float,int))and not isinstance(expected,bool):equal=isinstance(actual,(int,float))and abs(expected-actual)<.008
   else:equal=expected==actual
   if not equal:diffs.append({'id':id,'key':key,'source':expected,'candidate':actual})
a.output.write_text(json.dumps({'scope':'new connected Selection callbacks50 only; DOM source/headless handler comparison, not physical input proof','passed':not diffs,'checks':checks,'differences':diffs,'nonce':s['nonce']},ensure_ascii=False,indent=2)+'\n');print(checks,'checks',len(diffs),'differences');raise SystemExit(bool(diffs))
