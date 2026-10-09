"""Reuse the identical original tbody transition timeline without rerunning protected evidence."""
from pathlib import Path
import argparse, hashlib, json, re

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('platform', choices=['mac', 'windows'])
p.add_argument('candidate', type=Path)
a = p.parse_args()
source = root / 'docs/replica/source' / f'capabilityrow-{a.platform}-source-timing.json'
s = json.loads(source.read_text())
n = json.loads((a.candidate / 'progresspage-timing.json').read_text())
assert len(s['rows']) == len(n['rows']) == 48
errors = []
def color(value):
    if value.startswith('#'):
        return [int(value[i:i+2],16) for i in (3,5,7,1)]
    values = list(map(float,re.findall(r'[\d.]+',value)))
    return list(map(round,values[:3])) + [round(values[3]*255) if len(values)>3 else 255]
for x,y in zip(s['rows'],n['rows']):
    assert x['scenario'] == y['scenario'] and x['ms'] == y['ms']
    difference = max(abs(i-j) for i,j in zip(color(x['value']),color(y['value'])))
    if difference > 1:
        errors.append({'scenario':x['scenario'],'ms':x['ms'],'difference':difference})
    if x.get('reverseDuration') is not None and abs(x['reverseDuration']-y['reverseDuration']) > .0001:
        errors.append({'scenario':x['scenario'],'ms':x['ms'],'property':'reverseDuration'})
report = {'platform':a.platform,'samples':48,'failed':len(errors),'source':str(source),
    'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),
    'scope':'Same original tbody tr CSS background-color160ms ease/#3a3c44 with reversal; Progress own production track. Protected source evidence reused, not new actual Progress input.',
    'errors':errors}
(a.candidate / 'progresspage-timing-comparison.json').write_text(json.dumps(report,indent=2)+'\n')
print(report)
assert not errors
