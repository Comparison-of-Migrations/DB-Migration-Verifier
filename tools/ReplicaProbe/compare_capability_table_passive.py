"""Compare only previously failing parent coordinates plus final assembled peers."""
from pathlib import Path
import argparse,json,re
p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('output',type=Path);a=p.parse_args();r=Path(__file__).resolve().parents[2]
source=r/'artifacts/replica/workers/preflight/capabilitytable/f021e0de19e4455d953d37728054df00/actual-windows/input'
errors=[]
def equal(path,s,n):
 if isinstance(s,(int,float))and not isinstance(s,bool):
  if not isinstance(n,(int,float))or abs(s-n)>.0001:errors.append({'path':path,'source':s,'candidate':n})
 elif s!=n:errors.append({'path':path,'source':s,'candidate':n})
def lookup(v,path):
 for token in re.findall(r'[A-Za-z]+|\d+',path):v=v[int(token)]if token.isdigit()else v[token]
 return v
states={label:json.loads((a.folder/('native-'+label+'.json')).read_text())for label in ['passive','en-narrow','ko-narrow','en-wide']}
old=json.loads((r/'docs/replica/source/capabilitytable-windows-parent-comparison.json').read_text())['differences']
for item in old:
 label,path=item['path'].split('.',1);s=json.loads((source/('source-'+label+'.json')).read_text());equal(item['path'],lookup(s,path),lookup(states[label],path))
before=json.loads((a.folder/'native-passive-before.json').read_text());equal('fresh.focused',True,before['buttonFocused']);equal('new.requestDelta',0,states['passive']['permissions']-before['permissions'])
for label,n in states.items():equal(label+'.nonActionCount',0,n['permissions']);equal(label+'.scrollLeft',0,n['scrollLeft'])
trace=json.loads((a.folder/'routed-input.json').read_text());equal('nativePointerPhases',['down-tunnel','down-bubble','up-bubble'],[x['phase']for x in trace])
for phase in trace:
 equal(phase['phase']+'.x',91,phase['x']);equal(phase['phase']+'.y',111,phase['y'])
 if phase['sourceType']=='WebCapabilityTable':errors.append({'path':'routedSource','source':'inside Row hitplane','candidate':phase['sourceType']})
semantics=0
for lang,sourcefile in [('ko','source-ax-initial.json'),('en','source-ax-remount.json')]:
 s=json.loads((source/sourcefile).read_text());n=json.loads((a.folder/('native-ax-'+lang+'.json')).read_text())
 equal(lang+'.table.type',s['type'],n['type']);equal(lang+'.table.name',s['name'],n['name']);equal(lang+'.innerWidth',1808,n['box']['width']);equal(lang+'.groups',2,len(n['children']))
 groups=n['children'];rows=[row for group in groups for row in group['children']];equal(lang+'.rowCount',5,len(rows));equal(lang+'.groupRows',[1,4],[len(x['children'])for x in groups])
 for i,(sr,nr)in enumerate(zip(s['children'],rows)):
  equal(f'{lang}.row{i}.type',sr['type'],nr['type']);equal(f'{lang}.row{i}.name',sr['name'],nr['name']);equal(f'{lang}.row{i}.cells',4,len(nr['children']))
  for j,(sc,nc)in enumerate(zip(sr['children'],nr['children'])):
   equal(f'{lang}.row{i}.cell{j}.name',sc['name'],nc['name']);equal(f'{lang}.row{i}.cell{j}.childCount',len(sc['children']),len(nc['children']))
   if i:equal(f'{lang}.row{i}.cell{j}.type',sc['type'],nc['type']);semantics+=1
   else:equal(f'{lang}.header{j}.protectedType','ControlType.HeaderItem',nc['type'])
   for sb,nb in zip(sc['children'],nc['children']):
    equal(lang+'.button.name',sb['name'],nb['name']);equal(lang+'.button.type',sb['type'],nb['type']);equal(lang+'.button.children',0,len(nb['children']))
 def walk(node):
  yield node
  for c in node['children']:yield from walk(c)
 for node in walk(n):
  if node['box']['width']<=0 or node['box']['height']<=0:errors.append({'path':lang+'.emptyPeerBounds','candidate':node})
equal('cleanup',{'nativeWindow':0,'nativeProcess':0},json.loads((a.folder/'cleanup.json').read_text()))
report={'affectedCoordinates':len(old),'bodyCells':semantics,'languages':2,'scope':'passive1/affected translated coordinates/assembled KOEN UIA tree','headerProviderBoundary':'Protected HeaderItem versus Chromium DataItem; exact names/nochildren retained','errors':errors,'passed':not errors}
a.output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(len(errors),'affected parent differences');raise SystemExit(bool(errors))
