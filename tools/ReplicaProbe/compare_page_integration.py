import argparse,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);p.add_argument('--actual',action='store_true');a=p.parse_args()
source=json.loads(a.source.read_text());candidate=json.loads(a.candidate.read_text());actual={r.get('id',r.get('Id')):r for r in candidate['rows']}
differences=[]
for expected in source['rows']:
    row=actual.get(expected['id'])
    if row is None: differences.append({'id':expected['id'],'field':'missing'});continue
    for component in ['viewport','topbar','sidebar','strip','content','page','footer']:
        for dimension,value in expected[component].items():
            if value!=row[component].get(dimension):differences.append({'id':expected['id'],'field':component+'.'+dimension,'source':value,'candidate':row[component].get(dimension)})
result={'phase':('ACTUAL_OS_PASSED' if a.actual else 'HEADLESS_PASSED') if not differences else 'FAILED','scope':'Connected-page shell placement only; complete application flow remains unverified','source':str(a.source.resolve()),'candidate':str(a.candidate.resolve()),'cases':len(source['rows']),'differences':differences}
a.output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'phase':result['phase'],'cases':result['cases'],'differenceCount':len(differences),'first':differences[:10]}));raise SystemExit(bool(differences))
