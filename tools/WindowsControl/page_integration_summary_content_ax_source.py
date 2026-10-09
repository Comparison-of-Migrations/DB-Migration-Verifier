"""Exact original Edge ControlView4; source text grouping is observed, never inferred from DOM."""
from pathlib import Path
import argparse,hashlib,json,uuid
from driver import ps,q
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=['prepare','parse','run']);p.add_argument('manifest',nargs='?',type=Path);a=p.parse_args()
if a.action=='prepare':
 nonce=uuid.uuid4().hex;base=ROOT/'artifacts/replica/workers/home/pageintegrationsummarycontent-ax-source'/nonce;base.mkdir(parents=True)
 template=(ROOT/'artifacts/replica/windows-download/home-pageintegrationsummarycontent-ax.html').read_text();contexts=[]
 for lang in ['ko','en']:
  for width in [960,1280]:
   ident=lang+'-'+str(width);title='Original summary AX oracle - home '+nonce+' '+ident;name='home-summary-ax-'+nonce+'-'+ident+'.html';data=template.replace('Original summary AX oracle · home',title)
   f=ROOT/'artifacts/replica/windows-download'/name;f.write_text(data);url='http://10.211.55.2:8768/'+name+'?recording='+nonce+'&lang='+lang+'&width='+str(width)
   contexts.append({'id':ident,'language':lang,'width':width,'title':title,'url':url,'fixtureSha256':hashlib.sha256(f.read_bytes()).hexdigest(),'ready':'Ready original summary AX '+lang+' '+str(width)})
 m={'nonce':nonce,'contexts':contexts,'originalHashes':json.loads((ROOT/'docs/replica/source/home-pageintegrationsummarycontent-fixture.json').read_text())['originalHashes'],'guard':'exact nonce title/URL/Edge executable; bounded ControlView; WindowPattern.Close finally; no pointer/key/native candidate'};(base/'manifest.json').write_text(json.dumps(m,indent=2)+'\n');print(base/'manifest.json');raise SystemExit
