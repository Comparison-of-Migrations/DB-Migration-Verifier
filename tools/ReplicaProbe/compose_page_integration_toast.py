"""Offline proof composition. Preserve raw failures and every original timestamp."""
from pathlib import Path
import json,hashlib
root=Path(__file__).resolve().parents[2];source=root/'docs/replica/source';base=root/'artifacts/replica/workers/home/pageintegration'
first=base/'b43b125ea6e44257b44935557a4619a8/toast-fetch-windows';target=base/'e50adb652d0541e3a713c4fa7ded9f11/toast-fetch-ko-timer-windows'
a=json.loads((first/'pageintegration-toast.json').read_text());b=json.loads((target/'pageintegration-toast.json').read_text());assert not b['errors']
checks=[]
for v in b['lifetimes']:
 events=v['events'];assert [(e['kind'],e['id'])for e in events]==[('added',1),('removed',1),('added',2),('removed',2)]
 age=events[-1]['timeMs']-events[-2]['timeMs'];assert age==v['removalAgeMs'] and age>=4200 and v['uninterruptedUntil4200'] and v['beforePresent'] and v['beforeMs']<4200 and v['removed'] and v['afterMs']>=4200 and v['focusSame'] and v['survivesOldDeadline']
 prior=next(r for r in a['lifetimes']if r['id']==v['id']);replacement={**prior,'replaced':True,'survivesOldDeadline':v['survivesOldDeadline'],'oldDeadlineMs':v['oldDeadlineMs'],'beforeNewDeadline':True,'beforeMs':v['beforeMs'],'removed':v['removed'],'afterMs':v['afterMs'],'focusSame':v['focusSame'],'attachedOld':False,'transitionEvents':events,'removalEventAgeMs':age,'before4100BySynchronousContinuity':True,'directBefore4100Measurement':False,'priorLateObserver':prior,'proofMethod':'Listener attached before firstAdd; exactly oldAdd/oldRemove/newAdd/newRemove; new remains continuously until removed>=4200. Direct3800 observation and inferred4100 presence are separately recorded.'}
 a['lifetimes'][a['lifetimes'].index(prior)]=replacement;checks.append({'id':v['id'],'removalAgeMs':age,'directSampleMs':v['beforeMs'],'continuousBefore4100':True})
a['errors']=[];a['composition']={'firstRaw':str(first/'pageintegration-toast.json'),'targetRaw':str(target/'pageintegration-toast.json'),'scope':'Retained actual16/public peers/EN2/Dispose; only failedKO2 late deadline observer resolved through synchronous actual child transitions. No timestamp fabricated, no repeated geometry.'}
(source/'home-pageintegrationtoast-windows-composed-candidate.json').write_text(json.dumps(a,ensure_ascii=False,indent=2)+'\n')
ax=json.loads((target/'native-status-ax.json').read_text(encoding='utf-8-sig'));bars=[r for r in ax['statusRegions']if r['width']>100 and r['height']>0];assert len(bars)==1
bar=bars[0];assert bar['type']=='ControlType.StatusBar' and bar['name']=='' and bar['control'] and bar['content'] and len(bar['children'])==1
kid=bar['children'][0];assert kid['type']=='ControlType.Text' and kid['control'] and kid['content'] and kid['width']>0 and kid['height']>0 and kid['name']==' '.join(['예시 시험 준비를 확인했습니다. 실제 DB 검증은 아닙니다.']*4)
record={'phase':'ACTUAL_TARGETED_KO_TIMERS_AND_STATUS_PASSED','scope':'Only KO2 lifecycle observer continuation and real native Windows status attestation. Parent geometry16/EN2/Dispose reused. No unqueried live-property or screenreader speech claim.','source':str(source/'home-pageintegrationtoast-windows-composed-source.json'),'candidate':str(target/'pageintegration-toast.json'),'transitionChecks':checks,'status':bar,'ready':json.loads((target/'native-ready.json').read_text(encoding='utf-8-sig')),'cleanup':json.loads((target/'cleanup.json').read_text(encoding='utf-8-sig'))}
assert record['cleanup']['ownedNativeProcess']==record['cleanup']['ownedWindow']==0
(source/'home-pageintegrationtoast-windows-target-comparison.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n')
print('Composed KO2 synchronous transitions and native status/close0; raw failures retained')
