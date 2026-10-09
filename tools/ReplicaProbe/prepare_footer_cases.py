"""Only source presentation content becomes inputs; no source geometry is passed to native."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[2];s=r/'docs/replica/source';rows=[]
for width in [1000,1138,1280]:
 a=json.loads((s/f'actionfooter-mac-source-{width}.json').read_text())
 for row in a['rows']:
  def action(b):
   key=b['action'];loading=b['icon']=='spinner'
   icon='' if loading else {'back':'back','trial-back':'check'if b['variant']=='primary'else'back','restart':'refresh','connect':'db','preflight':'arrow','choose':'arrow','review':'arrow','trial-check':'check','run':'play','cancel':'stop','show-results':'arrow','save':'save','download':'save'}[key]
   return dict(action=key,label=b['label'],variant=b['variant'],iconName=icon,enabled=b['enabled'],loading=loading)
  back=next((action(b)for b in row['buttons']if b['variant']=='quiet'),None)
  actions=[action(b)for b in row['buttons']if b['variant']!='quiet']
  rows.append(dict(id=row['id'],viewport=width,hint=row['hint']['text'],back=back,actions=actions))
for p in [s/'actionfooter-native-cases.json',r/'artifacts/replica/windows-download/actionfooter-native-cases.json']:p.write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n')
print(len(rows),'presentation-only native inputs')
