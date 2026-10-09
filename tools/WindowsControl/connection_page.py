"""Fixed project ConnectionPage source; approved Parallels CLI/UIA with path/title guards."""
from pathlib import Path
import argparse,json,time,hashlib,re
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['source-open','source-record','source-close','prepare','auto','launch','read','invoke','close','set','busy-input','source-input-open','source-input-record','source-input-close','source-input-set','source-input-invoke','source-busy-input']);p.add_argument('--target',choices=['language','mount','reduced','record','connected','advanced','connect','preflight','start','sql','postgres','mysql','maria'],default='record');p.add_argument('--field',choices=['source-host','source-db','source-user','source-port-editor','target-host','target-db','target-user','target-port-editor','source-port','target-port']);p.add_argument('--value',default='');p.add_argument('--label',default='read-final');p.add_argument('--scenario',choices=['normal','bad'],default='normal');p.add_argument('--resume',action='store_true');p.add_argument('--stage',type=Path);a=p.parse_args()
if json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']!='connection':raise RuntimeError('Connection Windows resource required')
if a.action in ['prepare','auto','launch','read','invoke','close','set','busy-input']:
 if not a.stage:raise RuntimeError('Exact owned isolated stage manifest required')
 stage=json.loads(a.stage.read_text())
 if stage['owner']!='connection'or stage['component']!='connectionpage'or not stage['selfContained']or not re.fullmatch(r'DBMV-Isolated-connection-connectionpage-[0-9a-f]{32}',stage['guestFolder']):raise RuntimeError('Owned full Connection stage required')
 base="$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';"
else:base=''

