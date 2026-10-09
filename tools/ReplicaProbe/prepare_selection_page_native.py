"""Immutable complete Mac bundles; prepare only, never launches or uses input."""
from pathlib import Path
import sys,json,hashlib,shutil,plistlib,shlex
r=Path(__file__).resolve().parents[2];manifest=Path(sys.argv[1]).resolve();m=json.loads(manifest.read_text());assert m['owner']=='preflight' and m['component']=='selectionpage' and m['runtime']=='osx-arm64' and m['buildExitCode']==0
binary=Path(m['binary']);assert hashlib.sha256(binary.read_bytes()).hexdigest()==m['binarySha256']
for mode in ['geometry','input']:
 if (manifest.parent/('SelectionPage-'+mode+'.app')/'Contents/Info.plist').exists():continue
 bundle=manifest.parent/('SelectionPage-'+mode+'.app');assert not (bundle/'Contents/Info.plist').exists();mac=bundle/'Contents/MacOS';shutil.copytree(binary.parent,mac/'runtime',dirs_exist_ok=True);out=manifest.parent/'actual-macos'/mode;out.mkdir(parents=True,exist_ok=True)
 cases=json.loads((r/'docs/replica/source/selectionpage-cases.json').read_text())
 if mode=='input':cases=[c for c in cases if c['viewport']==1180 and c['variant']=='default']
 assert cases;(out/'cases.json').write_text(json.dumps(cases));flag='--selectionpage'if mode=='geometry'else'--selectionpage-window';extras=' --geometry-only'if mode=='geometry'else(' '+sys.argv[2] if len(sys.argv)>2 else' --resume-empty')
 executable=mac/'SelectionPageCheck';executable.write_text('#!/bin/sh\nexec /usr/bin/env DOTNET_ROOT='+shlex.quote('/Users/youptebook/.cache/db-migration-verifier/dotnet')+' '+shlex.quote(str(mac/'runtime/ReplicaProbe'))+' '+flag+' '+shlex.quote(str(out/'cases.json'))+' '+shlex.quote(str(out))+extras+' >'+shlex.quote(str(out/'probe.log'))+' 2>&1\n');executable.chmod(0o755)
 (bundle/'Contents/Info.plist').write_bytes(plistlib.dumps({'CFBundleIdentifier':'local.dbmv.replica.selectionpagecheck.'+mode+'.'+manifest.parent.name,'CFBundleExecutable':'SelectionPageCheck','CFBundleName':'SelectionPageCheck','CFBundlePackageType':'APPL','NSHighResolutionCapable':True}));(out/'bundle.json').write_text(json.dumps({'manifest':str(manifest),'bundle':str(bundle),'binarySha256':m['binarySha256']},indent=2));print(bundle)
