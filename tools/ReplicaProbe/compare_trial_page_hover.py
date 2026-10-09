"""Only new Trial body-row state and private detach scope, CSS source contract."""
from pathlib import Path
import argparse,json,hashlib
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('os',choices=['mac','windows']);p.add_argument('folder',type=Path);p.add_argument('--headless',action='store_true');p.add_argument('--tag',default='');a=p.parse_args();color=lambda x:x.replace('rgba(58, 60, 68, 1)','rgb(58, 60, 68)');expected=lambda m:'rgb(58, 60, 68)' if m=='hover' else 'rgba(0, 0, 0, 0)';rows=[]
if a.headless:
 data=json.loads((a.folder/'trialpage-hover-native.json').read_text());assert len(data)==16
 for x in data:assert x['background']==expected(x['mode']) and x['callbacks']==0 and x['hover']==(x['mode']=='hover');rows.append(x)
else:
 for lang in ['ko','en']:
  for reduced in [False,True]:
   for mode in ['hover','leave','cancel']:
    label=lang+('-reduced-' if reduced else '-normal-')+mode;s=json.loads((a.folder/('source-'+label+'.json')).read_text());n=json.loads((a.folder/('native-'+label+'.json')).read_text());sr=s['tableRows'][0];nr=n['tableRows'][0];assert color(sr['background'])==color(nr['background'])==expected(mode) and sr['hover']==nr['hover']==(mode=='hover');assert sr['duration']==('0s' if reduced else '0.16s') and sr['easing']=='ease';assert len(n['changes'])==0 and s['trial']['ready']==n['view']['Ready'] and s['trial']['consent']==n['view']['Consent'];rows.append({'language':lang,'reduced':reduced,'mode':mode,'sourceBackground':sr['background'],'nativeBackground':nr['background'],'duration':sr['duration'],'callbacks':0})
result={'cases':rows,'sourceCssSha256':hashlib.sha256((r/'docs/replica/source/original.css').read_bytes()).hexdigest(),'phase':'HEADLESS_PASSED_CSS_CONTRACT' if a.headless else 'ACTUAL_SOURCE_NATIVE_PASSED','errors':[]};(r/('docs/replica/source/trialpage-'+(a.tag or a.os+('-headless' if a.headless else '-actual'))+'-hover-comparison.json')).write_text(json.dumps(result,ensure_ascii=False,indent=2));print(result['phase'],len(rows),'exact new state scopes')
