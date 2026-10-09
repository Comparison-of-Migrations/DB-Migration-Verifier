"""Owned heading peer checks; headless process isolated, native UIA lease guarded."""
from pathlib import Path
import argparse, hashlib, json, uuid
from driver import ps, q
r=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=['headless','native']);p.add_argument('manifest',type=Path);a=p.parse_args()
m=json.loads(a.manifest.read_text());stage=json.loads((a.manifest.parent/'windows-stage.json').read_text())
assert m['owner']=='root' and m['component'] in ['actionbuttonsemantic'] and m['buildExitCode']==0 and stage['selfContained']
assert hashlib.sha256(Path(m['binary']).read_bytes()).hexdigest()==m['binarySha256']
resources=json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']
assert resources['windows']['owner']=='root' if a.action=='native' else 'root' in resources['headless']['owners']
flag='actionbuttonsemantic';title='Replica Action button semantics verification'
nonce=uuid.uuid4().hex;out=a.manifest.parent/a.action/nonce;out.mkdir(parents=True)
base="$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';$out=Join-Path $app "+q('heading-'+nonce)+";New-Item -ItemType Directory $out -Force|Out-Null;if((Get-FileHash ($app+'\\ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(m['binarySha256'].upper())+"){throw 'Owned binary mismatch'};"
if a.action=='headless':
    code=base+"$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';& ($app+'\\ReplicaProbe.exe') '--"+flag+"' 'unused' $out;if($LASTEXITCODE-ne0){throw 'Headless failed'};Get-Content -Raw (Join-Path $out '"+flag+"-native.json');"
    value=json.loads(ps(code,timeout=60));assert len(value['initial'])==8 and len(value['remount'])==8 and all(x['type']=='Button' and x['children']==0 and x['invoke'] for x in value['initial']+value['remount'])
else:
    code=base+"$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--"+flag+"-window','unused',$out) -PassThru;try{$deadline=[DateTime]::UtcNow.AddSeconds(12);do{$ws=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq "+q(title)+" -and $_.Current.ProcessId-eq $p.Id});if($ws.Count-eq1){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow-lt $deadline);if($ws.Count-ne1){throw 'Unique owned heading window missing'};if((Get-Process -Id $p.Id).Path-ne ($app+'\\ReplicaProbe.exe')){throw 'Exact executable mismatch'};$w=$ws[0];$rows=@(foreach($id in "+"@('ko-base','ko-disabled','ko-loading','ko-selected','en-base','en-disabled','en-loading','en-selected')"+"){$e=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+"('ButtonSemantics-'+$id)"+"));if($null-eq$e){throw 'Scoped peer missing'};[pscustomobject]@{id=$id;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;children=$e.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition).Count}});$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();$p.WaitForExit(5000)|Out-Null;if(!$p.HasExited){throw 'Owned cleanup not complete'};[pscustomobject]@{rows=$rows;processId=$p.Id;cleanup=0}|ConvertTo-Json -Depth 5}finally{if(!$p.HasExited){Write-Error 'Owned window still active; preserve diagnostic'}};"
    value=json.loads(ps(code,timeout=60));assert len(value['rows'])==8 and all(x['type']=='ControlType.Button' and x['children']==0 for x in value['rows']) and value['cleanup']==0
rows=value['initial'] if a.action=='headless' else value['rows']
assert all(x['name']==('Required permissions' if x['id'].removeprefix('ButtonSemantics-').startswith('en-') else '필요한 권한 보기') for x in rows)
(out/(flag+'-native.json')).write_text(json.dumps(value,indent=2)+'\n')
(out/'environment.json').write_text(json.dumps({'phase':'HEADLESS_PASSED' if a.action=='headless' else 'ACTUAL_WINDOWS_PASSED','manifest':str(a.manifest.resolve()),'binarySha256':m['binarySha256'],'scope':'KOEN button8 leaf role/name/children/Invoke; normal/disabled/loading/selected; layout/input unchanged reused'},indent=2)+'\n')
print(out)
