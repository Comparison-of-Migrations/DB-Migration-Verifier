"""Compare only source trusted filter focus states with real rendered native/headless frames."""
from pathlib import Path
import argparse,json,math
from PIL import Image
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);p.add_argument('--headless',action='store_true');a=p.parse_args();source=json.loads(a.source.read_text())['rows'];checks=[]
def check(name,ok,actual=None,expected=None):checks.append({'name':name,'pass':bool(ok),'actual':actual,'expected':expected})
pointer=[s for s in source if s['label']=='actual-filter-pointer'];keyboard=[s for s in source if s['label']=='actual-filter-keyboard'];held=[s for s in source if s['label']=='actual-filter-held']
assert len(pointer)==2 and len(keyboard)==1 and len(held)>=1,'Exact new trusted pointer/Space/samefocused source scope required'
expect=[pointer[0],keyboard[0],held[-1],pointer[-1]]
check('source samefocused held active',held[-1]['active'],held[-1]['active'],True)
if a.headless:
 data=json.loads((a.candidate/'resultspage-focus-headless.json').read_text());rows=data['states'];assert len(rows)==4
 frames=[a.candidate/('resultspage-focus-'+s['label']+'.png')for s in rows]
else:
 names=['input-filter-unknown-click.json','input-current-space.json','resultspage-focus-held-2.json','input-filter-unknown-hold.json'];rows=[]
 for name in names:
  d=json.loads((a.candidate/name).read_text());rows.append(d['history'][-1]|{'requests':d['requests']}if 'history'in d else d)
 frames=[a.candidate/'resultspage-focus-callback-1.png',a.candidate/'resultspage-focus-callback-2.png',a.candidate/'resultspage-focus-held-2.png',a.candidate/'resultspage-focus-callback-3.png']
for i,(s,n,frame)in enumerate(zip(expect,rows,frames)):
 label=['pointer','Space','samefocused held','samefocused release'][i];point=n.get('buttonBox',n.get('filterWindowPoint'));size=n.get('buttonSize',n.get('filterSize'));x,y=point['X'],point['Y'];w,h=size['Width'],size['Height'];im=Image.open(frame).convert('RGB');assert im.size==(2400,1800)
 outside=im.crop((math.floor((x-6)*2),math.floor((y-6)*2),math.ceil((x+w+6)*2),math.ceil((y+h+6)*2)))
 count=0
 for py in range(outside.height):
  for px in range(outside.width):
   wx=(math.floor((x-6)*2)+px)/2;wy=(math.floor((y-6)*2)+py)/2
   if x<=wx<=x+w and y<=wy<=y+h:continue
   if outside.getpixel((px,py))==(167,177,255):count+=1
 check(label+' source focused filter',s['focusAction']=='filter:unknown',s['focusAction'],'filter:unknown')
 check(label+' original filtered content',s['articleIds']==['U-001'],s['articleIds'],['U-001'])
 check(label+' native focused filter',n.get('focusId',n.get('focused'))=='Results-filter-unknown',n.get('focusId',n.get('focused')),'Results-filter-unknown')
 check(label+' request count',len(n['requests'])==[1,2,2,3][i],len(n['requests']),[1,2,2,3][i])
 check(label+' actual painted outline',(count>100)==s['focusVisible'],count,s['focusVisible'])
 if i!=2:check(label+' actual navigation',n['navigationMethod']==('Tab'if s['focusVisible']else'Pointer'),n['navigationMethod'],'Tab'if s['focusVisible']else'Pointer')
 if i==2 and not a.headless:check(label+' native pressed',n['pressed'],n['pressed'],True)
result={'status':'PASSED'if all(c['pass']for c in checks)else'FAILED','checks':len(checks),'differences':[c for c in checks if not c['pass']],'records':checks,'source':str(a.source.resolve()),'candidate':str(a.candidate.resolve()),'scope':'only new pointer/Space/same-focused held/release; unrelated earlier scopes reused'};a.output.write_text(json.dumps(result,indent=2)+'\n');print(result['status'],result['checks'],'differences',len(result['differences']));raise SystemExit(0 if result['status']=='PASSED'else 1)
