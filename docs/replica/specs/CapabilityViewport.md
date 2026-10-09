# CapabilityViewport

LOCKED · 양OS 실제검증 완료. [검증목록](../source/capabilityviewport-validation.json), [보호hash](../source/capabilityviewport-lock.json). 작업 규칙은 [MASTER_RULES](../MASTER_RULES.md), 검증 운영은 [HEADLESS_VERIFICATION](../HEADLESS_VERIFICATION.md).

직접 부모 CapabilityTable은 WAITING CHILD. 원본 table-shell의 overflow-x:auto는 computed x/y auto이고 높이는 content auto-height다. Mac fulltable KO478은 tablewidth478보다 descendant scrollWidth498이 크며, EN478은 intrinsic tablewidth615.7265625로 parent478을 넘는다. 기존 잠긴 ScrollViewport는 Sidebar 수직 계약만 검증되어 이 단위의 근거가 아니다.

## 범위
새 WebCapabilityViewport: 부모가 제공하는 logical contentwidth와 실제 descendant scroll extent를 구별하고, auto-height 가로 viewport/OS gutter, clip, offsets, actual wheel/key/thumb/track/focus/lifecycle를 검증한다. Border/radius/background와 Table column auto-layout/AX table은 부모 소유다. 보호 child를 수정하지 않는다. 구현 방법은 새 source 실측 후 결정한다.

## 원본 준비
prepare_capability_viewport.py는 literal original.css bytes와 실제 table-shell class를 사용하며 부모 border/radius만0으로 분리한다. Width478/478.25/610/656 × fit/intrinsic(+137.7265625)/descendant(+20.2734375) × height120/684.6796875:24조건, zero/fractional123.456/maximum offset:72측정. fixture authored UI recorder가 setter/actual measured offsets를 기록한다. 실행 전 값은 통과 근거가 아니다.

## 재개 · 2026-10-08
AGENTS→MASTER→중앙 상태/병렬/Headless/worker→Table WAITING CHILD 명세→literal capabilities/CSS→locked vertical viewport의 관련 계약을 실제 읽었다. verify_lock 전체통과. Viewport제품/probe 없음. 다음 DOM-only ownsource24/72→명세확정→구현/probe→독립Headless→양OSactual.

## 원본 Mac 확인
capabilityviewport-mac-source.json의 실제24×3/DPR2: auto-height120/684.6796875, gutter0, scrollWidth/clientWidth 정수반올림과 실제 box폭은 별개다. Offset123.456→123.5. Integer width478 descendant498.2734375 max20.5, fractional478.25 descendant498.5234375 max20. Source max는 device-snapped extent edge−device-snapped viewport edge로 양자화한다.

실제 Chrome 고유창/URL의 BODY pointer key10: Left/Right±40, Up/Down/Page/Home/End/Space/ShiftSpace 가로100유지. pixel wheel X100→100, Y100/CGflag ShiftY100→0. capabilityviewport-mac-input/source-*.json 및 finite13-summary.json. Source close후 cleanup sourceWindow0/nativeNeverLaunched0. 초기 freshness 두실패와 lateleft60 보존; 실제파일의 마지막 trusted ArrowLeft/60을 확인하고 이후11만 fresh boundedpoll로수행했다. 반복right 후 원본 최소화면보다작은창문제와도구기록 지연을 구분했으며, 후보완료증거로승격하지않는다.

## 구현 결정
잠긴 vertical 인스턴스의 transpose adapter는 채택하지 않는다: 그 public maximum은 round(extent−client)라 fractional478.25 원본max20를 만들면 실제viewport폭 또는ScrollWidth/Thumb분모가바뀐다. 보호 API확장없이새 owned 가로축 모델을구현한다. 기존검증된 easing/hold/chrome계산의 축전환을 사용하고 새가로동작/geometry/양OS입력은새증거로대조한다. 보호ScrollViewport파일은불변. 가로 key는Left/Right만,나머지는부모routing유지; verticalwheel은가로로 임의변환하지않는다. WindowsShiftwheel 동작은실제source검증 전미확인이다.