assert a.manifest and json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home'
m=json.loads(a.manifest.read_text());base=a.manifest.parent
code=r'''
$ProgressPreference='SilentlyContinue';
function FindOwn($title,$url){
 $all=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition);$found=@();
 foreach($w in $all){if(!$w.Current.Name.StartsWith($title)){continue};$process=Get-Process -Id $w.Current.ProcessId;if($process.ProcessName-ne'msedge'-or!$process.Path.EndsWith('\msedge.exe',[StringComparison]::OrdinalIgnoreCase)){throw 'Exact source Edge executable required'};$address=$false;foreach($n in $w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)){$pattern=$null;if($n.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$pattern)){if($pattern.Current.Value-eq$url-or$pattern.Current.Value-eq$url.Replace('http://','')){$address=$true;break}}};if(!$address){throw 'Exact nonce source URL required'};$found+=@($w)}
 if($found.Count-gt1){throw 'Unique owned source window required'};if($found.Count-eq1){return $found[0]};return $null
}
function WalkOwn($node,$parent,$depth){
 if($depth-gt35-or$script:nodes.Count-gt3000){throw 'Bounded owned source ControlView required'}
 $v=$node.Current;$id=$script:nodes.Count;$b=$v.BoundingRectangle
 $script:nodes.Add([pscustomobject]@{id=$id;parent=$parent;depth=$depth;name=$v.Name;type=$v.ControlType.ProgrammaticName;localizedType=$v.LocalizedControlType;class=$v.ClassName;automationId=$v.AutomationId;control=$v.IsControlElement;content=$v.IsContentElement;offscreen=$v.IsOffscreen;box=[pscustomobject]@{x=$b.X;y=$b.Y;width=$b.Width;height=$b.Height}})
 $walker=[System.Windows.Automation.TreeWalker]::ControlViewWalker;$child=$walker.GetFirstChild($node)
 while($null-ne$child){WalkOwn $child $id ($depth+1);$child=$walker.GetNextSibling($child)}
}
CONTEXTS
foreach($context in $contexts){
 $window=$null;$started=$false
 try{
  if($null-ne(FindOwn $context.title $context.url)){throw 'Owned source already live'}
  Start-Process msedge.exe -ArgumentList @('--new-window',$context.url);$started=$true
  $end=[DateTime]::UtcNow.AddSeconds(10);do{Start-Sleep -Milliseconds 150;$window=FindOwn $context.title $context.url}while($null-eq$window-and[DateTime]::UtcNow-lt$end)
  if($null-eq$window){throw 'Owned source not found; no relaunch'}
  $condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$context.ready)
  $end=[DateTime]::UtcNow.AddSeconds(6);do{Start-Sleep -Milliseconds 100;$ready=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)}while($null-eq$ready-and[DateTime]::UtcNow-lt$end)
  if($null-eq$ready){throw 'Finite original source not ready'}
  $script:nodes=[System.Collections.Generic.List[object]]::new();WalkOwn $window -1 0
  [pscustomobject]@{kind='source';id=$context.id;title=$window.Current.Name;pid=$window.Current.ProcessId;hwnd=$window.Current.NativeWindowHandle;url=$context.url;nodes=$script:nodes}|ConvertTo-Json -Depth 15 -Compress
 }finally{
  if($started){if($null-eq$window){$window=FindOwn $context.title $context.url};if($null-ne$window){$pidOwned=$window.Current.ProcessId;$hwnd=$window.Current.NativeWindowHandle;$guard=FindOwn $context.title $context.url;if($null-eq$guard-or$guard.Current.NativeWindowHandle-ne$hwnd-or$guard.Current.ProcessId-ne$pidOwned){throw 'Source close identity guard'};([System.Windows.Automation.WindowPattern]$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)).Close();Start-Sleep -Milliseconds 200;$remaining=FindOwn $context.title $context.url;[pscustomobject]@{kind='cleanup';id=$context.id;pid=$pidOwned;hwnd=$hwnd;ownedSourceWindow=[int]($null-ne$remaining);pointerKeyInput=0}|ConvertTo-Json -Compress;if($null-ne$remaining){throw 'Owned source remains'}}else{throw 'Source launch unknown; preserve and readonly inventory before any close'}}
 }
}
'''.replace('CONTEXTS','$contexts='+q(json.dumps(m['contexts']))+'|ConvertFrom-Json;')
f=ROOT/'artifacts/replica/windows-download'/('home-summary-ax-source-'+m['nonce']+'-command.txt');f.write_text(code);sha=hashlib.sha256(f.read_bytes()).hexdigest().upper();loader="$ProgressPreference='SilentlyContinue';$f=Join-Path $env:TEMP "+q(f.name)+";try{Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+f.name)+" -OutFile $f;if((Get-FileHash $f -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Source command hash mismatch'};& ([scriptblock]::Create([IO.File]::ReadAllText($f,[Text.Encoding]::UTF8)))}finally{Remove-Item $f -ErrorAction SilentlyContinue};"
if a.action=='parse':
 loader=loader.replace('& ([scriptblock]::Create([IO.File]::ReadAllText($f,[Text.Encoding]::UTF8)))',"$tokens=$null;$errors=$null;$null=[System.Management.Automation.Language.Parser]::ParseInput([IO.File]::ReadAllText($f,[Text.Encoding]::UTF8),[ref]$tokens,[ref]$errors);if($errors.Count){throw ($errors|Out-String)};[pscustomobject]@{phase='SCRIPT_PARSED';sourceLaunchPerformed=$false}|ConvertTo-Json -Compress")
 result=ps(loader,timeout=15);(base/'parse.json').write_text(result+'\n');print(result);raise SystemExit
try:result=ps(loader,timeout=50)
except Exception as e:(base/'failure.json').write_text(json.dumps({'phase':'UNVERIFIED','exception':str(e),'commandSha256':sha,'replayedInput':False,'next':'fresh exactnonce inventory; no relaunch'},indent=2)+'\n');raise
(base/'transport.log').write_text(result+'\n');rows=[];clean=[]
for line in result.splitlines():
 try:r=json.loads(line)
 except json.JSONDecodeError:continue
 if r.get('kind')=='source':rows.append(r)
 if r.get('kind')=='cleanup':clean.append(r)
(base/'source-controlview.json').write_text(json.dumps({'phase':'SOURCE_RECORDED','scope':'Exact Edge original ControlView grouping4','originalHashes':m['originalHashes'],'rows':rows},ensure_ascii=False,indent=2)+'\n');(base/'cleanup.json').write_text(json.dumps(clean,indent=2)+'\n');assert len(rows)==len(clean)==4 and all(r['ownedSourceWindow']==0 for r in clean);print('Recorded source ControlView4 / exactSourceWindow0:',base)
