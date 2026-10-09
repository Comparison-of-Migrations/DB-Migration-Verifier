# DbmsOption

IN PROGRESS · 2026-10-07 · 단일활성. Notice LOCK 이후 직접 원본 DOM/CSS/실행 Connection만 조사했다. 기존 상위 구현은 조사 후 후보와 비교하며, 원본으로 추측하지 않는다.

## 원본 구조 및 부모

connect(): fieldset.dbms-fieldset → legend + div.dbms-grid(repeat4 minmax0,1fr/gap10) → label.dbms-option 네 개. 각 label은 SVG db25 + span(strong engine, small metadata) + inputradio20/name dbms/aria-label engine. SQL Server/Microsoft·1433, PostgreSQL/PostgreSQL·5432, MySQL/MySQL·3306, MariaDB/MariaDB·3306. KO/EN 모두 카드 자체 문구는 동일하다. 언어는 부모 html lang에서 바뀐다.

root grid 30px 1fr 20px/gap12/pad16/border1/r10/minH88/aligncenter. viewport≤1180: columns1fr20/gap8/pad14/directSVG displaynone/small10. 큰 창 small11, strong13/700/block. 단위없는1.6의줄높이는strong20.796875, small compact16/full17.59375(DPR2). 별도 텍스트 여백 없음. span은 두 block 줄 합계 높이. SVG25는 30column 왼쪽에 정렬(임의중앙배치금지). minH88의 내용 중앙, grid fr의 min-content/nowrap 메타데이터 자동 최소폭 보존.

실제1138창 카드207.5×88/compact: span local15,25.6015625/149.5×36.796875; radio172.5,34/20×20. SVG없음. 원본 CSS 실제 실행에서 source 조사했으며 full mode는 다음 고립 원본으로 최종 계산을 확인한다.

## 상태/상호작용

normal bg#2b2d31/border#7c838c. selected bg#3b3d52/border#8b94ff/inset 0 0 0 1px #8b94ff. hover(선택되어도) bg#383b44/border#a7b1ff, selected inset은유지. 실제SQL Server label텍스트click으로 checked+hover를확인했다. selected icon#b4bdff,기타#b5bac1. strong#f2f3f5/small#b5bac1. focus-visible인radio가있으면label outline3/offset3/#a7b1ff;input자체focus는LOCKED Radio계약도유지. rootbackground/border/shadow transition160ms/defaultCSS ease,역방향전환은CSSshortening. root에pressed/scale/WAAPIpress없음: button[data-action]이아닌label이므로추가금지. inputpressed만Radio가담당. disabled옵션은실행source connect()에없으며미지원엔진을임의추가하지않는다.

label 전체 클릭은 inputfocus→checked→change를 발생시킨다. native는잠긴WebRadio그룹/keyboard계약을사용하고,radio밖label클릭은pointerfocus후true선택만전달한다. checked=false/uncheck 알림은sourcechange로상위에전달하지않는다. 프로그램이초기값을설정할때change를발행하지않는다. aria-label에는engine이름만포함한다. 다른신규animation/UX없음.

상위Connection에남길계약: DBMSchange는SQL1433/Postgres5432/MySQL·Maria3306의양쪽포트갱신,trial.ready/consent/connected초기화,visited connect만,render()의id-basedfocus복원. 카드컴포넌트에DB접속/검사상태재설계를넣지않는다.

## 조립 계획/현재 단계

의존 Radio + Typography + SvgIcon + Surface + Motion(기존CssEase곡선). 지금은 source compact/full 4engine·크기·selected/hover/focus의고립명세수집. 작은strong/small본문→grid row→surface/focus→labelinput전달/transition→양OS대조순서. 기존MainWindow/ConnectionPanel/잠긴코드변경없다. 다음은고립원본과현재MainWindow.HomeConnection의카드차이를읽고위수치를최종확정한다. 서버17927,전체재개제약유지.

## 기존 native 차이 및 고립 조사
MainWindow.HomeConnection Connections는nativeRadioButton/db-card + handdrawn18dot +textStackgap2이며기존텍스트weight600/small11/hiddenSVG/지원MySQLMaria비활성,한정2엔진keyboard순환이다. 원본은20Radio/4엔진/strong700/gap0/fullSVG25/compactsmall10이다. 기존코드를Source로쓰지않으며제품backend지원범위와시안4선택가능상태를분리하여Connection조립시정확한차이를처리해야한다. backend연결지원을완료했다고표시하지않는다.
80고립표본:2언어×2media문맥×3실제창에해당하는폭×4엔진의geometry48, 각문맥selected×hover×focus8의32상태. 원본 CSS를 보존하고고립fixture에media의full/compact크기규칙만복제하여VM viewport차이가geometry문맥을혼합하지않도록한다. selected/hover같은specificity이면원본뒤선언hover가우선하며actualclick으로확인했다. source만수집중/native후보없음. 현재서버54881.

