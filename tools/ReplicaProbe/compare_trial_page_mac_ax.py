"""Natural table hierarchy and named controls; literal OS roles retained in evidence."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[2];nroot=r/'artifacts/replica/workers/home/trialpage/ead97bf9d43a46428c9501741738b6a4/actual-macos';sroot=r/'artifacts/replica/workers/home/trialpage/ec53805086a04095ab4d7b7605a9c14d/actual-macos'
def tree(d,i):
 node=dict(d[i]);i+=1;children=[]
 for _ in range(node['children']):c,i=tree(d,i);children.append(c)
 node['nodes']=children;return node,i
def flatten(n):return [n]+[x for c in n['nodes'] for x in flatten(c)]
result=[]
for lang in ['ko','en']:
 sp=sroot/('source-en-final-ax.json' if lang=='en' else 'source-ko-ax.json')
 if not sp.exists():sp=r/'artifacts/replica/workers/home/trialpage/d1b71f23922b441fa669d37e26dd9e0d/actual-macos/source-ko-ax.json'
 s=json.loads(sp.read_text());n=json.loads((nroot/f'native-{lang}-table-ax.json').read_text());st,_=tree(s,next(i for i,x in enumerate(s)if x['role']=='AXTable'));nt,_=tree(n,next(i for i,x in enumerate(n)if x['role']=='AXTable'))
 sr=st['nodes'][:4];nr=[row for group in nt['nodes'] for row in group['nodes']];assert len(nt['nodes'])==2 and len(nr)==len(sr)==4
 for a,b in zip(sr,nr):
  assert a['role']=='AXRow' and b['role']=='AXCell' and len(a['nodes'])==len(b['nodes'])==3
  for ac,bc in zip(a['nodes'],b['nodes']):
   assert len(ac['nodes'])==len(bc['nodes'])==1 and ac['nodes'][0]['value']==bc['nodes'][0]['value']
   for leaf in [bc,bc['nodes'][0]]:assert leaf['box']['width']>0 and leaf['box']['height']>0
 assert nt['box']['width']>0 and nt['box']['height']>0
 sh=[x['title'] for x in s if x['role']=='AXHeading'];nh=[x['title'] for x in n if x['role']=='AXHeading'];assert sh==nh and len(nh)==4
 nf=[x['title'] for x in n if x['role']=='AXTextField'];sf=[x['title'] for x in s if x['role']=='AXTextField' and x['title'] in nf];assert nf==sf and len(nf)==4
 nc=[x['title'] for x in n if x['role']=='AXCheckBox'];sc=[x['title'] for x in s if x['role']=='AXCheckBox'];assert nc==sc and len(nc)==1
 result.append({'language':lang,'rows':4,'cells':12,'positiveBounds':True,'namedHeading':4,'fields':4,'consent':1,'sourcePath':str(sp.relative_to(r)),'nativeTableRole':nt['role'],'sourceRowRole':sr[0]['role'],'nativeRowRole':nr[0]['role'],'nativeHeaderRole':nr[0]['nodes'][0]['role'],'nativeBodyCellRole':nr[1]['nodes'][0]['role']})
(r/'docs/replica/source/trialpage-mac-ax-comparison.json').write_text(json.dumps({'cases':result,'errors':[]},ensure_ascii=False,indent=2));print('Mac natural table/leaf/name/positive bounds KOEN passed')
