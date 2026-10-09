# SvgIcon

## 상태·범위

LOCKED · **SvgIcon primitive의 24×24 도형·크기/색/상속/회전/비표시 계약을 검증했다.** 부모 컴포넌트의 배치·설정까지 잠근 상태가 아니다. 원본은 [manifest](../source/manifest.json), 직접 관측은 [SVG computed style/geometry](../source/svg-icon-baseline.json), [원본 화면](../screenshots/baseline.jpg)과 [DOM](../screenshots/baseline-dom.txt)이다.

원본 위치: `examples/ui-prototype/index.html`의 `icons`, `icon(name,cls)`, `.svg` 및 부모별 override. 구현: `src/Verifier.Desktop/SvgIcon.cs`, `SvgIcon.Definitions.cs` 및 `MainWindow.Shell.cs::UiIcon` 호환 wrapper. 부모의 레이아웃/버튼 구현은 이번 수정 범위가 아니다.

## 원본 DOM·geometry

`<svg class="svg …" viewBox="0 0 24 24" aria-hidden="true">…</svg>`.
이름별 path뿐 아니라 circle/ellipse/rect도 포함한다. 없는 이름은 `icons.info`로 대체한다. 전체 geometry 정본은 [로드된 JS](../source/original.js)의 `icons` 한 곳이다. 기존 native 경로로 추측해 옮기지 않는다.

## 원본 CSS 핵심값

`.svg`: width/height 20px, display inline-block, vertical-align middle, fill none, stroke currentColor, stroke-width 1.7, stroke-linecap/linejoin round. 전역 border-box가 적용된다. flex 부모에서는 실제 computed display가 block이다. 그 차이를 아래 증거에 기록했다.

| 직접 부모/상태 | 이번 실행 관측 | 관련 override |
|---|---|---|
| `.brand` | 외곽 30×30, padding 5, 흰 stroke | accent 배경/radius 8, 내부 drawable 20×20 |
| `.sample-strip` | 20×20, `rgb(181,186,193)` | flex none |
| `.dbms-option` | 4개 모두 실제 rect 0×0, display none | 선언 25×25여도 숨김; selected 색도 그리면 안 됨 |
| `.role-mark` | 22×22, muted stroke | 크기 override |
| `.btn.quiet` | 18×18, muted stroke | `.btn svg`, flex none |
| `.btn.primary` | 18×18, white stroke | 부모 currentColor |

1138×1097 CSS px, DPR 2, 한국어 연결 페이지 PostgreSQL selected 상태다. 정적 관측에서 SVG 자체 animation none, transform none; 다른 상태·부모는 아직 확인하지 않았다. 선언과 실제 값은 [원본 CSS](../source/original.css)와 JSON을 함께 확인한다.

## 직접 부모에서 추가 확인할 범위

`.step-index`(15), `.category-icon`(19), `.category-toggle`(18), `.notice`(18 및 align-self center/margin-top 0), `.search-field`(18 위치 absolute), `.match-method`(14), `.db-symbol`(58 및 stroke 1.3), `.compare-figure/.result-header`(30), `.entry-hint`(15), `.result-columns`(14), `.row-chevron/.trial-result-row`(18), `.prepared-db`(green). 괄호 값은 CSS 선언 조사이며 실제 실행 크기/상태 확인 완료가 아니다. 해당 부모의 아이콘 범위만 조사한다.

## 수정 전 Avalonia 차이 · 조사 이력

`UiIcon`: Viewbox + 단일 Path, size 기본 20, 24×24 geometry, StrokeThickness 1.7, round cap/join, Center. 원본 circle/ellipse/rect와 일부 path 좌표가 다르다. 원본 `grid/warning/swap` 이름을 현재 `structure/warn/refresh`로 대체한 곳도 있다. 없는 이름을 원본 info 대신 가로 선으로 그린다. 부모에서 default muted 색을 강제하는 경로가 있다. 코드를 읽어 발견한 차이이며 실제 native 시각 검증은 미수행이다.

## 구현 전 비교·완료 체크 · 조사 이력

