"""Literal InfoDialog comparison; preserve raw default OS scrollbar offsets."""
from pathlib import Path
import argparse, copy, hashlib, json, sys
p = argparse.ArgumentParser()
p.add_argument('source', type=Path);p.add_argument('candidate', type=Path)
p.add_argument('report', type=Path);p.add_argument('--windows', action='store_true')
a = p.parse_args()
source, native = [json.loads(x.read_text()) for x in [a.source, a.candidate]]
assert len(source['rows']) == len(native['rows']) == 32
def differences(left, right, path='root'):
    errors=[]
    if isinstance(left,dict):
        for k,v in left.items(): errors.extend(differences(v,right.get(k) if isinstance(right,dict) else None,path+'.'+k))
    elif isinstance(left,list):
        if not isinstance(right,list) or len(left)!=len(right):errors.append({'path':path,'source':left,'candidate':right})
        else:
            for i,(x,y) in enumerate(zip(left,right)):errors.extend(differences(x,y,f'{path}[{i}]'))
    elif isinstance(left,bool):
        if type(right) is not bool or left!=right:errors.append({'path':path,'source':left,'candidate':right})
    elif isinstance(left,(int,float)):
        if not isinstance(right,(int,float)) or abs(left-right)>.001:errors.append({'path':path,'source':left,'candidate':right})
    elif left!=right:errors.append({'path':path,'source':left,'candidate':right})
    return errors
raw=[];errors=[];allowed=[]
for original,candidate in zip(source['rows'],native['rows']):
    row=copy.deepcopy(original);environment=row.pop('environment')
    raw.extend(differences(row,candidate,row['id']))
    gx=environment['innerWidth']-environment['clientWidth']
    gy=environment['innerHeight']-environment['clientHeight']
    if a.windows and (gx or gy):
        assert environment['minWidth']=='960px' and environment['minHeight']=='720px'
        assert environment['innerHeight'] in [400,600] and gx==15 and gy==(15 if environment['innerWidth']==960 else 0)
        row['x']+=gx/2;row['y']+=gy/2
        allowed.append({'id':row['id'],'environment':environment,'xOffset':gx/2,'yOffset':gy/2,'reason':'Measured original default Windows root scrollbar centering'})
    errors.extend(differences(row,candidate,row['id']))
errors.extend(differences(source['dpr'],native['dpr'],'dpr'))
result={'source':str(a.source.resolve()),'candidate':str(a.candidate.resolve()),'sourceSha256':hashlib.sha256(a.source.read_bytes()).hexdigest(),'candidateSha256':hashlib.sha256(a.candidate.read_bytes()).hexdigest(),'cases':32,'rawErrors':raw,'allowedOSOffsets':allowed,'errors':errors}
a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(len(errors),'differences');print(json.dumps(errors[:8],ensure_ascii=False,indent=2));sys.exit(bool(errors))
