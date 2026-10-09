"""Confirm native focus paint against the original computed outline colors."""
from pathlib import Path
from PIL import Image
import json,re
r=Path(__file__).resolve().parents[2];cases=json.loads((r/'docs/replica/source/sidebaritem-cases.json').read_text())
for platform in ['mac','windows']:
 s=json.loads((r/f'docs/replica/source/sidebaritem-{platform}-source.json').read_text());n=json.loads((r/f'docs/replica/source/sidebaritem-{platform}-native/sidebaritem-native.json').read_text());image=Image.open(r/f'docs/replica/source/sidebaritem-{platform}-native/sidebaritem-native.png').convert('RGBA');checks=[]
 for row,(c,x,y) in enumerate(zip(cases,s['rows'],n['rows'])):
  if c['interaction']!='focus-visible':continue
  match=re.search(r'rgb\((\d+), (\d+), (\d+)\)',x['style']['outline']);assert match is not None
  expected=tuple(map(int,match.groups()))+(255,);actual=image.getpixel((51,round((30+row*110+y['height']/2)*2)))
  checks.append(dict(id=c['id'],expected=expected,actual=actual,passed=expected==actual));assert expected==actual,(platform,checks[-1])
 (r/f'docs/replica/source/sidebaritem-{platform}-focus-paint.json').write_text(json.dumps(dict(scope='Original computed focus color versus exact native PNG left outline center; includes synthetic disabled CSS endpoint, not a claim disabled controls accept focus',checks=checks),indent=2)+'\n');print(platform,len(checks),'exact focus color pixels passed')
