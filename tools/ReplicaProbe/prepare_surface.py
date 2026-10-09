"""Isolate observed paint values; slot sizes are fixture inputs, not a page redesign."""
from pathlib import Path
import json, re, html

r=Path(__file__).resolve().parents[2]/'docs/replica'
source=json.loads((r/'source/surface-review-live.json').read_text())
cases=[]; seen=set()
for item in source['surfaces']:
    c=item['css']; key=json.dumps(c,sort_keys=True)
    if key in seen: continue
    seen.add(key)
    shadow=c['boxShadow']; values=re.findall(r'(-?[\d.]+)px',shadow)
    colour=re.search(r'rgba?\([^)]*\)',shadow)
    args=dict(background=c['backgroundColor'],border=c['borderTopColor'],
        left=float(c['borderLeftWidth'][:-2]),top=float(c['borderTopWidth'][:-2]),
        right=float(c['borderRightWidth'][:-2]),bottom=float(c['borderBottomWidth'][:-2]),
        radius=[float(c[k][:-2]) for k in ['borderTopLeftRadius','borderTopRightRadius','borderBottomRightRadius','borderBottomLeftRadius']],
        shadowColor=colour.group() if colour else 'transparent',shadowX=float(values[0]) if values else 0,
        shadowY=float(values[1]) if values else 0,blur=float(values[2]) if values else 0,
        spread=float(values[3]) if values else 0,inset='inset' in shadow)
    assert c['backgroundImage']=='none'
    assert len({c[k] for k in ['borderTopColor','borderRightColor','borderBottomColor','borderLeftColor']})==1
    for variant in [0,1]:
        cases.append(dict(id=f'surface-{len(cases)}',source=item['cls'] or item['tag'],
            width=min(200,item['rect']['width'])+variant*.5,height=min(60,item['rect']['height'])+variant*.25,
            css=c,**args))
(r/'source/surface-cases.json').write_text(json.dumps(cases,ensure_ascii=False,indent=2)+'\n')
out=['<!doctype html><meta charset="utf-8"><title>Source surface paint fixture</title><style>body{margin:0;background:#313338}#atlas{display:grid;grid-template-columns:repeat(3,240px);width:720px}.cell{height:100px;display:flex;align-items:center;justify-content:center}.paint{box-sizing:border-box;flex:none}</style><div id="atlas">']
for case in cases:
    c=case['css'];style=f'width:{case["width"]}px;height:{case["height"]}px;background:{case["background"]};'
    for side in ['Top','Right','Bottom','Left']:style+=f'border-{side.lower()}:{c[f"border{side}Width"]} solid {c[f"border{side}Color"]};'
    style+='border-radius:'+' '.join(str(v)+'px' for v in case['radius'])+';box-shadow:'+c['boxShadow']+';'
    out.append(f'<div class="cell"><div class="paint" data-id="{case["id"]}" style="{html.escape(style,quote=True)}"></div></div>')
out.append('</div>');(r/'source/surface-reference.html').write_text('\n'.join(out))
print(f'{len(cases)} paint cases; 720×{((len(cases)+2)//3)*100} reference.')
