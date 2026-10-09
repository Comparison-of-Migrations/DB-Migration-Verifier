"""Final changed Windows semantic name/control-content tree against actual Edge UIA."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[2]/'docs/replica/source/capabilityheader-windows-names'
errors=[]
for label in ['ko','en','remount']:
 source=json.loads((r/('source-'+label+'.json')).read_text());native=json.loads((r/('native-'+label+'.json')).read_text())
 assert len(source)==len(native)==4
 for s,n in zip(source,native):
  for key in ['id','name','enabled','focusable','invoke']:
   if s[key]!=n[key]:errors.append({'case':label,'cell':s['id'],'property':key,'source':s[key],'native':n[key]})
  sc=[c for c in s['children'] if c['control'] or c['content']];nc=[c for c in n['children'] if c['control'] or c['content']]
  if sc!=nc:errors.append({'case':label,'cell':s['id'],'property':'controlContentChildren','source':sc,'native':nc})
headless=sorted(r.glob('headless-*.json'));assert len(headless)==1
h=json.loads(headless[0].read_text())
for state,label in zip(h['result']['states'],['ko','en','remount']):
 expected=[c['name'] for c in json.loads((r/('source-'+label+'.json')).read_text())]
 if state['names']!=expected:errors.append({'case':'headless-'+label,'property':'names','source':expected,'native':state['names']})
 if any(state.get('children',[[],[],[],[]])):errors.append({'case':'headless-'+label,'property':'children'})
report={'platform':'windows','actualStates':3,'headlessStates':3,'errors':errors,'scope':'Actual KO/EN/remount source th names with no control/content children, enabled/nonfocusable/noninvoke passive semantics. Default Edge DataItem/native HeaderItem role bridge is recorded separately; application names and children are exact.'}
(r/'comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');raise SystemExit(bool(errors))
