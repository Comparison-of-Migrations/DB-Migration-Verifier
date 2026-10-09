"""Strict parent layout/copy/policy comparison; protected child glyph probes stay reusable."""
import argparse,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('source');p.add_argument('candidate');p.add_argument('--output',required=True);a=p.parse_args()
s=json.load(open(a.source));n=json.load(open(a.candidate));diff=[];checks=0
def eq(label,x,y):
 global checks
 checks+=1
 if isinstance(x,(int,float)) and not isinstance(x,bool):
  if not isinstance(y,(int,float)) or abs(x-y)>1e-6:diff.append({'field':label,'source':x,'candidate':y})
 elif x!=y:diff.append({'field':label,'source':x,'candidate':y})
def box(label,x,y):
 for k in ('x','y','width','height'):eq(label+'.'+k,x[k],y[k])
nn={r['id']:r for r in n['rows']};eq('DPR',s['dpr'],n['dpr']);eq('cases',len(s['rows']),len(n['rows']))
for sr in s['rows']:
 id=sr['id'];nr=nn.get(id)
 if nr is None:diff.append({'field':id,'candidate':'missing'});continue
 for key in ('root','heading','tableShell'):box(id+'.'+key,sr[key]['box'],nr[key])
 box(id+'.table',sr['table'],nr['table'])
 for key in ('title','description'):
  box(id+'.'+key,sr[key]['box'],nr[key]['box']);eq(id+'.'+key+'.text',sr[key]['text'],nr[key]['text'])
 box(id+'.notice',sr['notice']['root']['box'],nr['notice']['root']);eq(id+'.notice.title',sr['notice']['title']['text'],nr['notice']['title']);eq(id+'.notice.body',sr['notice']['body']['text'],nr['notice']['body']);eq(id+'.notice.kind','warn',nr['notice']['kind'])
 box(id+'.optional',sr['optional']['root']['box'],nr['optional']['root'])
 for key in ('title','body'):
  box(id+'.optional.'+key,sr['optional'][key]['box'],nr['optional'][key]['box']);eq(id+'.optional.'+key+'.text',sr['optional'][key]['text'],nr['optional'][key]['text'])
 for key in ('hint','back','forward'):
  sb=sr['footer'][key]['box'];fr=sr['footer']['root']['box'];local={**sb,'x':sb['x']-fr['x'],'y':sb['y']-fr['y']};box(id+'.footer.'+key,local,nr['footer'][key]['box']);eq(id+'.footer.'+key+'.text',sr['footer'][key]['text'],nr['footer'][key]['text'])
 eq(id+'.footer.enabled',sr['footer']['enabled'],nr['footer']['enabled']);eq(id+'.english',sr['english'],nr['english'])
 # Buttons may visually overflow cell boundaries; compare the exact page-relative box.
 box(id+'.permission',sr['permission']['box'],nr['permission']['box']);eq(id+'.permission.text',sr['permission']['text'],nr['permission']['text'])
result={'status':'PASSED' if not diff else 'DIFFERENCES','cases':len(s['rows']),'checks':checks,'differences':diff,'source':str(Path(a.source).resolve()),'candidate':str(Path(a.candidate).resolve())};Path(a.output).write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps({'cases':len(s['rows']),'checks':checks,'differences':len(diff)}));raise SystemExit(bool(diff))
