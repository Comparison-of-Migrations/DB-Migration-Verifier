"""Explicitly authorized SendInput for the two owned NumberInput fixtures only."""
import argparse,ast,hashlib,json,subprocess,time,uuid
from pathlib import Path
from driver import ps,q,CLI,VM,FIND,APP_FOLDER,encoded_script
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('target',choices=['source','native']);p.add_argument('--mode',choices=['inspect','hover','click','hold','refresh'],default='inspect');p.add_argument('--half',choices=['up','down'],default='up');p.add_argument('--check-length',action='store_true');a=p.parse_args()
if a.target=='source':
 tree=ast.parse((ROOT/'tools/WindowsControl/number_inputs.py').read_text())
 guard=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='FIND' for t in n.targets))
else:
 guard="$probeApp=Join-Path (Join-Path $env:TEMP "+q(APP_FOLDER)+") 'app';$expected='Replica NumberInput verification';"+FIND+"$w=$window;"
field="$c=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'number-default'));if($null -eq $c){throw 'Owned port input missing'};$r=$c.Current.BoundingRectangle;$scale=$r.Width/112;if([Math]::Abs($r.Height/44-$scale) -gt .01){throw 'Unexpected owned field geometry'};"
if a.mode=='inspect':
 print(ps(guard+field+"[pscustomobject]@{title=$w.Current.Name;id=$c.Current.AutomationId;rect=$r;scale=$scale;mouseApi='Windows official SendInput, explicitly authorized 2026-10-06'}|ConvertTo-Json -Depth 5;",retry_read=True));raise SystemExit
if a.mode=='refresh' and a.target!='source':p.error('Refresh is source-fixture-only')
mouse_assembly='OwnedNumberMouse-'+hashlib.sha256((ROOT/'tools/WindowsControl/owned_number_mouse.cs').read_bytes()).hexdigest()[:12]+'.dll'
source_record=ROOT/'docs/replica/source/windows-source-numberinput.json'
source_stamp=source_record.stat().st_mtime_ns if source_record.exists() else 0
native_stamp=ps(guard+"$f=Join-Path $probeApp 'numberinput-pointer.json';if(Test-Path $f){(Get-Item $f).LastWriteTimeUtc.Ticks}else{0};",retry_read=True) if a.target=='native' and a.mode in ('hold','click') else None
nonce=str(uuid.uuid4());ready=ROOT/'docs/replica/source/windows-control-ready.json'
def post(phase):
 return "Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/evidence/control-ready.json' -Method POST -ContentType 'application/json' -Body (ConvertTo-Json -InputObject ([pscustomobject]@{nonce="+q(nonce)+";phase="+q(phase)+";rect=$r;scale=$scale}))|Out-Null;"
code=guard+field+"$c.SetFocus();if([System.Windows.Automation.AutomationElement]::FocusedElement.Current.ProcessId -ne $w.Current.ProcessId){throw 'Wrong owned focus'};"
if a.mode=='refresh':code+=post('refresh-ready')+"Start-Sleep -Seconds 4;"
else:code="$app=Join-Path (Join-Path $env:TEMP "+q(APP_FOLDER)+") 'app';Add-Type -Path (Join-Path $app "+q(mouse_assembly)+");[OwnedNumberFixture]::Run("+q(a.target)+","+q(a.mode)+","+q(a.half)+","+q(nonce)+");"
encoded,_=encoded_script(code)
if a.check_length:print({'code':len(code),'encoded':len(encoded)});raise SystemExit
proc=subprocess.Popen([CLI,'exec',VM,'--current-user','powershell.exe','-NoProfile','-NonInteractive','-WindowStyle','Hidden','-EncodedCommand',encoded],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
seen=set();deadline=time.monotonic()+25
while proc.poll() is None and time.monotonic()<deadline:
 try:d=json.loads(ready.read_text())
 except (FileNotFoundError,json.JSONDecodeError):d={}
 if d.get('nonce')==nonce and d['phase'] not in seen:
  phase=d['phase'];seen.add(phase)
  if phase=='refresh-ready':
   def key(scan,event):subprocess.run([CLI,'send-key-event',VM,'--scancode',str(scan),'--event',event],capture_output=True,check=True,timeout=5)
   try:
    key(29,'press')
    try:key(19,'press')
    finally:key(19,'release')
   finally:key(29,'release')
  elif phase in ('hover','pressed') and (a.mode!='click' or phase=='hover'):
   stem=f'numberinput-windows-{a.target}-{a.half}-{a.mode}-{phase}'
   (ROOT/'docs/replica/source'/f'{stem}.json').write_text(json.dumps(d,indent=2))
   subprocess.run([CLI,'capture',VM,'--file',str(ROOT/'artifacts/replica'/f'{stem}-full.png')],capture_output=True,check=True,timeout=10)
 time.sleep(.03)
try:out,err=proc.communicate(timeout=5)
except subprocess.TimeoutExpired:proc.terminate();raise RuntimeError('Owned pointer helper timeout')
if proc.returncode:raise RuntimeError((out+err).decode('utf8',errors='replace'))
if a.mode=='refresh':
 if 'refresh-ready' not in seen:raise RuntimeError('No guarded reload handshake')
 print('Owned source fixture reload key sent.');raise SystemExit
if a.target=='source':
 assert source_record.exists() and source_record.stat().st_mtime_ns>source_stamp,'No fresh owned source pointer record'
 data=json.loads(source_record.read_text())
 if a.mode in ('hold','click'):assert any(e['event']=='pointerup' and e['id']=='number-default' for e in data['events'])
else:
 if a.mode=='hover':
  data=json.loads(ps(guard+field+"[pscustomobject]@{rows=@([pscustomobject]@{value=$c.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value;rect=$r})}|ConvertTo-Json -Depth 5;",retry_read=True))
 else:data=json.loads(ps(guard+"$f=Join-Path $probeApp 'numberinput-pointer.json';if((Get-Item $f).LastWriteTimeUtc.Ticks -le "+str(int(native_stamp))+"){throw 'No fresh owned native pointer record'};Get-Content $f -Raw;",retry_read=True))
name=f'numberinput-windows-{a.target}-{a.half}-{a.mode}-result.json';(ROOT/'docs/replica/source'/name).write_text(json.dumps(data,indent=2)+'\n');print({'target':a.target,'mode':a.mode,'phases':sorted(seen),'value':data['rows'][0]['value']})
