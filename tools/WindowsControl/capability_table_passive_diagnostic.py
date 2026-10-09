"""Affected parent scope only: one passive click, three post states and finite UIA tree.
Each completed step is saved before the next step; uncertain input is never replayed.
"""
from pathlib import Path
import argparse,base64,hashlib,json,re,uuid
from driver import ps,q
r=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--stage',type=Path,required=True);a=p.parse_args()
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
v=json.loads(a.stage.read_text());assert v['owner']=='preflight' and v['component']=='capabilitytable' and v['selfContained'] and re.fullmatch(r'DBMV-Isolated-preflight-capabilitytable-[0-9a-f]{32}',v['guestFolder'])
nonce=uuid.uuid4().hex;out=a.stage.parent/'actual-windows-passive'/nonce;out.mkdir(parents=True)
served=r/'artifacts/replica/windows-download'
def serve(path,suffix):
 name='preflight-capabilitytable-'+nonce+'-'+suffix
 payload=path.read_bytes();(served/name).write_bytes(payload)
 return name,hashlib.sha256(payload).hexdigest().upper()
helper,hsha=serve(r/'tools/WindowsControl/owned_capability_table_input.cs','input.cs')
cases,csha=serve(r/'docs/replica/source/capabilitytable-input-cases.json','cases.json')
base="$base=Join-Path $env:TEMP "+q(v['guestFolder'])+";$app=Join-Path $base 'app';$out=Join-Path $app "+q('passive-'+nonce)+";"
find="function OwnedWindows{@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name-eq 'Replica CapabilityTable verification'-and (Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')})};"
code=base+find+r'''
$ProgressPreference='SilentlyContinue';if(@(OwnedWindows).Count){throw 'Owned Table already open'}
New-Item -ItemType Directory $out|Out-Null
function Save($name,$value){[IO.File]::WriteAllText((Join-Path $out ($name+'.json')),($value|ConvertTo-Json -Depth 30 -Compress),[Text.UTF8Encoding]::new($false))}
function State($expectedId){for($i=0;$i-lt 20;$i++){try{$stream=[IO.FileStream]::new(($app+'\capabilitytable-input.json'),[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite-bor [IO.FileShare]::Delete));$reader=[IO.StreamReader]::new($stream);try{$raw=$reader.ReadToEnd()}finally{$reader.Dispose()};$state=$raw|ConvertFrom-Json;if($state.id-eq $expectedId){return $state}}catch{};Start-Sleep -Milliseconds 25};throw ('Fresh record unavailable: '+$expectedId)}
function Find($id){$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$id))}
function Tree($e,$d=0){if($d-gt 7){throw 'Unexpected Table depth'};$b=$e.Current.BoundingRectangle;$children=@($e.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition));if($children.Count-gt 12){throw 'Unexpected Table child count'};return @{id=$e.Current.AutomationId;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;control=$e.Current.IsControlElement;content=$e.Current.IsContentElement;offscreen=$e.Current.IsOffscreen;box=@{x=$b.X;y=$b.Y;width=$b.Width;height=$b.Height};children=@($children|ForEach-Object{Tree $_ ($d+1)})}}
function ControlTree($e,$d=0){if($d-gt 7){throw 'Unexpected control Table depth'};$b=$e.Current.BoundingRectangle;$walker=[System.Windows.Automation.TreeWalker]::ControlViewWalker;$child=$walker.GetFirstChild($e);$children=@();$seen=@{};for($i=0;$null-ne $child-and $i-lt 12;$i++){$key=[string]::Join(',', $child.GetRuntimeId());if($seen.ContainsKey($key)){throw 'Repeated control sibling'};$seen[$key]=$true;$children+=,(ControlTree $child ($d+1));$child=$walker.GetNextSibling($child)};if($null-ne $child){throw 'Unexpected control child count'};return @{id=$e.Current.AutomationId;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;control=$e.Current.IsControlElement;content=$e.Current.IsContentElement;box=@{x=$b.X;y=$b.Y;width=$b.Width;height=$b.Height};children=$children}}
'''
for name,digest,target in [(cases,csha,'cases-passive.json'),(helper,hsha,'input-passive.cs')]:
 code+="Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+name)+" -OutFile ($app+'\\"+target+"');if((Get-FileHash ($app+'\\"+target+"') -Algorithm SHA256).Hash-ne "+q(digest)+"){throw 'Owned input hash mismatch'};"
