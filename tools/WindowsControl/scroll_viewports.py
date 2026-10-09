"""Project-only candidate scroll geometry using the approved official CLI/UIA path."""
from pathlib import Path
import argparse,hashlib,json,base64
from driver import ps,q
r=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=['prepare','auto','glyph-fit','launch','close','boundaries','minimum']);a=p.parse_args()
base="$app=Join-Path (Join-Path $env:TEMP 'DBMV-ScrollViewport-20261007') 'app';"
if a.action=='prepare':
    # Runtime/dependencies unchanged: copy an existing closed, owned published
    # fixture locally in the guest; transfer only the two changed assemblies.
    code=base+"$old=Join-Path (Join-Path $env:TEMP 'DBMV-ResultChart-20261007') 'app';foreach($f in @($app,$old)){if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($f+'\\ReplicaProbe.exe')}).Count){throw 'Owned fixture must be closed'}};if(!(Test-Path $app)){if(!(Test-Path ($old+'\\ReplicaProbe.exe'))){throw 'Known owned runtime missing'};Copy-Item $old $app -Recurse};$ProgressPreference='SilentlyContinue';"
    for name in ['ReplicaProbe.dll','Verifier.Desktop.dll']:
        src=r/'artifacts/replica/windows-arm64-check'/name
        digest=hashlib.sha256(src.read_bytes()).hexdigest().upper()
        (r/'artifacts/replica/windows-download'/('scrollviewport-update-'+name)).write_bytes(src.read_bytes())
        code+="$f=Join-Path $app "+q(name)+";$tmp=$f+'.update';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/scrollviewport-update-'+name)+" -OutFile $tmp;if((Get-FileHash $tmp -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned assembly hash mismatch'};Copy-Item $f ($f+'.previous') -Force;Move-Item $tmp $f -Force;"
    print(ps(code+"[Console]::WriteLine('Owned closed scroll fixture prepared and assemblies hash verified');"))
elif a.action=='launch':
    print(ps(base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned fixture already running'};$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--scrollviewport-window',$app) -PassThru;$deadline=[DateTime]::UtcNow.AddSeconds(6);do{$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$ready=@($wins|Where-Object{$_.Current.Name-eq 'Replica ScrollViewport verification'-and $_.Current.ProcessId-eq $p.Id});if($ready.Count){break};if($p.HasExited){throw 'Owned fixture exited before window ready'};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow-lt $deadline);if(!$ready.Count){throw 'Owned window not ready'};[Console]::WriteLine($p.Id)"))
elif a.action=='close':
    print(ps(base+"$wins=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($wins|Where-Object{$_.Current.Name-eq 'Replica ScrollViewport verification'-and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});$w=@($w|Group-Object {$_.Current.NativeWindowHandle}|ForEach-Object{$_.Group[0]});if($w.Count-ne 1){throw 'Unique owned fixture required'};$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()"))
elif a.action=='glyph-fit':
    code=base+"$f=Join-Path $app 'glyph-fit';New-Item -ItemType Directory -Force $f|Out-Null;$ProgressPreference='SilentlyContinue';"
    for name in ['up-normal','down-normal','down-pressed']:
        filename='scroll-'+name+'.png'
        digest=hashlib.sha256((r/'artifacts/replica/windows-download'/filename).read_bytes()).hexdigest().upper()
        code+="$p=Join-Path $f "+q(filename)+";Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+filename)+" -OutFile $p;if((Get-FileHash $p -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned source crop hash mismatch'};"
    code+="$o=Join-Path $f 'scroll-glyph-fit.json';$prior=if(Test-Path $o){(Get-Item $o).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--scroll-glyph-fit' $f;if($LASTEXITCODE-ne 0){throw 'Owned glyph diagnostic failed'};if((Get-Item $o).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh glyph diagnostic'};"
    print(ps(code,timeout=60))
    data=ps(base+"Get-Content -Raw (Join-Path (Join-Path $app 'glyph-fit') 'scroll-glyph-fit.json')",retry_read=True)
    (r/'docs/replica/source/scrollviewport-windows-glyph-fit.json').write_text(data+'\n')
else:
    record='scrollviewport-native-minimum.json' if a.action=='minimum' else 'scrollviewport-native-boundaries.json' if a.action=='boundaries' else 'scrollviewport-native-layout.json'
    flag='--scrollviewport-minimum' if a.action=='minimum' else '--scrollviewport-boundaries' if a.action=='boundaries' else '--scrollviewport'
    print(ps(base+"$f=Join-Path $app "+q(record)+";$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') "+q(flag)+" $app;if($LASTEXITCODE-ne 0){throw 'Owned scroll candidate failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh owned scroll record'};",timeout=60))
    data=ps(base+"Get-Content -Raw (Join-Path $app "+q(record)+")",retry_read=True)
    out=r/'docs/replica/source/scrollviewport-windows-native';out.mkdir(exist_ok=True)
    (out/record).write_text(data+'\n')
    print('Fresh candidate cases:',len(json.loads(data)['rows']))
    if a.action=='auto':
        image=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($app+'\\scrollviewport-native-top.png'))",retry_read=True)
        (out/'scrollviewport-native-top.png').write_bytes(base64.b64decode(image))
