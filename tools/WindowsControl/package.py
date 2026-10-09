"""Package the already-published, isolated development verifier (not a product release)."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

repo = Path(__file__).resolve().parents[2]
source = repo / 'artifacts/replica/windows-arm64-check'
target = repo / 'artifacts/replica/windows-download/replica-check.zip'
with ZipFile(target, 'w', ZIP_DEFLATED) as archive:
    for file in source.rglob('*'):
        if not file.is_file():
            continue
        if file.suffix.lower() == '.json' and not (
            file.name.endswith(('.deps.json', '.runtimeconfig.json')) or file.name in ('typography-cases.json','action-button-cases.json','spinner-cases.json','checkbox-cases.json','radio-cases.json','textinput-cases.json')
        ):
            continue
        if file.suffix.lower() == '.png' or (file.suffix.lower() == '.txt' and file.name != 'evidence-endpoint.txt'):
            continue
        archive.write(file, file.relative_to(source))
    assert 'ReplicaProbe.runtimeconfig.json' in archive.namelist()
    assert 'ReplicaProbe.deps.json' in archive.namelist()
print('Published verifier package:', target.name)
