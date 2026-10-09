# Surface

## 상태·범위

LOCKED · 2026-10-05 관찰 원본의 paint 계약 검증 완료. Typography와 독립적인 paint primitive다. 내용·텍스트·선택/이벤트·부모 배치·motion은 이번 범위가 아니다. 색/각 변 테두리 두께/모서리 radius/shadow와 fractional border-box 계약만 구현한다. 원본에 없는 효과를 추가하지 않는다.

## 원본 근거

[실행 Review computed style](../source/surface-review-live.json). 실제 CSS는 캡처 original.css와 일치했다. 현재 viewport1120×1097, DPR2, 영어 Review다. topbar/rail/sample strip/footer, 완료·선택·기본 step/step-index, panel, warn notice, checkbox, primary/quiet button에서 표면 값을 읽었다. DOM의 내용/글자 metrics는 이 primitive 검증에 포함하지 않는다.

확인값: border 0/1/1.5px, radius0/5/8/10px, 투명/불투명 배경, 전체 또는 한쪽 border, 기본·활성·완료 색. 원본 panel shadow0 2 4 rgba(0,0,0,.063), primary0 2 3 rgba(0,0,0,.14), footer0 -4 16 rgba(0,0,0,.094), active step inset3 0 0 rgb(139,148,255). 최종 computed 값이 이전 선언보다 우선한다.

## Avalonia 대응·수정 범위

기존 `MainWindow.Shell.cs::Panel`은 radius10/border1/#41434a/#2b2d31, padding24이며 shadow는 빠져 있다. 원본 panel의 모든 부모가 검증된 것은 아니므로 여기서 일괄 shadow·padding 교정을 하지 않는다. `WebSurface`는 배경·border/radius/shadow를 명시적으로 받아 WebSurfaceBox(Decorator)를 만든다. 부모는 향후 자기 원본 상태 값을 공급한다. SvgIcon LOCK 범위·Typography 후보·상위 배치는 변경하지 않는다.

## 검증 계획

실측 paint 값을 텍스트 없는 고립 비교판에 적용한다. 표시된 source의 표면 종류를 각각 대조하고 native1×/2× 렌더, 실제 border-box/각 변 두께/radius/shadow 인수를 기록한다. AA와 shadow blur는 차이를 보존하며 불일치를 통과로 감추지 않는다. 현재 미검증이므로 LOCK하지 않는다.

## 현재 차이·다음 행동

26조건의 border-box 위치·크기는 모두 정확히 일치했다. 기본 Border는 radius를 border 중간선에 적용하며 비균일 두께에서는 shadow를 그리지 않는 차이를 확인했다. [Avalonia 12.1.3 실제 렌더 코드](https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Utils/BorderRenderHelper.cs)에 complex-border branch의 shadow 누락이 있다. 따라서 CSS outer border-box 기준 그림자/배경과 outer–inner border ring을 그리는 작은 Decorator primitive로 교정한다. 부모 디자인은 바꾸지 않는다.

원본 캡처는 실제 JPEG bytes이므로 확장자는 jpg로 보관한다. JPEG 색/AA 차이를 CSS 색 값 변경 근거로 사용하지 않는다. layout 수치·원본/native paint 인수·실제 그림자 존재도 함께 대조한다. 아직 LOCK 전이다.

## 재개 기록

2026-10-02: Typography font mismatch를 미완료로 보존한 뒤 독립 Surface 하나로 활성 작업을 전환했다. 관련 원본 live CSS/computed paint와 기존 Panel을 직접 읽었다. 압축 후 AGENTS/MASTER/status/이 명세/관련 source/native/현재 차이 순서로 재개한다.

## LOCK 변경 기록

없음. 아직 LOCKED가 아니다.

## 현재 구현·중단 시점

2026-10-02: `WebSurface.cs` / `WebSurfaceBox.cs` 구현 후보를 실제 Avalonia로 렌더했다. 기본 Border 후보에서 발견한 비균일 border shadow 누락과 중간선 radius를 CSS outer border-box + outer/inner ring 방식으로 교정했다. 26조건의 위치/크기 차이는 0px이며 footer 위쪽 그림자도 실제 렌더 픽셀로 확인했다. Source JPEG/native PNG의 색 관리·AA 차이는 아직 완료 판정하지 않았다. 실제 native 창 캡처와 Windows build도 남아 있다. Surface는 아직 LOCKED가 아니고 기존 페이지 Panel에는 조립하지 않았다. 사용자 기존 앱/원본 웹은 변경하지 않았다.

증거: [source cases](../source/surface-cases.json), [web layout](../source/surface-web-layout.json), [native layout](../screenshots/surface-native-layout.json), [web](../screenshots/surface-web.jpg), [native1×](../screenshots/surface-native-1x.png), [native2×](../screenshots/surface-native-2x.png). 개발 도구 `prepare_surface.py`, Probe `--surface`에서 같은 비교를 재현한다. `--font-api`/`REPLICA_FONT`는 조사용 후보 도구이며 제품 정책이 아니다. 다음 재개는 현재 차이 검증부터이며 설계를 초기화하지 않는다.

2026-10-02 재개: AGENTS/MASTER/status/Surface 및 native 후보를 읽고, 다시 연 원본의 관련 computed paint와 CSS 일치를 확인했다. 비활성 검증판을 실제 native 창에서도 확인하는 개발용 Probe 모드를 추가한다. 원본·제품 페이지·LOCK 범위는 변경하지 않는다.

