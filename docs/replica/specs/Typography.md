# Typography

## 상태·범위

LOCKED · 검증된 plain-text primitive. macOS/Windows의 원본 글꼴·행간·자간·배치·줄바꿈과 동적 상태 계약을 보존한다. 현재 단계와 다음 행동은 문서 끝의 **현재 재개 지점**이 정본이다. 아래 날짜별 내용은 조사 당시 기록이며 최신 상태로 해석하지 않는다. SvgIcon/Surface는 LOCKED이며 변경하지 않는다. 직접 부모는 텍스트를 쓰는 atomic/component/section이며 텍스트의 글꼴·크기·굵기·행간·자간·줄바꿈 계약이 범위다. 페이지 구조/간격/버튼 스타일을 동시에 바꾸지 않는다.

## 원본 위치

실행 웹 `http://127.0.0.1:8765/`, `examples/ui-prototype/index.html`의 body/h1–h3/p/strong/code 및 역할별 최종 CSS. [버전](../source/manifest.json). 작업 직전 실제 DOM/computed style을 읽으며 이전 token 표를 실측 대신 사용하지 않는다.

## 기존 Avalonia 위치

`src/Verifier.Desktop/MainWindow.cs::Text`, `MainWindow.Shell.cs::Heading`, `App.axaml`의 font/text 속성. 기존 helper는 12 이하 line-height18 / 14 이하22 / 나머지 size×1.3으로 근사한다. 이는 미검증 부모 코드다. 현재 후보는 WebTypography.CreateCandidate / WebSystemTextBlock / MacSystemTextLayout이며 explicit CSS metrics로 검증한다. 전체 역할 범위는 아직 미완료다.

## 조사·검증·차이

초기 TextBlock 후보는 한영 Home 역할16조건의 box 위치·폭·행 높이가 browser CSS quantization 차이0.003125px 이내였지만 glyph 폭·굵기·줄바꿈이 불일치했다. 현재 CoreText 후보/남은 차이는 아래 현재 재개 지점을 따른다. [대조 수치](../source/typography-comparison.json), [web](../screenshots/typography-web.png), [native 후보](../screenshots/typography-native.png), [native glyph/line](../screenshots/typography-native-layout.json), [font diagnostics](../source/typography-native-fonts-detailed.json). TextBlock 영역 크기를 글자 일치 근거로 오인하지 않는다.

`.AppleSystemUIFont`의 실제 shaped Latin run은 Helvetica로 바뀌며 일부 한글 Bold 요청은 Apple SD Gothic Neo 400으로 렌더됐다. 이름 목록에 SF Pro Text가 있어도 실제 typeface는 Helvetica로 돌아간다. `.SF NS, Apple SD Gothic Neo`와 `System Font, Apple SD Gothic Neo`도 원본 폭/굵기를 맞추지 못했으므로 채택하지 않았다. 원본 CSS나 글자 크기/자간을 바꾸어 폭을 억지로 맞추지 않는다.

