"""Bounded TextInput fixture control, explicitly authorized CLI + UIA only."""
import argparse,base64,json,subprocess,time,uuid,datetime
from pathlib import Path
from driver import ps,q,FIND,APP_FOLDER,CLI,VM,encoded_script
ROOT=Path(__file__).resolve().parents[2]
common="$probeRoot=Join-Path $env:TEMP "+q(APP_FOLDER)+";$probeApp=Join-Path $probeRoot 'app';"
owned=common+"$expected='Replica TextInput verification';"+FIND
p=argparse.ArgumentParser();p.add_argument('action',choices=['render','launch','read','close','set','disabled','key']);p.add_argument('--target',type=int,choices=range(3),default=0);p.add_argument('--text',choices=['orders','a  b  ','sample.example.invalid','마이그레이션 예시','long',''],default='orders');p.add_argument('--key',choices=['escape','enter','tab','shift-tab','left','right','select-all','backspace','f6','f7','f8','f9','f10','f11','f12'],default='escape');a=p.parse_args()
if a.text=='long':a.text='sample.'*15
if a.action=='render':
 data=ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--textinput',($probeApp+'\\textinput-cases.json'),$probeApp) -WorkingDirectory $probeApp -PassThru -Wait;if($p.ExitCode -ne 0){throw 'TextInput render failed'};[IO.File]::ReadAllText($probeApp+'\\textinput-native.json')",timeout=90)
 (ROOT/'docs/replica/source/textinput-windows-native.json').write_text(data)
 png=ps(common+"[Convert]::ToBase64String([IO.File]::ReadAllBytes($probeApp+'\\textinput-native-2x.png'))",retry_read=True)
 (ROOT/'docs/replica/screenshots/textinput-windows-native-2x.png').write_bytes(base64.b64decode(png));print('Windows TextInput render received')
elif a.action=='launch':print(ps(common+"$p=Start-Process ($probeApp+'\\ReplicaProbe.exe') -ArgumentList @('--textinput-window',$probeApp) -WorkingDirectory $probeApp -PassThru;[Console]::WriteLine($p.Id)"))
elif a.action=='close':print(ps(owned+"$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();[Console]::WriteLine('Owned TextInput window closed')"))
elif a.action=='read':
 data=ps(owned+"[IO.File]::ReadAllText($probeApp+'\\textinput-input.json')",retry_read=True);print(data)
else:
 target="$c=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('ReplicaTextInput'+str(a.target))+"));if($null -eq $c){throw 'Owned text input missing'};"
 before=json.loads(ps(owned+"[IO.File]::ReadAllText($probeApp+'\\textinput-input.json')",retry_read=True))
 if a.action in ('set','disabled'):
  code=owned+target
  if a.action=='disabled':
   assert a.target==2;code+="if($c.Current.IsEnabled){throw 'Expected disabled preview'};$refused=$false;try{$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('sample')}catch{$refused=$true};if(!$refused){throw 'Disabled value not refused'};[Console]::WriteLine('Disabled input refused')";print(ps(code));raise SystemExit
  print(ps(code+"$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("+q(a.text)+");[Console]::WriteLine('Sample input requested')"))
 else:
  nonce=str(uuid.uuid4());code=owned+target+"$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $window.Current.ProcessId){throw 'Wrong focus'};Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body "+q(json.dumps({'nonce':nonce}))+"|Out-Null;Start-Sleep -Seconds 5;"
  encoded,_=encoded_script(code);control=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
  ready=ROOT/'docs/replica/source/windows-control-ready.json';deadline=time.monotonic()+10
  while time.monotonic()<deadline:
   if ready.exists() and json.loads(ready.read_text()).get('nonce')==nonce:break
   if control.poll() is not None:raise RuntimeError(control.communicate())
   time.sleep(.05)
  else:control.terminate();raise RuntimeError('No verified focus; key blocked')
  option='--key' if a.key in ('left','right') else '--scancode';value={'escape':1,'enter':28,'tab':15,'shift-tab':15,'left':100,'right':102,'select-all':30,'backspace':14,'f6':64,'f7':65,'f8':66,'f9':67,'f10':68,'f11':87,'f12':88}[a.key]
  def send(opt,code,event):subprocess.run([CLI,'send-key-event',VM,opt,str(code),'--event',event],check=True,capture_output=True,timeout=10)
  try:
   if a.key in ('select-all','shift-tab'):send('--scancode',29 if a.key=='select-all' else 42,'press')
   try:send(option,value,'press')
   finally:send(option,value,'release')
  finally:
   if a.key in ('select-all','shift-tab'):send('--scancode',29 if a.key=='select-all' else 42,'release')
  out,err=control.communicate(timeout=10)
  if control.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
 # Read actual state including no-op keys; only edits promise a fresh file.
 snapshot=('textinput-keyup-Tab-'+('Shift' if a.key=='shift-tab' else 'None')+'.json') if a.action=='key' and a.key in ('tab','shift-tab') else 'textinput-state-'+a.key.upper()+'.json' if a.action=='key' and a.key.startswith('f') and a.key!='f12' else 'textinput-input.json'
 data=ps(owned+"[IO.File]::ReadAllText($probeApp+'\\"+snapshot+"')",retry_read=True);record=json.loads(data)
 if a.action=='set':assert record['rows'][a.target]['text']==a.text
 if a.action=='key':
  expected={'escape':'Escape','enter':'Enter','tab':'Tab','shift-tab':'Tab','left':'Left','right':'Right','select-all':'A','backspace':'Back','f6':'F6','f7':'F7','f8':'F8','f9':'F9','f10':'F10','f11':'F11','f12':'F12'}[a.key]
  fresh=record['keys'][len(before['keys']):]
  assert any(k['key']==expected and k['eventType']=='KeyDown' for k in fresh),(expected,fresh)
  g=record['rows'][0]['geometry']
  if a.key=='f6':assert g['width']==238
  if a.key=='f7':assert g['width']==366
  if a.key=='f8':assert g['reduced']!=before['rows'][0]['geometry']['reduced']
  if a.key in ('f9','f10'):assert g['preedit']=={'f9':'ㄱ','f10':'가'}[a.key] and record['rows'][0]['text']==before['rows'][0]['text']
  if a.key=='f11':assert g['preedit'] is None
 if a.action=='key' and a.key=='escape' and a.target==1:assert record['rows'][1]['text']=='' and record['rows'][1]['focused']
 if a.action=='key' and a.key=='select-all':assert record['rows'][a.target]['selectionStart']==0 and record['rows'][a.target]['selectionEnd']==len(record['rows'][a.target]['text'] or '')
 if a.action=='key' and a.key=='f12':
  blink=ps(owned+"[IO.File]::ReadAllText($probeApp+'\\textinput-blink.json')",retry_read=True)
  b=json.loads(blink);assert len(b['samples'])==6 and all(x['focused'] for x in b['samples']) and b['utc']>before['utc']
  (ROOT/'docs/replica/source/textinput-windows-blink.json').write_text(blink)
 path=ROOT/f'docs/replica/source/textinput-windows-{a.action}-{a.key}-target-{a.target}.json'
 if path.exists():path.rename(path.with_name(path.stem+'-'+datetime.datetime.now(datetime.timezone.utc).strftime('%H%M%S%f')+'.json'))
 path.write_text(data);print('Verified owned input state:',[(r['id'],r['text'],r['focused'],r['selectionStart'],r['selectionEnd']) for r in record['rows']])
