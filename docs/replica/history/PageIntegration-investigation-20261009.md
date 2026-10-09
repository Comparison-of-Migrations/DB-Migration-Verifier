# PageIntegration historical investigation

Archived2026-10-09 without dropping failures/changes/evidence. Past WAITING/unverified statements describe their timestamp only; current state is COMPONENT_STATUS and specs/PageIntegration.md.

# PageIntegration · root · IN PROGRESS

정책은 MASTER_RULES §13. 기존 App → MainWindow 실행 경로 안에 --replica-preview 옵션으로 잠긴 예시 페이지를 부분 연결한다. 별도 fixture를 실제 앱 완료로 표시하지 않는다. 실제 DB 경로와 원본의 disabled-password/sample-only 계약이 다르므로 기존 DB 경로를 보존하고 명시적인 예시 실행만 연결한다. 전체 앱/미완성 페이지는 미검증이다.

압축 복구: MASTER_RULES §5 및 root STATUS, 아래 직접 source/구현/페이지 matrix를 실제 읽는다. 2026-10-08 복구 완료.

## 직접 원본 조사
실행 원본 own tab59 / http://127.0.0.1:8765/. home→start→connect 실제 클릭과 freshDOM 확인. shell1280×720; top60/sidebar220/workspace1060×660. sample strip36, gap8, padding7px40px/bg#292b30/bottom#3d3f46. content overflowauto/padding36px40px, page max1120/margin auto; footer84. <=1180 content padding28, strip horizontal28/footer28. source original.js render/go/footer/initial and click/input/change handler 확인.

## 조립 계약
직접 하위 TopBar/Sidebar/ActionFooter/Connection/Review/Save/PageHeading/Notice/SvgIcon/Typography/Surface/ScrollViewport LOCK. 독립 상태 없는 sample-strip copy/icon 및 page width allocator는 이 부모 범위. 원본 initial SQLServer/dummy endpoints/selected4/tables3/json; visits gating; run 중 rail disabled; input 변경시 연결/후속방문 무효화; lang rerender scroll/focus 유지; go headingfocus+scroll0. Connection busy550ms 실제 원본과 locked child 사용. Review stopped gate, Save formats disabled gate를 자식 API 그대로 사용.

진행 중 Home/Preflight/Selection/Trial/Progress/Results는 연결 대기 표시만 하는 개발 예시 경로. 원본과 일치/흐름 완료를 주장하지 않는다. 각 담당 페이지가 검증 완료되면 root factory에 연결하고 부모 배치/동작/양OS를 추가 확인한다.

## 구현/증거
App.axaml.cs/MainWindow.cs root scoped constructor/ReplicaWorkspace.cs. 별도 예시 경로는 로그인/외부서버/DB키 사용 없음. 원본 sample report schema/1MiB/whitelist/정수 제한 재읽기; 실제 report route와 분리. OS file picker는 실제 OS 확인 전 미검증.

검증 계획: 전체 제품 snapshot build(leaf exclusion 없음), 새로운 연결 상태·동작·양언어·viewport 부모배치 headless 실제render; 실제Mac/Win 창 입력·focus·filedialogs·pageflows. 같은 자식 통과는 반복하지 않는다. 현재 source DOM 조사만 완료; 구현/연결/흐름 미검증.

## 부분 연결 headless · 2026-10-08
전체제품 snapshot45cfaa3a1de0474aab243b5389276bdc, actual App.Initialize/MainWindow ctor 경로. 원본24fullshell와 후보24parent bounding strict0, source-backed 신규상태12(시작/오류/예시연결/footer/부분preflight/back/입력무효화/reviewgate/선택확인/빈formats/언어/fullrestart/연결중언어) 통과. 원본단위 child 검증 재실행없이 새부모·새상태만 비교. sourceRoot59/원본JSON 및 frozenoutputs870871/native/checks/SkiaPNG 확인. 미완성preflight 경로이동은 완성page flow로표시하지않는다. 실제양OS확인/파일dialog/전체flow미검증이므로 LOCK불가.

초기 probe IsVisibleProperty 단계는MainWindow.Opened이전이라client1024×768/하위0을기록했고check실패했다. 원본/제품 수정없이 실제Opened이후 측정으로교정. checkboxKey event 전에페이지Focus queue를flush해실제headless키전달을확인했다. 기존실패보존; 통과주장에사용안함. Connection busy중언어로child교체되면previewasync완료가무시되던부모bug를 root에서corrected(sourceglobalstate resumes connected). 대응headless신규1통과.

최근 새scope: 원본sourceopenFile/report/download parser/JSON·CSV·HTML 및toast CSS/4200ms 부모범위, globalfocusidentity 복원. 실제 DB/다른파일형식에예시parser적용하지않음. sourceReader가받은unknown extras/findings는저장/화면으로에코하지않음. 이추가scope와 실제OS는새검증필요. 동일24 geometry는현재frozen범위로유효하며새코드버전완료에확대하지않는다.

Actual Mac frozen87938c: actualApp/MainWindow fullparent24 strict0 (actual ready headlessfalse/DPR2); sourcegeometry reused Mac24. Actual pointer language→focus language, connectionbusy→completed Footer-preflight, edit→invalidated state, reviewchoice/footer, saveformat/footer, language+Tab, OSopenCancel→Save-open restored. F9/F10/F11 preparations are probe-only and not navigation pass. Found invalidconnection rerender loses Footer-connect focus (actual focus null); preserve invalid-focus-before-correction, fix root dispatch restore source identity without lockedchild changes. Full flow, Windows, filecontent and modal parent remain unverified.

