"""Actual numeric steps against independently recorded source states."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[2]/'docs/replica/source'
rows=[]
for native in sorted(root.glob('numberinput-windows-native-step-*.json')):
 source=root/native.name.replace('-native-step-','-step-')
 if not source.exists():raise RuntimeError('Missing original state: '+source.name)
 s=json.loads(source.read_text())['rows'][0];n=json.loads(native.read_text())['rows'][0]
 differences=[k for k in ('value','number','badInput','rangeUnderflow','rangeOverflow','stepMismatch','valid') if s[k]!=n[k]]
 for k in ('x','y','width','height'):
  if s['rect'][k]!=n['rect'][k]:differences.append('rect.'+k)
 if not n['focused'] or n['peerOffscreen']:differences.append('actual focus/peer visibility')
 rows.append({'native':native.name,'source':source.name,'value':n['value'],'differences':differences})
assert rows,'No actual numeric steps'
out={'scope':'Windows actual UIA focus + official keys; numeric state/box; not full paint/clipboard acceptance','checks':len(rows),'passed':all(not r['differences'] for r in rows),'rows':rows}
(root/'numberinput-windows-actual-comparison.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(f"Windows actual numeric steps: {sum(not r['differences'] for r in rows)}/{len(rows)} agree.")
if not out['passed']:raise SystemExit(1)
