"""Strict common physical milestones; optional separately evidenced live interruption.

Physical scroll offsets and omitted filter/detail route differ from the handler
fixture. Live counts are validated/preserved, never replaced with fixture counts.
This checks saved evidence; it does not generate an OS input proof.
"""
import argparse
import copy
import json
from pathlib import Path
import re

FIELDS = ('screen english connected busy error categories tables maps expanded trial '
          'stopped visited formats saved modal query title footer focus announcement').split()
COMMON = ('home connect invalid-connect connected preflight permissions permissions-close '
          'selection tables-empty tables-restored mapped trial-required trial trial-ready '
          'trial-return review review-confirmed running completed interrupted results save restart').split()
PLAN = ('home connect host-empty invalid-connect host-restored connected preflight permissions '
        'permissions-close selection tables-empty tables-restored mapping-open mapping-draft mapped '
        'trial-required trial consent trial-ready trial-return review review-confirmed running '
        '{outcome} results save csv html restart').split()

def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('source', type=Path)
    p.add_argument('physical', type=Path, help='Aggregate with languages[].evidence and cleanup')
    p.add_argument('output', type=Path)
    p.add_argument('--live-interruption', action='store_true', help='Require pre-pointer, transport clock and stability files; no fixture-count equality')
    a = p.parse_args()
    original = {r['id']: r for r in read(a.source)['rows']}
    aggregate = read(a.physical)
    differences, matched, live = [], [], []
    checks = 0
    def diff(path, expected, actual):
        nonlocal checks
        if isinstance(expected, dict):
            if not isinstance(actual, dict) or set(expected) != set(actual):
                differences.append(dict(path=path, expected=expected, actual=actual)); return
            for key in expected: diff(path+'.'+key, expected[key], actual[key])
        elif isinstance(expected, list):
            if not isinstance(actual, list) or len(expected) != len(actual):
                differences.append(dict(path=path, expected=expected, actual=actual)); return
            for i, value in enumerate(expected): diff(f'{path}[{i}]', value, actual[i])
        else:
            checks += 1
            if type(expected) is not type(actual) or expected != actual:
                differences.append(dict(path=path, expected=expected, actual=actual))
    def require(path, condition): diff(path, True, bool(condition))
    def valid_run(path, run):
        require(path+'.generatedID', isinstance(run, dict) and bool(re.fullmatch(r'UI-[A-Z0-9]+', run.get('id', ''))))
    for phase in aggregate['languages']:
        lang = phase['language']
        require(lang+'.language', lang in ('ko', 'en'))
        evidence = Path(phase['evidence'])
        if not evidence.is_absolute(): evidence = a.physical.parent / evidence
        rows = read(evidence)['checks']
        outcome = 'interrupted' if lang == 'en' else 'completed'
        diff(lang+'.milestone-order', [x.format(outcome=outcome) for x in PLAN], [x['id'] for x in rows])
        lookup = {r['id']: r for r in rows}
        for key, value in phase['cleanup'].items():
            if key in ('nativeProcess', 'nativeWindow'): diff(lang+'.cleanup.'+key, 0, value)
        require(lang+'.cleanup-fields', all(k in phase['cleanup'] for k in ('nativeProcess','nativeWindow')))
        for row in rows:
            require(lang+'.'+row['id']+'.actorPassed', row.get('passed') is True)
            key = lang+'-'+row['id']
            if row['id'] not in COMMON or key not in original: continue
            expected, actual = original[key], row['state']
            matched.append(key)
            for field in FIELDS:
                require(key+'.present.'+field, field in actual)
                diff(key+'.'+field, expected[field], actual.get(field))
            if row['id'] in ('completed','interrupted','results','save'):
                run = actual.get('run'); valid_run(key, run)
                if a.live_interruption and lang == 'en':
                    # Immutable shape/selection is still exact; dynamic counts are checked below.
                    for field in ('complete','interrupted','trialSelected','tables','matches'):
                        diff(key+'.run.'+field, expected['run'][field], run.get(field))
                    diff(key+'.run.groupBlueprint', [(g['key'],g['total']) for g in expected['run']['groups']], [(g['key'],g['total']) for g in run['groups']])
                else:
                    normalized = copy.deepcopy(run); normalized['id'] = expected['run']['id']
                    diff(key+'.run', expected['run'], normalized)
        diff(lang+'.finalFormats', ['json','csv','html'], lookup['html']['state']['formats'])
        final_run = lookup[outcome]['state']['run']
        for milestone in ('results','save','csv','html'):
            diff(lang+'.runPersistence.'+milestone, final_run, lookup[milestone]['state']['run'])
        if a.live_interruption and lang == 'en':
            folder = evidence.parent
            pre = read(folder/'immediately-pre-cancel-pointer.json')
            stable = read(folder/'cancel-stability.json')
            transport = (folder/'input-transport.txt').read_text(encoding='utf-8-sig')
            require('en.prePointer.running', pre['state']['screen']=='running' and not pre['state']['run']['complete'] and not pre['state']['run']['interrupted'])
            require('en.cancel.freshClock', bool(re.search(r'input-target=cancel mouse-down-utc=\S+ monotonic=\d+ frequency=\d+', transport)) and bool(re.search(r'input-target=cancel mouse-up-utc=\S+ monotonic=\d+', transport)))
            diff('en.stoppedRun', final_run, stable['run']); diff('en.stoppedStable', True, stable['stoppedStateStable'])
            completed = original['ko-completed']['run']['groups']
            require('en.sameRunBeforeCancel', pre['state']['run']['id']==final_run['id'])
            seen_unfinished = False; seen_pending = False; interrupted = 0
            for i, group in enumerate(final_run['groups']):
                before = pre['state']['run']['groups'][i]
                require(f'en.group{i}.monotonicBoundedCount', type(group['done']) is int and before['done']<=group['done']<=group['total'])
                if group['status']=='completed':
                    require(f'en.group{i}.orderedCompletedPrefix', not seen_unfinished)
                    diff(f'en.group{i}.completedExact', completed[i], group)
                else:
                    seen_unfinished=True
                    require(f'en.group{i}.status', group['status'] in ('pending','interrupted'))
                    require(f'en.group{i}.orderedInterrupted', group['status']!='interrupted' or not seen_pending)
                    interrupted += group['status']=='interrupted'
                    seen_pending = seen_pending or group['status']=='pending'
                    require(f'en.group{i}.unfinished', group['done']<group['total'])
                    if group['status']=='pending': diff(f'en.group{i}.pendingDone',0,group['done'])
                    for counter in ('equal','different','unknown'): diff(f'en.group{i}.'+counter,0,group[counter])
            require('en.atMostOneInterrupted', interrupted<=1)
            live.append(dict(language=lang,prePointer=pre,stoppedRun=final_run,transport=str(folder/'input-transport.txt'),clockPolicy='Actual raw clocks/counts retained; original140ms timer policy, not fixture35ms count equality'))
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(dict(phase='PHYSICAL_APP_MILESTONES_PASSED' if not differences else 'DIFFERENCES',source=str(a.source.resolve()),evidence=str(a.physical.resolve()),sourceMatchedStates=matched,checks=checks,differences=differences,liveInterruption=live,scope='29 ordered physical milestones/language; exact common states/focus/announcement/footer. Generated ID only normalized for fixture run comparison. Scroll offset and unexecuted filter/detail route excluded explicitly; their existing OS proofs separate. Live interruption mode requires raw pre-pointer clocks/count bounds and exact persistence; not identical scheduler timing or whole-app LOCK.'),ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(dict(matched=len(matched),checks=checks,differences=len(differences))))
    return bool(differences)
if __name__=='__main__': raise SystemExit(main())
