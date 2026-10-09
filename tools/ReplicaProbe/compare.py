"""Compare the source-derived browser atlas with Avalonia, retaining raster differences."""
from pathlib import Path
from PIL import Image, ImageChops, ImageFilter
import json
root=Path(__file__).resolve().parents[2]; r=root/'docs/replica'
web=Image.open(r/'screenshots/svg-atlas-web.png').convert('RGB')
native=Image.open(r/'screenshots/svg-atlas-native-1x.png').convert('RGB')
assert web.size==native.size,(web.size,native.size)
cases=json.loads((r/'source/svg-cases.json').read_text()); results=[]
def mask(image):
    output=Image.new('L',image.size)
    output.putdata([255 if max(abs(v-bg) for v,bg in zip(p,(49,51,56)))>40 else 0 for p in image.get_flattened_data()])
    return output
for i,c in enumerate(cases):
    box=(i%10*64,i//10*64,i%10*64+64,i//10*64+64)
    a=web.crop(box);b=native.crop(box); ma=mask(a);mb=mask(b)
    missing_a=ImageChops.subtract(ma,mb.filter(ImageFilter.MaxFilter(5))).getbbox()
    missing_b=ImageChops.subtract(mb,ma.filter(ImageFilter.MaxFilter(5))).getbbox()
    difference=ImageChops.difference(a,b)
    results.append(dict(id=c['id'],passed=not missing_a and not missing_b,
        meanError=sum(sum(p) for p in difference.get_flattened_data())/(64*64*3),
        webBounds=ma.getbbox(),nativeBounds=mb.getbbox(),
        webOnlyOutside2px=bool(missing_a),nativeOnlyOutside2px=bool(missing_b)))
failed=[x['id'] for x in results if not x['passed']]
report=dict(note='Browser capture is logical pixels despite DPR 2. Compared with actual Avalonia 1× RenderTargetBitmap. Native 2× rendered independently. Isolated geometry fixture is not page parity.',
    criterion='Visible strokes must have corresponding ink within 2 logical pixels in both directions; hidden case must be empty. Tolerance covers capture and renderer antialiasing, not SVG geometry or parent layout deviations.',
    passed=len(cases)-len(failed),failed=failed,cases=results)
(r/'source/svg-pixel-comparison.json').write_text(json.dumps(report,indent=2)+'\n')
print(f'{report["passed"]}/{len(cases)} silhouette comparisons passed; {len(failed)} failed.')
assert not failed,failed
