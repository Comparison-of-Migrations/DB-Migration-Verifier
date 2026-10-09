"""Compare independently recorded actual actions; rail/trial/navigation belongs to App."""
from pathlib import Path
import argparse,json,sys
r=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser();parser.add_argument('os');parser.add_argument('labels',nargs='*');parser.add_argument('--native-dir',type=Path);parser.add_argument('--source-os');args=parser.parse_args();os=args.os;source_os=args.source_os or os;native_dir=args.native_dir or r/f'docs/replica/source/connectionpage-{os}-input'
labels=args.labels or ['engine','advanced-key','required','error-recovery','busy-input','connected-input','language','lifecycle-normal','lifecycle-reduced','busy-invalid']
errors=[];checks=[]
for label in labels:
 evidence=json.loads((r/f'docs/replica/source/connectionpage-{source_os}-source-input/{label}.json').read_text())
 s=evidence['last'];n=json.loads((native_dir/f'{label}.json').read_text());v=n['view']
 expected={key:s[key]for key in ['english','dbms','source','target','connected','busy','advanced','error','footer']}
 endpoint=lambda d:dict(host=d['Host'],port=d['Port'],db=d['Database'],user=d['Reader'])
 actual={'english':v['English'],'dbms':v['Dbms'],'source':endpoint(v['Source']),'target':endpoint(v['Target']),'connected':v['Connected'],'busy':v['Busy'],'advanced':v['Advanced'],'error':v['Error'],'footer':n['footer']}
 checks.append(label)
 if expected!=actual:errors.append({'label':label,'source':expected,'native':actual})
 for key in ['status','success']:
  if key in n and s[key]!=n[key]:errors.append({'label':label,'key':key,'source':s[key],'native':n[key]})
 if label in ['busy-input','busy-invalid','connected-input','error-recovery']:
  rows=[x for x in n['history']if x['eventName']=='input']
  if not rows:errors.append({'label':label,'missing':'native input'})
  elif not all(rows[-1]['same'][key]for key in ['page','source','advanced','footer']):errors.append({'label':label,'partialUpdateNative':rows[-1]['same']})
 if label in ['busy-input','busy-invalid']:
  rows=[x for x in n['history']if x['eventName']=='input'and x['view']['Busy']]
  source_rows=[x for x in evidence['events']if x.get('event')=='input'and x.get('busy')]
  checking={'action':'connect','enabled':False,'label':'Checking…'if v['English']else'확인 중…','loading':True}
  for kind,items in [('source',source_rows),('native',rows)]:
   if not items:errors.append({'label':label,'missing':kind+' busy input'})
   elif items[-1]['footer']!=checking or not all(items[-1]['same'][key]for key in ['page','source','advanced','footer']):errors.append({'label':label,'busyInput'+kind:items[-1]})
  busy=[x for x in n['history']if x['eventName']=='busy'];complete=[x for x in n['history']if x['eventName']=='connected']
  if busy and complete:
   elapsed=complete[-1]['time']-busy[-1]['time'];checks.append({'label':label,'busyElapsedMs':elapsed})
   if elapsed<549:errors.append({'label':label,'earlyCompletionMs':elapsed})
  else:errors.append({'label':label,'missing':'busy/completion'})
 if label.startswith('lifecycle-'):
  mount=[x for x in n['history']if x['eventName']=='mount'];unmount=[x for x in n['history']if x['eventName']=='unmount']
  if not mount or not unmount:errors.append({'label':label,'missing':'actual unmount/mount'})
  else:
   row=mount[-1];opacity=1 if label.endswith('reduced')else .35
   if row['attached']is not True or unmount[-1]['attached']is not False or abs(row['opacity']-opacity)>.001:errors.append({'label':label,'mount':row})
   if not all(row['same'][key]for key in ['page','source','advanced','footer']):errors.append({'label':label,'mountReferences':row['same']})
  if not n['attached']:errors.append({'label':label,'notAttached':True})
 if label=='engine':
  radios=n['history'][-1]['radioFocus'];selected=next(x for x in radios if x['EngineName']==v['Dbms'])
  if selected['visible']!=s['focus']['visible']:errors.append({'label':label,'sourceFocusVisible':s['focus']['visible'],'nativeRadio':selected})
 if label=='advanced-key'and n['history'][-1]['advancedFocusVisible']!=s['focus']['visible']:errors.append({'label':label,'sourceFocusVisible':s['focus']['visible'],'nativeAdvancedFocusVisible':n['history'][-1]['advancedFocusVisible']})
 focus={'engine':'dbms-1','advanced-key':'advanced','required':'Footer-connect','error-recovery':'source-host','connected-input':'source-db','busy-input':'Footer-preflight','busy-invalid':'Footer-preflight'}.get(label)
 if focus and (n['focus']or{}).get('id')!=focus:errors.append({'label':label,'expectedFocus':focus,'nativeFocus':n['focus']})
output={'os':os,'checks':checks,'errors':errors};
if os=='headless':output['phase']='HEADLESS_FAILED'if errors else'HEADLESS_PASSED'
(r/f'docs/replica/source/connectionpage-{os}-input-comparison.json').write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(output,ensure_ascii=False,indent=2));sys.exit(bool(errors))
