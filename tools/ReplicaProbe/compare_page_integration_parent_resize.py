"""Compare only newly measured connected parent CSS resize contracts."""
from pathlib import Path
import argparse
import json

p = argparse.ArgumentParser()
p.add_argument('source', type=Path)
p.add_argument('candidate', type=Path)
p.add_argument('output', type=Path)
a = p.parse_args()
source = json.loads(a.source.read_text())
native = json.loads(a.candidate.read_text())
errors = []
checks = 0

def check(context, index, key, actual, expected):
    global checks
    checks += 1
    if actual != expected:
        errors.append(dict(context=context, index=index, key=key, actual=actual, expected=expected))

def box(context, index, key, actual, expected):
    if actual is None or expected is None:
        check(context, index, key, actual, expected)
        return
    for axis in ('x', 'y', 'width', 'height'):
        global checks
        checks += 1
        if abs(actual[axis] - expected[axis]) > .01:
            errors.append(dict(context=context, index=index, key=key+'.'+axis,
                               actual=actual[axis], expected=expected[axis]))

check('all', -1, 'source scope', source['scope'], 'parent-resize-only6')
check('all', -1, 'context ids', [r['id'] for r in native['rows']], [r['id'] for r in source['rows']])
check('all', -1, 'render scale', native['dpr'], source['dpr'])
for s_context, n_context in zip(source['rows'], native['rows']):
    context = s_context['id']
    check(context, -1, 'state count', len(n_context['rows']), len(s_context['rows']))
    check(context, -1, 'original width sequence', [r['width'] for r in s_context['rows']], [1280, 1100, 960, 1280])
    for index, (s, n) in enumerate(zip(s_context['rows'], n_context['rows'])):
        for key in ('pageSame', 'headingSame', 'focusTargetSame', 'focusTargetFocused'):
            check(context, index, 'original '+key, s[key], True)
        check(context, index, 'original no active page animation', s['pageAnimations'], 0)
        check(context, index, 'candidate no entry replay', n['noEntry'], True)
        for key in ('width', 'height', 'pageSame', 'headingSame', 'focusTargetSame',
                    'focusTargetFocused', 'selected', 'screen', 'english', 'announcement',
                    'pageOpacity', 'mainScroll', 'documentScroll', 'footerActions',
                    'formats', 'stopped', 'visited'):
            check(context, index, key, n[key], s[key])
        check(context, index, 'box keys', list(n['boxes']), list(s['boxes']))
        for key, expected in s['boxes'].items():
            box(context, index, 'boxes.'+key, n['boxes'][key], expected)

result = dict(status='PASSED' if not errors else 'FAILED', checks=checks,
              differences=errors, source=str(a.source.resolve()), candidate=str(a.candidate.resolve()),
              contexts=len(source['rows']), states=sum(len(r['rows']) for r in source['rows']),
              scope='Only six connected parent resize identities/focus/selection/entry/layout at KOEN widths1280/1100/960/1280; passive API seed and pending Progress without timer. Existing child matrices and trusted callbacks reused; actual OS resize and close0 evidence separate.')
a.output.write_text(json.dumps(result, indent=2, ensure_ascii=False)+'\n')
print(result['status'], checks, 'checks', len(errors), 'differences')
raise SystemExit(0 if not errors else 1)
