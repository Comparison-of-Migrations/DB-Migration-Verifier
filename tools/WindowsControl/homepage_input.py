"""Owned Home source/native actual input; explicit Windows lease and fresh diagnostics."""
from pathlib import Path
import argparse,base64,hashlib,json,time
from driver import ps,q
def literal(value):return "[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String("+q(base64.b64encode(value.encode()).decode())+"))"
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['start','read','input','batch','neutral','inspect','close']);p.add_argument('--manifest',type=Path,required=True);p.add_argument('--source',action='store_true');p.add_argument('--resume-after',default='');p.add_argument('--target',choices=['start','open','sample','research','HomePage-toggle'],default='start');p.add_argument('--mode',choices=['pointer','enter','space','passive-help','passive-hint','passive-gap'],default='pointer');p.add_argument('--label',default='state');a=p.parse_args()
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home';m=json.loads(a.manifest.read_text());s=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='home' and m['component']=='homepage' and m['runtime']=='win-x64' and s['selfContained']
base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';$path=Join-Path $app 'ReplicaProbe.exe';$A=[System.Windows.Automation.AutomationElement];$T=[System.Windows.Automation.TreeScope];"
owned="$ws=$A::RootElement.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name-eq'Replica HomePage verification'-and(Get-Process -Id $_.Current.ProcessId).Path-eq$path});"
if a.source:owned="$ws=$A::RootElement.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition);$w=@($ws|Where-Object{$_.Current.Name.StartsWith('HomePage · original isolation')-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('\\Microsoft\\Edge\\Application\\msedge.exe')});"
guard=owned+"if($w.Count-ne1){throw 'Unique owned Home window required'};$w=$w[0];"
out=r/'docs/replica/source/homepage-windows-input';out.mkdir(exist_ok=True)
if a.action=='start':
 if a.source:
  print(ps(base+owned+"if($w.Count){throw 'Own source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','http://10.211.55.2:8768/homepage.html');"));raise SystemExit(0)
 f=r/'artifacts/replica/windows-download/homepage-cases.json';served='home-homepage-'+m['id']+'-input-cases.json' if 'id' in m else 'home-homepage-'+a.manifest.parent.name+'-input-cases.json';(f.parent/served).write_bytes(f.read_bytes());sha=hashlib.sha256(f.read_bytes()).hexdigest().upper()
 print(ps(base+owned+"if($w.Count){throw 'Own Home already open'};$ProgressPreference='SilentlyContinue';$cases=Join-Path $app 'homepage-input-cases.json';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+served)+" -OutFile $cases;if((Get-FileHash $cases -Algorithm SHA256).Hash-ne "+q(sha)+"-or(Get-FileHash ($app+'\\ReplicaProbe.dll') -Algorithm SHA256).Hash-ne "+q(m['binarySha256'].upper())+"){throw 'Own binary/cases mismatch'};Remove-Item Env:DBMV_HEADLESS,Env:DBMV_HEADLESS_SCALE -ErrorAction SilentlyContinue;Start-Process -FilePath $path -WorkingDirectory $app -ArgumentList @('--homepage-window','homepage-input-cases.json','.');"))
elif a.action=='close':print(ps(base+guard+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='inspect':
 data=ps(base+guard+"@($w.FindAll($T::Descendants,[System.Windows.Automation.Condition]::TrueCondition)|ForEach-Object{[pscustomobject]@{id=$_.Current.AutomationId;name=$_.Current.Name;type=$_.Current.ControlType.ProgrammaticName;focusable=$_.Current.IsKeyboardFocusable;control=$_.Current.IsControlElement;content=$_.Current.IsContentElement;box=$_.Current.BoundingRectangle.ToString()}})|ConvertTo-Json -Depth 4",retry_read=True);json.loads(data);(out/(('source-' if a.source else 'native-')+'ax-'+a.label+'.json')).write_text(data)
elif a.action=='read':
 assert not a.source;data=ps(base+"Get-Content -Raw ($app+'\\homepage-input.json')",retry_read=True);(out/(a.label+'.json')).write_text(data);v=json.loads(data);print({k:v[k] for k in ['actions','english','focused']})
else:
 helper=r/'tools/WindowsControl/homepage_input_mouse.cs';served=r/'artifacts/replica/windows-download/home-homepage-input-mouse.cs';served.write_bytes(helper.read_bytes());sha=hashlib.sha256(helper.read_bytes()).hexdigest().upper();dll='HomeInput-'+sha[:12]+'.dll'
 load="$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'home-homepage-input-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/home-homepage-input-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Input helper hash mismatch'};$r=$A::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll','System.Drawing.dll',$r.GetType().Assembly.Location,$A.Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;$px=0;$py=0;"
 mode=a.mode;passive=mode.startswith('passive-')
 if passive:
  if a.source:
   id='HomePage-help' if mode=='passive-help' else 'HomePage-hint';load+="$v=$w.FindAll($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,"+q(id)+"));if($v.Count-eq0){$v=$w.FindAll($T::Descendants,[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new($A::ControlTypeProperty,[System.Windows.Automation.ControlType]::Text),[System.Windows.Automation.OrCondition]::new([System.Windows.Automation.PropertyCondition]::new($A::NameProperty,"+literal(json.loads((r/'docs/replica/source/homepage-windows-source.json').read_text())['rows'][0]['help' if mode=='passive-help' else 'hint']['text'] if mode=='passive-help' else json.loads((r/'docs/replica/source/homepage-windows-source.json').read_text())['rows'][0]['hint']['text']['text'])+"),[System.Windows.Automation.PropertyCondition]::new($A::NameProperty,"+literal(json.loads((r/'docs/replica/source/homepage-windows-source.json').read_text())['rows'][8]['help']['text'] if mode=='passive-help' else json.loads((r/'docs/replica/source/homepage-windows-source.json').read_text())['rows'][8]['hint']['text']['text'])+"))))};if($v.Count-ne1){throw 'Unique passive source required'};$v=$v[0].Current.BoundingRectangle;$px=$v.Left+$v.Width/2;$py=$v.Top+$v.Height/2;"
   if mode=='passive-gap':load+="$b=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'start'));$py=$v.Bottom+12*($b.Current.BoundingRectangle.Height/52);"
  else:
   load+="$v=Get-Content -Raw ($app+'\\homepage-input.json')|ConvertFrom-Json;$b=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'start'));$br=$b.Current.BoundingRectangle;$scale=$br.Height/52;$x0=$br.Left-$scale*$v.row.buttons[0].box.x;$y0=$br.Top-$scale*$v.row.buttons[0].box.y;"
   if mode=='passive-help':load+="$rect=$v.row.help.box;"
   elif mode=='passive-hint':load+="$rect=$v.row.hint.text.box;"
   else:load+="$e=$v.row.entry;$rect=[pscustomobject]@{x=$e.x+$e.width/2;y=$e.y+$e.height-12;width=1;height=1};"
   load+="$px=$rect.x+$rect.width/2-$v.row.buttons[0].box.x;$py=$rect.y+$rect.height/2-$v.row.buttons[0].box.y;"
 mode=('passive' if a.source else 'passive-relative') if passive else mode;evidence=r/'docs/replica/source/home-homepage-windows-input.json';prior=evidence.stat().st_mtime_ns if evidence.exists() else 0
 if a.action=='neutral':
  assert not a.source
  code=base+guard+load+"$states=@();$progress=Join-Path $app 'homepage-neutral-progress.txt';$journal=Join-Path $app 'homepage-neutral-states.json';"
  def record(phase):
   return "[IO.File]::WriteAllText($progress,"+q(phase)+");$v=Get-Content -Raw ($app+'\\homepage-input.json')|ConvertFrom-Json;$states+=,[pscustomobject]@{phase="+q(phase)+";actions=$v.actions;english=$v.english;focused=$v.focused;pointerEvents=$v.pointerEvents};[IO.File]::WriteAllText($journal,($states|ConvertTo-Json -Depth 12));"
  for language in ['ko','en']:
   if language=='en':
    code+="$toggle=$w.FindFirst($T::Descendants,[System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty,'HomePage-toggle'));$toggle.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep -Milliseconds 120;"
    for target in ['start','open','sample','research']:
     for method in ['pointer','enter','space']:
      code+="[OwnedHomeInput]::Run($app,$false,"+q(target)+","+q(method)+",0,0);"+record(language+'-'+target+'-'+method)
   for target in ['help','hint','gap']:
    if language=='ko' and (a.resume_after=='ko-passive-gap' or (a.resume_after=='ko-passive-help' and target=='help')):continue
    code+="$v=Get-Content -Raw ($app+'\\homepage-input.json')|ConvertFrom-Json;"
    if target=='help':code+="$rect=$v.row.help.box;"
    elif target=='hint':code+="$rect=$v.row.hint.text.box;"
    else:code+="$e=$v.row.entry;$rect=[pscustomobject]@{x=$e.x+$e.width/2;y=$e.y+$e.height-12;width=1;height=1};"
    code+="$dx=$rect.x+$rect.width/2-$v.row.buttons[0].box.x;$dy=$rect.y+$rect.height/2-$v.row.buttons[0].box.y;[OwnedHomeInput]::Run($app,$false,'start','passive-relative',$dx,$dy);"+record(language+'-passive-'+target)
   for method in ['neutral-enter','neutral-space']:
    code+="[OwnedHomeInput]::Run($app,$false,'start',"+q(method)+",0,0);"+record(language+'-'+method)
  code+="$states|ConvertTo-Json -Depth 12;";data=ps(code,timeout=60);(out/'native-neutral-batch.json').write_text(data);v=json.loads(data);assert len(v)==({'ko-passive-help':21,'ko-passive-gap':19}.get(a.resume_after,22))
  expected=0
  for row in v:
   if row['phase'].startswith('en-') and 'passive' not in row['phase'] and 'neutral' not in row['phase']:
    expected+=1;assert row['actions'][-1]==row['phase'].split('-')[1]
   else:assert row['focused']=='home-body',row
   assert len(row['actions'])==expected,row
  print('Actual neutral passive6/keys4 and new ENactions12 passed');raise SystemExit(0)
 if a.action=='batch':
  code=base+guard+load+"$states=@();$progress=Join-Path $app "+q("homepage-batch-"+a.label+"-progress.txt")+";[IO.File]::WriteAllText($progress,'started');"
  for target in ['start','open','sample','research']:
   for method in ['pointer','enter','space']:
    code+="[OwnedHomeInput]::Run($app,$"+str(a.source).lower()+","+q(target)+","+q(method)+",0,0);[IO.File]::WriteAllText($progress,"+q(target+" "+method)+");"
    if not a.source:code+="$v=Get-Content -Raw ($app+'\\homepage-input.json')|ConvertFrom-Json;$states+=,[pscustomobject]@{target="+q(target)+";method="+q(method)+";actions=$v.actions;english=$v.english;focused=$v.focused};"
  if not a.source:code+="$states|ConvertTo-Json -Depth 5|Set-Content -Encoding UTF8 ($app+'\\homepage-batch-'+"+q(a.label)+"+'.json');"
  ps(code,timeout=60)
  if not a.source:
   states=ps(base+"Get-Content -Raw ($app+'\\homepage-batch-'+"+q(a.label)+"+'.json')",retry_read=True);(out/('batch-'+a.label+'.json')).write_text(states)
 else:
  immediate=ps(base+guard+load+"[OwnedHomeInput]::Run($app,$"+str(a.source).lower()+","+q(a.target)+","+q(mode)+",$px,$py);"+("Get-Content -Raw ($app+'\\homepage-input.json');" if not a.source else ""),timeout=30)
 if a.source:
  deadline=time.monotonic()+5
  while time.monotonic()<deadline:
   if evidence.exists() and evidence.stat().st_mtime_ns>prior:break
   time.sleep(.05)
  else:raise RuntimeError('No fresh source input diagnostic')
  data=evidence.read_text()
 else:data=immediate if a.action!='batch' else ps(base+"Get-Content -Raw ($app+'\\homepage-input.json')",retry_read=True)
 (out/(('source-' if a.source else 'native-')+a.label+'.json')).write_text(data);v=json.loads(data);print({k:v[k] for k in ['actions','english','focused']})