| 항목 | 상태 |
|---|---|
| 원본 구조·일부 부모 실제 geometry/CSS | 조사 중 |
| native 구조·크기·위치·spacing·색·border/radius | 미검증 |
| typography | SVG 자체 문자는 없음; 부모 baseline 배치 확인은 남음 |
| 상태 UI·interaction | 자체 decorative aria-hidden; 부모 상태의 색/숨김/회전/동작은 미검증 |
| 전체 직접 부모 배치 | 미검증 |

## 구현 전 차이·다음 행동 · 조사 이력

위 native geometry/fallback/이름 차이와 미관측 부모 상태를 확인한다. 원본 geometry를 충실히 옮길 대응 구조를 기록한 후 UiIcon 범위만 구현한다. 부모 디자인이나 페이지를 수정하지 않는다. 미확인 항목이 남아 있어 LOCK 금지.

## 재개 기록

2026-10-02: 새 사용자 규칙 → 기존 AGENTS/상태 확인 → 실행 원본 캡처 → UiIcon 코드 확인. 다음 재개는 MASTER_RULES → COMPONENT_STATUS → 이 명세 → 관련 icon/CSS/부모 → UiIcon → 남은 차이 순서다.

## LOCK 변경 기록

2026-10-02 최초 LOCK. 아래 최종 검증 범위와 해시를 따른다.


## 2026-10-02 · 구현 직전 결정

- 실행 웹의 style/script가 보존 기준과 동일함을 다시 확인했다. 연결/확인 완료/사전 확인/선택 접힘·펼침/시험 준비/준비 완료/실행 전/진행/결과에서 아이콘만 조사했다. 세부 관측은 `source/svg-*.json`, 대응 화면은 `screenshots/svg-*-web.jpg`다. 페이지 재설계 조사는 하지 않았다.
- 18개 원본 이름의 SVG geometry를 [icon-definitions.json](../source/icon-definitions.json)에 추출했다. path/circle/ellipse/rounded rect를 각각 유지한다. 미등록 이름은 원본 info fallback이다.
- Avalonia `SvgIcon`은 동일 24×24 viewBox를 직접 그리는 비상호작용 Control로 만든다. Shape의 tight bounds/Stretch로 좌표계를 바꾸지 않는다. 종횡비를 유지하고 가운데 정렬하는 원본 기본 SVG 동작을 따른다. stroke=1.7, fill=none, round cap/join, 부모색 상속/명시색, 크기·stroke override·표시/회전은 부모 계약이다. 자체 hover/애니메이션을 추가하지 않는다.
- 기존 UiIcon wrapper는 호출 크기/색/부모 배치를 유지하고 이 primitive를 사용하도록 교체한다. 기존 `structure/warn/down` 호환 이름만 wrapper에서 원본 grid/warning/chevron 90도 회전으로 대응한다. 대응 변경 버튼의 잘못된 refresh 사용, 브랜드 크기/패딩, 숨겨야 할 DBMS 아이콘 등 부모 차이는 여기서 수정하지 않는다.
- 구현 범위: `SvgIcon.cs`, `SvgIcon.Definitions.cs`, `MainWindow.Shell.cs::UiIcon`. 검증 도구는 해당 primitive만 비교한다. 다른 컴포넌트의 디자인·구조·backend 수정 금지.
- 재개: 규칙/상태/이 명세 → 실행 원본 코드 동일 확인 → 현재 UiIcon 차이 읽기 → 이 결정부터 구현. LOCK 아님.


### 배치 대조 중 발견·교정

153개 표시 조건의 부모-slot 실제 수치를 대조하니 15/19px 홀수 크기의 0.5px 중심 위치가 Avalonia 기본 layout rounding으로 정수 위치에 놓였다. SvgIcon에만 `UseLayoutRounding=false`를 적용해 원본 fractional 위치를 유지한다. 회전 조건은 원점을 bbox로 오인하지 않고 네 모서리 변환 후 bounding box로 비교한다. 부모 디자인을 변경하는 수정이 아니다. 교정 후 다시 검증한다.


## 최종 검증·LOCK 범위

2026-10-02. [원본 18도형](../source/icon-definitions.json)과 동일한 경로/원/타원/둥근 사각형을 유지한다. 14/15/18/19/20/22/30/58px, 58px stroke 1.3, 색/부모색 상속/90도 회전/비표시/opacity/fallback/직사각 slot의 원본 SVG 기본 종횡비를 포함해 154조건을 대조했다.

