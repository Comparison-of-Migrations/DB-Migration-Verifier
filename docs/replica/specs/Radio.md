# Radio

LOCKED · 검증한20px 원자 후보의 계약은 아래 최종 판정이다. 부모 카드/label 조립은 별도 TODO다. 과거 진행 기록은 최종 판정보다 우선하지 않는다.

## 범위 / 직접 의존

20px radio box +8px centered dot, 상태별 색상/outline/hover/scale/transition과 native group 선택을 복제한다. 부모 dbms-option/match-option의 카드·label·크기/상태/animation은 부모 소유다. LOCKED Surface/Motion 계약을 사용한다. Checkbox를 리팩터링하거나 원본 CSS를 바꾸지 않는다.

## 다음 행동

Windows 소유 Radio창 종료 확인 후 최신 패키지를 다시 준비하고 72 timing rows를 비교한다. 필요한 실제 pointer 검증을 끝내고 적용 범위/부모 경계를 기록한다. 미검증 상태는 LOCK하지 않는다.

## 2026-10-06 조사 / 재개 기록

규칙/STATUS/명세를 읽고, Checkbox 7LOCK 보존 검사를 실행했다. 직접 original.css radio / dbms-option / match-option과 original.js dbms change/render/matchingOptions를 확인했다. 실제 연결 화면 및 대응 대화상자의 DOM/computed 상태를 수집했다. 기존 MainWindow.HomeConnection의18px 가짜 dot 및 두 DBMS만 왕복하는 arrow handler는 원본 근거가 아니다. 기존 Matching UI도 다른 구조이므로 원자 후보만 새로 만든다.

### 원본 값 / 부모

- radio input20×20 / border1.5 / padding/margin0 / inline-grid center / flex:none / radius50%. checkbox와 같은 #1e1f22/#7c838c → checked #5865f2/#8b94ff, disabled #2e3035/#727985.
- ::after8×8 content-box / border0 / radius50% / bg#2b2d31 / center10,10. unchecked scale0, checked scale1, transform160ms cubic-bezier(.22,1,.36,1).
- hover border#a7b1ff / spread3#a7b1ff20; active scale.94. focus-visible outline3#a7b1ff/offset3 / shadow:none (hover specificity가 우선). pointer focus generic shadow #5865f214. bg/border/shadow120ms / outer scale80ms CSS ease.
- DBMS card actual207.5×88 at viewport1138, grid1fr20 / gap8 / padding14 / center. svg는 display:none. 이것은 DbmsOption 부모 소유다. atomic에 카드나 label을 그리지 않는다.
- matching card의 title/description/badge, unavailable 배경과64px 최소행은 MatchingDialog/부모 소유다. disabled input은 read-only metadata와20px glyph만 맡는다.

### 실제 그룹 입력

DBMS group name=dbms, SQLServer/PostgreSQL/MySQL/MariaDB 순서. ArrowRight SQL→PostgreSQL, ArrowLeft PostgreSQL→SQL, ArrowLeft SQL→MariaDB로 순환했다. Enter MariaDB→유지. Tab은 남은 radio를 건너뛰고 Source Server textbox로 이동했다. DBMS change는 ports를 갱신하고 connection을 미확인으로 되돌리며 render로 DOM을 재생성한다. 원자의 기존노드 transition과 이 부모 재생성은 구분하고 부모 조립에서 source 근거대로 처리한다.

matching name=match-target: customers checked / orders disabled / line_items disabled / archive enabled. ArrowDown은 두 disabled를 건너뛰고 archive 선택+focus-visible로 이동했다. draft만 바뀌며 Apply 전에는 map을 바꾸지 않는다. 이 조사에서는 Apply를 누르지 않았다. Native RadioButton의 group manager는 서로 다른 객체의 selected를 관리하지만 HTML arrow/Tab 규칙을 자동 제공하는 것으로 가정하지 않는다. own candidate에서 group scoped visible/enabled 순서, wrap 및 단일 Tab stop을 구현하고 actual 양OS로 대조해야 한다.

증거: source/radio-live-{dbms,arrow-left,wrap,enter,tab,matching,skip-disabled}.json, screenshots/radio-source-matching-keyboard.png. 공식 Avalonia12.1.3 RadioButton / RadioButtonGroupManager / KeyboardNavigation 소스를 확인했다. source input의 Enter 무변경을 candidate에 적용한다. 기타 부모·backend는 수정하지 않는다.

## 후보 결정 / 다음

