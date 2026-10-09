"""Investigation: compare 1x browser JPEG and 2x CoreText raster ink positions.
Capture antialiasing/JPEG differences are reported, not used to change CSS metrics.
"""
from pathlib import Path
from PIL import Image
import json
ROOT = Path(__file__).resolve().parents[2]
S = ROOT / 'docs/replica/screenshots'
web = Image.open(S/'typography-web-oct05.jpg').convert('RGB').crop((0,0,1000,1040))
native = Image.open(S/'typography-coretext-native-2x.png').convert('RGB').resize((1000,1040), Image.Resampling.LANCZOS)
cases = json.loads((ROOT/'docs/replica/source/typography-cases.json').read_text())
def ink(im):
    mask = Image.new('1', im.size)
    mask.putdata([max(p)>95 for p in im.getdata()])
    return mask.getbbox()
rows=[]
for i,c in enumerate(cases):
    x=(i%2)*500; y=(i//2)*130
    box=(x+5,y+5,x+495,y+125)
    a=ink(web.crop(box)); b=ink(native.crop(box))
    rows.append({'id':c['id'],'webInkBounds':a,'coreTextInkBounds':b,'difference':None if a is None or b is None else [b[j]-a[j] for j in range(4)]})
comparison=Image.new('RGB',(2000,1040));comparison.paste(web,(0,0));comparison.paste(native,(1000,0))
comparison.save(S/'typography-web-coretext-comparison.png')
(ROOT/'docs/replica/source/typography-coretext-raster-comparison.json').write_text(json.dumps({'scope':'ink bounds threshold max RGB>95; browser JPEG 1x vs native 2x downsample; not exact pixel assertion','cases':rows},indent=2))
print(json.dumps(rows,indent=2))
