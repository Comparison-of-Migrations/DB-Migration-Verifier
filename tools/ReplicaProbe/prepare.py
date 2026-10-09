"""Prepare an isolated reference from captured web SVG definitions, without changing the product web."""
from pathlib import Path
import json, math
root=Path(__file__).resolve().parents[2]
r=root/'docs/replica'
icons=json.loads((r/'source/icon-definitions.json').read_text())
cases=[]
def add(id,name,size=20,height=None,color='#b5bac1',stroke=1.7,rotation=0,visible=True,inherited=False,opacity=1):
    cases.append(dict(id=id,name=name,width=size,height=height or size,color=color,strokeWidth=stroke,rotation=rotation,visible=visible,inherited=inherited,opacity=opacity))
for name in icons:
    for size in [14,15,18,19,20,22,30,58]:
        add(f'{name}-{size}',name,size,stroke=1.3 if size==58 else 1.7)
for i,(color,rotation,visible,inherit,opacity) in enumerate([
    ('#ffffff',0,True,False,1),('#f0c47c',0,True,False,1),('#ff959d',0,True,False,1),('#6bdda3',0,True,False,1),
    ('#b5bac1',90,True,True,1),('#b5bac1',0,False,False,1),('#dbdee1',0,True,True,.4),('#5865f2',0,True,True,1)]):
    add(f'state-{i}','chevron',18,color=color,rotation=rotation,visible=visible,inherited=inherit,opacity=opacity)
add('fallback','unknown');add('letterbox','db',40,20)
(r/'source/svg-cases.json').write_text(json.dumps(cases,indent=2)+'\n')
html=['<!doctype html><meta charset="utf-8"><title>Original SVG geometry comparison fixture</title><style>html,body{margin:0;background:#313338}#atlas{display:grid;grid-template-columns:repeat(10,64px);width:640px}.cell{height:64px;display:flex;align-items:center;justify-content:center}.svg{display:block;fill:none;stroke:currentColor;stroke-width:1.7;stroke-linecap:round;stroke-linejoin:round;transform-origin:center}</style><div id="atlas">']
for c in cases:
    style=f"width:{c['width']}px;height:{c['height']}px;stroke-width:{c['strokeWidth']};transform:rotate({c['rotation']}deg);display:{'block' if c['visible'] else 'none'}"
    html.append(f'<div class="cell" style="color:{c["color"]};opacity:{c["opacity"]}" data-id="{c["id"]}"><svg class="svg" viewBox="0 0 24 24" style="{style}" aria-hidden="true">{icons.get(c["name"],icons["info"])}</svg></div>')
html.append('</div>');(r/'source/svg-reference.html').write_text('\n'.join(html))
print(f'{len(cases)} cases; reference atlas 640×{math.ceil(len(cases)/10)*64}.')
