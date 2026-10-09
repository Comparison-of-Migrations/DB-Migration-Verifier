"""Compare actual requested states and final attached geometry; source is independently recorded."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[2]/'docs/replica/source';checks=[]
def check(source,native,label):
 def compare(a,b):
  if isinstance(a,dict):
   for k,v in a.items():
    if k!='id':compare(v,b[k])
  elif isinstance(a,list):
   assert len(a)==len(b)
   for x,y in zip(a,b):compare(x,y)
  elif isinstance(a,(int,float)) and not isinstance(a,bool):assert abs(a-b)<.001,(label,a,b)
  else:assert a==b,(label,a,b)
 compare(source,native['snapshot']);assert native['progress']==int(source['expanded']);checks.append(label)
mac=r/'categorysection-mac-input'
for label in ['expanded','selected','collapsed','space-expanded']:
 s=json.loads((mac/('source-'+label+'.json')).read_text());n=json.loads((mac/(label+'.json')).read_text());check(s['rows'][0],n,'mac-'+label)
 assert all(e['trusted']for e in s['events']);assert [{k:v for k,v in e.items()if k!='trusted'}for e in s['events']]==n['events']
for label,source in [('final-reduced-collapsed','collapsed'),('final-space-expanded','space-expanded')]:
 n=json.loads((mac/(label+'.json')).read_text());s=json.loads((mac/('source-'+source+'.json')).read_text());check(s['rows'][0],n,'mac-'+label)
 assert n['events']==[{k:v for k,v in e.items()if k!='trusted'}for e in s['events']]
source=json.loads((r/'categorysection-windows-source.json').read_text())['rows'];win=r/'categorysection-windows-input'
for label,expanded in [('expanded-accessibility',True),('collapsed-accessibility',False),('reduced-expanded-accessibility',True)]:
 n=json.loads((win/(label+'.json')).read_text());s=next(x for x in source if x['id']=='ko-302-data-0'+str(int(expanded)));check(s,n,'windows-'+label)
 # Locked typography's wrapper + inner native text peers share the same name.
 assert set(n['automation']['details'])==({x['text']for x in s['items']}if expanded else set())
 assert n['events']==[{'action':'expand','value':v}for v in ([True]if label.startswith('expanded-')else[True,False]if not expanded else[True,False,True])]
(r/'categorysection-input-comparison.json').write_text(json.dumps({'checks':checks,'errors':[],'windowsTextPeerScope':'Open: two distinct detail strings, wrapper/inner peer duplicates retained; closed: zero.'},ensure_ascii=False,indent=2)+'\n')
print(len(checks),'actual state comparisons passed')
