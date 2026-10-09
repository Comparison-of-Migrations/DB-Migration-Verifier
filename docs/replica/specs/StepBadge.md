# StepBadge

LOCKED · 2026-10-07 · 단일 활성. 하위 SvgIcon/Typography/Surface LOCK 계약 사용.

## 원본 조사 시작

실행 중인 Selection `.step > .step-index` DOM/CSS/box를 재확인했다. span(display:grid/place-items:center)24×24/border1/radius5/padding0/font12/inherited1.6. done은check SVG15(로컬4.5/4.5), 숫자는1–7. parent가 active/done/disabled 상태와 글자 weight/색을 공급한다. source final CSS: normal border#7c838c/background투명, done background#363846/border#697185/color#d1d6ff, active background/border#5865f2/color#fff/weight650. parent active+done이면 done 규칙이 더 늦게 적용되지만 실제 renderer는 그 조합을 만들지 않는지 확인한다. source에 배지 자체 animation/transition-duration 없음; parent pressed transform/motion과 배지 paint 역할을 분리한다.

## 다음 행동

실제 UI의 남은 상태 및 숫자 text rect를 수집하고 source 고립 fixture에서 inherited disabled/active/done 및 갱신을 기록한다. 기존 native 상위 helper를 근거로 추측하지 않는다. 확인 후 작은 배지 후보만 구현하고 양 OS 대조한다. SearchInput은 LOCK이므로 수정하지 않는다.

## 재개 기록

AGENTS/MASTER_RULES/상태/SearchInput 명세와 원본/후보/차이를 복구하고 SearchInput의 남은 양OS 비교를 마쳤다. 다음 컴포넌트 source `.step/.step-index/topAndRail()`만 읽었다. StepBadge 구현은 아직 없다.


## 명세 확정과 후보

source/stepbadge-live-selection-ko.json 및 고립 actual60상태를 보존했다. normal-enabled #cdd0d6 / normal-disabled #b5bac1; active는 disabled여도 white/650; done은 disabled여도 #d1d6ff/check15. topAndRail은 index===i와 i<index가 동시에 참일 수 없어 active+done은 적용되지 않는다. 언어가 숫자를 바꾸지 않는다. 원본 숫자 child linebox19.1953125(DPR2 unitless1.6), local y2.3984375; check4.5/4.5/15. 원본 parent196×52에서 local15/14. transition-duration0s, 독립 입력·focus·pressed·hover 없음. 부모 step이 해당 동작을 소유한다.

WebStepBadge.cs는 passive UserControl24×24 → LOCKED WebSurfaceBox border1/r5 → BadgeCenter → LOCKED Typography numeric12 또는 SVG check15다. Index/State/ParentDisabled를 부모가 공급한다. center는 원본 LayoutUnit 단위로 위치를 내림한다. Typography와 Surface 등의 LOCK 파일은 변경하지 않았다. StepBadgeProbe는 parent border1+padding14/12의 실제 창에서 같은 배지의60state 갱신도 측정한다. source digit 측정용 span은 grid anonymous text item에 대응하며 원본 자체에는 넣지 않는다. 원본 웹 파일은 그대로다.

양 OS의 실제 후보 대조를 완료했다. source fixture server84184에 예시 source-stepbadge.json endpoint만 추가했다. handshake partial JSON 문제는 진단 서버의 atomic replace로 교정했다. NumberInput 전용 공식 mouse/capture 허용은 이 단위에 확대하지 않는다.


## 최종 판정 · StepBadge LOCK

source/stepbadge-{mac,windows}-comparison.json: 양OS 60state +60same-object update 모두 통과. 24×24/border1/radius5/각 상태색·weight·숫자/check·parent196×52에서15/14 배치·childlinebox/y/checkgeometry 일치. 숫자advance/x는 기존 Typography 1/64CSSpx 계약 안이다. 실제 Windows source는 unique Edge/record ID UIA Invoke의 fresh POST60/DPR2, 후보는2026-10-07T01:38:58.6168102Z 새TEMP실행으로 회수했다. Color.ToString의 White 이름을 수치rgba로 읽지 못했던 최초 비교기 오류를 수정한 뒤 통과했고 제품색은 변경하지 않았다.

screenshots/stepbadge-state-overview.png에서 원본/macOS/Windows의 normal/disabled/active/done을 직접 시각 비교했다. 원본 브라우저 캡처는 압축/1x라 raster 동일성의 수치 증거가 아니며 layout/CSS 실제수치와 잠긴 paint/font/SVG 계약을 합격 근거로 삼았다. passive badge에 별도 click/focus/animation을 추가하지 않았다(focusable/tabstop false). 부모 transform/hover/키보드/aria-current는 SidebarItem에서 검증한다.

source/stepbadge-lock.json으로 WebStepBadge.cs를 보호한다. 알려진 컴포넌트 차이 없음. 실제 renderer에서 active와done은 index===i/i<index가 양립하지 않아 동시조합 없음. 기존MainWindow의 배지를 임의 수정하지 않고 상위 Sidebar 조립에서 교체한다. 전체 복제는 미완료이며 재개 제약은 유지한다. 다음 단일활성은 SidebarItem.

## LOCK 변경 예외 · SidebarItem 조립에 필요한 inherited color

변경 전 기록. 이유: 원본 `.step .step-index{color:inherit}`의 normal 배지는 부모 button의120ms color transition 중간값을 그대로 받는다. 현재 ParentDisabled bool만으로는 endpoint만 표현해 SidebarItem 조립에서 원본 전이를 복제할 수 없다. 영향: StepBadge normal foreground 공급 계약과 SidebarItem 색상전이. 근거: 원본 original.css `.step .step-index{color:inherit}`, `button{transition:...color var(--fast) var(--ease)}` 및 actual computed step transition120ms. active.white/done.#d1d6ff는 상위 선택자 때문에 부모색보다 우선한다.

변경: normal에만 적용할 선택적 InheritedForeground 공급. 기본60상태 geometry/paint는 유지한다. 잠금을 해제해 원본 parent inline color3값 × normal/active/done 각언어의18추가 표본과 기존60을 양OS에서 재대조한 후 잠근다. SidebarItem 후보는 그동안 구현하지 않는다.

### 예외 재검증 완료

기존60+부모상속18=78state 및78same-object 갱신을 양OS에서 actual source/native로 재대조했다. 기본geometry/paint 불변 및normal은부모색3값상속,active/done은부모색3값에영향없음 확인. stepbadge-{mac,windows}-comparison.json failed0. 최종파일을새hash로 재LOCK했다. 예전LOCK hash는 stepbadge-before-inheritance-lock-evidence.json으로 증거보존하며 현재보호파일로 사용하지 않는다. 컴포넌트범위를확장한근거는위예외기록이다.
