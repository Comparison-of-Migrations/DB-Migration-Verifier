from pathlib import Path
import json,hashlib
r=Path(__file__).resolve().parents[2]/'docs/replica/source';v={}
for os in ['mac','windows']:
 previous={49:r/('typography-notice-mac49-after/typography-candidate-layout.json' if os=='mac' else 'windows-typography-final49-native.json'),66:r/f'typography-notice-{os}66-native/typography-state-native.json'}
 for n,name in [(49,'typography-candidate-layout.json'),(66,'typography-state-native.json')]:
  a=json.loads(previous[n].read_text());b=json.loads((r/f'typography-inline-{os}{n}'/name).read_text());diff=[{'id':x.get('id'),'before':x,'after':y}for x,y in zip(a,b)if x!=y];assert len(a)==len(b)==n;v[f'{os}{n}']={'count':n,'differences':diff}
  if os=='mac':
   old=previous[n].parent if n==49 else r/'typography-notice-mac66-before';d=[f.name for f in (r/f'typography-inline-mac{n}').glob('*.png')if hashlib.sha256(f.read_bytes()).digest()!=hashlib.sha256((old/f.name).read_bytes()).digest()];v[f'{os}{n}']['changedRasters']=d;assert not d,d
  assert not diff,diff[:2]
(r/'typography-inline-regression.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(v)
