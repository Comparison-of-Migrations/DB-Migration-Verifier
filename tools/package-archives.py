"""Archive the existing OS runtime outputs; does not build or include fixtures."""
from pathlib import Path
import hashlib,json,zipfile

repo=Path(__file__).resolve().parents[1]
output=repo/'artifacts/dev'
packages=[]
for target,source,prefix in [('macos-arm64',output/'DB-Migration-Verifier.app','DB-Migration-Verifier.app'),('win-x64',output/'win-x64','')]:
    files=sorted(f for f in source.rglob('*') if f.is_file())
    if not files:raise RuntimeError('Publish the OS runtime before archiving')
    archive=output/('DB-Migration-Verifier-'+target+'.zip')
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as bundle:
        for file in files:
            if file.is_symlink() or file.name in ('fixture.json','ui.json','.env','server.crt','server.pem','server.key'):
                raise RuntimeError('Unexpected non-runtime file in output')
            bundle.write(file,str(Path(prefix)/file.relative_to(source)))
        if target=='macos-arm64':bundle.write(repo/'tools/runtime-README.txt','README.txt')
        for notice in ('LICENSE','NOTICE','THIRD_PARTY_NOTICES.txt'):
            bundle.write(repo/notice,notice)
    with zipfile.ZipFile(archive) as bundle:
        if bundle.testzip() or 'README.txt' not in bundle.namelist() or any(i.flag_bits&1 for i in bundle.infolist()):
            raise RuntimeError('Runtime archive validation failed')
    packages.append({'target':target,'archive':str(archive),'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),
        'bytes':archive.stat().st_size,'files':len(files),'runtimeIncluded':True,'noPassword':True,
        'sourceAssemblies':{str(f.relative_to(source)):hashlib.sha256(f.read_bytes()).hexdigest() for f in source.rglob('Verifier.*.dll')}})
(output/'packages.json').write_text(json.dumps({'developmentOnly':True,'packages':packages},indent=2)+'\n')
print('Development runtime archives checked; actual OS acceptance is tracked separately')
