"""Compare source CSS/layout contracts, including updates of the same attached pill."""
import argparse,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('platform',choices=['mac','windows']);a=p.parse_args()
source=ROOT/'docs/replica/source'/('statuspill-mac-source.json' if a.platform=='mac' else 'windows-source-statuspill.json')
s=json.loads(source.read_text());n=json.loads((ROOT/'docs/replica/source'/f'statuspill-{a.platform}-native/statuspill-native.json').read_text())
def color(value):
    if value.startswith('#'):
        value=value[1:];return tuple(int(value[i:i+2],16) for i in [2,4,6,0])
    values=[float(x) for x in re.findall(r'[\d.]+',value)];return tuple(values[:3])+((values[3]*255 if len(values)>3 else 255),)
errors=[];maximum_width=0;maximum_dot=0
assert n['scale']==s['dpr']==2
for mode in ('rows','updates'):
    assert len(n[mode])==len(s['rows'])
    for expected,actual in zip(s['rows'],n[mode]):
        def check(name,want,got,tolerance=0):
            if (abs(want-got)>tolerance if isinstance(want,(int,float)) and not isinstance(want,bool) else want!=got):errors.append(dict(mode=mode,id=expected['id'],property=name,expected=want,actual=got))
        for key in ('id','text','kind','category','dot'):check(key,expected[key],actual[key])
        # Typography's existing advance contract is one CSS LayoutUnit, not arbitrary visual tolerance.
        width_error=abs(expected['rect']['width']-actual['rect']['width']);maximum_width=max(maximum_width,width_error)
        check('width',expected['rect']['width'],actual['rect']['width'],1/64)
        check('height',expected['rect']['height'],actual['rect']['height'],1e-8)
        check('foreground',color(expected['style']['foreground']),color(actual['foreground']))
        check('background',color(expected['style']['background']),color(actual['background']))
        check('padding','8,3,8,3',actual['padding']);check('radius',5,actual['radius']);check('border',0,actual['border'])
        check('fontSize',12,actual['fontSize']);check('fontWeight',400,actual['fontWeight']);check('focusable',False,actual['focusable']);check('tabStop',False,actual['tabStop'])
        if expected['dotRect']:
            assert actual['dotRect'] is not None
            for key in ('x','y','width','height'):
                maximum_dot=max(maximum_dot,abs(expected['dotRect'][key]-actual['dotRect'][key]))
                check('dot.'+key,expected['dotRect'][key],actual['dotRect'][key],1e-8)
        else:check('dotRect',None,actual['dotRect'])
        if expected['textRect']:
            check('label.x',20 if expected['dot'] else 8,actual['labelRect']['x'],1e-8)
            check('lineBoxHeight',expected['rect']['height']-6,actual['labelRect']['height'],1e-8)
        else:check('labelRect',None,actual['labelRect'])
report=dict(scope='Passive .pill CSS/layout/visible-dot/attached updates; no full-page or raster bit-perfect claim',platform=a.platform,scale=n['scale'],checks=len(s['rows'])*2,failed=len(errors),maximumWidthDifference=maximum_width,maximumDotDifference=maximum_dot,errors=errors)
(ROOT/'docs/replica/source'/f'statuspill-{a.platform}-comparison.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='errors'},indent=2));print(errors[:8]);assert not errors
