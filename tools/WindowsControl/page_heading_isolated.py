"""Actual Windows PageHeading checks against a frozen full owned runtime."""
from pathlib import Path
import argparse, base64, hashlib, json, time
from driver import ps, q

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('action', choices=['auto', 'launch', 'read', 'close'])
p.add_argument('manifest', type=Path)
p.add_argument('--home', action='store_true')
a = p.parse_args()
m = json.loads(a.manifest.read_text())
stage = json.loads((a.manifest.parent / 'windows-stage.json').read_text())
lease = json.loads((root / 'docs/replica/ENVIRONMENT_LEASE.json').read_text())
if lease['resources']['windows']['owner'] != 'root':
    raise RuntimeError('Root Windows resource required')
kind = 'home' if a.home else 'common'
base = "$base=Join-Path $env:TEMP " + q(stage['guestFolder']) + ";$app=Join-Path $base 'app';$out=Join-Path $base " + q('actual-' + kind) + ";"
find = base + "$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica PageHeading verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
unique = "if($w.Count-ne 1){throw 'Exact owned heading window missing'};$w=$w[0];"
output = a.manifest.parent / ('actual-windows-' + kind)
output.mkdir(exist_ok=True)
if a.action in ['auto', 'launch']:
    cases = root / 'docs/replica/source' / ('pageheadinghome-cases.json' if a.home else 'pageheading-cases.json')
    served = 'root-pageheading-' + kind + '-' + a.manifest.parent.name + '-cases.json'
    (root / 'artifacts/replica/windows-download' / served).write_bytes(cases.read_bytes())
    code = base + "if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned heading runtime active'};New-Item -ItemType Directory -Path $out -Force|Out-Null;$cases=Join-Path $out 'cases.json';"
    code += "Invoke-WebRequest -UseBasicParsing " + q('http://10.211.55.2:8768/' + served) + " -OutFile $cases;"
    code += "if((Get-FileHash $cases -Algorithm SHA256).Hash-ne " + q(hashlib.sha256(cases.read_bytes()).hexdigest().upper()) + "){throw 'Case hash mismatch'};"
    code += "Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;"
    if a.action == 'auto':
        code += "$file=Join-Path $out 'pageheading-native.json';$prior=if(Test-Path $file){(Get-Item $file).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--pageheading' $cases $out;if($LASTEXITCODE-ne 0){throw 'Native heading failed'};if((Get-Item $file).LastWriteTimeUtc.Ticks-le $prior){throw 'Native evidence stale'};"
        print(ps(code, timeout=60))
        data = ps(base + "Get-Content -Raw ($out+'\\pageheading-native.json')", retry_read=True)
        value = json.loads(data)
        assert len(value['rows']) == (12 if a.home else 84) and value['dpr'] == 2
        (output / 'pageheading-native.json').write_text(data)
        image = ps(base + "[Convert]::ToBase64String([IO.File]::ReadAllBytes(($out+'\\pageheading.png')))", retry_read=True)
        (output / 'pageheading.png').write_bytes(base64.b64decode(image))
        (output / 'environment.json').write_text(json.dumps({'buildManifest': str(a.manifest.resolve()), 'binarySha256': m['binarySha256'], 'headless': False, 'cases': str(cases), 'scale': value['dpr'], 'guestFolder': stage['guestFolder']}, indent=2))
        print(output)
    else:
        print(ps(code + "Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--pageheading-window',$cases,$out);"))
        print(ps("$deadline=[DateTime]::UtcNow.AddSeconds(8);do{" + find + "if($w.Count-eq 1){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);" + unique + "[Console]::WriteLine('Owned heading ready');", retry_read=True))
elif a.action == 'read':
    data = ps(find + unique + "Get-Content -Raw ($out+'\\pageheading-input.json')", retry_read=True)
    json.loads(data)
    (output / 'input.json').write_text(data)
    print(data)
else:
    print(ps(find + unique + "$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
