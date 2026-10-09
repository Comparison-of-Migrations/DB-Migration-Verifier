"""Compare measured source input advance and real production focus-track samples."""
import argparse,json,re,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('native',type=Path);p.add_argument('output',type=Path);p.add_argument('--motion',action='store_true');p.add_argument('--font-fragments',action='store_true');a=p.parse_args();s=json.loads(a.source.read_text());n=json.loads(a.native.read_text());rows=[]
if not a.motion:
 for src,native in zip(s['rows'],n):
  assert src['id']==native['id'];delta=native['textWidth']-src['width'];assert abs(delta)<=1/64
  for prop in ('width','height'):assert abs(src['rect'][prop]-native['box'][prop])<1e-9
  if a.font_fragments:assert src['fontHeight']==native['caret']['height'],(src,native)
  rows.append({'id':src['id'],'advanceDelta':delta,'width':native['box']['width'],'height':native['box']['height']})
else:
 for src,native in zip(s['output'],n):
  assert src['name']==native['name']
  for direction in ('samples','reversed'):
   for b,c in zip(src[direction],native[direction]):
    assert b['time']==c['time'];rgb=[float(x) for x in re.findall(r'[\d.]+',b['border'])];raw=c['border'];paint=[math.floor(raw[k]+.5) for k in ('R','G','B')];assert rgb==paint,(b,c)
    shadow=b['shadow'];spread=0 if shadow=='none' else float(re.findall(r'(-?[\d.]+)px',shadow)[-1]);error=abs(spread-c['spread']);assert error<1e-5,(b,c)
    rows.append({'scenario':src['name'],'direction':direction,'time':b['time'],'spreadError':error})
  for d in src['durations']:assert abs(d['duration']-native['duration'])<1e-5
result={'scope':'fractional CSS focus tracks' if a.motion else 'single-line input width/box; not full component acceptance','samples':len(rows),'passed':True,'rows':rows};a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(result['scope'],len(rows),'passed')
