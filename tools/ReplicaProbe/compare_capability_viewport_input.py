"""Actual original/native finite input comparison with explicit platform focus mapping."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('output',type=Path);p.add_argument('--lifecycle',action='store_true');p.add_argument('--drag',action='store_true');p.add_argument('--focus-only',action='store_true');a=p.parse_args()
labels=['right','left','down','up','page-down','page-up','home','end','space','shift-space','wheel-x','wheel-y','wheel-shift-y']
if a.focus_only:labels=['pointer-restore','tab-viewport','keyboard-restore','tab-child']
if a.lifecycle:labels+=['fit-no-overflow','fit','child-focus','mount-detached','mount']
if a.drag:labels+=['drag']
diffs=[];checks=[]
for label in labels:
 s=json.loads((a.folder/f'source-{label}.json').read_text());n=json.loads((a.folder/f'native-{label}.json').read_text())
 fields=['dpr','offset','attached','focusVisible']
 if s['attached']:fields+=['width','height','clientWidth','clientHeight','scrollWidth','scrollHeight']
 for key in fields:
  if s[key]!=n[key]:diffs.append({'label':label,'field':key,'source':s[key],'native':n[key]})
 # Browser BODY focus after body click/child removal maps to Avalonia's internal
 # pointer routing focus or null. Named descendant focus must match literally.
 active=s.get('active');target=n.get('active')
 if (active=='BODY'and target not in ['', 'CapabilityViewport-box'])or(active!='BODY'and active!=target):diffs.append({'label':label,'field':'active','source':active,'native':target})
 checks.append({'label':label,'offset':n['offset'],'activeSource':active,'activeNative':target,'attached':n['attached']})
v={'count':len(checks),'differences':diffs,'checks':checks,'detachedBounds':'DOM zero vs cached native Bounds not rendered; both detached/offset0/no named focus','scope':'actual endpoint/state/focus; timing path and held behavior separate'};a.output.write_text(json.dumps(v,indent=2)+'\n');print(len(checks),'checks',len(diffs),'differences');raise SystemExit(bool(diffs))
