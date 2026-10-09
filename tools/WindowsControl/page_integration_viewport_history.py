"""Finite original-only history measurement; exact owned Edge window closes first."""
from pathlib import Path
import datetime,hashlib,json,uuid
from driver import ps,q
ROOT=Path(__file__).resolve().parents[2]
assert json.loads((ROOT/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='connection'
nonce=uuid.uuid4().hex;out=ROOT/'artifacts/replica/workers/connection/pageintegration/viewport-history'/nonce;out.mkdir(parents=True)
served=ROOT/'artifacts/replica/windows-download';endpoint=ROOT/'docs/replica/source/connection-pageintegrationviewporthistory-windows-source.json'
alias=served/'connection-pageintegrationviewporthistory-windows-source-latest.json'
if not alias.exists() and not alias.is_symlink():alias.symlink_to(endpoint)
url='http://10.211.55.2:8768/connection-pageintegrationviewporthistory.html?recording='+nonce
code=r"""
$ProgressPreference='SilentlyContinue';$nonce=__NONCE__;$url=__URL__;$window=$null;$sourcePid=0;$sourceHwnd=0;$sourcePath='';
function HasOwnedUrl($candidate){foreach($element in $candidate.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Edit))){$pattern=$null;if($element.Current.ControlType-eq[System.Windows.Automation.ControlType]::Edit-and$element.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$pattern)){if($pattern.Current.Value-eq$url-or$pattern.Current.Value-eq$url.Substring(7)){return $true}}};return $false}
function FindOwned{$historyFound=[Collections.Generic.List[object]]::new();foreach($candidate in [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)){$title=$candidate.Current.Name;if(!(($title.Contains($nonce)-and($title.StartsWith('Parent resize history · original isolation ')-or$title.StartsWith('Parent resize history recorded · original isolation ')))-or$title.StartsWith('Parent resize history preparing'))){continue};$path=(Get-Process -Id $candidate.Current.ProcessId).Path;if(!$path.EndsWith('Microsoft\Edge\Application\msedge.exe',[StringComparison]::OrdinalIgnoreCase)-or!(HasOwnedUrl $candidate)){continue};$historyFound.Add($candidate)};if($historyFound.Count-gt1){throw 'Unique fresh exact source URL/window required'};return @($historyFound.ToArray())}
Start-Process msedge.exe -ArgumentList @('--new-window',$url);
try{$deadline=[DateTime]::UtcNow.AddSeconds(20);$ready=$false;do{$found=@(FindOwned);if($found.Count-eq1){$window=$found[0];$sourcePid=$window.Current.ProcessId;$sourceHwnd=$window.Current.NativeWindowHandle;$sourcePath=(Get-Process -Id $sourcePid).Path;try{$data=Invoke-RestMethod -Uri 'http://10.211.55.2:8768/connection-pageintegrationviewporthistory-windows-source-latest.json'}catch{$data=$null};if($null-ne$data-and$data.recording-eq$nonce){if($data.scope-eq'parent-resize-history6-error'){throw ('Actual source observer error: '+$data.error)};$ready=$data.scope-eq'parent-resize-history6'-and@($data.rows).Count-eq6}};if(!$ready){Start-Sleep -Milliseconds 80}}while(!$ready-and[DateTime]::UtcNow-lt$deadline);if(!$ready){throw 'Fresh history6 recording absent; no replay'}}
finally{$fresh=@(FindOwned);if($fresh.Count-eq1){$current=$fresh[0];if($null-ne$window-and($current.Current.NativeWindowHandle-ne$sourceHwnd-or$current.Current.ProcessId-ne$sourcePid-or(Get-Process -Id $sourcePid).Path-ne$sourcePath)){throw 'Exact source finally-close identity changed'};$sourcePid=$current.Current.ProcessId;$sourceHwnd=$current.Current.NativeWindowHandle;$sourcePath=(Get-Process -Id $sourcePid).Path;$frame=$current.Current.BoundingRectangle;if($sourceHwnd-eq0-or$frame.Width-lt300-or$frame.Width-gt5000-or$frame.Height-lt300-or$frame.Height-gt3000){throw 'Exact source close frame guard'};$current.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()};$end=[DateTime]::UtcNow.AddSeconds(2);do{$remaining=@(FindOwned).Count;if($remaining){Start-Sleep -Milliseconds 30}}while($remaining-and[DateTime]::UtcNow-lt$end);if($remaining-ne0-or$sourcePid-eq0){throw 'Exact source close not proved'};[pscustomobject]@{scope='Original viewport history6; no candidate/input';recording=$nonce;sourceWindow=$remaining;sourcePid=$sourcePid;sourceHwnd=$sourceHwnd;sourcePath=$sourcePath;candidateProcess=0;candidateWindow=0}|ConvertTo-Json -Compress};
""".replace('__NONCE__',q(nonce)).replace('__URL__',q(url))
f=served/('connection-viewporthistory-'+nonce+'-command.txt');f.write_text(code);sha=hashlib.sha256(f.read_bytes()).hexdigest().upper()
(out/'command.json').write_text(json.dumps(dict(nonce=nonce,url=url,startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),commandSha256=sha),indent=2)+'\n')
loader="$file=Join-Path $env:TEMP "+q(f.name)+";try{Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+f.name)+" -OutFile $file;if((Get-FileHash $file -Algorithm SHA256).Hash-ne "+q(sha)+"){throw 'Owned source command hash changed'};&([scriptblock]::Create([IO.File]::ReadAllText($file,[Text.Encoding]::UTF8)))}finally{Remove-Item $file -ErrorAction SilentlyContinue};"
try:result=ps(loader,timeout=35)
except Exception as error:
    (out/'failure.json').write_text(json.dumps(dict(status='UNVERIFIED_NO_REPLAY',nonce=nonce,error=str(error)),indent=2)+'\n')
    import re
    matched=re.search(r'(\{"scope":"Original viewport history6;[^\r\n]+\})',str(error))
    if matched:(out/'cleanup.json').write_text(json.dumps(json.loads(matched.group(1)),indent=2)+'\n')
    if endpoint.exists():
        fresh=json.loads(endpoint.read_text())
        if fresh.get('recording')==nonce:(out/'source-or-error.json').write_text(json.dumps(fresh,ensure_ascii=False,indent=2)+'\n')
    raise
(out/'transport.log').write_text(result+'\n');cleanup=json.loads(result);assert cleanup['sourceWindow']==0;(out/'cleanup.json').write_text(json.dumps(cleanup,indent=2)+'\n')
d=json.loads(endpoint.read_text());assert d['recording']==nonce and d['scope']=='parent-resize-history6' and len(d['rows'])==6 and d['dpr']==2
(out/'source.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');target=ROOT/'docs/replica/source/pageintegrationparentresize-windows-history-source.json';target.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(out);print('Fresh original history6 preserved; sourceWindow0/candidateWindow0/runtime0')