## 후보 첫 독립 검증
WebCapabilityViewport.cs hash17b8c7b50db1b4f9c93ad572be695454f9e3448b159abaadcddeb4ffc60e1381. 별도 가로 content host가 logicalwidth와 paintedExtent를 구별하고 crossautoheight를사용한다. Chrome paint만축전환; source키/휠/bring-into-view는가로정책으로 구현했다. 보호코드불변.

Isolated bb429352fabd45288ea019b01ab665fb build0warnings/errors, output1f4f0c8ff0604e8f80107ffa79585937. Mac source72 strict0 (capabilityviewport-headless-comparison.json), Headless key9+detach/remount/fit3 assertions 통과, 실제SkiaPNG bar/stripes/clip 관찰. 범위 HEADLESS_PASSED72+12; 아직전체단위LOCK아님. 초기556b shaderuniformdouble compilefailure,9107 다른worker draftnullabilityfailure 보존/교정후 freshbb429 사용했다.

남은 범위: sourceWindows72+finite13, Headless 미검증 ShiftSpace/wheel/childfocus/bring-into-view/resizeclamp/helddetach, 실제양OSgeometry/입력/scrollbarthumb/track/hold/overlay/paint/AX/cleanup. 기존통과72/key9/수명3은제품변경/새우려없으면반복하지않는다.

## 새 포커스 범위 근거
Own DOM-only IAB tab15 source child630/150×44 in logical800 parent478: child.focus()로 source offset322(max), focused Required permissions. capabilityviewport-mac-dom-child-focus.json; 네이티브Tab입력완료주장아님. nearest302라는추론은채택하지않는다. Focused target가offscreen이면 browser focus scroll의 center alignment/clamp를 후보에 반영하고 affected childFocus만새검증한다. 기존72/key9/수명3는그handler branch를사용하지않아근거유지. 새headless확장scope의expected도322로교정하고검증한다.

## 추가 headless 영향 범위
새focuseligibility/childFocus 분기수정후 hash8c1c8fc802edd2ec566a15a03ea3cf949d35da130d95dc9980a44193917a6fd6. 2f193 snapshot/output31cfa의 ShiftSpace/Xwheel/Ywheel/ShiftYwheel/resizeClamp/restore6 통과가개별JSON에보존됨. 같은고정크기child 추가시에parentArrange가다시실행안되어Tabstop이stale였으므로 LayoutUpdated에서eligibility 갱신으로교정했다. 이후Child.Focus만으로Avalonia가RequestBringIntoView를보내지않아offset0실패; non-pointer GotFocus child가직접BringIntoView하도록새sourcefocusedtarget 계약연결. 마지막4a6c snapshot/outputcb221d의 childFocus322/childnoparentTab/animation중detach0/remountpending0 affected4만통과. 72와oldkey9는재비교하지않음. Pointer focus와키Tab/programmatic focus의source경계는실제OS로추가확인한다.

## Windows 원본 완료
capabilityviewport-windows-source.json: 실제DPR2/72조건, fitheight120/684.6796875, overflow시shellheight+15/clientheight원본content높이. Device-snappededges/max는Mac동일. capabilityviewport-windows-input/source-*13 및 finite13-summary/batch-manifest: 단일공식SendInput배치10keys+3wheels, freshnonce/개별파일. trusted Left/Right±40, 다른8key 가로100유지. wheelXdelta100→100, wheelY100→0, 실제heldShift+Y100→100(DOMshifttrue). Mac pixelCGflagShiftY0와구분해Windows에서만ShiftY가로이동지원. 제품수정 전근거기록; affectedWindowswheelShift 및첫Windows조건만검증, Mac이미통과branch반복안함. Source창Close/cleanup0 후root반납.

