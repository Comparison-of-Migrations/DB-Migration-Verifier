"""Fresh raw topology and actual ControlView names/positive bounds, Windows only."""
from pathlib import Path
import argparse,json
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('folder',type=Path);a=p.parse_args()
def tree(d,i):
 n=dict(d[i]);i+=1;c=[]
 for _ in range(n['children']):v,i=tree(d,i);c.append(v)
 n['nodes']=c;return n,i
def first_table(d):return tree(d,next(i for i,x in enumerate(d)if x['type']=='ControlType.Table'))[0]
def positive(n):assert n['box']['width']>0 and n['box']['height']>0
results=[]
source=json.loads((r/'docs/replica/source/trialpage-windows-source.json').read_text())
for lang in ['ko','en']:
 raw_s=json.loads((a.folder/('source-'+lang+'-rawview-ax.json')).read_text());raw_n=json.loads((a.folder/('native-'+lang+'-rawview-ax.json')).read_text());cv_s=json.loads((a.folder/('source-'+lang+'-controlview-ax.json')).read_text());cv_n=json.loads((a.folder/('native-'+lang+'-controlview-ax.json')).read_text())
 sr,nr=first_table(raw_s),first_table(raw_n);st,nt=first_table(cv_s),first_table(cv_n)
 assert len(sr['nodes'])==4 and len(nr['nodes'])==2
 assert all(g['className']=='RowGroup' and not g['isControl'] and not g['isContent'] for g in nr['nodes'])
 assert sum(len(g['nodes']) for g in nr['nodes'])==4
 assert len(st['nodes'])==len(nt['nodes'])==4
 for index,(srow,nrow) in enumerate(zip(st['nodes'],nt['nodes'])):
  assert srow['type']==nrow['type']=='ControlType.DataItem' and srow['name']==nrow['name']==''
  assert len(srow['nodes'])==len(nrow['nodes'])==3
  positive(srow);positive(nrow)
  for sc,nc in zip(srow['nodes'],nrow['nodes']):
   assert sc['name']==nc['name'] and sc['name'] and sc['children']==nc['children']==0
   assert sc['type']=='ControlType.DataItem' and nc['type']==('ControlType.HeaderItem' if index==0 else 'ControlType.DataItem')
   positive(sc);positive(nc)
 positive(st);positive(nt)
 case=next(x for x in source['rows'] if x['id']==lang+'-1280-default');headings=[case['title']['text']]+[x['text']for x in case['subheadings']];labels=[x['label']['text']for x in case['fields']];consent=case['consent']['label']['text']
 for text in headings:assert any(x['name']==text and x['type']=='ControlType.Text' for x in cv_s) and any(x['name']==text and x['type']=='ControlType.Text' for x in cv_n)
 for text in labels:assert len([x for x in cv_s if x['name']==text and x['type']=='ControlType.Edit'])==len([x for x in cv_n if x['name']==text and x['type']=='ControlType.Edit'])==1
 assert len([x for x in cv_s if x['name']==consent and x['type']=='ControlType.CheckBox'])==len([x for x in cv_n if x['name']==consent and x['type']=='ControlType.CheckBox'])==1
 results.append({'language':lang,'sourceRawTableRows':4,'nativeRawGroupCount':2,'nativeRawRows':4,'rawGroupsControlContent':False,'managedVisualGroupControlContent':False,'sourceNativeControlViewRows':4,'primaryNamedCells':12,'positiveBounds':True,'headingNames':4,'fieldNames':4,'consentName':1,'nativeHeaderRole':'HeaderItem','sourceHeaderRole':'DataItem(column header)','rawGroupMapping':'source raw Table→Row4; candidate raw Table→noncontrol/noncontent visualGroup2→Row4; actual ControlView source/native both Table→Row4; rawdifference preserved'})
(r/'docs/replica/source/trialpage-windows-ax-comparison.json').write_text(json.dumps({'cases':results,'errors':[]},ensure_ascii=False,indent=2));print('Windows raw/ControlView named cells12, rows4, controls9 KOEN passed')
