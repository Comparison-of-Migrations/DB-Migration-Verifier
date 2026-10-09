# Checkbox

IN PROGRESS · ActionButton/Spinner LOCK 후 다음 단일 atomic. 원본 실제 선택 화면과 해당CSS/JS 및 기존 AlignedCheckBox를 다시 확인한 뒤 구현한다. 현재 후보는 없다.

## 범위와 직접 관계

원본 `input[type=checkbox]`의20px appearance-none box와::after check/indeterminate, 상태별paint/transition/native입력/checked·indeterminate semantics를 복제한다. `.choice`/`.category-choice`/`.bulk-choice`/`.table-choice`/download-option/부모행의label·gap·44/48px target은각부모소유다. 기존AlignedCheckBox의Fluent glyph를원본으로추측하지않는다. 잠긴Primitive public계약을사용한다.

## 다음 행동

live원본에서 preflight→selection으로 이동해 실제DOM/computed/정렬과전체선택/일부선택의checked·indeterminate 값을읽는다. 관련CSS cascade/JS change·syncTableSelection과기존AlignedCheckBox/Appstyles를읽고명세를확정한다. 원본/부모를변경하거나Checkbox primitive20px를Fluent minheight44px로바꾸지않는다.

## 2026-10-06 원본 조사 / 압축 후 복구

AGENTS → MASTER_RULES → COMPONENT_STATUS → 이 명세 → 관련 original.css / original.js의 syncTableSelection 및 선택 change → 실행 웹 선택 화면 → 기존 AlignedCheckBox / App.axaml을 실제 확인했다. verify_lock.py의 기존 6 LOCK이 통과했다. 기존 Fluent 템플릿과 MinHeight44는 원본 glyph 근거가 아니며 변경하지 않고 별도 후보를 만든다.

### 원본 정본 값

- input border box 20×20, min/max width/height20, padding/margin0, inline-grid/place-content:center, flex:none, vertical-align:middle, appearance:none, border1.5, radius5. 원본 DPR2의 border1.5이며 다른 DPR은 별도 확인한다.
- 기본 #1e1f22 / border#7c838c. checked 또는 indeterminate #5865f2 / #8b94ff. disabled #2e3035 / #727985, mark opacity를 내리지 않는다.
- hover:not(disabled): border#a7b1ff + spread3 #a7b1ff20. active:not(disabled): centered scale.94.
- focus-visible outline3 #a7b1ff, offset3. focus-visible은 box-shadow:none이지만 hover의 더 높은 specificity 때문에 동시 hover에서는 glow가 우선한다. generic input:focus는 #5865f214 spread3이므로 pointer focus만 있는 상태도 fixture로 대조한다.
- background/border/shadow 120ms, transform80ms은 default CSS ease(.25,.1,.25,1). mark transform160ms은 --ease(.22,1,.36,1). reduced는 transition:none이며 앱 내부 진단 신호로 확인하고 OS 설정은 변경하지 않는다.
- ::after는 **content-box**다. universal *는 pseudo에 적용되지 않는다. checked content5×9 + right/bottom border2 → outer7×11, origin3.5/5.5, margin-top-2, white L shape rotate45/scale1. unchecked rotate45/scale0. indeterminate content9×2, border0, bg#2b2d31, margin0, transform:none / origin4.5/1.
- grid 중앙 배치상 L의 before-transform 위치6.5/3.5, mixed dash5.5/9가 예상값이다. 후보 렌더 및 실제 원본 pixel 대조 전에는 검증 완료로 간주하지 않는다.
- mixed→checked는 geometry가 즉시 바뀌고 mark pose가 rotate0→45/scale1로 transition한다. unchecked→mixed는 rotate45/scale0→0/scale1이다. geometry 변경 자체는 transition하지 않는다.

### 입력 / 직접 부모 관계

원본 checkbox는 클릭/Space로 false↔true, indeterminate는 underlying checked=false다. 실제 선택 화면 demo.customers 해제 후 bulk는 mixed이며 keyboard focus-visible, Space 후 모든3개 선택 / bulk checked로 바뀌었다. 부모의 선택 집계·검색·bulk 동기화는 Checkbox 자체가 아니라 Selection 쪽 책임이다. source JS의 change와 syncTableSelection 계약을 부모 명세에서 조립한다.

category-choice44×44 안 grid center, table-choice min44/gap12, bulk-choice min44/gap9, choice min48/gap12, download-option의 label/선택 surface는 각 부모 소유다. Atomic은20px 입력만 만들며 부모 label 전체 hit target을 독단적으로20px로 줄이지 않는다. 부모가 focus-visible 관련 :has를 재현할 수 있게 후보의 실제 focus-visible 상태를 노출한다.

### 조사 증거

