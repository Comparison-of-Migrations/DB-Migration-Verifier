"""Compare actual owned SCK frames. Fit each input's phase independently;
OS delivery/render latency is NOT silently treated as equal wall-clock time.
"""
from pathlib import Path
import json,sys
import numpy as np
root=Path(__file__).resolve().parents[2];folder=root/'docs/replica/source'
def read(label,scenario):
 p=folder/f'scrollviewport-mac-{label}-{scenario}-stream.json'
 if not p.exists() and scenario=='hide':p=folder/f'scrollviewport-mac-{label}-stream.json'
 v=json.loads(p.read_text());assert len(v['rows'])>40
 return v['rows']
def median(rows,begin,end,band=None):
 values=[r['rgb'] if band is None else r['rightBand'][band] for r in rows if begin<=r['ms']<=end]
 assert len(values)>=3
 return np.median(values,axis=0).tolist()
def difference(a,b):return float(np.max(np.abs(np.array(a)-np.array(b))))
phase=np.linspace(0,1,1001);low=np.zeros(1001);high=np.ones(1001)
for _ in range(40):
 t=(low+high)/2;x=3*(1-t)**2*t*.42+3*(1-t)*t*t*.58+t**3
 low=np.where(x<phase,t,low);high=np.where(x<phase,high,t)
t=(low+high)/2;ease=3*(1-t)*t*t+t**3
fits={}
for label in ('source','candidate'):
 rows=read(label,'hide');items=[r for r in rows if 400<=r['ms']<=900]
 times=np.array([r['ms'] for r in items]);actual=np.array([r['rgb'] for r in items]);best=None
 for start in range(450,631,2):
  for duration in range(200,321,2):
   alpha=1-np.interp(np.clip((times-start)/duration,0,1),phase,ease)
   predicted=np.array([35,36,40])+alpha[:,None]*(np.array([145,146,148])-np.array([35,36,40]))
   err=np.abs(actual-predicted);mae=float(np.mean(err))
   if best is None or mae<best['meanChannelError']:best=dict(fadeStartMsFromRequestedInput=start,durationMs=duration,meanChannelError=mae,maxChannelError=float(np.max(err)))
 fits[label]=best
checks=[]
for scenario,window,band in [('hover-short',(700,850),7),('reenter-expanded',(1650,1900),7),('reenter',(1000,1300),7),('wheelcancel',(800,1100),None)]:
 a=median(read('source',scenario),*window,band);b=median(read('candidate',scenario),*window,band)
 checks.append(dict(case=scenario,windowMs=window,logicalBandX=None if band is None else 200+band,sourceMedianRgb=a,nativeMedianRgb=b,maxChannelDifference=difference(a,b),passed=difference(a,b)<=2))
for scenario in ('hover-short','reenter'):
 # Subsequent hide; the two event queues have different delivery latency.
 a=median(read('source',scenario),1870,1940);b=median(read('candidate',scenario),1870,1940)
 checks.append(dict(case=scenario+'-late-hide',sourceMedianRgb=a,nativeMedianRgb=b,maxChannelDifference=difference(a,b),passed=difference(a,b)<=2))
fitPass=all(abs(v['durationMs']-250)<=25 and v['meanChannelError']<=2.5 for v in fits.values())
result=dict(platform='mac',scope='owned actual stream; hide curve and hover/exit/reentry/repeated-wheel endpoints. Independent phase fits do not assert identical input latency or bit-exact raster.',fadeFits=fits,fadeFitPassed=fitPass,checks=checks,passed=fitPass and all(c['passed'] for c in checks))
(folder/'scrollviewport-mac-overlay-comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result));sys.exit(not result['passed'])