## 실제 포인터 진단
Mac 첫bar drag native12/source322 불일치. 후보fixedCanvas1200×800가1250×850Window 안에서25pxcentered인paint였고 defaultAXbox는그inset없이보고되었다. SourcebarDrag322는새실제CGEvent기록으로확인, 기존13입력재비교안함. 제품계산을추측수정하지않고 owned probe canvas를Left/Top정렬하여원본fixture부모30/70와같은기준에두고 새drag/부모화면위치scope만확인한다. nativeutility CG 실패도AXdiagnosticInvoke로분리하여rawcomponent입력을유지했다.

## 재개/현재 실제 checkpoint
2026-10-08 AGENTS→MASTER→중앙상태/병렬/Headless/worker→활성명세→literal original CSS/JS→owned 제품/driver 실제읽음. verify_lock 우리 범위 전부 통과; 별도 home WelcomeSteps824 기록된 예외파일에서 전체중단(root통지), 보호변경없음.

최종제품hash ab7f10274687cd7bd25c3cb3ee1db8bc6acfa933d891adf29d072b39d492c14a. WindowsShiftY 분기추가 외 Macbranch불변. Mac actual37e native72/comparison strict0 및 source/native finite13, fit/restore childfocus322 detach/remount 완료. native1c Canvas Left/Top 진단정렬 후 drag322/source322, 최초12 실패는 failed-native-drag-before-probe-align.json에보존. 제품bar계산변경없음. 실제source生命周期7/native5와drag 비교는 capabilityviewport-mac-input-comparison.json. DOM BODY vs native 내부pointer routing/nullfocus는 named descendantfocus동일/outline없음; detached DOMzero vs nativecachedBounds는 양쪽mountedfalse/offset0/AX미노출로구분하며 visiblegeometry를면제하지않음. Mac실제paint/screenshots capabilityviewport-mac-{source-actual,native-aligned}.png, AX개별txt. sourceWindow0/nativeProcess0 cleanup-actual-checkpoint.json 후자원반납.

Windows finalShift headlessf969 extra10: capabilityviewport-windows-headless/47f0ece58bf147bd95baaba71fb18aa0.json. ProbeCanvas정렬을포함한 fresh2495 coldbuild0warnings/errors, fullruntimepack/stage, actualnative72 첫측정DPR2/strict0. 실제13input 진행중. 새문제/미검증: 양OStrack/held/overlay/Tab 및Windowsactualchild/lifecycle/paint/AX, exactcleanup. 이범위를기존passed로확대하지않으며 아직LOCK아님.

## Windows 실제 checkpoint / 남은 focus 복원
2495 실제key/wheel13 strict0, source/native sourceExtra10/nativeExtra10에서 track322/hold-left0/drag322·childfocus322/제거복귀0 일치. nativeFit2 처음 UIA diagnosticInvoke는 원본utility 실제pointerclick과 modality가달라trueoutline 실패를 failed-native-fit*-automation-modality.json에보존. 동일pointerbody setup의 영향fit2만새검사하여 false/0 일치. 기존통과13/72 반복안함. Windows19 endpoint 비교 strict0이며 track/hold endpoint2 별도대조완료. scopepaint 직접열어확인, sourceWindow0/nativeProcess0/nativeWindow0 후root반납.

추가 실제paint에서 native창재활성화의Unspecified focus가 pointerfocus를keyboard로재분류하여파란outline이생기는own제품분기문제 입증. 원본BODY/outlinefalse를포인터focus와매핑한후복원에도visibleoutline이생기면안됨. 변경사전근거: source-current false와 nativeactual PNG blueoutline, OnLostFocus가pointermode를삭제하고 OnGotFocus가Unspecified를keyboard로취급. 다음 마지막modality를보존하여Unspecified복원만교정, rawTab/childTab/restore 양OS 새scope3·headless검증. 72/13·bar계산변경없음.

