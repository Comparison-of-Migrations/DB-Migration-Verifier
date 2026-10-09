# Motion primitive · LOCKED

## 원본 및 범위

Source of Truth: 실행 웹의 CSS/JS. 관련 원본 위치: `source/original.css` :root/button/input/keyframes/reduced-motion, `source/original.js` animateResult/press/closeModal/clear-search. 현재 후보는 이 primitive만 만들고 MainWindow Pulse/Enter나 부모 구조를 교체하지 않는다. 직접 하위 컴포넌트 없음.

2026-10-06 Typography는 Windows actual UI 환경 및 명세의 미해결 차이를 보존한 WAITING VERIFICATION 상태다. Motion만 활성으로 전환했다. 잠긴 SvgIcon/Surface 검사는 통과했다.

## 실제 computed style

원본 Home EN의 page-in:200ms,cubic-bezier(.22,1,.36,1),fill none. 버튼은 background/color/shadow120ms,transform80ms,동일 곡선. transform-origin은 요소 중앙. --fast120,--base200,--panel280. 별도 `settled` class는 page animation none이다. 측정 fixture는 source를 변경하지 않고 독립 생성한다.

## 시간·값 계약

| 역할 | 시간 | 시작→끝 | 곡선 |
|---|---:|---|---|
| Page/Notice/Finding/PreparedDB/Enter |200ms|opacity .35→1,Y6→0|(.22,1,.36,1)|
| Modal |280ms|opacity0→1,Y10→0|동일|
| Drawer |280ms|opacity0→1,X20→0|동일|
| Toast |200ms|Modal 시작/끝|동일|
| Result number |220ms|opacity.6→1,Y5→0|동일|
| Result mini bar |320ms|scaleX0→1;origin left|동일|
| Press WAAPI |160ms|0:scale1/op1,.5:scale.98/op.86,1:scale1/op1|CSS ease-out(0,0,.58,1)|
| Modal close |160ms|opacity1→0,Y0→6,fill forwards|ease-out|
| Search clear fade |160ms|opacity.5→1|ease-out|
| Running dot |1000ms infinite|opacity1→.3→1|CSS ease-in-out(.42,0,.58,1)|
| Spinner |800ms infinite|rotation0→360|linear|

CSS transition 원본 값: chevron200custom; meter-bar200linear; category grid-row/visibility200custom; checkbox/radio background/border/shadow120ease,transform80ease,pseudo glyph160custom; DBMS/category/matching/table hover160ease; result chevron120custom. 색·크기·상태 전환·visibility·discrete 효과의 최종값은 각 부모 명세에서 검증한다. 이 primitive에서 임의로 정하지 않는다.

reduced-motion은 모든 CSS animation/transition을 없애고 JS press/result/close/searchfade를 생략한다. Running dot opacity1, spinner 단색 배경 opacity.7이다. primitive는 호출부에서 bool을 받아 true이면 애니메이션을 만들지 않는다. OS 설정 조회와 각 부모의 정지 표시·완료 동작은 상위 조립에서 확인한다. 원본에 없는 수동 toggle을 추가하지 않는다.

## Avalonia 대응

원본과 같은 cubic-bezier WebCubicBezierEasing 및 double transform setter의 Animation을 사용한다. 표준 CubicEaseOut을 원본의 custom/ease-out 대신 사용하지 않는다. FillMode/반복/reduced/cancellation을 명시한다. global easing과 여러 keyframe의 시간 계산은 Web paused animation과 controlled Avalonia clock을 대조한 뒤 채택한다. CSS/WAAPI 합성 순서는 ActionButton 등 부모에서 재확인한다.

## 완료 조건

독립 fixture의 각 시각 opacity/transform, duration/easing/fill/반복, 종료 복원·cancel·reduced, 실제 부모 내부의 origin과 macOS 실제 창 재생을 비교한다. Windows 실제 UI 미확인은 남긴다. 미검증 상태를 LOCK하지 않는다.

## 검증 도구의 clock 범위

설치된 Avalonia12.1.3의 IClock/ClockBase/Clock은 internal이며 public 문서가 직접 구현 가능한 API라는 뜻은 아니었다. 개발 probe에서 해당 버전의 Clock 인스턴스/Pulse와 Animation.RunAsync(clock) overload를 reflection으로만 호출해 실제 엔진을 결정된 시각에 샘플한다. 제품 WebMotion은 public Animation/KeyFrame/Easing만 사용하고 internal clock에 의존하지 않는다. global clock/OS 시간/설정을 수정하지 않는다. 실패한 첫 API build는 검사 통과로 기록하지 않는다.

