"""Primary source cell metadata comparison; shared Button descendant scope stays explicit."""
from pathlib import Path
import argparse,json
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('directory',type=Path);p.add_argument('--os',choices=['mac','windows'],required=True);p.add_argument('--headless',action='store_true');p.add_argument('--initial-only',action='store_true');a=p.parse_args();src=json.loads((r/'docs/replica/source/capabilityrow-table-semantic-source.json').read_text());diff=[];count=0;button_descendant_differences=[]

def walk(n):
 yield n
 for c in n.get('children',[]):yield from walk(c)

def check(actual,expected,where):
 global count
 count+=1
 if a.os=='windows':
  for k,want in [('name',expected['name']),('type','DataItem' if a.headless else 'ControlType.DataItem')]:
   if actual.get(k)!=want:diff.append({'where':where,'field':k,'source':want,'candidate':actual.get(k)})
  children=actual.get('children',[]);e=expected['children']
  if len(children)!=len(e):diff.append({'where':where,'field':'children','source':len(e),'candidate':len(children)})
  if len(children)==1:
   for k,want in [('name',e[0]['name']),('type','Button' if a.headless else 'ControlType.Button')]:
    if children[0].get(k)!=want:diff.append({'where':where+'/button','field':k,'source':want,'candidate':children[0].get(k)})
   if not a.headless and len(children)==1 and children[0].get('children',[]):button_descendant_differences.append({'where':where+'/button','sourceChildren':0,'candidateChildren':len(children[0]['children'])})
 else:
  if a.headless:
   if actual.get('name') not in [None,'']or actual.get('type')!='Custom':diff.append({'where':where,'field':'macContract','candidate':actual})
  else:
   if actual.get('role')!='AXUnknown' or actual.get('title') or actual.get('description'):diff.append({'where':where,'field':'macUnnamedCell','candidate':actual})
  if len(actual.get('children',[]))!=(2 if where.endswith('/0')else 1):diff.append({'where':where,'field':'macChildren','candidate':len(actual.get('children',[]))})
if a.headless:
 data=json.loads((a.directory/'capabilityrow-semantic-native.json').read_text())
 for record in data['records']:
  expected=next(x for x in src['rows']if x['id']==record['id'])
  for i,(c,e)in enumerate(zip(record['peer']['children'],expected['cells'])):check(c,e,record['scope']+'/'+expected['id']+'/'+str(i))
 if not data['detached']:diff.append({'where':'detached','candidate':False})
else:
 for label,english in ([('ko',False),('en',True)] if a.initial_only else [('ko',False),('en',True),('remount',True)]):
  data=json.loads((a.directory/('native-'+label+'.json')).read_text())
  for expected in [x for x in src['rows']if x['english']==english]:
   found=[n for n in walk(data)if n.get('id')=='CapabilityRow-semantic-'+expected['key']]
   if len(found)!=1:diff.append({'where':label+'/'+expected['key'],'field':'uniqueRow','candidate':len(found)});continue
   cells=found[0].get('children',[])
   if len(cells)!=4:diff.append({'where':label+'/'+expected['key'],'field':'fourCells','candidate':len(cells)});continue
   for i,(c,e)in enumerate(zip(cells,expected['cells'])):
    check(c,e,label+'/'+expected['id']+'/'+str(i));b=c.get('box',{})
    if b.get('width',0)<=0 or b.get('height',0)<=0:diff.append({'where':label+'/'+expected['id']+'/'+str(i),'field':'physicalBox','candidate':b})
 detached=json.loads((a.directory/'native-detached.json').read_text());remaining=[x for x in walk(detached)if x.get('id','').startswith('CapabilityRow-cell-')or x.get('id')=='CapabilityRow-permissions']
 if remaining:diff.append({'where':'detached','field':'remainingCells','candidate':len(remaining)})
 cleanup=json.loads((a.directory/'cleanup.json').read_text())
 if any(cleanup.values()):diff.append({'where':'cleanup','candidate':cleanup})
result={'os':a.os,'headless':a.headless,'cellsChecked':count,'differences':diff,'passedOwnedRowScope':not diff,'buttonDescendantsPendingRoot':bool(button_descendant_differences),'buttonDescendantDifferences':button_descendant_differences,'scope':'Cell metadata/direct Button and refresh/remount only; no layout/input replay','evidence':str(a.directory.resolve())};(a.directory/'comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'cells':count,'differences':len(diff),'passed':not diff}));raise SystemExit(bool(diff))
