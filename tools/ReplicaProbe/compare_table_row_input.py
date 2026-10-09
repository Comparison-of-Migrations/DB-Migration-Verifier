"""Compare actual row input evidence against original DOM/CSS behavior and event boundaries."""
from pathlib import Path
import json

root=Path(__file__).resolve().parents[2]/'docs/replica/source'
source={x['name']:x for x in json.loads((root/'tablemappingrow-live-input.json').read_text())}
assert not source['enter-check']['selected'] and source['enter-check']['checkboxFocusVisible']
assert source['space-check']['selected'] and source['choice-click']['selected']
assert source['target-click']['selected'] and not source['target-click']['checkboxFocus']
assert all(source[x]['dialogOpen'] and source[x]['selected'] for x in ['change-click','verified-enter-change','verified-space-change'])
expectations={
 'cancel-held-inside':dict(selected=False,selections=0,changes=0,hover=True),
 'cancel-held-outside':dict(selected=False,selections=0,changes=0,hover=False),
 'actual-choice-cancel':dict(selected=False,selections=0,changes=0,hover=False,background='#00000000'),
 'actual-held':dict(selected=False,selections=0,changes=0,hover=True,background='#ff3a3c44'),
 'actual-choice-hold':dict(selected=True,selections=1,changes=0,checkboxFocus=True,checkboxFocusVisible=False,hover=True,background='#ff343640'),
 'actual-target-click':dict(selected=True,selections=1,changes=0,checkboxFocus=False,buttonFocus=False,hover=True,background='#ff343640'),
 'actual-change-click':dict(selected=True,selections=1,changes=1,checkboxFocus=False,buttonFocus=True,hover=True,background='#ff343640'),
 'normal-final-unselect':dict(selected=False,selections=2,changes=1,hover=True,background='#ff3a3c44'),
 'normal-final-hover':dict(selected=False,selections=2,changes=1,hover=True,background='#ff3a3c44'),
 'normal-final-leave':dict(selected=False,selections=2,changes=1,hover=False,background='#00000000'),
 'key-enter-check':dict(selected=False,selections=2,changes=1,checkboxFocus=True,checkboxFocusVisible=True),
 'key-space-check':dict(selected=True,selections=3,changes=1,checkboxFocus=True,checkboxFocusVisible=True),
 'key-enter-change':dict(selected=True,selections=3,changes=2,buttonFocus=True),
 'key-space-change':dict(selected=True,selections=3,changes=3,buttonFocus=True),
 'reduced-hover-selected':dict(selected=True,selections=3,changes=3,reduced=True,hover=True,background='#ff343640'),
 'reduced-choice-click':dict(selected=False,selections=4,changes=3,reduced=True,hover=True,background='#ff3a3c44'),
 'reduced-hover-unselected':dict(selected=False,selections=4,changes=3,reduced=True,hover=True,background='#ff3a3c44'),
 'reduced-leave':dict(selected=False,selections=4,changes=3,reduced=True,hover=False,background='#00000000'),
}
errors=[]
for platform in ['mac','windows']:
 folder=root/f'tablemappingrow-{platform}-input'
 build=root.parents[2]/'artifacts/replica'/('tablerow-mac-app'if platform=='mac'else'windows-arm64-check')/'Verifier.Desktop.dll'
 published=build.stat().st_mtime_ns
 for name,expected in expectations.items():
  file=folder/(name+'.json')
  assert file.stat().st_mtime_ns>=published, f'Stale input evidence: {platform}/{name}'
  actual=json.loads(file.read_text())
  # The mapping request opens a parent modal in the original; atomic native row emits the request.
  for key,value in dict(expected,target='demo.orders',manual=False).items():
   if actual[key]!=value:errors.append(dict(platform=platform,sample=name,property=key,expected=value,actual=actual[key]))
report=dict(platforms=['mac','windows'],snapshots=36,failed=len(errors),scope='Actual label hold/cancel/release, nonfocusable-cell blur, independent change request, Enter/Space/Tab focus, normal/reduced hover and leave. Original DOM snapshots plus source row CSS endpoints. Held outside paint is an intermediate timer record; only hover/selection are asserted there. MatchingDialog is a separate parent.',errors=errors)
(root/'tablemappingrow-input-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print({k:v for k,v in report.items()if k!='errors'});print(errors[:20]);assert not errors
