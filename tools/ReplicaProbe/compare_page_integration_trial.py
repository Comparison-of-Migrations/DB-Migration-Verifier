from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);p.add_argument('--actual',action='store_true');p.add_argument('--input',action='store_true');p.add_argument('--changed',action='store_true');p.add_argument('--boundary-input',action='store_true');p.add_argument('--neutral-alias',action='store_true');a=p.parse_args()
s={} if a.input else json.loads(a.source.read_text());d=[];count=0;pending=[]

def compare(expected,actual,path):
    if isinstance(expected,dict):
        for k,v in expected.items():
            if k not in actual:d.append(dict(field=path+'.'+k,source=v,candidate='MISSING'))
            else:compare(v,actual[k],path+'.'+k)
    elif expected!=actual:d.append(dict(field=path,source=expected,candidate=actual))
if a.input:
    names=['bom-valid','nel-valid','bom-host','nel-host'] if a.boundary_input else ['disabled','inputs-priority','invalid-empty','host-empty','db-empty','same-source','same-target','exact-whitespace','success','stale-success','edit-reset','consent-reset','return-reopen','language','quiet-back']
    for lang in ('ko','en'):
        for name in names:
            identifier=lang+'-'+name;sp=a.source/(identifier+'-source.json');cp=a.candidate/(identifier+'-native.json')
            if not sp.exists() or not cp.exists():d.append({'field':identifier,'candidate':'MISSING actual file'});continue
            expected=json.loads(sp.read_text());actual=json.loads(cp.read_text());e=expected['state'];v=actual['state']
            fields=['screen','english','host','database','validInput','invalidInput','consent','ready','error']
            if 'announcement' in e and e['screen']=='trial':fields+=['announcement']
            if e['screen']=='trial':fields+=['primary','focus']
            else:pending.append({'id':identifier,'destination':e['screen'],'scope':'Unimplemented Selection footer/H1 remains unverified','source':{k:e[k] for k in ('primary','focus','announcement')},'candidate':{k:v[k] for k in ('primary','focus','announcement')}})
            for k in fields:
                actualValue=v[k]
                if k=='focus' and a.neutral_alias and actualValue=='' and actual.get('rawFocus') is None and not any(c['focused'] for c in actual.get('controls',[])):actualValue='BODY'
                compare(e[k],actualValue,identifier+'.'+k)
            if name=='edit-reset':
                for k in ('pageRetained','editorRetained','primaryRetained','readyNotice'):compare(expected[k],actual[k],identifier+'.'+k)
            count+=1
if not a.input and (a.candidate/'pageintegration-trial-layout.json').exists():
    got={r['id']:r for r in json.loads((a.candidate/'pageintegration-trial-layout.json').read_text())['rows']}
    for row in s['rows']:
        if row['id'] not in got:d.append({'field':row['id'],'candidate':'MISSING'})
        else:compare(row,got[row['id']],row['id'])
        count+=1
if not a.input and (a.candidate/('pageintegration-trial-validation-flows.json' if a.changed else 'pageintegration-trial-flows.json')).exists():
    got={r['id']:r for r in json.loads((a.candidate/('pageintegration-trial-validation-flows.json' if a.changed else 'pageintegration-trial-flows.json')).read_text())['flows']}
    for row in s['flows']:
        r=got.get(row['id'])
        if r is None:d.append({'field':row['id'],'candidate':'MISSING'});continue
        r={**r,**(r.get('extra') or {})};r.pop('extra',None)
        compare(row,r,row['id']);count+=1
checks=a.candidate/('pageintegration-trial-changed-checks.json' if a.changed else 'pageintegration-trial-checks.json')
if checks.exists():
    for check in json.loads(checks.read_text()):
        if not check['pass']:d.append({'field':check['id'],'candidate':'FAILED assertion'})
if not count:d.append({'field':'recordedScope','candidate':'NO RESULTS'})
result={'phase':('ACTUAL_OS_PASSED' if a.actual else 'HEADLESS_PASSED') if not d else 'FAILED','scope':'New connected Trial parent only; source synthetic flow oracle does not replace actual OS input','cases':count,'source':str(a.source.resolve()),'candidate':str(a.candidate.resolve()),'differences':d,'unverifiedDestinations':pending}
a.output.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n');print(json.dumps({'phase':result['phase'],'cases':count,'differences':len(d),'first':d[:10]},ensure_ascii=False));raise SystemExit(bool(d))