Freshoriginal file-reader26 adversarial/sample cases root-pageintegration-mac-input/oracle: source accepts numeric integer exponent/decimal and null/false/zero matching default; parent sample parser corrected only those observed semantics, rejects unknown/duplicate/table/count/oversize/nullid safely, discards incoming findings/extra. New source-reader comparator/roundtrip/privacy markers pending candidate run. Sourcefixture html does not alter protected original.

Root research/permissions Modal integration newscope: reuse LOCK WebInfoDialog; source openModal blur/inert shell/opener restoration. Parent stateless background layer owns shell; dialog/toast outside. No childvisual/action restyling. Actual parent/focus/language/inert/AX checks remain pending. Probe F12 alters modality, so nativeobserver atomic200ms source-state snapshots added to avoid diagnostic key affecting pointer evidence.

복구 2026-10-08 (현재 압축 후): AGENTS/MASTER/STATUS/PARALLEL/HEADLESS/lease/활성 명세와 직접 original.js startRun/cancel/go/footer, ReplicaWorkspace/ReplicaSampleRun/App/MainWindow/Probe를 실제 재읽었다. 61 LOCK manifests 모두 통과. Cached parent automation peer의 modal 전/중/후 및 sample run140ms/취소/마지막완료tick을 이번 새 범위로 검증한다. 기존24배치·26reader·13상태는 동일 구현 범위 재실행하지 않는다. `run`이 external no-op인 부모 누락을 원본 startRun59/cancel70 근거로 root 범위에서 수정하며 Progress 미완성/흐름 미검증 표시는 유지한다.

Targeted root e766: cached automation peer modal4 passed (10fab output); fresh original6 live-run frame sequences match model6 and real Dispatcher timer/Stopped gate/cancel/partial/restart/zero-total7 passed (b8d output). source140ms clock preserved, 취소된 수치는 검사완료로 판정하지 않음; source complete only following final group tick. Parent pendingProgress is still unverified, no real DB. Actual root timer/modal/newobserver checks queued. Windows passive read replaces diagnosticF12 input; corrected SendInput static construction retained.

저장 형식 새 조사: 자체 pageintegration-exports.html은 보호original.js/css를 그대로 사용하고 downloadFile 인수를 기록한 후 원본 함수를 변경 없이 호출하는 observer만 추가했다. KO/EN 완료/중단4 × JSON/CSV/HTML =12 실제 원본 다운로드 인수 및 합성 escaping9 fresh root-pageintegration-export-mac-source.json 보존. CSV/HTML 문자열과 JSON 의미구조 비교를 수행하며 결과 finding provider 앱 연결은 Results LOCK 이후 별도 확인한다.

export 비교2e4/f50에서 KO완료 JSON/CSV는 통과했으나 HTML은 WebUtility.HtmlEncode가 원본의 가운데점(·)을 &#183;으로 확장하여 문자열이 달랐다. 보호원본 esc(v)의 5문자(& < > " ') 변환을 그대로 사용하도록 root ReplicaSampleExport만 교정한다. 기존 실패 증거 보존, 새로운 출력/escaping 범위를 다시 확인한다.

추가 source-reader 경계: original openFile r.version !== 1은 1.0/1e0 숫자를 동일하게 받는다. C# GetInt32의 표기제약을 source의 숫자 값 동등성으로 교정(root sample model만). 새 2개 표기와 문자열·boolean 거부4를 별도 원본/후보 확인하며 기존26은 범위 그대로 재사용한다.

원본 version4 조사에서 IAB locator는 해당 fixture button을 찾았으나 클릭 NoNode/press timeout으로 실행되지 않았다. UI입력 통과로 표시하지 않는다. 합성 파일을 원본 openFile에 보내는 fixture 테스트 함수를 자동 실행하는 source runner로 이 표기4 비교만 진행한다. 보호 원본/제품 동작은 수정하지 않고 실제 OS 파일 대화상자 검증을 대체하지 않는다.
HTML esc 좁은 교정 후 frozen487/d6e 출력12 + escaping9 =21 모두 통과. JSON 의미구조, CSV/HTML 본문이 원본과 일치. 실제디스크파일 저장/다시열기와 finding provider 연결은 대기.

## Home LOCK 이후 실제 앱 연결
2026-10-08 root가 기존 Progress 새부모18/track48 headless 체크포인트를 보존하고 이 활성 통합 범위로 전환했다. Home e91e/f59 manifest/보호63 검사를 확인했다. fresh 원본 tab59 /8765 home DOM과 original home()/render/go/action을 다시 확인했다. 원본 initial home의 footerhint, start/open/sample/research4 동작·go headingfocus/scroll0·language focusidentity/scroll 유지 및 .page 200ms 입장은 이 앱 부모 소유다.

변경 전 범위: ReplicaWorkspace에 보호 WebHomePage를 조립하고 기존 DispatchAsync/ExternalActionRequested/ShowInfo 경로에 연결한다. Home 자식 파일은 변경하지 않는다. Home page-in은 부모에만 적용하고 취소/교체를 정리한다. pageheading focus switch에 Home을 추가한다. 추가검증은 새 Home shell 부모배치8/KOEN/4폭, rawinput 요청→route/modal/복원/언어/scroll/open/sample 및 실제양OS App 창에 집중한다. 미완성 Results 경로 진입은 Results 완성/전체flow로 표시하지 않는다.

