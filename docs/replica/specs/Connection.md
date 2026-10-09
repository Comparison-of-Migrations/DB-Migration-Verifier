# Connection

LOCKED · viewport-only exception revalidated · 2026-10-09. Prior LOCK · 2026-10-08. 양OS source/native96 + validation62, 실제입력10 및 추가키/포인터, 독립headless96/62+10 엄격차이0. 페이지계약 완료; App 통합경계는 아래에 남김.

초기 기록: IN PROGRESS · 2026-10-08. AGENTS/MASTER/STATUS/직접 하위 PageHeading/DbmsPicker/ConnectionPanel/AdvancedConnection/Notice/ActionFooter/Motion 계약, 실행 connect()와 input/change/click/render/validateConnections()/footer()를 확인했다. 47 LOCK 유지. 기존 MainWindow.HomeConnection은 원본이 아니다.

## 구조/배치
page→PageHeading(bottom24)→DbmsPicker(bottom24)→split source/target panels→section(top30)→error?Notice(top20)→connected?Noticeok(top20). 폭904/viewport1204 원본 heading70.9921875, picker146.390625 y94.9921875, split490.171875 y265.3828125, advanced expanded166.796875 y785.5546875, root952.3515625. split equal columns, gap20; viewport≤1100 gap16. 카드 compact≤1180와 별개. 모든 source field label/id와 password disabled 유지. 실제 DB 연결이라고 표시하지 않는다.

## 상태 전환 정본
- dbms change: 양쪽port SQL1433/Postgres5432/기타3306, connectedfalse, trial.ready/consentfalse, visited connect만. error/advanced/busy는 이 분기에서 지우지 않는다. 전체 render; id 기반 새radio focus+모드 복원.
- source/target input: 값 갱신, connectedfalse/trialready·consentfalse/visitedreset. DOM 부분 갱신: 양쪽pill 미확인, successnotice 제거, preflight footer 버튼을 connect로 교체, stage비활성. error는 남는다. input·page·advanced 재생성 없음, 입력 포커스 유지. App은 trial/visited/rail 갱신 소유.
- advanced toggle: 전체render, data-action advanced 포커스 복원, 열린 패널 entry 재생. 언어/구조 변경은 새로운 디자인을 만들지 않는다.
- connect click: validateConnections()의 필수host/db/user trim→포트ASCII정수1..65535→host startsWith bad→host/port/db exactsame 순서. 실패errornotice/버튼허용 유지. 성공busytrue/render→550ms→busyfalse/connectedtrue/render→preflightfocus. 입력필드를busy 중 잠그지 않는 원본 상태도 그대로. 이 지연은 시안이며 실제 서버 접속 없음.
- footer: Back/Changes require another connection check.; connected?preflight(primaryarrow,disabledbusy):connect(primarydb 또는busy16spinner,disabledbusy/Checking…). Page 공급, App navigation 소유.

## 검증 계획/경계
보호 자식 조립과 기본/source부터 구현. 한영×viewport2×폭656/904/1060×8분기(default/advanced/connected/busy/required/port/bad/same)를 literal connect/footer로 독립 수집하고 양OS96조건 box/text/값/disabled/footerguard를 비교한다. validateConnections 원본 결과는 별도 예시 matrix로 대조. 실제 입력/engine/advanced포커스/오류회복/busy550/finalfocus/부분갱신/normal-reduced/제거를 확인한다. 자동표본의 상태설정을 실제입력증거로 대신하지 않는다.

앱 전체 stage/trialreset/scroll/항해는 MainContent/MainWindow 통합에서 남는다. 잠긴 패널/카드의 실제 입력·transition 증거는 하위 계약을 사용한다. DB backend/I05는 보류, 네엔진 시안 선택을 실제 네DB지원으로 주장하지 않는다. 모든 probe자료는 example.invalid 예시이고 실제비밀번호/DB값 없다. 전체복제 완료 전 재개제약 제거 금지.

## 현재 단계
LOCK. 검증 완료 목록은 최종 증거 절 참조. 아래 시간순 기록의 이전 미검증 상태는 해당 당시의 기록이다.

