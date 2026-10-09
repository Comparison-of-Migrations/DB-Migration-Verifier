"""Nonce/hash/count guard for existing posted Selection callback oracle; no browser input."""
from pathlib import Path
import argparse,json,hashlib
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('plan',type=Path);p.add_argument('--os',choices=['mac','windows'],default='mac');a=p.parse_args();plan=json.loads(a.plan.read_text());source=r/f'docs/replica/source/preflight-selectioncallbacks-{a.os}-source.json';v=json.loads(source.read_text());assert v['nonce']==plan['nonce']and v['originalHashes']==plan['originalHashes'];assert len(v['records'])==2 and {x['language']for x in v['records']}=={'ko','en'}
expected=['initial','category-data','expand-data','collapse-data','query-orders','table-orders','filtered-bulk','filtered-deselect','query-clear','mapping-open','mapping-draft','mapping-cancel','mapping-applied','query-target','mapping-removes-opener','modal-before-language','modal-language-reopened','modal-language-cancel','resize-ready','resize-960','resize-1180','resize-1280','invalid-connect','error-back-home','error-start-connect']
for c in v['records']:
 assert [x['id']for x in c['rows']]==expected
 for x in c['rows']:
  if x['id'].startswith('resize-'):assert all(x[key]for key in ['pageSame','headingSame','searchSame','categorySame','tableCheckboxSame'])
 assert c['rows'][-1]['error']==c['rows'][-2]['error']==''and c['rows'][-3]['error']
o=a.plan.parent;target=o/f'original-{a.os}.json';target.write_bytes(source.read_bytes());(o/f'original-{a.os}-record.json').write_text(json.dumps({'scope':'DOM handler oracle only; no physical OS input claim','nonce':plan['nonce'],'source':str(source),'immutable':str(target),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'contexts':2,'states':50,'dpr':v['dpr']},indent=2)+'\n');print('Fresh callbacks50 preserved; close completed source tab immediately')
