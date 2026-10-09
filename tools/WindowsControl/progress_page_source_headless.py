"""Original protected fixture in a separate official Edge headless process/profile."""
from pathlib import Path
import json,time,uuid
from driver import ps,q
r=Path(__file__).resolve().parents[2];nonce=uuid.uuid4().hex;out=r/'artifacts/replica/workers/root/progresspage/windows-original-headless'/nonce;out.mkdir(parents=True)
assert 'root' in json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['headless']['owners']
for ext in ['js','css']:
    assert (r/f'artifacts/replica/windows-download/progresspage-original.{ext}').read_bytes()==(r/f'docs/replica/source/original.{ext}').read_bytes()
target=r/'docs/replica/source/root-progresspage-windows-source.json';before=target.stat().st_mtime_ns if target.exists() else 0
code="$edge=Join-Path ${env:ProgramFiles(x86)} 'Microsoft\\Edge\\Application\\msedge.exe';if(!(Test-Path $edge)){throw 'Official installed Edge missing'};$profile=Join-Path $env:TEMP "+q('DBMV-root-progress-source-headless-'+nonce)+";$log=Join-Path $profile 'dump.html';New-Item -ItemType Directory $profile -Force|Out-Null;$process=Start-Process -FilePath $edge -ArgumentList @('--headless=new',('--user-data-dir='+$profile),'--force-device-scale-factor=2','--window-size=1600,1000','--virtual-time-budget=12000','--dump-dom','http://10.211.55.2:8768/progresspage.html?os=windows') -RedirectStandardOutput $log -RedirectStandardError (Join-Path $profile 'edge.log') -PassThru -Wait;$exitCode=$process.ExitCode;[pscustomobject]@{exitCode=$exitCode;profile=$profile;dumpBytes=(Get-Item $log).Length;remaining=@(Get-CimInstance Win32_Process -Filter \"name='msedge.exe'\"|Where-Object{$_.CommandLine.Contains($profile)}).Count}|ConvertTo-Json;"
result=json.loads(ps(code,timeout=60));assert result['exitCode']==0 and result['remaining']==0,result
assert target.exists() and target.stat().st_mtime_ns>before,'Fresh original recorder POST missing'
v=json.loads(target.read_text());assert len(v['rows'])==80 and v['dpr']==2
(out/'source.json').write_bytes(target.read_bytes())
(out/'environment.json').write_text(json.dumps({'phase':'WINDOWS_ORIGINAL_HEADLESS_RECORDED','source':'unchanged original JS/CSS + measurement fixture','browser':'official installed Edge headless/new independent profile','dpr':2,'rows':80,'nativeInput':False,'result':result},indent=2)+'\n');print(out)