| 완료 항목 | 증거·결과 |
|---|---|
| 구조·좌표·색·stroke·round cap/join | 원본 geometry 그대로; [web atlas](../screenshots/svg-atlas-web.png)와 [Avalonia 1×](../screenshots/svg-atlas-native-1x.png), [2×](../screenshots/svg-atlas-native-2x.png) 시각 대조 |
| 크기·위치·spacing·부모 slot 배치 | 표시 153조건의 실제 x/y/width/height가 0.0001 logical px 허용 범위 안에서 일치. [대조](../source/svg-layout-comparison.json), [web 수치](../source/svg-web-layout.json), [native 수치](../screenshots/svg-native-layout.json) |
| 도형 래스터 | [154/154 대조](../source/svg-pixel-comparison.json). 양방향 stroke silhouette 2 logical px 범위 안. 브라우저 캡처와 native의 antialiasing 픽셀 자체는 동일하지 않음; geometry/layout 허용 오차로 사용하지 않음 |
| typography·border/radius | 자체 문자는 없음. 원본 grid/stop rounded rect geometry 포함. 브랜드 배경·radius·padding은 TopBar 소유 |
| 상태별 UI·interaction | decorative/Focusable=false/IsHitTestVisible=false/Raw 접근성. 자체 클릭·hover·pressed·focus 애니메이션 없음. 부모의 색/회전/비표시/opacity 입력을 위 조건으로 검증. 원본에 없는 동작 추가 없음 |
| 실제 제품 안 렌더링 | macOS 최종 빌드 [시작](../screenshots/svg-home-native.jpg), [결과](../screenshots/svg-results-native.jpg), [저장 KR](../screenshots/svg-save-native.jpg), [저장 EN](../screenshots/svg-save-en-native.jpg). 원본 한영 조사 `source/svg-home[-en]/svg-save[-en].json` |
| 빌드 | macOS arm64 publish·Windows x64 build 경고/오류 0. Windows 실제 렌더링/앱 실행은 미수행 |

검증 도구와 재현 방법은 [ReplicaProbe](../../../tools/ReplicaProbe/README.md)다. 비교 fixture는 캡처한 원본 도형을 고립시킨 검증 자료이며 원본 웹을 대체하지 않는다. 브라우저 fixture 캡처는 DPR 2여도 logical-pixel 이미지로 반환되어 native 1×와 대조했다. native 2×는 별도 확인 자료다.

**LOCK은 primitive와 명세된 wrapper 범위에만 적용한다.** 원본의 직접 부모에서 요구하는 size/색/회전/표시 값은 위 primitive 계약으로 검증했다. 기존 페이지가 잘못 전달하는 인수·배경·간격·레이아웃까지 완료 처리하지 않는다. 이후 부모 조립은 원본 값으로 이 잠긴 primitive를 사용한다.

### 부모 작업에서 남은 차이 · primitive 변경 이유가 아님

- TopBar: 원본 브랜드 외곽 30/padding5/내부20, 기존 native 외곽32/내부22. 부모 치수/배경의 작업이다.
- Home: 원본 entry-hint15/paper22, 기존 native16/24. 해당 부모 호출을 원본 치수로 조립해야 한다.
- IconButton/Notice/ActionFooter: 원본 부모 currentColor, 원본19/18 등의 override를 해당 명세에서 적용해야 한다. wrapper의 기존 muted 인수를 그대로 둔 이유는 다른 컴포넌트 동시 변경 금지다.
- TableMappingRow: 원본 swap을 refresh로 쓰는 호출이 남았다. 부모의 아이콘 선택을 교정해야 한다.
- Windows 실제 배치/배율과 전체 페이지 일치 검증은 상위 작업 및 최종 OS 인수에서 확인한다. 위 primitive LOCK을 전체 앱 완료로 전용하지 않는다.

### LOCK 보존

[svg-lock.json](../source/svg-lock.json)의 소유 파일/UiIcon 심볼 해시를 보관했다. 후속 변경 전 `tools/ReplicaProbe/verify_lock.py`로 확인한다. 변경이 반드시 필요하면 원본 근거·이유·영향을 수정 전에 여기 기록하고 재검증한다. 현재 LOCK 이후 예외 변경 없음.
