"""Compare recorded application transitions with the literal original handler oracle.

This is a handler/state comparison, not proof of native OS input. Run IDs are
generated from wall-clock time; only that value is normalized, after validation.
No geometry, focus, counts, ordering or missing fields are silently ignored.
"""
import argparse
import copy
import json
from pathlib import Path
import re

parser = argparse.ArgumentParser()
parser.add_argument('source', type=Path)
parser.add_argument('candidate', type=Path)
parser.add_argument('output', type=Path)
args = parser.parse_args()
source = json.loads(args.source.read_text(encoding='utf-8-sig'))
candidate = json.loads(args.candidate.read_text(encoding='utf-8-sig'))
differences = []
checks = 0


def diff(path, expected, actual):
    global checks
    if isinstance(expected, dict):
        if not isinstance(actual, dict):
            differences.append({'path': path, 'expected': expected, 'actual': actual})
            return
        for key, value in expected.items():
            if key not in actual:
                differences.append({'path': path + '.' + key, 'missing': True})
            else:
                diff(path + '.' + key, value, actual[key])
    elif isinstance(expected, list):
        if not isinstance(actual, list) or len(expected) != len(actual):
            differences.append({'path': path, 'expected': expected, 'actual': actual})
            return
        for index, value in enumerate(expected):
            diff(f'{path}[{index}]', value, actual[index])
    else:
        checks += 1
        if type(expected) != type(actual) or expected != actual:
            differences.append({'path': path, 'expected': expected, 'actual': actual})


expected_rows = source['rows']
actual_rows = candidate['rows']
expected_ids = [row['id'] for row in expected_rows]
actual_ids = [row['id'] for row in actual_rows]
assert len(set(expected_ids)) == len(expected_ids), 'Duplicate source transition'
assert len(set(actual_ids)) == len(actual_ids), 'Duplicate candidate transition'
diff('ordered-transition-ids', expected_ids, actual_ids)
lookup = {row['id']: row for row in actual_rows}
for expected in expected_rows:
    if expected['id'] not in lookup:
        continue
    actual = copy.deepcopy(lookup[expected['id']])
    if expected['run'] is not None and actual.get('run') is not None:
        run_id = actual['run'].get('id')
        checks += 1
        if not isinstance(run_id, str) or not re.fullmatch(r'UI-[A-Z0-9]+', run_id):
            differences.append({'path': expected['id'] + '.run.id', 'invalid': True})
        actual['run']['id'] = expected['run']['id']
    diff(expected['id'], expected, actual)

args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text(json.dumps({
    'phase': 'HANDLER_FLOW_PASSED' if not differences else 'DIFFERENCES',
    'scope': 'Recorded handler transitions only; actual OS input is separately required',
    'source': str(args.source.resolve()), 'candidate': str(args.candidate.resolve()),
    'transitions': len(expected_rows), 'checks': checks,
    'normalization': 'Validated generated run ID only; every source row field is compared',
    'differences': differences,
}, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'transitions': len(expected_rows), 'checks': checks, 'differences': len(differences)}))
raise SystemExit(bool(differences))
