"""Normalize fresh original expanded4 to its child section; preserve raw parent evidence."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('output',type=Path);a=p.parse_args();s=json.loads(a.source.read_text());rows=[]
for r in s['rows']:
 g=r['geometry'];origin=g['advancedToggle'];dx=origin['x'];dy=origin['y']
 def box(v):return {'x':v['x']-dx,'y':v['y']-dy,'width':v['width'],'height':v['height']}
 geometry={'root':{'x':0,'y':0,'width':g['root']['width'],'height':g['advancedPanel']['y']+g['advancedPanel']['height']-dy},'toggle':box(g['advancedToggle']),'panel':box(g['advancedPanel']),'split':box(g['advancedSplit']),'columns':[box(v)for v in g['advancedColumns']],'titles':[box(v)for v in g['advancedTitles']],'descriptions':[box(v)for v in g['advancedDescriptions']]}
 rows.append({'id':r['id'],'language':r['language'],'width':r['width'],'actualWidth':r['actualWidth'],'toggleRetained':r['toggleRetained'],'explanationRetained':r['explanationRetained'],'columnRetained':r['columnRetained'],'newPageAnimations':0,'geometry':geometry,'parentAllocation':{'pageWidth':g['root']['width'],'document':r['document']}})
v={'scope':'Original expanded Advanced child4 derived only by subtracting literal section origin; parent not compared. Actual parentCSS pagewidth controls standalone allocatedwidth. No inferred role/font/input exemptions.','rawSource':str(a.source.resolve()),'recording':s.get('recording'),'dpr':s['dpr'],'rows':rows};a.output.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n');print(a.output)