## 2026-10-08 worker 복구/첫 slice
실제로 AGENTS/MASTER/COMPONENT_STATUS/PARALLEL_WORK/worker상태와 직접 자식 최종계약·connect/footer/render/validate/input/change 및 후보를 확인했다. verify_lock47 통과. 별도 worker 원본IAB는 기존 사용자탭을 노출하지 않아 새 소유 원본탭으로 Home→Connection만 이동했다. viewport1280/width980의 root829.5546875, heading70.9921875 y0, picker146.390625 y94.9921875, pair490.171875 y265.3828125, closedadvanced44 y785.5546875를 실제 확인했다. fixture literal96(언어2×viewport2×폭3×8상태)+validation62의 DPR2 원본을 connectionpage-mac-source.json에 바로 보존했다. source/sidebar endpoint 재사용은 lease내에서만 수행했다.
Build 성공(경고0/오류0), mac unique connectionpage-mac-check 및 win-x64 package publish 성공. 이 사실은 native 비교 통과가 아니다. 소유 원본/fixture탭은 닫고 root에 lease 반납했다. 다음 Mac96/Windows96와 실제입력. 전체상태 미검증, LOCK 아님.

페이지의 input은 footer preflight 기존버튼만 connect로부분변경하며, busy/default connect버튼은 그대로 유지해야 한다. SetFooter(footer,true)는 이 정책으로 구성하고 기존 ActionFooter callback의 이전 action은 ResolveFooterAction이 현재connected로 해석한다. StateChanged reason=input/dbms는 앱 trialready/consent·visited reset 신호다. finalfocus 및 App항해는 부모 callback으로 공급한다. 실제 입력 harness에서 reference 유지/교체와 focus/시간을 별도 수집할 예정이다.

## 양OS 조립 기하/validation 첫 완료
각OS source96와 actual attached native96의 root/heading/picker/pair/양패널/advanced/notice10입력 box·값·disabled·언어문구·selected·status·footer hint/action/loading/enabled 비교 **차이0**, 각 validation62도차이0. connectionpage-{mac,windows}-comparison.json. mac compactKRadvancedPNG 및 WindowsnativePNG의glyph/edge AA는기본OSrenderer범위로기록하고기하/색/state는면제하지않는다. 보호하위수정없음.

Mac exactoriginal input harness: PostgreSQL본문→양5432/id=dbms-1 pointerfocus복원; Spaceadvanced열림/focus유지; requirednotice와connectfocus; 입력수정은기존error를남기고 page/source/advanced/footerreference 유지. 실제busy550중242.5ms후 hostinput→Checking…disabled/spinner유지(footerreference도유지), 완료→connected/양pill/success/preflightfocus. 연결뒤db입력→양미확인/success제거/기존forwardbuttonconnect로부분변경, inputfocus유지. source/connectionpage-mac-source-input/*.json.

Mac candidate실제engine/advancedSpace+Enter/error는실행했으며, 검사오류후footerfocus가검증용부모callback에서누락된것을확인했다. 제품페이지는footerfocus를부모에위임하는계약이고probecallback을교정한다. required-before-harness-focus.json은실패자료로보존, 통과증거로쓰지않음. geometry/state96에는영향없는probe-only교정. 양OSactualcompletion/errorfocus/reduced/lifecycle는아직남아LOCK하지않는다.

공유검증환경은root요청대로양OS96 stablecheckpoint에서반납한다. 소유Macsource/IAB·Win측정창은닫았으며Mac검증앱 close뒤CUA상태관찰이새인스턴스를열어exact-ownedpid를종료했다. 다음lease에서실제입력을이어간다.

## Mac 실제 focus callback 교정 확인
새probe-only부모callback으로 requirederror뒤새 Footer-connect focus복원실제확인. Macengine/advanced-key/required source/native최종값/문구/footer 비교차이0(connectionpage-mac-input-comparison.json). 소스의normalsettledpage remove/readd에서는pageopacity.35로진입재시작, Booleanreduced에서는1이며입력/DBMS/펼침상태보존. 언어전환은값/advanced와errorstring을그대로보존한다. busy도중badhost입력해도550ms뒤connectedtrue가되는원본정책을직접확인했다(시안의예시연결이며실제접속없음). source/*/language,lifecycle-{normal,reduced},busy-invalid.json.

root의짧은원본획득차례요청에따라stableactualcheckpoint에서lease반납: MacSource탭닫음·소유nativepid종료. Nativeerrorrecovery/busy/finalfocus/connectedinput/reduced/lifecycle와Windowsactual은이어서검증한다. 이분할은미검증을면제하지않으며현재LOCK아님.

