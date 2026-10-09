"""Compare immutable original records to an explicitly named isolated result."""
from pathlib import Path
import argparse, hashlib, json, sys

p = argparse.ArgumentParser()
p.add_argument('source', type=Path)
p.add_argument('candidate', type=Path)
p.add_argument('report', type=Path)
a = p.parse_args()
source, candidate = (json.loads(x.read_text()) for x in [a.source, a.candidate])
errors = []
def compare(x, y, key):
    if isinstance(x, dict):
        if not isinstance(y, dict):
            errors.append({'path': key, 'source': x, 'candidate': y})
            return
        for k, value in x.items():
            compare(value, y.get(k), key + '.' + k)
    elif isinstance(x, list):
        if not isinstance(y, list) or len(x) != len(y):
            errors.append({'path': key, 'source': x, 'candidate': y})
        else:
            for i, (left, right) in enumerate(zip(x, y)):
                compare(left, right, f'{key}[{i}]')
    elif isinstance(x, bool):
        if type(y) is not bool or x != y:
            errors.append({'path': key, 'source': x, 'candidate': y})
    elif isinstance(x, (int, float)):
        if not isinstance(y, (int, float)) or isinstance(y, bool) or abs(x-y) > .001:
            errors.append({'path': key, 'source': x, 'candidate': y})
    elif x != y:
        errors.append({'path': key, 'source': x, 'candidate': y})
compare(source, candidate, 'root')
report = {'source': str(a.source.resolve()), 'candidate': str(a.candidate.resolve()),
          'sourceSha256': hashlib.sha256(a.source.read_bytes()).hexdigest(),
          'candidateSha256': hashlib.sha256(a.candidate.read_bytes()).hexdigest(),
          'cases': len(source.get('rows', [])), 'errors': errors,
          'note': 'Only fields present in original records. Separate input/lifecycle/paint and actual OS evidence required.'}
a.report.parent.mkdir(parents=True, exist_ok=True)
a.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(report['cases'], 'cases;', len(errors), 'differences')
print(json.dumps(errors[:8], ensure_ascii=False))
sys.exit(bool(errors))
