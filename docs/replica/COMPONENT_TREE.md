# 복제 컴포넌트 의존 지도

원본: [실행 웹](http://127.0.0.1:8765/), [HTML](../../examples/ui-prototype/index.html). 코드 기준은 [캡처 manifest](source/manifest.json). 현재 파일명은 논리 컴포넌트의 매핑이며 새 구조를 먼저 만드는 설계안이 아니다. 필요할 때 해당 원본과 직접 부모/자식만 다시 조사한다.

## 하위 → 상위 의존

```text
Primitive
  SvgIcon / Typography / Surface / Motion / Spinner
Atomic
  ActionButton ← Spinner + SvgIcon + Typography + Surface + Motion
  Checkbox / Radio ← Surface + Motion
  TextInput / StatusPill
Composite
  IconButton ← ActionButton + SvgIcon + Typography + Motion
  SearchInput ← TextInput + IconButton + Surface
  NumberInput ← TextInput (원본 number UA steppers/입력 정리 별도 검증)
  StepBadge ← SvgIcon + Typography + Surface
  SidebarItem ← StepBadge + Typography + Surface + Motion (원본 .step 전용 Button; .btn 입력 계약 대조)
  Notice ← SvgIcon + Typography + Surface + Motion
  DbmsOption ← Radio + Typography + SvgIcon + Surface + Motion
  Field ← TextInput + NumberInput(포트) + Typography
  CategoryHeader ← Checkbox + SvgIcon + Typography + StatusPill + Surface + Motion (직접 DOM 확인: 전용 전체 헤더 Button, IconButton 자식 없음)
  TableMappingRow ← Checkbox + Typography + SvgIcon + ActionButton + Surface
  ResultCount ← Typography + Surface + Motion
  ResultSummaryRow ← ResultCount + Typography + SvgIcon + Surface + Motion (원본 plain button.result-row; .btn 자식 없음)
  ResultChart ← ResultSummaryRow + Typography + SvgIcon + Surface + Motion
Section
  ScrollViewport ← Surface (원본 .rail overflow:auto의독립스크롤 입력/OS gutter)
  Sidebar ← SidebarItem + Typography + Surface + ScrollViewport
  TopBar ← SvgIcon + Typography + ActionButton + Surface
  ActionFooter ← ActionButton + Spinner + SvgIcon + Typography + Surface + Motion
  ConnectionPanel ← Field + StatusPill + SvgIcon + Surface
  CategorySection ← CategoryHeader + Typography + Surface
  MatchOption ← Radio + Typography + Surface + Motion
  MatchingDialog ← MatchOption + SearchInput + Radio + IconButton + Surface
  PreparedDb ← SvgIcon + Typography + Surface + Motion
  TrialPreparation ← PreparedDb + Notice + ActionButton
  BulletList ← Typography
  ResultDetailsBody ← BulletList + Notice + Typography
  ResultDetails ← ResultDetailsBody + IconButton + ActionButton + Typography + Surface + Motion + ScrollViewport
  DownloadOption ← Checkbox + Typography + Surface
  SaveOptions ← DownloadOption
  DbmsPicker ← DbmsOption + Typography
  Choice ← Checkbox + Typography
  PageHeading ← Typography + Surface
  InlineText ← Typography
  SummaryItem ← InlineText + Typography + Surface
  SummaryList ← SummaryItem + Surface
  CapabilityRow [LOCKED] ← Typography + StatusPill + ActionButton
  CapabilityTable [LOCKED] ← CapabilityHeader + CapabilityRow + CapabilityViewport + 원본 column allocator
  FindingArticle [LOCKED] ← Typography + Surface
  WelcomeStep [LOCKED] ← Typography (literal article/shared-subgrid text tracks)
  WelcomeSteps [LOCKED] ← WelcomeStep ×3 (parent columns/outer margin source contract)
Page
  Review ← PageHeading + SummaryList + Notice + Choice + Typography + Motion
  Save ← PageHeading + SaveOptions + SummaryList + Notice + ActionButton + Motion
  Home ← WelcomePaper + WelcomeSteps + SampleReport + PageHeading
  Connection ← ConnectionPanel + DbmsPicker + AdvancedConnection + PageHeading
  Preflight ← CapabilityTable + Notice + PageHeading
  Selection ← CategorySection + TrialPreparation + SearchInput + Checkbox + TableMappingRow + CapabilityViewport + PageHeading
    독립 상태 없는 toolbar/auto-column allocator/empty/caption은 SelectionPage.Layout 부모 범위
  Trial / Review / Progress / Results / Save ← 각 페이지 명세의 LOCK된 직접 하위
Application
  MainContent ← 각 Page
  MainWindow ← TopBar + Sidebar + MainContent + ActionFooter
```

상위 작업 시작 전 실제 직접 의존 항목을 명세에 확정하고 모두 LOCKED인지 확인한다. 페이지별 필요한 하위 목록은 해당 페이지 착수 시 원본 함수/DOM으로 좁혀 기록한다. 표의 페이지 경계는 기존 웹 `home/connect/capabilities/select/trial/review/running/results/save`에서 확인했다.

## 원본 ↔ 기존 Avalonia 위치

| 논리 컴포넌트 | 원본 DOM/CSS/JS 검색 위치 | 현재 구현 위치 |
|---|---|---|
| SvgIcon | `icons`, `icon()`, `.svg`, 부모별 svg override | `MainWindow.Shell.cs` · UiIcon |
| Typography / Motion | `:root`, body/h1–h3, `@keyframes`, `press/animateResult` | `MainWindow.cs` · Text/Pulse, `MainWindow.Shell.cs` · Enter, App.axaml |
| Surface / Divider | panel/table/card/border별 선언, `.divider` | `MainWindow.Shell.cs` · Panel, `MainWindow.cs` · Line |
| ActionButton / IconButton | `.btn`, button, `[data-action]`, `press()` | MainWindow.cs · Action/Pulse; MainWindow.Shell.cs · WithIcon; App.axaml |
| Checkbox / Radio / TextInput | input/choice/role별 CSS와 event handler | AlignedCheckBox.cs, PasswordTextBox.cs, App.axaml, 각 view |
| StatusPill / Notice | `.pill`, `.notice` | MainWindow.Shell.cs · Pill/Notice |
| SearchInput | `.search-field`, table/matching 검색 handler | MainWindow.ScopeViews.cs, MainWindow.Matching.cs |
| SidebarItem / Sidebar / TopBar | `topAndRail()`, `.step/.steps/.rail/.topbar` | MainWindow.Shell.cs · Render |
| ActionFooter | `footer()`, `.foot` | MainWindow.Shell.cs · Render |
| ConnectionPanel / DbmsOption / Field | `connectionSide/connect()`, `.dbms-option/.field` | MainWindow.HomeConnection.cs |
| CategoryHeader/Section, TableMappingRow, TrialPreparation | `select/trialPreparation/tableRows/trial()` | MainWindow.ScopeViews.cs |
| MatchingDialog | `matchingOptions()`, matching dialog event handler | MainWindow.Matching.cs |
| ResultChart / ResultSummaryRow / ResultDetails / SaveOptions | `resultChart/results/findings/trialResult/save()` | MainWindow.ResultViews.cs |
| Pages / MainContent / MainWindow | `render/go()`, `#main`, `.page/.app` | MainWindow.cs, MainWindow.Shell.cs, 각 view partial |

구현 경로는 모두 `src/Verifier.Desktop/` 기준이다. 현재 하나의 partial 파일에 여러 논리 컴포넌트가 섞여 있다. LOCK은 파일 전체가 아니라 명세에 지정한 심볼·스타일 범위에 적용한다. 같은 파일의 다른 컴포넌트를 작업해도 LOCK된 범위를 변경하지 않는다.

Typography의 고립된 macOS 후보 구현 위치는 `WebTypography.CreateCandidate` / `WebSystemTextBlock` / `MacSystemTextLayout`이다. 이 후보의 검증과 기존 MainWindow helper의 상위 조립은 구분한다. 원본 역할·현재 증거·미완료 범위는 Typography 명세가 정본이다.

2026-10-06 독립 Divider는 원본에서 실사용되지 않는 CSS 선언으로 확인했다. [조사 근거](specs/Divider.md). Sidebar의 실제 구분선은 rail-note border이며 Surface/부모 범위에서 검증한다.

2026-10-06 실제 Connection busy의 `.btn.primary:disabled > span.spinner`를 확인했다. Spinner는16px 순수 paint+Motion primitive이며 ActionButton은 해당 하위 LOCK 후 loading을 조립한다. primitive를 만드는 동안 ActionButton 후보는 WAITING CHILD로 보존한다.

2026-10-07 ActionFooter 직접DOM은 .btn quiet/primary/danger이며 IconButton이 아니다. 해당하위계약을수정해지도에기록했다. ScrollViewport WAITING PAINT와Sidebar WAITING CHILD를보존하고모든직접하위가LOCKED인독립ActionFooter를순차작업한다.

2026-10-08 Connection의 직접 하위 AdvancedConnection ← ActionButton + Typography + Surface + Motion을 별도 section으로 검증 후 상위 페이지를 조립한다. DbmsPicker의 직접 자식은 잠긴 DbmsOption 네 개다.

2026-10-08 병렬 담당: Connection=connection; CapabilityRow → CapabilityTable → Preflight=preflight; SampleReport 및 Home 내부 섹션 → Home=home; InfoDialogBody → InfoDialog/root Application=root. 상태/파일 소유권은 PARALLEL_WORK 및 각 workers 상태를 따른다. 부모 조립은 직접 하위 LOCK 이후.

InfoDialogBody ← SummaryList + Notice + Typography (양OS32/7 LOCK); InfoDialog ← InfoDialogBody + 보호 ActionButton/Surface/ScrollViewport 계약.

2026-10-08 확인한 내부 의존: WelcomePaperContent(LOCK) → WelcomePaper(+SvgIcon/ActionButton) → Home. WelcomeSteps ← WelcomeStep(각LOCK). CapabilityHeader + CapabilityRow + CapabilityViewport → CapabilityTable → Preflight(직접 하위 LOCK 완료). FindingArticle → Results 내부 상세 목록. InfoDialog LOCK은 잠긴 InfoDialogBody/공통 모달 하위를 조립하고 actual 입력을 검증했다.

ProgressMeter(.meter/.meter-bar)→Progress: readonly accessible progress value, full-width clip/200ms linear transform. ProgressMeter LOCK. RunningDot은 독립 상태 없는 Progress 내부 장식으로 포함한다.

현재 페이지 경계/부분 연결/장식 묶음 정책은 [MASTER_RULES §13](MASTER_RULES.md#13-페이지-완성--실제-앱-연결--사용자-흐름-검증--2026-10-08)을 따른다. 원본에서 독립 동작이 있는 CapabilityViewport는 Preflight/Selection/Progress 표에서 사용한다.

2026-10-09 현재 복제 실행 경로는 `App/MainWindow --replica-preview` → `ReplicaWorkspace`의 보호 Web* 페이지 조립이다. 위 기존 MainWindow partial 표는 과거 구현 위치이며 복제 일치 근거가 아니다. 구현·연결·흐름 상태는 COMPONENT_STATUS가 정본이다. 현재 Selection/9페이지와 MainContent/MainWindow 전체 조립은 LOCKED·연결됐으며 양OS 한국어/영어 사용자 흐름 검증을 마쳤다. 정확한 상태는 COMPONENT_STATUS만 참조한다.

Application assembly contract: [MainContent](specs/MainContent.md) / [MainWindow preview scope](specs/MainWindow.md), implemented in `ReplicaWorkspace.cs`, `App.axaml.cs`, the preview constructor in `MainWindow.cs`, and `MainWindow.ReplicaActions.cs`. Legacy live-DB partial views are outside the UI replica LOCK. Current gate is COMPONENT_STATUS; source/implementation locations above describe the earlier legacy helpers and do not override the current Web* assembly.
