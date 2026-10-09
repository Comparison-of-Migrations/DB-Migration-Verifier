"""Cold owned FindingArticle rendering/source/passive peer; approved official guest UIA only."""
from pathlib import Path
import argparse,json,re,hashlib,time,base64
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','source-peer','launch','read','peer','close','read-precision']);p.add_argument('--stage',type=Path);p.add_argument('--lifecycle',action='store_true');p.add_argument('--precision',action='store_true');a=p.parse_args()
if json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']!='connection':raise RuntimeError('Connection Windows resource required')
base='';native=not a.action.startswith('source-')
if native:
 if not a.stage:raise RuntimeError('Owned full stage required')
 s=json.loads(a.stage.read_text())
 if s['owner']!='connection'or s['component']!='findingarticle'or not s['selfContained']or not re.fullmatch(r'DBMV-Isolated-connection-findingarticle-[0-9a-f]{32}',s['guestFolder']):raise RuntimeError('Owned isolated Finding stage required')
 base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';"
title='FindingArticle lifecycle · original isolation'if a.lifecycle else'FindingArticle · original isolation'
owned=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{"+("$_.Current.Name-eq 'Replica FindingArticle verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"if native else"$_.Current.Name.StartsWith("+q(title)+") -and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')")+"});if($w.Count-ne 1){throw 'Unique owned Finding window required'};$w=$w[0];"
if a.action=='source-open':print(ps("Start-Process msedge.exe -ArgumentList @('--new-window',"+q('http://10.211.55.2:8768/findingarticle'+('-lifecycle'if a.lifecycle else'')+'.html?os=windows')+");"))
elif a.action=='source-record':
 file=r/('docs/replica/source/connection-findingarticle-windows-'+('input'if a.lifecycle else'source')+'.json');before=file.stat().st_mtime_ns if file.exists()else 0
 print(ps(owned+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('findingarticle-run'if a.lifecycle else'findingarticle-record')+"));if($null-eq $c-or !$c.Current.IsEnabled){throw 'Fixed record target missing'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 end=time.monotonic()+6
 while time.monotonic()<end:
  if file.exists()and file.stat().st_mtime_ns>before:break
  time.sleep(.05)
 else:raise RuntimeError('Fresh source record missing')
 data=json.loads(file.read_text());(r/('docs/replica/source/findingarticle-windows-'+('lifecycle-source'if a.lifecycle else'source')+'.json')).write_text(json.dumps(data,indent=2)+'\n');print('Recorded',len(data['rows']))
elif a.action=='source-peer':
 code=owned+"$items=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'F-001')));if($items.Count-ne 1){throw 'Unique original identifier required'};$c=$items[0];$rows=@();for($i=0;$i-lt 6;$i++){$c=[System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($c);if($null-eq $c){break};$texts=@($c.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ControlType-eq [System.Windows.Automation.ControlType]::Text-and $_.Current.IsControlElement-and $_.Current.IsContentElement}|ForEach-Object{[pscustomobject]@{name=$_.Current.Name;type=$_.Current.ControlType.ProgrammaticName}});$rows+=,[pscustomobject]@{name=$c.Current.Name;type=$c.Current.ControlType.ProgrammaticName;focusable=$c.Current.IsKeyboardFocusable;children=$texts};};$rows|ConvertTo-Json -Depth 5;"
 data=ps(code,retry_read=True);(r/'docs/replica/source/findingarticle-windows-source-parents.json').write_text(data);print(data)
elif a.action=='launch':
 f=r/'artifacts/replica/windows-download/findingarticle-source.json';digest=hashlib.sha256(f.read_bytes()).hexdigest()
 print(ps(base+"if(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count){throw 'Owned runtime already active; inspect instead of relaunch'};$f=Join-Path $app 'findingarticle-source.json';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/findingarticle-source.json' -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Case hash mismatch'};Remove-Item Env:DBMV_HEADLESS -ErrorAction SilentlyContinue;$started=[DateTime]::UtcNow;Start-Process -FilePath ($app+'\\ReplicaProbe.exe') -ArgumentList @('--findingarticle-window',$f,$app"+(",'--precision-only'"if a.precision else"")+") -WorkingDirectory $app;$deadline=[DateTime]::UtcNow.AddSeconds(25);do{$ws=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica FindingArticle verification'-and(Get-Process -Id $_.Current.ProcessId).Path-eq($app+'\\ReplicaProbe.exe')});$file=Join-Path $app 'findingarticle-native.json';if($ws.Count-eq1-and(Test-Path $file)-and(Get-Item $file).LastWriteTimeUtc-ge$started){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow-lt$deadline);if($ws.Count-ne1-or!(Test-Path $file)){throw 'Owned startup incomplete; inspect without relaunch'};"))
elif a.action=='read-precision':
 data=ps(owned+"Get-Content -Raw (Join-Path $app 'findingarticle-native.json');",retry_read=True);json.loads(data);(r/'docs/replica/source/findingarticle-windows-precision-native.json').write_text(data);print('Affected native line42 only saved')
elif a.action=='read':
 out=r/'docs/replica/source/findingarticle-windows-native';out.mkdir(exist_ok=True)
 for name in ['findingarticle-native.json','findingarticle-lifecycle.json']:
  data=ps(owned+"Get-Content -Raw (Join-Path $app "+q(name)+");",retry_read=True);json.loads(data);(out/name).write_text(data)
 for name in ['findingarticle-en-326-unknown.png','findingarticle-ko-904-data.png']:
  data=ps(owned+"[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $app "+q(name)+")))");(out/name).write_bytes(base64.b64decode(data))
 print('Native geometry42 + lifecycle + PNGs saved')
elif a.action=='peer':
 data=ps(owned+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'FindingArticle'));if($null-eq $c){throw 'Owned article missing'};[pscustomobject]@{name=$c.Current.Name;type=$c.Current.ControlType.ProgrammaticName;focusable=$c.Current.IsKeyboardFocusable;children=@($c.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.IsContentElement-and $_.Current.IsControlElement}|ForEach-Object{[pscustomobject]@{name=$_.Current.Name;type=$_.Current.ControlType.ProgrammaticName}})}|ConvertTo-Json -Depth 5;")
 (r/'docs/replica/source/findingarticle-windows-peer.json').write_text(data);print(data)
else:
 print(ps(owned+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
 if native:print(ps(base+"[Console]::WriteLine(@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')}).Count)"))
