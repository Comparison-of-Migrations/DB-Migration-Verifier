"""Approved exact CategoryHeader Windows pointer control."""
from pathlib import Path
import argparse,hashlib,json
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('mode',choices=['button-click','choice-click','button-hold']);a=p.parse_args()
s=r/'tools/WindowsControl/owned_category_mouse.cs';(r/'artifacts/replica/windows-download/owned-category-mouse.cs').write_bytes(s.read_bytes());digest=hashlib.sha256(s.read_bytes()).hexdigest();dll='OwnedCategory-'+digest[:12]+'.dll'
b="$app=Join-Path (Join-Path $env:TEMP 'DBMV-CategoryHeader-20261007') 'app';$dll=Join-Path $app "+q(dll)+";if(!(Test-Path $dll)){$cs=Join-Path $app 'owned-category-mouse.cs';Invoke-WebRequest -UseBasicParsing 'http://10.211.55.2:8768/owned-category-mouse.cs' -OutFile $cs;if((Get-FileHash $cs -Algorithm SHA256).Hash-ne "+q(digest.upper())+"){throw 'Hash mismatch'};$rect=[System.Windows.Automation.AutomationElement]::RootElement.Current.BoundingRectangle;Add-Type -Path $cs -ReferencedAssemblies @('System.dll','System.Core.dll',$rect.GetType().Assembly.Location,[System.Windows.Automation.AutomationElement].Assembly.Location,[System.Windows.Automation.ControlType].Assembly.Location) -OutputAssembly $dll};Add-Type -Path $dll;"
d=json.loads(ps(b+"[OwnedCategoryFixture]::Run("+q(a.mode)+");"));o=r/'docs/replica/source/categoryheader-windows-input';o.mkdir(exist_ok=True);(o/('actual-'+a.mode+'.json')).write_text(json.dumps(d,indent=2)+'\n');print(d)
if a.mode=='button-hold':(o/'actual-held.json').write_text(ps("$app=Join-Path (Join-Path $env:TEMP 'DBMV-CategoryHeader-20261007') 'app';Get-Content -Raw ($app+'\\actual-held.json')",retry_read=True)+'\n')
