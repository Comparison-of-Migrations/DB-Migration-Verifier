"""Prepare one new immutable owned Summary content native bundle; no launch or OS input."""
from pathlib import Path
import argparse,hashlib,json,plistlib,shutil
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);a=p.parse_args();manifest=a.manifest.resolve();m=json.loads(manifest.read_text())
assert m['owner']=='home' and m['component']=='pageintegration' and m['runtime']=='osx-arm64' and m['buildExitCode']==0
assert manifest.parent.is_relative_to(ROOT/'artifacts/replica/workers/home/pageintegration')
binary=Path(m['binary']);assert hashlib.sha256(binary.read_bytes()).hexdigest()==m['binarySha256']
bundle=manifest.parent/'PageIntegrationSummaryContentCheck.app';assert not bundle.exists(),'New immutable bundle required; no overwrite of prior candidate'
contents=bundle/'Contents';contents.mkdir(parents=True);shutil.copytree(binary.parent,contents/'MacOS')
executable=contents/'MacOS/ReplicaProbe';assert executable.is_file() and executable.stat().st_mode&0o111,'Actual Mac apphost must be executable'
(contents/'Info.plist').write_bytes(plistlib.dumps({'CFBundleExecutable':'ReplicaProbe','CFBundleIdentifier':'local.dbmv.home.summarycontent.'+manifest.parent.name,'CFBundleName':'Summary Content Check','CFBundlePackageType':'APPL','NSHighResolutionCapable':True}))
oracle=ROOT/'docs/replica/source/home-pageintegrationsummarycontent-mac-source.json';record={'scope':'File-only full runtime immutable bundle preparation; native not launched','manifest':str(manifest),'bundle':str(bundle),'binarySha256':hashlib.sha256((contents/'MacOS/ReplicaProbe.dll').read_bytes()).hexdigest(),'oracle':str(oracle),'oracleSha256':hashlib.sha256(oracle.read_bytes()).hexdigest(),'files':{str(f.relative_to(bundle)):hashlib.sha256(f.read_bytes()).hexdigest()for f in bundle.rglob('*') if f.is_file()}}
assert record['binarySha256']==m['binarySha256'];(manifest.parent/'summary-content-macos-bundle.json').write_text(json.dumps(record,indent=2)+'\n');print(bundle)