관련 upstream 자료는 [macOS system font issue](https://github.com/AvaloniaUI/Avalonia/issues/21565), [Avalonia custom font/variable font 제한](https://docs.avaloniaui.net/docs/styling/custom-fonts). 이는 현상 진단 참고이며 이 앱의 검증을 대체하지 않는다. 현재 OS SFNS.ttf의 variable axes(wdth/opsz/GRAD/wght)도 확인했다. 대체 글꼴 설치·원본 변경·미검증 폰트 재배포는 하지 않았다.

후보 WebTypography/Window font 연결은 아직 WIP이고 실제 배포 앱에 publish하지 않았다. 기존 사용자 앱도 변경하지 않았다. Typography에 의존하는 부모는 LOCK 전까지 착수하지 않는다. Surface는 별도 검증을 완료하고 LOCK했다. 현재 Typography만 활성이다.

## 다음 행동

문서 끝의 **현재 재개 지점**을 따른다. 과거 조사 순서를 다시 시작하지 않는다. 값 근사·새 폰트로 원본 재해석 금지. macOS/Windows 실제 UI 조건을 구분해 기록한다.

## 재개 기록

2026-10-02: SvgIcon 검증·LOCK 완료 후 Typography를 단일 활성 컴포넌트로 전환. 압축 후 규칙 → 상태 → 이 명세 → 관련 원본 텍스트 → 기존 helper/폰트 → 차이 순서로 재개한다.

2026-10-02 재개: AGENTS → MASTER_RULES → status → 이 명세 → 원본 Home 텍스트 코드/fixture → WebTypography/Text/App resource → 실제 glyph 차이를 확인했다. 한영 fixture를 캡처·대조한 결과 font 이름만으로 해결되지 않아 미완료로 보존했다. Surface는 Typography의 부모가 아닌 독립 primitive라 다음 단일 작업으로 진행한다.

## LOCK 변경 기록

최초 LOCK: 2026-10-06. 보호 범위와 계약은 문서 끝의 최종 판정을 따른다. 이후 예외 변경은 이유·영향·원본 근거를 먼저 이 절에 기록한다.


## 원본 조사·구현 직전 결정

한영 Home의 29개 텍스트 computed style/bounds를 각각 [KR](../source/typography-home-kr.json), [EN](../source/typography-home-en.json)에 보관했다. 원본 `system-ui,-apple-system,system-ui,Segoe UI,sans-serif`와 달리 [기존 Avalonia 해석](../source/typography-native-fonts.json)은 Helvetica다. macOS `.AppleSystemUIFont`는 실제 System Font로 해석됐다. Windows는 원본 fallback의 Segoe UI를 사용하며 실제 OS 대조는 최종 인수에서 확인한다.

원본 Home title36/700/line48.6/spacing-1.26, subtitle14/400/25.2/0, help15/400/28.5/0, hint12/400/19.2/0, step title14/700/22.4/0, description13/400/24.05/0, paper title13/600/22.1/spacing-0.195, brand14/650/22.4/spacing-0.35. 부모별 값이므로 크기로 행간을 추정하지 않는다.

`WebTypography.Create`는 font size/line-height/weight/letter-spacing을 명시적으로 받는 텍스트 primitive를 만든다. fractional layout을 유지하고 OS 시스템 글꼴을 대응한다. 기존 Text helper의 부모들이 맡기는 근사 line-height/개별 색/크기는 당장 전체 페이지 재설계로 바꾸지 않는다. 현재 helper를 primitive에 연결해 글꼴의 기본 차이를 교정하고 향후 각 부모에서 정확한 metrics를 전달한다. Window의 상속 font family도 이 동일 primitive 시스템 글꼴로 연결한다. 화면 위치·padding·버튼 스타일은 변경하지 않는다.

수정 범위: `WebTypography.cs`, `MainWindow.cs::Text`, `App.axaml`의 Window font family 및 App.Initialize resource 연결. 원본 값의 독립 한영 fixture로 글꼴·줄바꿈·행간·자간을 대조한 뒤에만 LOCK한다. SvgIcon LOCK 영역은 변경하지 않는다.

2026-10-05 재개: 규칙/상태/이 명세/관련 원본 16조건 및 WebTypography helper를 다시 확인했다. 현재 live Home의 font/size/weight/line-height/spacing은 보존 기준과 동일하다. 이름 바꾸기만으로 해결되지 않은 차이를 OS CoreText 시스템 font/weight의 직접 측정과 대조한다. 조사용 측정부터 진행하며 제품 렌더러 교체나 대체 폰트 도입은 아직 결정하지 않는다.

## 2026-10-05 CoreText 조사 후보 · 아직 LOCK 아님

Chromium의 실제 macOS `system-ui` 선택과 같은 public CoreText API로 시스템 UI font를 만들고 CSS weight variation을 적용했다. [Chromium font matcher](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/platform/fonts/mac/font_matcher_mac.mm)의 UIFont/weight 처리 근거를 사용했다. 개발 전용 `tools/ReplicaProbe/CoreTextMeasure.cs`이며 제품 렌더러는 아직 교체하지 않았다. 시스템 폰트 파일을 설치·수정·배포하지 않는다.

[직접 측정](../source/typography-coretext.json)과 [폭 비교](../source/typography-coretext-comparison.json): 한영 16조건의 줄바꿈 일치. 대부분 trimmed line width 차이 0.008px 이내; EN 제목 0.1074px, EN 단계 제목 0.0279px, 논문 제목 0.0196px 차이는 남아 있다. 브라우저 shaping/양자화 차이 여부는 미확인이며 임의 폰트 크기/자간 보정으로 덮지 않는다.

CoreText 실제 2× raster [native](../screenshots/typography-coretext-native-2x.png), [1× 비교용](../screenshots/typography-coretext-native-1x.png), [현재 원본](../screenshots/typography-web-oct05.jpg). native raster는 OS API 조사 결과이며 Avalonia control/실제 창 검증을 대신하지 않는다. baseline/ink bounds, 실제 control 적용, Windows 실제 UI는 미완료다.

재개: AGENTS/MASTER_RULES/status/이 명세/원본 fixture/WebTypography와 조사 코드/현재 차이를 다시 읽었다. 다음은 postscript 이름으로 같은 face를 Avalonia에서 선택할 수 있는지 조사하고, CoreText glyph 위치를 원본과 비교한다. 폰트 이름으로 해결되지 않으면 public OS API 대응을 검토하되 해당 컴포넌트에 한정한다. 부모/UI 구조 변경 금지.

Product Design index/url-to-code를 확인했다. frontend 웹 신규 복제·모바일·Sites workflow는 이 native desktop 작업에 적용하지 않는다(사용자 지정 C#/Avalonia·desktop·기존 프로젝트·단일 컴포넌트 규칙 우선). 원본 근거 우선과 같은 조건의 비교 원칙을 적용한다.

### 다음 구현의 한정 범위

postscript `.SFNS-Regular`/`.SFNS-Bold`도 Avalonia에서 Helvetica로 돌아갔다([조사](../source/typography-postscript-aliases.json)). CoreText raster와 원본의 ink bounds는 threshold 대조에서 0–1 logical px 차이([결과](../source/typography-coretext-raster-comparison.json), [같은 크기 비교](../screenshots/typography-web-coretext-comparison.png)). 이는 JPEG/AA가 포함된 조사이며 일치 완료를 뜻하지 않는다.

이에 `WebTypography.Create`의 macOS plain text만 public CoreText layout/raster로 대응한 후보를 만든다. Windows는 기존 native TextBlock을 사용한다. CSS size/weight/spacing/line-height/색을 그대로 전달하고 최대폭에 따른 줄바꿈·center/right 정렬·텍스트 변경을 처리한다. 한정된 plain-text primitive이며 편집기/inline/상위 페이지를 동시에 변경하지 않는다. OS font redistributing/새 패키지/브라우저 임베딩 없이 구현한다. 실제 Avalonia bitmap/창 검증과 작은 남은 차이 조사 전 LOCK하지 않는다.

### 실제 Avalonia 후보 적용 중

`WebTypography.CreateCandidate` → `WebSystemTextBlock` → `MacSystemTextLayout`로 고립된 후보를 구현했다. TextBlock.Render가 sealed이므로 별도 Decorator 기반 plain-text primitive를 사용하며 기존 Create/부모의 TextBlock 계약은 변경하지 않았다. CoreText는 public OS API로 numeric weight·fallback·line break를 계산하고 transparent RGBA를 Avalonia에 전달한다. Windows/명시적 비시스템 font는 child TextBlock에 위임한다. 픽셀 source rect와 logical destination rect를 구분하여 2× 중복 확대 오류를 교정했고, allocation ceil 때문에 CSS bounds로 래스터를 늘이지 않도록 처리했다. 후보는 아직 WIP이며 별도 actual window/동적 속성 검증 전이다.

검증 실행: Probe `--text-candidate`의 [actual layout](../screenshots/typography-candidate-layout.json)과 actual Avalonia 1×/2× raster를 생성했다. 이전 CoreText 독립 bitmap을 Avalonia 렌더링 완료로 혼동하지 않는다. 다음: 현재 코드로 별도 Typography Check 창 검증, 텍스트/폭/색/정렬 변경 검증, 관련 원본 text 역할 누락 확인. 본 앱 publish/상위 페이지 수정 없음.

### 2026-10-05 실제 창·동적 속성 검증 결과

actual macOS window client1000×1040 / RenderScaling2에서 16조건을 확인했다. Text automation peer는 실제 한영 내용을 Text로 노출한다. [실제 창](../screenshots/typography-candidate-window.jpg), [동일 크기 비교](../screenshots/typography-web-avalonia-window-comparison.png), [캡처 환경](../source/typography-window-capture.json). 최초 캡처가 710×758로 축소되어 있던 것을 발견하고 최신 2000×2136 캡처로 교체했다. 캡처 배율을 RenderScaling만으로 추정하지 않는다.

동일 control의 language/width/size/weight/spacing/color/alignment/empty 변경10조건 [원본 측정](../source/typography-state-web.json), [native](../screenshots/typography-state-native.json), [대조](../source/typography-state-comparison.json), [비교 화면](../screenshots/typography-state-web-native-comparison.png). 모두 line count/width box가 맞으며 높이 차이는 CSS quantization 최대0.003125px, glyph line width 차이는 최대0.01951px다. detached probe는 LayoutManager가 없어 root measure를 명시적으로 갱신했고, 속성 변경 후 child IsMeasureValid=false를 별도로 기록했다. 이는 실제 부모 자동 배치를 대신하지 않으므로 실제 창 N/R 입력으로 추가 확인했다.

실제 창 N은 source state oracle의 parent row220/left/width240 조건에서 KR help를 표시: [native narrow](../source/typography-live-narrow.json) height114, line4로 원본과 일치. R은 brand width480/height22.4/line1로 복원([결과](../source/typography-live-restored.json)). [변경 캡처](../screenshots/typography-candidate-live-narrow.jpg). 130px atlas parent를 그대로 쓴 첫 시도는 available height110 및 Stretch의 명시 폭 중앙 배치 때문에 원본 state fixture와 부모 조건이 달랐다. 검증 부모를 원본 조건으로 교정했으며 제품 UI의 위치를 고친 것으로 주장하지 않는다.

macOS publish와 최신 Windows RID build가 경고/오류0으로 끝났다. Windows 실제 UI는 실행하지 않았다. 본 앱 publish/상위 조립은 하지 않았다.

잔여: [독립 Canvas 진단](../source/typography-canvas-metrics.json) EN title width432.71698, CoreText432.61137(약0.1056px 차이); EN step162.23047 vs162.20654(약0.0239px). Range quantization만으로 설명되지 않는다. 원본 CSS를 수정하거나 metric 보정을 넣지 않는다. shaper 차이 원인을 조사한 뒤 실제 나머지 font roles(ui-monospace, tabular numbers,500 weight 등)로 범위를 확장한다. System-only 16조건 성공을 Typography 전체 LOCK으로 승격하지 않는다.

### 잔여 차이 조사 후 확인한 교정 근거

HarfBuzz에 실제 OS SFNS font의 wght/opsz와 point size(ptem)를 전달하니 EN title432.71684 / step162.23047로 Canvas432.71698 /162.23047과 일치했다([조사](../source/typography-harfbuzz-metrics.json)). ptem 없이 계산하면 SF tracking이 적용되지 않아 폭이 크게 달라졌다. 이는 같은 font에서도 shaping 경로가 중요하다는 근거다. 아직 제품 candidate는 CoreText shaping이며 HarfBuzz 대응은 **미적용**이다. 임의 숫자 보정을 넣지 않는다.

기준선도 [Chromium FontMetrics](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/platform/fonts/font_metrics.cc)와 [SimpleFontData](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/platform/fonts/simple_font_data.cc)의 정상 크기 ascent/descent 반올림을 확인했다. 원본 Canvas에서14px은14/3,36px은35/8이며 native CT 측정13.535/2.953,34.805/7.594와 같은 반올림 결과다. 이 근거로 round한 baseline 후보를 별도로 생성했다. 그러나 [실제 baseline-marker 측정](../source/typography-baseline-web.json)과 [Range line box](../source/typography-range-line-boxes.json)가 예상 좌표를 그대로 확인해 주지 않았다. 자료만으로 적용을 확정하지 않고 기존 float baseline을 유지했다. round 후보는 screenshots/typography-rounded-investigation에 보관하며 source 위치/renderer snapping 검증 후 판단한다. 아직 baseline 일치 완료를 주장하지 않는다.

### 2026-10-06 재개 · OS font tables 검증

AGENTS → MASTER_RULES → 상태 → 이 명세 → 관련 fixture/실행 Home typography → 후보/조사 코드 → 남은 폭 차이를 다시 확인했다. SvgIcon/Surface 잠금 검사 통과. 실제 Home system font/size/weight/line-height/spacing은 기존 원본과 같다.

`--coretext-tables`는 선택된 CTFont의 public CTFontCopyTable → HarfBuzz Face callback과 actual variation/point size를 사용한다. 고정 파일 경로 없이 한영16조건의 모든 CoreText fallback run을 측정했고 missing glyph0이다([측정](../source/typography-coretext-tables.json)). EN title432.71684, step162.23047로 이전 파일 조사/Canvas와 같다. 논문 제목 첫 줄439.85019로 원본 Range439.85156에 가까워졌으며 한글 fallback도 유지된다. 중간 font-run 경계의 공백을 지우지 않고 line 끝 공백만 제외한다.

다음 후보 변경 범위는 MacSystemTextLayout과 직접 내부 glyph shaping helper뿐이다. OS가 정한 fallback font와 CT line break를 유지하며 각 run의 glyph id/advance/offset을 HarfBuzz로 계산하고 public CTFontDrawGlyphs로 그린다. CSS 수치/원본/부모/LOCKED primitive는 변경하지 않는다. 이는 **검증 전 후보**이며 같은16조건·동적10조건·실제 창을 다시 비교해야 한다. line-break 경계에서 shaper 폭 차이가 영향을 주는 조건, baseline/snapping, 나머지 font roles는 아직 미완료다. 후보에 미검증 맞춤 수치 보정은 넣지 않는다.

font-table callback은 HarfBuzz가 넘겨받는 reference와 managed Blob reference를 분리해 해제하며 CFData는 duplicate copy 직후 해제한다. callback 오류는 native 경계를 넘어 throw하지 않고 shaping 직후 확인해 실패시킨다. 글꼴 정보 누락을 정상 렌더 완료로 숨기지 않는다. 근거: [HarfBuzz table face](https://harfbuzz.github.io/harfbuzz-hb-face.html#hb-face-create-for-tables), [Apple CTFontCopyTable](https://developer.apple.com/documentation/coretext/ctfontcopytable(_:_:_:)), [HarfBuzzSharp callback](https://raw.githubusercontent.com/mono/SkiaSharp/v3.119.4/binding/HarfBuzzSharp/DelegateProxies.cs).


### HarfBuzz 후보 적용·재검증 결과

`MacSystemGlyphShaper`는 CTLine의 fallback font/run 순서를 사용하고 public OS font tables/variation + HarfBuzz point size로 glyph/position을 만든다. `MacSystemTextLayout` 후보는 public CTFontDrawGlyphs로 그린다. callback의 returned native reference와 managed Blob 수명을 분리하고 callback 오류는 shaping 후 전달한다. 폰트 파일 경로·새 패키지·CSS 숫자 보정은 없다.

[현재 원본16조건](../source/typography-web-oct06.json) / [실제 후보16조건 및 동적10조건 비교](../source/typography-harfbuzz-comparison.json): line count 모두 일치, box/height 차이 최대0.003125px, 줄별 폭 차이 최대0.0074463px(CSS Range 양자화 포함). 기존 EN title의 약0.1056px shaper 차이를 해소했다. 이 수치가 pixel/기준선 일치 완료를 뜻하지 않는다.

macOS actual Typography Check 창에서 N/R 변경/복원을 다시 확인했다: [narrow](../source/typography-harfbuzz-live-narrow.json) width240/height114/4lines, [restored](../source/typography-harfbuzz-live-restored.json) width480/height22.4/1line. [실제 창 원본 비교](../screenshots/typography-harfbuzz-candidate/typography-web-native-window.png). native2000×2136, client2000×2080이고 titlebar56px를 제외한 후1× 비교했다. 원본 full-page1280×1040을 캡처했다(첫 viewport720px 캡처는 전체 증거로 교체). OS 둥근 창의 바깥 흰 코너는 glyph 비교 ROI에서 제외했다. [ink bounds 조사](../source/typography-harfbuzz-raster-comparison.json)는 AA/JPEG 포함 최대2px이며 exact pixel 일치를 주장하지 않는다. 후보 macOS publish/Windows RID build 경고·오류0. Windows actual UI는 미확인. 검증 앱은 종료 확인, 본 앱은 배포하지 않았다.

[실행 browser 환경/marker](../source/typography-environment-web.json): Chrome154 UA, DPR2, zoom1. Baseline marker 재측정도 기존 좌표와 같다. float baseline은 유지했으며 정상 크기 ascent/descent 반올림만 적용해서 원본이 맞는다고 주장하지 않는다. 실제 baseline/paint snapping 관계는 잔여 확인이다.


### Result font-role 추가 측정 및 후보 확장 결정

현재 실행 결과 화면에서33개 distinct font/text 역할을 분리했다([원본 cases](../source/typography-role-cases.json), [독립 fixture](../source/typography-role-reference.html), [Range 측정](../source/typography-role-web.json)). CTFontUIFontUserFixedPitch(1)는 이 OS에서Menlo-Regular로 해석됐고 source ui-monospace와 폭이 맞는다. public CTFont feature settings + HarfBuzz tnum=1은 원본 숫자 폭 설정과 맞는다. [33역할 조사 비교](../source/typography-role-coretext-comparison.json) 최대0.007584px, missing glyph0. Canvas는 CSS tnum을 적용하지 않는 별도 normal digit 폭 진단이며 Range가 tnum oracle이다. Canvas context의 normal 자간 초기화 누락을 발견·교정했고 독립 fixture만 다시 측정했다.

이 근거로 후보 Typography의 mono family/TabularNumbers 속성을 추가한다. macOS는 public fixed-pitch UIFont, CT feature settings 및 HB tnum을 사용하고 Windows child TextBlock은 Consolas/FontFeatures tnum으로 대응한다(Windows 실제 화면은 미확인). factory의 추가 인수는 기본값을 두고 기존 parent/Create 계약은 유지한다. 한글 fallback 및 기존16조건·동적10조건은 보존한다. 우선33역할 후보 폭/행간/래스터를 검증하며 조사 결과만으로 LOCK하지 않는다.


### mono/tabular 후보 구현 및 검증 완료 범위

`WebTypography.CreateCandidate`에 monospaced/TabularNumbers 기본 false 인수를 추가하고 `WebSystemTextBlock`에 상태·measure/raster invalidation을 연결했다. macOS는 public fixed-pitch UIFont + CT feature settings/HarfBuzz tnum, Windows child TextBlock은 Consolas/FontFeatures tnum 경로다. Windows 실제 렌더링은 실행하지 않았다. 기존 Create/부모 계약은 유지한다.

[실제 후보33조건](../source/typography-role-candidate-comparison.json)은 모든 줄 수가 일치하고 Range 대비 폭 차이 최대0.007584px/높이 차이 최대0.003125px다. [동일 control 추가10상태](../source/typography-role-state-comparison.json)는 system↔mono·tnum on/off·size/weight/text 변경·empty에서 모두 measure invalidation이 발생했고, 최대 폭 차이0.007264px/높이 차이0.003125px다. [기존16조건 및10상태 최종 회귀](../source/typography-default-regression-comparison.json)는 확장 전 layout/line-width JSON과 완전히 같았다. 합계49텍스트조건·20상태변경이며 전체 Typography 역할 완료를 뜻하지 않는다.

macOS actual Typography Check에 결과 역할16조건을 넣고 실제 창에서 system44/650/tnum 숫자 → mono11/400 ID(N) → 기존 숫자(R)의 family/size/feature 변경과 복원을 확인했다. [원본 전환 oracle](../source/typography-role-transition-web.json), [native N](../source/typography-role-live-narrow.json), [native R](../source/typography-role-live-restored.json), [같은 크기 창 비교](../screenshots/typography-role-candidate/typography-web-native-window.png). native2000×2136/client2000×2080/scale2, source는 첫1000×1040 atlas 영역으로 대조했다. 실제 창의 텍스트 접근성 노출도 확인했다. 검증 앱은 종료했다. 최신 macOS publish 및 Windows RID build는 경고/오류0이고 Windows 실제 UI는 미확인이다.

폭이 같다는 이유만으로 mono face 일치를 주장하지 않았다. [별도 원본 face fixture](../source/typography-mono-face-reference.html)에서 같은 size11/weight400/LH17.6/tnum/color 및 JPEG8px block 정렬 조건으로 비교했다. 이 브라우저의 원본 ui-monospace와 명시적 Menlo 및 generic monospace는 [캡처 표본 pixel 차이0](../source/typography-mono-face-comparison.json)이었다. SFMono-Regular/.SFNSMono-Regular 별칭은 다른 모양/폭으로 나왔다. 이는 이 환경의 해당 표본에서 Menlo 선택을 확인하는 근거다. native와 source의 작은 글자 기준선·raster 차이가 해소됐다는 뜻은 아니다.

### 2026-10-06 압축 후 재개 확인

AGENTS/MASTER_RULES/COMPONENT_STATUS/이 명세, 관련 실제 결과 텍스트 computed style, 원본 CSS 및 현재 후보의 font-family/feature/shaping/raster 코드를 다시 확인했다. SvgIcon/Surface 잠금 검사는 통과했다. 기억에 의존해 다시 설계하지 않았고 단일 Typography 작업만 유지했다. 기존16역할·10상태 회귀는 출력 폴더 준비 후 다시 실행해 통과했다. 원본 CSS/JS와 잠긴 primitive는 보존한다.

### 연속 진행 · 기준선 조사

사용자의 단위별 중단 금지 지시를 AGENTS/MASTER_RULES에 기록했다. 현재Typography 단일 작업을 유지하며 완료 뒤 다음 의존 작업으로 계속한다. UI/UX Pro Max의 targeted Avalonia 검색은 기준선 문제에 해당하는 직접 지침이 없었으므로 결과를 교정 근거로 적용하지 않는다. 원본 실측·Chromium 공식 코드와 public OS 측정만 사용한다.

49역할 독립 marker fixture에서 기기 배율2의 rounded ascent/descent 및 floor half-leading 가설을 비교했다. CTLine run metrics는 fallback font의 값일 수 있으므로 primary metrics를 대신할 수 없음을 확인했다(한글13/700 조건). 다음 조사 변경은 개발 CoreTextMeasure에 primary CTFontGetAscent/Descent를 별도로 기록하는 것뿐이다. 실제 renderer는 아직 float baseline을 유지한다. 원본/부모/잠긴 범위 변경 없음.

### 기준선 교정 근거 및 한정된 구현 결정

[primary font 측정](../source/typography-primary49-coretext.json)을 별도로 기록했다. 이전13/700 한글 조건의 불일치는 CTLine fallback metrics를 primary font 값으로 오인한 조사 계산에서 생겼다. 실제 후보는 primary CTFontGetAscent/Descent를 사용하므로 face 교체가 필요 없다.

[원본49 marker](../source/typography-baseline49-web.json) 및 source CSS zoom0.5/1/2의39조건에서 `round(primary ascent × effectiveScale)/effectiveScale`, descent도 동일하게 반올림하고 `floor((lineHeight - ascent - descent) × effectiveScale / 2)/effectiveScale`의 half-leading을 더하면 baseline·Range height·leading이 **모두 정확히 일치**한다([대조](../source/typography-baseline-primary-prediction.json)). Chromium의 normal font metric rounding 및 CalculateLeadingSpace floor 동작과 연결되는 실측 근거다. [font metrics](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/platform/fonts/font_metrics.cc), [leading](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/core/layout/inline/line_utils.cc). 소스 복사 없이 해당 수학 연산만 사용한다.

다음 변경은 MacSystemTextLayout.GetBaseline(scale)과 직접 WebSystemTextBlock 진단 연결이다. raster의 실제 RenderScaling을 사용하며 size/spacing/line height·parent 구조는 그대로 둔다. 기존 float baseline을 이 실측된 장치 배율 계산으로 교체하고49/20 및 actual window를 다시 비교한다. fractional parent 위치·multiline paint phase는 별도 확인해야 하므로 아직 LOCK하지 않는다.

### 줄바꿈 경계 조사 결과와 다음 교정 범위

15조건의 독립 원본/native 대조에서 평상시 크기가 같은 것만으로 통과를 판단할 수 없음을 확인했다([차이](../source/typography-boundary-differences.json)). 긴Latin 객체명이 source normal wrap에서는 overflow하는데 CTTypesetter는 문자별로 강제분할했다. 공백/newline/tab collapse가 없었고, paper 경계에서CT advance와HB advance 차이로 한 단어를 더 담았다. ligature·combining cluster·fallback 경계 공백 및 다른 Home 경계 조건은 줄 수/폭이 맞았다.

교정 전에 public CFStringTokenizer UnitLineBreak(3)의 break opportunities를 개발 측정 출력에 추가해 실제 source 경계와 확인한다. 다음 후보는 원본 white-space:normal의 ASCII 공백 collapse/line-edge trim, normal break와 explicit anywhere 분리, HB 실제 advance 기반 break 선택이다. CSS공백은NBSP 등 모든 Unicode whitespace를 뭉개는 char.IsWhiteSpace와 구분한다. normal overflow를clip으로 임의 제거하지 않는다. 현재 후보/원본/잠긴 코드 보존 후 해당Typography 내부만 수정한다.

### 공백·normal/anywhere 교정 구현 근거

Public CFStringTokenizer UnitLineBreak(3)의15조건 break 목록을 확인했다([측정](../source/typography-boundary-breaks-coretext.json)). Latin 단어/한글/combining/hyphen 경계가 원본과 대응하며 긴 객체명은 하나의 token이다. ASCII space/tab/CR/LF/form-feed만 collapse/line-edge trim하고 NBSP는 보존한다. 후보 기본값은 WrapWithOverflow(normal), explicit Wrap은 source overflow-wrap:anywhere, NoWrap은 줄을 나누지 않는다. 최종 줄 선택은 CT 추정 대신 실제 HB advance를 사용한다. probe prefix의 CTLine은 측정 직후 해제한다.

normal overflow는 자체 clip을 제거하고 필요한 좌우 ink extent를 bitmap에 포함한다. 정렬은 원본 box 폭을 기준으로 유지하며 raster origin을 별도로 반환한다. 부모 clip은 해당 부모 조립 때 원본에 맞춘다. 변경은 Typography 후보·직접 내부 및 검증 fixture에 한정한다. 기존 Create/잠긴 코드/원본은 그대로다.

### Wrap mode 10조건 검사

normal/anywhere/nowrap·NBSP·공백-only의 원본 줄 수와 HB 폭이 맞았다. anywhere는 단어 경계를 우선하고 그 첫 단어가 폭보다 클 때만 grapheme 경계로 나누므로 일반 단어를 불필요하게 쪼개지 않는다. 좌/중앙/우 overflow bitmap도 control 바깥으로 그려지는 것을 확인했다.

좁은 combining fixture의 native Bounds200/source223.984 차이는 고정220px parent의 available height200 제한이었다. 후보의 요구 높이는224이다. 해당 독립 probe parent 높이를 최소220/텍스트 요구높이+padding으로 잡아 clipping을 분리한다. 부모 구현 완료로 주장하지 않는다. 다중 행의 CSS line-height 양자화 차이는 별도로 실측하며 수치를 임의 보정하지 않는다.

### 行 높이 양자화 근거

원본49조건에서 `round(lineHeight × deviceScale × 64)/(deviceScale ×64)`를 행 수만큼 더한 높이가 모두 정확히 일치했다([대조](../source/typography-line-height-prediction.json)). 추가 zoom0.5/1/2 측정에서도 배율에 따라 격자가 달라지는 것을 확인했다([측정](../source/typography-line-height-scale-web.json)). 원본 CSS 입력값은 유지하고 Typography 내부 행 높이에만 원본 layout-unit 양자화를 적용한다. 실제 TopLevel 배율을 key에 포함하고 detached source-DPR2 probe는2로 명시한다. baseline49 및 줄바꿈/동적 조건을 다시 비교한다.

### 2026-10-06 이번 교정의 최종 검증 및 미해결 차이

[49조건 baseline/height 및45동적·경계/모드 조건 대조](../source/typography-wrap-final-comparison.json): 행 수·높이·기준선 일치. 줄 폭의 차이는 Range 양자화를 포함해 최대0.00745px다. 15경계와10mode에는 긴 객체명 overflow, ASCII collapse, NBSP 유지, normal/anywhere/nowrap, ligature·combining·fallback·경계 제목을 포함한다. 부모 높이 제한이 source와 달랐던 독립 probe는 요구 높이에 맞춰 넓혔으며 상위 제품 조립 변경으로 주장하지 않는다.

[추가31표본](../source/typography-extra-comparison.json) 중27개는 줄 수·높이·폭이 맞았다. 18px/700 제목4개는 폭 차이2.76–8.93px가 남는다. actual source Range와 Canvas도 다르며 auto/none은Source DB79.3828, explicit opsz18/36은87.390625였다([독립 원본](../source/typography-18-optical-web.json)). public CT/HB의 opsz18은87.39022, default-optical/opsz36은79.37859다. default-optical을 전체에 적용하면14/700 등 기존 표본이 깨진다. graphics font round-trip도 해결하지 못했다. 이 조사만으로18px에 맞춤 축을 강제하지 않는다. 개발 probe의 OpticalSize/FromGraphics/DefaultOptical은 조사 옵션이고 제품에는 적용하지 않았다.

교정 후 macOS 실제16표본 창, N(system→mono)/R 복원을 확인했다([창](../screenshots/typography-wrap-window/window.jpg), [N](../source/typography-wrap-live-narrow.json), [R](../source/typography-wrap-live-restored.json)). 창 client1000×1040/scaling2, 원본과 달랐던18px 표본도 숨기지 않고 포함했다. CUA 경로 handle의 실행 식별 오류는 bundle ID로 다시 연결해 해결했다. 교체한 개발 cases에서mono가 빠졌던 것을 보완했고 probe N에는 fallback도 추가했다. 최초 실패 시도를 통과로 기록하지 않는다. 마지막 close button 후 process 종료를 확인했다. 최신 후보 macOS publish/Windows RID build 경고·오류0; 실제 Windows UI는 아직 확인할 실행 환경이 없다. 본 앱/기존 Create/상위 페이지는 변경하지 않았다.

## 현재 재개 지점 · 2026-10-06

- **Typography WAITING VERIFICATION, LOCK 아님.** 소수점 부모 위치를 bitmap raster phase에 반영하는 후보와 layout invalidation을 구현했다.
- 49개 기준선/높이, 45개 동적·줄바꿈 상태, 추가 31개 역할 및 21개 phase의 배치 회귀가 모두 이전 검증값과 같다. 원본 18px 값은 현재 후보와 일치하며 과거 측정과 달라진 원인은 확정하지 않는다.
- 최신 macOS 자체 포함 publish, Windows x64 build, Windows ARM64 검증 패키지 publish 모두 경고/오류 0. 빌드는 실제 창 통과를 뜻하지 않는다.
- actual macOS 창 N/R 및 P → D → R → D 검증 완료. 다음은 Windows 실제 창. source capture와 native raster의 AA 비교 한계도 보존한다.
- Windows 실제 창은 Parallels EULA 동의에 대한 사용자 응답 대기. 미확인 영역을 잠그거나 부모를 조립하지 않는다. 전체 복제 전 압축 후 상기 제약을 유지한다.

### 18px 재측정 및 fractional phase 조사 재개

2026-10-06 Motion/독립 Divider 조사 뒤 Typography만 활성으로 재개. 관련 규칙/명세/원본 및 renderer를 다시 읽고 잠금 검사 통과. 현재 실제 Connection EN의18px/700 `Source DB`87.390625/`Target DB`82.0625로 후보와 맞는다(`source/typography-18-live-recheck.json`). 동일31조건 원본 fixture를 새로 열어 대조하니31개 모두 행수/높이 일치, 최대폭0.006836px(`typography-extra-*-recheck.json`). 과거79.3828 등의 측정과 현재 실행 값이 다른 사실을 보존하며 원인을 확정하지 않는다. 원본 source/size/spacing/후보 axis를 바꾸지 않았다.

21개fractional X/Y 조건의 독립 fixture/source와 native 렌더를 보관했다(`source/typography-phase-*`, `screenshots/typography-phase/`). source캡처는1280×720의logical JPEG이며 native1920×980 PNG다. 비교상의2× nearest확대는 exact pixel 근거가 아니고 기존 ink-offset 최대2physicalpx도 이 캡처 제약과 구분한다.

다음 한정 후보 변경: source가 사용하는 Skia mac glyph raster의 [public CG font-position/quantization flags](https://raw.githubusercontent.com/google/skia/main/src/ports/SkScalerContext_mac_ct.cpp)와 동일하게 CoreGraphics의subpixelposition허용/활성화, quantization허용/활성화false를 설정한다. 기존 bitmap context의 defaultflags를 source와 대조한 결과이며 임의fontaxis/크기보정이 아니다. typography glyph painting 내부만 변경하고 이전phase렌더를 보존해 전후비교한다. Skia의undocumented optical/tracking descriptor키는 publicAPI 정책 때문에 도입하지 않는다. 기존layout/shaping/parent는 보존한다.

### Subpixel flags 후보 교정 검증

`typography-phase-flags-comparison.json`:21개 표본 모두 source JPEG와의diagnostic meanabsolute차이가 감소(평균3.53178→3.22155/255). source logicalJPEG/native2×BOX라는 비교 한계를 명시하며 exactpixel 일치라고 부르지 않는다. `typography-flags-baseline49.json`의49기준선/높이 차이0, `typography-flags-regression.json`의45dynamic/boundary/mode는이전배치와동일. 추가31표본 행수/높이/폭도current source와일치.

교정macOS actual창16표본 및N/R을다시확인(`screenshots/typography-flags-window/`, `source/typography-flags-live-{narrow,restored}.json`). 창capture2000×2136/scaling2. 최신macOSpublish 및WindowsRID빌드0warning/error. CUAclose로검증앱종료확인. Source/glyphmetric/부모구조/LOCKED범위변경없음.

Windows 환경: 기존Windows11VM파일있음. Parallels app조회첫시도는실행없음이었지만뒤늦은inventory에실행앱이확인되어정정했다. 현재실행경로는translocated27.0.0이며업데이트안내를닫고현재버전선택후EULA+선택된통계전송화면이표시됐다. 계약동의는computer-use정책상사용자확인이필요하므로아무동의/통계변경도하지않았다. 사용자에게동의확인을요청한상태. 증거`screenshots/windows-environment-eula.jpg`. Windows환경없음이라는이전기록으로확정하지않는다.

### Global fractional phase 교정의 한정 범위

Motion검증지점을보존하고Typography만순차재개한다. publicCGflags는확인했지만현재텍스트bitmap은local0좌표에서그린후fractional부모좌표로이동하여imagefilter가추가된다. sourceSkia는glyph를devicephase에서그린다. `Typography` 내부에서untransformedlayoutorigin의devicefraction을bitmapdrawing좌표에포함하고, bitmapdestination에서같은fraction을빼서glyph위치는유지하며bitmap이devicepixelgrid에놓이게한다. RenderTransform은이계산에서제외하여원본CSS합성동작을임의재해석하지않는다. 실제Bitmaporigin/할당을검증하고 이전21phase/49metric/45dynamic/31role와회귀한다. 이것은후보결정이며측정전통과를주장하지않는다. parent/원본/LOCKED/Motion변경없음.

### Global phase 후보 및 회귀 · Mac 잠금 장애

2026-10-06 재개 시 AGENTS → MASTER_RULES → 상태 → 이 명세 → 현재 source phase DOM/CSS → 후보 코드를 실제로 다시 확인했다. 잠긴 SvgIcon/Surface 보호 검사를 통과했다. Typography만 수정했다.

`WebSystemTextBlock`은 untransformed layout origin의 장치 픽셀 fraction을 raster key에 넣는다. `MacSystemTextLayout.Raster`는 이 fraction만큼 bitmap 내부 glyph 좌표를 이동하고 destination에서 빼서 glyph의 논리 위치를 보존한다. LayoutUpdated에서 부모 offset/배율 변화를 확인해 필요한 render/measure만 무효화한다. RenderTransform은 finished raster의 합성으로 남긴다.

21개 source JPEG/native 2× BOX 진단의 평균 절대 차이는 3.22155 → 3.17887/255이다. 10개 개선, 6개 동일, 5개 소폭 증가가 있어 모든 표본 개선 또는 exact pixel 일치로 주장하지 않는다. 캡처 방식의 차이가 남는다. `source/typography-global-phase-comparison.json`.

`source/typography-global-regression.json`: 49개 baseline, 45개 state/boundary/mode, 추가 31개 role, 21개 phase의 layout JSON이 이전과 같다. 증거는 `screenshots/typography-global-regression/`. LayoutUpdated hook의 실제 attached-window 검증은 아직 수행하지 않았다.

개발용 TextProbeApplication에 P/D 진단을 추가했다. P는 부모 grid만 0.125px 이동하며, D는 강제 render 없이 이전 painted raster key를 읽는다. R은 기존 font/width/row 복원과 부모 위치 0 복원을 모두 수행한다. 제품 UI/기존 Create/원본/LOCKED 범위에는 연결하지 않았다.

최신 검증 창 실행을 CUA로 시도했으나 도구가 **Mac 잠김, 자동 해제 불가**를 반환했다. 창을 열었다고 기록하지 않는다. 사용자에게 잠금 해제를 요청했고 그동안 build/회귀/증거 정리를 완료했다. 실제 창 비교 없이 LOCK하지 않는다. Windows ARM64 개발 패키지에는 TypographyCheck.cmd와 16표본을 추가했다. 기존 개발 MotionCheck.cmd의 literal backslash CRLF를 실제 CRLF로 교정했다. 이는 패키지 실행 스크립트 교정이며 Motion 제품 동작 변경이 아니다.

### 2026-10-06 재개 · attached-window phase 검증

AGENTS/MASTER_RULES/상태/활성 명세/실제 source phase DOM/CSS/현재 후보와 probe 코드를 확인하고 재개했다. Mac 잠금은 해제되어 실제 검증 앱이 열렸다. P로 부모 grid만 0.125px 이동한 후 D로 이전 painted raster key를 읽었다. parentX/Y=0.125, childX/Y=10 그대로, paintedPhase X/Y=0.125다. R 복원 뒤 D는 parent=0, child=10, phase=0이다. 부모만 움직여도 render invalidation이 반영되며 복원되는 것을 확인했다. `source/typography-global-live-{shifted,restored-phase}.json`, `screenshots/typography-global-regression/parent-{shifted,restored}.jpg`.

N은 mono11/400 ID의 폭240으로 전환하고 R은 system11 label/폭480으로 복원한다. 접근성 텍스트도 함께 전환됐다. `source/typography-global-live-{narrow,restored}.json`, `screenshots/typography-global-regression/narrow.jpg`. 이 확인 중 새 제품 코드를 수정하지 않았다. 창 close를 수행했다. Windows EULA 승인/실제 화면 미확인이 남으므로 LOCK하지 않는다.

### Windows 환경 재개 및 진단 준비

사용자가 Parallels 로그인/활성화 완료를 알려 준 뒤 Windows11 실제 창을 확인했다. 기존 앱은 /Applications/Parallels Desktop.app에 설치되어 있으며 이전 EULA 장애가 현재 화면에 없다. Toolbox 추가 설치는 건너뛰었고 로그인/구매/OS 설정을 변경하지 않았다. 현재 CUA는 VM 내부 클릭/키 입력을 전달하지 못하고 있다. 사용자가 Windows 브라우저로 독립 fixture 주소를 열어 주도록 요청했다. 시도만으로 Windows UI 통과를 주장하지 않는다.

Windows 텍스트 진단 getter는 public TextBlock.TextLayout의 Width/Baseline을 읽도록 추가했다. macOS 경로는 그대로다. 개발 probe에 generated JSON만 opt-in VM bridge endpoint로 보내는 처리를 추가했다. 사용자 Mac 폴더 공유는 꺼진 상태를 유지하고, 예시 파일만 있는 isolated HTTP fixture를 bridge100의 10.211.55.2에 제공한다. 실제 개인정보/DB/토큰은 포함하지 않는다. 끝나면 서버를 종료한다. Windows ARM64 publish 경고/오류0. 이 후속 진단 변경의 actual 검증은 아직 기다린다.

### Windows49표본 수령 · 사용자 우선순위 변경

source/windows-source-typography.json은DPR2/49원본, windows-typography-candidate-layout.json은실제Windows후보49측정이다. 원본boxY는사용자scroll로-30이어서절대Y비교를하지않는다. windows-typography-initial-differences.json에서최대21px이상의Latin폭차이,한글fallback폭차이,기준선/lineheight차이를보존했다. 글꼴선택과line metrics 원인을 조사하기전값보정하지않는다. 이 시점사용자가Parallels를직접제어할방법부터찾도록우선순위를변경했다. Typography코드교정은대기하며공식guestCLI로고립된계산probe실행은확인했지만GUI입력장애는미해결이다. Motion창수동검증요청을반복하지않고제어경로진단을먼저한다.

### Windows 글꼴 선택 원인 확정 및 다음 한정 교정

규칙/상태/명세→원본CSS body system-ui와 실행 Connection EN의computed font→현재WebTypography/WebSystemTextBlock→49차이를 실제로 복구했다. 잠긴 범위 변경 없음. 공식UIA 경로 확정 후 Typography만 활성으로 재개했다.

Windows public menu font 메타데이터는 ‘맑은 고딕’이었다. Chromium의 FontCache::SystemFontFamily는MenuFontFamily를 반환한다([공식 코드](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/platform/fonts/win/font_cache_skia_win.cc)). 현재 후보는Segoe UI고정이었다. 개발용REPLICA_FONT=Malgun Gothic 대조에서16한·영 표본의행 수와폭이Source Range 양자화 범위(최대0.0074px)로 일치했다. windows-{font-resolutions,font-runs,menu-font-runs}.json. 기준선 차이는 여전히 남는다.

다음 제품 변경은Typography의 Windows system-ui 선택을public SPI_GETNONCLIENTMETRICS의lfMenuFont 이름 조회로 대응하는 것뿐이다. 설정/글꼴 설치/파일 재배포/OS변경은 하지 않는다. API 실패는증거로남기고Segoe UI를조용히동일원본이라고가정하지않는다. 별도 font metric 진단으로원본baseline 계산을확인한후교정한다. font-size/spacing에수동맞춤값을넣지않는다.

### Windows baseline 한정 교정 근거

메뉴 글꼴 선택 교정 후49표본 모두줄 수일치/폭최대0.007324px. Avalonia GlyphTypeface.Metrics는USE_TYPO_METRICS 등자체테이블선택을사용해Consolas의source DirectWrite/Skia metric과달랐다. public SKFont.Metrics로측정하면49전체에서primary rounded ascent/descent + floor half-leading의baseline예측차이0이다. windows-font-skia-metrics.json / windows-typography-skia-baseline-prediction.json. [Skia DirectWrite](https://raw.githubusercontent.com/google/skia/main/src/ports/SkScalerContext_win_dw.cpp), [Avalonia metric선택](https://raw.githubusercontent.com/AvaloniaUI/Avalonia/12.1.3/src/Avalonia.Base/Media/GlyphTypeface.cs).

다음 후보는Windows전용TextBlock의public RenderTextLayout override에서각TextLine.Draw를source primarybaseline으로배치한다. shaping/줄선택/정렬은현재49폭검증을통과한TextLayout을재사용한다. LineHeight는원본과동일한장치scale×64격자로양자화한다. CSSnormal overflow를child자체clip으로잘라내지않는다. Mac renderer/부모/원본/LOCKED범위는변경하지않는다. 데이터만바꾼getter로통과를꾸미지않고실제Draw위치를연결하고렌더를재검증한다.

### Windows49 실측 후 · 66 경계 조건의 현재 차이 · 2026-10-06

規칙/상태/명세/관련 원본CSS 및실행Connection EN computedfont/후보를 다시 읽고 잠금검사 통과후 재개했다. 메뉴font + SKFontbaseline 실제49측정에서 행수/높이/기준선차이0, 최대폭차이0.007324px였다. 새UIA N/R은새파일시각과제목으로확인했다. source/windows-typography-css-{layout,comparison,live-narrow,live-restored}.json.

추가66조건을실제WindowsEdge InvokePattern으로다시측정했다(DPR2). source/windows-source-typography-extended.json / windows-typography-extended-native.json / windows-typography-extended-comparison.json. 최신setter중복제거/검증창topalignment후publish패키지로실행했다. 긴객체명/NBSP를native가강제분할하고,empty/공백-only가1행을차지하며,동일control의weight전환/combiningcluster에폭차이가남는다. 원본/부모/Motion/잠긴범위수정없음.

다음은public TextLayout의실제font run을기록해dynamic font선택차이의원인을확인하는개발probe만확장한다. 결과를보기전fontweight보정/기억기반renderer교체를하지않는다. Typography는아직LOCK되지않았다.

### 동적 font/줄바꿈 교정의 조사 근거

실제66 run 기록(windows-typography-extended-runs.json)에서localizedfamily ‘맑은 고딕’의650/600이realbold대신regular+Bold simulation을선택했다. 처음49의Malgun Gothic700/None과달라폭이줄었다. SKTypeface가반환한canonicalfamily/실제weight를publicTextLayout에전달하는후보를검증한다. 원본requestedweight와UI속성은보존한다. 숫자폭보정은하지않는다.

normalwrap은shapedrun별로linebreak를판정해서긴객체명과NBSP를잘랐다. public UnicodeLineBreakEnumerator를전체문장에적용하고실제TextLayout advance로줄을고른뒤각줄을NoWrap으로shaping하는한정후보를검증한다. anywhere는grapheme경계로만쪼갠다. emptycollapsedtext는높이0/줄0. combininge+acute의fallbackConsolas가source와다르므로canonicalNFC상당문자로shaping하는후보를66원본으로대조한다. native접근성원문은보존하며확인전동등성을주장하지않는다.

### 경계 교정 후보의 현재 검증

canonicalface/actualweight·문장전체UAX14·NFCcluster·empty높이0교정후66조건은줄수/높이가모두일치했다. NBSP끝은GlyphRun이보존하지만TextLine.Width가제외하므로WidthIncludingTrailingWhitespace를쓰고그같은유효폭으로실제Draw정렬도교정했다. 삽입된강제newline이negativeLetterSpacing1개를추가한것을66/기존49회귀에서확인했다. 실제glyph없는개발줄바꿈의spacing만CssAdvance에서제외한다. 수동맞춤상수가아니며sourceLetterSpacing속성을재사용한다. 이후최종회귀/실제창은진행중이다.

Mac49재실행layout은이전global-regression/baseline와완전히같다. 두RIDpublish경고/오류0. Windows전용경로외Macshaper변경없음.

### Windows 최종 검증과 현재 재개 지점 · 2026-10-06

- 66경계/동적조건: 줄수/높이차이0, 최대advance차이0.007324px. windows-typography-extended-final-{native,comparison}.json.
- 기존49회귀: baseline/높이/행수유지, 이전advance차이최대2.2e-12. windows-typography-final49-native.json.
- 최신actual 창N/R은focus nonce·EXEpath·새JSON시각·창제목으로확인. windows-typography-final-live-{narrow,restored}.json. N width240/5행/height142.5, R480/1행/22.3984375.
- 실제Windows32표본(Home16+결과role16) source/browser vsnative창을공식capture 및각UIA Document/client영역으로대조했다. 모든표본의threshold32 inkboundingbox가같고ROI평균절대색차이최대0.07410/255. source/windows-typography-{final,role}-raster16-comparison.json 및screenshots/windows-control/typography-{final-source-maximized,final-top-native,role-source,role-native}-client.png. 동일PNG/DPR2이며exactpixel동일로주장하지않는다.
- 검증fixture의테마없는ScrollViewer는긴atlas가운데를보여주었으므로직접Canvas/Top/Left로교체했다. 제품부모/원본웹수정없음. 첫16 actualviewport가원본과같이보이며필드별위치대조를다시수행했다.
- macOS49최신회귀는이전baseline JSON과완전히같다. typography-windows-fix-mac-regression/; 기존Mac20dynamic/15boundary/10mode/31role/21phase 및attachedN/R/P/D증거는이명세의이전교정완료기록에연결돼있다.

현재다음행동: Windows의system↔mono/tnum actualN/R을추가확인하고새임시폴더에서개발ZIP이실행되는지확인한다. 이어Typography잠금계약·보호해시를기록한다. AA/캡처방식한계와legacyCreate/미검증부모를완료로혼동하지않는다.

### 최종 판정 · Typography LOCK · 2026-10-06

확정 API는 `WebTypography.CreateCandidate`와 `WebSystemTextBlock`이다. 이름이Candidate인것은기존호출계약을유지하기위함이며이primitive만검증/LOCK한다. 기존 `Create`/MainWindow의TextBlock근사helper·상위페이지는복제완료가아니다. 상위조립은LOCK된candidate를사용해원본역할의정확한수치와부모배치를전달한다. richinline/편집기/새언어/다른font는이plain-text계약에포함하지않는다.

계약: 원본의KR/EN plaintext; system-ui/mono; source font-size/weight/line-height/spacing/solidcolor/tnum; ASCII공백collapse·NBSP보존; normal/anywhere/nowrap; left/center/right; empty높이/줄수0; 상태변경시measure/rasterinvalidate; 부모의fractionaloffset과source색상공간. 자체hover/pressed/focus/animation/border/radius는원본textprimitive에없으므로부모가담당한다. sourceCSS는위상위역할에연결돼있다.

Windowsactualsystem↔Consolas/tnum N/R을새파일시각과제목으로재확인했다. windows-typography-role-live-{narrow,restored}.json. 별도새TEMP폴더에서비밀번호없는최신개발ZIP을해제해49개표본실행exit0, runtimeconfig/deps존재, host/guestZIP SHA256일치를확인했다(windows-control-clean-package.json). 이는개발검증묶음의검사이며최종제품배포/BRB제출완료가아니다. actual검증창은닫았다.

구조/실제glyphface/크기/위치/spacing/typography/color/줄바꿈/상태전환/부모배치의확인범위는위증거와기존Mac측정에연결한다. 윈도32실제PNG표본의inkbounds는일치한다. 브라우저/nativeAA와MacJPEG/2×다운샘플캡처의색차이는bit-for-bit이미지동일성증거로쓰지않는다. 측정값에맞춘font-size/spacing상수·source수정은없다. 재현환경은현재MacARM64/Windows11ARM64(DPR2)이며다른OS/배율/배포아키텍처의인수검증을완료했다고주장하지않는다. 이현재환경의원본계약은검증됐으므로primitive만LOCK한다.

보호해시: source/typography-lock.json. 원본/잠긴SvgIcon·Surface/Motion/부모코드변경없음. 다음단일활성컴포넌트는Motion최종primitive검토다. 압축후상기의무는복제전체가끝나지않았으므로유지한다.

## LOCK 예외 조사 · Notice 연결 · 2026-10-07
변경 전 기록: Notice의영문cleanup본문176폭에서source6줄/native7줄이다. sourceRange는3번째줄끝artifact T- 뒤break하고001로다음줄을시작한다. Mac CFStringTokenizer UnitLineBreak는해당경계를제공하는지진단한다. 영향대상 Typography의Macnormalwrap 및직접부모Notice; 다른plain-text부모는기존49/66회귀로확인한다. 기존source/typography-lock.json은아직수정하지않았고15해시통과상태. 먼저원본hyphen·숫자·NBSP경계와native break를수집하며확인전tokenizer교체나새기본값을정하지않는다. Notice상위조립은대기한다.

### LOCK 변경 전 확정 근거
48개 actualsource의12토큰×4폭과CoreText/설치Avalonia UAX14목록을대조했다. T-001/A-001/1-001은브라우저에서hyphen뒤break가가능하지만양nativeiterator는누락한다. A/001은브라우저가분리하지않지만양iterator는slash뒤break를제공한다. ASCIIhyphen과뒤숫자/앞ASCIIalnum 조건은[Chromium 공식 구현](https://chromium.googlesource.com/chromium/src/+/c09887405b8deb565f8ad16b37ae35467188ef4f/third_party/blink/renderer/platform/text/text_break_iterator.cc)의minus-sign예외와같고slash숫자는ASCIItable에break가없다. 다음변경은공유helper가이두경계만보정하고기존CF/UAX14기타경계·font·glyphshape·baseline·paint를유지한다. 영향MacSystemTextLayout/WindowsWebTextBlock/직접Notice, 기존LOCK연결부모는회귀대조한다. 이전LOCK증거를before-numeric-boundaries파일로보존하고새검증까지LOCK하지않는다.

### 예외 교정 재검증 및 재LOCK
원본에서실사용하는ASCII숫자경계/NBSP/한글40조건양OS높이·행수동일,advance차이1/64범위. Mac추가U+2010/U+2011도일치했으나Windows의해당8탐색표본은glyphfallback폭이다르다. 이문자들은original.js에존재하지않음을코드로확인했다. 이추가탐색은원본앱범위밖이며성공으로치환하지않는다:typography-notice-windows-boundary-comparison.json에원본/현재값보존. 신규문자·신규font범위는기존LOCK계약에서제외되어있고이번에도범위를확장하지않는다.
기존49baseline/height/advance양OS이전JSON과동일,66dynamic상태양OS이전JSON과동일. Mac66+49의68PNG SHA256도이전published동일candidate와변경없음. SidebarItem132endpoint/132reuse양OS회귀일치. 증거:typography-notice-{mac,windows}-regression.json, boundary-comparison.json 및SidebarItemcomparison. glyphmetric/raster/원본/잠긴상위제품파일변경없다. 둘의linebreak위치만교정한새보호해시typography-lock.json으로재LOCK한다. Notice를다시단일활성으로전환한다.

## LOCK 예외 · 2026-10-08 SummaryItem inline 소유경계
변경이유: 실제 Review .summary-item의mono값과small설명이동일font12/22의인라인한줄을공유하며색상만다르다. 별도블록으로쪼개면원본줄바꿈이바뀐다. 영향: MacSystemTextLayout/WebSystemTextBlock의새readonly SystemLineTexts 관찰API. 기존layout/shaping/raster수정없음. 원본근거: 실제tab1 Review .summary-item .mono+small DOM,computed둘다12px mono/22,smalldisplayinline. 새부모는원문색상구간을기존Typography가결정한동일줄배치에서clip한다. API추가전LOCK예외기록;기존49/66 probe를다시실행하며새부모의색상구간은별도컴포넌트검증으로계속한다.

readonly SystemLineTexts 추가 후 양OS 기존49/66 layout/metric/line width 이전최종JSON과완전히같음. Mac68 PNG SHA256도이전과같음. source/typography-inline-regression.json. 표본16 초기실행은49회귀가아니어서기준파일typography-baseline49-cases.json으로새로실행했다. shaping/layout/raster 변경없음; getter결과를이용하는새InlineText 부모는다음활성으로검증한다. Typography재LOCK.

## Parent assembly LOCK exception · 2026-10-08 · before edit
Root only scope: `WebSystemTextBlock.SystemTextAutomationPeer.GetAutomationControlTypeCore` returnHeader onmacOS when existing attachedHeadingLevel>0; ordinarytext and WindowsTextpeer unchanged. Reason: locked WelcomeStep(H3), WelcomePaperContent(H2), ResultChart(H2) annotate HeadingLevel but genericText maps AXStaticText, losing original heading role/level in actual parent traversal. Original fresh root59Home DOM+CUA AX headings1/2/3 `root-heading-semantics-original.json`, connection freshactualsourceResults H2 `resultspage-headings-original-provider.json`; official Avalonia12.1.3 nativeprovider automation.mm Header→AXHeading/valueHeadingLevel vsText→StaticText. No font/layout/render/parentstructure changes. Affectedparents Home/Results and futureselection headings usingthisexactprimitive. PageHeading ownH1HeadingFocuspeer separate laterexception; not changed inthisactivecomponent. Validation: new headlessmetadata ordinaryText/heading2/3 + theiractualbothOSAX role/name/level/child traversal. Reuse unchanged protectedTypography40/49/66/paint/parentlayouts; no duplicated fullgeometry. Untilnewsemanticsverified actualOS, INPROGRESS exception with oldmanifestpreserved, no reLOCKclaim.

Recovery checkpoint: actual AGENTS/MASTER/status/parallel/headless/root active record + this exception + original fresh Home heading snapshot + current peer/probe read again. Mac read-only AX helper initial compile failed Swift token spacing only; corrected helper compiled exit0, no input executed. Windows87fa10e complete staged runtime and independent rendered headless3bf8eaa four peers passed: Text/levels0,2,3,2, names/children preserved. Mac60dec5 metadata485a7b already passed; actual bothOS role checks pending resource queue, no repeated glyph/layout tests. Expected verify_lock failure remains only documented WebSystemTextBlock.cs exception.

Windows unchanged peer metadata explicitly has one inner WindowsWebTextBlock child on each generic text, whereas Mac has zero. Earlier summary said children preserved without numeric distinction; retain actual4metadata. The current exception changes Mac heading role only; Windows duplicate traversal is a separately known integration concern, must compare actual native/source before any extra scoped exception. Windows helper records children without imposing a guessed zero-child assertion. Names compared to four exact literals.

Stable peer checkpoint: bothOS headless4 passed, guarded actual helpers compiled/prepared. Actual resources queued; root switches to unchanged ProgressPage Windows source comparison while waiting, with no extra Typography edit. Return for scoped actual4 at grant.

Resume actual4 checkpoint: unchanged Progress source headless observer attempts preserved as failed/noPOST; no candidate edit. Home9f exactnative/sourcecleanup0 grants Windows to root for read-only heading4, then return resource. Mac stays preflight until its current parent9 checkpoint.

Heading peer exception reLOCK: Mac60dec5 actuald58db1 four AX peers match originalsemanticroles/names/headingValues2,3,2; plainAXStaticText unchanged, exact ownedwindow close/process exit0. Windows87fa10e actual82531c four Text names/children traversal recorded and processcleanup0; sourceWindowsH2 ControlType.Text confirmed Results original provider record. Windows unchanged generic wrapper child1 and PageHeading ownH1 are separate known parent integration concerns, not newly passed semantic equivalence claims. NewMacrole is the only product change; font/layout/raster and allordinarytext identical, prior40/49/66/parentgeometry reused. Bothheadless4 metadata/geometry comparison zero, affectedparents existing attachedHeadingLevel used directly. Oldmanifest saved typography-lock-before-heading-peer.json (history, not active guard); renewed typography-lock.json. No blanket completedpage/app claim.

## 변경 전 Mac slash 경계 한정 LOCK 예외 · 2026-10-09
상위 Advanced 실제폭453의 동일원본문구가 source3/native2 lines로 달라진다. literal/font/advance836.1376953125 동일이므로 OS표현차이 면제가 아니다. root-slash-boundary-mac-before-comparison.json:23조건중9줄분배차이; 숫자/공백 controls일치. CFStringTokenizer 실제읽기에서 한글첫글자 경계는존재하며 slash뒤 ASCIIletter 경계가추가된다. root는 MacSystemTextLayout의 tokenizer기회 목록에서 '/'→ASCIIletter 경계만제거한다. WebNumericLineBreaks/Windows/Font/glyph/spacing/문구/원본불변. 실제사용 Advanced, Save completed/interrupted/pending, Trial accounts/permissions 영향을새조건으로확인; 기존숫자/KR/EN 통과조건재사용. 새23+실제문구supplement headless/actualMac렌더 및 Advanced부모검증 완료전 IN PROGRESS, 기존manifest유지. 전체Typography다시분석/재설계없음. Chromium ASCII fastbreak table compatibility의 일차자료 https://chromium.googlesource.com/chromium/src/+/main/third_party/blink/renderer/platform/text/text_break_iterator.cc 로추가확인; source실행값이판정기준.

Mac slash 좁은예외 재LOCK: root5cf47 frozen, 원본23+실제Save/Trial문구8의 headless와 actualMac 총31조건에서 literal/폭/line/height/advance strict차이0. 실제PID60358/60376 측정저장직후Close/exit0, source76/77 즉시종료. root-slash-boundary-mac-after-comparison.json 및 cleanup참조. Windows callsite/파일불변으로 freshAdvancedWindows4와 기존Typography 증거재사용; Advanced부모resize4는 home가 다음검증하며 아직미완료. 기존glyph/font/numeric/peer 변경없음, oldmanifest before-slash-boundary보관.

## Before-change Windows parent reading exception · 2026-10-09
Home exact original ControlView4 manifest5edbbb/source-controlview.json: ordinary summary DBMS/SQLServer/Source→target/categorylabels each ONE named Text leaf, Control/Content true, positivebounds. Actual4464 SummaryControlView10 has each TWO identicallynamed Text(wrapperWebSystemTextBlock+innerWindowsWebTextBlock), plus3fullcombined Inline texts and invisibleM measurement children. Existing spec explicitly left Windowswrapperchild duplication as separate parent concern; not an OS/glyph waiver. Root narrow primitive change: Windows SystemTextAutomationPeer.GetChildrenCore returns empty, preserving its existing exactTextname/role and actualvisual child/render. Windows GetNameCore honors standard explicitlyattached AutomationProperties.Name (fallbackowner.Text), needed by dependent Inline text region sourceprefix/suffix semantics. No typography/layout/shape/paint/focus/input changes; Macchildren/name default identicalwhenName absent. Beforechange d6a manifest archived. Required headless/plaincurrentName/attachedName-update/emptychildren and actualWindows source-backedSummary4 nonmono names once/positivebbox/nohiddenM duplicates, existinggeometry reused. Inline Windows remains separate unresolvedchild scope and is not edited until primitive reLOCK; current globalreading/header/callback projectedchild implications recorded and must be verified on realOS.

Recovery performed: AGENTS/MASTER/status/ownership/HEADLESS/lease/root record, current Typography before-change contract, exact original summary DOM and original ControlView4 plus current peer code reread.

Windows single-text exception reLOCK: product6e8fcef, own Home snapshots d598/3e024 same product hashes; publicmetadata44 and unchangedgeometry4 strict549/0; attachedName/update/ClearValue/empty current cachedpeer4 strict28/0. ActualKOEN960/1280 ControlView/publicmetadata48/0, each ordinary name once, Text leaf children0/control/contenttrue/positivebounds and hiddenM0. ExactPID11316 WindowPattern.Close/process0/window0 immediately, headless2016 process0 (wrapper exitCode unavailable). Source original5edbbb ControlView4, linked source/home-pageintegrationsummarycontent-windows-generic-text-headless-corrected-composition.json (72/0); headless-geometry4/name-updates/actual-comparison.json. Mac branch and all visual/glyph/layout code unchanged; existing Mac/49/66 geometry proofs reused. Inline Windows prefix/suffix grouping remains separate pending, no whole-app completion claim.
