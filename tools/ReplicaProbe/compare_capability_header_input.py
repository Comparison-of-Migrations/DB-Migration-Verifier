"""Header language/parent-column/lifecycle and passive blur; fixture utilities are outside the component."""
from pathlib import Path
import argparse,json,re
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();o=r/f'docs/replica/source/capabilityheader-{a.platform}-input';errors=[];checks=[]
def rgba(x):
 if x.startswith('#'):return [int(x[i:i+2],16)for i in [3,5,7,1]]
 t=list(map(float,re.findall(r'[\d.]+',x)));return list(map(round,t[:3]))+[round(t[3]*255)if len(t)>3 else 255]
for label in ['passive','language','narrow','detach','remount','restore']:
 s=json.loads((o/('source-'+label+'.json')).read_text());n=json.loads((o/('native-'+label+'.json')).read_text());checks.append(label)
 for key in ['english','attached','narrow','texts']:
  if s[key]!=n[key]:errors.append({'case':label,'property':key,'source':s[key],'native':n[key]})
 for key in ['width','height']:
  if abs(s[key]-n[key])>.001:errors.append({'case':label,'property':key,'source':s[key],'native':n[key]})
 if rgba(s['background'])!=rgba(n['background']):errors.append({'case':label,'property':'background'})
 if label=='passive'and(s['focused']!=''or n['focused']!=''):errors.append({'case':label,'property':'passiveFocus','source':s['focused'],'native':n['focused']})
report={'platform':a.platform,'cases':len(checks),'errors':errors,'checks':checks,'scope':'Actual header passive click clears prior utility focus and performs no header action. Language/parent-column widths/display removal+return update named texts and boxes. Test utility AX focus is outside header product scope; compared only when header itself is the pointer target. Detached source display:none maps to state box0, native also records absent box0.'};(o/'comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:10],ensure_ascii=False,indent=2));raise SystemExit(bool(errors))