## 고립 원본 → 후보 착수
확정된명세를따라small/strong본문부터컴포넌트내부로만구현한다. 원본의4엔진텍스트·80표본geometry/state는고립Source로수집후대조하며후보완료로표시하지않는다. 카드의자체CSStransition160/default ease만구현,Source에없는전체카드pressedanimation은추가하지않는다. FocusOutlinecenter거리4.5/radius14.5는원본3offset+3stroke와r10에서계산하며실제paint로대조한다. 최종LOCK전label-click/keyboard/group/갱신및정방향/역방향motion을양OS에서확인한다.

## 재개 확인 · 2026-10-07
AGENTS→MASTER_RULES→STATUS→본명세→원본 connect/render/CSS와 실행카드→후보를 순서대로 재확인했다. 16 LOCK 해시 모두 통과. 후보 빌드 경고/오류 0; 80 원본표본의 양OS 수집과 후보 대조를 이어간다. MainWindow는 아직 후보를 조립하지 않았다.

## 대조 진행
양OS80endpoint+80동일객체갱신의geometry/spacing/typography/palette/선택inset/focusendpoint모두통과: source/dbmsoption-{mac,windows}-comparison.json. 강제endpoint는actualinput증거로쓰지않는다. 양OS50sourceCSS/privateproductionclock샘플통과: 색channel직렬화0.51/alpha1, shadowspread1e-5, 역방향curve수치시간1e-5ms허용차를명시. Native43µs등의실시간완료정밀도를측정한것은아니다. 실제Mac본문click/방향키의focus-before-change와단일truechange확인, Enter/Space이미선택상태변경없음. Windows실제키입력진행중,본문pointer는CUAwindow연결timeout으로미검증이며SendInput추가범위확인대기. 미검증이므로LOCK하지않는다. 현재서버88348.

## paint 비교 차이 · 2026-10-07
상태ROI실제이미지에서Radiofocusring은있지만카드외곽ring은모서리만보였다. endpoint속성만으로완료판정하지않는다. outlineDecoratorBounds240×88/Visibletrue이며하위Clipfalse를진단확인. rootUserControl의외곽clip을명시해제하여원본outlineoffset3영역을보존하고ROI를재비교한다. 원본roundedoutline과다른DPI는추측하지않는다.

## 포커스 paint 교정 확인
root ClipToBounds=false를명시한후원본과동일한외곽offset3/ring3/radius곡선이보인다. screenshots/dbmsoption-mac-state-detail.png에서unchecked/selected/selected-hover focus3표본을직접비교했다. 실제geometry/palette교정후80/80재대조pass. 이미지edgeAA차이는있으며pixel-identical을주장하지않는다. Radio public pointerfocus예외는48endpoint양OS회귀/실제refocus양OS통과후재LOCK했고보호해시16개모두통과했다.

## 최종 판정 · DbmsOption LOCK · 2026-10-07
- 최종 후보 src/Verifier.Desktop/WebDbmsOption.cs. 직접 하위 Radio/Typography/SvgIcon/Surface/Motion 계약을 조립했다. 양OS80endpoint+80동일객체갱신에서 모든 local box/spacing/typography/palette/inset/실제outlineVisible 대조 통과. source/dbmsoption-{mac,windows}-comparison.json.
- SourceCSS와 후보 production clock 50샘플씩 양OS 통과. native mount/reduced/제거/재표시9단계씩 및 초기값 silent/checked=true 한 번만 change 전달을 실제 창에서 확인. source/dbmsoption-lifecycle-comparison.json. 실제 frame을 제거 후 다시 칠하지 않으며 재부착은 현재상태 endpoint로 시작한다.
- actual Mac본문click→focus-before-change, 방향키, pointer 재포커스; Windows 승인된고정창 SendInput본문click/밖에서release취소, CLI방향키3→0순환/Enter·selectedSpace유지, pointer 재포커스 확인. source/dbmsoption-{mac,windows}-input/. sourceArrow/같은labelclick의focus-visible true→false 근거는 source/dbmsoption-source-pointer-refocus.json과 Radio예외기록.
- 실제 상태이미지에서 outerfocusring clipping을 찾아 rootClipfalse로 교정. screenshots/dbmsoption-mac-{overview,state-detail}.png를 직접 검토. glyph/edgeAA 차이는 있으며 pixel-identical 및 다른DPI 검증을 주장하지 않는다. 모든상태geometry/paintstyle은양OS실측 대조했다.
- 원본에 카드자체 pressedscale/독립enter WAAPI/disabled옵션이 없으므로 추가하지 않았다. 해당상태는 임의 추가 상태가 아니라 적용안함이다. parent label은 tabstop이 아니고 engine 이름의20pxradio가 이름/focus/group을 맡는다.
- 기존 연결 화면/DB접속 backend는 아직 교체하지 않았다. 4개엔진 선택가능 시안과 실제2엔진지원 차이를 상위Connection조립에서 그대로 명시해야 한다. 이LOCK은backend지원 완료가 아니다.
- source/dbmsoption-lock.json으로 보호한다. Radio pointerfocus 예외는 사전기록/양OS48회귀/actual재검증후 재LOCK했다. 다음단일활성은Field이며전체복제재개제약은유지한다.
