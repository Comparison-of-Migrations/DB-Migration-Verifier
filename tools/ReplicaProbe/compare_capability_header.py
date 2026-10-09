"""Strict source/candidate header boxes and actual paint/text/peer values."""
from pathlib import Path
import argparse,json,math,re
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);p.add_argument('--candidate',type=Path);p.add_argument('--report',type=Path);a=p.parse_args();s=json.loads((r/f'docs/replica/source/capabilityheader-{a.platform}-source.json').read_text());n=json.loads((a.candidate or r/f'docs/replica/source/capabilityheader-{a.platform}-native/capabilityheader-native.json').read_text());errors=[]
def equal(c,key,x,y):
 if isinstance(x,(int,float)) and isinstance(y,(int,float)):
  if abs(x-y)<=.001:return
 elif x==y:return
 errors.append({'case':c,'property':key,'source':x,'native':y})
def color(x):
 if x.startswith('#'):return [int(x[i:i+2],16)for i in [3,5,7,1]]
 t=list(map(float,re.findall(r'[\d.]+',x)));return list(map(round,t[:3]))+[round(t[3]*255)if len(t)>3 else 255]
assert len(s['headers'])==len(n['headers'])==12
for sh,nh in zip(s['headers'],n['headers']):
 c=sh['id'];equal(c,'id',c,nh['id'])
 for k in ['x','y','width','height']:equal(c,'root.'+k,sh['root'][k],nh['root'][k])
 for i,(sc,nc)in enumerate(zip(sh['cells'],nh['cells'])):
  for k in ['x','y','width','height']:equal(c,f'cell{i}.{k}',sc['box'][k],nc['box'][k])
  for k in ['text','textAlign']:equal(c,f'cell{i}.{k}',sc[k],nc[k])
  equal(c,f'cell{i}.fontSize',float(sc['fontSize'][:-2]),nc['fontSize']);equal(c,f'cell{i}.fontWeight',int(sc['fontWeight']),nc['fontWeight']);equal(c,f'cell{i}.lineHeight',math.floor(float(sc['lineHeight'][:-2])*128)/128,nc['lineHeight']);equal(c,f'cell{i}.color',color(sc['color']),color(nc['color']));equal(c,f'cell{i}.background',color(sc['background']),color(nc['background']));equal(c,f'cell{i}.border',color('rgb'+sc['border'].split('rgb')[1]),color(nc['border']));equal(c,f'cell{i}.peerType','HeaderItem',nc['peerType']);equal(c,f'cell{i}.peerRole','ColumnHeader',nc['peerRole']);equal(c,f'cell{i}.childNames',[] if a.platform=='windows' and 'peerName' in nc else [sc['text']],nc['childNames']);
  if a.platform=='windows' and 'peerName' in nc:equal(c,f'cell{i}.peerName',sc['text'],nc['peerName'])
  equal(c,f'cell{i}.lineCount',len(sc['lineRects']),len(nc['lineTexts']));equal(c,f'cell{i}.joinedText',re.sub(r'\s+','',sc['text']),re.sub(r'\s+','',''.join(nc['lineTexts'])))
  # DOM Range is a font box; the native text control is the CSS line box.
  # Horizontal range endpoints are quantized to CSS fixed point; source padding/line box is exact.
  equal(c,f'cell{i}.label.x',sc['textBox']['x'],nc['textLayout']['x']);equal(c,f'cell{i}.label.width',sc['textBox']['width'],nc['textLayout']['width']);line=math.floor(float(sc['lineHeight'][:-2])*128)/128;count=len(sc['lineRects']);equal(c,f'cell{i}.lineBox.y',12+math.floor((sh['root']['height']-24-.5-count*line)*64)/128,nc['textLayout']['y']);equal(c,f'cell{i}.lineBox.height',count*line,nc['textLayout']['height']);
  for j,(sr,nw) in enumerate(zip(sc['lineRects'],nc['lineWidths'])):
   width=math.ceil(nw*128)/128;equal(c,f'cell{i}.line{j}.width',sr['width'],width);left=nc['box']['x']+(math.floor((nc['box']['width']-width)*64)/128 if i in [1,2] else 18);equal(c,f'cell{i}.line{j}.x',sr['x'],left)
report={'platform':a.platform,'cases':12,'errors':errors,'scope':'Strict header/root/cell geometry, source fixed-point text range horizontal endpoints, CSS anonymous line box y/height, actual palette/labels/leaf names/four header peers. DOM font box y/height differs in representation from native line box; protected Typography owns baseline/raster.'};(a.report or r/f'docs/replica/source/capabilityheader-{a.platform}-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:16],ensure_ascii=False,indent=2));raise SystemExit(bool(errors))
