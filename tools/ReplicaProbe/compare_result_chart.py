from pathlib import Path
import json,argparse
p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();r=Path(__file__).resolve().parents[2]/'docs/replica/source';s=json.loads((r/f'resultchart-{a.platform}-source.json').read_text());n=json.loads((r/f'resultchart-{a.platform}-native/resultchart-native.json').read_text());cases=json.loads((r/'resultchart-cases.json').read_text());assert s['dpr']==n['scale']==2 and len(s['rows'])==len(n['rows'])==len(cases);errors=[]
def check(id,key,x,y):
 if (abs(x-y)>1e-7 if isinstance(x,(int,float)) else x!=y):errors.append(dict(id=id,property=key,expected=x,actual=y))
def box(id,key,x,y):
 for d in ['x','y','width','height']:check(id,key+'.'+d,x[d],y[d])
for x,y,c in zip(s['rows'],n['rows'],cases):
 id=x['id'];check(id,'id',id,y['id']);check(id,'present',x['present'],y['present'])
 if not x['present']:continue
 for key in ['width','height','unitText','headingText','helpText','captionText']:check(id,key,x[key],y[key])
 for key in ['header','heading','help','unit','columns','captionBox']:box(id,key,x[key],y[key])
 check(id,'labelcount',len(x['labels']),len(y['labels']))
 for i,(u,v)in enumerate(zip(x['labels'],y['labels'])):box(id,'label'+str(i),u,v)
 check(id,'rowcount',len(x['rows']),len(y['rows']))
 for i,(u,v)in enumerate(zip(x['rows'],y['rows'])):
  box(id,'row'+str(i),u['box'],v['box'])
  for key in ['name','numbers']:check(id,'row'+str(i)+'.'+key,u[key],v[key])
 expected=int(x['unitText'].split('–')[-1]);check(id,'maximum',expected,y['maximum'])
report=dict(platform=a.platform,states=len(cases),failed=len(errors),scope='38 original resultChart layouts, KO/EN 4 widths compact/wide, interrupted/unstarted/subsets, empty/trial-only, zero/5-step shared scale; child input/timing inherited locked contracts, parent entrance/detail still pending.',errors=errors);(r/f'resultchart-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items()if k not in ['scope','errors']});print(errors[:25]);assert not errors
