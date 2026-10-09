"""Cold owned MetricItem rendering/source/passive peer; approved official guest UIA only."""
from pathlib import Path
import argparse,json,re,hashlib,time,base64
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','source-peer','launch','read','peer','close']);p.add_argument('--stage',type=Path);p.add_argument('--lifecycle',action='store_true');a=p.parse_args()
if json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']!='connection':raise RuntimeError('Connection Windows resource required')
base='';native=not a.action.startswith('source-')
if native:
 if not a.stage:raise RuntimeError('Owned full stage required')
 s=json.loads(a.stage.read_text())
 if s['owner']!='connection'or s['component']!='metricitem'or not s['selfContained']or not re.fullmatch(r'DBMV-Isolated-connection-metricitem-[0-9a-f]{32}',s['guestFolder']):raise RuntimeError('Owned isolated Metric stage required')
 base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';"
title='MetricItem lifecycle · original isolation'if a.lifecycle else'MetricItem · original isolation'
owned=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{"+("$_.Current.Name-eq 'Replica MetricItem verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"if native else"$_.Current.Name.StartsWith("+q(title)+") -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')")+"});if($w.Count-ne 1){throw 'Unique owned Metric window required'};$w=$w[0];"
if a.action=='source-open':
 guard="$existing=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+") -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});if($existing.Count){throw 'Owned source already open; inspect rather than duplicate launch'};"
 print(ps(guard+"Start-Process msedge.exe -ArgumentList @('--new-window',"+q('http://10.211.55.2:8768/metricitem'+('-lifecycle'if a.lifecycle else'')+'.html?os=windows')+");"))
elif a.action=='source-record':
 file=r/('docs/replica/source/connection-metricitem-windows-'+('input'if a.lifecycle else'source')+'.json');before=file.stat().st_mtime_ns if file.exists()else 0
 try:
  print(ps(owned+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('metricitem-run'if a.lifecycle else'metricitem-record')+"));if($null-eq $c-or !$c.Current.IsEnabled-or $c.Current.Name-ne "+q('Run MetricItem lifecycle'if a.lifecycle else'Record MetricItem')+"){throw 'Fresh fixed record target missing; inspect existing evidence'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 except RuntimeError:
  if not file.exists()or file.stat().st_mtime_ns<=before:raise
  print('Ambiguous CLI return resolved by fresh owned source evidence; no duplicate invocation')
 end=time.monotonic()+6
 while time.monotonic()<end:
  if file.exists()and file.stat().st_mtime_ns>before:break
  time.sleep(.05)
 else:raise RuntimeError('Fresh source record missing')
 data=json.loads(file.read_text());(r/('docs/replica/source/metricitem-windows-'+('lifecycle-source'if a.lifecycle else'source')+'.json')).write_text(json.dumps(data,indent=2)+'\n')
 if not a.lifecycle:(r/'artifacts/replica/windows-download/metricitem-source.json').write_bytes(file.read_bytes())
 print('Recorded',len(data['rows']))
elif a.action=='source-peer':
 code=owned+"$items=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'불일치 항목')));if($items.Count-ne 1){throw 'Unique first metric label required'};$label=$items[0];$value=[System.Windows.Automation.TreeWalker]::ControlViewWalker.GetNextSibling($label);if($null-eq $value-or $value.Current.Name-ne '0'){throw 'First original metric value sibling required'};$rows=@($label,$value)|ForEach-Object{[pscustomobject]@{name=$_.Current.Name;type=$_.Current.ControlType.ProgrammaticName;control=$_.Current.IsControlElement;content=$_.Current.IsContentElement}};$rows|ConvertTo-Json -Depth 5;"
 data=ps(code,retry_read=True);(r/'docs/replica/source/metricitem-windows-source-peer.json').write_text(data);print(data)
elif a.action=='launch':
 f=r/'artifacts/replica/windows-download/metricitem-source.json';digest=hashlib.sha256(f.read_bytes()).hexdigest()
 print(ps(base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime already active; inspect instead of relaunch'};$f=Join-Path $app 'metricitem-source.json';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/metricitem-source.json' -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Case hash mismatch'};Start-Process -FilePath ($app+'\\ReplicaProbe.exe') -ArgumentList @('--metricitem-window',$f,$app) -WorkingDirectory $app;"))
elif a.action=='read':
 out=r/'docs/replica/source/metricitem-windows-native';out.mkdir(exist_ok=True)
 for name in ['metricitem-native.json','metricitem-lifecycle.json']:
  data=ps(owned+"Get-Content -Raw (Join-Path $app "+q(name)+");",retry_read=True);json.loads(data);(out/name).write_text(data)
 for name in ['metricitem-en-326-unknown-2.png','metricitem-ko-904-equal-1.png','metricitem-en-656-id-2.png']:
  data=ps(owned+"[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $app "+q(name)+")))",retry_read=True);(out/name).write_bytes(base64.b64decode(data))
 print('Native geometry72 + lifecycle + PNGs saved')
elif a.action=='peer':
 data=ps(owned+"$items=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ControlType-eq [System.Windows.Automation.ControlType]::Text-and $_.Current.IsContentElement-and $_.Current.IsControlElement});$items|ForEach-Object{[pscustomobject]@{name=$_.Current.Name;type=$_.Current.ControlType.ProgrammaticName;control=$_.Current.IsControlElement;content=$_.Current.IsContentElement}}|ConvertTo-Json -Depth 5;",retry_read=True)
 (r/'docs/replica/source/metricitem-windows-peer.json').write_text(data);print(data)

else:
 print(ps(owned+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 if native:print(ps(base+"[Console]::WriteLine(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count)",retry_read=True))
