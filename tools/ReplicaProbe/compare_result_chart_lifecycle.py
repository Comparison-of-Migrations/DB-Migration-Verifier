from pathlib import Path
import json
r=Path(__file__).resolve().parents[2]/'docs/replica/source';errors=[]
def check(platform,phase,key,ok):
 if not ok:errors.append(dict(platform=platform,phase=phase,property=key))
for platform in ['mac','windows']:
 source=json.loads((r/f'resultchart-{platform}-source-lifecycle.json').read_text());native=json.loads((r/f'resultchart-{platform}-native/resultchart-lifecycle.json' if platform=='mac'else r/f'resultchart-{platform}-lifecycle.json').read_text());check(platform,'all','dpr',source['dpr']==native['scale']==2)
 check(platform,'all','phasecount',len(source['phases'])==9 and len(native['phases'])==15)
 s={p['phase']:p for p in source['phases']};n={p['phase']:p for p in native['phases']}
 def counts(p):return[q for row in p['rows']for q in row['counts']]
 for phase in ['mount','start','running','completed','ordinary-rerender','empty','trial-only','trial-excluded','scale-update']:
  x,y=s[phase],n[phase];check(platform,phase,'present',x['present']==y['present']);check(platform,phase,'rows',x['rows']==len(y['rows']));check(platform,phase,'counts',[q['count']for q in x['counts']]==[q['count']for q in counts(y)])
  if x['present']:check(platform,phase,'maximum',int(x['unit'].split('–')[-1])==y['maximum'])
 check(platform,'start','source animations',len(s['start']['animations'])==24 and sorted(a['duration']for a in s['start']['animations'])==[220]*12+[320]*12 and all(a['easing']=='cubic-bezier(0.22, 1, 0.36, 1)'for a in s['start']['animations']))
 check(platform,'start','native number/bar',all(q['opacity']==.6 and q['y']==5 and q['barScale']==0 for q in counts(n['start'])))
 for phase in ['running']:
  check(platform,phase,'source active',all(0.6<q['opacity']<=1 for q in s[phase]['counts']) and len(s[phase]['animations'])>0)
  check(platform,phase,'native active',all(.6<q['opacity']<=1 and 0<q['barScale']<1 for q in counts(n[phase])) and n[phase]['status']=='WaitingForActivation')
 for phase in ['mount','completed','ordinary-rerender','trial-excluded','scale-update']:
  check(platform,phase,'source no entrance',not s[phase]['animations'] and all(q['opacity']==1 and q['number']=='none' and q['bar']=='none'for q in s[phase]['counts']))
 for phase in ['mount','completed','ordinary-rerender','reduced','cancelled','replaced','detached','reattached','replayed','trial-excluded','scale-update']:
  check(platform,phase,'native settled',all(q['opacity']==1 and q['y']==0 and q['barScale']==1 for q in counts(n[phase])))
 for phase in ['cancelled','replaced','detached']:check(platform,phase,'cancel status',n[phase]['status']=='Canceled')
 check(platform,'replaced','old released',all(not q['attached'] and q['opacity']==1 and q['y']==0 and q['barScale']==1 for q in n['replaced']['old']))
 check(platform,'detached','detached',not n['detached']['attached']);check(platform,'reattached','attached',n['reattached']['attached'])
 check(platform,'ordinary-rerender','English replacement',n['ordinary-rerender']['heading']=='Comparison results by category')
 for phase in ['completed','reduced','replayed']:check(platform,phase,'completed status',n[phase]['status']=='RanToCompletion')
 input=json.loads((r/f'resultchart-{platform}-input.json').read_text());check(platform,'input','four detail keys',input['requests']==['data','schema','access','objects'])
report=dict(sourcePhases=18,nativePhases=30,actualRequests=8,failed=len(errors),errors=errors,scope='Parent composition; no ordinary rerender entrance, shared axis and trial exclusion, attached entrance/lifetime/reduced/cancel/removal/replacement. Running samples use actual frame clocks; exact curves/timing inherited locked Count and Row contracts. Modal body and return-focus owned ResultDetails/Results.');(r/'resultchart-lifecycle-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items()if k not in ['errors','scope']});print(errors);assert not errors
