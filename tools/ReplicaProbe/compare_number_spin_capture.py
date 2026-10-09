"""Compare only the owned macOS NumberInput captures and guarded field rectangles."""
import json
from pathlib import Path
from PIL import Image, ImageChops
ROOT=Path(__file__).resolve().parents[2]
rows=[]
for half,state in [('up','hover'),('down','hover'),('up','pressed'),('down','pressed')]:
    images=[]
    for target in ('source','native'):
        prefix=ROOT/'artifacts/replica'/f'mac-{target}-{half}-{state}-official'
        metadata=json.loads(prefix.with_suffix('.json').read_text())
        assert metadata['target']==target and metadata['mode']==state and metadata['half']==half
        field,window=metadata['rect'],metadata['windowRect']
        assert field['width']==112 and field['height']==44
        image=Image.open(prefix.with_suffix('.png')).convert('RGB')
        assert image.size==(round(window['width']*2),round(window['height']*2))
        x=round((field['x']-window['x'])*2); y=round((field['y']-window['y'])*2)
        crop=image.crop((x,y,x+224,y+88))
        crop.save(ROOT/'docs/replica/screenshots'/f'numberinput-mac-{target}-{half}-{state}-official.png')
        images.append(crop.crop((176,22,198,66)))
    pixels=list(ImageChops.difference(*images).get_flattened_data())
    rows.append(dict(half=half,state=state,physicalBox=[22,44],rgbError=sum(map(sum,pixels)),differentPixels=sum(p!=(0,0,0) for p in pixels),maxChannelDifference=max(map(max,pixels))))
    side=Image.new('RGB',(352,352))
    for index,image in enumerate(images):side.paste(image.resize((176,352),Image.Resampling.NEAREST),(index*176,0))
    side.save(ROOT/'docs/replica/screenshots'/f'numberinput-mac-{half}-{state}-official-comparison.png')
(ROOT/'docs/replica/source/numberinput-mac-final-glyph-comparison.json').write_text(json.dumps(rows,indent=2)+'\n')
print(json.dumps(rows,indent=2))
assert all(r['rgbError']==0 for r in rows), 'Actual Mac spinner paint differs'
