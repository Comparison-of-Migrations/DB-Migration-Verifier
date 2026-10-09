"""Host-only composition; no launches or measurements. Scope belongs to MainContent parent."""
from pathlib import Path
import json,hashlib
root=Path(__file__).resolve().parents[2]
completion=root/'artifacts/replica/workers/preflight/mainpadding-completion';completion.mkdir(exist_ok=True)
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def evidence(p):
 assert p.is_file(),p
 return {'path':str(p.relative_to(root)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
base=root/'artifacts/replica/workers/preflight/pageintegration'
old=base/'ef5b263a66f5492088acc9b2949deeec';new=base/'5e3bffb5d9f5457497cece6d5b19a8ce'
windows=old/'actual-main-padding-windows';original=windows/'browser-reference';first=original/'091e8ab0880143cb80d765c38a7d0c27';rest=original/'0be6eafd94424a3094e2266ff96a5edf';native=new/'actual-main-padding-windows/native'
source=read(rest/'comparison.json');candidate=read(native/'comparison.json');assert len(source['checks'])==len(candidate)==4
frozenOld=read(old/'build.json');frozenNew=read(new/'build.json')
product={p:sha for p,sha in frozenNew['sourceHashes'].items() if p.startswith('src/')};assert all(frozenOld['sourceHashes'][p]==sha for p,sha in product.items())
originalHashes={ext:hashlib.sha256((root/f'docs/replica/source/original.{ext}').read_bytes()).hexdigest() for ext in ['js','css']}
checks=[]
for actual in candidate:
 key=actual['id'];reference=next(c for c in source['checks'] if c['id']==key);folder=(first if key=='ko-960' else rest)/key
 cleanup=read(folder/'cleanup.json');assert cleanup['sourceWindow']==cleanup['profileProcesses']==0
 ready=read(folder/'ready.json');assert ready['originalHashes']==originalHashes
 raw=read(first/'latest-post.json') if key=='ko-960' else read(folder/'click.json')
 for kind in ['pointerdown','pointerup','click']:
  event=next(e for e in raw['events'] if e['type']==kind);assert event['isTrusted'] and event['target']['id']=='main'
 assert reference['focusAfterClick']['tag']=='BODY' and reference['wheelOffset']==100 and reference['focusAfterWheel']['id']=='screen-title'
 assert actual['wheelOffset']==100 and actual['focusAfterWheel']['id']=='screen-title' and actual['focusAfterClick']['type']=='WebScrollViewport' and not actual['isFocusVisible'] and not actual['isTabStop']
 final=read(native/(key+'-final.json'));assert final['hit']['viewportAncestor'] and final['offset']==100
 guard=read(native/(key+'-click-guard.json'));assert guard['mode']=='click' and guard['sent'] and guard['hitPid']==guard['pid'] and guard['hitRoot']==guard['hwnd'] and guard['rawEvents']==['down','up']
 if key!='ko-960':
  wheelguard=read(native/(key+'-wheel-guard.json'));assert wheelguard['sent'] and wheelguard['mode']=='wheel' and wheelguard['hitPid']==wheelguard['pid']
 if key=='ko-960':
  prior=read(windows/'native'/sorted(p.name for p in (windows/'native').glob('main-padding-native-state-*.json'))[-1]);assert prior['offset']==100 and prior['focus']['id']=='screen-title'
 checks.append({'id':key,'pass':True,'sourceMethod':'Actual Windows nonheadless Edge scoped browser Input API click; KO saved OS wheel reused / EN browser wheel fresh','source':reference,'candidateMethod':'Actual Windows OS SendInput; KO960 prior completed physicalwheel reused by explicitly labeled fixture offset100, all four physicalclicks and other three physicalwheels fresh','candidate':actual,'sourceReady':evidence(folder/'ready.json'),'sourceTrustedEvents':evidence(first/'latest-post.json' if key=='ko-960' else folder/'click.json'),'sourceBrowserWire':evidence(folder/'browser-input-wire.json'),'sourcePhysicalCaptureDiagnostic':evidence(folder/'physical-capture-diagnostic.txt'),'sourceIdentity':evidence(folder/'owned-identity.json'),'sourceTarget':evidence(folder/'browser-target.json'),'sourceCleanup':evidence(folder/'cleanup.json'),'candidateFinal':evidence(native/(key+'-final.json')),'candidateClickGuard':evidence(native/(key+'-click-guard.json')),'settledReadonly':evidence(folder/'settled-readonly.json') if key!='ko-960' else {'boundary':'Reuses saved trusted pointerdown.after650ms BODY; up/click trusted but their timers absent, no repeatedclick'}})
cleanup=read(native/'cleanup.json');assert cleanup['nativeProcess']==cleanup['nativeWindow']==0
proof={'phase':'ACTUAL_WINDOWS_PASSED','scope':'New blank left MainContent padding only; KO/EN 960/1280x900 overflow Trial; geometry/child matrices reused','checks':checks,'originalHashes':originalHashes,'frozenProductHashes':product,'sourceComparison':evidence(rest/'comparison.json'),'nativeComparison':evidence(native/'comparison.json'),'nativeLaunch':evidence(native/'launch.json'),'nativeInputTrace':evidence(native/'input-trace.txt'),'nativeCleanup':evidence(native/'cleanup.json'),'priorPhysicalWheel':evidence(windows/'native'/sorted(p.name for p in (windows/'native').glob('main-padding-native-state-*.json'))[-1]),'priorNativeCleanup':evidence(windows/'native/cleanup.json'),'frozenOld':evidence(old/'build.json'),'frozenCorrectedObserver':evidence(new/'build.json'),'productSourceBytesUnchangedBetweenSnapshots':True,'focusBoundary':{'source':'Settled BODY after blankclick','candidate':'Pointer-selected WebScrollViewport internalfocus, outlinefalse and IsTabStopfalse','contract':evidence(root/'docs/replica/specs/ScrollViewport.md'),'purpose':'Existing locked native keyboard scroll delivery mapping, no source/body literal-peer claim and no new productfocus correction'},'methodBoundary':'Original reference browser API inputs are trusted actual-rendered Edge, explicitly not OS SendInput sourceclick proof. Candidate uses actual OS SendInput. No browser API substitutes for candidate OS gate.','historicalFailures':['Source OS clicks from diagnostic8632/12652 phaseuncertain; 11376 down/upforegroundguardfailed; 5916 sent2FGchanged/noDOM; 12220 sent2FGretained/noDOM. These remain diagnostic failures, not productpasses or OS exemptions.','Source browserKO960 completed click but up/click timers absent; trusted events and settled pointerdown.after650ms reused without reinjection.','Native7428 actualwheel reached100/H1; old helper asserted before animation settled. Observer repaired to wait100; no completedwheel replay.'],'cleanupBoundary':'Only exact owned validation windows/privateprofiles/runtime. User tabs/windows and unproven csrss diagnostic are excluded; unknown/other applications were never closed. CLI scopes finished; no remaining owned actor/input.'}
(completion/'windows-verification.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2)+'\n')
mac=read(completion/'mac-verification.json');assert len(mac['checks'])==4 and all(c['pass'] for c in mac['checks'])
allproof={'phase':'SCOPE_VERIFIED','component':'PageIntegrationMainPadding (MainContent parent affected input scope)','macOS':evidence(completion/'mac-verification.json'),'windows':evidence(completion/'windows-verification.json'),'sourceDOM':evidence(root/'artifacts/replica/workers/preflight/mainpadding-source/463b51bd272f400f85316c4ee234ea42/source.json'),'headlessAffected8':evidence(root/'docs/replica/source/preflight-mainpadding-mac-headless-comparison.json'),'onlyParentProductFix':'Root existing ReplicaWorkspace.Viewport.Background=Brushes.Transparent; locked ScrollViewport default/children unchanged','productSourceHashes':product,'protectedAndParentSource':'Hash map is exact snapshot sources; upper LOCK remains root responsibility','scopeCounts':{'macActualCases':4,'windowsActualCases':4,'renderedAffectedPoints':8},'remaining':[],'cleanup':'Every completed owned source/native/headless scope immediately closed exact0; see per-scope records. No user tab/all-browser0 claim.'}
cleanupRecords=[root/c['source']/'cleanup.json' for c in mac['checks']]
cleanupRecords.append(root/'artifacts/replica/workers/preflight/pageintegration/f2ffe9d191dc49dab03ea789202acde6/actual-main-padding-mac/cleanup.json')
cleanupRecords.extend(windows.rglob('cleanup.json'));cleanupRecords.append(native/'cleanup.json')
cleanupRecords.append(root/'artifacts/replica/workers/preflight/mainpadding-source/463b51bd272f400f85316c4ee234ea42/cleanup.json')
allproof['ownedCleanupInventory']=[dict(evidence(p),record=read(p)) for p in sorted(set(cleanupRecords))]
allproof['currentProductSourceHashesMatchFrozen']=all(hashlib.sha256((root/p).read_bytes()).hexdigest()==sha for p,sha in product.items())
assert allproof['currentProductSourceHashesMatchFrozen']
(completion/'verification.json').write_text(json.dumps(allproof,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(evidence(completion/'verification.json')))
