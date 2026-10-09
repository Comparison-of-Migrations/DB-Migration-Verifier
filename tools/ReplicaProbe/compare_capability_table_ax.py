"""Actual Table semantic geometry/ordered groups and reachable permission control."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('output',type=Path);a=p.parse_args();errors=[]
s=json.loads((a.folder/'source-initial-ax.json').read_text())[0];n=json.loads((a.folder/'native-initial-ax.json').read_text())[0]
def require(value,message):
 if not value:errors.append(message)
def descendants(node):
 yield node
 for child in node.get('children',[]):yield from descendants(child)
def label(node):return node.get('title')or node.get('description')or node.get('value')or''
def permissions(root):
 unique={}
 for node in descendants(root):
  if node.get('role')=='AXButton'and label(node)in['필요한 권한 보기','Required permissions']:
   key=(label(node),json.dumps(node.get('box'),sort_keys=True));unique[key]=node
 return list(unique.values())
require(n['id']=='CapabilityTable-root','Semantic Table ID missing');require(n['box']['width']==s['box']['width']==904,'Semantic inner Table width differs');require(abs(n['box']['height']-s['box']['height'])<1,'Table AX integer geometry differs')
# Chromium reflects virtual columns as well as physical rows. Candidate preserves DOM rowgroups.
require(len(n['children'])==2,'DOM rowgroup count differs');groups=n.get('children',[])
if len(groups)==2:
 require([len(x.get('children',[]))for x in groups]==[1,4],'Header/body ordered row counts differ')
 require(all(len(row.get('children',[]))==4 for group in groups for row in group.get('children',[])),'Ordered four-cell rows missing')
for node in descendants(n):
 box=node.get('box',{})
 require(box.get('width',0)>0 and box.get('height',0)>0,'Native node has empty physical AX bounds: '+node.get('id','')+' '+node.get('role',''))
sb=permissions(s);nb=permissions(n);require(len(sb)==len(nb)==1,'Permission AX control is missing/duplicated')
if len(sb)==len(nb)==1:
 for dimension in ['width','height']:require(abs(sb[0]['box'][dimension]-nb[0]['box'][dimension])<1.01,'Permission AX '+dimension+' differs beyond Chromium integer enclosure')
 for coordinate in ['x','y']:
  left=sb[0]['box'][coordinate]-s['box'][coordinate];right=nb[0]['box'][coordinate]-n['box'][coordinate];require(abs(left-right)<1.01,'Permission AX relative '+coordinate+' differs')
report={'source':'source-initial-ax.json','candidate':'native-initial-ax.json','errors':errors,'passed':not errors,'sourceIntegerEnclosureTolerance':1.01,'strictComputedGeometryEvidence':'capabilitytable-mac-actual-comparison.json'};a.output.write_text(json.dumps(report,indent=2)+'\n');print('Actual Table AX;',len(errors),'differences');raise SystemExit(bool(errors))
