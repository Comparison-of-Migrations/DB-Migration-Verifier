"""Independent original DOM boxes and state descriptors versus attached Avalonia footer."""
from pathlib import Path
import json,sys,re,xml.etree.ElementTree as ET
osname=sys.argv[1];assert osname in ['mac','windows'];r=Path(__file__).resolve().parents[2]/'docs/replica/source'
sources=[json.loads((r/f'actionfooter-{osname}-source-{w}.json').read_text())for w in [1000,1138,1280]]
native=json.loads((r/f'actionfooter-{osname}-native/actionfooter-native.json').read_text());assert all(x['dpr']==2 for x in sources)and native['scale']==2
source=[dict(viewport=s['viewport'],**x)for s in sources for x in s['rows']];assert len(source)==len(native['rows'])==132
icons_line=next(x for x in (r/'original.js').read_text().splitlines()if x.startswith('const icons='));icons=dict(re.findall(r"(\w+):'([^']*)'",icons_line));assert len(icons)>=18
errors=[]
def check(id,key,a,b):
 if (abs(a-b)>1e-7 if isinstance(a,(float,int)) and not isinstance(a,bool)else a!=b):errors.append(dict(id=id,property=key,source=a,native=b))
def box(id,key,a,b):
 if a is None or b is None:check(id,key,a,b);return
 for k in ['x','y','width','height']:check(id,key+'.'+k,a[k],b[k])
def icon(value):return [(n.tag,dict(n.attrib))for n in ET.fromstring('<svg>'+value+'</svg>')]
for s,n in zip(source,native['rows']):
 id=str(s['viewport'])+'/'+s['id']
 for k in ['id','viewport','width','height','paddingX']:check(id,k,s[k],n[k])
 box(id,'hint',s['hint']['box'],n['hint']['box']);check(id,'hint.text',s['hint']['text'],n['hint']['text']);box(id,'actions',s['actions'],n['actions'])
 check(id,'buttons.count',len(s['buttons']),len(n['buttons']))
 for i,(a,b)in enumerate(zip(s['buttons'],n['buttons'])):
  box(id,'button'+str(i),a['box'],b['box'])
  for k in ['action','enabled','label','name','variant']:check(id,'button'+str(i)+'.'+k,a[k],b[k])
  check(id,'button'+str(i)+'.loading',a['icon']=='spinner',b['loading'])
  if a['icon']!='spinner':check(id,'button'+str(i)+'.icon',icon(a['icon']),icon(icons[b['iconName']]))
result=dict(platform=osname,cases=132,failed=len(errors),scope='original 22 footer branch presentations ×2 languages ×3 real viewport widths; layout/update/descriptor/icon checks; page branch decisions and actual input are separate',errors=errors)
(r/f'actionfooter-{osname}-comparison.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n');print({k:v for k,v in result.items()if k not in ['scope','errors']});print(errors[:20]);sys.exit(bool(errors))
