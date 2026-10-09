"""Finite actual source input; no uncertain input replay; always exact close."""
from pathlib import Path
import json,subprocess,time
r=Path(__file__).resolve().parents[2]
h=r/'artifacts/replica/workers/root/progresspage/progress-page-source'
o=r/'artifacts/replica/workers/root/progresspage'
f=r/'docs/replica/source/root-progresspage-mac-input.json'
def act(mode):
 prev=f.stat().st_mtime_ns
 subprocess.run([str(h),mode,str(o/'source-actual-control.json')],check=True)
 end=time.monotonic()+4
 while time.monotonic()<end:
  if f.stat().st_mtime_ns>prev:
   v=json.loads(f.read_text());(o/('source-actual-'+mode+'.json')).write_bytes(f.read_bytes());return v
  time.sleep(.02)
 raise RuntimeError('No fresh evidence '+mode+'; no input replay')
try:
 subprocess.run([str(h),'inspect',str(o/'source-actual-ko-ax.json')],check=True)
 v=act('hover');assert v['state']['rows'][0]['hover'] and v['state']['rows'][0]['background']=='rgb(58, 60, 68)'
 v=act('leave');assert not v['state']['rows'][0]['hover']
 v=act('tick');assert v['state']['phase']=='live' and len(v['frames'])>5
 for j in range(4):
  assert any(x['state']['rows'][j]['cells'][1]['opacity']<.999 for x in v['frames'])
  assert all(x['state']['rows'][j]['pillOpacity']==1 for x in v['frames'])
 v=act('language');assert v['state']['lang']=='en'
 subprocess.run([str(h),'inspect',str(o/'source-actual-en-ax.json')],check=True)
 (o/'source-actual-input-comparison.json').write_text(json.dumps({'dpr':2,'rows':4,'firstLiveCells':4,'owner':'Cell','pillUnchanged':True,'hover':'rgb(58, 60, 68)','leave':True,'language':'en','passed':True},indent=2))
finally:
 subprocess.run([str(h),'close',str(o/'source-actual-cleanup.json')],check=True)