source/checkbox-live-selection.json, checkbox-live-partial.json, checkbox-live-keyboard-focus.json, checkbox-live-mixed-space.json. screenshots/checkbox-source-mixed-focus.png. 마지막 Space 직후의 mark matrix는 진행 중 transition 샘플이며 최종 rotate45로 일반화하지 않는다. focused snapshot의 indeterminate property 누락은 false 증거가 아니며 실제 DOM mixed 및 partial 기록을 근거로 한다.

## 후보 / 검증 계획

별도 WebCheckbox를 native CheckBox 입력/automation + own template + LOCKED Surface / Motion public easing으로 만든다. source-derived 고립 fixture에서 unchecked/checked/mixed × hover/press/focus/disabled 조합과 reduced를 조사한다. 실제 입력과 JSON/style 대조, actual 양OS 창, 가능한 같은DPR pixel 대조 후에만 LOCK한다. 기존 LOCK 파일과 상위 화면은 수정하지 않는다. 현재 LOCK 전이며 후보 작성이 다음 행동이다.

## 후보 / 실제 대조 · 2026-10-06

WebCheckbox.cs는 atomic20px 자체 템플릿이며 label을 그리지 않는다. 자신의 local Width/Height/Min/Max20, padding/margin0, local template과 source paint로 App.axaml의 generic Button/CheckBox min44 및 Fluent ContentPresenter를 받지 않는다. 직접 App.axaml의 현재 전체 스타일을 읽었으며 App/기존 AlignedCheckBox는 수정하지 않았다. FocusVisible direct property를 부모에 제공한다. parent가 ReducedMotion을 명시적으로 공급한다.

CheckBox의 native automation/input을 유지하되 source와 다른 두 기본 동작을 교정했다. Avalonia 12.1.3 ToggleButton.cs의 null→false 기본값은 원본 mixed→true와 달라 Toggle을 override했다. Button.cs Enter는 즉시 click하므로 source checkbox에서 Enter로 선택이 바뀌지 않는 실제 증거에 맞춰 Enter를 처리한다. Space는 native press/release를 사용한다. 근거: 공식 AvaloniaUI 12.1.3 src/Avalonia.Controls/{CheckBox.cs,Button.cs,Primitives/ToggleButton.cs}. Click에 source가 없는 button press effect를 추가하지 않는다.

### 상태 / 렌더

원본 CSS를 보존한 derivative checkbox-state.css에서 pseudo hover/active/focus 선택자만 명시 진단 class로 대응했다. checked/disabled/mixed는 실제 HTML property다. 체크 상태3 × 조합12 =36 rows. 정적 fixture의 transition:none은 endpoint 대조용이며 동작 검증을 대신하지 않는다. source/checkbox-mac-source.json 및 windows-source-checkbox.json, source/checkbox-windows-native.json과 artifacts의 mac native record를 비교했다. checkbox-state-comparison.json 72rows paint/border/glow/20px box/scale/mark matrix 일치. focus+hover glow 우선, pointer focus generic glow, disabled mark 불투명도 유지까지 포함한다.

Windows official capture의 실제 browser Document rectangle/DPR2와 native RenderTargetBitmap2x를 같은 좌표로 비교했다. 36 ROI 최대 MAE1.1535/255이며 차이는 곡선/사선 edge AA다. checked12 white stroke의 bidirectional 거리 ≤1 physical pixel. screenshots/checkbox-windows-comparison.png, source/checkbox-windows-raster-comparison.json / checkbox-windows-mark-ink-comparison.json. bit-perfect라고 주장하지 않는다. mac screenshot API는 logical image이므로 같은DPR raster equality로 주장하지 않는다. mac live native창의20px bounds와 states는 별도 actual 기록이다.

### 실제 입력 증거

- 원본 mixed Space→checked / 모든3선택, Enter→checked 유지: checkbox-live-mixed-space.json / checkbox-live-enter.json.
- mac actual mixed pointer click→true, Enter→유지, Space→false; Tab이 disabled를 건너뛰고 reduced input으로 이동; reduced Space→true. checkbox-mac-{mixed-click,enter,space,reduced}.json 및 screenshots/checkbox-mac-input-window.png.
- Windows actual mixed Space→true, Enter→유지, disabled UIA Toggle 거부, 다음 reduced Space→true. checkbox-windows-key-space-{0,1}.json / checkbox-windows-key-enter-1.json / checkbox-windows-input-2.json. disabled selected 값은 전후 유지했다. 각각 정확한 own title/EXE, UIA focus와 nonce, 새 file time + changes 수로 확인한다.
- 초기 driver 분기 추가가 close branch에도 잘못 적용되어 첫 key 요청은 own 창을 닫았다. 성공으로 쓰지 않았다. 분기를 수정하고 새 창을 열어 fresh input0→1부터 다시 검증했다.
- 양OS own native 검증 창을 닫았다. source/fixture tabs는 이어 검증할 수 있게 유지한다. 사용자 기존 Typography 창은 건드리지 않았다.