Home 최초 통합3f5d/d5b: shell 부모8 strict0. headless go-heading-focus 실패에서 Width1440→1280 측정이 Render(false)를 호출해 앞서 Navigate의 titlefocus 예약revision을 취소함을 확인했다. 원본 resize는 CSS만 바꾸고 go focus를 취소하지 않는다. 변경 전 범위: 앱 부모의 대기 중 navigation focus 의도를 다음 resize render에 전달하고 최신 callback에서만 해제한다. 보호페이지/Heading을 수정하지 않는다. 이 resize+go case 및 기존 연결/review/save focus 영향 상태만 추가 확인한다.

Home 연결 추가 증거: frozen840b/52b9 headless 신규route/modal/language/open/sample/neutral15 및 page-in80ms(.9461454580)/교체dispose 통과. 최초 research 실패는 offscreen rawinput probe로 BringIntoView 후 교정했고 제품 재설계 없음. actual Mac840b/4a8f App 부모8 strict0, 실제 Start→Connect/back→Home/titlefocus, scroll214/research inert/close opener, 언어/scroll복원, OSopenFile sheet>0→Escape sheet0/opener, sample→Results Complete 통과. Results page pending이므로 전체Resultsflow 완료가 아님. PID54970/ownedwindow exactcleanup0. Windows同범위/실제sourceflow pending. 2026-10-08 복구 때 규칙/상태/명세/direct original home/go 및 현재 Workspace 실제 확인 완료.

현재 PageIntegration 안정 체크포인트에서 WAITING: CapabilityTable 부모의 보호ActionButton Windows Text중복을 root 좁은 예외로 검증한 뒤 재개. 기존Home/geometry/input는 불변 조건 재사용.

Win66d3/bfce 새Home headless15 route/modal/lang/open/sample/neutral/entry/replacement 통과. source8 actualWindow/파일dialogflow 대기, oldHomeMac8/15 재사용. 보호페이지 파일불변, 부모pendingnavigationfocus수정 영향 actualWin까지 확인 필요.

Windows actualobserver 파일공유 예방 교정: 다른담당 Results에서 확인된 File.Move vs Copy-Item reader DeleteShare 부재를 원인근거로 root observer ReadShared(FileShare.ReadWrite|Delete)→fresh text snapshot으로 바꿨다. 제품 코드/원본/기존통과조건 불변, 같은Windows원자적 snapshot쓰기 시 actor관찰이 앱을종료시키지 않도록 실제새scope에서 확인한다.

## Results parent and original live announcement · 2026-10-09
Direct original render/go/makeRun/openModal/closeModal reread, LOCK66 check before root edits. Root assembles LOCK Results, owns persisted filter/whole-page replacement+newfilterfocus, go-only entrance and H1/scroll/announcement, safe unfiltered lowercase6field export provider, original categorydetail payload and modal-only parentpeer. Language-recreated modal uses neutral BODYcaller as measured; protected dialog detach restores background before recreation. First parent headless failure Motion needs attachedcontrol retained; only root attachment callback deferred Loaded/currentpage guard, no child change. Global original1×1 off-screen polite status has public Announcement/AnnouncementRegion and named leaf peer; new value/visibility assertions and actualAX remain newscope, not impliedpass. Root shared source fixture hidden-status omission corrected to actualoriginal sr/status/polite before newwholeApp source allocation checks; oldscope evidence preserved.

## Original document overflow integration · evidence before correction
Fresh Windows source6 `artifacts/replica/workers/root/pageintegration/root-viewport-source/c58373ceb8e84bc394cae8d3d8608f03/source.json` DPR2 confirms absolute1×1 announcer static y900→document901. Width960: client945×885, body/shell960×900, scroll960×901; width1179/1180/1181/1280/1440 client1164/1165/1166/1265/1425×900. CSS min-width960/max(720,100dvh) and media1180 use actual innerWidth (1181 is wide despite client1166). Source/native Trial geometry132 gap is missing document parent, not a typeface OS waiver.
Root change scope: retain full-viewport Workspace/overlay/modal, scroll only background shell via existing LOCK WebScrollViewport and LOCK WebCapabilityViewport. One parent-only extent wrapper reports shell height+1 and min-width960, no new independently reusable leaf, no protected child edit. Horizontal chrome is composed only when minwidth overflows, root vertical/horizontal offsets translate background only; shell media remains innerWidth, modal remains full viewport as original top layer. Mac existing shell unchanged (overlay expression boundary), unchanged passes retained. New Windows root6/Trialparent24/Resultsparent4, gutters and documentoffset boundary require explicit comparisons/actual input; whole app remains unverified. §13 applies to partial connections of already LOCK pages, unfinished pages remain gap markers.

## Progress LOCK page 연결 · 변경 전 계약
Standalone ProgressPage LOCK 후 원본 startRun/updateProgress/running/render/go를 재읽음. 진행 중 tick은 동일 page.Update, complete는 원본처럼 render(false)+cancel identity복원→show-results 및 완료 공지, cancel은 render(false)+중단 공지. 언어/폭 변경은 현재 Run으로 페이지 재생성한다. 기존 140ms timer/기존 model6+clock7 증거 재사용; 새 조립은 실제 앱 run→samepage live→lang→cancel→Results→restart→complete 및 양OS footer/H1/announcement/scroll/input 추가 검증한다. 보호 child 변경하지 않는다. Sample만 지원하며 실제 DB 성공 표시 안 함.

Document-only first headlessda4 observed 960 client945x900 instead of885: own hidden horizontal control wasn't measured. Parent now sets visible beforemeasure and extent heightinvalidate, LOCK child unchanged. e0 shortloader run transport InvalidArgument preserved; readonly unique runfolder/zipfalse/process0 confirms notstarted, noGUI. Progress integration31fec first headless initialheading assertion failure preserved; announcement correctlyrunning title; inspect managed focus target before product correction.