AX source-child-peer/native-child-peer: named Button Required permissions/control/content/focusable 일치, sourceoffscreen true/nativefalse 차이는미면제. 설치Avalonia12.1.3 XML은 Default가 IsEffectivelyVisible 기반, FromClip 옵션이clip기반임을명시한다. NuGetrepositorycommit8eeda4f6f546165b3f72e63c9f42247abb306905와 공식 [ControlAutomationPeer](https://github.com/AvaloniaUI/Avalonia/blob/8eeda4f6f546165b3f72e63c9f42247abb306905/src/Avalonia.Controls/Automation/Peers/ControlAutomationPeer.cs#L227) 확인: default는owner/rootvisibility만, FromClip은transformedboundsclip제로판정. 현재host ClipToBounds true는렌더와입력에적용되어paint와childfocusbring322가정상이고, 기본Button peer의flag계산과구분한다. 앱에서FromClip공식옵션을parent계약으로상속시킬수있는지다음소스확인, 보호Button클래스수정없음.

### 변경 전 AX 공식 옵션 범위
공식 AutomationProperties.cs177의 FromClip attached property는비상속이다. root는 own parent Windows-scoped descendantStyle의공식옵션교정을승인했다. Viewport localStyles 안에서만 child peers의defaultflag를clip기반으로평가하며 globalstyle/보호child소스변경없음. 영향을 받는범위는Windowsoffscreen→focusbringonscreen peer2이고 visiblechild누락/숨김없음확인필요. Macbranch는바꾸지않으며 부모clip/기하72/키13/bar기통과는변경밖.

## 영향 headless 새통과/실제 대기
최종제품hash8e0ea4a8f79ff913a1c7891ee532e8d800a24f244b1d4202c6a7d476466cd180. Mac928e/output4053445a의새focus6 통과(pointerrestorefalse/rawTabviewporttrue/keyboardrestoretrue/childTab322 및namedchild유지). Windows8dc 첫3 focusassertions는 followingclip실패 전개별JSON에저장되어 focus3-before-clip-selector-repair.json으로회수했다. FromClip localStyle의 OfType<Control>는정확타입selector라Button에매칭안되어첫clip실패; Is<Control>로derivedcontrols를포함하는WindowslocalStyle만교정. Mac조건은진입하지않아통과분기불변.

Windows124fc freshfullcoldbuild0/0, independent headless55d107c385fc470f8979839ef6a2d855 영향4: actualBehaviorFromClip/clip0→offscreentrue, rawTabchildfocus322→offscreenfalse/Requiredpermissions유지. 기존통과72/13/bar는그변경밖으로재실행하지않음. Macclosedbundlee065는그Windowsselector분기수정전스냅샷이지만Macbody같은제품분기로모달리티복원검증에사용(Windows스타일실행없음). 이후LOCKmanifest는최종제품hash기준이고이Windows-only분기근거를보존한다.

source fixture focusquery는진단sentinel을DOMviewport앞에놓고utilitytabindex−1하여원본viewport의implicitTab/childTab만분리한다. 원본CSS/boxdata는불변, windowfocus observer는freshread만추가. Mac source/native officialguarded helper focus4/track-held2와Windows source/native focus4/offscreen→onscreenpeer2 준비. 실제아직실행안함·leasequeue대기. 실제현재own창/프로세스없음.

## 마지막 실제 focus 영향 checkpoint
Windows124fc/source/nativefocus4 strict0 및FromClip actualpeer2 완전일치: child clipped true→Tabfocus322→visiblefalse·named/control/content보존. focus-peers-comparison.json/Windows-focus-comparison.json. source/native창·프로세스0반납.

Mac sourcefocus4는BODYfalse→implicitBoxTabtrue→keyboardrestoretrue→childTab322. Nativepointerrestorefalse/childTab322 및track/held322는일치. NativeTabViewport/keyboardrestore2는before sentinel에머물고Tab이shiftfalse/handledtrue로실제도착한기록. Failed-native-*-before-diagnostic.json으로보존. 제품8e0변경없이probe-only eligibility/contentfocusables/focusmethod로그추가, headless를실제ContentBox/Stripe구조로맞춘abc8/c082에서도새구조focus/child7조건pass. Native플랫폼순서/actor의이벤트문제로나눠원인분리하며추측제품수정안함. Mac source/native모두0cleanup-focus-checkpoint로반납; 다음실패2actual only진단요청.

## abc8 실제 rawTab 진단 / setup 경계
압축 뒤 AGENTS→MASTER→상태/병렬/Headless/worker→활성명세→literal original CSS→owned 제품/driver 실제복구. verify_lock 전체60범위 통과, 보호변경없음. abc8 두실패만 실제CG rawTab/창restore 수행: focusable true/tabStop true/contentFocusables[]; native active Before, shiftfalse/handledtrue. focusEvents의 최초Before method는 Pointer가 아니라 Tab이고 다음restore만Unspecified. 실패JSON failed-native-*-abc8-predecessor-unverified.json 보존. Source key record는 Tab직전Before active를보존하나 native helper의CG predecessor click은activeBefore precondition을검사하지않았음. Probe Button은controltheme가없는diagnostic이므로bounds클릭이focus됐다고추측하지않는다. 다음owned actor만 AXFocused predecessor+freshactiveBefore assertion으로 source/headless/Windows의같은초기focus조건을보장하고 actualrawTab/restore2만확인한다. 제품8e0 변경없음. 소유native/source창closed, exactfullbundle process0/sourceWindow0, cleanup-tab-diagnostic-checkpoint.json 후Macroot반납.
공식 KeyboardNavigationHandler.cs116–121(동일NuGetcommit)는 current focus로next계산하고 Move성공에Handled를설정한다. 따라서 rawTab후Before methodTab만생긴기록은 initialfocusnull→첫Before진입과도일치하므로 viewportfailure/OS차이로면제하지않는다.

## 최종 LOCK · 2026-10-08
제품hash8e0ea4a8f79ff913a1c7891ee532e8d800a24f244b1d4202c6a7d476466cd180. 마지막Mac actualrawTab/restore2는AXFocused predecessor를freshrecord activeBefore로검증한뒤공식CGEvent Tab을보내어 viewportactive/outline true와창restore true완전일치. source/nativefocus4 strict0(기존pointerrestore/childTab증거재사용). 기존실패는진단actor의precursorCGclick이focus를보장하지않았고 actual실행에서initialnull→첫Before로탐색한것으로확정; 제품/navigation변경과OS면제없음. native-predecessor-focus-verified.json의keys[]/Before Unspecified→native-tab-viewport.json의viewportTab 기록. 제품/원본불변. 소유창/프로세스0확인후Mac반납, cleanup-tab-verified-checkpoint.json.

양OS source/nativegeometry72 each strict0, actualkey/wheel13 each, fit/childfocus/lifecycle/drag19 each, Mac track/held322/Windowstrack322-held-left0, finitefocus4 each/Windowsclippeer2 strict0. 실제paint/AX를포함하고headless새focus/clip영향검증기록연결. 원본과OS의overlay/gutter기본렌더·창장식/텍스트래스터경계만허용하며offset/geometry/input/AX누락을면제하지않음. 전역verify_lock 기존60모두통과. 알려진제품차이/미확인항목없음.

공개조립계약: Content(Control), LogicalWidth(실제table logicalwidth), OverflowWidth(descendantpaintedextent), Width(parentclientwidth); Measure는content autoheight+Windows overflowgutter15, Mac0. Parent shell border/radius/background는Table소유. Offset/MaximumOffset/ScrollWidth/ClientWidth/ClientHeight/GutterHeight/ThumbBounds/OffsetChanged 제공. Source Table overflowextent와logicalwidth를같은값으로뭉치지않음. detachoffset0/animationcancel, focusedchildbringcenter/clamp 유지. Table 직접하위 Header/Row/Viewport 모두LOCK이므로 다음상위자동진행.