source="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('ConnectionPage · original isolation') -and (Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'});"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica ConnectionPage verification' -and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
inputsource=source.replace('ConnectionPage · original isolation','ConnectionPage input · original isolation')
unique="if($w.Count-ne 1){throw 'Unique owned ConnectionPage required'};$w=$w[0];"
if a.action=='source-open':print(ps(source+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/connectionpage.html?os=windows');"))
elif a.action=='source-record':
 out=r/'docs/replica/source/connection-connectionpage-windows-source.json';prior=out.stat().st_mtime_ns if out.exists()else 0
 print(ps(source+unique+"$deadline=[DateTime]::UtcNow.AddSeconds(8);do{$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'connectionpage-record'));if($null-ne $b){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);if($null-eq $b){throw 'Source record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();",retry_read=True))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists()and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh ConnectionPage source')
 v=json.loads(out.read_text());assert v['dpr']==2 and len(v['rows'])==96;(r/'docs/replica/source/connectionpage-windows-source.json').write_bytes(out.read_bytes());print('Fresh original96')
elif a.action=='prepare':raise RuntimeError('Use stage_isolated.py full cold package; legacy DLL patching is retired')
elif a.action=='auto':
 print(ps(base+"$f=Join-Path $app 'connectionpage-native.json';$prior=if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};& ($app+'\\ReplicaProbe.exe') '--connectionpage' ($app+'\\connectionpage-source.json') $app;if($LASTEXITCODE-ne 0){throw 'Owned ConnectionPage failed'};if((Get-Item $f).LastWriteTimeUtc.Ticks-le $prior){throw 'No fresh native ConnectionPage'};",timeout=60));value=ps(base+"Get-Content -Raw ($app+'\\connectionpage-native.json')",retry_read=True);v=json.loads(value);assert len(v['rows'])==96;out=r/'docs/replica/source/connectionpage-windows-native';out.mkdir(exist_ok=True);(out/'connectionpage-native.json').write_text(value+'\n');
 for filename in []:
  payload=ps(base+"Get-Content -Raw ($app+'\\"+filename+"')",retry_read=True);json.loads(payload);(out/filename).write_text(payload+'\n')
 import base64
 for filename in ['connectionpage.png','connectionpage-compact-ko-904-advanced.png','connectionpage-wide-en-904-connected.png']:
  png=ps(base+"[Convert]::ToBase64String([IO.File]::ReadAllBytes(($app+'\\"+filename+"')))",retry_read=True);(out/filename).write_bytes(base64.b64decode(png))
 print('Fresh native96')
elif a.action=='source-close':print(ps(source+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))

elif a.action=='launch':
 cases=r/'artifacts/replica/windows-download/connectionpage-source.json';digest=hashlib.sha256(cases.read_bytes()).hexdigest().upper();print(ps(base+"Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/connectionpage-source.json' -OutFile ($app+'\\connectionpage-source.json');if((Get-FileHash ($app+'\\connectionpage-source.json') -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned case hash mismatch'};Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--connectionpage-window',($app+'\\connectionpage-source.json'),$app"+(",'--resume'" if a.resume else '')+");"))
 print(ps("$deadline=[DateTime]::UtcNow.AddSeconds(8);do{"+find+"if($w.Count-eq 1){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow-lt $deadline);"+unique+"[Console]::WriteLine('Owned window ready');",retry_read=True))
elif a.action=='read':
 data=ps(find+unique+"Get-Content -Raw ($app+'\\connectionpage-input.json')",retry_read=True);value=json.loads(data);out=r/'docs/replica/source/connectionpage-windows-input';out.mkdir(exist_ok=True);(out/(a.label+'.json')).write_text(data+'\n');print(json.dumps({k:value[k]for k in ['view','focus','footer','opacity','advancedOpacity','attached']},ensure_ascii=False));print('history',len(value['history']))
elif a.action=='close':print(ps(find+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='invoke':
 target='dbms-'+str(['sql','postgres','mysql','maria'].index(a.target)) if a.target in ['sql','postgres','mysql','maria'] else 'advanced' if a.target=='advanced' else 'Footer-'+a.target if a.target in ['connect','preflight'] else 'ConnectionPage-'+a.target
 code=find+unique+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(target)+"));if($null-eq $c){throw 'Owned button missing'};$pattern=[System.Windows.Automation.InvokePattern]::Pattern;"
 if a.target in ['sql','postgres','mysql','maria']:code+="$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId-ne $w.Current.ProcessId){throw 'Owned radio focus guard failed'};$c.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select();"
 else:code+="$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId-ne $w.Current.ProcessId){throw 'Owned action focus guard failed'};$c.GetCurrentPattern($pattern).Invoke();"
 print(ps(code))
elif a.action=='set':
 if not a.field:raise RuntimeError('Fixed owned field required')
 code=find+unique+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(a.field)+"));if($null-eq $c){throw 'Owned field missing'};$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId-ne $w.Current.ProcessId){throw 'Owned input focus guard failed'};$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("+q(a.value)+");"
 print(ps(code))

if a.action=='source-input-open':print(ps(inputsource+"if($w.Count){throw 'Owned input source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/connectionpage-input.html?os=windows');"))
elif a.action=='source-input-close':print(ps(inputsource+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='source-input-record':
 out=r/'docs/replica/source/connection-connectionpage-windows-input.json';prior=out.stat().st_mtime_ns if out.exists()else 0
 print(ps(inputsource+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'connectionpage-input-record'));if($null-eq $b){throw 'Owned record missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
 deadline=time.monotonic()+8
 while time.monotonic()<deadline:
  if out.exists()and out.stat().st_mtime_ns>prior:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source input')
 v=json.loads(out.read_text());assert 'last'in v;target=r/'docs/replica/source/connectionpage-windows-source-input';target.mkdir(exist_ok=True);(target/(a.label+'.json')).write_bytes(out.read_bytes());print(v['last'])
elif a.action=='source-input-set':
 if not a.field:raise RuntimeError('Fixed source field required')
 print(ps(inputsource+unique+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q(a.field)+"));if($null-eq $c){throw 'Source field missing'};$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId-ne $w.Current.ProcessId){throw 'Source input focus guard failed'};$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("+q(a.value)+");"))
elif a.action=='source-input-invoke':
 names={'start':['새 검사 시작','Start a new check'],'connect':['두 연결 확인','Check both connections'],'preflight':['조회 범위 확인','Check visibility'],'advanced':['고급 연결 설정','Advanced connection settings'],'language':['영어로 변경','Switch to Korean']}
 if a.target in ['mount','reduced','record']:
  condition="[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('connectionpage-input-record'if a.target=='record'else'connectionpage-'+a.target)+")"
 elif a.target in names:
  condition="[System.Windows.Automation.OrCondition]::new("+','.join("[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,"+q(name)+")"for name in names[a.target])+")"
 else:raise RuntimeError('Fixed source button required')
 print(ps(inputsource+unique+"$items=@($w.FindAll([System.Windows.Automation.TreeScope]::Descendants,"+condition+"));if($items.Count-ne 1){throw 'Unique owned source button missing'};$c=$items[0];$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId-ne $w.Current.ProcessId){throw 'Owned source focus guard failed'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"))
elif a.action=='source-busy-input':
 code=inputsource+unique+"$name='두 연결 확인';$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$name));if($null-eq $c-or !$c.Current.IsEnabled){throw 'Owned source check missing'};$c.SetFocus();$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();$deadline=[DateTime]::UtcNow.AddMilliseconds(400);do{$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'확인 중…'));if($null-ne $c-and !$c.Current.IsEnabled){break}}while([DateTime]::UtcNow-lt $deadline);if($null-eq $c-or $c.Current.IsEnabled){throw 'Busy source not reached before bounded input'};$f=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'source-host'));if($null-eq $f){throw 'Owned source busy input missing'};$f.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId-ne $w.Current.ProcessId){throw 'Owned source busy focus guard failed'};$f.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("+q('bad.example.invalid'if a.scenario=='bad'else'source-busy.example.invalid')+");"
 print(ps(code))
elif a.action=='busy-input':
 code=find+unique+"$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Footer-connect'));if($null-eq $c){throw 'Owned connect missing'};$c.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();$deadline=[DateTime]::UtcNow.AddMilliseconds(400);do{$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Footer-connect'));if($null-ne $c-and !$c.Current.IsEnabled){break}}while([DateTime]::UtcNow-lt $deadline);if($null-eq $c-or $c.Current.IsEnabled){throw 'Busy state not reached before bounded input'};$f=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'source-host'));$f.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId-ne $w.Current.ProcessId){throw 'Owned busy input focus guard failed'};$f.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("+q('bad.example.invalid' if a.scenario=='bad' else 'source-busy.example.invalid')+");"
 print(ps(code))
