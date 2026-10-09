# ResultCount

LOCKED · 2026-10-07. TableMappingRow20LOCK 뒤 시작한다. ResultSummaryRow의 직접 내부 `.result-count`가3번 반복되며 Typography와 mini-track/mini-bar를 포함한다. Bottom-Up을 위해 이 요소를 먼저 명세·검증·LOCK한 뒤 행을 조립한다. live resultChart()의 실제 DOM/CSS/JS, 수치·zero·maximum·normal/reduced 갱신을 다시 확인한다. 표본 DOM 생성만으로 LOCK하지 않는다. source tree의 빠진 direct child를 source 근거로 추가한다.

## 최초 착수 기록
원본 웹의 예시 결과 화면을 열어 result-count3칸의 실제 bbox/computed styles와 숫자/bar animation을 확인한다. 관련 resultChart()/animateResults()/CSS만 읽고 mini-track의 Surface/gradient 및 glyph linebox를 분해한다. 현재 native CountBar를 기준으로 원본을 추측하지 않는다. 아직 구현/검증 완료 없음. 전체복제 재개 제약은 유지한다.

원본8765서버가연결거부상태였다. 기존열린페이지는유지하고캡처본 original.html.txt의전체파일/CSS/JS해시가manifest 원본과정확히같음을확인했다. source-candidates나현재examples의W01수정본은사용하지않는다. 격리된original-live에원본바이트그대로복사하여로컬8765미리보기를복구하고실행결과부터확인한다.

## 2026-10-07 재개·원본 계약
AGENTS → MASTER_RULES → status → 이 명세 → resultChart/animateResult/CSS → 직접 실행 결과 DOM → 후보를 확인하고 기존20LOCK 보호 검사를 통과했다. 원본8765는 보존 HTML 바이트로 복구되었으며 source-candidates를 사용하지 않았다.

`.result-count`는 숫자 b + mini-track/mini-bar의 직접 반복 내부 요소다. 분류 행/공통축 계산/클릭은 부모 소유이며 별도 수치 컴포넌트를 먼저 잠근다. track/bar는 독립 사용자 상태 없는 이 요소 내부 paint/layout이다. 숫자는 system-ui26/600/line1.2/spacing-.65/tabular-nums; 실제 DPR2 행높이31.1953125px. 숫자 아래 gap9px와 높이5px track을 합한 전체45.1953125px. track #41434a/radius2/overflow hidden, bar는 red #ff959d, equal #6bdda3, unknown 135도 반복 빗금 #f0c47c 0–3px/#98753e 3–5px. 0은 kind와 무관하게 숫자 #b5bac1. 부모가 공급한 maximum에 대한 percentage width를 CSS layout unit으로 계산한다.

수치는 최종 정수 그대로이며 count-up은 없다. focused result entrance에서만 원본 WAAPI: 숫자 translateY5/opacity.6→identity/1 220ms, bar left-origin scaleX0→1 320ms, ease(.22,1,.36,1). reduced는 효과 생략; 평범한 rerender는 재생하지 않는다. SetValue/제거 시 이전 entrance를 취소하고 parent explicit PlayEntrance만 재생한다. 부모의 row click/focus와 accessible 전체 문장은 나중에 검증한다.

`resultcount-live.json` 실제12개 source 및 독립54조건(3width×3kind×6 count/max)/48 normal+reduced 실제 원본 WAAPI paused time의 새 Mac source 기록을 생성했다. native 후보 WebResultCount는 잠긴 Typography/Surface/Motion을 조립한다. 최초 빌드에서 nullable 없는 Surface.Create에 null border를 전달하여 실패했고 transparent/zero thickness로 교정했다. 기존 LOCK code는 변경하지 않았다. 현재 geometry/glyph/gradient/lifecycle 양OS 검증 전이며 LOCK하지 않는다.

bridge8768은 allowlist에 source-resultcount.json만 추가하고 기존 PID85349 확인 후 종료/재시작했다(PTY22129). 원본과 과거 fixture 보호 해시는 보존한다.

