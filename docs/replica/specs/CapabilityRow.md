# CapabilityRow

## 현재 상태
LOCKED · CapabilityTable 부모 요구 Windows semantic 좁은 LOCK 예외의 headless/actual 양OS48 완료 및 root 공유Button dependency 재LOCK 확인. 이전 headless48/입력16 및 실제 양OS48/전환48/actual 입력11 증거는 해당 불변범위 재사용. 직접 하위 Typography/StatusPill/ActionButton/Surface/Motion LOCKED. 원본 table auto-layout는 부모가 공급한다.

## 원본 계약 · 2026-10-08
실행 original.js capabilities()의 tbody tr 4개. data/schema/access/objects 순서와 bilingual categoryDefs name/desc를 그대로 사용한다. 실제 localhost 원본에서 새 검사→예시 연결→사전 확인을 직접 확인했다. Worker browser inventory가 비어 자체 탭을 만들었고 root user original은 조작하지 않았다.

- tr → td(strong + small), td.capability-state(pill ok Connected), td.capability-state(pill ok Available 또는 access warn Partially available), td(div.capability-details → span + access에만 quiet permissions button).
- td12×18 padding, collapsed border1 (#41434a), vertical middle. strong font13/700 line1.6 =20.796875 DPR2, inherited #dbdee1. small block font12 line19.1953125 #b5bac1 margin-top3.
- pills 기존 계약 dot6/gap6/pad3×8, connected ok, access warn, 나머지 capability ok. 가운데 정렬, nowrap.
- details flex gap12 / align center / justify space-between font12, span flex1/min-width0. permissions quiet12/600/pad6×10/min-height44/flex:none. 부모 행/셀은 클릭 action 없음. button만 즉시 permissions 요청, modal/실제 권한/탐색은 Application 소유.
- 좁은 폭에서는 access detail span 폭0까지 축소되어 여러 줄로 커지고 button이 셀을 넘어간다. 원본을 개선하지 않는다.
- row transition background-color160ms (원본 default ease), tbody tr:hover #3a3c44. 행에 selection/focus 정책 없음. passive cell pointer는 브라우저처럼 기존 focus 해제.
- border collapse는 첫/중간 top.5 bottom.5, last top.5 bottom0. table/header/shell/column allocation/가로scroll은 부모 소유.

## 증거
source/capabilityrow-mac-source.json: 실제 원본48행 KO/EN × 내폭478/610/656/858/904/1118. source/capabilityrow-mac-cases.json의 공유 columnWidths를 atomic row 부모 입력으로 전달한다. fixture는 보호 original CSS bytes, capabilities() literal definition/categoryDefs를 사용하고 heading/notice/section만 제거한다. 원본 수정 없음. prepare_capability_row.py에 조사 fixture가 보존됐다.

## 재개/진행
AGENTS→MASTER_RULES→COMPONENT_STATUS→PARALLEL_WORK→worker 상태를 읽고 verify_lock47 통과 후 조사했다. 원본 td의 final CSS가 초기14×16과 달라 최종12×18 적용을 실제 computed로 확인했다. permissions 원본 modal focus는 header 닫기; close 후 opener 복귀. Modal 구현은 root의 Application scope.

## 검증/차이
최초 단계의 미검증 기록은 아래 교정/실제OS 결과로 갱신했다. 현재 geometry/색상/문구/행 state·actual 입력 통과; 최종 증거 audit 뒤 LOCK한다.

## LOCK 변경 기록
없음.

## 후보 조립·접근성
4개의 private Cell panel에 source td의 직접 자식들을 조립한다. Row peer(DataItem/localized row) → Cell peer(Custom/localized cell) 4개, 각 실제 label/pill/details/button child를 유지한다. Avalonia12.1.3의 enum은 Row/Cell 전용 값 없이 DataItem/Custom을 사용한다; ClassName과 localized role을 원본의 row/cell로 지정한다. Table/header peer는 다음 부모 범위. Probe에서 실제 생성된 Row/fourCell tree를48조건 모두 읽고 검증한다. 결과는 top-level peers 증거에 보관해 palette/geometry와 구분한다.

## 첫 양OS 대조 · 2026-10-08
mac48:78개 차이는 전부 StatusPill intrinsic width의 fixed-point ceil 미적용(최대0.007 CSSpx). 나머지 모든 geometry/text 및 Row/fourCell peer48 통과. Windows48:39개 차이, 30개 동일pill intrinsic ceil, 9개 KO478 access details span 폭0에서 Avalonia TextBlock의 zero-width measure가 무제한으로 해석되어 발생했다. 원본span 폭0은249.5390625px/13줄이며 후보1줄이다. 두 차이 모두 미교정/미검증으로 기록하고 LOCK하지 않는다. 독립 부모layout 교정 뒤 양OS 재검증한다. build는 양OS0warning/0error; 빌드만 통과로 삼지 않는다. Windows 최초 auto CLI InvalidArgument 실패 뒤 실행process0/파일absent를 read로 확인한 후 재실행했으며 새48JSON만 비교했다.

## 교정 근거 · 수정 전/후
첫 mac source/native 연결pill72.8984375/72.89501953125, available48.765625/48.760009765625. Windows EN Connected86.4609375/86.458984375, Partially121.484375/121.48046875. 모두 부모 inline-flex 기여 width `ceil(intrinsic*128)/128`에 해당한다. 보호된 Pill 구현은 바꾸지 않고 행에서 allocation을 ceil한다; 중앙위치는 같은 fixed-point floor 식으로 계산한다. Windows zero-span은 `measure width=1/Units`로 정상 wrap을 강제하고 arrange 폭은 실제 source0으로 유지한다. 이는 부모flex span의 CSS box0/overflow를 보존하기 위한 행 내부 계약이며 primitive 변경이 아니다. 교정후 실행은 아직 대기중이며 통과로 기록하지 않는다.

## zero-span paint 교정 경계
독립 코드 확인에서 보호 Typography의 mac Render가 Bounds.Width<=0이면 그리지 않는다는 사실을 발견했다. 원본 KO478 span의 폭0/높이249.5390625/overflow text가 존재하므로 measureepsilon만으로 paint 완료를 주장할 수 없다. 행 Details 내부에 literal CSS span을 대응하는 private OverflowSpan wrapper를 추가했다. wrapper의 실제 box/arrange 폭은0, text-node에 해당하는 보호 text child만 최소1/Units 폭으로 measure/arrange하여 기존 정상 wrap/overflow paint를 사용한다. 클립/문구/좌표/height는 바꾸지 않는다. Probe의 detailText geometry 대상은 이 CSS span 대응 wrapper이며 underlying text lines/count도 별도 기록한다. KO478 PNG를 새로 추가해 실제 glyph 존재를 확인한 뒤에만 교정 완료로 판정한다. 보호 Typography는 변경하지 않는다.

## 독립 headless 단계 · 2026-10-08
압축 후 실제 AGENTS/MASTER_RULES/공유 상태/worker/명세/후보와 직접 원본 코드 다시 읽음. verify_lock의 유일한 차이는 root가 PageHeading 명세에 사전 기록한 Home 부모 API 예외다. Row 및 보호된 직접 하위는 수정 범위가 아니다. 사용자 신규 병렬 정책에 따라 root의 isolated.py source snapshot/obj/bin/temp/process/output 분리를 사용하며 실제 OS 검증 증거를 덮어쓰지 않는다.
첫 독립 빌드 0warning/0error 뒤 headless에서 강화한 Cell 상세 자식 수 검사가 실패했다. 진단 peer tree는 실제 literal td → div.capability-details → span + button 구조였다. 기존 검사에서 div를 건너뛰고 Cell 직속 자식2개를 요구한 기준 오류를 수정했다. Row/fourCell 및 중간 div의 실제 자식1/2를 각각 확인한다. 이 실패를 구조 통과나 OS 예외로 면제하지 않는다. 다음 snapshot 빌드는 다른 worker probe의 중간 문법 오류를 보존했으며 담당자가 교정했다고 전달받았다.
Row private BackgroundTrack에 기존 다른 잠금 행과 동일한 optional clock을 추가하여 실제 source CSS transition timeline seek와 hover/unhover/30ms reverse 48개 sample을 비교할 준비를 했다. 자동 row probe는 geometry 외 production timing JSON 및 headless에서만 finite pointer/Space/Enter/취소/언어/숨김/수명 입력 증거를 생성한다. 원본 추가 source fixture는 보호 CSS의 실제 hover declaration을 읽으며 기록 유틸리티는 검사 중 focus를 바꾸지 않는다. Mac 취소 driver의 mouse-up 좌표는 실제 밖으로 이동한 좌표로 교정했다.
HEADLESS_RECORDED와 HEADLESS_PASSED는 실제 macOS/Windows 완료 또는 LOCK을 의미하지 않는다. geometry48/paint/peer/finite input 이후 실제 양OS 기본환경과 입력을 다시 확인한다.

### headless 부모 allocation 후속 교정
새48 geometry에서 pill78개 폭차이는 여전히 남았고 그 밖의 기하/13줄/peer 및 headless 입력16상태는 통과했다. 원인은 부모의 ceil arrange폭을 공급해도 StatusPill의 기본 HorizontalAlignment.Left가 Control arrange에서 intrinsic폭으로 다시 줄이는 것이다. 행이 정확한 개별 box를 직접배치하므로 두 보호 pill의 public HorizontalAlignment를 Stretch로 공급해 이미 계산한 ceil box폭을 적용한다. primitive 코드/라벨/점/lineheight/색상은 변경하지 않는다. 위치는 기존 원본과 일치한 계산을 유지한다. 새 snapshot으로 확인하기 전까지 차이 해결로 기록하지 않는다.

## Windows 실제 교정 대조 · 2026-10-08
Windows resource 단독 배정 후 component-only cold win-x64 self-contained snapshot646d56b9ecb14ae0b931cdd636582694, build0warning/error. tools/WindowsControl/stage_isolated.py의 전체 archive/binary hash 및 coreclr/Avalonia.Headless를 검증해 고유 TEMP DBMV-Isolated-preflight-capabilityrow-646d…/app으로 배포했다. 옛 generic/twoDLL runtime을 쓰지 않았다. owned driver는 stage owner/component/UUID/path/title/geometry/foreground guard를 확인한다.
새 실제 source48와 native48 strict 기하/text/peer/원본 줄 수 차이0. Windows zero-span13줄 glyph overflow PNG를 직접 열어 확인했다. original CSS hover/unhover/30ms reversal48과 production48도 차이0(색상채널1/255 quantization, solver duration0.0001ms 기존motion계약). 최초 motion fixture가 CSS 첫 hover 선언을 뽑아 최종 override를 지운 오류는 별도 initial-fixture-error evidence로 보존하고 마지막선언으로 교정한 원본48만 통과에 썼다.
Actual native: pointer request1/focustrue, passive cell request불변/focusfalse, outside cancel request불변, Space release request2, Enter request3, English/hide/restore request불변, reduced request4, detach/remount focusfalse/request불변/transparent. source 실제 Space/Enter 및 language/hide/restore/reduced permissions AX/detach/remount8상태와 요청offset1을 명시해 대조했다. source UIA Record Invoke가 focus를 훔친 초기 표본은 record-focus-artifact로 보존하고 자동 delayed 기록으로 새 입력을 수집했다. 요청/포커스 판정에 초기 artifact를 쓰지 않는다.
실제 source/native 입력 JSON은 source/capabilityrow-windows-input, 자동 배치/paint/timing은 source/capabilityrow-windows-native 및 comparison에 연결된다. Source pointer/passive/cancel의 같은 입력 대조는 아직 별도 확인이 남아 있다. 각 창을 닫고 Windows resource를 root에 반납했다. Mac source timing/paint와 실제 default native48/input/paint도 남아 있으며 LOCK 없음.

## macOS 실제 교정 대조 및 재개 · 2026-10-08
AGENTS→MASTER_RULES→COMPONENT_STATUS→PARALLEL_WORK→worker→명세→직접 original capabilities()/final CSS→후보를 실제 다시 읽었다. verify_lock 유일 차이는 root가 사전 기록한 PageHeading Home API 예외이며 행/직접 자식 보호 범위는 불변이다.
Cold self-contained osx-arm64 snapshot1cb84cb8cf4f470dba133eb739d5ee47의 실제 창에서 source/native48 기하·문구·4Cell·줄수 차이0, final CSS hover/unhover/reverse48 차이0. 원본 Mac KO478 폭0 span/13줄 overflow와 후보 capabilityrow-mac-native/capabilityrow-ko-478-2.png의 실제 glyph를 직접 확인했다. 원본 table shell clip은 다음 Table 부모 범위이며 이 행만의 PNG에 임의 clip을 추가하지 않았다.
Actual Mac input은 source/capabilityrow-mac-input/actual-comparison.json의11상태 차이0이다. 포인터 요청/수동 셀 blur/outside release 취소/Space release/Enter, KO→EN/버튼 제거·복귀/reduced/행 detach·remount를 확인했다. 원본 actual source와 native counter 순서 offset을 각 check에 명시한다. 최초 native 클릭은 driver가 이전 밖 cursor를 복원한 뒤 기록해 source의 button hover와 위치가 다르다. 그 파일은 after-cursor-return으로 보존하고 요청/포커스만 비교하며 같은 위치 hover는 passive/cancel 및 전환48으로 확인한다. 이후 capture-before-return으로 고친 helper의 passive/cancel 실제 상태를 사용한다. Mac utility AXPress language는 이전 permission focus를 유지하지만 source utility pointer는 utility로 focus를 이동한다. 이를 실제 앱 차이로 처리하지 않고 AX-invoke-focus-artifact로 보존했으며 같은 guarded CGEvent utility pointer 입력으로 다시 확인한 focusfalse를 사용했다. 재확인은 reduced 이후여서 이 language 표본만 offset2, 나머지는 calibration pointer offset1이다.

### 허용된 OS 접근성 표현 경계
원본 브라우저는 tr/td를 row/cell로 노출한다. Avalonia12.1.3의 표준 DataItem/Custom peer가 Mac bridge에서 각각 AXCell/AXUnknown으로 표시된다. 앱 peer는 ClassName Row/Cell, localized row/cell, 실제 Row→4Cell→strong/small·pill·literal Details div→span/button 구조를 모두 제공하며48조건 검사했다. actual-ax-tree.txt에는 4개의 셀 ID, 모든 이름/내용, 중간 details container, KO/EN permission AXButton/label이 그대로 노출됨을 기록했다. 실제 CGEvent pointer/Space/Enter/언어·hide/restore로 권한 버튼 접근과 요청이 정상임을 확인했다. literal native role 문자열 변환은 framework 기본 provider 경계이며 누락한 앱 peer/name/child가 없다. root가 기능 영향 확인 후 이 플랫폼 기본 role 변환만 허용하도록 확정했다. framework나 잠긴 primitives를 변경하지 않는다.

Mac 고유 native 창과 성공한 원본 탭은 모두 닫았다. bundle local.dbmv.replica.capabilityrowcheck isRunning=false 확인 후 자원을 root에 반납했다. LOCK 전 남은 확인은 Windows original raw pointer/passive/cancel3개뿐이다. 기존 Windows geometry/keys/lifecycle/Mac 통과를 반복하지 않는다.

## Windows 원본 raw pointer 잔여 완료
Connection cleanup 이후 Windows 자원을 배정받아 original msedge fixture에서 SendInput pointer/passive/outside cancel3만 확인했다. title/official msedge path/고정 ID/geometry/foreground/FromPoint pid/held release/소유 foreground에서만 cursor복원 guard 사용. Fresh source는 source-pointer-0-cancel-False.json (request1/focustrue/hoverfull), source-pointer-1-cancel-False.json (request1/focusfalse/hoverfull), source-pointer-0-cancel-True.json (request1/focustrue/transparent)다. native passive/cancel과 strict0, 기존 입력8과 합쳐 comparison10 strict0. 원본 취소 후 Record는 실제 pointer이며 mousedown.preventDefault로 permission focus를 보존한다. 고유 Edge 창 닫고 source/native 제목의 남은창0을 cleanup.json에 기록해 Windows도 반납했다.
증거 audit에서 native 최초 KO button-click JSON의 재사용 이름이 이후 EN reduced pointer로 덮인 사실을 발견했다. 최초 클릭은 실제 request1/focustrue로 관찰했고 그 직후 passive/cancel 파일의 request1이 남아 있으나 최초 상태를 추측해 JSON 재구성하지 않는다. strict JSON pair에는 passive/cancel만 포함하고 native 최초 pointer 관찰은 실행 transcript 근거로 기록한다. 최종 audit는 root와 확인 중이며 아직 LOCK 없음.

### 실제 accessible name 재확인
최종 audit에서 td 이름이 child text를 합성한다는 추측을 완료 근거로 쓰지 않았다. Mac 자원 반환 후 사용자 포커스/입력을 바꾸지 않는 고유 IAB DOM-only observation에서 원본 source-ax-tree.txt를 새로 저장했다(탭7 즉시 닫음). 실제 원본 body row 자체의 표시된 이름은 비어 있고 td는 고유 fixture ID를 보인 뒤 strong/small, pill, span/button의 text names를 노출한다. 후보 actual-ax-tree.txt도 같은 cell ID 및 named children을 노출한다. 원본에서 없는 합성 셀 이름을 임의 추가하지 않는다. row/cell role 문자열의 플랫폼 mapping만 다르고 내용 이름·직접 자식 구조·권한 입력 접근은 실제 확인했다.

### live 원본과 fixture의 protocol 경계 확인
root 요청에 따라 own hidden 원본 탭8에서 예시값을 유지한 새검사→두연결확인→조회범위확인으로 live capabilities()를 관찰했다. live-original-dom-snapshot.txt의 row/cell 이름은 child문구를 space로 합성한다(예: 사용자·권한 사용자·역할·소유자·권한·행 정책). 같은 live원본 getAXState는 row/cell 자체 이름을 표시하지 않고 named text/button children을 노출한다(live-original-ax-tree.txt). 동일 standalonefixture도 DOMsnapshot은 정확히 같은 합성이름, getAXState는 cell ID와 named children을 노출한다(fixture-dom-snapshot.txt/source-ax-tree.txt). 따라서 이름 차이는 live 대 fixture가 아니라 browser DOM/ARIA snapshot protocol 대 실제 Mac AX표현 경계다. 명시 aria-label/name은 원본에 없고 child텍스트는 동일 노출된다. 후보의 실제 Mac AX도 같은 child이름/내용/버튼 접근을 제공한다. source에 없는 explicit 합성이름은 추가하지 않는다. 모든 새 source탭을 닫았고 사용자 원본 tab1을 바꾸지 않았다.

## 최종 판정 · CapabilityRow LOCK · 2026-10-08
Windows 최초 native KO click evidence 파일 충돌 우려는 root 지시에 따라 동일 cold646d runtime의 초기 pointer 한 건만 새로 확인해 native-initial-ko-pointer.json으로 보강했다(request1/focustrue/hoverfull). 이전 EN reduced pointer는 pointer-en-reduced.json으로 보존했다. 제품/geometry/keys/lifecycle 검사 재반복 없음. source raw pointer3와 strict pair까지 포함하여 Windows actual-comparison11 차이0, Mac11 차이0이다. actual48 기하/문구/4Cell/줄수 및 motion48 각각 양OS 차이0. 양OS 좁은 span0/13줄 paint 직접 확인. 원본·후보 기본renderer raster와 Mac 표준 AXrole/protocol 표현 경계만 위에 기록한다; 앱 배치·상태·입력 구현 차이나 미확인 항목 없음.
Row 부모입력 계약은 ColumnWidths4개/최소36, table 공유 auto-layout와 collapsed border shares, Source shell overflow clip은 부모 CapabilityTable 소유다. PermissionsRequested는 root Application modal callback 범위다. probe literal peer/state, bilingual4category6width 및 actual언어/hide/restore/reduced/detach/keyboard/cancel 범위를 포함한다. 최종 코드 hash와 evidence는 source/capabilityrow-lock.json 및 capabilityrow-validation.json에 연결한다. 직접 자식 보호코드를 변경하지 않았다. 모든 소유 actual창/성공한source탭을 닫고 자원 반납했다. 자동 다음 작업은 CapabilityTable의 새로운 원본 조사다.

## CapabilityTable 부모 요구 LOCK 예외 · 변경 전 2026-10-08
root 승인: Row 단일 활성, Table WAITING ROW. 압축 복구 순서의 규칙/상태/명세/원본 capabilities()/후보를 실제 읽고 verify_lock62 통과했다. 기존 보호hash ad560ed5b5c7560047c81d1de275df720540ad1080e91955da01f2b523102c29.

변경 이유: 새 부모의 실제 Windows Edge primary UIA는 tbody 셀을 DataItem, child 문구를 단일 space로 합성한 nonempty Name으로 노출하며 단순 셀 children0, access 마지막 셀만 직접 Button1이다. 이전 Mac protocol 근거를 Windows에 확장한 기존 Custom/emptyName/텍스트 하위 노출은 이 새 OS 부모 계약과 다르다. 원본 literal td/strong/small/pill/div/span/button 시각 구조는 그대로다. source-ax-initial/remount와 exact KO/EN32셀의 이름·타입·자식·물리box 원본은 [새 primary 근거](../source/capabilityrow-table-semantic-source.json)에 hash/path와 함께 보존했다. 이름 공백은 actual UIA 값 그대로 쓴다.

영향: Windows private CellPeer의 DataItem/name/children와 SetPresentation 후 이름 갱신만 수정한다. Mac 기존 unnamed Custom/실제 named child 노출, 부모 열 배치/overflow/paint/hover/입력/버튼 자체는 변경하지 않는다. Windows hidden DetailsPanel peer는 direct Button parent를 Cell로 연결할 때 visual 중간 div가 EnsureConnected로 부모를 되찾지 않도록 자식 없는 비노출 연결 wrapper로 제한한다. 이는 actual 원본 direct Cell→Button 구조를 보존하기 위한 같은 private semantic 범위다. protected Typography/Pill/ActionButton 코드는 수정하지 않는다. 기존48기하/48timing/양OS11입력/paint는 코드불변 범위로 재사용하고, 새 KOEN32셀/언어·분류갱신·제거/재부착 및 actual 양OS peer 영향만 검증한다.

Source Button children0 대 candidate Button nested Text2는 root 소유 ActionButton 별도 조사 범위다. Row direct Button 연결을 먼저 검증하되 그 아래 차이는 자동 OS면제하지 않고 부모 Table 검증 미완료로 남긴다. Table Windows passiveblur/focus→scroll118.5 차이도 입력 actor/hit/post JSON 조사 전 제품교정하지 않는다.

### 예외 새영향 검증 checkpoint
Windows-only Cell DataItem/name/empty children/access directButton 및 hidden Details 연결 구현hash f9d85fc032bcbc5206a8ddfeb0e367055e7be203bea7df18011300051ee5d269. Mac 기본 Cell/Details peer 분기는 그대로이며 시각/입력 함수는 변경하지 않았다. Mac29bb headless8926와 Windows0e4a headless9577 각각 초기KOEN32+remount16 exact메타데이터/직접Button parent 비교 strict0. Mac actual29bb 초기KOEN32 및 d667 remount16/제거cells0/실제물리box positive 비교48 strict0. d667 comparison.json에 reused-initial-evidence.json의 경로/hash를 연결한다. 실제 source Mac contract의 unnamed AXUnknown Cell, title/description/pill/details named children은 유지됐고 양OS기통과48geometry/48timing/11입력은 변경범위 밖이라 재사용한다.

Diagnostic 첫 isolated-run extraflag가 output자리로 해석돼 기존48 case deserialize 실패한 기록은 보존했고 명령만 교정했다. Mac first readiness0(창 생성 대기) guard는 입력 전에 중단됐다. 후속 actor는 KOEN32/제거snapshot 이후 remount utility가 거절됐고 ownprocess가 사라져 remount 통과로 삼지 않았다. 원인 확정 없이 diagnostic periodic Save의 detached peer.GetBoundingRectangle/parent 조회를 금지했다. 새 d667 ownstderr로그와 remaining lifecycle-only창에서 detach→remount16만 확인했으며 기통과KOEN32를 다시 검사하지 않았다. 가설/실패/정확cleanup0는 actual-macos-semantic/lifecycle-actor-failure.json 및 comparison/provenance에 보존한다. Root Button Windows leaf 새변경은 별도 root 검증 scope이며 실제 Windows Row fresh756 snapshot에 포함돼 직접Button 하위차이를 추가 확인한다. 현재 Row 재LOCK 전 Windows actual32/remount16 남음, Table 미완료 유지.

### Windows actual 예외 검증 완료
Fresh fullcold7562326b5ebf445aac3022ca654f6525 실제Windows 창에서 KOEN32+remount16 셀 name/DataItem/단순children0/access directButton1 및 actualphysical positivebox를 sourceprimary와 대조해 strict48 diff0이다. native-detached의 실제셀/permission0과 exactnativeProcess0/window0 cleanup도 확인했다. [actual Windows 비교](../../../artifacts/replica/workers/preflight/capabilityrow/7562326b5ebf445aac3022ca654f6525/actual-windows-semantic/3c9070e62120455fa4d4b83fd616824a/comparison.json).

같은 frozen parent의 KO/EN permissions Button primary type/name/children0는 sourceexact와 일치한다. [root 공유Button parent증거](../../../artifacts/replica/workers/preflight/capabilityrow/7562326b5ebf445aac3022ca654f6525/actual-windows-semantic/3c9070e62120455fa4d4b83fd616824a/capabilityrow-shared-button-parent-evidence.json)에 변경된 공유Buttonhash0556d146f7ce9a971757bf0f08305ae34a118b558e934c4c11379c352d42c5aa 및 buildmanifest를 넣었다. 기존 아랫부분 Text2 차이를 OS로 면제하지 않았으며 root의 Windows-only leaf peer가 실제 부모에서도 source0을 복원했다. Button 자체 재LOCK은 root소유며 Row는 그 dependency 확인 뒤 재LOCK한다. 새semantic actual검증으로 원본읽기/명세/코드/headless/양OS 실제조건을 완료했고, 기존layout/paint/motion/input은 변경하지 않고 재사용한다.

### 좁은 예외 최종 재LOCK · 2026-10-08
Root ActionButton leaf dependency0556d146 재LOCK 확인 후 ownRow f9d85fc032bcbc5206a8ddfeb0e367055e7be203bea7df18011300051ee5d269로 보호manifest를 갱신했다. 새양OS48 semantic diff0/물리bounds/언어/제거·재부착/cleanup0와 기존불변48geometry/48timing/11input/paint를 모두 연결한 capabilityrow-validation.json을 정본 근거로 삼는다. Windows 이름/자식 누락은 sourceprimary에 맞춰 교정했고 Macrole/protocol 기본표현만 이전계약으로 유지한다. 남은 Row 차이/미확인 없음. 단일활성 예외를 끝내고 CapabilityTable 남은 passive actor1/sourceHit/postJSON으로 자동 복귀한다.