### 시간 대조 / 현재 남은 단계

Checkbox의 새 private Track은 LOCKED Motion public CSS ease / SourceEase를 사용한다. LOCKED ActionButton을 추출·변경하지 않았다. own Track의 reverse shortening 및 premultiplied color, glow spread, mark angle/scale를 source CSS transition과 대조한다. checkbox-motion.html은 원본 derivative이며 transition을 정지하고 fractional currentTime을 샘플한다. check/mixed/mixed→checked/uncheck/hover/pressed6 × forward/reverse9 =108rows. mac의108paint bytes/scale/mark/glow 및 reverse durations가 통과했다. source computed alpha는8bit paint 값으로 대조하며 precise 중간 double과 직렬화한 alpha를 혼동하지 않는다. matrix tolerance1e-6, shadow spread1e-5px, reverse duration1e-5ms (관측 최대 약1.06e-6ms). candidate render precision / source serialization 차이를 숨기지 않는다.

Windows source motion recorder는 실행했다. 첫 native timing CLI가 InvalidArgument를 반환했고 result file 없음까지 확인했다. 이는 통과가 아니다. 짧은 동일 scoped command로 다시 실행 중이며 결과 JSON 대조가 끝나기 전 LOCK하지 않는다. 다음: 해당 fresh Windows 결과를 회수·108대조 → 최종 범위 명시 → LOCK → Radio로 진행. 기존 source manifest 파일 hash는 그대로였다.


## 최종 판정 / Checkbox LOCK · 2026-10-06

Windows fresh timing 재실행 exit0 / 새 JSON 회수 및108rows 대조까지 통과했다. mac108 + Windows108의 색상 paint byte, scale/mark matrix, glow spread와 역전 duration을 source와 비교했다. 비교 도구: tools/ReplicaProbe/compare_checkbox.py / compare_checkbox_motion.py. 원본 파일 해시는 manifest와 같고 기존 6LOCK이 유지됐다. Debug build 및 Windows ARM64 self-contained publish 통과.

LOCK 범위는 src/Verifier.Desktop/WebCheckbox.cs의20px input / 3상태 / 조사된 hover·press·focus·disabled 조합 / explicit reduced / 실제입력이다. 직선 및 glyph위치/center, border1.5/radius5 및 outline을 포함한다. native API와 source render AA 때문에 raster bit equality를 보장하지 않는다. 확인 환경은 macOS M4 Pro와 Windows11 ARM64/DPR2다. 다른 DPI·Windows x64는 전체 배포 검증의 별도 범위이며 통과했다고 표시하지 않는다. 단순 문자열 label은 원자 범위에 없고 parent가 제공한다. 부모 label click/hit44/48px, scope aggregation, 실제 DB/backend, 페이지 조립은 완료가 아니다.

이 파일의 이전 “남은 단계”는 당시 상태다. 현재 Checkbox는 LOCKED이며 상위는 이 계약을 사용한다. 임시 복구 제약은 전체 복제가 끝날 때까지 유지한다. 다음 단일 active는 Radio다.

## LOCK 변경 사전 기록 · DownloadOption 포인터 포커스 · 2026-10-08
변경 이유: 원본 DownloadOption isolation35 실제 본문 click→Enter→Space→본문 click에서 마지막 focusVisible=false를 확인했다. 같은 순서 native DownloadOption 창은 true가 남았다. 이미 집중된 Checkbox에 Focus(Pointer)만 호출하면 GotFocus가 다시 발생하지 않기 때문이다. 영향: Checkbox에 부모용 FocusFromPointer API를 추가하고 활성 DownloadOption의 해당 한 줄을 교체한다. 기존 스타일·20px 크기·track·키보드·직접 체크박스 입력 로직은 변경하지 않는다. Radio의 앞서 검증된 같은 부모 진입 계약과 동일하되 원본35의 실제 입력을 직접 근거로 삼는다. DownloadOption은 WAITING이며 이 교정 동안 Checkbox만 활성이다. 양OS endpoint 회귀 및 같은 입력 순서 대조 후 재LOCK한다.

### 부모 포인터 API 재LOCK · 2026-10-08
추가한 FocusFromPointer 외의 코드 범위는 변경하지 않았다. macOS/Windows 각각 기존36 endpoint를 새 binary로 다시 수집하고 compare_checkbox.py의72상태 대조가 통과했다. DownloadOption actual click→Enter→Space→본문click에서 두 OS 모두 changes3/selected=true/focused=true/focusVisible=false로 원본35와 일치한다. 증거: source/downloadoption-{mac,windows}-input/pointer-after-key-final.json, 각 enter/space 기록. Checkbox 재LOCK, DownloadOption 재개.