Progress connection headless10 passed ebeef/76b235 strict0; first31fec failed observer checking TitleText instead of lockedHeading.FocusTarget, onlyobservercorrected/no productfocuschange. Samepage actualcounts/status at325ms, ENrunidentity, cancel→partialResults→restart, complete8→show-results focused/KOannouncement. ProcessExit0/windowclosed byprobe. Actual bothOS integration remainsrequired. Windows Document root6 e0 first4 client/extent/shell/media pass at960; horizontal tab eligibility reset from LOCK child's LayoutUpdated was observed. Ownparent policy now suppresses newdocumentchrome tabstop via propertychanged, child unmodified.

Windows Document parent headless36/6 passed4f7b/d7bf60 (realSkia,DPR2,actualsource root6 retained). Checks:945x885 at960/doc960x901,960minshell; widths1179/1180/1181/1280/1440 classic15gutter; actualinnerWidth media1181false despite1166body; background x/y rootoffset; noaddedTabstop. RenderPNG960/1181 inspected. Processexit0; noGUI. Actual root scrollbar input/resize and wholeApp modal/focus remainrequired. Trial affected24 passed home364/e98dfd strict0; Windows originalmetric peer/glyphrenderer boundaries unchanged.
Fresh Progress integrated source6 KOENpending/interrupted/complete recorded hiddenownedIAB61 and immediatelyclosed; exact originalCSS/JS preserved,6 shapes/footers/announcements/data. OuterRecordbutton blurs iframe sourcefocus (empty), so that observational focus field is not a navigationfocus proof. Native/headless focus checks are separate.

Connected Progress Mac parent6/60 strict0:6ca/1ca0fd plus actions10 ebeef retained. Windows actions10 actualWindowsheadless4f7b/462028 strict0, processExit0; protected child proofs reused. Actual fullflow remainsrequired. Newroot active actualfile scope: invoke real sample SaveFilePicker/OpenFilePicker inside existingMainWindow App, ownUUID folder, exactcontent comparison against readonly full/unfiltered provider, invalidJSON doesnotexposecontent, cancel/error focus and savedstate; no actualDB or userfile input. Source callback/export12/escaping9 previouslypassed reused; OSdialog actual steps new. Guard title/path/PID/freshsheet, finish→close0.

Mac actual sample-file scope 09bdd8/2be673 completed: JSON/CSV/HTML StorageProvider native saved, actual bytes JSON semantics and CSV/HTML exact original source ko-complete with run ID substitution strict4/0; JSON reopen restores UI-MUZVKIW3 counts3/2/45, no rerun. EN OpenPicker cancel restores Save-open focus and unchanged Save/run; synthetic invalidJSON toast matches direct original.js line66 (catch toast, not alert), contents not echoed and previous result preserved. Source/read/export passed evidence reused, files are synthetic only. Fresh AX close201 immediately ended owned app PID36789, cleanup.json process0; Mac lease released. Windows file dialogs and full navigation remain unverified. Recovery AGENTS/MASTER/STATUS/PARALLEL/HEADLESS/lease/spec/direct openFile/ReplicaActions actually reread; protected67 unchanged.

Preflight source capabilities()/go/footer reviewed and protected68 guardpassed. Manifest0a7b0e21 bothOS actual and correction proofs checked; root now connects WebPreflightPage.SetPresentation/SetFooter/ActionRequested permissions and H1 navigation to existing Workspace (8pages). Child untouched; new parent callbacks/focus/geometry/actualOS/fullflow pending. Selection is still incomplete; no wholeapp pass claim.

## 복구 및 원본 전체 handler oracle · 2026-10-09
AGENTS→MASTER/STATUS/PARALLEL/HEADLESS/lease→현재 명세→직접original render/go/click·Workspace/probe를 실제 재읽었다. 보호68 모두 unchanged. 원본 전체 프로그램 focus/click KO완료/EN중단52상태 `source/root-appflow-mac-source.json` 확보, 실제OS입력으로 승격하지 않음. 첫 observer는 modal닫힘160ms를35ms로 판정하여 다음dialog가 없어지는 실패; fixture의 modal null대기만 교정, 원본/제품불변. ownIAB63 완료 후즉시close/listabsent. ownfailedtab62는 잘못된 loopback bridgeURL에서 연결실패한 오류페이지로, APIget URL-policy 차단; 사용자창을 건드리지 않고 실제Maclease후 exact native UI close pending. 새Preflight 연결8페이지이며 parent/actualfullflow 미검증.

Preflight connected新scope frozena7f208 / output2f155159: originalfullshell8 KOEN×960/1180/1280/1600, 六parentboxes+titlefocus/announcement/scroll/footer와 새permission모달/restore/뒤로연결8 = **HEADLESS_PASSED72/0**, renderedSkiaDPR2 exit0. 原본ownIAB65 완료즉시close/listabsent. 기존standalone16 재실행없음; 실제Win/Macwholeflow pending. ownerror-tab62 nativeCodex cleanup는 CUA 'app not allowed safety'로거절, 우회기술사용안함; nativeGUI0/원본user탭보존. 이오류탭종료 미확인은 actualcomponent완료로 주장하지 않음.

Windowsfile native helper는 connection actualscope에서 진단된 닫힌 unrelatedUIAelement/stale read 근거로 root단계에서 globalTrueCondition→fresh exactPID PropertyCondition을 변경했다. 앱제품/보호파일변경없음, 실제실행전prepareguard한정; unique title/path/creation/FileDialogguards 유지. stderr/ready/cleanup actual은 실행시 별도확인하며 아직통과아님.

