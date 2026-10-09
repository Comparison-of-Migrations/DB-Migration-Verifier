from pathlib import Path
import json, html
root=Path(__file__).resolve().parents[2];r=root/'docs/replica';cases=[]
for lang in ['kr','en']:
    texts=json.loads((r/f'source/typography-home-{lang}.json').read_text())['texts']
    selected=[texts[0],next(x for x in texts if x['tag']=='H1'),next(x for x in texts if x['tag']=='P' and x['cls']==''),next(x for x in texts if x['cls']=='help'),next(x for x in texts if x['cls']=='entry-hint'),next(x for x in texts if x['tag']=='H3'),next(x for x in texts if x['tag']=='P' and x['css']['fontSize']=='13px'),next(x for x in texts if x['tag']=='H2')]
    for index,t in enumerate(selected):
        css=t['css'];cases.append(dict(id=f'{lang}-{index}',text=t['text'],size=float(css['fontSize'].removesuffix('px')),lineHeight=float(css['lineHeight'].removesuffix('px')),weight=int(css['fontWeight']),spacing=0 if css['letterSpacing']=='normal' else float(css['letterSpacing'].removesuffix('px')),color=css['color'],family=css['fontFamily']))
(r/'source/typography-cases.json').write_text(json.dumps(cases,ensure_ascii=False,indent=2)+'\n')
output=['<!doctype html><meta charset="utf-8"><title>Source text metrics fixture</title><style>body{margin:0;background:#313338}#atlas{display:grid;grid-template-columns:repeat(2,500px);width:1000px}.cell{height:130px;box-sizing:border-box;padding:10px}.text{width:480px;margin:0;white-space:normal;overflow-wrap:normal}</style><div id="atlas">']
for c in cases:
    style=f'font-family:{c["family"]};font-size:{c["size"]}px;font-weight:{c["weight"]};line-height:{c["lineHeight"]}px;letter-spacing:{c["spacing"]}px;color:{c["color"]}'
    output.append(f'<div class="cell"><div class="text" data-id="{c["id"]}" style="{html.escape(style,quote=True)}">{html.escape(c["text"])}</div></div>')
output.append('</div>');(r/'source/typography-reference.html').write_text('\n'.join(output))
print('16 source-derived text metric cases, width 1000×1040.')
