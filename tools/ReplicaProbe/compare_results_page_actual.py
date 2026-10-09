"""Actual guarded Windows pointer/keyboard callbacks, replacement focus and safe text."""
from pathlib import Path
import json,sys
folder=Path(sys.argv[1]);output=Path(sys.argv[2]);errors=[];checks=[]
def check(ok,label,actual=None):
 checks.append(label)
 if not ok:errors.append({'path':label,'actual':actual})
steps=[('filter-unknown-click','filter:unknown','unknown',['U-001'],False),('current-space','filter:unknown','unknown',['U-001'],True),('current-enter','filter:unknown','unknown',['U-001'],True),('filter-different-click','filter:different','different',['F-001','F-002','F-003'],False),('research-enter','research','different',None,True),('data-space','detail:data','different',None,True),('trial-space','detail:trial','different',None,True),('trial-hover',None,'different',None,None),('trial-leave',None,'different',None,None),('trial-reverse',None,'different',None,None),('trial-cancel',None,'different',None,None),('trial-hold','detail:trial','different',None,False)]
requests=[];summary=[]
for name,request,filter_key,ids,keyboard in steps:
 p=folder/('input-'+name+'.json');check(p.exists(),name+':freshRecord')
 if not p.exists():continue
 d=json.loads(p.read_text());last=d['history'][-1]
 if request:requests.append(request)
 check(d['requests']==requests,name+':callbacks',d['requests'])
 check(last['filter']==filter_key,name+':filter',last['filter'])
 if ids is not None:check(last['articleIds']==ids,name+':safeArticles',last['articleIds'])
 if request and keyboard is not None:check(last['keyboard']==keyboard,name+':inputKind',last['keyboard'])
 if request and request.startswith('filter:'):check(last['focusId']=='Results-filter-'+filter_key,name+':replacementFocus',last['focusId'])
 if name=='trial-space':check(last['trialFocused'] is True and last['trialFocusVisible'] is True,name+':keyboardFocus',last)
 if name=='trial-hold':check(last['trialFocused'] is True and last['trialFocusVisible'] is False,name+':pointerFocus',last)
 summary.append({'step':name,'requests':d['requests'],'state':last})
r={'status':'FAILED'if errors else'PASSED','checks':checks,'errors':errors,'steps':summary,'scope':'Fresh actual SendInput; finite source data/filter/language oracle and literal original callbacks reused; Trial trace comparison separately required'};output.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'status':r['status'],'checks':len(checks),'errors':errors},ensure_ascii=False));raise SystemExit(bool(errors))