Mac native新Preflight parent auto72/0: a7f208/actual-preflight-macos/b4f01157, DBMV_HEADLESS=0 actual App/MainWindow; scope含新parentlayout/callback/managedfocus only, physicalinput/OSAX/fullflow는아직미검증. ownprocessExit0/cleanup.json即시종료. sharedSelection probe --selectionpage/--selectionpage-window +isolatedentry root등록완료, child INPROGRESS라appbinder는LOCK후에만연결한다.

복구 2026-10-09: MASTER/AGENTS/STATUS/PARALLEL/HEADLESS/lease/활성명세와 직접original capabilities/footer/render/click, 현재Workspace/Preflight probe를 실제확인. 보호68 unchanged. §14는 이미 정본에 있으며 중복규칙 추가하지 않음; 완료/진단scope는 즉시 ownedclose0 후 환경반납한다. Windows connection Results 입력 전COM진단은 미검증 보존, exactPID2892/window0 증거 후 Home 잔여scope에 차례배정.

Windows Preflight 새부모 source8 helper 준비: 원본 CSS/JS 그대로인 root-app-preflight recorder에 nonce/자동기록 observer만 추가, exact project title/path 단일 Edge guard와 finally 즉시close0. source Mac8/72 재사용하며 Win폰트/gutter는 freshWin8로 비교한다. 아직 실제실행 미검증. COMPONENT_TREE의 이미 LOCK된CapabilityTable/FindingArticle 및 Selection 부모범위 의존만 갱신, 제품구조/보호child 변경 없음.

## Windows 원본/후보 실제 차이 교정 전 기록 · 2026-10-09
connection actual native30 strict495 중489통과, 6차이는 KOEN Data/Trial/Research DialogBox.x 330 vs원본322.5. 실제원본 inner1280/client1265 및 document15gutter와 대응. parent arrange에서 실제client폭을 사용해 기존 Info/ResultDetails protectedsurface의 중앙배치를 교정한다. CSS vw/dvh 기반 크기 측정은 유지하며 width>652인현재actual 조건은 surface크기불변; 작은viewport/modal-heightcap은 별도 추가확인 대상이다. 제품 자식/LOCK파일은 수정하지 않는다.
실제Windows KOEN markerpeer strict26각각 8차이는 namedText child0 vs 원본 unnamedStatusBar→namedText child1의 구조. Windows 부모announcer의 peer만 StatusBar + textchild로 교정하며 Mac namedText leaf 계약은 유지한다. 실제텍스트변경의通知/Control/Content/focus/value와 bounds 표현은 추가실제검증; sourceUIA의 unsupportedARIA/live 속성을 임의보완하지 않는다. 기존native30 callback489/parent4/source30 재실행없음, 영향받는modal6/peerKOEN 및 작은viewport 신규상태만추가확인한다. 모든scope 끝나면 §14 즉시close0.
Windows Preflight 새부모 headless72/0 dbf77/d1553a12 realSkiaDPR2/process0, freshWindows source8 350d3a9e sourceWindow0. child기존증거재사용. actualnativeauto72 scope 실행중이며physicalwholeflow는미검증.

Windows Preflight ACTUAL_NATIVE_PARENT72/0 dbf77/409bbc33 freshsource8, actual App/MainWindow DBMV_HEADLESS=0 processExit0/cleanup0 완료. 이는 native 배치·콜백·관리포커스 범위이며 physical/AX/fullflow는 별도. 이후 Windowsoverlay/announcer parent교정은 Results 직접영향만 추가검증하고 Preflight child/비모달 배치 동일증거재사용한다.

Windows새parent 좁은headless1acd strict52 중modal6/announcement/type/name/상태 모두 일치, KOENpeer각각 한 개 nestedText wrapper 때문에4차이. 실제보호Typography Windows 내부 TextBlock의 공개 Child peer를 parent가 직접 투영하는 교정만 수행하며 보호파일 변경없음. 통과modal24조건은재사용, 바뀐peer2와 실제modal6/peer2만 추가확인한다.

Windows파일scope 관찰기 한정: exactMainPID2364/path/creation 확인 후 Win32 GW_ENABLEDPOPUP3933220의 owner8455318=MainHWND、동일PID、visible、title예시결과저장 확인. UIA.Root Children PID필터는 popup누락하지만 FromHandle는 동일PID의 FileNameControlHost→1001와 nativeSave1/Cancel2를 드러내며 모두 Pane/pattern0. OS기본provider표현을제품UIA구현오류로오인하지않는다. 사용자 허용 SendInput으로 owned nativeEdit/Save/Cancel만실제입력 검증; freshowner/PID/path/title/nativeHWnd/semanticparent/visible/enabled/physicalDPI/cursor/exacttargethit/foreground guard 및 finallyrelease 유지. 검사자료는 고유TEMP합성파일만이며 실제유저파일선택/덮어쓰기 없음. 아직저장/재열기 actual통과아님.

복구 2026-10-09: AGENTS/MASTER/COMPONENT_STATUS/PARALLEL/HEADLESS/lease/root STATUS/본 명세, direct original openFile/download/render 및 현재 Workspace/file helper 실제 재읽음. 보호68 unchanged. Windows filename 실제입력 후 GetWindowText 다른프로세스 Edit 관찰 false-negative를 fresh UIA ValuePattern 동일 own TEMP path로 진단; 입력 재실행 없음. helper 검증읽기만 UIA로 교정하고 accept fresh ownpath gate를 유지. JSON 첫파일 저장 확인, 나머지 실제dialog scope 진행 중. 완료 후 즉시 exact owned close0.

