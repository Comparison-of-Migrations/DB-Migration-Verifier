"""New native Selection LastChangeReason callback metadata8 only, not old geometry/input matrix."""
from pathlib import Path
import argparse,json,hashlib,base64,io,zipfile
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);a=p.parse_args();v=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert v['owner']=='preflight'and v['component']=='selectionpage';assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='preflight';o=a.manifest.parent/'actual-windows-input';o.mkdir(exist_ok=True)
cs=r/'tools/WindowsControl/owned_selection_page_input.cs';digest=hashlib.sha256(cs.read_bytes()).hexdigest();name='preflight-selection-reasons-'+digest+'.cs';(r/'artifacts/replica/windows-download'/name).write_bytes(cs.read_bytes());base="$app=Join-Path (Join-Path $env:TEMP "+q(v['guestFolder'])+") 'app';"
code=base+"$ProgressPreference='SilentlyContinue';$dll=Join-Path $app "+q('OwnedSelection-'+digest+'.dll')+";$cs=Join-Path $app 'owned-selection-reasons.cs';Invoke-WebRequest -UseBasicParsing "+q('http://10.211.55.2:8768/'+name)+" -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Actor hash mismatch'};$rect=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$rect.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll;Add-Type -Path $dll;$checks=@();$expectedCount=0;"
for english in [False,True]:
 if english:code+="$null=[OwnedSelectionPageInput]::Run($app,'next');"
 code+="$null=[OwnedSelectionPageInput]::Run($app,'scroll-table');"
 for mode,reason in [('bulk-label','bulk'),('bulk-toggle','bulk'),('deselect','clear-tables'),('category-data','category:data')]:
  if mode=='category-data':code+="$null=[OwnedSelectionPageInput]::Run($app,'scroll-top');"
  label=('en-'if english else'ko-')+mode;flag='$true'if english else'$false'
  code+="$raw=[OwnedSelectionPageInput]::Run($app,"+q(mode)+");$expectedCount++;$limit=[DateTime]::UtcNow.AddSeconds(4);do{$s=$raw|ConvertFrom-Json;if($s.english-eq"+flag+"-and$s.lastChangeReason-eq"+q(reason)+"-and @($s.changes).Count-eq$expectedCount){break};Start-Sleep -Milliseconds 50;$raw=[IO.File]::ReadAllText($app+'\\selectionpage-input.json')}while([DateTime]::UtcNow-lt$limit);$raw|Set-Content -Encoding UTF8 ($app+'\\selection-reason-"+label+".json');if($s.english-ne"+flag+"-or$s.lastChangeReason-ne"+q(reason)+"-or@($s.changes).Count-ne$expectedCount){throw 'Metadata mismatch; no input replay'};$checks+=@{english="+flag+";mode="+q(mode)+";reason="+q(reason)+";passed=$true};"
code+="$checks|ConvertTo-Json;exit 0;"
try:
 result=ps(code,timeout=35);(o/'reason-transport.txt').write_text(result);checks=json.loads(result);assert len(checks)==8;(o/'reason-comparison.json').write_text(json.dumps(checks,indent=2))
except Exception as e:(o/'reason-failure.txt').write_text(str(e));raise
finally:
 result=ps(base+"$files=@(Get-ChildItem ($app+'\\selection-reason-*.json'),($app+'\\selectionpage-native-*-transport.txt') -ErrorAction SilentlyContinue);if($files.Count){Compress-Archive -Path $files.FullName -DestinationPath ($app+'\\reason-evidence.zip');[Convert]::ToBase64String([IO.File]::ReadAllBytes($app+'\\reason-evidence.zip'))};exit 0",timeout=15,retry_read=True)
 if result:
  blob=base64.b64decode(result);(o/'reason-evidence.zip').write_bytes(blob)
  with zipfile.ZipFile(io.BytesIO(blob))as z:
   for f in z.namelist():assert '/'not in f and '\\'not in f;(o/f).write_bytes(z.read(f))
print('Native reason metadata8 saved; close immediately')
