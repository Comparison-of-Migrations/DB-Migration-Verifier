"""Cold owned Metrics rendering/source/passive peer; approved official guest UIA only."""
from pathlib import Path
import argparse,json,re,hashlib,time,base64
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','source-peer','launch','read','peer','capture','close']);p.add_argument('--stage',type=Path);p.add_argument('--lifecycle',action='store_true');a=p.parse_args()
if json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']!='connection':raise RuntimeError('Connection Windows resource required')
base='';native=not a.action.startswith('source-')
if native:
 if not a.stage:raise RuntimeError('Owned full stage required')
 s=json.loads(a.stage.read_text())
 if s['owner']!='connection'or s['component']!='metrics'or not s['selfContained']or not re.fullmatch(r'DBMV-Isolated-connection-metrics-[0-9a-f]{32}',s['guestFolder']):raise RuntimeError('Owned isolated Metrics stage required')
 base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';"
title='Metrics lifecycle · original isolation'if a.lifecycle else'Metrics · original isolation'
owned=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{"+("$_.Current.Name-eq 'Replica Metrics verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"if native else"$_.Current.Name.StartsWith("+q(title)+") -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')")+"});if($w.Count-ne 1){throw 'Unique owned Metrics window required'};$w=$w[0];"
if a.action=='source-open':
 guard="$existing=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+") -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});if($existing.Count){throw 'Owned source already open; inspect rather than duplicate launch'};"
 print(ps(guard+"Start-Process msedge.exe -ArgumentList @('--new-window',"+q('http://10.211.55.2:8768/metrics'+('-lifecycle'if a.lifecycle else'')+'.html?os=windows')+");"))