변경 전 root Trial resize 호출 범위: actual source8의 동일 page/editor/selection3–8/noentry와 strict30차이 근거 및 TrialPage 좁은 API 예외를 확인했다. Workspace Measure 폭변화에서 Trial에 한해 현재page.SetViewport(width), Footer.SetViewportWidth(width), 기존 responsive Viewport.Padding만 갱신한다. Render(false)/page교체/revision증가/entry dispose나 재시작/focus복원 callback은 실행하지 않는다. 다른 페이지 resize는 기존 경로이며 아직 별도 미검증; 전체 resize 완료로 표시하지 않는다. 신규 KOEN8와 1100배치/실제양OS 영향 검증 필요.

변경 전 Connection stopped 부모 상태: fresh original endpoint/dbms2 및 frozen2586 후보2 strict mismatch2(home-pageintegrationtrial-stopped-headless-comparison.json)는 기존 Review confirmed Stopped를 원본이 유지하고 후보만false로 지움을 증명했다. root Connection.StateChanged input/dbms 분기의 Stopped=false 한 대입만 제거한다. Trial ready/consent, visited, Connected, Sidebar/Footer는 기존원본처럼 초기화하며 다른 stopped policy(선택범위 변경/start/restart)는 유지. 새 동일2 comparison/실제전체flow에서 확인, 기존child 변경없음.

Windows sample파일 actual1c5eff 완료: native3 저장/JSONreopen UI-MUZY4ZTD/no rerun/ENcancel/invalid정상toast·내용비노출·동일run보존 flow6/0, JSON semantic/CSV+HTML exact originalbytes/BOM4/0. 완료직후 exactWindowPattern.Close PID2364 process0, guest-output/d/cleanup.json. root-pageintegration-files-windows-comparison.json 연결. 취소 PrlJob Invalid argument는 session전 실패/동일popupstillopen 상태관찰후 잔여1동작만 수행. filedialogshapeID Save1001/Open1148 실측guard. 기존Mac파일 strict4/0 재사용; 전체앱navigation/DB검증 아님. Windows 자원 shortSelectionsource에반납.

변경 전 Windows announcement nativeprovider: c7 KOmodal3 geometry24 retained, actual marker GetFirstChild E_FAIL 후 exactPID8520 즉시close0. HEADLESS shape27 통과는 actualFragment tree 보장이 아님. 공식 Avalonia12.1.3 ControlAutomationPeer.EnsureConnected는 visualancestor wrapper.GetChildren로 child parent를wrapper에연결하며, parent가wrapper.Childpeer를직접열거하면 UIA fragmentparent/sibling 관계가불일치할 수 있음(실제cause는추가검증). Root는 Windows marker의 직접자식으로 native TextBlock 하나를두어 실제visual/automation parent를일치시킨다. 보호Typography파일/사용페이지 변경없음. 이1×1offscreen장식텍스트는 독립재사용component로분리하지않고 marker부모범위로유지. 기존Mac leaf/text계약 유지, Windows StatusBar→namedText/name update/control/content/실제GetFirstChild 새headless2+actualKOEN2 확인 필요. 이미통과modal3는반복없음.

작은 viewport 새 범위: 변경하지 않은 원본 CSS/JS의 KOEN960×720/1180×720 4조건 ×Data/Trial/Research/Permissions16모달. 최초 observer240ms에 modal-in의 y변환이 남아 firstsource/fixture를 보존하고 own68즉시close0했다. 유한 원본 animations.finished를 기다리는 관찰만 교정한 nonceb source4는 안정값이며 own69즉시close0. root ace831/4d23aa 신규modal/body128수치 HEADLESS_PASSED 차이0/processExit0, 기존child증거 재실행없음. Windows 원본폰트/gutter source 및 실제Mac native는 미검증; source/root-modal-bounds-mac-comparison.json. 현재 parentoverlay는 새Mac원본과 차이없으며 이를Windows검증으로 승격하지 않는다.

Windows 작은modal source4 exactEdge title/path/freshnonce 자동측정·finally WindowPattern.Close/source0 helper root_modal_bounds_source.py 준비. source는유한animation완료후 측정, handler실제입력으로승격하지않는다. Windows lease없으므로실행전대기; same fullruntime root modalprobe128만추가검증한다.

Windows directannouncement 제거wrapper 영향 재확인: protectedTypography의 실제 Windows TextBlock은 SystemFontFamily 및 WrapWithOverflow를 사용한다. root directTextBlock은 이값을명시하여 originalbody14px/1.6 normalwrap을 유지한다. peer role/name/children구조는기존directTextBlock27조건과동일해재사용하고 새actualKOEN2에서 익명browserText vs nativeControl leafbbox 표현을관찰한다. Source1×1 sr/clip와 candidate1×1clip 불변, 실제provider차이 증거없이 OS면제하지않는다.

복구 2026-10-09: AGENTS/MASTER/상태/소유권/Headless/lease/root STATUS와 현재 명세, direct original render/go/input/change/resize 및 Workspace/modal-bounds probe를 실제 다시 읽었다. Trial 좁은 예외61e…만 보호검사 차이이며 기존 기록 유지. Mac 작은modal128 actual/close0 증거 재사용, 새Windows source4와128 영향 범위부터 재개한다. §14는 정본 한 곳에 유지한다.

Windows 작은modal freshsource4 nonce0bc1…/sourceWindow0, root698 headless128/0 (4f0d…)와 actualnative128/0 (70d0…) 완료. 측정 종료 즉시 process0 확인 후 home 자원반납. Mac 같은 신규128 actual proof 재사용. 이 범위는 모달 배치이며 실제입력/전체flow와 구분한다.