## 첫 engine 실행에서 확인한 대응 수정

RenderTransform에 TransformOperations를 Setter로 넘기는 Animation animator가12.1.3에 등록되지 않아 첫 실행이 실패했다. 해당 실패는 통과 증거가 아니다. source transform 값은 그대로 두고, 지원되는 TranslateTransform.X/Y,ScaleTransform.ScaleX/Y,RotateTransform.Angle의 double setter로 연결한다. 좌표/origin/반복 값은 바꾸지 않는다. 부모 source의 기존 transform과 합성은 부모 단계에서 검증한다.

## 시간 샘플에서 확인한 교정

첫132시각 비교에서 Press의 global easing은 일치했지만 CSS RunningDot의 구간별 easing이 달랐다. pulse에만 구간별 시간 매핑을 적용한다. 표준 SplineEasing의 근사 오차는 alpha 최대0.00048,이동0.00289px였다. source cubic-bezier의 동일 control point를 x역함수로40회 bisection해 계산하는 WebCubicBezierEasing으로 대응한다. 스타일/시간/target값을 맞춤 수치로 보정하지 않는다.

RunAsync는 무한 반복에 faulted Task를 반환하므로 개발 probe의 무한 반복은 동일 엔진 Apply로 검사한다. 제품 무한 Animation은 부모 Style.Animations에서 수명과 함께 적용해야 하며 await RunAsync로 사용하지 않는다. ResultNumber27.5ms 경계에서 작은 시간 양자화 차이는 추가 확인한다. 아직 LOCK하지 않는다.

## 현재 재개 지점 ·2026-10-06

압축 후 AGENTS→MASTER_RULES→STATUS→이 명세→관련 원본/live→candidate→차이를 실제로 복구했다. live page200custom/button120custom+transform80custom/origincenter 재확인. 고립된 후보11종류, CSS/WAAPI 곡선 구분, public API 구현 빌드/발행 성공. 부모/기존 Pulse/Enter 변경 없음.

- `source/motion-web.json` / `motion-native.json` / `motion-comparison.json`:132개 fractional 시각. ResultNumber27.5ms는 native27ms에 해당, spinner800.8ms는 native800ms에 해당한다. 프레임 시각의 정수ms 양자화가 남음. native의 alpha차0.002794/Y0.034926px/rotation matrix0.006283은 숨기지 않는다.
- `source/motion-*-integer.json`:같은 정수ms132개를 비교. opacity최대4.53e-7, matrix최대4.39e-6. source computed serialization의 유효 숫자 범위. CSS dot의 구간별 easing·두 번째 반복·fill none복원 및 close forward확인. 이 비교는 fractional 차이를 면제하지 않는다.
- midpoint cancel:None은 opacity1/identity 복귀, closeForward는 중간값 유지. 원본 close는 cancel하지 않고 finished 후 제거하는 흐름이라 부모 조립에서 반드시 그 수명 유지. controlled probe의 finite Task 일부는 Dispatcher를 실행하지 않아 WaitingForActivation. RunJobs를 샘플 사이에 호출하면 시스템clock도 진행되어 오염되므로 사용하지 않는다. 실제 창의 완료 증거로 별도 확인했다.
- `source/motion-window-{completed,cancelled,reduced}.json`, `screenshots/motion-window/`:macOS 실제 창에서9finite완료·2infinite재생, C키중단으로infinite opacity/transform복원, M키에서11effect생략, R키다시9finite완료 확인. probe용 키/문구는 제품에 추가하지 않는다. CUAclose로 검증 창을 닫았다.
- 사용자 OS reduced-motion 설정을 바꾸지 않았다. Boolean primitive 검증이며 OS조회/부모정지 spinner표현은 부모 명세에서 확인한다.

미해결: fractional clock해상도, transformorigin의 재생중 실제 픽셀 비교, transition 실제 상태 합성, Windows 실제 창. LOCK하지 않는다. 후보/evidence해시는 `source/motion-candidate-revision.json`. 다음 활성은 독립 Divider. Motion/Typography는 동시에 수정하지 않는다.

### 회전/확대 기준점·곡선·시간 해상도 진단

원본 CSS/WAAPI 상태와 후보를 다시 확인하고단일Motion작업만재개했다. 현재Windows는ParallelsEULA사용자확인대기이며Typography동시수정없음.

