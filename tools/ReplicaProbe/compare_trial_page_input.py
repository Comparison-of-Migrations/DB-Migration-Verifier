"""Actual source/native field reset and consent rendering, KOEN fourteen new scopes."""
from pathlib import Path
import json,sys
r=Path(__file__).resolve().parents[2];os=sys.argv[1];folder=Path(sys.argv[2]);checks=[]
for lang in ['ko','en']:
 for phase in ['field0','field1','field2','field3','pointer','space','enter']:
  sf=folder/f'source-{lang}-{phase}.json'
  if os=='mac' and lang=='ko' and phase=='field0':sf=folder/'source-ko-field0-corrected.json'
  s=json.loads(sf.read_text());n=json.loads((folder/f'native-{lang}-{phase}.json').read_text());v=n['view'];assert s['english']==v['English']==(lang=='en')
  for source,native in [('host','Host'),('db','Database'),('validInput','ValidInput'),('invalidInput','InvalidInput'),('consent','Consent'),('ready','Ready')]:assert s['trial'][source]==v[native],(lang,phase,source,s['trial'][source],v[native])
  assert s['error']==v['Error'] and v['Error'] and not v['Ready'] and s['errorVisible'] and not s['okVisible']
  expected='trial-'+['host','db','validInput','invalidInput'][int(phase[-1])] if phase.startswith('field') else 'flag-trial.consent'
  assert n['focused']==expected and s['focus']==('trial.consent' if expected=='flag-trial.consent' else expected)
  if phase.startswith('field'):
   i=int(phase[-1]);assert s['fields'][i]['retained'] and s['fields'][i]['selectionStart']==13 and s['fields'][i]['value']=='owned-example' and not v['Consent']
  else:assert v['Consent']==(phase=='pointer')
  checks.append({'language':lang,'phase':phase,'source':sf.name,'native':f'native-{lang}-{phase}.json'})
(r/f'docs/replica/source/trialpage-{os}-input-comparison.json').write_text(json.dumps({'cases':checks,'errors':[]},ensure_ascii=False,indent=2));print('Actual',os,'14 input source/native scopes passed')
