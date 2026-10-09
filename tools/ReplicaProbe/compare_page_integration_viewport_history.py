"""Strict new prior-gutter histories; glyph ink boxes remain renderer evidence, not control boxes."""
from pathlib import Path
import argparse,json,re
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('candidate',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
s=json.loads(a.source.read_text());c=json.loads(a.candidate.read_text());differences=[];checks=0

def equal(identity,key,expected,actual):
    global checks
    checks+=1
    if expected!=actual:differences.append(dict(id=identity,key=key,source=expected,candidate=actual))
def number(identity,key,expected,actual):
    global checks
    checks+=1
    if not isinstance(actual,(int,float)) or abs(expected-actual)>.001:differences.append(dict(id=identity,key=key,source=expected,candidate=actual))
def box(identity,key,expected,actual):
    for axis in ('x','y','width','height'):number(identity,key+'.'+axis,expected[axis],actual.get(axis) if isinstance(actual,dict) else None)
equal('scope','source', 'parent-resize-history6',s.get('scope'));equal('scope','candidate','parent-resize-history6',c.get('scope'))
equal('paths','ids',[r['id'] for r in s['rows']],[r['id'] for r in c['rows']])
for source,native in zip(s['rows'],c['rows']):
    equal(source['id'],'widths',source['widths'],native['widths']);equal(source['id'],'states',len(source['states']),len(native['states']))
    for index,(left,right) in enumerate(zip(source['states'],native['states'])):
        identity=source['id']+'/'+str(index)
        for field in ('width','height','dpr','pageSame','headingSame','summarySame','media','publicState'):equal(identity,field,left[field],right.get(field))
        equal(identity,'source animations',0,left['pageAnimations']);equal(identity,'source loaded font','loaded',left['fontReady']);equal(identity,'noEntry',True,right.get('noEntry'));equal(identity,'opacity',1,right.get('pageOpacity'))
        for field in ('page','heading','summary','choice'):box(identity,field,left[field],right[field])
        box(identity,'main.box',left['main']['box'],right['main']['box'])
        for field in ('clientWidth','clientHeight','scrollHeight','scrollTop','gutter'):number(identity,'main.'+field,left['main'][field],right['main'].get(field))
        # CSS shorthand uses top/right/bottom/left expansion. Compare numerical CSS padding.
        padding=[float(v.removesuffix('px')) for v in left['main']['padding'].split()]
        if len(padding)==2:padding*=2
        elif len(padding)==1:padding*=4
        elif len(padding)==3:padding.append(padding[1])
        for field,value in zip(('top','right','bottom','left'),padding):number(identity,'main.padding.'+field,value,right['main']['padding'].get(field))
        equal(identity,'items.length',len(left['items']),len(right['items']))
        for i,(item,actual) in enumerate(zip(left['items'],right['items'])):
            prefix='items['+str(i)+']';box(identity,prefix+'.row',item['row'],actual['row'])
            for part in ('label','value'):
                equal(identity,prefix+'.'+part+'.text',item[part]['text'],actual[part]['text'])
                equal(identity,prefix+'.'+part+'.lines',[line['text'].rstrip() for line in item[part]['lines']],actual[part]['lines'])
                box(identity,prefix+'.'+part+'.box',item[part]['box'],actual[part]['box'])
phase='ACTUAL_OS' if c.get('phase')=='ACTUAL_OS_RECORDED' else 'HEADLESS'
result=dict(phase=phase+('_PASSED' if not differences else '_DIFFERENCES'),scope='six exact KO default Review histories,18 settled states; prior gutter, geometry, full text/wrap and stable page/heading/summary references; no full flow/AX/timer proof',source=str(a.source.resolve()),candidate=str(a.candidate.resolve()),paths=len(s['rows']),states=sum(len(r['states']) for r in s['rows']),checks=checks,differences=differences)
a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(phase=result['phase'],checks=checks,differences=len(differences))))
raise SystemExit(bool(differences))
