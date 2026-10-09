# CategoryHeader

LOCKED · 2026-10-07. 직접 하위 Checkbox/SvgIcon/Typography/StatusPill/Surface/Motion은 LOCKED. Field는 검증 후 LOCK됐으며 본 단위의 의존이 아니다. 원본 DOM/CSS/JS와 실행 상태 조사부터 시작한다. 기존 상위 구현으로 원본을 추측하지 않는다.

## 원본 조사와 경계
실행 원본 Selection의 category-head를 측정했다(source/categoryheader-live.json). 구조는 header div → full-width button.category-toggle(icon34/svg19 + text strong14/700/22px, small11/400/18px/margin-top1 + pill + chevron18)와 sibling label.category-choice(44×44) → checkbox20이다.
Header의 전체 button은 min-height64, padding9/16/9/58, flex gap12, border/radius0, shadow없음. 텍스트 flex1/min-width0이며 줄바꿈에 따라 높이가 커진다. 실제858폭의 icon은58/15/34/34, text104/11.5/647.234375/41, checkbox18/22/20/20, label6/10/44/44, arrow824/23/18/18. Pill은 context ok의 투명 배경이며 경고만 amber배경을 쓴다.
CategorySection 부모가 border/radius10/overflow:hidden/선택 inset shadow/본문 펼침을 소유한다. Header는 부모 selection에 따른 배경transparent↔#343640(160ms CSS 기본ease)을 반영한다. Button hover#3e414c(120ms sourceease), pressed scale.98(80ms sourceease), focus outline3px #a7b1ff/offset-3. Chevron은 expanded에 따라0↔90deg(200ms sourceease). 클릭의 원본 press160ms ease-out은 Motion LOCK계약을 사용한다. Label hit area hover 또는 input focus-visible은 즉시 #a7b1ff16/radius7. 원본 없는 hover·장식은 추가하지 않는다.
선택 checkbox와 펼침button은 독립이다. 실제 펼침은 checkbox를 바꾸지 않고, checkbox 해제는 펼침을 닫지 않는 것을 확인했다. 본문 grid/visibility/inert/announcer는 CategorySection/Selection 부모 소유다. 본 단위는 expanded/check의 사용자 동작 이벤트를 전달하고, programmatic state assignment로 이벤트를 만들지 않는다.
직접 원본은 IconButton 자식이 없는 별도 button임을 확인했다. 의존 지도 초안의 IconButton을 SvgIcon+Motion+Surface로 좁힌다. Checkbox/Typography/StatusPill과 함께 모두LOCKED이므로 원본 전용 Button/row/overlay를 조립한다. 현재 native MainWindow.ScopeViews의 기존 분류 UI는 변경하지 않는다.

## 빠른 검증 경로
독립측정은 묶되 컴포넌트 활성은 하나다. 원본 CSS/DOM의 생성 표본을 한 번 수집하고, native 한 인스턴스에서 상태·언어·폭을 순차 변경하여 실제 layout/name/paint를 기록한다. 모든 표본을 거대한 native canvas에 배치하는 대신 고정 크기 검증 창과 필요한 PNG만 사용한다. actual pointer/keyboard/transition/부모문맥은 별도로 확인하며 자동 layout 통과로 대신하지 않는다.

Field 새 binary 빌드가 아직 미검증 CategoryHeader의 존재하지 않는 PatternInterface override 때문에 막혔다. 상위/디자인 변경 없이 후보의 해당 compiler/API 이름 오류만 교정한다. CategoryHeader의 상태는 보존하며 actual 검증·LOCK을 주장하지 않는다.

## 재개 · 2026-10-07
Field18번째LOCK 후 이 단위만 활성화했다. 저장된 원본 DOM/CSS/JS·live Selection 상태와 후보를 다시 확인하여 geometry fixture부터 시작한다. compiler-only 교정 후보는 아직 검증/LOCK되지 않았다. 전체 복제 임시 복구 제약 유지.

192개 독립표본 fixture/한 인스턴스 순차 Probe를 준비했다. 첫 native 실행은 창이 열리기 전에 fixture Width/내용을 지정하지 않아 infinite measure로 실패했다. 첫 표본을 show 전에 지정하도록 검증 fixture를 교정하며 실패를 통과로 기록하지 않는다. source 탭 신규 연결도 timeout으로 실패해 기존 탭 상태 확인 후 복구한다.

첫192 측정의 높이/줄바꿈/주요정렬은 동일했다. Pill의 flex 할당폭이 원본 LayoutUnit 올림보다 최대0.006px 작아 arrow x도 같은 차이였다. 잠긴 Pill 내부를 바꾸지 않고 HeaderRow의 자식 할당폭을 원본 CSS 단위로 올림/Stretch한다. expanded arrow는 원본 getBoundingClientRect와 native transformed origin 비교의 차이였으므로 Probe를 transformed4코너 AABB로 교정한다.

