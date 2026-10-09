"""Complete immutable Mac bundles from one owned snapshot; preparation does not launch."""
from pathlib import Path
import sys,json,hashlib,shutil,plistlib,shlex
r=Path(__file__).resolve().parents[2];manifest=Path(sys.argv[1]).resolve();m=json.loads(manifest.read_text());assert m['owner']=='preflight' and m['component']=='preflightpage' and m['runtime']=='osx-arm64' and m['buildExitCode']==0
binary=Path(m['binary']);assert hashlib.sha256(binary.read_bytes()).hexdigest()==m['binarySha256']
for mode in ['geometry','input']:
 bundle=manifest.parent/('PreflightPage-'+mode+'.app');assert not bundle.exists();mac=bundle/'Contents/MacOS';shutil.copytree(binary.parent,mac/'runtime');out=manifest.parent/'actual-macos'/mode;out.mkdir(parents=True)
 cases=json.loads((r/'docs/replica/source/preflightpage-cases.json').read_text())
 if mode=='input':cases=[c for c in cases if c['id']in ['ko-1180','en-1180']]
 (out/'cases.json').write_text(json.dumps(cases));flag='--preflightpage'if mode=='geometry'else'--preflightpage-window'
 executable=mac/'PreflightPageCheck';executable.write_text('#!/bin/sh\nexec /usr/bin/env DOTNET_ROOT='+shlex.quote('/Users/youptebook/.cache/db-migration-verifier/dotnet')+' '+shlex.quote(str(mac/'runtime/ReplicaProbe'))+' '+flag+' '+shlex.quote(str(out/'cases.json'))+' '+shlex.quote(str(out))+' >'+shlex.quote(str(out/'probe.log'))+' 2>&1\n');executable.chmod(0o755)
 (bundle/'Contents/Info.plist').write_bytes(plistlib.dumps({'CFBundleIdentifier':'local.dbmv.replica.preflightpagecheck.'+mode,'CFBundleExecutable':'PreflightPageCheck','CFBundleName':'PreflightPageCheck','CFBundlePackageType':'APPL','NSHighResolutionCapable':True}));(out/'bundle.json').write_text(json.dumps({'manifest':str(manifest),'bundle':str(bundle),'binarySha256':m['binarySha256']},indent=2))
 print(bundle)
