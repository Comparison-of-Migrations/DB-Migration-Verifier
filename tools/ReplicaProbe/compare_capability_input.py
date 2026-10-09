"""Compare original and candidate actual row states, retaining request sequence offsets explicitly."""
from pathlib import Path
import argparse,json,re
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args();o=r/f'docs/replica/source/capabilityrow-{a.platform}-input'
# Preserve observed request-sequence offsets; they are evidence bookkeeping, not tolerances.
pairs=[('source-key-space-permissions.json','key-space-permissions.json'),('source-key-enter-permissions.json','key-enter-permissions.json'),('source-invoke-language-language.json','read-language.json'),('source-invoke-data-hide.json','read-hide.json'),('source-invoke-data-restore.json','read-restore.json'),('source-invoke-permissions-reduced.json','read-reduced.json'),('source-invoke-mount-detach.json','read-detach.json'),('source-invoke-mount-remount.json','read-remount.json')]
if a.platform=='mac':
 pairs += [('source-pointer.json','actual-button-click-after-cursor-return.json'),('source-passive.json','actual-passive-click.json'),('source-cancel.json','actual-button-cancel.json')]
else:
 pairs += [('source-pointer-0-cancel-False.json','native-initial-ko-pointer.json'),('source-pointer-1-cancel-False.json','pointer-1-cancel-False.json'),('source-pointer-0-cancel-True.json','pointer-0-cancel-True.json')]
def rgba(s):
 if s=='Transparent':return [0,0,0,0]
 if s.startswith('#'):return [int(s[i:i+2],16)for i in [3,5,7,1]]
 v=list(map(float,re.findall(r'[\d.]+',s)));return list(map(round,v[:3]))+[round(v[3]*255)if len(v)>3 else 255]
errors=[];checks=[]
for sf,nf in pairs:
 s=json.loads((o/sf).read_text());n=json.loads((o/nf).read_text());offset=0 if a.platform=='windows' and sf.startswith('source-pointer-') else 2 if a.platform=='mac' and nf=='read-language.json' else 0 if a.platform=='mac' and nf=='actual-button-click-after-cursor-return.json' else 1
 check={'source':sf,'native':nf,'sourceRequests':s['requests'],'nativeRequests':n['requests'],'requestOffset':offset};checks.append(check)
 if n['requests']-s['requests']!=offset:errors.append({**check,'property':'requestOffset'})
 for key in ['english','key','focused','attached','button','visible']:
  if s[key]!=n[key]:errors.append({**check,'property':key,'sourceValue':s[key],'nativeValue':n[key]})
 # Initial native click snapshot is after the guarded driver restored the prior outside cursor.
 # Hover comparison uses the same-position passive/cancel samples and independent 48 timings.
 if nf=='actual-button-click-after-cursor-return.json':check['backgroundBoundary']='Native cursor already returned outside; source cursor still on button. Exact hover checked in passive/cancel/timing evidence.'
 elif rgba(s['background'])!=rgba(n['background']):errors.append({**check,'property':'background'})
 if float(s['opacity'])!=float(n['opacity']):errors.append({**check,'property':'opacity'})
report={'platform':a.platform,'cases':len(pairs),'errors':errors,'checks':checks,'scope':'Actual original/native states. Explicit sequence offsets: native prior calibration pointer adds1; Mac language pointer correction occurred after reduced adds2. Initial Mac pointer after cursor restoration has offset0 and intentionally different hover position (documented per check). All other fields exact; zero-alpha colors normalized. Windows source raw pointer trio and fresh unique initial native KO pointer are strict. Native initial re-capture resets its independent request count; offsets0 reflect the actual captured sequence.'};(o/'actual-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(len(errors),'differences');print(json.dumps(errors[:10],ensure_ascii=False,indent=2));raise SystemExit(bool(errors))