elif a.action=='source-record':
 file=r/('docs/replica/source/connection-metrics-windows-'+('input'if a.lifecycle else'source')+'.json');before=file.stat().st_mtime_ns if file.exists()else 0
 try:
  print(ps(owned+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('metrics-run'if a.lifecycle else'metrics-record')+"));if($null-eq $c-or !$c.Current.IsEnabled-or $c.Current.Name-ne "+q('Run Metrics lifecycle'if a.lifecycle else'Record Metrics')+"){throw 'Fresh fixed record target missing; inspect existing evidence'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 except RuntimeError:
  if not file.exists()or file.stat().st_mtime_ns<=before:raise
  print('Ambiguous CLI return resolved by fresh owned source evidence; no duplicate invocation')
 end=time.monotonic()+6
 while time.monotonic()<end:
  if file.exists()and file.stat().st_mtime_ns>before:break
  time.sleep(.05)
 else:raise RuntimeError('Fresh source record missing')
 data=json.loads(file.read_text());(r/('docs/replica/source/metrics-windows-'+('lifecycle-source'if a.lifecycle else'source')+'.json')).write_text(json.dumps(data,indent=2)+'\n')
 if not a.lifecycle:(r/'artifacts/replica/windows-download/metrics-source.json').write_bytes(file.read_bytes())
 print('Recorded',len(data['rows']))
elif a.action=='source-peer':
 code=owned+"$names=@('불일치 항목','3','확인 불가·미검증 항목','2','일치 항목','45','예시 검사 ID','UI-MUZ1LP3S');$nodes=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ControlType-eq [System.Windows.Automation.ControlType]::Text-and $_.Current.IsControlElement-and $_.Current.IsContentElement-and $names-contains $_.Current.Name});if($nodes.Count-ne8-or(($nodes|ForEach-Object{$_.Current.Name})-join '|')-ne($names-join '|')){throw 'Exact original visible Metrics eight Text/order required'};$nodes|ForEach-Object{[pscustomobject]@{name=$_.Current.Name;type=$_.Current.ControlType.ProgrammaticName;control=$_.Current.IsControlElement;content=$_.Current.IsContentElement}}|ConvertTo-Json -Depth 5;"
 data=ps(code,retry_read=True);(r/'docs/replica/source/metrics-windows-source-peer.json').write_text(data);print(data)
elif a.action=='launch':
 f=r/'artifacts/replica/windows-download/metrics-source.json';digest=hashlib.sha256(f.read_bytes()).hexdigest()
 print(ps(base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime already active; inspect instead of relaunch'};$f=Join-Path $app 'metrics-source.json';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/metrics-source.json' -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Case hash mismatch'};$started=[DateTime]::UtcNow;Start-Process -FilePath ($app+'\\ReplicaProbe.exe') -ArgumentList @('--metrics-window',$f,$app) -WorkingDirectory $app;$deadline=[DateTime]::UtcNow.AddSeconds(10);do{$ws=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica Metrics verification'-and(Get-Process -Id $_.Current.ProcessId).Path-eq($app+'\\ReplicaProbe.exe')});$record=Join-Path $app 'metrics-lifecycle.json';if($ws.Count-eq1-and(Test-Path $record)-and(Get-Item $record).LastWriteTimeUtc-ge$started){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow-lt$deadline);if($ws.Count-ne1-or!(Test-Path $record)){throw 'Owned Metrics startup incomplete; inspect without relaunch'};[Console]::WriteLine('Fresh owned Metrics native72/8 ready');"))
elif a.action=='read':
 out=r/'docs/replica/source/metrics-windows-native';out.mkdir(exist_ok=True)
 for name in ['metrics-native.json','metrics-lifecycle.json']:
  data=ps(owned+"Get-Content -Raw (Join-Path $app "+q(name)+");",retry_read=True);json.loads(data);(out/name).write_text(data)
 for name in ['metrics-en-1000-326-0.png','metrics-en-1280-326-2.png','metrics-ko-1280-904-0.png']:
  data=ps(owned+"[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $app "+q(name)+")))",retry_read=True);(out/name).write_bytes(base64.b64decode(data))
 print('Native geometry72 + lifecycle + PNGs saved')
elif a.action=='peer':
 data=ps(owned+"$items=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ControlType-eq [System.Windows.Automation.ControlType]::Text-and $_.Current.IsContentElement-and $_.Current.IsControlElement});$items|ForEach-Object{[pscustomobject]@{name=$_.Current.Name;type=$_.Current.ControlType.ProgrammaticName;control=$_.Current.IsControlElement;content=$_.Current.IsContentElement}}|ConvertTo-Json -Depth 5;",retry_read=True)
 (r/'docs/replica/source/metrics-windows-peer.json').write_text(data);print(data)

elif a.action=='capture':
 code=owned+"Add-Type -AssemblyName System.Drawing;Add-Type 'using System;using System.Runtime.InteropServices;public static class MetricsCaptureGuard{[DllImport(\"user32.dll\")]public static extern bool SetForegroundWindow(IntPtr h);[DllImport(\"user32.dll\")]public static extern IntPtr GetForegroundWindow();}';[MetricsCaptureGuard]::SetForegroundWindow([IntPtr]$w.Current.NativeWindowHandle)|Out-Null;if([MetricsCaptureGuard]::GetForegroundWindow().ToInt64()-ne$w.Current.NativeWindowHandle){throw 'Owned capture foreground mismatch'};$r=$w.Current.BoundingRectangle;if($r.Width-lt500-or$r.Width-gt4000-or$r.Height-lt400-or$r.Height-gt2400){throw 'Owned capture bounds mismatch'};$b=[Drawing.Bitmap]::new([int]$r.Width,[int]$r.Height);$g=[Drawing.Graphics]::FromImage($b);$g.CopyFromScreen([int]$r.Left,[int]$r.Top,0,0,$b.Size);$m=[IO.MemoryStream]::new();$b.Save($m,[Drawing.Imaging.ImageFormat]::Png);[Console]::WriteLine([Convert]::ToBase64String($m.ToArray()));$g.Dispose();$b.Dispose();$m.Dispose();"
 out=r/'docs/replica/source/metrics-windows-native';out.mkdir(exist_ok=True);(out/'native-frame.png').write_bytes(base64.b64decode(ps(code)));print('Owned actual native capture saved')

else:
 print(ps(owned+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 if native:print(ps(base+"[Console]::WriteLine(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count)",retry_read=True))
