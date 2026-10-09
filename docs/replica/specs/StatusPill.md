# StatusPill

LOCKED · 2026-10-07 · NumberInput 뒤 단일 atomic. 원본은 `.pill`, `.dot`, `pill(text,kind)`, invalidateConnections의 direct textContent 갱신, `.category .pill.ok` 예외다. 잠긴 Typography를 조립한다. 부모 layout/click/판정은 아직 구현하지 않는다.

## 2026-10-07 조사 · 구현 전

규칙/상태/현재 원본 CSS/JS와 actual Connection→Preflight→Selection EN/KR DOM/computed를 읽었다. 10 LOCK 보호 범위를 확인했다. 보관 JS의 pill()에는 점이 있지만 actual 초기 DOM에 점이 없었던 것은 source invalidateConnections가 className='pill';textContent='미확인/Not checked'로 덮어쓰기 때문이다. 원본 변경으로 오인하거나 점을 임의 복구하지 않는다.

- span.pill inline-flex; align-items:center; gap6; padding3px8px; fontsize12/weight400/lineheight19.2; radius5; border0; nowrap. 실제 DPR2 height25.1953125. form-head flex item은 display:flex로 blockify되며 margin-left:auto는 부모 소유다.
- dot6×6, radius50%, currentColor, flex:none; 수직 중앙. text가 직접 anonymous flex item이다. native에서는 동일 텍스트 metrics의 잠긴 WebSystemTextBlock과 도형으로 대응한다.
- neutral: background#2b2d31/foreground#b5bac1; ok:#263d33/#6bdda3; warn:#3b342a/#f0c47c; bad:#3c2a31/#ff959d; accent 최종 override:#3b3d52/#b4bdff.
- category 안 ok는 transparent/#b5bac1(점도 같은 색). trial.ready의 ok에도 같은 override. 그 외 kind는 기본palette 유지.
- 자체 hover/pressed/disabled/focus/transition 없음. passive span이며 button 안에서도 부모 event에 참여하는 표시다. 임의 버튼·hover·애니메이션을 추가하지 않는다. CSS transition-property의 기본all은 duration0s이며 움직임을 의미하지 않는다.
- text/kind/category context/점 유무 변경은 부모 데이터에 따른 별도 속성이다. label/hint/판정·접속 invalidation 정책을 이 atomic에 넣지 않는다.

실측 정본: `source/statuspill-live-connect-en.json`, `statuspill-live-preflight-en.json`, `statuspill-live-selection-{en,ko}.json`. 원본 CSS/JS 위치는 `source/original.{css,js}`. 현재 기존 MainWindow.Shell.cs::Pill은 padding8/4, 잘못된palette, 점누락이므로 기준으로 삼지 않는다. 부모 helper 교체는 해당 상위 작업에서 한다.

## 후보 구조

`WebStatusPill` → Border(padding8/3,radius5,border0) → private PillFlex(gap6, LayoutUnit 중앙 배치) → Ellipse6(currentColor) + WebTypography.CreateCandidate(text,12,19.2,400,NoWrap). empty text는 anonymous item이 없듯 감춘다. ShowDot=false는 원본 invalidation 표시다. CategoryContext는 현재 실제 부모 override를 명시적으로 공급한다.

구현 파일: `src/Verifier.Desktop/WebStatusPill.cs`. 작업 순서: 정확한 source CSS와 pill 구조의 고립 fixture를 측정 → 후보 layout/paint 양OS 비교 → text/kind/점/context 갱신 확인 → 알려진 차이 확인 → LOCK. 아직 구현/검증 완료가 아니다.

## 다음 행동

source fixture와 실제 부모 기록을 대조하고 고립 후보를 검증한다. 다른 atomic/부모/LOCKED 코드는 수정하지 않는다. 전체 복제 재개 제약 유지.

### 첫 검증의 fixture 전제 교정

초기 detached Canvas에는 UserControl template이 붙지 않아 probe가 실패했다. Window를 붙인 즉시 동기 측정한 첫 기록도 native scale1 및 update layout 전의 이전 width였다. source DPR2와 다른 전제이므로 해당 결과를 통과로 쓰지 않는다. 실제 Desktop lifetime의 Opened/정상 layout 갱신 뒤 scale를 기록하고 대조한다. 데이터 갱신 비교에 forced invalidation을 넣어 누락을 숨기지 않는다.

### 실제 lifecycle 교정 후

Desktop lifetime/Open/UpdateLayout으로 native scale2와38회 속성 갱신을 확인했다. empty 마지막 변경도 width16/height6으로 정상 재측정된다. 초기 stale update는 fixture 순서 문제였다. 현재 height25.203125와 source25.1953125의1/128 CSS px 차이는 별도 조사 중이며 원본 일치로 숨기지 않는다.

### unitless line-height · 교정 전 근거