## Actual 잔여 검사 도구 준비 · 2026-10-08
압축후규칙/상태/직접자식계약/source/candidate를실제로읽고 verify_lock49 통과. Mac actual3labels는엄격focus비교추가뒤에도차이0다. 나머지는미검증이며LOCK하지않는다. probe-only optional --resume은전차례requiredView를진단용시작상태로복원하여분할검증을잇고, 그seed를실제입력으로주장하지않는다. synchronous mount/unmount/status/success metadata와actualcomparator의busy/partialreferences/focus/lifecycle검사를준비했다. Win UIA 고정sourcebusy/helper 및focusguard를준비했고 Python syntax검사통과; live검사전이다. 제품WebConnectionPage는양OS96최종검증이후불변이다.

최신사용자정책에따라root가OS별 actual자원과독립headlessbuild를설정중이다. headless 성공만으로완료/LOCK을주장하지않고HEADLESS_PASSED로분리한다. 실제창의남은입력/포커스/busy/수명검증은두OS에서이어진다. 현재소유창/프로세스는모두닫혔고환경grant없음.

## 독립 실제 렌더 headless96 · HEADLESS_PASSED
새 HEADLESS_VERIFICATION 운영을 실제로 읽고 독립source snapshot/build/process/output으로 실행했다. build14ce7c6d46144386a88a784c9ab9f78d는0warnings/errors, runb97852081237453b815533e03f0d1ebe의realSkia(UseHeadlessDrawing=false)/DPR2기하96·validation62를Mac원본과엄격비교 **차이0**. connectionpage-headless-comparison.json 및 해당run environment.json/기하JSON/compactKRadvancedPNG. 실제렌더PNG를직접확인했다. HEADLESS_PASSED는새환경의해당범위만이며Windowssupport/actual잔여를대신하지않고Connection은IN PROGRESS다.

유한headless input probe의첫compile7(API필수인수/enumshadow/Bitmap.Save서명)는실패snapshot보존후교정했다. 초기busy검사의currentBusy사후assert는KeyTextInput이입력을Busytrue에서수행한뒤completionjobs까지처리하는API동작때문에falsefailure였다. 기록timeline의input행Busy로검사한다. product550ms지연은수정하지않았다. headless내Ctrl+A/Back/TextInput 순차API가유효시간을넘길수있어busy만publicSelectAll로selection설정뒤pointerfocus/KeyTextInput을쓴다. 다른필드는keypress/release선택·삭제·입력전체를검사한다. Native OS 증거는여전히별도필요하다.

## 독립 유한 headless 입력10 · HEADLESS_PASSED
최종build49e38857012745bfa5d7600aeec74c17/run c0f0229e6123476c855ace2798842e99: source Mac 독립입력10labels와 finalView/값/문구/status/success/footer·Focus·input부분reference·busy중Checking/spinner유지·550이후완료·normalmountopacity.35/reduced1/동일node보존을엄격비교 **차이0**. connectionpage-headless-input-comparison.json. finite10runJSON·environment·실제SkiaWindowframe 보존/직접검토. Busy→connected기록608.46/634.22ms는probe callback/dispatcher처리포함시간이고 제품Task.Delay550을바꾸지않았다. footer/check/text/DBMS/advanced는publicHeadless Mouse/KeyPress/Release/KeyTextInput 경로다. 테스트용bareButton은제품스타일template가없어포인터surface가없으므로language/reduced/mount는진단Action을직접호출했다. 이세항목은page presentation/lifecycle계약이고앱parent actualnavigation을대신하지않는다.

mountprobe의Canvas.Add는페이지를footer위z-order로옮겼다. source DOM복원과같은원래순서를보존하도록Insert(0,page)로교정했다. Native도이검증부모교정을사용한다. ProductWebConnectionPage변경없음. Windowframe은진단용Canvas(footer y900)의입력/상태렌더증거이며제품MainWindow부모배치합격으로쓰지않는다. 실제전체페이지기하는별도양OS96/PNG에있다.

## 실제 Mac 입력10 최종 대조 완료
Mac actual10 source/native **차이0** (connectionpage-mac-input-comparison.json). requiredcheckpoint에서resume한초기View는seed로표시하고새error-recovery부터실제input만증거로썼다. 빈Host CUAsetValue는notsettable로거절돼focus확인뒤실제typeText로회복했다. error/page/source/advanced/footerreference 보존. CGEventguard+AX입력 normal35.10ms/bad22.19ms후Busytrue에서값변경, Checkingdisabled/spinner/참조모두유지. 부모callback포함완료기록617.37/616.69ms, 연결완료후preflightfocus/양pill/success. 연결뒤DB수정은success제거/양미확인/기존footerconnect변경/필드focus·참조를보존했다.

