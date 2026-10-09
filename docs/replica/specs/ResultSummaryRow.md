# ResultSummaryRow

LOCKED · 2026-10-07. 직접 반복 내부 ResultCount를 잠근 뒤 시작한다. 원본 resultChart()의 button.result-row, result-category/icon/strong/small,3×result-count와 row-chevron/sr이 범위다. chart heading/column headers/caption/축계산/전체figure는 부모. locked SvgIcon/Typography/Surface/Motion/ActionButton/ResultCount 계약을 확인하고 한 단위만 변경한다. 기존 전체앱의 미검증 Result row를 원본 근거로 사용하지 않는다.

## 초기 조사 계획 · 이력
실제 original result 화면의4분류행 DOM/CSS/computed sizes/공통 grid/whole-button hover·focus·pressed·click/keyboard·reduced 및 count 갱신을 조사하고 명세를 기록한다. 모든 하위는 LOCKED다. source 8765는 manifest와동일한 보존HTML로복구했으며 원본버전을바꾸지않는다. 아직 native row 구현/검증 없음.

## 실제 원본 조사 · 구현 전 계약
actual original978px 행4개의 직접 DOM/CSS를 확인했다. 각 button.result-row가자체grid와전체clicktarget이며 내부 .btn/ActionButton 자식은없다. 기존ActionButton은.btn label/variant/sizing 계약이므로 LOCK 예외로 바꾸지 않고 SidebarItem/CategoryHeader와같은 전용 plain Button을 조립한다. 의존지도에서 잘못된 ActionButton 직접의존을원본근거로정정했다. category와chevron은독립입력상태없이그행모델/배치에귀속되는직접내부layout이다.

