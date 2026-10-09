"""Bounded actual Raw/Control walker H1/H2/P metadata; no focus or input."""
from pathlib import Path
import argparse,json,re
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--stage',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--source',action='store_true');p.add_argument('--label',required=True);a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight';v=json.loads(a.stage.read_text());assert v['owner']=='preflight'and v['component']=='preflightpage'and re.fullmatch(r'DBMV-Isolated-preflight-preflightpage-[0-9a-f]{32}',v['guestFolder']);a.output.mkdir(parents=True,exist_ok=True)
base="$app=Join-Path (Join-Path $env:TEMP "+q(v['guestFolder'])+") 'app';";title='PreflightPage Input · original isolation'if a.source else'Replica PreflightPage verification';guard="(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')"if a.source else"(Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"
find=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+")-and "+guard+"});if($w.Count-ne 1){throw 'Exact owned Preflight window required'};$w=$w[0];"
code=r'''function Node($e){
 if($null-eq $e){return $null};$r=$e.Current.BoundingRectangle;
 $hp=@($e.GetSupportedProperties()|Where-Object{$_.ProgrammaticName-like '*HeadingLevel*'});
 @{id=$e.Current.AutomationId;name=$e.Current.Name;type=$e.Current.ControlType.ProgrammaticName;control=$e.Current.IsControlElement;content=$e.Current.IsContentElement;focusable=$e.Current.IsKeyboardFocusable;headingLevel=if($hp.Count){$e.GetCurrentPropertyValue($hp[0]).ToString()}else{$null};box=@{x=$r.X;y=$r.Y;width=$r.Width;height=$r.Height}}
};function Children($walker,$e,$limit){
 $items=[Collections.Generic.List[object]]::new();$seen=[Collections.Generic.HashSet[string]]::new();$child=$walker.GetFirstChild($e);$count=0;
 while($null-ne $child-and $count++-lt $limit){if(!$seen.Add(($child.GetRuntimeId()-join ','))){break};$items.Add($child);$child=$walker.GetNextSibling($child)};
 return $items.ToArray()
};$views=[Collections.Generic.List[object]]::new();foreach($view in @('Control','Raw')){
 $walker=if($view-eq 'Raw'){[System.Windows.Automation.TreeWalker]::RawViewWalker}else{[System.Windows.Automation.TreeWalker]::ControlViewWalker};
 $stack=[Collections.Generic.Stack[object]]::new();$stack.Push(@($w,0));$nodes=[Collections.Generic.List[object]]::new();$visits=0;$seen=[Collections.Generic.HashSet[string]]::new();
 while($stack.Count-and $visits++-lt 700){$entry=$stack.Pop();$e=$entry[0];$d=[int]$entry[1];if(!$seen.Add(($e.GetRuntimeId()-join ','))){continue};$n=$e.Current.Name;
 if($e.Current.ControlType-eq [System.Windows.Automation.ControlType]::Text-and ($n.StartsWith('앱이 확인할 수 있는 범위입니다')-or $n.StartsWith('What the app can verify')-or $n.StartsWith('별도 DB 동작 시험은 선택입니다')-or $n.StartsWith('Behavior tests are optional')-or $n.StartsWith('기본 비교는 읽기 전용입니다.')-or $n.StartsWith('The default comparison is read-only.'))){
  $parent=$walker.GetParent($e);$pc=@(Children $walker $parent 16|ForEach-Object{Node $_});$nc=@(Children $walker $e 16|ForEach-Object{Node $_});
  $nodes.Add(@{node=(Node $e);children=$nc;parent=(Node $parent);parentChildren=$pc})
 };if($d-lt 24){foreach($child in (Children $walker $e 80)){$stack.Push(@($child,($d+1)))}}
 };$views.Add(@{view=($view+'ViewWalker');visited=$visits;truncated=[bool]$stack.Count;nodes=$nodes.ToArray()})
};@{view='ControlViewWalker';nodes=$views[0].nodes;views=$views.ToArray()}|ConvertTo-Json -Depth 14'''
data=ps(find+code,timeout=30,retry_read=True);value=json.loads(data);(a.output/(('source-'if a.source else'native-')+a.label+'-semantics.json')).write_text(data);print('Raw/Control parent text peers saved: '+str([(x['view'],len(x['nodes']),x['truncated']) for x in value['views']]))
