"""Only new connected Trial Windows neutral and announcement/AX records."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('directory',type=Path);p.add_argument('output',type=Path);a=p.parse_args();checks=[]
def read(n):return json.loads((a.directory/n).read_text(encoding='utf-8-sig'))
for lang in ('ko','en'):
    for label in ('disabled','neutral-32','neutral-13','neutral-restore'):
        s=read(lang+'-'+label+'-source.json');v=read(lang+'-'+label+'-native.json')
        fields=('screen','english','host','database','validInput','invalidInput','consent','ready','error','primary','focus','announcement')
        candidateFocus=v['state']['focus'];neutralAlias=candidateFocus=='' and v['rawFocus'] is None and not any(c['focused'] for c in v['controls']);comparisonFocus='BODY' if neutralAlias else candidateFocus
        same=all(s['state'][f]==(comparisonFocus if f=='focus' else v['state'][f]) for f in fields)
        checks.append(dict(id=lang+'-'+label,pass_=same and s.get('documentOffset')==v.get('documentOffset') and comparisonFocus=='BODY' and not v['state']['ready'] and s['offset']==0 and v['offset']==0,sourceRawFocus=s['rawFocus'],candidateRawFocus=v['rawFocus'],candidateRawStateFocus=candidateFocus,comparisonFocus=comparisonFocus,neutralAlias=neutralAlias,sourceDocumentOffset=s.get('documentOffset'),candidateDocumentOffset=v.get('documentOffset'),documentOffsetEqual=s.get('documentOffset')==v.get('documentOffset')))
    v=read(lang+'-disabled-native.json');peer=v['announcementPeer']
    checks.append(dict(id=lang+'-announcement-peer',pass_=peer['name']==v['state']['announcement'] and bool(peer['name']) and peer['type']=='Text' and peer['live']=='Polite' and peer['control'] and peer['content'] and peer['children']==0 and peer['box']['width']>0 and peer['box']['height']>0,peer=peer))
passed=all(c['pass_'] for c in checks);result=dict(phase='ACTUAL_OS_PASSED' if passed else 'FAILED',scope='New neutral8 and actual managed announcement2; native UIA/paint separately recorded',checks=checks)
a.output.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n');print(result['phase'],len(checks));raise SystemExit(not passed)
