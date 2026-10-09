# 복제 primitive 검증

제품 웹을 변경하지 않고 캡처 원본 `docs/replica/source/icon-definitions.json`의 geometry로 고립된 비교 화면을 만든다. 실제 production SvgIcon을 Avalonia로 렌더한다. 별도의 UI 디자인안이 아니다.

1. `python3 tools/ReplicaProbe/prepare.py` — 원본 도형의 154조건과 `source/svg-reference.html` 생성.
2. `dotnet run --project tools/ReplicaProbe -c Release -- docs/replica/source/svg-cases.json docs/replica/screenshots` — 실제 Avalonia 1×/2× 렌더와 layout 수치.
3. `docs/replica`를 loopback 정적 서버로 열고 CUA에서 reference 페이지의 640×1024 영역을 `screenshots/svg-atlas-web.png`로 캡처. `.cell > svg`의 DOM bounding box도 `source/svg-web-layout.json`으로 보관한다. 제품 원본 탭에는 검사 코드를 주입하지 않는다.
4. Pillow가 있는 Python에서 `tools/ReplicaProbe/compare.py` 실행. silhouette 대조 결과와 래스터 차이를 보존한다. 위치/크기는 web/native layout JSON의 표시 153조건을 별도로 비교하며 0.0001 logical px 이내 일치를 요구한다.
5. `python3 tools/ReplicaProbe/verify_lock.py` — 등록된 모든 잠긴 파일/심볼의 무변경 확인. 다른 심볼 변경은 금지하지 않는다. 필요한 LOCK 수정은 명세에 먼저 기록한다.

2 logical px silhouette 허용 범위는 브라우저 캡처/렌더러 antialiasing 대조용이며 geometry/배치 값을 바꾸는 허용 오차가 아니다. hidden 조건은 양쪽 빈 그림이어야 한다. 도형 geometry는 원본 그대로이며 native layout rounding을 끄고 fractional 중심 좌표도 별도로 대조했다.

이 검증은 macOS에서 수행했다. Windows build 성공과 Windows 실제 UI 사용은 구분한다. 전체 부모/페이지 일치 완료 근거로 사용하지 않는다. 실행에는 기존 프로젝트의 .NET/Avalonia와 개발용 Pillow만 사용하며 앱 사용자가 이 도구를 실행할 필요는 없다.

## Surface

`prepare_surface.py`는 관찰한 original paint 값을 26조건의 `source/surface-reference.html`로 분리한다. `--surface source/surface-cases.json screenshots`는 생산 WebSurface의1×/2× PNG와 layout을 기록한다. `--surface-children source/surface-child-cases.json screenshots`는 관찰한 여백/테두리 안의 child 배치를 기록한다. 경로는 `docs/replica/`를 기준으로 전달한다.

실제 창은 `--surface-window <cases> <output>` 모드로 검증한다. macOS 개발용 .app 안의 `Contents/MacOS/surface-cases.json`이 있으면 기본 인수로 이 모드를 사용한다. 색 공간 준비는 제품 WebSurfaceColorSpace를 직접 사용하며 앱 사용자 데이터/DB에 연결하지 않는다. 캡처는 CUA로 수행한다. jpeg/ICC/배율을 함께 기록하고 원본 색/spacing을 capture pixel에 맞춰 바꾸지 않는다. 원본 shadow sigma와 offset을 그대로 사용하며 OS 창의 색 공간만 명시한다.

## Typography (아직 미완료)

`prepare_text.py`와 `--text <cases> <output>`의 한영 16조건을 사용한다. 영역 수치 외에 glyph run의 실제 font/weight와 line widths/line breaks를 기록한다. `--fonts`/`--font-api`/`REPLICA_FONT`는 조사 도구이며 원본 폰트를 대신하는 제품 정책이 아니다. 자세한 현재 차이는 `docs/replica/specs/Typography.md`를 따른다.

`--coretext <cases> <output.json>`은 macOS 시스템 UIFont + CSS weight variation의 줄바꿈/폭을 직접 측정한다. `--coretext-render`는 동일 측정에 case별 1000×260 RGBA raster를 추가한다. 개발 조사 모드이며 제품 TextBlock 또는 실제 Avalonia 창이 아니다. `Typography.md`에 비교 범위·남은 차이를 기록한다.

`--text-candidate <cases> <output>`는 실제 후보 WebSystemTextBlock의1×/2× Avalonia raster와 OS line widths를 기록한다. 기존 `--text`의 TextLayout 진단을 후보의 실제 renderer로 오인하지 않는다. `--text-states <state-cases> <output>`는 한 control의 동적10조건을 root measure pass와 함께 비교한다. `--text-window` 또는 .app내 typography-cases.json 기본 인수는 후보 실제 창이며 N/R이 source state oracle의 narrow/restored 조건을 전환한다. 이는 개발 fixture이며 제품의 새 shortcut/레이아웃이 아니다.

`--harfbuzz-text <cases> <output.json>`/`--hb-api`는 existing HarfBuzz runtime의 조사 모드다. 실제 OS SFNS를 메모리로 읽고 variation/ptem을 비교한다. dev OS 파일 경로는 제품에서 사용하지 않는다. `typography-baseline-reference.html`/Canvas fixture는 독립 조사 화면이며 live 제품 웹의 수정본이 아니다. baseline round 조사 후보는 채택하지 않았다.

`--coretext-tables <cases> <output.json>`은 public CTFontCopyTable callback + actual variation/point size로 fallback run별 폭을 측정한다. 고정 font 파일 경로를 사용하지 않는다. 최신 후보는 같은 OS table 경로의 HarfBuzz glyph positions를 CTFontDrawGlyphs로 그린다. 기존 독립 CoreText raster와 최신 candidate 증거는 구분한다.


Typography case JSON의 `Family`는 system/mono, `Tabular`는 OpenType tnum 설정이다(생략 시 system/false). `--text-candidate`는 조건 수에 맞춰 atlas 높이를 정한다. 출력 폴더는 먼저 준비한다. `typography-role-cases.json`은 실제 결과 화면에서 분리한33조건, `typography-role-state-cases.json`은 동일 control의10추가 상태다. 기존16/10 회귀는 `typography-default-regression-comparison.json`에 기록했다. 실제 Typography Check의 현재 기본 cases는 결과 역할16조건이고 N/R은 mono ID↔첫 숫자로 전환한다. 다른 fixture의 N/R 조건과 혼동하지 않는다. 현재 재개/잔여 항목은 Typography 명세 끝을 따른다.