2026-10-05 재개: 중단 뒤 AGENTS/MASTER/status/명세/관련 WebSurface·Probe를 다시 읽고 LOCK 무변경을 확인했다. 원본 서버를 복구했다. 10월 2일 추가한 실제 창/NSWindow 색공간 조사 후보는 아직 검증 전이므로 현재 지점부터 확인한다. 해당 실험은 개발 Probe에만 있으며 제품 앱에 적용하지 않았다.

2026-10-05 교정 결정(구현 전): NSWindow.colorSpace=sRGB 개발 후보에서 실제 창 색상(91,101,233)이 원본 JPEG(91,101,233)와 일치했고 warn 배경도(56,51,45)로 일치했다. 기존 원본 RGBA를 바꾸지 않는다. Apple 공개 NSWindow.colorSpace/NSColorSpace.sRGB API를 Surface의 창 paint 환경 계약으로 제공하되 상위 MainWindow에는 아직 조립하지 않는다. 원본 그림자의 blur는 CSS sigma=blur/2이며 Avalonia renderer의 blur→sigma 변환과 달라 의미를 변환해야 한다. source 값은 보존하고 렌더 인수만 역변환하며 실제 캡처로 재대조한다.

2026-10-05 추가 차이(구현 전): active step의 inset3px은 웹에서는 padding edge 뒤 3 logical px이나 native 2× 창에서는 1.5px만 보인다. 기존 Skia renderer의 translated shadow 경로가 scale 영향을 받으며 inset clip도 outer edge에 놓인다. blur sigma 변환만으로는 해결되지 않는다. WebSurface shadow만 공개 ISkiaSharpApiLeaseFeature 경로에서 logical 좌표·CSS sigma·padding-edge inset clip으로 그린다. 원본 offset/blur/radius/색을 보존하고 별도 1×/2× 및 실제 창에서 대조한다. 부모/LOCK 도형은 그대로 둔다.

## 완료 판정 · 조립 계약

- 26조건의 원본/native border-box 위치·크기 차이는 0px이다. 원본 각 변 두께/outer radius/배경·테두리 RGBA는 그대로 전달한다. [대조](../source/surface-verified-comparison.json).
- CSS inset은 padding edge에서 시작한다. 원본 3px inset은 native1×의 3 physical pixels, 2×의 6 physical pixels로 확인했다. 이전 2×의 절반 폭은 교정됐다. Gaussian sigma는 CSS blur/2이며 기존 Avalonia blur 변환을 사용하지 않는다. [Chromium shadow 정의](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/core/style/shadow_data.h), [W3C shadow blur](https://www.w3.org/TR/css-backgrounds-3/#shadow-blur).
- `WebSurfaceColorSpace.Apply(window)`는 macOS 실제 NSWindow에 sRGB를 명시한다. 상위 조립은 Window.Opened의 실제 handle이 생긴 후 한 번 호출한다. 다른 OS는 no-op이다. 색 값을 화면에 맞춰 재작성하지 않는다. [Apple window colorSpace](https://developer.apple.com/documentation/appkit/nswindow/colorspace). 이 연결은 아직 제품 MainWindow에 넣지 않았다.
- Border+padding 안의 왼쪽/중앙 정렬을 원본 실측 세 조건에서 대조해 x/y/width/height 차이 0px를 확인했다. [원본 child](../source/surface-child-web.json), [native child](../screenshots/surface-child-native.json).
- 상태 변경은 부모가 정확한 paint 값을 공급한다. 기본/완료/활성·경고·primary/quiet를 포함한 관찰 13종×2조건이 검증 범위다. Surface 자체에는 텍스트/이벤트/focus/motion/hover 정책이 없으며 이들은 각 부모 명세에서 검증한다.
- 실제 창의 검증 조건: macOS arm64,720×900 client,DPR2. [창 수치](../screenshots/surface-native-window.json), [교정 창](../screenshots/surface-native-checked-window.jpg), [원본](../screenshots/surface-web-oct05.jpg). PNG1×/2×도 보존했다.
- JPEG의 해상도/색차 서브샘플링과 렌더러 AA에 따른 edge pixel 차이는 raw 수치를 숨기지 않고 [raster 기록](../source/surface-raster-comparison.json)에 보관했다. 좁은 20/24px 파란 표면에서 주변 JPEG ringing이 centre patch까지 영향을 준다. 이를 소스 RGBA/크기 변경의 근거로 쓰지 않는다. 숫자·도형·그림자 폭의 실제 불일치를 JPEG 차이로 면제하지 않았다.
- macOS publish와 Windows build는 경고0/오류0이었다. Windows 실제 UI는 해당 OS의 전체 인수에서 별도로 확인해야 한다. 이 primitive LOCK은 부모/전체 앱 일치나 Windows 실사용 완료를 뜻하지 않는다.

보호 범위는 [surface-lock.json](../source/surface-lock.json)의 네 파일이다. 이미 완료된 SvgIcon 심볼은 변경하지 않았다. 이후 원본 근거/영향/사유를 먼저 기록하지 않고 표면 값·구조를 변경하지 않는다. 다음 단일 작업은 Typography의 실제 글꼴 차이 교정이다.