`source/motion-engine-time-inspection.txt`:실제TimeSpan27.5ms는275000ticks이며입력손실없음. 설치된12.1.3AnimationInstance의현재시각/Duration처리에서ticks를10000으로integerdivision하는것을확인했다. 제품에privateAPI나보정시간을도입하지않았다. 이는원본속도/곡선의잘못된대응이아닌framework의해상도제약이다.

`source/motion-origin-{reference.html,comparison.json}`/`motion-{web,native}-origin.json`:132개integer시각의실제DOMboundingbox와AvaloniaCanvas부모에대한TranslatePoint4corner를비교했다. Press확대/Spinner회전center, ResultBarleftcenter가원본과대응하며boundingbox차이최대0.000115px(브라우저float직렬화포함). 부모에서잊지않도록고립된public`WebMotion.OriginFor(kind)`에원본값을두고검증probe도이를사용한다. 실제버튼/차트상위는아직조립하지않았다.

`source/motion-curves-{web,native,comparison}.json`:custom/ease/ease-out/ease-in-out 각각101개총404값을actualsourceWAAPI로비교. 차이최대6.14e-7. CSS상태별color/border/visibility/transform합성은원본값을소유하는직접부모의완료검사에서확인한다. primitive에상위상태를임의추가하지않는다.

Windows11ARM기존VM용독립probe패키지를win-arm64self-contained로준비중이다. EULA동의/설치/OS변경을하지않았고VM실제실행은아직없다. 본앱배포/부모변경없음. 다음:패키지빌드확인및검증설명/현재증거정리, 사용자응답후허용된Windows환경검증. 아직LOCK아님.

### 2026-10-06 검증 지점 보존

win-arm64 self-contained 독립 probe 발행이 성공했다(`artifacts/replica/windows-arm64-check`). 설치없이실행하는검증용README/launcher/sourcefixture를포함한다. 실제Windows통과증거는아니다. 현재EULA확인대기이며원본합성의부모별상태는부모조립에서검증한다. controlledclockinteger해상도원인·132origin·404곡선과actualmacOS완료/중단은확인했다. OriginFor원본값을검증probe도사용한다. Windows확인과framework시간해상도제약이남아LOCK하지않는다. 다음독립진전은Typography의globalfractionalpaintphase이며Motion을동시에수정하지않는다.

### 정수 ms 손실의 public API 교정 계획 · 구현 전 기록

Typography Windows 자료/진단 준비를 보존하고 Motion만 활성으로 전환한다. 기존 AnimationInstance의 integer-ms 나눗셈은 source fractional 시각과 실제 값 차이를 만들므로 임의 시간/Duration 보정으로 덮지 않는다. 설치된 Avalonia12.1.3 public `TopLevel.RequestAnimationFrame(Action<TimeSpan>)`은 다음 render tick callback을 제공한다. source duration/keyframes/easing/반복을 그대로 사용하는 고정밀 playback 후보를 이 public callback과 Stopwatch elapsed(TimeSpan.TotalMilliseconds)에 연결한다. 제품에 private clock/reflection/새 패키지를 넣지 않는다.

Opacity/transform은 animation-priority binding으로 source 값만 공급하고 underlying 값은 덮어쓰지 않는다. none 완료/중단은 binding을 해제해 현재 underlying 상태를 복원하고, forward는 caller dispose/visual detach 때까지 최종값을 유지한다. infinite는 detach/cancel에서 정리한다. 부모 UI/기존 Pulse/Enter는 변경하지 않는다. 고립 probe에서 같은132 fractional 시각의 실제 target opacity/matrix를 source와 비교하고 actual macOS 완료·cancel·reduced를 다시 확인한 뒤 판정한다.

### 재개 확인 · public playback

AGENTS/MASTER_RULES/STATUS/현재 명세와 실행 Web의 page200custom/button120custom+transform80custom/origincenter, 후보를 실제 읽고 복구했다. 잠긴 Surface/SvgIcon은 변경하지 않았다.132fractional 후보 비교의 opacity최대4.52e-7/matrix4.38e-6, underlying 현재값 복원 및 reduced 생략을 확인했다. 독립 실제 창 probe를 WebMotion.Play로 연결해9finite완료/2infinite/취소/재생/종료 시 dispose를 검증한다. 기존 Animation 창 증거와 새 방식의 증거는 분리한다. Typography Windows49건은 수령했으며 차이가 있다. 동시 수정하지 않고 후속으로 다룬다.

### public playback 실제 macOS 창 검증

