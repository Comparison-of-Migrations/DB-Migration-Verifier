"""One finite owned actual scope. Every action logged; no automatic replay."""
from pathlib import Path
import json,sys
from driver import ps,q
r=Path(__file__).resolve().parents[2];manifest=Path(sys.argv[1]);m=json.loads(manifest.read_text());stage=json.loads((manifest.parent/'windows-stage.json').read_text());current=json.loads((manifest.parent/'current-windows.json').read_text());out=Path(current['output']);assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='root'
code="$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';$out=Join-Path $app "+q('actual-'+current['nonce'])+";"
code+="Add-Type -TypeDefinition "+q((r/'tools/WindowsControl/owned_progress_page_input.cs').read_text())+" -ReferencedAssemblies @('UIAutomationClient','UIAutomationTypes','WindowsBase');"
code+="function Record($label){$p=Join-Path $out 'progresspage-actual-state.json';$f=[IO.File]::Open($p,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete);$rd=[IO.StreamReader]::new($f);try{$value=$rd.ReadToEnd()}finally{$rd.Dispose()};[IO.File]::WriteAllText((Join-Path $out ('state-'+$label+'.json')),$value);};function Act($mode,$label,$delay){[IO.File]::AppendAllText((Join-Path $out 'actual-actions.jsonl'),('{\"requested\":\"'+$mode+'\",\"label\":\"'+$label+'\"}'+[Environment]::NewLine));$message=[OwnedProgressInput]::Run($app,$mode);Start-Sleep -Milliseconds $delay;Record $label;[IO.File]::AppendAllText((Join-Path $out 'actual-actions.jsonl'),('{\"completed\":\"'+$mode+'\",\"label\":\"'+$label+'\"}'+[Environment]::NewLine));};Record 'initial';"
actions=[('hover','hover-normal',180),('leave','leave-normal',180),('F10','live',740),('F8','language',120),('F7','detached',120),('F7','remounted',120),('F6','reduced',120),('hover','hover-reduced',180),('leave','leave-reduced',180),('F11','interrupted',120)]
for mode,label,delay in actions:code+='Act '+q(mode)+' '+q(label)+' '+str(delay)+';'
code+="[Console]::WriteLine('FINITE_ACTUAL_SCOPE_COMPLETED');"
(out/'sequence-command.json').write_text(json.dumps({'actions':actions,'scope':'real OS guarded pointer/key; diagnostics setup only; no replay'},indent=2))
try:
 result=ps(code,timeout=20);(out/'sequence-result.txt').write_text(result);print(result)
except Exception as e:
 (out/'sequence-failure.txt').write_text(str(e));raise
