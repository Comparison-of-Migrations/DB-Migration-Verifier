"""Compare candidate viewport geometry against actual original browser measurements."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2]
osname=sys.argv[1]
assert osname in ('mac','windows')
source=json.loads((r/f'docs/replica/source/scrollviewport-{osname}-source-layout.json').read_text())
native=json.loads((r/f'docs/replica/source/scrollviewport-{osname}-native/scrollviewport-native-layout.json').read_text())
assert source['dpr']==native['dpr']==2
assert len(source['rows'])==len(native['rows'])==18
differences=[]
def compare(expected,actual,path):
    for key,value in actual.items():
        target=expected[key]
        if isinstance(value,dict): compare(target,value,path+'.'+key)
        elif abs(target-value)>.001: differences.append(dict(path=path+'.'+key,source=target,native=value))
for i,(a,b) in enumerate(zip(source['rows'],native['rows'])):compare(a,b,str(i))
result=dict(platform=osname,cases=18,differences=differences,scope='layout/programmatic offsets only; paint/input pending')
(r/f'docs/replica/source/scrollviewport-{osname}-layout-comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(result,ensure_ascii=False))
sys.exit(bool(differences))
