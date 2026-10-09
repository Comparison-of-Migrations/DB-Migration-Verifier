"""Finite original Edge horizontal input batch, unique helper/runtime and fresh individual evidence."""
from pathlib import Path
import argparse,hashlib,json,time,uuid,concurrent.futures
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('action',choices=['open','batch','close','cleanup','geometry-open','geometry-record','geometry-close','extra','focus']);p.add_argument('--stage',type=Path);p.add_argument('--focus-only',action='store_true');a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
title='CapabilityViewport · original isolation'if a.action.startswith('geometry')else'CapabilityViewport Input · original isolation';owned="$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+")-and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});";unique="if($w.Count-ne 1){throw 'One exact viewport source required'};$w=$w[0];"
out=r/'docs/replica/source/capabilityviewport-windows-input';out.mkdir(exist_ok=True)
if a.action in ['open','geometry-open']:
 page='capabilityviewport.html'if a.action=='geometry-open'else'capabilityviewport-input.html';print(ps(owned+"if($w.Count){throw 'Owned source already exists'};Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized','--force-device-scale-factor=2',"+q('http://10.211.55.2:8768/'+page+'?os=windows'+('&focus=1'if a.focus_only else''))+");"))
elif a.action in ['close','geometry-close']:print(ps(owned+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"))
elif a.action=='cleanup':
 data=json.loads(ps(owned+"@{sourceWindowCount=$w.Count}|ConvertTo-Json",retry_read=True));assert data['sourceWindowCount']==0;(out/'cleanup.json').write_text(json.dumps(data)+'\n');print(data)
elif a.action=='geometry-record':
 path=r/'docs/replica/source/preflight-capabilityviewport-windows-source.json';before=path.stat().st_mtime_ns if path.exists()else 0
 print(ps(owned+unique+"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'capabilityviewport-record'));if($null-eq $b){throw 'Record target missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();"));end=time.monotonic()+5
 while time.monotonic()<end:
  if path.exists()and path.stat().st_mtime_ns>before:break
  time.sleep(.05)
 else:raise RuntimeError('No fresh source geometry')
 d=json.loads(path.read_text());assert len(d['samples'])==72 and d['dpr']==2;(r/'docs/replica/source/capabilityviewport-windows-source.json').write_text(json.dumps(d,indent=2)+'\n');print('Windows original72 recorded')
else:
 f=r/'tools/WindowsControl/owned_capability_viewport_source.cs';served=r/'artifacts/replica/windows-download/preflight-capabilityviewport-source.cs';served.write_bytes(f.read_bytes());digest=hashlib.sha256(f.read_bytes()).hexdigest();nonce=uuid.uuid4().hex
 code="$folder=Join-Path $env:TEMP "+q('DBMV-Viewport-Source-'+nonce)+";New-Item -ItemType Directory $folder|Out-Null;$cs=Join-Path $folder 'source.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/preflight-capabilityviewport-source.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Owned helper hash mismatch'};$rect=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$rect.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location);[OwnedCapabilityViewportSource]::"+("Focus"if a.action=="focus"else"Extra"if a.action=="extra"else"Batch")+"("+q(nonce)+(",$app"if a.action=="focus"else'')+");"
 if a.action=='focus':
  assert a.stage;v=json.loads(a.stage.read_text());assert v['owner']=='preflight'and v['component']=='capabilityviewport';code="$app=Join-Path (Join-Path $env:TEMP "+q(v['guestFolder'])+") 'app';"+code
 ready=r/'docs/replica/source/preflight-capabilityviewport-windows-control-ready.json';inp=r/'docs/replica/source/preflight-capabilityviewport-windows-input.json';seen=set();labels={'right','left','down','up','page-down','page-up','home','end','space','shift-space','wheel-x','wheel-y','wheel-shift-y'}
 if a.action=='extra':labels={'reset-before-fit','fit-no-overflow','fit','child-toggle','child-focus','mount-detached','mount','track','hold-left','drag'}
 if a.action=='focus':labels={'pointer-restore','tab-viewport','keyboard-restore','tab-child'}
 with concurrent.futures.ThreadPoolExecutor(max_workers=1)as ex:
  future=ex.submit(ps,code,timeout=60)
  while not future.done():
   if ready.exists():
    v=json.loads(ready.read_text());label=v.get('label')
    if v.get('nonce')==nonce and label in labels-seen:
     value=json.loads(inp.read_text());assert value['dpr']==2 and(value['width']==478 or(label=='mount-detached'and value['width']==0)); (out/('source-'+label+'.json')).write_text(json.dumps(value,indent=2)+'\n');seen.add(label);print(label,'offset',value['offset'],'last',value.get('lastInput'),flush=True)
   time.sleep(.03)
  result=future.result();print(result);peers=[json.loads(line[5:])for line in result.splitlines()if line.startswith("PEER:")];
  if peers:(out/"source-focus-peers.json").write_text(json.dumps(peers,indent=2)+"\n")
 assert seen==labels,seen;(out/('extra-manifest.json'if a.action=='extra'else'batch-manifest.json')).write_text(json.dumps({'nonce':nonce,'helperHash':digest,'states':sorted(seen)},indent=2)+'\n')
