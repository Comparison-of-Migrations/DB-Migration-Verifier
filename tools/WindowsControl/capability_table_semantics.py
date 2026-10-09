"""Finite original table Raw/ControlView observation, no component input replay."""
from pathlib import Path
import base64,hashlib,json,uuid
from driver import ps,q
r=Path(__file__).resolve().parents[2]
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
nonce=uuid.uuid4().hex;out=r/'artifacts/replica/workers/preflight/capabilitytable/source-semantics'/nonce;out.mkdir(parents=True)
served=r/'artifacts/replica/windows-download';name='preflight-capabilitytable-semantic-'+nonce+'.html'
old=(served/'capabilitytable-input.html').read_text();title='CapabilityTable Input · original isolation · '+nonce
html=old.replace('<title>CapabilityTable Input · original isolation</title>','<title>'+title+'</title>');assert html!=old
(served/name).write_text(html)
(out/'source-fixture.json').write_text(json.dumps({'fixture':name,'sha256':hashlib.sha256(html.encode()).hexdigest(),'scope':'KOEN original Raw/ControlView only; title-only isolation, literal content/styles/cases unchanged'},ensure_ascii=False,indent=2)+'\n')
base="$out=Join-Path $env:TEMP "+q('DBMV-CapabilityTableSource-'+nonce)+";"
find="function OwnedWindows{@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+")-and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')})};"
code=base+find+"$ProgressPreference='SilentlyContinue';if(@(OwnedWindows).Count){throw 'Own source already open'};New-Item -ItemType Directory $out|Out-Null;"
code+="function Save($name,$value){[IO.File]::WriteAllText((Join-Path $out ($name+'.json')),($value|ConvertTo-Json -Depth 30 -Compress),[Text.UTF8Encoding]::new($false))};"
code+="function WalkerTree($e,$walker,$d=0){if($d-gt 7){throw 'Unexpected Table depth'};$b=$e.Current.BoundingRectangle;$child=$walker.GetFirstChild($e);$children=@();$seen=@{};for($i=0;$null-ne $child-and $i-lt 12;$i++){$key=[string]::Join(',', $child.GetRuntimeId());if($seen.ContainsKey($key)){throw 'Repeated Table sibling'};$seen[$key]=$true;$children+=,(WalkerTree $child $walker ($d+1));$child=$walker.GetNextSibling($child)};if($null-ne $child){throw 'Unexpected Table child count'};return @{id=$e.Current.AutomationId;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;control=$e.Current.IsControlElement;content=$e.Current.IsContentElement;box=@{x=$b.X;y=$b.Y;width=$b.Width;height=$b.Height};children=$children}}\nfunction RawTree($e){WalkerTree $e ([System.Windows.Automation.TreeWalker]::RawViewWalker)}\nfunction ControlTree($e){WalkerTree $e ([System.Windows.Automation.TreeWalker]::ControlViewWalker)}\n"
code+="Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized','--force-device-scale-factor=2',"+q('http://10.211.55.2:8768/'+name+'?os=windows')+");$w=$null;try{"
code+="$until=[DateTime]::UtcNow.AddSeconds(8);do{$wins=@(OwnedWindows);if($wins.Count-eq 1){$w=$wins[0];$table=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CapabilityTable-source'));if($null-ne $table){break}};Start-Sleep -Milliseconds 80}while([DateTime]::UtcNow-lt $until);if($null-eq $table){throw 'Fresh original Table absent'};"
code+="Save 'source-raw-ko' (RawTree $table);Save 'source-control-ko' (ControlTree $table);for($i=0;$i-lt 3;$i++){$next=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CapabilityTable-next'));$next.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep -Milliseconds 150};"
code+="$table=$w.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CapabilityTable-source'));Save 'source-raw-en' (RawTree $table);Save 'source-control-en' (ControlTree $table);"
code+="}finally{if($null-ne $w){$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()};$until=[DateTime]::UtcNow.AddSeconds(4);do{$left=@(OwnedWindows);if(!$left.Count){break};Start-Sleep -Milliseconds 80}while([DateTime]::UtcNow-lt $until);Save 'cleanup' @{sourceWindow=$left.Count}};[Console]::WriteLine('Source KOEN trees saved and own window closed')"
(out/'command.json').write_text(json.dumps({'sourceFixture':name,'guestOutput':'DBMV-CapabilityTableSource-'+nonce},indent=2)+'\n')
try:(out/'command-result.txt').write_text(ps(code,timeout=35))
except Exception as e:(out/'transport-error.txt').write_text(str(e));print(out);raise
names=['source-raw-ko.json','source-control-ko.json','source-raw-en.json','source-control-en.json','cleanup.json']
read=base+"foreach($name in @("+','.join(q(n)for n in names)+")){[Console]::WriteLine($name);[Console]::WriteLine([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $out $name))))}"
lines=ps(read,timeout=20,retry_read=True).splitlines();assert len(lines)==len(names)*2
for i,name in enumerate(names):assert lines[i*2]==name;(out/name).write_bytes(base64.b64decode(lines[i*2+1]))
assert json.loads((out/'cleanup.json').read_text())=={'sourceWindow':0}
def walk(node):
 yield node
 for child in node['children']:yield from walk(child)
for lang,header in [('ko','검사 분류'),('en','Category')]:
 for view in ['raw','control']:
  v=json.loads((out/('source-'+view+'-'+lang+'.json')).read_text());assert v['type']=='ControlType.Table' and any(n['name']==header for n in walk(v))
print(out);print('Original KOEN Raw/ControlView2 and cleanup0')