실제 배지는 font-size12만 지정하고 body의 unitless1.6을 상속한다. computed19.2와 실제 line box19.1953125가 다르다. 잠긴 Typography API는 explicit CSS 길이의 nearest layout-unit 규칙이라19.203125를 만든다. 배지 조립에서 이 원본 unitless 계산의 layout 결과를 명시적으로 공급할 수 있는지 관련 Blink 경로와 별도 explicit/inherited source 표본을 확인한다. Typography 잠금을 임의 해제하거나 원본을 바꾸지 않는다.

### 원본 경로 확정 · 교정 전

source/statuspill-line-height-source.json에서 같은 computed19.2라도 inherited unitless는19.1953125, explicit19.2px는19.203125로 실제 다르다. Blink ComputedLineHeightAsFixed의 HasPercent→MinimumValueForLength와 fixed→FromFloatRound 경로가 근거다(공식 computed_style.cc). StatusPill은 상속unitless1.6의 원본 layout 결과를 자체 measure에서 공급하며 잠긴 Typography의 explicit 길이 API를 수정하지 않는다. dot center도 Blink LayoutUnit 정수 나눗셈처럼 layout-unit에서 아래로 양자화한다. private content panel의 Measure/Arrange에만 적용하고 부모/다른 컴포넌트는 바꾸지 않는다. ScalingChanged 때 현재 TopLevel 배율로 재측정한다.

### 현재 검증 · 2026-10-07

Mac 교정 후 actual Desktop lifetime scale2의 static38+같은 객체 update38=76 대조 통과. source/pill height25.1953125 및 dot y9.59375가 정확히 일치한다. palette·border0/radius5/padding8·3·empty 높이12/6·점 유무·context override·focusable=false 일치. text advance의 최대차이0.0062255859375는 잠긴 Typography의1/64 CSS LayoutUnit 계약 안이다. source/statuspill-mac-comparison.json 및 tools/ReplicaProbe/compare_statuspill.py. Windows 동일 최신 코드 publish·actual source38 대조가 아직 남아 LOCK하지 않는다.

현재 서버 PTY8281(기존95324 종료), 동일bridge10.211.55.2:8768·예시 전용 source-statuspill endpoint 추가. Windows source UIA record 버튼 Invoke가 실제 성공해38원본/DPR2를 저장했다(source/windows-source-statuspill.json). 앱·계정·OS 설정 변경 없음. 다음: 성공한 최신 Win-x64 probe를 새TEMP 격리 폴더에서 실행하고 생성 JSON/PNG 회수·대조.

## 최종 판정 · 2026-10-07

macOS/Windows 실제 Desktop lifetime/DPR2에서 38개 고립 표본과 같은 객체의 38회 갱신을 각각 대조했다(양OS 각76/76). 실제 Connection/Preflight/Selection의 직접 부모에서 확인한 CSS/context를 명시적으로 공급한다. 높이·점 위치·점 유무·padding·radius·border·foreground/background·category override·empty text·일반 속성 갱신이 일치한다. 폭 차이는 Mac 최대0.0062255859375/Win0.005859375 CSS px로 잠긴 Typography의1/64 LayoutUnit 계약 안이다. 배지는 passive span이라 자체 클릭/hover/focus/animation 상태는 원본에 존재하지 않는다. 부모 상호작용은 해당 상위 컴포넌트의 책임이다.

- 원본: source/statuspill-mac-source.json, windows-source-statuspill.json, statuspill-live-*.json, statuspill-line-height-source.json.
- 구현: source/statuspill-{mac,windows}-native/statuspill-native.json/png.
- 대조: source/statuspill-{mac,windows}-comparison.json, tools/ReplicaProbe/compare_statuspill.py.
- 시각 확인: screenshots/statuspill-source-mac.png 및 screenshots/statuspill-native-comparison-overview.png. 후자는 두 native 그림을 축소해 표시한 개요이며 pixel equality 증거가 아니다. 텍스트 raster는 Typography의 OS별 font/AA 계약을 따른다.
- Windows 최신 성공 publish를 별도 사용자TEMP DBMV-StatusPill-20261007/app에 풀고 실행했다. actual native scale2/38+38, exit0 및 fresh2026-10-07T01:01:48.8455485Z 결과를 회수했다. 기존 NumberInput GUI 폴더/사용자 DB/OS 설정은 변경하지 않았다.

보호: source/statuspill-lock.json의 WebStatusPill.cs 전체 SHA256. 부모 helper 조립·전체페이지·다른DPI/OS의 증거로 범위를 확대하지 않는다. 해당 범위의 알려진 차이/미검증 없음. 다음 단일 작업은 IconButton 원본 조사다. 전체복제 재개 제약은 유지한다.

### 복구 확인

규칙→상태→현재 명세→실행웹 Selection의5개 배지→original.css/JS→WebStatusPill.cs→현재 차이를 실제 확인한 뒤 Windows의 남은 대조에서 재개했다. 요약을 원본 대신 사용하거나 다른 LOCKED 컴포넌트를 수정하지 않았다.