별도 WebRadio를 native RadioButton + own20px template + source circle/dot + LOCKED Surface/Motion public easing으로 만든다. checked group은 native manager를 사용한다. 같은 visual root/GroupName의 visible+enabled radio를 원본 순서로 방향키 순환하고 selected(없으면 첫 enabled)만 Tab stop으로 둔다. FocusVisible property는 부모 :has 대응 신호다. parent가 GroupName/label/ReducedMotion을 공급한다. 24 endpoint와 방향키/Space/Enter/Tab/disabled actual, scale/color/reverse timing을 대조하기 전 LOCK하지 않는다.

## 후보와 검증 진행 · 2026-10-06

- 구현: src/Verifier.Desktop/WebRadio.cs. 별도 RadioButton own template, Surface round box와8px dot, FocusVisible/ReducedMotion, native group manager와 방향키 wrap/disabled skip/단일 Tab stop. LOCKED Checkbox 등은 변경하지 않았다. Debug 및 win-arm64 빌드 성공.
- tools/ReplicaProbe/RadioProbe.cs와 radio.html/radio-motion.html은 원본 CSS를 보존한 격리 측정용이다. static24×양OS=48 endpoint 모두 일치(source/radio-state-comparison.json). macOS forward/reverse fractional72 rows 모두 일치(source/radio-mac-motion-comparison.json). precise premultiplied RGBA의 최종8bit paint, scale matrix1e-6, glow spread1e-5px, reverse duration1e-5ms로 대조했다.
- Windows source/native DPR2 24 glyph ROI의 최대 MAE1.6302/255. screenshots/radio-windows-comparison.png에서20px circle/8px dot 위치를 확인했다. edge AA 차이가 있으며 bit-perfect로 주장하지 않는다. macOS screenshot API는 논리 이미지이므로 같은DPR raster라고 주장하지 않는다.
- 실제 macOS: Down disabled2개 skip→3선택, Enter 유지, Down wrap→0, Tab→다음 그룹 Reduced4, Space→선택. source/radio-mac-{down,enter,wrap,tab,reduced}.json과 screenshots/radio-mac-input-window.png.
- 실제 Windows: 공식 UIA exact title+exe/nonce focus 후 공식 CLI key press/release, 새 파일 수정시각/changes 검사. Down skip0→3(count2), Down wrap3→0(count4), Enter 유지, Tab 다음 그룹4, reduced Space count5. source/radio-windows-key-down-{0,2}.json, radio-windows-key-enter-4.json, radio-windows-key-tab-4.json, radio-windows-reduced-space.json. disabled Select 거부와 count4 불변도 읽었다.
- 기존 key 파일 이름은 key+count만 포함해 target0 Space 유지 기록을 target4 reduced Space가 덮어썼다. 현재 radio-windows-key-space-4.json은 reduced 입력이다. 이를 별도 radio-windows-reduced-space.json으로 보존하고 driver 이름에 target을 추가했다. 덮어쓴 파일을 두 행동의 증거로 사용하지 않는다.
- 일부 prlctl InvalidArgument 요청은 실패했고 그 후 실제창 count4를 다시 읽어 해당 입력만 재시도했다. 창이 실행 중인 상태에서 Expand-Archive는 runtime DLL 잠금으로 실패했다. 이 묶음 준비는 성공으로 기록하지 않는다. reduced count5 성공 확인 후 exact owned 창 종료 확인, full package 준비를 별도 성공 검사로 순차 재실행한다.
- 재개: AGENTS→MASTER_RULES→STATUS→이 명세→direct 원본 CSS/JS/실행 matching draft→WebRadio→현재 차이를 실제 읽었다. 7 prior LOCK 보존 검사를 통과했다. 전체 원본을 다시 분석하거나 부모/다른 atomic을 수정하지 않았다.

현재 원본은 EN selection의 matching dialog에서 archive를 draft 선택한 상태이며 Apply는 누르지 않았다. group final state/Tab은 확인했으나 parent render와 change-focus ordering은 부모 단계에서 원본을 다시 대조한다. source에서 없는 임의 radio group을 일반화해 검증 완료로 주장하지 않는다.

## 후속 교정 / 현재 다음 행동

Windows full prepare와 timing 실행/회수 성공, forward/reverse72 rows failures0. source/radio-windows-motion-comparison.json. 실제 source derivative event 기록에서 ArrowDown은 destination focus→input→change 순서다(source/radio-source-input-events.json). 후보는 checked→focus 순서였으므로 MoveGroup을 focus→checked로 교정했다. paint/transition은 변경하지 않았다. macOS 실제 Last pointer도 count5→7의 그룹 선택을 확인(source/radio-mac-pointer.json). 교정한 후보를 양OS에서 다시 실행하여 change callback의 focused index가 destination인지 확인한 뒤 LOCK한다. probe에 changeEvents 진단만 추가했다.