새 public-render-playback 창에서9finite의 Completion이 RanToCompletion,2infinite가 대기 상태로 재생되는 것을 확인했다. C키에서 infinite두Task Canceled 및 opacity1/identity 복원, M키에서11Task 즉시 완료/전부 생략, R키 재생에서도9finite완료를 확인했다. source/motion-playback-window의3개JSON과screenshots/motion-playback-window의3개캡처가 새 방식 증거다. WindowClosed에서 전체IDisposable과token을 정리한다. 기존MainWindow/Pulse/Enter를 변경하지 않았다. Windows단독CPUprobe는 공식prlctl exec 경로로 실행 가능하나 새 패키지 다운로드/해제 완료 전에 첫 실행이 시도되어 파일 없음으로 실패했다. 실패를 검사 통과로 쓰지 않고 완료 후 재실행한다. Windows실제창과부모상태합성은 여전히미확인이다.

Windows11ARM64의같은detached132fractionalprobe가exit0,opacity4.52e-7/matrix4.38e-6로source와일치했다. windows-motion-playback-{native,comparison}.json은실제WindowsCPU/target속성증거이며실제창재생증거가아니다. 사용자에게새fixture의MotionCheck.cmd와C/M/R키검증을요청했고응답대기한다. Motion은WAITING으로보존하고Typography49표본차이교정만활성화한다.

### 공식 CLI/UIA 실제 Windows 후속

강화된 제어 경로로 C/M/R을 전달하고 창 제목과 새 결과 파일 시각을 함께 확인했다. windows-uia-{cancelled,reduced,completed}.json에서2infinite 취소/복원,11effect 생략,9finite 완료+2infinite 재생을 확인했다. 새 검증창 screenshot은 windows-control/motion-uia-client.png. 키 포커스용 TextBlock 변경은 검증 harness에만 있다. 이전 file-only/fullscreen 복사 증거를 새 입력 증거로 사용하지 않는다.

현재 제품 playback 계약은 public WebMotion.Play + IDisposable + Completion이다. 위의 legacy Style.Animations/RunAsync 권고는 새 playback 이전의 조사 기록이며 부모는 Play를 사용하고 detach/종료/cancel에서 dispose한다. source duration/easing/keyframes/origin/underlying 값은 그대로다. 실제 부모의 transition/색/상태 합성은 해당 부모 명세에서 별도 확인한다. Typography는 별도의 미해결 차이가 있으므로 부모 조립은 대기한다.

### 최종 primitive 판정 · LOCK · 2026-10-06

Typography LOCK 후 단일 Motion 검토로 전환했다. 규칙/상태/명세/실행Connection의page200custom·버튼120custom+transform80custom·centerorigin/직접sourceCSS·JS/현재3구현파일/양OS수치및실제창증거를읽었다. 잠긴범위통과. 제품Motion코드변경없음.

확정계약은public `WebMotion.Play` + `WebMotionPlayback.Completion` + `IDisposable`이다. 11 source keyframe효과/정확한duration/easing/fill/repeat/reduced bool/현재underlying값복원/detach·cancel·dispose정리를보존한다. 부모는UIthread에서호출·dispose하고,원본origin으로RenderTransformOrigin을설정한뒤Play한다(`OriginFor`). ModalClose는Completion후제거/해제한다. 부모의CSS색/hover/pressed/transition·기존transform합성/OSreduced조회·spinner정지표시는해당부모명세에서검증한다. primitive완료를그부모들완료로승격하지않는다.

근거: source/motion-playback-comparison.json 및windows-motion-playback-comparison.json의132fractional샘플(opacity최대4.52e-7/matrix4.38e-6,computed직렬화범위),motion-origin-comparison.json의132원본bbox(최대0.000115px),motion-curves-comparison.json의404곡선값(최대6.14e-7). 이전integer-ms손실은새publicrendercallback+Stopwatch timeline으로해소했다. legacyAnimationInstance실험기록은현재제품계약이아니다. 검증전용reflection은detachedtimeline샘플에만있고제품은public API를쓴다.

실제창: source/motion-playback-window/motion-window-{completed,cancelled,reduced}.json(macOS),windows-uia-{completed,cancelled,reduced}.json(Windows11ARM64). 9finite완료/2infinite재생,무한효과취소후opacity1·identity복원,11effect생략,재재생을확인했다. Windows는noncefocus→공식scancode→목표제목+새파일시각확인경로이며기존stale복사기록은증거에서제외한다. 실제parent의상태합성/한프레임픽셀검증은그parent의범위다.

원본과primitive값·시간·기준점·수명계약은검증돼LOCK한다. primitive자체의색상/border/spacing은없으며부모가정한다. 보호해시source/motion-lock.json. 다음은ActionButton 원본조사다. 전체복제재개제약은유지한다.
