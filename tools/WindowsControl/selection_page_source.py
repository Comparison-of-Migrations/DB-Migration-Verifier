"""Fresh actual Selection original36+bounded Raw/Control KOEN; official owned Edge only."""
from pathlib import Path
import json,time,uuid,hashlib,shutil
from driver import ps,q
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/workers/preflight/selectionpage/source-windows'/uuid.uuid4().hex;o.mkdir(parents=True);print(o,flush=True)
assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight'
def find(title):
 return "$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+")-and (Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});"
def close(title,label):
 guard=find(title);code=guard+"if($w.Count-gt 1){throw 'Ambiguous own source close'};if($w.Count-eq 1){$w[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()};Start-Sleep -Milliseconds 250;"+guard+"@{sourceWindowCount=$w.Count}|ConvertTo-Json"
 raw=ps(code,timeout=15);data=json.loads(raw);assert data['sourceWindowCount']==0;(o/(label+'-cleanup.json')).write_text(raw)
def launch(title,url,label):
 raw=ps(find(title)+"if($w.Count){throw 'Own source already exists'};Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized','--force-device-scale-factor=2',"+q(url)+");[Console]::WriteLine('Owned source launch sent')",timeout=15);(o/(label+'-launch.json')).write_text(json.dumps(dict(title=title,url=url,stdout=raw,producer=o.name)))
title='SelectionPage · original isolation';url='http://10.211.55.2:8768/selectionpage.html?os=windows&record=1&batch='+o.name;started=time.time()
try:
 launch(title,url,'source36');chunks={};deadline=time.monotonic()+45
 while time.monotonic()<deadline:
  for index,part in enumerate(['a','b']):
   path=r/'docs/replica/source'/f'preflight-selectionpage-{part}-windows-source.json'
   if path.exists()and path.stat().st_mtime>started:
    try:
     d=json.loads(path.read_text())
     if d.get('batch')==o.name and d.get('chunk')==index and d['dpr']==2 and len(d['rows'])==18:chunks[index]=d;shutil.copyfile(path,o/('source-chunk-'+part+'.json'))
    except (json.JSONDecodeError,KeyError):pass
  if len(chunks)==2:break
  time.sleep(.08)
 else:raise RuntimeError('Fresh original36 exactnonce chunks absent; no repeated launch')
 combined=dict(dpr=2,rows=chunks[0]['rows']+chunks[1]['rows'],batch=o.name,transport='bounded18+18sameproducer',chunks=[dict(path=str(o/('source-chunk-'+x+'.json')),sha256=hashlib.sha256((o/('source-chunk-'+x+'.json')).read_bytes()).hexdigest())for x in ['a','b']]);assert len({x['id']for x in combined['rows']})==36
 (o/'selectionpage-source.json').write_text(json.dumps(combined,ensure_ascii=False,indent=2));shutil.copyfile(o/'selectionpage-source.json',r/'docs/replica/source/selectionpage-windows-source.json')
 (r/'docs/replica/source/selectionpage-windows-cases.json').write_text(json.dumps([dict(id=x['id'],width=x['root']['box']['width'],viewport=x['viewportWidth'],english=x['english'],variant=x['variant'])for x in combined['rows']],indent=2))
finally:close(title,'source36')
code=r'''function Node($e){$v=$e.Current;$b=$v.BoundingRectangle;@{name=$v.Name;id=$v.AutomationId;type=$v.ControlType.ProgrammaticName;control=$v.IsControlElement;content=$v.IsContentElement;focusable=$v.IsKeyboardFocusable;enabled=$v.IsEnabled;offscreen=$v.IsOffscreen;box=@{x=$b.X;y=$b.Y;width=$b.Width;height=$b.Height}}};function Tree($e,$walker,$depth){if($null-eq $e-or $depth-ge 22-or $script:visits++-ge 1100){return $null};$v=Node $e;$children=[Collections.Generic.List[object]]::new();$seen=[Collections.Generic.HashSet[string]]::new();$c=$walker.GetFirstChild($e);$count=0;while($null-ne $c-and $count++-lt 70){if(!$seen.Add(($c.GetRuntimeId()-join ','))){break};$children.Add((Tree $c $walker ($depth+1)));$c=$walker.GetNextSibling($c)};$v['children']=$children.ToArray();return $v};$views=[Collections.Generic.List[object]]::new();foreach($view in @('Raw','Control')){$script:visits=0;$walker=if($view-eq 'Raw'){[System.Windows.Automation.TreeWalker]::RawViewWalker}else{[System.Windows.Automation.TreeWalker]::ControlViewWalker};$tree=Tree $w $walker 0;$views.Add(@{view=$view;visits=$script:visits;tree=$tree})};@{pid=$w.Current.ProcessId;path=(Get-Process -Id $w.Current.ProcessId).Path;title=$w.Current.Name;views=$views.ToArray()}|ConvertTo-Json -Depth 55 -Compress'''
for language in ['ko','en']:
 title='SelectionPage Input · original isolation';url='http://10.211.55.2:8768/selectionpage-input.html?os=windows&batch='+o.name+'&language='+language
 try:
  launch(title,url,'peer-'+language);deadline=time.monotonic()+10;endpoint=r/'docs/replica/source/preflight-selectionpage-windows-input.json'
  while time.monotonic()<deadline:
   try:
    d=json.loads(endpoint.read_text())
    if d.get('batch')==o.name and d.get('language')==language:break
   except (FileNotFoundError,json.JSONDecodeError):pass
   time.sleep(.08)
  else:raise RuntimeError('Own peer source state absent; no repeated launch')
  shutil.copyfile(endpoint,o/('source-'+language+'-state.json'))
  raw=ps(find(title)+"if($w.Count-ne 1){throw 'Exact own source peer window missing'};$w=$w[0];"+code,timeout=25);json.loads(raw);(o/('source-'+language+'-raw-control.json')).write_text(raw)
 finally:close(title,'peer-'+language)
print('Selection source36 + Raw/ControlKOEN source2 captured and exact source0',flush=True)