KR/EN값·DBMS·advanced보존 및동일node normalremove-return(.35)/reducedremove-return(1) 실제확인. 테스트용bare진단버튼은Canvas끝으로offscreen이라CUAclick가거절됐다. 고정ConnectionPage-{language,reduced,mount,record}만허용하는별도AXPresshelper(정확bundleURL/title/PID/role/enabled/유일target검사)로pagepresentation/lifecycle 계약을확인했다. OSreduced설정변경없다. 제품Apptopbar의focus/항해는root통합범위다.

최종Reducedbusy후AX트리의focus표시가생략돼fresh전체tree와실제Space를확인했다. Space는Footer-preflight의ActionRequested를발생시켰고requests끝preflight/AXfocusedFooter-preflight가일치했다(completion-key-activation.json). native검증windowframe도직접시각확인했으며진단Canvas부모배치를제품App배치로주장하지않는다. 소유NativePID8508 종료/잔여없음, source탭없음, Macresource root반납확인. Windowsactual10은아직남아LOCK하지않는다.


## 최종 Windows 실제입력 / LOCK 증거
Full cold self-contained win-x64 isolatedbuild a3236e5b86d841d8b8c9412d1a61f5b5는0warnings/errors, archive/binary SHA검증 후 고유guestfolder로전개했다. source원본전체와native fresh actual10 엄격차이0(connectionpage-windows-input-comparison.json). DBMS/advanced 실제Space press/release 및required/errorfocus/input복구/연결후부분갱신/한영/normal.35-reduced1 remove-return을확인했다. source/native Busytrue행에서정상·badhost입력했고 Checking disabled/spinner 및page/source/advanced/footer참조가유지됐다.

Windows 원본 normal입력80.10ms/완료555.90ms, bad입력62.60ms/완료563.60ms; native normal입력222.31ms/완료653.60ms, bad입력247.43ms/완료608.71ms. connectionpage-windows-input-timing.json은실측timer값이며 상태일치에서timing을추론하지않는다. native완료시간은probe callback/dispatcher포함, 제품Task.Delay550은변경없다.

최종Footer-preflight실제Space는requests마지막preflight와해당focus를확인했다. 추가고정SendInput advancedclose→open은정확path/title/PID/geometry/foreground/UIAhit/releasefinally/pointerrestore guard를통과하고state/data/connected/focus를보존했다(pointer-closed/open.json). 첫helpergeometryguard는buttonwidth를fullpagewidth로가정해입력전차단됐다; 실제UIA296×88(DPR2, logical148×44)읽은뒤허용geometry를교정했다. 제품변경없음. WinownedNativeProcesses0/ownedSourceWindows0(cleanup.json)확인후rootresource반납. Mac도cleanup완료.

LOCK 범위는WebConnectionPage 단일페이지조립/값/로컬state/validation/footer공급/부분갱신/entrylifecycle다. App stage/trial/navigation/currentglobal550/scroll 및동일최종프로그램두OS통합은부모검증으로남는다. 양OS96·62와actual10+추가실제키/포인터가완료됐고, 신규headless실행은HEADLESS_PASSED로별도보존했다. 기본OSglyph/edgeAA경계외기하/상태면제없음. 제품SHA f7409d7ba55d95fe063c920c7f4665fc32e732adb1efca0f224705c2202c2674.

## Narrow viewport-only exception · before edit · 2026-10-09
Root assigned Home parentresize scope; Connection agent confirmed no overlapping productedits. Original JS70 windowresize onlyCSS/sidebar scrollIntoView; rootIAB71 actualrender source8 (KOEN1280→1100→960→1280, viewportheight900) preservespage/editor/field/picker reference, focus/value/selection3–8 andnewPageAnimation0. Literalwrapper copiesoriginalbytes unchanged. [Immutable source8](../source/home-pageintegrationconnection-resize-mac-default-root-a.json). Frozen3f90/a8af actualSkia headless8 has42 differences: six changedwidths re-createpage/editor/field/radios, selection0–0,newentry1; allfreshgeometrymatches. [Pre-changecomparison](../source/home-pageintegrationconnection-resize-headless-before-comparison.json).

Reason: rootWorkspace.Measure still Render(false) fornonTrial, protectedConnection.SetPresentation rebuildsChildren/editors/entry; privatePanelPair.Gap constructorinit-only blocksparentin-placebreakpoint update. Proposed public SetViewport(double viewportWidth): equalityguard,_viewport update, ownpair SetGap(viewport<=1100?16:20), publicPicker.Options Compact=(viewport<=1180), Advanced.SetViewport(viewport), InvalidateMeasure. KeepView/Children/Fields/Inputs/Picker/radios/events/revision/_entry/validation/busy/error/connectedstate untouched. ExistingSetPresentation/constructors/measure/arrange expressions unchanged. Rootowns callerFooter.SetViewportWidth+responsivepadding inplace, noRender/revision/focusrestore/newentry.

