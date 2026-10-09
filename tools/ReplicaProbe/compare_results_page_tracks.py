"""Actual pointer endpoints/activation/cancel versus original CSS + fresh source traces.
Frame timing is measured, never inferred from a settled screenshot; endpoints are strict.
"""
from pathlib import Path
import argparse,json,re
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('native',type=Path);p.add_argument('output',type=Path);a=p.parse_args();s=json.loads(a.source.read_text());errors=[];checks=[]
def check(condition,label,detail=None):
 checks.append(label)
 if not condition:errors.append({'path':label,'detail':detail})
def rgb(v):
 if v.startswith('#'):v=v[-6:];return tuple(int(v[i:i+2],16)for i in (0,2,4))
 return tuple(map(int,re.findall(r'\d+',v)[:3]))
sf=s['frames'];check(len(sf)>20,'source:actualFrames',len(sf));check(any(f['pressed']and f['over']and abs(f['scale']-.98)<.001 for f in sf),'source:held98');check(any(f['pressed']and not f['over']and abs(f['scale']-.98)<.001 for f in sf),'source:outsideStillActive');check(any(f['over']and rgb(f['background'])==(53,55,64)for f in sf),'source:hoverColor');check(any(not f['over']and rgb(f['background'])==(43,45,49)for f in sf),'source:restColor');check(s['requests'].count('detail:trial')==1,'source:singleHoldRelease',s['requests'])
summary={};seen=0
for mode in ['hover','leave','reverse','cancel','hold']:
 possible=[a.native/(mode+'-resultspage-tracks.json'),a.native/('tracks-trial-'+mode+'.json')];file=next((x for x in possible if x.exists()),None);check(file is not None,'native:'+mode+':record')
 if file is None:continue
 d=json.loads(file.read_text());all_frames=d['frames'];frames=all_frames[seen:];seen=len(all_frames);summary[mode]={'frames':len(frames),'requests':d['requests'],'pressedFrames':sum(f['pressed']for f in frames),'minScale':min(f['scale']for f in frames),'minOpacity':min(f['opacity']for f in frames)}
 check(len(frames)>10,'native:'+mode+':frames',len(frames))
 check(all(.97<=f['scale']<=1.001 and .85<=f['opacity']<=1.001 for f in frames),'native:'+mode+':validBounds')
 check(all(all(lo<=v<=hi for v,lo,hi in zip(rgb(f['background']),(43,45,49),(53,55,64)))for f in frames),'native:'+mode+':colorRange')
 if mode=='hover':check(any(f['over']and rgb(f['background'])==(53,55,64)for f in frames),'native:hover:endpoint')
 if mode=='leave':check(any(not f['over']and rgb(f['background'])==(43,45,49)for f in frames),'native:leave:endpoint')
 if mode=='reverse':
  check(any(f['over']and rgb(f['background'])==(53,55,64)for f in frames),'native:reverse:settled')
  check(any(43<rgb(f['background'])[0]<53 for f in frames),'native:reverse:actualIntermediate')
 if mode in ['cancel','hold']:check(any(f['pressed']and f['over']and abs(f['scale']-.98)<.001 for f in frames),'native:'+mode+':held98')
 if mode=='cancel':check(any(f['pressed']and not f['over']and abs(f['scale']-.98)<.001 for f in frames),'native:cancel:outsideStillActive')
 if mode=='hold':check(any(f['opacity']<.89 for f in frames),'native:hold:actualClickPress160')
if all(m in summary for m in ['reverse','cancel','hold']):
 check(summary['cancel']['requests']==summary['reverse']['requests'],'native:cancel:noActivation',summary['cancel']['requests'])
 check(summary['hold']['requests']==summary['cancel']['requests']+['detail:trial'],'native:hold:oneActivation',summary['hold']['requests'])
r={'status':'PASSED'if not errors else'FAILED','checks':checks,'sourceFrameCount':len(sf),'native':summary,'errors':errors,'timingBoundary':'Fresh actual frame traces preserved; sampled curves are not asserted bit-exact to independent OS render ticks.'};a.output.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'status':r['status'],'checks':len(checks),'errors':errors},ensure_ascii=False));raise SystemExit(bool(errors))
