"""Check all protected replica code ranges; no code or lock records are modified."""
from pathlib import Path
import json,hashlib
root=Path(__file__).resolve().parents[2]
for path in sorted((root/'docs/replica/source').glob('*-lock.json')):
    lock=json.loads(path.read_text())
    for name,expected in lock['files'].items():
        assert hashlib.sha256((root/name).read_bytes()).hexdigest()==expected, f'LOCK changed: {name}'
    for fragment in lock.get('fragments',[]):
        text=(root/fragment['file']).read_text()
        part=text[text.index(fragment['start']):text.index(fragment['end'])]
        assert hashlib.sha256(part.encode()).hexdigest()==fragment['sha256'], f"LOCK changed: {fragment['file']} fragment"
    print(f'{path.stem}: protected code ranges unchanged.')