## 교정 기록 · 아직 LOCK 전
최초 actual window는 Count에 폭을 배치하기 전 Canvas가 infinity로 측정하면서 내부 track이 infinity desired size를 반환해 실패했다. Count의 intrinsic 숫자 폭으로 내부 track을 먼저 측정하도록 교정하고 동일 창의54update/48timing/10lifecycle를 재실행했다.

양OS 첫 대조는9개 거의100% 길이에서1 CSS unit 차이와1 timing 직렬화 차이를 검출했다. source의123456789/123456790×100%가 computed Length float에 저장되어100%가 되는 근거를 [Chromium Length](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/platform/geometry/length.h)에서 확인했다. native도 percentage 계산 뒤 float로 저장하는 일반 변환을 적용했다. 문자열별 보정은 없다. DOM Range의 glyph width는 shaped advance의 CSS unit enclosing ceil로 대조하며26px/600/tnum의 raw advance 차이는 레이아웃 양자화 전 값으로 보존한다. Native Typography 코드는 바꾸지 않았다.

CSS matrix()는6유효자리로 직렬화(예1.17568)하므로 timeline matrix 대조만5e−6, opacity1e−6; geometry/glyph-enclosing은1e−7을 유지한다. source native paint pair를 실제 확인했다. 위치·높이·색·135도 빗금·반복 간격·끝 모양이 일치한다. source screenshot1CSSpixel과native2x→1x의 AA/이미지압축을 bit-perfect라고 주장하지 않는다.

## 최종 판정 · ResultCount LOCK
양OS54/54 geometry·same-instance count/kind/maximum/zero 갱신,48/48 원본 WAAPI 시간 표본, actual attached10/10 mount/start/running/completed/reduced/replaced/detached/reattached/replayed 기록 대조 실패0. 최신 native 재게시 이후 전부 재실행했다. [Mac](../source/resultcount-mac-comparison.json) · [Windows](../source/resultcount-windows-comparison.json) · [Mac source](../source/resultcount-mac-source.json) · [Win source](../source/resultcount-windows-source.json) · [paint pair](../screenshots/resultcount-paint-pair.png). Windows 실제 창의 diagnostic render8/9도 수령하여 숫자/빗금/radius/clip 확인했다. 배치 폭90/137.375/190.90625, 0·1·11·20·매우 작은 percentage·9자리 숫자의 overflow/거의100% 경계를 포함한다. KO/EN에서도 이 atomic은 동일 아라비아 숫자이며 언어 레이블은 부모다.

반복 값·크기 변경과 effect는 별개다. 부모가 정상 entrance를 명시적으로 요청하면220/320ms 효과를 시작하고 ordinary SetValue에는 재생하지 않는다. Native task cancellation/dispose는 source JS의 공개 계약이 아니며 제거/새 DOM의 화면 상태 복원을 위한 native lifecycle 계약이다. 실제 원본 CSS/WAAPI와 settled/replacement 화면을 대응했고 cancellation task status를 browser status라고 주장하지 않는다. source reduced 표본은 정확한 원본 no-effect branch와 기존 Motion LOCK에 대응하며 OS 설정을 바꾸지 않았다. 이 컴포넌트는 원본에서 클릭/키보드/focus/hover/pressed/disabled를 독립 소유하지 않으므로 새 동작을 만들지 않았다. 행 전체 입력은 다음 부모 범위다.

구현: src/Verifier.Desktop/WebResultCount.cs. 보호: source/resultcount-lock.json. 기존20LOCK 모두 그대로다. mac actual fixture는 종료되었고 Windows auto fixture도 스스로 정상 종료했다. Windows11 ARM64 guest에서 win-x64 self-contained build를 실행했으며 native Windows ARM64 빌드나 실제 x64 하드웨어 검증으로 일반화하지 않는다.

현재 다음 행동: Count의 계약을 조립하여 ResultSummaryRow의 실제 live DOM/CSS/grid/input/states를 확인한다. Count 디자인/구조/크기를 임의 변경하지 않는다. 전체 replica의 임시 재개 제약은 유지한다.
