from pathlib import Path
import json,re,argparse
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();o=r/'docs/replica/source';s=json.loads((o/f'capabilityrow-{a.platform}-source-timing.json').read_text());n=json.loads((o/f'capabilityrow-{a.platform}-native/capabilityrow-timing.json').read_text());assert len(s['rows'])==len(n['rows'])==48;errors=[]
def color(s):
 if s.startswith('#'):return [int(s[i:i+2],16)for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return list(map(round,v[:3]))+[round(v[3]*255)if len(v)>3 else 255]
for x,y in zip(s['rows'],n['rows']):
 assert x['scenario']==y['scenario']and x['ms']==y['ms']
 expected=x['value'];actual=y['value'];diff=max(abs(a-b)for a,b in zip(color(expected),color(actual)))if isinstance(expected,str)else abs(expected-actual)
 tolerance=1 if isinstance(expected,str)else (1e-4 if x["scenario"].startswith("expand") else 1e-6)
 if diff>tolerance:errors.append(dict(scenario=x['scenario'],ms=x['ms'],expected=expected,actual=actual,diff=diff))
 if x.get('reverseDuration')is not None and abs(x['reverseDuration']-y['reverseDuration'])>.0001:errors.append(dict(scenario=x['scenario'],property='reverseDuration',expected=x['reverseDuration'],actual=y['reverseDuration']))
report=dict(platform=a.platform,samples=48,failed=len(errors),scope='Actual row CSS transition timeline seek vs production background track, 4 scenarios including reversal; channel rounding tolerance1/255, cubic solver reversal duration tolerance0.0001ms. Actual click/reduced lifecycle verified separately.',errors=errors);(o/f'capabilityrow-{a.platform}-timing-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items()if k!='errors'});print(errors[:20]);assert not errors
