"""Owned full-cold InfoDialog runtime and original source, guarded by Windows resource."""
from pathlib import Path
import argparse,base64,hashlib,json,time
from driver import ps,q

root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','auto','launch','invoke','read','close'])
p.add_argument('manifest',type=Path);p.add_argument('--target',default='record');p.add_argument('--label',default='current')
a=p.parse_args();m=json.loads(a.manifest.read_text());stage=json.loads((a.manifest.parent/'windows-stage.json').read_text())
assert m['owner']=='root' and m['component']=='infodialog'
assert json.loads((root/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='root'
base="$base=Join-Path $env:TEMP "+q(stage['guestFolder'])+";$app=Join-Path $base 'app';$out=Join-Path $base 'actual';"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica InfoDialog verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('InfoDialog · original isolation') -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});"
unique="if($w.Count-ne 1){throw 'Unique owned InfoDialog window required'};$w=$w[0];"
output=a.manifest.parent/'actual-windows';output.mkdir(exist_ok=True)
if a.action=='source-open':
    print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/infodialog.html?os=windows');"))
elif a.action=='source-record':
    file=root/'docs/replica/source/root-infodialog-windows-source.json';prior=file.stat().st_mtime_ns if file.exists() else 0
    print(ps("$deadline=[DateTime]::UtcNow.AddSeconds(8);do{"+source+"if($w.Count-eq 1){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);"+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'infodialog-record'));if($null-eq $b){throw 'Source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",retry_read=True))
    deadline=time.monotonic()+8
    while time.monotonic()<deadline:
        if file.exists() and file.stat().st_mtime_ns>prior:break
        time.sleep(.05)
    else:raise RuntimeError('Original evidence not fresh')
    value=json.loads(file.read_text());assert len(value['rows'])==32 and value['dpr']==2
    (root/'docs/replica/source/infodialog-windows-source.json').write_bytes(file.read_bytes());print('Fresh source32')
elif a.action in ['auto','launch']:
    cases=root/'docs/replica/source/infodialog-cases.json';served='root-infodialog-'+a.manifest.parent.name+'-cases.json'
    (root/'artifacts/replica/windows-download'/served).write_bytes(cases.read_bytes())
    code=base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime active'};New-Item -ItemType Directory -Path $out -Force|Out-Null;$cases=Join-Path $out 'cases.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $cases;if((Get-FileHash $cases -Algorithm SHA256).Hash-ne "+q(hashlib.sha256(cases.read_bytes()).hexdigest().upper())+"){throw 'Case hash mismatch'};Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;"
    if a.action=='auto':
        code+="$f=Join-Path $out 'infodialog-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--infodialog' $cases $out;if($LASTEXITCODE-ne 0){throw 'Native failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'Stale native'};"
        print(ps(code,timeout=60))
        for name in ['infodialog-native.json','infodialog-lifecycle.json']:
            text=ps(base+"Get-Content -Raw ($out+'\\"+name+"')",retry_read=True);json.loads(text);(output/name).write_text(text)
        image=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes(($out+'\\infodialog.png')))",retry_read=True);(output/'infodialog.png').write_bytes(base64.b64decode(image))
        (output/'environment.json').write_text(json.dumps({'headless':False,'buildManifest':str(a.manifest.resolve()),'binarySha256':m['binarySha256'],'guestFolder':stage['guestFolder'],'casesSha256':hashlib.sha256(cases.read_bytes()).hexdigest()},indent=2)+'\n');print(output)
    else:
        print(ps(code+"Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--infodialog-window',$cases,$out);"))
        print(ps("$deadline=[DateTime]::UtcNow.AddSeconds(8);do{"+find+"if($w.Count-eq 1){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);"+unique+"[Console]::WriteLine('Owned InfoDialog ready');",retry_read=True))
elif a.action=='invoke':
    assert a.target in ['open','small','reduced','record','close','footer-close','fixture-close']
    print(ps(find+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('InfoDialog-'+a.target)+"));if($null-eq $b-or !$b.Current.IsEnabled){throw 'Owned enabled target required'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
elif a.action=='read':
    assert a.label.replace('-','').isalnum()
    value=ps(find+unique+"Get-Content -Raw ($out+'\\infodialog-input.json')",retry_read=True);json.loads(value);folder=output/'input';folder.mkdir(exist_ok=True);(folder/(a.label+'.json')).write_text(value);print(value)
else:
    print(ps((source if a.action=='source-close' else find)+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
