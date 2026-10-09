"""Actual source parentcallbacks4, controlled identity2 and optional passive1, fresh records copied immediately."""
from pathlib import Path
import argparse,json,re,time,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--stage',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight';v=json.loads(a.stage.read_text());assert v['owner']=='preflight'and v['component']=='preflightpage'and re.fullmatch(r'DBMV-Isolated-preflight-preflightpage-[0-9a-f]{32}',v['guestFolder']);a.output.mkdir(parents=True,exist_ok=True)
base="$app=Join-Path (Join-Path $env:TEMP "+q(v['guestFolder'])+") 'app';";guard="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith('PreflightPage Input · original isolation')-and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});";unique="if($w.Count-ne 1){throw 'Unique owned original required'};$w=$w[0];"
cs=r/'tools/WindowsControl/owned_preflight_page_input.cs';digest=hashlib.sha256(cs.read_bytes()).hexdigest();name='preflight-preflightpage-input-'+digest+'.cs';(r/'artifacts/replica/windows-download'/name).write_bytes(cs.read_bytes());dll='OwnedPreflightPageInput-'+digest+'.dll';init=base+"$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'owned-preflight-page-input.cs';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+name)+" -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Helper hash mismatch'};$rect=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$rect.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;"
endpoint=r/'docs/replica/source/preflight-preflightpage-windows-input.json'
def fresh(before,predicate=None):
 end=time.monotonic()+6
 while time.monotonic()<end:
  if endpoint.exists()and endpoint.stat().st_mtime_ns>before:
   try:
    value=json.loads(endpoint.read_text())
    if predicate is None or predicate(value):return value
   except json.JSONDecodeError:pass
  time.sleep(.06)
 raise RuntimeError('Fresh expected observation absent, do not repeat input')
prior=endpoint.stat().st_mtime_ns if endpoint.exists()else 0
print(ps(guard+"if($w.Count){throw 'Owned source already open'};Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized','--force-device-scale-factor=2','http://10.211.55.2:8768/preflightpage-input.html?os=windows');"));initial=fresh(prior,lambda x:x['requests']==[] and x['english']==False);(a.output/'source-initial.json').write_text(json.dumps(initial));assert initial['dpr']==2
try:
 for mode,label,predicate in [('pointer','permission-pointer',lambda x:x['requests']==['permissions']),('key','permission-key',lambda x:x['requests']==['permissions','permissions']),('back','back',lambda x:x['requests']==['permissions','permissions','back']),('choose','choose',lambda x:x['requests']==['permissions','permissions','back','choose']),('focus-permission','permission-before',lambda x:x['permissionFocused']),('language','permission-language',lambda x:x['permissionFocused']and x['english']),('focus-title','title-before',lambda x:x['headingFocused']),('language','title-language',lambda x:x['headingFocused']and not x['english']),('passive','optional-passive',lambda x:not x['headingFocused']and x['requests']==['permissions','permissions','back','choose']),('mount','detach',lambda x:not x['attached']),('mount','remount',lambda x:x['attached']and x['opacity']>=.999),('reduced','reduced',lambda x:x['reduced']and x['english']and x['opacity']>=.999)]:
  before=endpoint.stat().st_mtime_ns;response=ps(init+"[OwnedPreflightPageInput]::Run($true,$app,"+q(mode)+");",timeout=30);(a.output/('source-'+label+'-actor.txt')).write_text(response);value=fresh(before,predicate);(a.output/('source-'+label+'.json')).write_text(json.dumps(value,ensure_ascii=False));print(label+' saved',flush=True)
  if label in ['permission-language','optional-passive']:
   import subprocess,sys
   inspected=subprocess.run([sys.executable,str(r/'tools/WindowsControl/preflight_page_semantics.py'),'--stage',str(a.stage),'--output',str(a.output),'--source','--label',('en'if label=='permission-language'else'ko')],cwd=r,text=True,capture_output=True);(a.output/('source-'+('en'if label=='permission-language'else'ko')+'-peer-process.json')).write_text(json.dumps({'exitCode':inspected.returncode,'stdout':inspected.stdout,'stderr':inspected.stderr}));assert inspected.returncode==0,inspected.stderr

finally:
 print(ps(guard+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"));cleanup=json.loads(ps(guard+"@{sourceWindowCount=$w.Count}|ConvertTo-Json",retry_read=True));(a.output/'source-delta-cleanup.json').write_text(json.dumps(cleanup));assert cleanup['sourceWindowCount']==0
print('Source callbacks4/identity2/passive1/lifecycle3 saved, scoped window closed0')
