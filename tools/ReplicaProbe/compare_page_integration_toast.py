"""Strict parent toast boxes/style/wrap and real-time lifecycle; keyframes reused."""
from pathlib import Path
import argparse,json,re
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
s=json.loads(a.source.read_text());c=json.loads(a.candidate.read_text());checks=0;diff=[]
def eq(path,x,y,tol=.001):
 global checks
 checks+=1
 equal=abs(x-y)<=tol if isinstance(x,(int,float)) and not isinstance(x,bool) and isinstance(y,(int,float)) and not isinstance(y,bool) else x==y
 if not equal:diff.append({'path':path,'source':x,'candidate':y})
def rgba(v):
 if v.startswith('#'):
  h=v[1:];return [int(h[i:i+2],16)for i in [2,4,6,0]] if len(h)==8 else [int(h[i:i+2],16)for i in [0,2,4]]+[255]
 nums=[float(x)for x in re.findall(r'[\d.]+',v)];return [*map(int,nums[:3]),round(nums[3]*255)if len(nums)==4 else 255]
def nums(v):return [float(x)for x in re.findall(r'-?\d+(?:\.\d+)?',str(v))]
def thickness(v,css=False):
 n=nums(v)
 if len(n)==1:return n*4
 if css:
  if len(n)==2:n=[n[0],n[1],n[0],n[1]]
  return [n[3],n[0],n[1],n[2]]
 return n
def shadow(v):
 color=re.search(r'rgba?\([^)]*\)|#[a-fA-F0-9]+',v).group(0);values=nums(v.replace(color,''));return {'values':values+[0]*(4-len(values)),'color':rgba(color)}
by={r['id']:r for r in c['rows']};eq('geometry.count',len(s['rows']),len(c['rows']))
for r in s['rows']:
 id=r['id'];v=by.get(id)
 if v is None:diff.append({'path':id,'source':'case required','candidate':None});continue
 for k in ['width','height','dpr','text','lines']:eq(id+'.'+k,r[k],v[k])
 for k in ['x','y','width','height']:eq(id+'.box.'+k,r['box'][k],v['box'][k])
 for k in ['background','borderColor','color']:eq(id+'.style.'+k,rgba(r['style'][k]),rgba(v['style'][k]))
 eq(id+'.style.borderWidth',thickness(r['style']['borderWidth'],True),thickness(v['style']['borderWidth']))
 eq(id+'.style.radius',thickness(r['style']['radius']),thickness(v['style']['radius']))
 eq(id+'.style.padding',thickness(r['style']['padding'],True),thickness(v['style']['padding']))
 eq(id+'.style.shadow',shadow(r['style']['shadow']),shadow(v['style']['shadow']))
 for k in ['fontSize','lineHeight','maxWidth']:eq(id+'.style.'+k,nums(r['style'][k])[0],nums(v['style'][k])[0])
 eq(id+'.style.hitTest',r['style']['hitTest'],v['style']['hitTest']);eq(id+'.opacity',1,v['opacity'])
 if r['id'].endswith('-long'):eq(id+'.max420',420,v['box']['width'])
 if 'accessibilitySource'in s:
  peer=v['accessibility']
  expected={'toastAttachedLive':'Polite','toastPeerType':'StatusBar','toastPeerLive':'Polite','toastPeerName':'','toastPeerControl':True,'toastPeerContent':True,'toastPeerChildren':1,'labelName':r['text'],'labelType':'Text','labelLive':'Off','labelControl':True,'labelContent':True,'labelChildren':0}
  for k,value in expected.items():eq(id+'.accessibility.'+k,value,peer.get(k))
bylife={r['id']:r for r in c.get('lifetimes',[])}
for r in s.get('lifetimes',[]):
 v=bylife.get(r['id'])
 if v is None:
  if c.get('geometryOnly'):continue
  diff.append({'path':r['id']+'.lifetime','source':'required','candidate':None});continue
 for k in ['replaced','survivesOldDeadline','beforeNewDeadline','removed','focusSame','attachedOld']:eq(r['id']+'.'+k,r[k],v[k])
 eq(r['id']+'.timing.oldAfter4200',True,v['oldDeadlineMs']>=4200)
 eq(r['id']+'.timing.before4200',True,v['beforeMs']<4200)
 eq(r['id']+'.timing.after4200',True,v['afterMs']>=4200)
if not c.get('geometryOnly'):
 eq('disposal.removed',True,c['disposal']['disposedRemoved']);eq('disposal.noOldTimerReappearance',True,c['disposal']['oldTimerNoReappearance'])
for d in s.get('disposals',[]):
 eq('source.discardedContext',True,d['browsingContextRemoved']);eq('source.oldTimerCancelled',True,d['oldTimerDidNotMutateDiscardedDocument'])
native=c['phase'].startswith('ACTUAL');result={'phase':('ACTUAL_OS_' if native else 'HEADLESS_')+('DIFFERENCES' if diff else 'PASSED'),'scope':'MainContent parent Toast geometry'+str(len(c['rows']))+' and applicable real timer/lifetime; Motion200ms unchanged/reused','source':str(a.source),'candidate':str(a.candidate),'checks':checks,'differences':diff,'normalization':'CSS rgba/ARGB and thickness order/omitted shadow spread normalized; numeric geometry tolerance0.001; no font/geometry waiver; actual/native noninteractive presentation seeds, not pointer input'}
a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'phase':result['phase'],'checks':checks,'differences':len(diff)}))
