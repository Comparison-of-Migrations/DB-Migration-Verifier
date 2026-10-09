"""Changed Windows Header name/content tree scope, in its unique isolated runtime."""
from pathlib import Path
import argparse,json,re,uuid
from driver import ps,q
r=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=['headless','ax']);p.add_argument('--stage',type=Path,required=True);p.add_argument('--source',action='store_true');p.add_argument('--label',choices=['ko','en','remount'],default='ko');a=p.parse_args()
v=json.loads(a.stage.read_text());assert v['owner']=='preflight' and v['component']=='capabilityheader' and v['selfContained'] and re.fullmatch(r'DBMV-Isolated-preflight-capabilityheader-[0-9a-f]{32}',v['guestFolder'])
base="$base=Join-Path $env:TEMP "+q(v['guestFolder'])+";$app=Join-Path $base 'app';"
out=r/'docs/replica/source/capabilityheader-windows-names';out.mkdir(exist_ok=True)
if a.action=='headless':
 identity=uuid.uuid4().hex
 folder='names-headless-'+identity
 code=base+"$out=Join-Path $app "+q(folder)+";New-Item -ItemType Directory $out|Out-Null;$env:DBMV_HEADLESS='1';$env:DBMV_HEADLESS_SCALE='2';& ($app+'\\ReplicaProbe.exe') '--capabilityheader' ($app+'\\capabilityheader-cases.json') $out '--names-only';if($LASTEXITCODE-ne 0){throw 'Changed header names failed'};Get-Content -Raw ($out+'\\capabilityheader-names.json');"
 data=ps(code,timeout=60);print(data);value=json.loads(data.splitlines()[-1]);assert value['dpr']==2 and len(value['states'])==3
 (out/('headless-'+identity+'.json')).write_text(json.dumps({'owner':'preflight','component':'capabilityheader','stage':str(a.stage),'guestOutput':folder,'headless':True,'scale':2,'result':value},ensure_ascii=False,indent=2)+'\n')
else:
 title='CapabilityHeader Input · original isolation' if a.source else 'Replica CapabilityHeader verification'
 guard="(Get-Process -Id $_.Current.ProcessId).ProcessName-eq 'msedge'" if a.source else "(Get-Process -Id $_.Current.ProcessId).Path-eq ($app+'\\ReplicaProbe.exe')"
 code=base+"$w=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)|Where-Object{$_.Current.Name.StartsWith("+q(title)+")-and "+guard+"});if($w.Count-ne 1){throw 'Exact owned header window required'};$w=$w[0];$all=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition);$cells=@($all|Where-Object{$_.Current.AutomationId.StartsWith('CapabilityHeader-cell-')});if($cells.Count-ne 4){throw 'Four actual header cells required'};@($cells|ForEach-Object{$c=$_;$children=@($c.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition));@{id=$c.Current.AutomationId;name=$c.Current.Name;type=$c.Current.ControlType.ProgrammaticName;enabled=$c.Current.IsEnabled;focusable=$c.Current.IsKeyboardFocusable;invoke=@($c.GetSupportedPatterns()|Where-Object{$_.ProgrammaticName-eq 'InvokePatternIdentifiers.Pattern'}).Count-gt 0;children=@($children|ForEach-Object{@{name=$_.Current.Name;type=$_.Current.ControlType.ProgrammaticName;control=$_.Current.IsControlElement;content=$_.Current.IsContentElement}})}})|ConvertTo-Json -Depth 6"
 data=ps(code,retry_read=True);value=json.loads(data);assert len(value)==4
 expected=['검사 분류','원본 / 대상','검사 가능 범위','안내'] if a.label=='ko' else ['Category','Source / target','Capability','Details']
 assert [c['name'] for c in value]==expected
 assert all(c['enabled'] and not c['focusable'] and not c['invoke'] for c in value),value
 assert all(not any(ch['control'] or ch['content'] for ch in c['children']) for c in value),value
 (out/(('source-' if a.source else 'native-')+a.label+'.json')).write_text(data+'\n');print(data)
