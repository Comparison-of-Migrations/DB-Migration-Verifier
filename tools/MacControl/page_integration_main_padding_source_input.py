"""Finite trusted source blank-padding scope; own private profile, never user Chrome."""
from pathlib import Path
import json,subprocess,time,uuid,hashlib
r=Path(__file__).resolve().parents[2];assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='preflight'
actor=r/'artifacts/replica/workers/preflight/mainpadding/native-input';chrome=Path('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome')
base=r/'artifacts/replica/workers/preflight/mainpadding-source-native'/uuid.uuid4().hex;base.mkdir(parents=True);checks=[]
for en in [False,True]:
 for width in [960,1280]:
  label=('en'if en else'ko')+'-'+str(width)
  if label=='ko-960':continue  # Already executed pair4c923a4: preserve, never replay.
  out=base/label;out.mkdir();nonce=uuid.uuid4().hex;profile=out/'chrome-profile';launch=out/'launch.json';ready=out/'source-ready.json'
  url='http://10.211.55.2:8768/preflight-mainpadding.html?os=mac&interactive=1&lang='+('en'if en else'ko')+'&width='+str(width)+'&recording='+nonce
  command=[str(chrome),'--force-renderer-accessibility','--no-first-run','--no-default-browser-check','--user-data-dir='+str(profile),'--new-window','--window-size=1400,1050',url]
  log=(out/'chrome.log').open('w');proc=subprocess.Popen(command,stdout=log,stderr=log);launch.write_text(json.dumps({'pid':proc.pid,'nonce':nonce,'profile':str(profile),'command':command,'url':url},indent=2)+'\n');success=False
  def actor_run(mode):
   result=subprocess.run([str(actor),str(proc.pid),'source',mode,str(launch),str(ready),label+'-'+mode,str(out)],text=True,capture_output=True,timeout=10);(out/(mode+'-actor.log')).write_text(result.stdout+result.stderr);assert result.returncode==0,result.stderr
  def wait_file(name,predicate):
   p=r/'docs/replica/source'/name;end=time.monotonic()+30
   while time.monotonic()<end:
    assert proc.poll()is None,'Owned source exited before observation'
    try:
     value=json.loads(p.read_text())
     if value.get('recording')==nonce and predicate(value):return value
    except(FileNotFoundError,json.JSONDecodeError):pass
    time.sleep(.05)
   raise RuntimeError('Fresh source observation absent; no replay')
  try:
   value=wait_file('preflight-mainpadding-mac-control-ready.json',lambda s:len(s['rows'])==1);ready.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n');actor_run('inspect');actor_run('wheel')
   wheel=wait_file('preflight-mainpadding-mac-input.json',lambda s:any(e['type']=='wheel'and 'after'in e for e in s['events']));(out/'wheel-observation.json').write_text(json.dumps(wheel,ensure_ascii=False,indent=2)+'\n');assert all(e['isTrusted']for e in wheel['events']);event=next(e for e in wheel['events']if e['type']=='wheel');assert event['after']['main']['scrollTop']>0
   actor_run('click');click=wait_file('preflight-mainpadding-mac-input.json',lambda s:any(e['type']=='click'for e in s['events'])and any(e['type']=='pointerup'and 'after'in e for e in s['events']));(out/'click-observation.json').write_text(json.dumps(click,ensure_ascii=False,indent=2)+'\n');assert all(e['isTrusted']for e in click['events']);checks.append({'id':label,'scrollTop':event['after']['main']['scrollTop'],'focusAfterWheel':event['after']['focus'],'focusAfterClick':next(e['after']['focus']for e in click['events']if e['type']=='pointerup'),'wheel':str(out/'wheel-observation.json'),'click':str(out/'click-observation.json')});success=True
  finally:
   if proc.poll()is None:
    try:actor_run('close')
    except Exception as e:(out/'close-diagnostic.json').write_text(json.dumps({'exception':str(e),'scope':'Exact source profile termination still guarded below'}))
    command_now=subprocess.run(['ps','-p',str(proc.pid),'-o','command='],capture_output=True,text=True).stdout
    if proc.poll()is None:
     assert str(profile)in command_now and str(chrome)in command_now;proc.terminate();proc.wait(timeout=8)
   log.close();end=time.monotonic()+3
   while True:
    listing=subprocess.run(['ps','-axo','pid=,command='],capture_output=True,text=True,check=True).stdout;own=[l for l in listing.splitlines()if str(profile)in l and 'Google Chrome'in l]
    if not own or time.monotonic()>end:break
    time.sleep(.05)
   (out/'cleanup.json').write_text(json.dumps({'pid':proc.pid,'sourceWindow':0,'sourceBrowserProcess':0 if proc.poll()is not None else 1,'ownedProfileProcesses':len(own),'success':success},indent=2)+'\n');assert not own and proc.poll()is not None
(base/'comparison.json').write_text(json.dumps({'scope':'New blank left padding trusted CGEvent remaining source3; KO960 original4c923a4 pair reused; original controlled setup, actual physical wheel/click only','checks':checks},ensure_ascii=False,indent=2)+'\n');print(base)