변경 전 Windows announcer text layout: 실제 direct TextBlock UIA traversal/semantic44×KOEN 및 ENmodal3 통과/closePID4256=0이나 source 숨긴 inline EN3line와 ordinary TextBlock EN7line overflow 차이 발견. protected WindowsWebTextBlock은 CSS overflow wrapping+baseline을 이미 구현하며 WebSystemTextBlock의 실제 내부 TextBlock이다. root marker 직접 child에 이 LOCKED 구현을 그대로 재사용하여 wrapper 없이 visual/provider 부모 일치와 CSS wrap 모두 유지한다. protected class/manifest 변경 없음. 기존 peer shape/name/control/content 및 모달 배치 증거는 영향불변 범위 재사용; 새 KOEN 숨김텍스트 line geometry/actual provider traversal만 추가확인하며 ink vscontrolbbox 표현을 실제 근거로 분리한다.

다음 전체flow oracle 비교 도구 tools/ReplicaProbe/compare_appflow.py 준비: 직접 원본52상태의 모든 키/순서/수치/포커스/공지/footer를 엄격 비교하고 실제시각 기반 생성run ID만 형식검증 후 대응한다. 아직 candidate52실행/통과 아님. 실제OS부분중단의 tick시점은 별도 실제 입력 증거와 구분하고 이 comparator에 임의수치허용을 넣지 않는다.

Selection 부모 state 조립 준비(아직 페이지 연결 아님): original initial/입력/expand/restart는 ordered expanded와tableQuery를 앱 전역에 보관하고 restart는query만지우며 start는둘을초기화한다. root는 이 두상태 저장과 공개읽기만 먼저 추가한다. LOCK 전Selection factory/Matching binder를 실행하지 않으며 새 source52 observer에서 실제state를읽도록 준비한다.

복구 2026-10-09: AGENTS/MASTER/COMPONENT_STATUS/PARALLEL/HEADLESS/lease/root STATUS/현재명세와 직접 original render/go/input/change 및 Workspace/FlowSnapshot/원본52 fixture를 실제 읽었다. Selection 연결은 여전히 LOCK 대기이며 Advanced→Connection 좁은 예외는 home 소유로 유지한다. root는 새 전체flow observer/handler 준비만 이어 진행하고 완료창은 §14대로 즉시 종료한다.

전체flow 새관찰기 준비: PageIntegrationFlowProbe/FlowSnapshot은 원본52순서와 실제 버튼/event·headless 키입력으로 비교하는 handler범위 및 별도의 actualOS readonly immutable snapshot을 분리한다. Selection factory는 아직미연결이며 LOCK 게이트를 우회하지 않는다. root68d98 준비빌드0warnings/errors, 실제flow 실행미검증. 새source ownIAB73(Advancedtext)/74(Parentresize48)/75(Home4관찰시점수정)는 POST 저장 후즉시close/listabsent; 완료원본 창 보존없음. 공유FlowProbe TranslatePoint extension using 누락 compile실패는 관찰기수정으로한정하며 제품변경 없음.

변경 전 여섯 연결페이지 CSSresize: 새 source48의동일page/heading/focus와원본animations0, frozen03dd/231a strict2595 중318제품차이(36 changedwidth의교체/포커스손실/entry) 확인. 관찰기 BackButton 누락32는 별도관찰수정으로기록. root는 Home/Preflight의기존 in-place SetPresentation, Results의 public Metrics.ViewportWidth 및 Chart.Rows.Compact, Progress의재측정만사용하여 page/입력/entry를보존한다. Review/Save는 protected Summary.SetPresentation에 original review()/save()와동일한현재 View의문구·Strong/Mono/Suffix 값만전달하여170→130 breakpoint를반영한다. 무상태텍스트는부모범위이며독립새component나LOCK파일수정없음. 자체cachedviewport를쓰는기존choice/format callback 이후에도동일 summary parentupdate를호출한다. Footer publicviewport/padding만갱신, Render(false)/revision/포커스복원·신규entry없음. Connection은Advanced하위reLOCK전까지기존경로유지. 추가sourcegeometry/state48/headless및 actual양OS외부resize 필요.

Summary resize 실제범위: 기존 Summary.SetPresentation은 stateless label/value row 내부를 다시 만든다. 원본 CSS는 textDOM도유지하므로 내부row객체 identity까지같다고 주장하지 않는다. MASTER§13에 따라 별도state·동작·입력·재사용필요없는텍스트는부모범위이며 현재source와동일문구/flags/semanticstructure/geometry를대조한다. focusedStopped/Formats/page/Heading 및독립 LOCK component instances는유지한다. 기존 source및protected API의구조나디자인을바꾸지않았다. 추가callback rebuild 뒤current breakpoint도갱신하여oldviewport의cachedcolumn으로되돌아가지않도록한다.

현재 새범위결과: six parent48 Mac headless2595/0 + 실제외부AXSize48 strict2595/0, e923/actual-parent-resize-macos exactPID49095 close/window0/process0. Windows same48 원본/실제는차례대기. 이scope는첫 summary label/valuebox, 전체 static paragraphline/textsemantic은별도관찰이필요하며같다고추정하지않음. Selection Windows final12396 native입력/Controlleaf/focus/lifecycle/paint통과후immediateclose0, preflight최종LOCK기록대기. rootactualFlow 아직실행안함. Typography slash source23 ownIAB76 Recorded23 즉시close/listabsent; 기본font/텍스트를바꾸지않고headless 차이부터조사한다.

