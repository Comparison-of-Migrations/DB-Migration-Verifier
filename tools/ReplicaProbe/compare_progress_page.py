from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('--output',type=Path);a=p.parse_args()
s=json.loads(a.source.read_text());n=json.loads(a.candidate.read_text());original={r['id']:r for r in s['rows']};bad=[]
def eq(id,key,x,y):
 if isinstance(x,(int,float)) and isinstance(y,(int,float)):
  if abs(x-y)>.011:bad.append({'id':id,'key':key,'source':x,'native':y})
 elif x!=y:bad.append({'id':id,'key':key,'source':x,'native':y})
for r in n['rows']:
 o=original[r['id']]
 for key in ['page','heading','title','panel','main','number','note','idLabel','meter','tableShell','table','notice','dot']:
  x=o[key];y=r[key]
  if x is None or y is None:eq(r['id'],key,x,y);continue
  x=x if key=='page' else x['box'];y=y if key=='page' else y['box']
  for part in ['x','y','width','height']:eq(r['id'],key+'.'+part,x[part],y[part])
 for i,fragment in enumerate(r['small']):
  if 'smallFragments' in o:
   for part in ['x','y','width','height']:eq(r['id'],f'small{i}.'+part,o['smallFragments'][i][part],fragment['box'][part])
 for i,col in enumerate(r['columns']):eq(r['id'],'column'+str(i),o['header'][i]['box']['width'],col)
 for i,row in enumerate(r['rows']):
  for part in ['x','y','width','height']:eq(r['id'],f'row{i}.'+part,o['rows'][i]['box'][part],row['box'][part]);eq(r['id'],f'pill{i}.'+part,o['rows'][i]['pill']['box'][part],row['pill']['box'][part])
  eq(r['id'],f'pill{i}.text',o['rows'][i]['pill']['text'],row['pill']['text'])
 eq(r['id'],'note.text',o['note']['text'],r['note']['text']);eq(r['id'],'id.text',o['idLabel']['text'],r['idLabel']['text'])
out={'cases':len(n['rows']),'differences':bad,'passed':not bad};print(json.dumps({'cases':out['cases'],'differenceCount':len(bad),'first':bad[:35]},ensure_ascii=False,indent=2))
if a.output:a.output.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