### 교정 후 확인

최종 후보의 macOS Down/순환/pointer/선택된 Space유지와 Windows Down은 changeEvents의 focused index가 모두 목적지였다. radio-mac-final-{down,wrap,pointer,selected-space}.json, radio-windows-key-down-0-target-0.json. HTML 원본 이벤트는 focus→input→change이며 Avalonia는 그룹의 checked/uncheck 각각 IsCheckedChanged를 발생시킨다. 부모는 checked=true의 해당 sender를 사용하고 false 이벤트를 source input/change처럼 처리하지 않는다. 이 내부 알림 차이를 웹 UI/동작 차이로 노출하지 않는다. 그룹 선택 전 포커스 교정은 paint/tracks를 변경하지 않았으므로 기존48 endpoint/144 timing 증거는 유효하며 비교를 다시 실행해 모두 통과했다.

## 최종 판정 · Radio LOCK · 2026-10-06

- 원본 DOM의20px input만 복제: border1.5/radius50%, content-box8px dot/center10,10, padding/margin0. 카드 hit area·label·badge·font는 부모 소유다. 색상/glow/outline/scale/hover/pressed/checked/disabled/focus-visible/reduced와 두OS48 endpoint/144 fractional timing를 비교 완료했다. 사용 font/label은 이 원자 안에 없으므로 typography는 적용되지 않는다.
- 실제 원본의 DBMS4방향 순환/Tab/Enter와 matching disabled skip을 확인했다. 후보 양OS actual Down skip/wrap/Enter/Space/Tab/reduced, macOS pointer 및 최종 focus-before-change를 확인했다. Windows 마지막 selected Space는 radio-windows-key-space-4-target-0.json(count4유지)이며 reduced Space의 파일과 분리했다.
- 구조/로컬20pxbox/부모측정위치/상태 paint/interaction 비교를 완료했다. 부모 dbms/matching의 카드/DOM 재생성·적용확정은 원자 범위가 아니므로 해당 부모에서 다시 대조한다. 후보 GroupName과 순서는 부모가 실제DOM에 맞춰 공급한다. 실제source에 없는 unchecked 다중진입의 임의 그룹을 검증 범위로 일반화하지 않는다.
- macOS M4Pro와 Windows11 ARM64/DPR2 검증. Windows 동등DPR screenshot ROI 최대 MAE1.6302/255의 edge AA 차이는 남지만 glyph 크기/중심/형상은 일치한다. bit-perfect/다른DPI/x64 release 검증이라고 주장하지 않는다. 이 환경 확장은 배포검증 시 별도로 수행한다.
- paint/tracks 변경 없는 focus순서 교정 후 빌드0warning/0error, 상태/타이밍 비교 재실행 pass, 양OS actual 새binary focus검증 pass. 기존7LOCK 보호 불변. source/radio-lock.json으로 WebRadio.cs를 보호한다. 이후 부모 조립 때 이 계약을 사용한다.
- macOS 검증 창 종료 확인, Windows exact owned 창 종료도 확인했다. fixture 서버는 후속 atomic 측정을 위해 유지한다. 원본 matching archive는 draft이며 Apply하지 않았다. 다음 단일 작업은 TextInput 원본 조사다. 전체복제의 임시재개제약은 유지한다.

## LOCK 예외 · 2026-10-07 DbmsOption pointer 재포커스
수정 전 기록: 원본 실행 Connection에서 ArrowRight→PostgreSQL본문click을 직접 수행. 기존같은input에focus가유지되어도:focus-visible은true→false가된다. Native는Focus(Pointer)를이미focus된Radio에호출하면GotFocus가없어focus-visible=true가남는다(WindowsDbmsPointer2증거). 영향은Radio의새pointerfocus진입API와직접부모DbmsOption의그API호출한줄이다. 기존Radio스타일/크기/상태알고리즘변경금지. 새로운public FocusFromPointer에서pointerfocus후focus-visible=false를명시한다. 48Radioendpoint양OS회귀와실제DBMS이미focus된labelclick양OS재검증전기존LOCK유효로표시하지않는다. DbmsOption은WAITING,Radio만활성.

### 예외 재검증 / 재LOCK
새public FocusFromPointer만추가. 양OS24기존endpointJSON동일, 실제Mac/Windows이미focus된DBMS본문click후focus-visible=false 및selected유지확인. source/radio-dbms-pointer-regression.json. 양OS빌드성공;style/track/geometry불변. radio-lock.json최종hash갱신,Radio재LOCK. DbmsOption재개.
