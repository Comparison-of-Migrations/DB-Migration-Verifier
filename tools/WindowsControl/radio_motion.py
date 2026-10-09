"""Bounded source recorder/native timing runner; no arbitrary guest UI target."""
import argparse,json
from pathlib import Path
from driver import ps,q,APP_FOLDER
r=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=['open','record','run','read']);a=p.parse_args()
if a.action=='open':print(ps("Start-Process 'msedge.exe' -ArgumentList '--new-window http://10.211.55.2:8768/radio-motion.html';[Console]::WriteLine('Owned Radio motion page requested')"))
elif a.action=='record':
 code="$ws=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name -like 'Radio motion · original isolation*' -and (Get-Process -Id $_.Current.ProcessId).ProcessName -eq 'msedge'});if($w.Count -ne 1){throw 'Expected owned Radio motion source'};$c=$w[0].FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Record Radio motion'));if($null -eq $c){throw 'Expected own recorder'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();[Console]::WriteLine('Owned Radio motion record requested')"
 print(ps(code))
 Path(r/'docs/replica/source/radio-windows-source-motion.json').write_bytes((r/'docs/replica/source/windows-source-radio-motion.json').read_bytes())
else:
 common="$p=Join-Path $env:TEMP "+q(APP_FOLDER)+";$a=Join-Path $p 'app';"
 if a.action=='run':print(ps(common+"$n=Start-Process ($a+'\\ReplicaProbe.exe') -ArgumentList @('--radio-timing',($a+'\\radio-native-motion.json')) -WorkingDirectory $a -Wait -PassThru;if($n.ExitCode -ne 0){throw 'Radio timing failed'};[Console]::WriteLine('Owned Radio timing exited0')"))
 else:
  data=ps(common+"[IO.File]::ReadAllText($a+'\\radio-native-motion.json')",retry_read=True);(r/'docs/replica/source/radio-windows-native-motion.json').write_text(data);print('Native Radio timing records:',len(json.loads(data)))