Influence: Connectionpagepairgap andpickercompact only plusdependentAdvancedexpandedgap. Otherprotectedchildren untouched. DefaultConnection96/validation62/input10/lifecycle/paint reusablebecause existing construction/state handlers unchanged; new parentdefault8/freshgeometry andexpanded4 identity/gap/geometry +actualbothOS required before reLOCK. Advanced privateColumns init-onlygap needs separatelyrecorded sourceproof/resize-onlyAPIexception; rootapproved scopeafter source4 proof. Productstillunchanged at this pre-edit record.

## Connection parent new Mac scope · 2026-10-09
Direct Advanced5e0f reLOCK preceded root Workspace Connection.SetViewport caller (root Measure in-place switch), then own frozena67a64 +Winca4c6 compiled0/0. Product Connection1a816 keeps children/entry/editors and changes existing pairgap/Picker.Options.Compact/Advanced viewport only. New parent allocation+identity scope differs from standalone child4, so root requested full default8+expanded4.

[Mac default8 headless](../source/home-pageintegrationconnection-resize-mac-headless-default-comparison.json) /[expanded4 headless](../source/home-pageintegrationconnection-resize-mac-headless-expanded-comparison.json) strict0; both independent processes exit0. [Actual default8](../source/home-pageintegrationconnection-resize-mac-actual-default-comparison.json) /[actual expanded4](../source/home-pageintegrationconnection-resize-mac-actual-expanded-comparison.json) strict0. External AXSize client1280→1100→960→1280 KOEN default and1280→1100 expanded; only F8 baseline language/expanded setup, no product API logical width bypass. Page/editor/field/picker/toggle/explanation/column retained, focused/value/selection3–8/noentry preserved, literal parent geometry allmatches.

Native PID67184 AXClose immediately after finite12 in finally, own nativeProcess0/window0 verified; source previous rootIAB71/72 alreadyclosed. Mac resource released before writing this record. New actual EN1280 PNG inspected, full viewport fields/page/controls render with no clipping anomaly. Windows full coldruntimeca4c packed, native12/fresh defaultsource8 remain unverified until resource grant. Connection still IN PROGRESS; old96/input10/validation62 paths unchanged and preserved.

## Connection viewport exception reLOCK · 2026-10-09
Current product1a816a5383b6785fb1792163c15172e638f030b0a29e27a5a58a0a6f631b40f9; oldf7409 manifest archived in connectionpage-before-viewport-manifest.json. Advanced5e0f and Typography1def narrow affected proofs completed before parent. Exact diff adds SetViewport and private gap setter only; constructors/SetPresentation/validation/state/entry/events unchanged.

BothOS new parent defaultKOEN8+expandedKOEN4 headless and actual strict0 (960+608 geometry fields perOS). External Mac AXSize and Windows UIA.TransformPattern.Resize through actual root Measure preserve page/editor/fields/radios/advanced references, focus/value/selection3–8 and no new entry at1280→1100→960→1280(default) /1280→1100(expanded). Windows ca4 frozen source default22dd/expanded6ee matches environment/DPR2/viewportheight900; root unrelated later Navigate Error/mappingClosed changes excluded from resize scope by explicit root reuse authorization.

Actual MacPID67184/WindowsPID1476 immediately closed in finally; exact process0/window0, allownsource0 before resource release/docwriting. Windows launch transport first InvalidArgument preserved; fresh launchRecordfalse/process0/window0 proved no owned runtime before one bounded launch. Source close preparation inert-loader issue and corrected guardedSHAclose preserved, no original measurement rerun. Actual EN1280 RenderTarget screenshots opened/inspected; geometry below viewport separately measured, no full-panel screenshot assertion. Old96/validation62/input10/AX/lifecycle/paint reuse justified by exact diff; new APIs validated eachOS12 without broad repeats.

Final [proof matrix](../source/home-pageintegrationconnection-resize-proof-matrix.json) /[manifest](../source/connectionpage-lock.json). API public SetViewport(double viewportWidth); root caller already integrated in-place with footer/padding, noRender/revision/focusrestore. Wholeflow root-owned remains separately active.
