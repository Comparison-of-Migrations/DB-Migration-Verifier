"""Join immutable passed callback intervals; never launch a process or replay input."""
from pathlib import Path
import hashlib, json

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'artifacts/replica/workers/preflight/pageintegration'
DEST = ROOT / 'artifacts/replica/workers/preflight/selectioncallbacks/completion'
DEST.mkdir(parents=True, exist_ok=True)

def evidence(path):
    path = Path(path)
    return {'path': str(path.relative_to(ROOT)), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}

def load(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))

mac = {
    'ko': [
        ('985e8c45f6a34b4bb07fabced17ff578/actual-selectioncallbacks-ko', 5),
        ('321a12c2b1f24a879a375954753c3d59/actual-selectioncallbacks-ko-resume-corrected-parent', 5),
        ('e713a7ee7c7d4127a8e0b15a5db02b3a/actual-selectioncallbacks-ko-resume-initial-guard-corrected', 25)],
    'en': [('e713a7ee7c7d4127a8e0b15a5db02b3a/actual-selectioncallbacks-en', 35)]}
windows = {
    'ko': [
        ('3365b87908f04491bb0554be7b104013', '0dda9c261d2e415cbb094b156d65579e', 5),
        ('3365b87908f04491bb0554be7b104013', 'fbeb22b8f00a48a793fd3f270fa4771d', 11),
        ('10c8854076b342aa8d6ef1c6c8bce8bb', '13284d99ac4742a3b15ce037e5c03157', 19)],
    'en': [
        ('10c8854076b342aa8d6ef1c6c8bce8bb', 'f317643b83aa4bc99074041fdcd717f9', 16),
        ('10c8854076b342aa8d6ef1c6c8bce8bb', 'd9bcfc53b9c746b18be3801429ba8fa8', 13),
        ('fc32ced341f6499bb002a44abf8d44f3', 'c87d053ddcb348c9abf7ba054b652662', 6)]}
aliases = {'mapping-draft-cancel': 'mapping-target-cancel', 'mapping-query-preserved': 'mapping-context', 'mapping-opener-removed': 'mapping-removal'}
utilities = {'stopped-fixture-setup': 'F6 labeled Stopped fixture', 'modal-language-handler-api': 'F9 source language handler API; inert TopBar physical click not claimed', 'language-fixture-reset': 'F9 fixture language reset', 'resize-fixture-seed': 'F7 identity/search-focus fixture', **{'resize-'+str(w): 'F8 client-width metadata fixture' for w in [960,1180,1280]}}
result = {'status': 'VERIFIED', 'scope': 'New connected Selection callback parent gate only; entire Application LOCK belongs to root', 'actual': {}, 'source': {}, 'headless': {}, 'boundaries': [], 'currentHashes': {}}
for os, batches in [('mac', mac), ('windows', windows)]:
    result['actual'][os] = {}
    for language, intervals in batches.items():
        records, parts = [], []
        for interval in intervals:
            if os == 'mac':
                folder, count = interval
                directory = BASE / folder
                comparison, cleanup = directory/'comparison.json', directory/'cleanup.json'
            else:
                snapshot, nonce, count = interval
                directory = BASE/snapshot/'actual-selectioncallbacks-windows'
                comparison, cleanup = directory/('comparison-'+nonce+'.json'), directory/('cleanup-'+nonce+'.json')
            v, c = load(comparison), load(cleanup)
            assert c['nativeProcess'] == c['nativeWindow'] == 0
            assert len(v['checks']) == count and all(x['passed'] for x in v['checks'])
            parts.append({'comparison': evidence(comparison), 'cleanup': evidence(cleanup), 'pid': c['pid'], 'passedCheckpoints': count, 'partialFollowingGuardDiagnostic': v.get('failed', None)})
            for check in v['checks']:
                literal = check['id']
                records.append({'literalId': literal, 'normalizedId': aliases.get(literal, literal), 'evidence': evidence(comparison), 'sequence': check['sequence'], 'classification': utilities.get(literal, 'Guarded actual native UI pointer/keyboard callback'), 'passed': True})
        ids = [x['normalizedId'] for x in records]
        assert len(ids) == len(set(ids)) == 35
        assert sum(x['literalId'] in utilities for x in records) == 7
        result['actual'][os][language] = {'passed': True, 'checkpoints': 35, 'guardedUICheckpoints': 28, 'explicitSetupOrAPICheckpoints': 7, 'intervals': parts, 'records': records, 'ownedNativeProcess': 0, 'ownedNativeWindow': 0}
