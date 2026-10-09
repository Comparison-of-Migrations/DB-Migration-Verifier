"""Owned Row semantic-only processes; actual mode requires the Windows lease."""
from pathlib import Path
import argparse,json,re,uuid,hashlib
from driver import ps,q
r=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('mode',choices=['headless','actual']);p.add_argument('--stage',type=Path,required=True);a=p.parse_args();v=json.loads(a.stage.read_text())
assert v['owner']=='preflight' and v['component']=='capabilityrow' and v['selfContained'] and re.fullmatch(r'DBMV-Isolated-preflight-capabilityrow-[0-9a-f]{32}',v['guestFolder'])
if a.mode=='actual':assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
nonce=uuid.uuid4().hex;out=a.stage.parent/(a.mode+'-windows-semantic')/nonce;out.mkdir(parents=True)
f=r/'docs/replica/source/capabilityrow-table-semantic-source.json';name='preflight-capabilityrow-semantic-'+nonce+'.json';(r/'artifacts/replica/windows-download'/name).write_bytes(f.read_bytes());sha=hashlib.sha256(f.read_bytes()).hexdigest().upper()
base="$base=Join-Path $env:TEMP "+q(v['guestFolder'])+";$app=Join-Path $base 'app';$out=Join-Path $app "+q('semantic-'+nonce)+";"
setup=base+"New-Item -ItemType Directory $out|Out-Null;Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+name)+" -OutFile ($out+'\\cases.json');if((Get-FileHash ($out+'\\cases.json') -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Semantic input hash mismatch'};"
if a.mode=='headless':
 raw=ps(setup+"$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';& ($app+'\\ReplicaProbe.exe') '--capabilityrow' ($out+'\\cases.json') $out '--table-semantic-only';if($LASTEXITCODE-ne 0){throw 'Row semantic headless failed'};[Console]::WriteLine([IO.File]::ReadAllText($out+'\\capabilityrow-semantic-native.json'));",timeout=60)
 data=json.loads(raw[raw.index('{'):]);assert len(data['records'])==12 and data['detached'];(out/'capabilityrow-semantic-native.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
else:
 find=base+"$wins=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica CapabilityRow semantic verification'-and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')});"
 unique="if($wins.Count-ne 1){throw 'Unique owned Row semantic window required'};$w=$wins[0];if([Math]::Abs($w.Current.BoundingRectangle.Width-2100)-gt 30){throw 'Row semantic physical window geometry guard'};"
 print(ps(find+"if($wins.Count){throw 'Already open owned semantic window'};"+setup+"$p=Start-Process ($app+'\\ReplicaProbe.exe') -ArgumentList @('--capabilityrow-window',($out+'\\cases.json'),$out,'--table-semantic-only') -PassThru;$p.Id"))
 tree="function Tree($e,$depth=0){$b=$e.Current.BoundingRectangle;$children=@();if($depth-lt 9){$c=[System.Windows.Automation.TreeWalker]::ControlViewWalker.GetFirstChild($e);while($null-ne $c){$children+=Tree $c ($depth+1);$c=[System.Windows.Automation.TreeWalker]::ControlViewWalker.GetNextSibling($c)}};[ordered]@{id=$e.Current.AutomationId;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;box=@{x=$b.X;y=$b.Y;width=$b.Width;height=$b.Height};children=$children}};"
 read="[Console]::WriteLine((Tree $w|ConvertTo-Json -Depth 35 -Compress));"
 invoke=lambda id:"$b=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,"+q('CapabilityRow-semantic-'+id)+"));if($null-eq $b){throw 'Owned semantic utility missing'};$b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep -Milliseconds 250;"
 ready=ps(find+"$limit=[DateTime]::UtcNow.AddSeconds(8);while($wins.Count-ne 1-and [DateTime]::UtcNow-lt $limit){Start-Sleep -Milliseconds 80;"+find+"};"+unique+tree+read)
 (out/'native-ko.json').write_text(ready+'\n')
 for label,action in [('en',invoke('language')),('detached',invoke('mount')),('remount',invoke('mount'))]:
  raw=ps(find+unique+action+tree+read);(out/('native-'+label+'.json')).write_text(raw+'\n')
 print(ps(find+unique+"$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();Start-Sleep -Milliseconds 300;"+find+"$processes=@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')});@{nativeWindow=$wins.Count;nativeProcess=$processes.Count}|ConvertTo-Json -Compress"))
 (out/'cleanup.json').write_text(ps(find+"$processes=@(Get-Process -Name ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq ($app+'\\ReplicaProbe.exe')});@{nativeWindow=$wins.Count;nativeProcess=$processes.Count}|ConvertTo-Json -Compress",retry_read=True)+'\n')
if a.mode=='actual':
 def nodes(n):
  yield n
  for child in n.get('children',[]):yield from nodes(child)
 samples=[]
 for language in ['ko','en']:
  tree=json.loads((out/('native-'+language+'.json')).read_text());buttons=[n for n in nodes(tree)if n.get('id')=='CapabilityRow-permissions'];assert len(buttons)==1;samples.append({'language':language,'button':buttons[0],'source':next(x for x in json.loads(f.read_text())['rows']if x['id']==language+'-access')['cells'][3]['children'][0]})
 build=json.loads((a.stage.parent/'build.json').read_text());hashes={k:h for k,h in build['sourceHashes'].items()if k.endswith('/WebActionButton.cs')}
 (out/'capabilityrow-shared-button-parent-evidence.json').write_text(json.dumps({'changedButtonFileHashes':hashes,'buildManifest':str((a.stage.parent/'build.json').resolve()),'samples':samples,'passed':all(x['button']['name']==x['source']['name']and x['button']['type']=='ControlType.Button'and not x['button']['children']for x in samples)},ensure_ascii=False,indent=2)+'\n')
(out/'environment.json').write_text(json.dumps({'owner':'preflight','component':'capabilityrow','mode':a.mode,'stage':str(a.stage),'sourceSha256':sha,'scope':'KOEN32 Cell name/type/children + remount16; sharedButton descendants pending root; no geometry/input replay'},indent=2)+'\n');print(out)
