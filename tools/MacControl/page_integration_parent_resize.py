"""Finite external native parent resize48; preserve then immediate owned close0."""
from pathlib import Path
import argparse
import hashlib
import json
import plistlib
import shutil
import subprocess
import time

ROOT = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('manifest', type=Path)
p.add_argument('action', choices=['prepare', 'launch', 'batch', 'close'])
a = p.parse_args()
m = a.manifest.resolve()
meta = json.loads(m.read_text())
assert meta['owner'] == 'connection' and meta['component'] == 'pageintegration'
assert meta['runtime'] == 'osx-arm64' and meta['buildExitCode'] == 0
out = m.parent/'actual-parent-resize-macos'
out.mkdir(exist_ok=True)
bundle = m.parent/'PageIntegrationParentResizeCheck.app'
helper = ROOT/'artifacts/replica/workers/connection/pageintegration/page-integration-parent-resize'
if a.action == 'prepare':
    assert not bundle.exists(), 'Fresh immutable native bundle required'
    shutil.copytree(Path(meta['binary']).parent, bundle/'Contents/MacOS')
    assert hashlib.sha256((bundle/'Contents/MacOS/ReplicaProbe.dll').read_bytes()).hexdigest() == meta['binarySha256']
    (bundle/'Contents/Info.plist').write_bytes(plistlib.dumps(dict(
        CFBundleIdentifier='com.dbmigration.replica.connection.parentresize.'+m.parent.name,
        CFBundleName='Parent resize verification', CFBundleExecutable='ReplicaProbe',
        CFBundlePackageType='APPL', NSHighResolutionCapable=True)))
    print(bundle)
    raise SystemExit(0)
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner'] == 'connection'

def run(mode, value='', label='scope'):
    r = subprocess.run([str(helper), mode, str(bundle), str(out), str(value), label],
                       text=True, capture_output=True, timeout=12)
    (out/(label+'.actor.log')).write_text(r.stdout+r.stderr)
    if r.returncode:
        raise RuntimeError(r.stdout+r.stderr)

def current():
    return json.loads((out/'pageintegrationparentresize-actual-state.json').read_text())

def wait(predicate):
    end = time.monotonic()+5
    while time.monotonic() < end:
        try:
            value = current()
            if predicate(value):
                return value
        except (FileNotFoundError, json.JSONDecodeError):
            pass
        time.sleep(.04)
    raise RuntimeError('Fresh actual parent state missing; no input replay')

def close():
    run('close', label='native-close')
    time.sleep(.4)
    run('cleanup', label='native-cleanup')
    print('Exact own process/window0', flush=True)

if a.action == 'launch':
    assert not (out/'launch.json').exists(), 'Fresh launch only; inspect uncertain launch before replay'
    run('launch', label='native-launch')
    value = wait(lambda v: v['ready'] and v['index'] == 0)
    print('Fresh baseline', value['id'], value['snapshot'], flush=True)
elif a.action == 'close':
    close()
else:
    records = []
    try:
        for index in range(12):
            if index:
                run('next', label='next-'+str(index))
            baseline = wait(lambda v: v['ready'] and v['index'] == index)
            rows = []
            for step, width in enumerate([1280, 1100, 960, 1280]):
                before = current()
                run('resize', width, baseline['id']+'-'+str(step))
                value = wait(lambda v: v['ready'] and v['index'] == index
                             and v['snapshot'] > before['snapshot']+1
                             and abs(v['state']['width']-width) < .01)
                rows.append(value['state'])
                result = dict(dpr=value['state']['dpr'], phase='ACTUAL_OS_RECORDED',
                              scope='External native AXSize only; F8 public parent baseline setup',
                              rows=records+[dict(id=baseline['id'], rows=rows)])
                (out/'pageintegrationparentresize-records.json').write_text(
                    json.dumps(result, ensure_ascii=False, indent=2)+'\n')
                print(baseline['id'], width, value['state']['pageSame'],
                      value['state']['focusTargetFocused'], flush=True)
            records.append(dict(id=baseline['id'], rows=rows))
    except Exception as error:
        (out/'batch-failure.json').write_text(json.dumps(dict(
            phase='UNVERIFIED', exception=str(error), completedContexts=len(records),
            replayedInput=False), indent=2)+'\n')
        raise
    finally:
        close()
