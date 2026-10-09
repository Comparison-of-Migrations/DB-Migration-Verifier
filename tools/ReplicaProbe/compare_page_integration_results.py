"""Strict new Results App wiring comparison; no child matrix replay."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);p.add_argument('--marker-layout',action='store_true',help='Only newly changed hidden status text line behavior; source ink/native controlboxes recorded separately');p.add_argument('--boundary-peers',action='store_true',help='Only new marker peers2; prior modal6 headless retained');p.add_argument('--boundary',action='store_true',help='Narrow source-derived marker+modal6 after parent-only correction');p.add_argument('--modal-boxes',action='store_true',help='Only actual native root modalBox6 vs retained source6');p.add_argument('--peer',action='store_true',help='Exact actualWindows announcer Control/Content subtree; source provider fields only');p.add_argument('--entry',action='store_true',help='Only narrow public entry property trace4 after late observer diagnostic');p.add_argument('--actual',action='store_true',help='Only new trustedOS parent callback source/native checks');p.add_argument('--semantic-only',action='store_true',help='DOM state/focus/report/export only; OS geometry requires separate sameOS source4 compare');p.add_argument('--layout',action='store_true',help='Only new actualOS App parent4 allocations');p.add_argument('--wrapper',action='store_true',help='Compare corrected literal wrapper4 geometry against preserved prior source68');a=p.parse_args()
source=json.loads(a.source.read_text());native=json.loads(a.candidate.read_text());errors=[];checks=0
def check(case,label,key,actual,expected):
 global checks
 checks+=1
 if actual!=expected:errors.append(dict(case=case,label=label,key=key,actual=actual,expected=expected))
def box(case,label,key,n,s):
 global checks
 if n is None or s is None:check(case,label,key,n,s);return
 for k in ['x','y','width','height']:
  checks+=1
  if abs(n[k]-s[k])>.01:errors.append(dict(case=case,label=label,key=key+'.'+k,actual=n[k],expected=s[k]))
if a.marker_layout:
 check('marker','all','contexts',[r['id'] for r in native],[r['id'] for r in source['rows']])
 expressions=[]
 for sr,nr in zip(source['rows'],native):
  case=sr['id']
  for key in ['text','lineTexts','fontSize']:check(case,'layout',key,nr[key],sr[key])
  check(case,'layout','line count',len(nr['lineTexts']),len(sr['lineTexts']))
  check(case,'layout','source normal whitespace',sr['whiteSpace'],'normal');check(case,'layout','source normal overflowWrap',sr['overflowWrap'],'normal');check(case,'layout','candidate CSS overflow wrap',nr['textWrapping'],'WrapWithOverflow')
  check(case,'layout','candidate rounded CSS lineHeight',nr['lineHeight'],int(sr['lineHeight']*source['dpr']*64+.5)/(source['dpr']*64))
  for key in ['width','height']:check(case,'layout','clipped parent '+key,nr['parentBox'][key],sr['parentBox'][key])
  for index,(rect,width) in enumerate(zip(sr['lineRects'],nr['lineWidths'])):
   checks+=1
   if abs(rect['width']-width)>.01:errors.append(dict(case=case,label='layout',key='lineAdvance.'+str(index),actual=width,expected=rect['width']))
  check(case,'layout','source CSS line origins match native lineHeight',all(abs((b['y']-a['y'])-nr['lineHeight'])<.01 for a,b in zip(sr['lineRects'],sr['lineRects'][1:])),True)
  expressions.append(dict(id=case,sourceInlineInkRects=sr['lineRects'],candidateLineWidths=nr['lineWidths'],candidateArrangedControlBox=nr['controlBox'],sourceFontFamily=sr['fontFamily'],candidateFontFamily=nr['fontFamily'],expression='Actual source DOMRange inline ink rectangles versus Avalonia arranged control; no pixel equivalence or OS waiver inferred. Line texts/counts and CSS parent/lineHeight compare separately.'))
 result=dict(status='PASSED' if not errors else 'FAILED',checks=checks,differences=errors,representations=expressions,source=str(a.source.resolve()),candidate=str(a.candidate.resolve()),scope='Only new invisible status text layout KOEN; source APIsetup not trusted callback replay; original role44/modal6/parent4/callback30 retained. Actual native traversal and immediateclose evidence separate.')
 a.output.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n');print(result['status'],checks,'checks',len(errors),'differences');raise SystemExit(0 if not errors else 1)
if a.boundary or a.boundary_peers or a.modal_boxes:
 source_rows=source['rows'];expected=[row for context in source_rows for row in context['modalRows']]
 if not a.boundary_peers:
  actual=[row for context in native for row in context['modalRows']] if a.boundary else native
  check('boundary','all','modal labels',[r['id'] for r in actual],[r['id'] for r in expected])
  for sr,nr in zip(expected,actual):box('boundary',sr['id'],'modalBox',nr['modalBox'],sr['modalBox'])
 if a.boundary or a.boundary_peers:
  check('boundary','all','contexts',[r['id'] for r in native],[r['id'] for r in source_rows])
  for sr,nr in zip(source_rows,native):
   check(sr['id'],'peer','announcement',nr['announcement'],sr['announcement']);check(sr['id'],'peer','DOM polite source contract',nr['live'],'Polite');check(sr['id'],'peer','node count',len(nr['peerNodes']),len(sr['peerNodes']))
   for index,(sn,nn) in enumerate(zip(sr['peerNodes'],nr['peerNodes'])):
    for key in ['name','type','control','content','children']:check(sr['id'],str(index),key,nn[key],sn[key])
 result=dict(status='PASSED' if not errors else 'FAILED',checks=checks,differences=errors,source=str(a.source.resolve()),candidate=str(a.candidate.resolve()),scope='New root marker/modal geometry only; APIsetup not actual callback proof, passed native30/parent4 retained; actual UIA peer separately required')
 a.output.write_text(json.dumps(result,indent=2)+'\n');print(result['status'],checks,'checks',len(errors),'differences');raise SystemExit(0 if not errors else 1)
if a.peer:
 check('peer','all','announcement',native['announcement'],source['announcement'])
 check('peer','all','views',[v['view'] for v in native['views']],[v['view'] for v in source['views']])
 for sv,nv in zip(source['views'],native['views']):
  view=sv['view'];check('peer',view,'source complete bounded tree',sv['truncated'],False);check('peer',view,'candidate complete bounded tree',nv['truncated'],False)
  check('peer',view,'node count',len(nv['nodes']),len(sv['nodes']))
  for index,(sn,nn) in enumerate(zip(sv['nodes'],nv['nodes'])):
   for key in ['id','name','type','control','content','focusable','focused','value','children']:check(view,str(index),key,nn[key],sn[key])
 result=dict(status='PASSED' if not errors else 'FAILED',checks=checks,differences=errors,source=str(a.source.resolve()),candidate=str(a.candidate.resolve()),scope='Actual Windows source/native bounded announcer Control/Content exactrole/name/child structure/focus/value; default text raster/overflow boxes and unsupported source ARIA/LiveSetting provider properties recorded separately, no implicit exemptions')
 a.output.write_text(json.dumps(result,indent=2)+'\n');print(result['status'],checks,'checks',len(errors),'differences');raise SystemExit(0 if not errors else 1)
if a.entry:
 source_cases={c['id']:c for c in source['rows']};native_cases={c['id']:c for c in native}
 check('all','entry','context ids',sorted(native_cases),sorted(source_cases))
 for case,c in source_cases.items():
  trace=native_cases.get(case)
  if trace is None:continue
  original=next(r for r in c['rows'] if r['label']=='sample-go-start')
  changes=[r for r in trace['samples'] if r['stage']=='opacity-changed' and r['current']]
  check(case,'entry','live current-page changes recorded',bool(changes),True)
  if changes:
   check(case,'entry','initial opacity',changes[0]['opacity'],original['pageOpacity'])
   check(case,'entry','terminal opacity',changes[-1]['opacity'],1)
   check(case,'entry','opacity monotonic',all(a['opacity']<=b['opacity'] for a,b in zip(changes,changes[1:])),True)
  settled=next((r for r in trace['samples'] if r['stage']=='settled'),None)
  check(case,'entry','settled current page',settled is not None and settled['current'] and settled['opacity']==1,True)
 result=dict(status='PASSED' if not errors else 'FAILED',checks=checks,differences=errors,source=str(a.source.resolve()),candidate=str(a.candidate.resolve()),scope='Narrow initial public property observation4 only; full68 late observer diagnostic retained; no timing exemption or full App/geometry/actualOS pass inferred')
 a.output.write_text(json.dumps(result,indent=2)+'\n');print(result['status'],checks,'checks',len(errors),'differences');raise SystemExit(0 if not errors else 1)
if a.actual:
 sr={r['id']:r for r in source};nr={r['id']:r for r in native};unobserved=[]
 check('actual','all','callback labels',list(nr),list(sr))
 for label,sr_row in sr.items():
  xrow=nr.get(label)
  if xrow is None:continue
  check('actual',label,'source helper assertion',sr_row['passed'],True);check('actual',label,'candidate helper assertion',xrow['passed'],True)
  s=sr_row['state'];x=xrow['state']
  for key in ['screen','english','announcement','focus','scroll','title','articleIds','backgroundEnabled','report']:check('actual',label,key,x[key],s[key])
  check('actual',label,'visited',sorted(x['visited']),sorted(s['visited']))
  if s['screen']=='results':check('actual',label,'filter',x['filter'],s['filter'])
  if x.get('focusVisible') is not None:check('actual',label,'public focus-visible',x['focusVisible'],s['focusVisible'])
  else:unobserved.append(dict(label=label,property='focus-visible',reason='Generic locked ActionButton lacks public getter; new caller-ring paint must be inspected, no private reflection'))
  if s['modal'] is None:check('actual',label,'modal',x['modal'],None)
  elif x['modal'] is None:check('actual',label,'modal present',False,True)
  else:
   for key in ['type','payload','title','checks','notices','open']:check('actual',label,'modal.'+key,x['modal'][key],s['modal'][key])
   check('actual',label,'modal-only peer',x['peerChildren'],1)
   if 'modalBox' in s and 'modalBox' in x:box('actual',label,'modalBox',x['modalBox'],s['modalBox'])
 result=dict(status='PASSED' if not errors else 'FAILED',checks=checks,differences=errors,unobserved=unobserved,source=str(a.source.resolve()),candidate=str(a.candidate.resolve()),scope='New trustedOS realApp parent callbacks/safe findings/modalscope/focus/scroll; offset keys setup only; actual caller-ring paint and exactcleanup evidence separate')
 a.output.write_text(json.dumps(result,indent=2)+'\n');print(result['status'],checks,'checks',len(errors),'differences');raise SystemExit(0 if not errors else 1)
if a.layout:
 source_cases={c['id']:c for c in source['rows']}
 check('all','actual-layout','context ids',sorted(c['id'] for c in native['rows']),sorted(source_cases))
 for n in native['rows']:
  case=n['id'];c=source_cases.get(case)
  if c is None:continue
  if 'boxes' not in n:n={**next(r for r in n['rows'] if r['label']=='sample-go'),'id':case,'viewport':n['viewport']}
  s=next(r for r in c['rows'] if r['label']=='sample-go');check(case,'actual-layout','viewport',n['viewport'],c['viewport'])
  for key in ['focus','scroll','announcement','title','articleIds','pageOpacity']:check(case,'actual-layout',key,n[key],s[key])
  for key in s['boxes']:box(case,'actual-layout','boxes.'+key,n['boxes'][key],s['boxes'][key])
  check(case,'actual-layout','footer length',len(n['footer']),len(s['footer']))
  for i,(nb,sb)in enumerate(zip(n['footer'],s['footer'])):
   for key in ['action','text','disabled']:check(case,'actual-layout','footer.'+str(i)+'.'+key,nb[key],sb[key])
   box(case,'actual-layout','footer.'+str(i),nb['box'],sb['box'])
 result=dict(status='PASSED' if not errors else 'FAILED',checks=checks,differences=errors,source=str(a.source.resolve()),candidate=str(a.candidate.resolve()),scope='New actualOS App parent4 allocations/focus/scroll/footer; passive API setup is not trusted input, protected child matrices reused')
 a.output.write_text(json.dumps(result,indent=2)+'\n');print(result['status'],checks,'checks',len(errors),'differences');raise SystemExit(0 if not errors else 1)
if a.wrapper:
 old_cases={c['id']:c for c in source['rows']};roots=[]
 check('all','wrapper','context ids',sorted(c['id'] for c in native['rows']),sorted(old_cases))
 for c in native['rows']:
  case=c['id'];old=old_cases.get(case)
  if old is None:continue
  check(case,'wrapper','viewport',c['viewport'],old['viewport'])
  check(case,'wrapper','single new geometry state',len(c['rows']),1)
  if not c['rows']:continue
  x=c['rows'][0];original=next(r for r in old['rows'] if r['label']=='sample-go')
  for key in ['label','screen','english','focus','scroll','title','articleIds','pageOpacity','report']:check(case,'wrapper',key,x[key],original[key])
  for key in original['boxes']:box(case,'wrapper','boxes.'+key,x['boxes'][key],original['boxes'][key])
  check(case,'wrapper','footer length',len(x['footer']),len(original['footer']))
  for i,(nb,sb) in enumerate(zip(x['footer'],original['footer'])):
   for key in ['action','text','disabled']:check(case,'wrapper','footer.'+str(i)+'.'+key,nb[key],sb[key])
   box(case,'wrapper','footer.'+str(i),nb['box'],sb['box'])
  announcer=x['root']['announcer'];check(case,'wrapper','announcer class',announcer['className'],'sr');check(case,'wrapper','announcer role',announcer['role'],'status');check(case,'wrapper','announcer position',announcer['position'],'absolute')
  for key in ['width','height']:check(case,'wrapper','announcer '+key,announcer['box'][key],1)
  roots.append(dict(id=case,root=x['root']))
 result=dict(status='PASSED' if not errors else 'FAILED',checks=checks,differences=errors,roots=roots,source=str(a.source.resolve()),candidate=str(a.candidate.resolve()),scope='Corrected literal original hidden announcer/new root dimensions and parent4 allocation; unchanged68 semantic/report/export evidence retained, no candidate integration pass')
 a.output.write_text(json.dumps(result,indent=2)+'\n');print(result['status'],checks,'checks',len(errors),'differences');raise SystemExit(0 if not errors else 1)
native_cases={c['id']:c for c in native['rows']}
for c in source['rows']:
 case=c['id'];n=native_cases.get(case)
 check(case,'context','present',n is not None,True)
 if n is None:continue
 check(case,'context','viewport',n['viewport'],c['viewport'])
 sr={r['label']:r for r in c['rows']};nr={r['label']:r for r in n['rows']}
 check(case,'context','state labels',list(nr),list(sr))
 for label,s in sr.items():
  x=nr.get(label)
  if x is None:continue
  for key in ['screen','english','announcement','focus','articleIds','title']:
   check(case,label,key,x[key],s[key])
  check(case,label,'visited',sorted(x['visited']),sorted(s['visited']))
  if s['screen']=='results':check(case,label,'filter',x['filter'],s['filter'])
  if label!='sample-go-start':
   check(case,label,'main scroll',x['scroll'],s['scroll'])
   if not a.semantic_only:
    for key in s['boxes']:box(case,label,'boxes.'+key,x['boxes'][key],s['boxes'][key])
  check(case,label,'full unfiltered safe report',x['report'],s['report'])
  if label=='sample-go-start':
   check(case,label,'normal entry created',x['pageOpacity']<1,False if c['reduced'] else s['pageOpacity']<1)
  elif s['screen']=='results':check(case,label,'settled opacity',x['pageOpacity'],s['pageOpacity'])
  if s['modal'] is None:check(case,label,'modal',x['modal'],None)
  elif x['modal'] is None:check(case,label,'modal present',False,True)
  else:
   for key in ['type','payload','title','checks','notices','open']:check(case,label,'modal.'+key,x['modal'][key],s['modal'][key])
   check(case,label,'background blocked',x['backgroundEnabled'],False)
   check(case,label,'modal-only peer',x['peerChildren'],1)
  check(case,label,'footer length',len(x['footer']),len(s['footer']))
  for i,(nb,sb)in enumerate(zip(x['footer'],s['footer'])):
   for key in ['action','text','disabled']:check(case,label,'footer.'+str(i)+'.'+key,nb[key],sb[key])
   if label!='sample-go-start' and not a.semantic_only:box(case,label,'footer.'+str(i),nb['box'],sb['box'])
 check(case,'exports','count',len(n['exports']),len(c['exports']))
 for nb,sb in zip(n['exports'],c['exports']):
  check(case,'exports','name',nb['name'],sb['name'])
  if sb['name'].endswith('.json'):check(case,'exports','JSON complete adapter',json.loads(nb['body']),json.loads(sb['body']))
  else:check(case,'exports',sb['name'].rsplit('.',1)[-1]+' exact bytes',nb['body'],sb['body'])
result=dict(status='PASSED'if not errors else'FAILED',sourceCases=len(source['rows']),candidateCases=len(native['rows']),checks=checks,differences=errors,source=str(a.source.resolve()),candidate=str(a.candidate.resolve()),scope='New realApp parent Results go/filter/modal/restore/report adapter; locked child matrices reused',geometryCompared=not a.semantic_only)
a.output.write_text(json.dumps(result,indent=2)+'\n');print(result['status'],checks,'checks',len(errors),'differences');raise SystemExit(0 if not errors else 1)
