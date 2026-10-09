"""Fresh readonly typography API regression in the guarded project fixture runtime."""
from pathlib import Path
import json,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-PageHeading-20261008') 'app';$ProgressPreference='SilentlyContinue';if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime must be closed'};"
for source,mode,name,number in [('typography-baseline49-cases.json','--text-candidate','typography-candidate-layout.json',49),('typography-windows-extended-cases.json','--text-states','typography-state-native.json',66)]:
 f=r/'docs/replica/source'/source;served=r/'artifacts/replica/windows-download'/source;served.write_bytes(f.read_bytes());digest=hashlib.sha256(f.read_bytes()).hexdigest().upper()
 print(ps(base+"$f=$app+'\\"+source+"';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/"+source+"' -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Cases hash mismatch'};$o=$app+'\\"+name+"';$prior=if(Test-Path $o){(Get-Item $o).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') "+q(mode)+" $f $app;if($LASTEXITCODE-ne 0){throw 'Typography regression failed'};if((Get-Item $o).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh typography output'};",timeout=60))
 value=ps(base+"Get-Content -Raw ($app+'\\"+name+"')",retry_read=True);rows=json.loads(value);assert len(rows)==number;out=r/f'docs/replica/source/typography-inline-windows{number}';out.mkdir(exist_ok=True);(out/name).write_text(value+'\n');print('Fresh',number)