reference = {r['normalizedId'] for r in result['actual']['mac']['en']['records']}
assert all({r['normalizedId'] for r in lang['records']} == reference for os in result['actual'].values() for lang in os.values())
source = ROOT/'artifacts/replica/workers/preflight/selectioncallbacks/source/bcb156a1156d4681b0f59ee9741f5f4c'
for os, path in [('mac', source/'original-mac.json'), ('windows', source/'windows/original-windows.json')]:
    v = load(path)
    assert v['nonce'] == 'bcb156a1156d4681b0f59ee9741f5f4c' and v['dpr'] == 2
    assert len(v['records']) == 2 and all(len(x['rows']) == 25 for x in v['records'])
    result['source'][os] = {**evidence(path), 'states': 50, 'oracle': 'Literal original DOM handler oracle, not physical OS input', 'originalHashes': v['originalHashes']}
result['source']['windows']['cleanup'] = evidence(source/'windows/cleanup.json')
assert load(source/'windows/cleanup.json')['sourceWindowCount'] == 0
result['source']['mac']['cleanupProvenance'] = 'Root contemporaneous tool close + list absence of owned IAB78; no standalone cleanup JSON. User original tab preserved; not a claim that all tabs were closed.'
for os, path in [('mac', BASE/'865608124588471fb6b1c8814b8f50ff/outputs/91cd753a88c3434eb348321e61158410/comparison.json'), ('windows', BASE/'3365b87908f04491bb0554be7b104013/headless-selectioncallbacks-windows/baa1fc3fc9f040bf85464f8d8ba32e4b/comparison.json')]:
    v = load(path)
    assert v['passed'] and v['checks'] == 848 and not v['differences']
    result['headless'][os] = {**evidence(path), 'checks': 848, 'differences': 0, 'existingPassedEvidenceReused': True}
for path in [ROOT/'src/Verifier.Desktop'/f for f in ['ReplicaWorkspace.cs','WebSelectionPage.cs','WebSelectionPage.Layout.cs','WebSelectionPage.Peers.cs','WebMatchingDialog.cs','WebScrollViewport.cs','WebSystemTextBlock.cs','WebInlineText.cs']]:
    result['currentHashes'][str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
result['revisionProof'] = evidence(BASE/'10c8854076b342aa8d6ef1c6c8bce8bb/callback-revision-proof.json')
result['boundaries'] = [
    '35 checkpoints per language per OS comprise 28 guarded UI checkpoints and 7 explicitly labeled setup/API/metadata checkpoints. F6/F7/F8/F9 are probe fixture utilities, never production user gestures.',
    'Source modal language update uses original handler API; inert TopBar physical language click is not claimed.',
    'Prior standalone12506 geometry and root52/wholeflow navigation proofs reused; no duplicate unchanged matrices.',
    'Windows startup8920/3972 diagnostics and beforeDown guards retained; successful no-redirection startup does not prove prior failure causation.',
    'Exact observed csrss PID828/HWND8586366 project-titled0xc0000142 dialog remains unproven and was not dismissed/killed; no failed snapshot PID linkage claimed. Owned candidate process/window0 is distinct from this unresolved diagnostic.',
    'Radio/tool hit mismatch and taskbar obstruction were beforeDown observations, not product failures or OS waivers.',
    'Conditional exact-owned Transform.Move and official SetWindowPos helpers record client/DPI/workarea/fresh hit guards. Final passing KO19 and EN6 default cascade positions required no move; no executed SetWindowPos proof is claimed.',
    'All passed partial intervals retained without replay. Explicit saved-context provider/handler restoration is setup only. Every completed candidate interval immediately closed/process0/window0.'
]
final_snapshot = BASE/'fc32ced341f6499bb002a44abf8d44f3'
identity = []
for relative, current in result['currentHashes'].items():
    frozen = hashlib.sha256((final_snapshot/'source'/relative).read_bytes()).hexdigest()
    identity.append({'file': relative, 'frozen': frozen, 'current': current, 'equal': frozen == current})
assert all(item['equal'] for item in identity)
result['finalFrozenProductIdentity'] = {'manifest': str((final_snapshot/'build.json').relative_to(ROOT)), 'binarySha256': load(final_snapshot/'build.json')['binarySha256'], 'currentMatchesFrozen': identity}
(DEST/'verification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n')
print(DEST/'verification.json')
print('VERIFIED original50 eachOS / headless848x2 / actual35x4 (28 UI +7 fixture/API per language) / individual candidate close0')