grid minmax(170,1.4fr)+3×minmax(90,1fr)+18, gap20, padding12×20, center, height>=82. bottom border1(#41434a), last0: last의세로센터는 다른행보다.5px내려간다. rootfont14/400/line22.4. title13/700/line20.8/#f2f3f5; small10/400/line17/gap3/#b5bac1. category fullcellflex gap12,30×30transparenticon(radius8),SVG19 center. number은잠긴Count의45.1953125h. chevroncell은18width이지만라인박스22.3984375h이며SVG18 vertical-align:middle actualMac Y3.3125; 단순수직center가아니다.

rest transparent, hover#353740/boxshadow:none; hoverchevron translateX2 120ms sourceease. button generic active scale.98 80ms sourceease와click시160ms ease-out press WAAPI;행hover배경120ms sourceease. focus-visible outline3#A7B1FF/offset-3. source실제행은disabled되지않으며표시분류status를클릭가능으로유지한다. disabled의genericCSS경계는별도fixture에서확인하고미검증이면적는다.

aria-label은분류명+different/unknown/equal3수치+세부보기, sr Details는그label로대응한다. click은그분류의상세열기요청1회며modal내용/닫기/focusrestore는상위소유다.

source bbox978의열폭267.265625/190.9140625/190.90625/190.90625/18과마지막0.0078125남는공간을확인했다. 하드코딩하지않고CSS누적track layout quantization을후보에적용해610/858/978/1058,KO/EN,4category,rest/hover/focus 총96행을비교한다. 아직native/비교완료없다.

## 첫 후보와 차이 교정
WebResultSummaryRow의private category/chevron/grid는 잠긴Typography/SvgIcon/Surface/Count를조립하고 전용plain Button의input/state를소유한다. source glyph의vertical-align:middle은14px systemfont strut baseline−xHeight/2−9를CSS unit floor로배치해actualMac SVG Y3.3125와일치했다. CoreText/Skiafontmetric을읽으며OS font/설정변경없다.

첫96Mac 배치대조에서610/1058의fractional grid track이1CSSunit씩달랐다. double 누적 floor 근사는채택하지않는다. [Chromium grid flexible track algorithm](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/core/layout/grid/grid_track_sizing_algorithm.cc)의raw LayoutUnit/float fr_size/leftover carry를직접확인하여동일계산으로교정했다. Frozen170과각최소90을보존한다. 폭별수치를하드코딩하지않는다. native후보재게시/96대조부터재개하며기존21LOCK은그대로다.

현재 source bridge는PTY26892(6874/22129종료), source-resultrow.json만allowlist추가했다. 실제original8765는유지한다. 아직Rowtiming/actual input/partialstatus/disabled및Windows검증은미완료이며LOCK하지않는다.

## Windows 첫 대조
Windows96개새원본/actualnative를대조한결과모든grid/숫자/한영title/description은일치했지만SVGvertical-middle이각행1CSSunit 위에있어96실패를기록했다. source는baseline에서half x-height의LayoutUnit값을빼는데후보는raw값을뺀최종y만floor했다. half x-height를먼저CSS unit으로변환하는일반계산으로교정하고양OS재검증한다. OS별수치보정은없으며미검증을통과로올리지않는다.

같은probe에실제전체행입력모드를추가했다. 행label/수치/chevron,키보드Enter/Space,drag cancel/peer,partialstatus/count/language/reduced갱신을제목/path/focus/geometryguard로검증할예정이다. 아직actual input/timing은미확인이다.

## vertical-middle 반올림 근거 · 이전 floor 후보 폐기
half-xHeight floor 후보는Mac96개에1unit 차이를새로만들어실패했다. 원본측정에0폭baseline marker와1ex 진단을잠깐추가·제거해실제baseline/xHeight를분리했고측정후DOM을원상복원했다. nativeMac strut baseline16/xHeight7.369140625, source baseline16/1ex layout7.3671875를확인했다. [Chromium inline_box_state::vertical-align middle](https://chromium.googlesource.com/chromium/src/+/98c96b6520f8669d93842c0af056859c93820c3e/third_party/blink/renderer/core/layout/inline/inline_box_state.cc)은parent font의half x-height에FromFloatRound를적용한다. 해당일반round계산으로교정한다. 단순최종floor/half-floor두후보는합격으로사용하지않는다. 양OS96대조를다시실행하고이후actualinput/timing을진행한다.

## 재개 확인 및 현재 다음 행동
AGENTS/MASTER_RULES/현재상태/본명세/직접 원본CSS·JS/현재후보를실제확인하고21LOCK해시통과. nearest half-xHeight 후보의Mac96 strict대조는실패0, 새Windows게시·대조중이다. 기존floor후보실패는유지하며합격근거로쓰지않는다.

원본CSS47의viewport<=1180 분기를추가확인했다. 이는모바일추가가아닌desktop창폭분기다: category155/1.4fr, count75/1fr, chevron16, gap16, padding-inline16, categorygap8. 현재후보wide-only는미완료로기록한다. 부모가Compact를전달하고범위의두배치를모두검증한다. 추가로중단/미시작설명과disabled boundary,actual입력/timing을확인한뒤에만LOCK한다. source live에서데이터행click후dialog와활성닫기button을확인했으며modal내용·닫기·복귀는상위소유다.

## 확장 대조 · 230개 및 전환
wide/compact 192개,중단/미시작32개,disabled6개를현재원본computed값과대조했다. Mac230개실패0. disabled는후반CSS의#3b3d44로기초CSS값을덮어쓰므로후보를교정했다. Chrome matrix 직렬화6유효숫자 때문에chevron timing최대차4.91e−6px를그정밀도경계5e−6px로검증하며native계산을근사값으로바꾸지않았다. Mac72개전환/역전환실패0;Windows재게시·대조진행중. actual창의cases.Single은compact추가후중복선택으로실패했으며wide/enabled필터를추가해검증도구만교정했다. 실패한launch를입력검증으로기록하지않는다.

## actual 입력 중 발견한 교정
첫Windowspointer확인에서UIA로이미focus한행을마우스로눌렀을때focus-visible이남았다. 원본pointer modality에맞춰PointerPressed에서clear하도록교정했다. Mac첫창은초기requests0/enabledtrue가아닌변경상태를관찰했으므로해당첫기록을합격증거로쓰지않고두OS창을닫고새초기상태부터대조한다. opacity0 측정용M이MacAX에도나와privatechevronpeer에서측정용내부children을노출하지않는다. product새디자인/lockedchild변경은없다. 현재candidate양OS재게시후배치·actual재검증이필요하다.

## 교차 OS 입력 분리
Mac lifecycle 첫자동실행의click-completed에서fixture ReducedMotion이자발적으로true로관찰되어second-playing이실행되지않았다. 같은시점Windows실제key runner가M/D/U/E를전달중이었다. 간섭경로는단정하지않지만OS입력은독립으로취급하지않고Windows키입력종료·창닫기후Mac lifecycle을새창에서다시실행한다. 해당첫lifecycle은실패이며완료근거로사용하지않는다. 빌드/원본read처럼입력에영향없는작업만병행한다. 브라우저viewportoverride는기존tab에적용되지않아원본1280유지로관찰했고reset했다;iframe의실제1180/1181 viewport와원본CSS media에서각각compact/wide gap·padding을확인했다(resultrow-iframe-viewport.json).

## lifecycle cold-window 측정 교정
양OS실제입력24snapshot씩은실패0였으나Windows새자동창의첫click-playing45ms시점은초기font/raster paint가UIthread를늦춰이미완료된상태를기록했다(2실패). 두번째playing/취소/복귀는확인됐다. 제품시간을늘리거나검사를면제하지않고새fixture의첫paint가끝난뒤측정하도록시작전500ms안정화만추가한다. 양OS9phase를동일새probe로재실행한다. productRow/Motion은변경하지않는다.

## 최종 계약 및 LOCK · 2026-10-07
구현: `src/Verifier.Desktop/WebResultSummaryRow.cs`. `SetResult`는분류/설명/수치/공통축/언어와aria이름을한행에서갱신한다. plain전체Button,독립peer와분류key를유지한다. `Compact`는부모가전체viewport<=1180에서전달한다. 부모figure/gridcolumnheading/축산정/차트caption/행입장animation/실제modal조립은ResultChart/상위소유이며이LOCK의완료주장이아니다.

wide/compact의정확한grid float-remainder계산, category30/19·gap12/8, title13/700/20.796875,description10/17·gap3,잠긴Count45.1953125,Chevron의inline font-middle과마지막행bottom-border0에따른.5px센터차를검증했다. Rowheight>=82;gap20/16,padding12×20/16. source의disabled최종색#3b3d44/hover제외와hoverchevron2px조건을그대로유지한다. 포커스inside3#A7B1FF,pointerclear/keyboardvisible. 내부측정glyph M은AX에노출하지않는다.

증거:
- `source/resultrow-{mac,windows}-comparison.json`: 각230geometry/update 실패0(192wide/compact KO/EN 4폭·4분류·rest/hover/focus,32중단/미시작,6disabled endpoint). source/native JSON에원본bbox및현재동일instance연속갱신을보존했다.
- `source/resultrow-{mac,windows}-timing-comparison.json`: 각72 CSS hover/press/chevron와30ms역전환실패0. productiontrack을원본실제transition clockseek와대조. 색channel1/255,scale1e−6,6유효숫자matrixtranslation5e−6px,reverse-duration1e−4ms 직렬화정밀도범위.
- `source/resultrow-input-comparison.json`: 양OS24actual snapshot씩(48)과9attached lifecycle씩(18),총66 실패0. guarded title/icon/count/chevronclick 각1요청;hold/release·outsidecancel;독립peer·Enter/Space;disabledpointer0요청;reduced/data/language/복귀. UIA focus로hover전에focus-visible을설정하는도구상태는원본hover의focus변화로주장하지않는다.
- `source/resultrow-original-actions.json`: 실제full원본행/수치/chevron/categoryicon/Enter/Space6개에서해당분류dialog가열렸다. 닫기후원본focus가행으로돌아가는상위근거도확인.
- `source/resultrow-iframe-viewport.json`: 실제1180/1181 iframeviewport에서원본media분기gap/padding/categorygap/columns확인. IABoverride기존tab적용실패기록은별도보존하며합격근거가아니다.
- `screenshots/resultrow-paint-pair.png`과`resultrow-windows-native-row.png`를실제열어배치/아이콘/수치/track/border를확인했다. webclip은CSSpixel, nativePNG는DPR2이며출력AA/소스캡처리사이즈를bit-identical로주장하지않는다. 부모figure의radius/border는다음단위소유다.

입력검증창은종료했다. lifecycle은실제붙은native창의production OnClick을호출하여playing/완료/playing중detach취소/재부착/reduced를검증했으며추가입력simulation으로포장하지않는다. 새창첫font/rasterpaint이후500ms에시작하고mac/windows입력은직렬로진행했다. 실패기록과교정이력은위에보존한다.

원본에selected/expanded/busy Row상태가없어새상태를추가하지않았다. 시험동작trial은원본resultChart에서제외되어별도trialResult부모범위다. 단위범위의미확인차이는없다. 전체앱/차트조립은미완료다. 기존21LOCK은변경없으며Row파일전체hash를새로보호한다.