Selection 基準LOCK8275/a836/331b actual양OS증거·의존hash를 읽기확인했다. 원본 change handler는 category/key·bulk·clear-tables마다서로다른공지며 query/개별table/mapping은공지없음; ViewChanged listdiff만으로bulk와deselect를추측하면안된다. root는preflight에 readonlyLastChangeReason metadata만추가하는변경전예외/신규event 검증·재LOCK를배정했고 factory 실행은새LOCK후에만진행한다. 원래변경값/event/paint/input구조불변. 사용자원본Tab1/51을닫지않으며component검증완료프로세스만종료한다.

復구 2026-10-09: AGENTS/MASTER/상태/parallel/headless/lease/root상태와 본명세/Typography 관련기록, 직접 original connect/select/footer/change 및 Workspace resize/SlashProbe 실제재읽음. 기존 Advanced/Connection 기록된 viewport예외만 guard실패; Typography 아직변경없음. 완료창즉시종료 §14 유지. Windows home11712 종료확인후 connection ParentResize48로차례전환. 현재 Mac slash23 원본과 frozen a6c1/04a9 후보의 새차이를 진단한다.

변경 전 Connection caller: Advanced5e0f 신규양OSactual4/reLOCK과Typography1def 한정 교정증거확인. 원본같은widthresize에서 sameConnectionpage/fields/selection3–8/noentry를유지하고 childgap만변경하는계약이다. root Workspace Measure switch에 WebConnectionPage.SetViewport(width)만추가한다. 보호child/형제페이지변경없음. home freshdefault8+expandedparent4 publicdelegation을검증하며 기존 standalone32/96 반복없음. 전체앱은미검증유지.

Selection45/5c/331 metadata 재LOCK/양OS새scope 확인후 root binder시작. literal select()/change/bulk/expand/updateScopeSelection/closeModal/mapping-apply 원본과parent-binding-contract를기준으로 orderedstate 복사, readonlyLastChangeReason 공지, 보호Matching modal 조립, samepage query/expand/checkbox 업데이트, Applied freshMaps+render, Closed currentopener fallback, langmodal 재생성, resizeinplace를구현한다. 보호Selection/Matching 파일불변. 새52 handler strictcompare/실제양OSflow·부모배치 확인전 IN PROGRESS.

첫Flow52 fa843/681dca handler2988checks에서4차이보존: 초기언어별sourceinitial visited[]에비해probe Dispatchstart 후home visitedconnect(2)는fixture setup차이, 실제constructor새인스턴스로교정한다. mappingApplied Renderfalse 후닫힘에서oldopenerdetached/newRows아직배치전이라BODY남음(2)은제품parent결함; 새page loaded/layout후존재하는동일sourceRow.Focus만복원한다. sourcecloseModal preventScroll/opener연결근거를따르며보호dialog/row변경없음. 추가원본go() state.error초기화지적도rootNavigation sameglobalerrorclear로교정하고새invalid→back→start scope에서확인.

Fixed39fb/b0c6 Flow52 HANDLER_FLOW_PASSED2988/0: KO완료+EN중단원본모든state/순서/focus/footer/announcement/run/count exact, generatedvalidatedrunID만정규화. 처음fa843의4차이는fixturefreshinstance와mappingcurrentRowsLoaded복원으로교정. Windowheadless 자동종료/exit0; actualMac현재39fb native69399 fullflow외부입력 진행, Windows/새Selectioncallbacks/남은parent2geometry는미검증. 새actualscope는live140ms timer중간counts를oracle35ms에강제로맞추지않고 milestone/연속동작과별도clock기존증거를사용하며조건을기록한다.

2026-10-09 actual recovery: AGENTS/MASTER/current status/HEADLESS/lease/current PageIntegration and Selection callback spec, direct original go()/selection handlers, current Workspace and native helpers read. Native69399 finished partial12 states then failed scroll; cleanup exact process0/window0 preserved. Cause: CGEvent wheel created before mouse move retained previous event location; old passed ScrollViewport actor explicitly sets wheel.location. Helper only corrected wheel.location plus delivery observation; protected/product code unchanged. New session output preserves failed scope; new physical run required for failed whole-flow scope. Root source callbacks50 IAB78 recorded and immediately closed/listabsent; shared --selection-callbacks-only entry registered.

Physical39fb/wheelLocation2 passed20 milestones including mapped→Trialready; native75071 closed/process0. Scroll helper correction proven offset changes and visible targets. Next failure is duplicate original action trial-back on both footer buttons; native AX ID also duplicate by source action, so actor must select exact source primary label, no product ID redesign. New unique output session for final physical completion, prior evidence retained.

ActualMac39fb KO29/EN29 physical milestones completed at1280x900/DPR2/normalmotion, source preview only. Mapping focus/current state, separatedTrialprepare/consent, Review/runtimer complete vs cancel, Results→Save formats→restart verified; finally75758/76325 exactclose/process0. Evidence root-appflow-mac-physical.json; handler52 strict2988/0 separate retained. Windows Connection12 PID1476 close0 reported home, nextprecision source5 leaseconnection. Mac available preflight freshCallbacks actual, no completed root native window retained. Overall app/remaining newparents unverified.

Recovery2026-10-09: actually read AGENTS/MASTER/current status/parallel/headless/lease/rootcheckpoint, active InlineText/ScrollViewport/Selection spec, direct original review/save/go/startRun140ms and current Workspace/FlowObserver/leafpeers. InlineText actual4 source/currentName/oneAXtext proof received/cleanup0 → narrow reLOCK. Scroll fixedpoint and Selection peer exceptions remain pending; no original redesign/repeated passed geometry. §14 kept canonical once.
