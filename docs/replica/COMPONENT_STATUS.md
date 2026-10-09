# 복제 컴포넌트 현재 상태

규칙: [MASTER_RULES](MASTER_RULES.md) · 원본/구현 위치와 의존: [COMPONENT_TREE](COMPONENT_TREE.md).

## 현재 작업 · 에이전트별 한 개만 활성

2026-10-08 사용자 요청: 하위 3개 `gpt-6.1-sol / high`, root 배분·통합. [파일 소유권/환경 차례](PARALLEL_WORK.md).

| 담당 | 활성 작업 | 재개 상태 |
|---|---|---|
| connection | INACTIVE · Windows KO/EN 전체 흐름·즉시 종료0 완료 | [worker 상태](workers/connection/STATUS.md) |
| preflight | INACTIVE · MainContent blank-padding 양OS 실제 검증/즉시 종료0 완료 | [worker 상태](workers/preflight/STATUS.md) |
| home | INACTIVE · Toast 양OS 실제 검증/즉시 종료0 완료, 정본 근거 점검 | [worker 상태](workers/home/STATUS.md) |
| root | INACTIVE · 최종 증거 합성·MainContent/MainWindow LOCK 완료 | [worker 상태](workers/root/STATUS.md) |

- **[LOCKED] Typography — Windows single text reading narrow exception verified/reLOCK; existing Mac slash31 headless/actual render 일치, 기존 glyph/layout 증거 재사용; Mac heading peer 좁은 교정 양OS headless4/actual4 완료.** [계약/증거](specs/Typography.md#최종-판정--typography-lock--2026-10-06)
- **[LOCKED] Motion — public playback132fractional 양OS 일치 및 실제창 완료/취소/reduced 확인.** [명세](specs/Motion.md)
- [NOT PRESENT] 독립 Divider:unused CSS class. 실제 구분선은 Surface/부모 border에서 검증. [근거](specs/Divider.md)
- [LOCKED] SvgIcon / Surface. 기존 보호 범위 유지.
- **[LOCKED] ActionButton — 104state/52timing/양OS28local-box 및 actual입력 대조 완료. Spinner 조립·KR/EN normal/reduced busy·양OS actual 제거/복귀 검증까지 완료했다.** [명세](specs/ActionButton.md)
- **[LOCKED] Spinner — 원본 연결 busy16px 표시의 paint/lifecycle를 검증했다.** [명세](specs/Spinner.md)
- **[LOCKED] Checkbox** — 양OS72 endpoint /216 timing / actual Space·pointer·Enter·disabled·reduced 검증 완료. [명세](specs/Checkbox.md)
- **[LOCKED] Radio** — 양OS48 endpoint/144 timing/actual group 및 focus-before-change 검증. [명세](specs/Radio.md)
- **[LOCKED] TextInput** — NumberInput peer확장을위한sealed제약해제한줄의양OSdefault20case재검증후재LOCK. [기록](specs/TextInput.md)
- **[LOCKED] NumberInput** — 139 수치 상태·양OS actual input/hold/paste·Mac4상태 paint 일치. Windows raster 및 browser pattern의 적용 경계는 [명세](specs/NumberInput.md)에 명시했다.
- **[LOCKED] StatusPill** — 양OS76/76 layout·palette·dot·context·갱신 대조 완료. [명세](specs/StatusPill.md)
- **[LOCKED] IconButton** — 양OS44상태/12이름갱신 및 actual입력 대조. [명세](specs/IconButton.md)
- **[LOCKED] SearchInput** — 양OS48layout/7update·부모여백·actual clear/focus/Escape 경계 검증.
- 단위마다 턴 종료/새 승인 없이 다음 의존 작업을 계속한다. 실제 미검증은 숨기거나 잠그지 않는다.
- 원본 보호: [manifest](source/manifest.json), W01 후보 제외. 변경 전 verify_lock.py 실행.

## 전체 상태

- [LOCKED] SvgIcon · [명세](specs/SvgIcon.md)
- [LOCKED] Typography · Windows ordinaryText single leaf source-backed exception verified · [명세](specs/Typography.md)
- [LOCKED] Surface · [명세](specs/Surface.md)
- [LOCKED] Motion
- [LOCKED] ActionButton · Windows-only leaf peer headless16/actualKOEN2 + Mac비영향8 재LOCK
- [LOCKED] Spinner
- [LOCKED] Checkbox · 부모 포인터 API 교정 후 양OS72 endpoint 및 actual 동일-input 재포커스 회귀 통과.
- [LOCKED] Radio
- [LOCKED] TextInput
- [LOCKED] NumberInput
- [LOCKED] StatusPill
- [NOT PRESENT] Divider · [근거](specs/Divider.md)
- [LOCKED] IconButton · [명세](specs/IconButton.md)
- [LOCKED] SearchInput · [명세](specs/SearchInput.md)
- [LOCKED] StepBadge
- [LOCKED] SidebarItem · [명세](specs/SidebarItem.md)
- [LOCKED] Notice · 양OS42/42 geometry·inline baseline·actual mount/peer 검증
- [LOCKED] DbmsOption · 양OS80/80 geometry·50timing·실제입력/포커스·lifecycle 검증
- [LOCKED] Field · 양OS44+44 layout/update 및 실제 label3개·disabled·port focus 일치.
- [LOCKED] CategoryHeader · 양OS192 geometry/update·96timing 및 actual independent pointer/keyboard/held/reduced/peer 검증
- [LOCKED] TableMappingRow · 양OS48layout/update·96timing·36actual input snapshot 검증. 공유 열 auto-layout/모달은 부모 소유.
- [LOCKED] ResultCount · 양OS54layout/update·48WAAPI timing·10actual attached lifecycle 검증. 수치/zero/공통축막대/빗금/clip; 행 입력은 부모.
- [LOCKED] ResultSummaryRow · 양OS230layout/update·72timing,48actual input·18attached lifecycle 검증. Compact/whole-row/detail request 계약 잠금.
- [LOCKED] ResultChart · 양OS38조립 layout/update·source18/native30lifecycle·actual8상세요청 검증. Results입장/Detail모달은부모소유.
- [LOCKED] Sidebar · 양OS62배치/상태 차이0, actual 이동 요청·실행중 차단·재열기/언어 갱신 검증. [명세](specs/Sidebar.md)
- [LOCKED] ScrollViewport · MainContent Windows previous-gutter-first opt-in c721 재LOCK; source/history18/headless+actual2499/0; default Sidebar branch protected · 양OS64배치/경계·최소높이8조건·actual 입력/overlay 검증. OS 기본 scrollbar renderer 경계 차이는 최신 사용자 조건으로 허용하고 기록. Sidebar 수직 범위. [정본](specs/ScrollViewport.md)
- [LOCKED] TopBar · 양OS8조건 배치/문구/접근성 차이0, 실제 언어 전환·버튼 교체 검증.
- [LOCKED] ActionFooter · 양OS 각각132조건 배치/갱신/아이콘 차이0, actual 요청·disabled·언어 변경 확인. 기본 OS renderer 표현 차이는 명세에 기록. Page 정책은 부모 소유.
- [LOCKED] ConnectionPanel · shadow 최종 CSS override/alpha 교정 후 양OS40조건(실제shadow 포함) 재검증. 입력 기존 증거 유지.
- [LOCKED] CategorySection · 양OS192상태/72transition 및 실제9상태·접근성 목록 열림/닫힘 검증.
- [LOCKED] MatchingDialog · 양OS48 기하/상태/shadow 및 실제 포커스·검색·적용/취소·충돌·reduced 검증. Windows 600px root scrollbar16조건은 측정 근거와 OS예외 보존. [명세](specs/MatchingDialog.md)
- [LOCKED] MatchOption · 양OS136상태/50transition 및actual9상태 검증. 부모포커스는MatchingDialog범위.
- [LOCKED] TrialPreparation · 양OS40 배치/갱신 차이0, lifecycle7 및 actual 준비/수정/숨김/English 요청 확인.
- [LOCKED] PreparedDb · 양OS96 배치/문구/줄바꿈 차이0, attached lifecycle10 및 시각 확인.
- [LOCKED] ResultDetails · 양OS40 배치/상태 차이0, lifecycle6 및 actual close/focus/scroll/reduced 검증.
- [LOCKED] BulletList · 양OS24 기본 목록 배치/줄바꿈 차이0 및 List/ListItem 실제 갱신 검증.
- [LOCKED] ResultDetailsBody · 양OS80 내용/배치/분기 차이0, peer80/lifecycle5 확인.
- [LOCKED] SaveOptions · 양OS48 조립 배치 차이0, actual 선택집계/삽입순서/재생성 포커스·event metadata 검증.
- [LOCKED] DownloadOption · 양OS36 배치/상태/기준선 차이0, 실제 입력·포커스·갱신 검증.
- [LOCKED] PageHeading · Home 부모API 좁은예외: Headless84+12 및 실제양OS84+12/actualfocus 검증 후 재LOCK.
- [LOCKED] InlineText · Mac combined leaf / Windows ordered prefix+suffix sibling leaves d135 재LOCK; headless7/0+actual4/0; process/window0; render/clip unchanged · 양OS32 동일문장줄배치/색상구간 및 actual paint·attached reuse 검증.
- [LOCKED] SummaryItem · 양OS384 row/label/value 기하·색상·문구·border/media/갱신 검증.
- [LOCKED] DbmsPicker · 양OS64기하/문구/집계와 실제순환·언어·silent 갱신 검증.
- [LOCKED] Choice · 양OS32기하/실제본문·입력·disabled·취소검증.
- [LOCKED] SummaryList · 보호요약행을0gap으로조립; panel/plain 표면.
- [LOCKED] SampleReport · 양OS8기하/문구/색상·actual수명3 및 시각검토 완료.
- [LOCKED] WelcomeStep · HEADLESS_PASSED24/수명3, 실제Mac24·Windows24 strict0/수명3/paint 완료.
- [LOCKED] WelcomeSteps · Headless8/실제양OS8 strict0·수명3·paint 검증.
- [LOCKED] WelcomePaperContent · Headless8/양OSactual8·수명3·paint 검증.
- [LOCKED] WelcomePaper · 양OS12/수명3·actual activation6/passive2·paint 완료.
- [PARENT SCOPE] WelcomeEntryHint · 독립 상태 없는 Home 내부. 교정·기존 증거는 HomePage에 포함.
- [LOCKED] Home · 부모16/수명3 및 양OS 동작/neutral focus 검증 완료. [명세](specs/HomePage.md)
- [LOCKED] AdvancedConnection · 양OS32기하/문구/peer 및 실제 펼침·포커스·lifecycle 검증.
- [LOCKED] Connection · Headless96/validation62/입력10 및 실제양OS입력10·busy·포커스·수명 검증.
- [LOCKED] FindingArticle · Headless42/실제양OS42·수명·paint 및 unnamedgroup/4Text peer 교정 검증.
- [LOCKED] MetricItem · 양OS72/수명·natural72/actual AX2Text/paint 검증.
- [LOCKED] Metrics · 양OS72/수명8/ordered8Text/paint/cleanup0 검증.
- [LOCKED] CapabilityRow · Windows Cell peer 좁은예외 양OS48/parentButton2 재LOCK · Headless48/입력16 및 실제양OS48기하/48timing/입력11·paint·AX 경계 검증.
- [LOCKED] CapabilityHeader · aca8 parent fractional fit5/actualWindows48 strict0・即時close0 · 양OS12/입력6 및 Windows named no-child peer 교정3 완료.
- [LOCKED] CapabilityTable · 양OS 조립/actual/peer/control projection 및 즉시 종료0
- [LOCKED] CapabilityViewport · 양OS72/13/19/focus4 및 실제 clip/held/paint 검증. [명세](specs/CapabilityViewport.md)
- [LOCKED] Preflight · 양OS16 strict1106/0 + callbacks/focus/life/motion/paint; Optional semantic2 actual 양OS·즉시close0
- [LOCKED] Selection · Mac cached original peer ancestry7b3c reLOCK; actualKOENpositivebounds/identity/callbacks/close0; reason4521/5c62 unchanged · 양OS 원본36/metadata 신규검증 완료, 부모callbacks50 별도
- [LOCKED] Trial · viewport61e; 기존 양OS80/입력14/hover12/AX/paint 및 새 부모resize 각OS8/416 strict0/즉시close0
- [LOCKED] Review · 양OS120조립·실제확인/포커스/허용/요청/수명 검증.
- [LOCKED] ProgressMeter · 양OS30/18/7·actual AX30/paint/cleanup 완료.
- [PARENT SCOPE] RunningDot · Progress 내부 장식. 원본8/후보 빌드 증거 보존.
- [LOCKED] Progress · 양OS80/수명9/actual입력·firstlive·AX·paint·즉시close0; 앱 연결은 별도 검증
- [LOCKED] Results · 양OS144/입력/motion/focus/AX 검증 및 정확 종료0
- [LOCKED] Save · 양OS48조립·실제형식/포커스/요청/수명 검증.
- [LOCKED] InfoDialogBody · 양OS32기하/문구/색상 차이0, 실제 모달조사·attached7수명·시각확인 완료.
- [LOCKED] InfoDialog · Headless32/finite8 및 실제양OS32·수명6·입력/scroll/복원 검증.
- [LOCKED] MainContent · 9페이지 연결/양OS KO·EN 전체 흐름 완료; blank-padding·Toast·SummaryContent·SelectionCallbacks 완료 · [계약](specs/MainContent.md)
- [LOCKED] MainWindow · 실제 앱 --replica-preview 최종 통합·양OS 흐름·파일 연동 완료 · [계약](specs/MainWindow.md)

## 완료 판정

컴포넌트 명세에 원본/구현·상태·부모 배치의 검증 증거가 모두 연결되면 해당 항목만 LOCKED로 바꾼다. 직접 하위/페이지 연결 예외는 [MASTER_RULES §13](MASTER_RULES.md#13-페이지-완성--실제-앱-연결--사용자-흐름-검증--2026-10-08)을 따른다. 이 목록은 현재 상태의 정본이며 진행 이야기는 활성 명세에만 짧게 기록한다.

## 페이지별 통합 상태

정책은 [MASTER_RULES §13](MASTER_RULES.md#13-페이지-완성--실제-앱-연결--사용자-흐름-검증--2026-10-08)을 참조한다. 기존 실행 앱의 과거 화면은 복제 페이지 연결 근거가 아니다.

| 페이지 | 구현 | 실행 앱 연결 | 연결/사용자 흐름 검증 |
|---|---|---|---|
| Home | LOCKED | CONNECTED · 기존 앱 --replica-preview | 부모 headless8/newflow15 통과 · 실제Mac8/입력/파일취소 완료 · Mac KO/EN 전체흐름29각 통과 · Windows KO29/EN29 실제 연결flow 완료/종료0 |
| Connection | LOCKED | CONNECTED · 기존 앱 --replica-preview | 부모 배치 양OS24 통과 · 새headless13/MacKO/EN 실제flow 통과 · Windows KO29/EN29 실제 연결flow 완료/종료0 |
| Preflight | LOCKED · 0a7b0e21 | CONNECTED · 기존 앱 --replica-preview | 새parent/콜백 양OS headless72/0 + 실제nativeauto72/0 각각 close0 · Mac KO/EN 실제 연결flow 통과 · Windows KO29/EN29 실제 연결flow 완료/종료0 |
| Selection | LOCKED · reason45/5c/peer7b3c | CONNECTED · 기존 앱 --replica-preview | 새52handler strict2988/0; MacKO/EN 실제 흐름 통과 · 추가callbacks source50/headless848·actual35(28 UI+7 labeled API/setup) 양OS/언어 완료 · Windows KO29/EN29 실제 연결flow 완료/종료0 |
| Trial | LOCKED · resize61e… 양OS actual 재LOCK; 기존 b5ef 보호증거 보존 | CONNECTED · 기존 앱 --replica-preview | 기존 양OS 부모/finite30/boundary8 재사용 · 새resize8/1100배치104 및 Stopped2 headless 차이0 · resize 각OS8/geometry416 strict0 및즉시close0 · MacKO/EN Selection 귀환/전체flow 통과 · Windows KO29/EN29 실제 연결flow 완료/종료0 |
| Review | LOCKED | CONNECTED · 기존 앱 --replica-preview | 부모 배치 양OS24 통과 · 새headless13/MacKO/EN 실제flow 통과 · Windows KO29/EN29 실제 연결flow 완료/종료0 |
| Progress | LOCKED · root · 장식은 부모 범위 | CONNECTED · 기존 앱 --replica-preview | Mac actions10/parent60·Windows actions10 통과 · MacKO/EN 실제flow 통과 · Windows KO29/EN29 실제 연결flow 완료/종료0 |
| Results | LOCKED · c9ad… | CONNECTED · 기존 앱 --replica-preview | 부모68 headless/실제Mac콜백30 통과 · Windows 실제30의 callback/상태489통과 · modal6 실제25/0, peer actual44×KOEN/0 및 원본같은10/3line wrap36/0 완료·즉시close0 · 양OS KO29/EN29 연결flow 완료 |
| Save | LOCKED | CONNECTED · 기존 앱 --replica-preview | 부모 배치 양OS24 통과 · 실제양OS3형식/파일내용/재열기/EN취소/오류 통과 close0 · Mac KO/EN 전체navigation 통과 · Windows KO29/EN29 전체navigation 완료/종료0 |
| 전체 앱(UI replica-preview 범위) | LOCKED | 9페이지 연결 · 전체 제품 빌드0/0 | 원본52handler strict2988/0; 실제Mac KO29/EN29/close0 · Windows KO29/EN29 실제 흐름 strict44/3308/0/종료0 완료 · 양OS blankpadding 실제입력 완료/종료0 · Toast 양OS595/0/종료0 완료 · 부모 문구·추가callbacks 양OS 완료 |

- TrialPage resize 좁은 LOCK 예외 재LOCK 완료: original source8 vs candidate8 30차이; 근거/영향/API는 [TrialPage 명세](specs/TrialPage.md#제안된-좁은-lock-예외--부모-resize-identity--아직-제품-변경-없음). home는 own WebTrialPage SetViewport만, root는 Workspace resize 호출만 변경한다. old hash/증거 보존, 새 각OSresize8/416 strict0 증거 후61e 재LOCK.

- PageIntegration 실제 파일 저장·재열기: Mac 기존byte4/0 및 Windows 새 nativeflow6/0/byte4/0/즉시PID2364 close0. [Windows 증거](source/root-pageintegration-files-windows-comparison.json). Mac KO/EN 전체navigation 통과 · Windows KO29/EN29 전체navigation 완료/종료0.
- 작은viewport 새modal16/body128scalar Mac HEADLESS_PASSED0 및 원본tab69close0: [증거](source/root-modal-bounds-mac-comparison.json). actualMac128/0 및 freshWindows source4/headless128/0/native128/0 완료/ownedclose0; physicalwholeflow는별도.

## REPLICA-01 완료 · 2026-10-09

[최종 합성 증거](source/root-final-assembly-proof.json): 모든9페이지 구현·실제 앱 연결·양OS 한국어/영어 사용자 흐름 완료. 보호71개/101범위 검사 통과, 현재 전체 제품129파일 해시 일치, 두RID 빌드 경고/오류0. 담당자/실제OS 자원 모두 INACTIVE, 완료된 소유 검증 창·프로세스 종료0. MASTER§14 종료 조건은 유지하며 AGENTS 임시 압축 재개 절만 제거했다.

이는 원본 웹 예시 화면의 복제 완료다. Windows 영어 흐름은 동일 DLL을 공식 x64 실행기로 검증했으며 배포용 apphost 시작 원인·릴리스 검증은 별개로 남는다. 기본 DB 실행 경로·실제 검증 범위 확장·BR-B 제출/포트폴리오는 이 완료 범위 밖이다.
