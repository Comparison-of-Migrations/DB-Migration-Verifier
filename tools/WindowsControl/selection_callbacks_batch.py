"""Finite new Windows Selection callback scope; exact own runtime, no uncertain input replay."""
from pathlib import Path
import argparse, base64, hashlib, json, uuid
from driver import ps, q

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser(); p.add_argument('manifest', type=Path); p.add_argument('--plan-only', action='store_true'); a = p.parse_args()
m = json.loads(a.manifest.read_text()); stage = json.loads((a.manifest.parent/'windows-stage.json').read_text())
assert m['owner'] == stage['owner'] == 'preflight' and m['component'] == 'pageintegration' and m['buildExitCode'] == 0
assert json.loads((root/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner'] == 'preflight'
out = a.manifest.parent/'actual-selectioncallbacks-windows'; out.mkdir(exist_ok=True)
launch_file = out/'active-launch.json'
if launch_file.exists(): launch = json.loads(launch_file.read_text())
else:
    assert a.plan_only
    launch = {'binary':m['binarySha256'],'english':False,'pid':0,'output':'C:\\ProjectOwnedPlanOnly','path':'C:\\ProjectOwnedPlanOnly\\ReplicaProbe.exe'}
assert launch['binary'] == m['binarySha256']; english = launch['english']; nonce = uuid.uuid4().hex
served = root/'artifacts/replica/windows-download'
data = (root/'tools/WindowsControl/selection_callbacks_native.cs').read_bytes(); helper_name = 'preflight-selectioncallbacks-'+nonce+'-helper.txt'; (served/helper_name).write_bytes(data)
original = json.loads((root/'artifacts/replica/workers/preflight/selectioncallbacks/source/bcb156a1156d4681b0f59ee9741f5f4c/windows/original-windows.json').read_text())
source_rows = {r['id']:r for r in original['records'][1 if english else 0]['rows']}
code = "$ProgressPreference='SilentlyContinue';$app=Join-Path (Join-Path $env:TEMP "+q(stage['guestFolder'])+") 'app';$exe=Join-Path $app 'ReplicaProbe.exe';$out="+q(launch['output'])+";$pidOwned="+str(launch['pid'])+";$checks=[Collections.Generic.List[object]]::new();$failed=$null;"
code += "$helper=Join-Path $out 'actor-helper.txt';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+helper_name)+" -OutFile $helper;if((Get-FileHash $helper -Algorithm SHA256).Hash-ne "+q(hashlib.sha256(data).hexdigest().upper())+"){throw 'Owned helper hash guard'};Add-Type -TypeDefinition ([IO.File]::ReadAllText($helper,[Text.Encoding]::UTF8)) -ReferencedAssemblies UIAutomationClient,UIAutomationTypes,WindowsBase,System.Drawing;[OwnedSelectionCallbacksInput]::Init($exe,$pidOwned,"+q(m['binarySha256'])+");[OwnedSelectionCallbacksInput]::TransportPath=Join-Path $out 'physical-transport.txt';[IO.File]::WriteAllText([OwnedSelectionCallbacksInput]::TransportPath,'Fresh exact actor initialized before anyDown\n');"
code += r'''
function Latest { $file=Get-ChildItem $out -Filter 'selectioncallback-state-*.json'|Sort-Object Name|Select-Object -Last 1;if(!$file){throw 'Fresh immutable callback state absent'};return ([IO.File]::ReadAllText($file.FullName,[Text.Encoding]::UTF8)|ConvertFrom-Json) }
function WaitState([long]$after,[scriptblock]$predicate,[string]$label){$end=[DateTime]::UtcNow.AddSeconds(8);do{try{$s=Latest}catch{Start-Sleep -Milliseconds 40;continue};if($s.sequence-gt$after-and(&$predicate $s)){return $s};Start-Sleep -Milliseconds 40}while([DateTime]::UtcNow-lt$end);throw ('New callback absent '+$label+'; no replay')}
function EnsureVisible([string]$id){for($i=0;$i-lt4;$i++){$s=Latest;$c=@($s.controls|Where-Object{$_.id-eq$id});if($c.Count-ne1){throw ('Fresh observer unique target '+$id)};if($c[0].box.y-ge98-and($c[0].box.y+$c[0].box.height)-le($s.height-84)){return};[OwnedSelectionCallbacksInput]::Wheel($exe,($c[0].box.y-ge98));Start-Sleep -Milliseconds 140};throw ('Callback target remains outside main clip '+$id)}
function Step([string]$label,[string]$target,[string]$id,[string]$operation,[string]$value,[scriptblock]$predicate){
 $before=Latest;
 if($operation-eq'utility'){[OwnedSelectionCallbacksInput]::Utility($exe,$value)}
 else{
  if($id-and!$id.StartsWith('Footer-')-and!$id.StartsWith('MatchingDialog-')-and$id-ne'start'){EnsureVisible $id}
  $s=Latest;
  if($id){$c=@($s.controls|Where-Object{$_.id-eq$id})}else{$c=@($s.controls|Where-Object{$_.name-eq$value})}
  if($c.Count-ne1){throw ('Unique managed named callback target '+$label)}
  [OwnedSelectionCallbacksInput]::Run($exe,$false,$target,'click',$id,$c[0].name);
  if($operation-eq'edit'){[OwnedSelectionCallbacksInput]::ReplaceQuery($exe,$id,$value)}
 }
 $post=WaitState $before.sequence $predicate $label;
 $checks.Add(@{id=$label;sequence=$post.sequence;state=$post;passed=$true});
 @{scope='New parent physical callbacks; explicitly marked provider/API fixture utilities, old geometry and fullflow reused';checks=$checks}|ConvertTo-Json -Depth 50|Set-Content -Encoding UTF8 (Join-Path $out 'comparison.json');
}
try{
 WaitState -1 {param($s)$s.ready-and$s.state.screen-eq'select'} 'ready'|Out-Null;
'''
def step(label, target, ident, operation, value, predicate):
    global code
    code += 'Step '+ ' '.join(q(x) for x in [label,target,ident,operation,value]) + ' {param($s) '+predicate+'};\n'
if not launch.get('resumeAfterResize',False):
    if not launch.get('resumeAfterQuery',False):
        step('category-data','category-data','','click','Data check selection' if english else '데이터 검사 선택', "$s.reason-eq'category:data'-and($s.state.categories-join'|')-eq'schema|access|objects'-and!$s.state.stopped-and$s.state.announcement-eq"+q(source_rows['category-data']['announcement']))
        step('stopped-fixture-setup','current','','utility','seed-stopped','$s.state.stopped')
        # Toggle button's complete literal accessible name comes from the current observer.
        code += "$expand=@((Latest).controls|Where-Object{$_.name.StartsWith("+q('Data All values' if english else '데이터 전체')+")});if($expand.Count-ne1){throw 'Exact category expansion name'};"
        for label, expected in [('expand-data',True),('collapse-data',False)]:
            predicate="$s.reason-eq'expand:data'-and$s.state.stopped-and$s.state.announcement-eq"+q(source_rows[label]['announcement'])
            code += "Step "+q(label)+" 'expand-data' '' 'click' $expand[0].name {param($s) "+predicate+"};\n"
        step('query-orders','table-search','table-search','edit','orders',"$s.state.query-eq'orders'-and($s.shown-join'|')-eq'demo.orders'-and$s.state.stopped-and$s.state.announcement-eq"+q(source_rows['query-orders']['announcement']))
    elif not launch.get('resumeAfterRemovalOpen',False):
        code += "if((Latest).state.query-ne'orders'-or!(Latest).state.stopped){throw 'Saved query checkpoint restore guard'};"
    if not launch.get('resumeAfterRemovalOpen',False):
        step('table-orders','table-orders','Selection-table-demo.orders','click','',"$s.reason-eq'table:demo.orders'-and($s.state.tables-join'|')-eq'demo.customers|demo.line_items'-and!$s.state.stopped-and$s.state.announcement-eq"+q(source_rows['table-orders']['announcement']))
        step('filtered-bulk','table-all','table-all','click','',"$s.reason-eq'bulk'-and($s.state.tables-join'|')-eq'demo.customers|demo.line_items|demo.orders'-and$s.state.announcement-eq"+q(source_rows['filtered-bulk']['announcement']))
        step('filtered-deselect','clear-tables','Selection-clear-tables','click','',"$s.reason-eq'clear-tables'-and($s.state.tables-join'|')-eq'demo.customers|demo.line_items'-and$s.state.announcement-eq"+q(source_rows['filtered-deselect']['announcement']))
    for suffix, apply, target_id, target_name in [('cancel',False,'MatchingDialog-choice-3','archive.orders'),('context',True,'MatchingDialog-choice-3','archive.orders'),('removal',True,'MatchingDialog-choice-1','demo.orders')]:
        if launch.get('resumeAfterRemovalOpen',False) and suffix!='removal': continue
        if suffix=='removal' and not launch.get('resumeAfterRemovalOpen',False):step('query-target','table-search','table-search','edit','archive',"$s.state.query-eq'archive'-and($s.shown-join'|')-eq'demo.orders'")
        if not launch.get('resumeAfterRemovalOpen',False):step('mapping-open-'+suffix,'mapping-open','Selection-mapping-demo.orders','click','','$null-ne$s.modal')
        step('mapping-target-'+suffix,'mapping-choice',target_id,'click','',"$null-ne$s.modal-and$s.modal.draft-eq"+q(target_name))
        expected_map='demo.orders' if suffix in ['cancel','removal'] else 'archive.orders'
        pred="$null-eq$s.modal-and$s.state.maps.'demo.orders'-eq"+q(expected_map)
        if suffix=='cancel':pred+="-and$s.state.focus-eq'mapping:demo.orders'"
        if suffix=='context':pred+="-and$s.state.query-eq'orders'"
        if suffix=='removal':pred+="-and$s.shown.Count-eq0-and$s.state.focus-eq'BODY'"
        step('mapping-'+suffix,'mapping-apply' if apply else 'mapping-close','MatchingDialog-apply' if apply else 'MatchingDialog-close','click','',pred)
    step('query-clear','table-search','table-search','edit','',"$s.state.query-eq''-and$s.shown.Count-eq3")
    step('modal-language-open','mapping-open','Selection-mapping-demo.orders','click','','$null-ne$s.modal')
    step('modal-language-query','mapping-search','MatchingDialog-search','edit','archive',"$null-ne$s.modal-and$s.modal.query-eq'archive'")
    step('modal-language-draft','mapping-choice','MatchingDialog-choice-0','click','',"$null-ne$s.modal-and$s.modal.draft-eq'archive.orders'")
    step('modal-language-handler-api','current','','utility','language-api',"$s.state.english-eq"+('$false' if english else '$true')+"-and$null-ne$s.modal-and$s.modal.draft-eq'demo.orders'-and$s.modal.query-eq''")
    step('modal-language-cancel','mapping-close','MatchingDialog-close','click','',"$null-eq$s.modal-and$s.state.focus-eq'BODY'")
    step('language-fixture-reset','current','','utility','language-api',"$s.state.english-eq"+('$true' if english else '$false'))
    step('resize-fixture-seed','current','','utility','seed-resize',"$s.identity.pageSame-and$s.identity.headingSame-and$s.identity.searchSame-and$s.identity.categorySame-and$s.identity.tableCheckboxSame")
    for width in [960,1180,1280]:
        step('resize-'+str(width),'current','','utility','resize',"$s.width-eq"+str(width)+"-and$s.identity.pageSame-and$s.identity.headingSame-and$s.identity.searchSame-and$s.identity.categorySame-and$s.identity.tableCheckboxSame-and($s.state.tables-join'|')-eq'demo.customers|demo.line_items'")
step('error-navigation-preflight','back','Footer-back','click','',"$s.state.screen-eq'capabilities'")
step('error-navigation-connect','back','Footer-back','click','',"$s.state.screen-eq'connect'")
step('error-host-empty','source-host','source-host','edit','',"!$s.state.connected")
step('error-invalid-connect','connect','Footer-connect','click','',"$s.state.error-eq"+q(source_rows['invalid-connect']['error']))
step('error-back-home','back','Footer-back','click','',"$s.state.screen-eq'home'-and$s.state.error-eq''")
step('error-start-reset','start','start','click','',"$s.state.screen-eq'connect'-and$s.state.error-eq''-and($s.state.tables-join'|')-eq'demo.customers|demo.orders|demo.line_items'")
code += r'''
}catch{$failed=$_.ToString();@{error=$failed;stage=[OwnedSelectionCallbacksInput]::LastStage;checks=$checks}|ConvertTo-Json -Depth 50|Set-Content -Encoding UTF8 (Join-Path $out 'failure.json')}
finally{
 $window=[OwnedSelectionCallbacksInput]::Window($exe,$false);$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();$end=[DateTime]::UtcNow.AddSeconds(5);do{Start-Sleep -Milliseconds 50;$count=@(Get-Process -Id $pidOwned -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$exe}).Count}while($count-ne0-and[DateTime]::UtcNow-lt$end);
 $windows=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.ProcessId-eq$pidOwned-and$_.Current.Name-eq'Replica PageIntegration application verification'}).Count;
 @{pid=$pidOwned;path=$exe;nativeProcess=$count;nativeWindow=$windows}|ConvertTo-Json|Set-Content -Encoding UTF8 (Join-Path $out 'cleanup.json');if($count-ne0-or$windows-ne0){throw 'Exact callback close0 failed'}
}
@{failed=$failed;checks=$checks;cleanup=([IO.File]::ReadAllText((Join-Path $out 'cleanup.json'))|ConvertFrom-Json);physicalTransport=[IO.File]::ReadAllText((Join-Path $out 'physical-transport.txt'))}|ConvertTo-Json -Depth 50 -Compress|ForEach-Object{[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($_))};exit 0;
'''
command = served/('preflight-selectioncallbacks-'+nonce+'-batch.txt'); command.write_text(code)
loader = "$file=Join-Path $env:TEMP "+q(command.name)+";try{Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+command.name)+" -OutFile $file;if((Get-FileHash $file -Algorithm SHA256).Hash-ne "+q(hashlib.sha256(command.read_bytes()).hexdigest().upper())+"){throw 'Exact bounded actor command guard'};&([scriptblock]::Create([IO.File]::ReadAllText($file,[Text.Encoding]::UTF8)))}finally{Remove-Item $file -ErrorAction SilentlyContinue};"
(out/('batch-plan-'+nonce+'.json')).write_text(json.dumps({'nonce':nonce,'pid':launch['pid'],'path':launch['path'],'output':launch['output'],'english':english,'scope':'new callback states only; utility F6/F7/F8/F9 explicitly fixture/API','commandSha256':hashlib.sha256(command.read_bytes()).hexdigest()},indent=2))
if a.plan_only:
    print(command); raise SystemExit(0)
result = ps(loader,timeout=58); (out/('batch-'+nonce+'.log')).write_text(result)
v = json.loads(base64.b64decode(result.strip().splitlines()[-1])); (out/('comparison-'+nonce+'.json')).write_text(json.dumps(v,ensure_ascii=False,indent=2))
assert v['cleanup']['nativeProcess'] == v['cleanup']['nativeWindow'] == 0
(out/('cleanup-'+nonce+'.json')).write_text(json.dumps(v['cleanup'],indent=2)); (out/'active-launch.json').rename(out/('closed-launch-'+nonce+'.json'))
assert not v['failed'], v['failed']
print('New native callback checkpoints',len(v['checks']),'exactclose0')