원본96시간 표본과 production Track의 cubic control points·duration·reversing 규칙은 동일했다. 첫 엄격1e-6ms assertion은 browser solver의 expand reverse duration110.802243321 vs110.802202812(0.000040509ms)와 serialize된 회전행렬의 angle0.000057368degree에서만 실패했다. CSS solver/행렬 직렬화 경계를 기준으로 허용범위0.0001ms/0.0001degree(18px화살표 위치 오차0.00002px미만), rgba1채널rounding을 명시한다. duration/색/크기 값을 통과용으로 바꾸거나 잠긴 Motion을 변경하지 않는다. 실제 GUI lifecycle/키입력은 별도로 검증한다.

## 재개 · 실제 입력 검증
2026-10-07 규칙/상태/명세/직접 원본 및 후보를 재확인했고 18 LOCK 보호 검사가 통과했다. 현재 후보의192 geometry 양OS 통과·96 timing Mac 통과는 보존한다. 실제 wrapper 빈 공간/헤더 본문/held/reduced/키보드 검증을 마저 수행한다. 첫 CGEvent helper는 Swift 수치 literal/whitespace compiler 오류로 입력 전에 실패했다. fail 경로 release 보장 및 AX point PID 확인을 넣고 다시 컴파일한다. 입력으로 확인하기 전 성공을 주장하지 않는다.

## 최종 검증 · 2026-10-07

- `source/categoryheader-{mac,windows}-comparison.json`: 양OS192/192. KO/EN · 5분류+준비된trial · 300/610/858/1058폭 · selected×expanded · 동일 인스턴스 상태 교체. Width300은 atomic 줄바꿈 stress이며 모바일 화면을 만든 것이 아니다. 실제 부모 inside keyline과 icon/text/pill/checkbox/chevron transformed AABB를 포함한다.
- `source/categoryheader-{mac,windows}-timing-comparison.json`: 양OS96/96 production Track ↔ actual CSS transition seek. select/unselect/hover/press/expand 및 reversal. 색1채널 rounding, 회전0.0001degree·scale0.000001·solver reverse0.0001ms의 명시적 직렬화 경계다. DB 값 비교의 오차 정책과 관계없다.
- `source/categoryheader-input-comparison.json`, 각 `categoryheader-{mac,windows}-input/`: 실제 헤더 본문70% 클릭은 펼침만, checkbox label의 빈공간은 선택만 변경. 원본 actual Enter는 checkbox 선택을 바꾸지 않고 Space는 선택만 변경한다. native 동일. ShiftTab/Enter로 헤더 펼침, normal held .98 및 release1, click playback 활성→완료, reduced held .98 즉시/클릭 playback 없음 확인.
- Windows UIA ExpandCollapse 상태/Invoke 제공 및 Expand 두 번은 한 번만 펼침, Collapse가 닫힘을 확인(`peer-expand-idempotent.json`, `peer-collapse.json`). macOS AXExpanded의 실제 bool 갱신을 확인(`ax-expanded.json`). CUA의 버튼 tree 표현이 expanded를 표시하지 않은 것은 raw AXExpanded가 누락된 것이 아니었다. API 매핑 근거: https://docs.avaloniaui.net/api/avalonia/automation/provider/iexpandcollapseprovider . 원본도 same focused button에 Enter 후 pointer click하면 focus-visible true를 유지한다. 관례로 포커스 표시를 바꾸지 않았다.
- `screenshots/categoryheader-original-ko-300.png`, `categoryheader-native-ko-300.png`, `categoryheader-paint-pair.png`: 원본1CSSpx screenshot과 native2× render를 같은 CSS 크기로 보아 실제 줄바꿈/색/아이콘/키라인/위치를 대조했다. locked Typography/Checkbox/SvgIcon/StatusPill의 paint 계약을 재해석하지 않았다. pair는 비교용 resample이며 pixel-identical 주장 근거가 아니다.

입력 기록 도구의 bubble listener는 Button이 처리한 pointer 이벤트를 받지 않아 초기 frame만 남겼다. 첫 held 기록의 .9995는 완료 주장 근거로 쓰지 않았다. fixture를 handled event를 포함하는 tunnel listener로 교정하고 실제 양OS held .98을 다시 확인했다. Windows 첫320ms 읽기도 clickActive true의 중간 기록이어서 완료로 쓰지 않았고 이후7tick의 actual 완료/scale1을 회수했다. 제품 track/duration은 변경하지 않았다.

## 구현 및 부모 계약
`src/Verifier.Desktop/WebCategoryHeader.cs`: 한 expansion button, sibling44px checkbox hit area, explicit 원본 flex allocation과 내부 typography/icon/pill 조립, selected background/hover/pressed/chevron/clipped inset focus. 공개 상태 갱신은 사용자 이벤트를 만들지 않는다. 사용자 선택·펼침 이벤트는 독립이다. Direct 하위 잠긴 파일은 변경하지 않았다.
CategorySection은 외곽 border/radius10/inset selected mark, 본문 grid/inert/announcer를 소유한다. Header만의 LOCK으로 페이지 본문·원본과 실제 DB의 수집 기능·전체 Selection 완료를 주장하지 않는다. 원본에는 CategoryHeader disabled 호출이 없으므로 제품 후보의 비활성 fallback을 추가 시나리오로 주장하지 않는다.
현재 범위에 남은 차이 없음. 부모/페이지 조립은 TODO이며 이 단위의 확정 계약을 사용한다. 전체복제 임시 재개 제약은 유지한다.