code+=r'''
$rect=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle
Add-Type -Path ($app+'\input-passive.cs') -ReferencedAssemblies @('System.dll','System.Core.dll',$rect.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location)
$pidOwned=(Start-Process ($app+'\ReplicaProbe.exe') -ArgumentList @('--capabilitytable-window',($app+'\cases-passive.json'),$app) -PassThru).Id
$w=$null
try{
 $until=[DateTime]::UtcNow.AddSeconds(8);do{$wins=@(OwnedWindows);if($wins.Count-eq 1){break};Start-Sleep -Milliseconds 80}while([DateTime]::UtcNow-lt $until)
 if($wins.Count-ne 1){throw 'Owned Table not ready'};$w=$wins[0]
 $table=Find 'CapabilityTable-root';$button=Find 'CapabilityRow-permissions'
 if($null-eq $table-or $null-eq $button-or [Math]::Abs($table.Current.BoundingRectangle.Width/2-904)-gt .01){throw 'Exact Table geometry missing'}
 Save 'native-ax-ko' (Tree $table)
 Save 'native-control-ax-ko' (ControlTree $table)
 $button.SetFocus();Start-Sleep -Milliseconds 250;$before=State 'ko-904'
 if(!$before.buttonFocused-or $before.permissions-ne 0){throw 'Fresh passive predecessor absent'}
 Save 'native-passive-before' $before
 $null=[OwnedCapabilityTableInput]::Run($false,$app,'passive');$passive=State 'ko-904'
 Save 'native-passive' $passive
 [IO.File]::Copy(($app+'\capabilitytable-pointer-transport.txt'),($out+'\pointer-transport.txt'))
 [IO.File]::Copy(($app+'\capabilitytable-routed-input.json'),($out+'\routed-input.json'))
 foreach($step in @(@('en-narrow','en-478'),@('ko-narrow','ko-478'),@('en-wide','en-904'))){$label=$step[0];$expectedId=$step[1]
  (Find 'CapabilityTable-next').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
  Start-Sleep -Milliseconds 250;Save ('native-'+$label) (State $expectedId)
 }
 Save 'native-ax-en' (Tree (Find 'CapabilityTable-root'))
 Save 'native-control-ax-en' (ControlTree (Find 'CapabilityTable-root'))
 Save 'complete' @{steps=6;pid=$pidOwned}
}finally{
 if($null-ne $w){$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()}
 $until=[DateTime]::UtcNow.AddSeconds(5);do{$owned=@(Get-Process -Id $pidOwned -ErrorAction SilentlyContinue);if(!$owned.Count){break};Start-Sleep -Milliseconds 80}while([DateTime]::UtcNow-lt $until)
 Save 'cleanup' @{nativeWindow=@(OwnedWindows).Count;nativeProcess=$owned.Count}
}
[Console]::WriteLine('Completed unique passive scope; files saved before cleanup')
'''
(out/'command.json').write_text(json.dumps({'stage':str(a.stage),'scope':'new hitplane passive1/post narrow3/final parentAX KOEN only','guestOutput':'passive-'+nonce,'helperSha256':hsha,'casesSha256':csha},indent=2)+'\n')
try:
 raw=ps(code,timeout=55);(out/'command-result.txt').write_text(raw)
except Exception as e:
 (out/'transport-error.txt').write_text(str(e));print(out);raise
names=['native-ax-ko.json','native-control-ax-ko.json','native-passive-before.json','native-passive.json','pointer-transport.txt','routed-input.json','native-en-narrow.json','native-ko-narrow.json','native-en-wide.json','native-ax-en.json','native-control-ax-en.json','complete.json','cleanup.json']
read=base+"foreach($name in @("+','.join(q(n)for n in names)+")){[Console]::WriteLine($name);[Console]::WriteLine([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $out $name))))}"
lines=ps(read,timeout=20,retry_read=True).splitlines();assert len(lines)==len(names)*2
for i,name in enumerate(names):
 assert lines[i*2]==name
 (out/name).write_bytes(base64.b64decode(lines[i*2+1]))
before=json.loads((out/'native-passive-before.json').read_text());after=json.loads((out/'native-passive.json').read_text());cleanup=json.loads((out/'cleanup.json').read_text())
assert before['buttonFocused'] and not after['buttonFocused'] and after['permissions']==before['permissions']==0 and cleanup=={'nativeWindow':0,'nativeProcess':0}
print(out);print('New parent passive passed; exact cleanup0')
