"""Strict original select parent geometry, copy, shared allocation and selection policy."""
import argparse,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('source');p.add_argument('candidate');p.add_argument('--output',required=True);a=p.parse_args()
s=json.load(open(a.source));n=json.load(open(a.candidate));differences=[];checks=0

def eq(label,x,y):
 global checks
 checks+=1
 if isinstance(x,(int,float)) and not isinstance(x,bool):
  if not isinstance(y,(int,float)) or abs(x-y)>1e-6:differences.append(dict(field=label,source=x,candidate=y))
 elif x!=y:differences.append(dict(field=label,source=x,candidate=y))
def box(label,x,y):
 if 'box' in x:x=x['box']
 if 'box' in y:y=y['box']
 for k in ('x','y','width','height'):eq(label+'.'+k,x[k],y[k])

eq('DPR',s['dpr'],n['dpr']);eq('cases',len(s['rows']),len(n['rows']));nn={r['id']:r for r in n['rows']}
for sr in s['rows']:
 id=sr['id'];nr=nn.get(id)
 if nr is None:differences.append(dict(field=id,candidate='missing'));continue
 for key in ('root','heading','scope','preparation','section','sectionHeading','toolbar','searchField','bulk','meta','tableShell','table'):box(id+'.'+key,sr[key],nr[key])
 for key in ('title','description','sectionTitle','selectionCount','bulkLabel','searchCount','searchHint','caption'):
  box(id+'.'+key,sr[key],nr[key]);eq(id+'.'+key+'.text',sr[key]['text'],nr[key]['text'])
 for key in ('search','clearSearch','bulkCheckbox','deselect'):
  box(id+'.'+key,sr[key],nr[key])
  for prop in (('value','placeholder') if key=='search' else ('checked','mixed','disabled') if key=='bulkCheckbox' else ('text','disabled')):eq(id+'.'+key+'.'+prop,sr[key][prop],nr[key][prop])
 eq(id+'.selection',sr['selection'],nr['selection'])
 for i,(x,y) in enumerate(zip(sr['categories'],nr['categories'])):
  for key in ('root','head','toggle','body','checkbox'):box(f'{id}.category{i}.{key}',x[key],y[key])
  eq(f'{id}.category{i}.expanded',x['expanded']=='true',y['expanded']);eq(f'{id}.category{i}.selected',x['checkbox']['checked'],y['checkbox']['checked'])
 for i,(x,y) in enumerate(zip(sr['headers'],nr['headers'])):box(f'{id}.header{i}',x,y);eq(f'{id}.header{i}.text',x['text'],y['text'])
 sourceRows=[r for r in sr['rows'] if r['checkbox'] is not None];eq(id+'.nonemptyrows',len(sourceRows),len(nr['rows']))
 for i,(x,y) in enumerate(zip(sourceRows,nr['rows'])):
  box(f'{id}.row{i}',x['root'],y['root'])
  for j,(cx,cy) in enumerate(zip(x['cells'],y['cells'])):box(f'{id}.row{i}.cell{j}',cx,cy)
  for key in ('checkbox','change'):box(f'{id}.row{i}.{key}',x[key],y[key])
  eq(f'{id}.row{i}.selected',x['checkbox']['checked'],y['checkbox']['checked']);eq(f'{id}.row{i}.change',x['change']['text'],y['change']['text'])
 for key in ('hint','back','forward'):
  sb=sr['footer'][key]['box'];fr=sr['footer']['root']['box'];local={**sb,'x':sb['x']-fr['x'],'y':sb['y']-fr['y']};box(id+'.footer.'+key,local,nr['footer'][key]);eq(id+'.footer.'+key+'.text',sr['footer'][key]['text'],nr['footer'][key]['text'])
 eq(id+'.footer.enabled',not sr['footer']['forward']['disabled'],nr['footer']['enabled'])
 for i in range(4):
  eq(f'{id}.intrinsic.max{i}',sr['intrinsic']['max-content']['columns'][i],nr['intrinsic']['max'][i]);eq(f'{id}.intrinsic.min{i}',sr['intrinsic']['min-content']['columns'][i],nr['intrinsic']['min'][i])
result=dict(status='PASSED' if not differences else 'DIFFERENCES',cases=len(s['rows']),checks=checks,differences=differences,source=str(Path(a.source).resolve()),candidate=str(Path(a.candidate).resolve()))
Path(a.output).write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(dict(cases=len(s['rows']),checks=checks,differences=len(differences))));raise SystemExit(bool(differences))
