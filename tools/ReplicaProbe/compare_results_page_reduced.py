"""Reduced own Trial immediate endpoints: actual source/native or finite headless."""
from pathlib import Path
import argparse,json,re
p=argparse.ArgumentParser();p.add_argument('native',type=Path);p.add_argument('output',type=Path);p.add_argument('--source',type=Path);a=p.parse_args();errors=[];checks=[]
def check(value,label,detail=None):
 checks.append(label)
 if not value:errors.append({'path':label,'detail':detail})
def rgb(value):
 if value.startswith('#'):return tuple(int(value[-6:][i:i+2],16)for i in (0,2,4))
 return tuple(map(int,re.findall(r'\d+',value)[:3]))
if a.source:
 s=json.loads(a.source.read_text());check(bool(s['frames']),'source:frames');check(all(f['reduced'] and not f['animations']for f in s['frames']),'source:noAnimations');check(s['requests']==['detail:trial'],'source:oneActivation',s['requests']);check(any(f['pressed'] and not f['over'] and abs(f['scale']-.98)<.001 for f in s['frames']),'source:outsideHeld98')
 for mode in ['hover','cancel','hold']:
  candidates=[a.native/('reduced-'+mode+'-resultspage-tracks.json'),a.native/('reduced-tracks-trial-'+mode+'.json')];file=next((x for x in candidates if x.exists()),None);check(file is not None,mode+':record')
  if file is None:continue
  d=json.loads(file.read_text());f=d['frames'];check(bool(f),mode+':frames');check(all(x['reduced'] for x in f),mode+':reduced');check(all(abs(x['opacity']-1)<.001 for x in f),mode+':noClickOpacity');check(all(rgb(x['background'])in[(43,45,49),(53,55,64)]for x in f),mode+':immediateColor');check(all(abs(x['scale']-(.98 if x['pressed'] else 1))<.001 for x in f),mode+':immediateScale');check(d['requests']==(['detail:trial']if mode=='hold'else[]),mode+':requests',d['requests'])
else:
 d=json.loads(a.native.read_text());check([x['label']for x in d['states']]==['hover','held','outside-held','cancelled','clicked'],'headless:coverage')
 for x in d['states']:
  name=x['label'];check(abs(x['opacity']-1)<.001,name+':noClickOpacity');check(abs(x['scale']-(.98 if x['pressed']else 1))<.001,name+':immediateScale');check(rgb(x['background'])==((53,55,64)if x['over']else(43,45,49)),name+':immediateColor');check(x['requests']==(['detail:trial']if name=='clicked'else[]),name+':requests',x['requests'])
r={'status':'FAILED'if errors else'PASSED','checks':checks,'errors':errors,'scope':'New reduced Trial immediate hover/held/outside/cancel/click; no repeated page matrices'};a.output.write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({'status':r['status'],'checks':len(checks),'errors':errors}));raise SystemExit(bool(errors))
