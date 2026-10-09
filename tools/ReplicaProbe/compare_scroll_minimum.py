"""Compare collected original DOM boundary cases against the attached native fixture."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];osname=sys.argv[1];assert osname in ('mac','windows')
a=json.loads((r/f'docs/replica/source/scrollviewport-{osname}-source-minimum.json').read_text())
b=json.loads((r/f'docs/replica/source/scrollviewport-{osname}-native/scrollviewport-native-minimum.json').read_text())
assert a['dpr']==b['dpr']==2 and len(a['rows'])==len(b['rows'])==4
differences=[]
def check(source,native,path):
 for key,value in native.items():
  expected=source[key];name=path+'.'+key
  if isinstance(value,dict):check(expected,value,name)
  elif isinstance(value,(int,float)):
   if abs(expected-value)>.001:differences.append(dict(path=name,source=expected,native=value))
  elif value!=expected:differences.append(dict(path=name,source=expected,native=value))
for i,(s,n) in enumerate(zip(a['rows'],b['rows'])):check(s,n,str(i))
result=dict(platform=osname,cases=4,differences=differences,scope='source padding-box minimum height; horizontal overflow is outside the Sidebar contract')
(r/f'docs/replica/source/scrollviewport-{osname}-minimum-comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result));sys.exit(bool(differences))
