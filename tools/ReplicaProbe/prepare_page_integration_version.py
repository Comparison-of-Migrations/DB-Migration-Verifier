from pathlib import Path
import json
r=Path(__file__).resolve().parents[2];out=r/'artifacts/replica/windows-download'
base=json.loads((r/'docs/replica/source/root-pageintegration-file-cases.json').read_text())[0]['raw']
cases=[{'id':id,'raw':base.replace('"version":1','"version":'+raw,1)} for id,raw in [('version-decimal','1.0'),('version-exponent','1e0'),('version-string','"1"'),('version-boolean','true')]]
html=(out/'pageintegration-files.html').read_text();begin=html.index('const fileCases=');end=html.index(';document.getElementById',begin);html=html[:begin]+'const fileCases='+json.dumps(cases)+html[end:];html=html.replace('root-pageintegration-mac-input.json','root-pageintegration-version-mac-input.json');html+='''<script>document.getElementById('files-record').onclick();</script>''';(out/'pageintegration-version.html').write_text(html)
(r/'docs/replica/source/root-pageintegration-version-cases.json').write_text(json.dumps(cases,indent=2)+'\n')
