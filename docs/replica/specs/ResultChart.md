# ResultChart

LOCKED · 2026-10-07. 단일활성단위. Row의직접하위는모두LOCKED이고ResultSummaryRow도잠겼다. 먼저실제원본resultChart figure와직접부모results/animateResult만확인한다. 이전앱차트를원본으로추정하지않는다.

## 시작시 확정한 조사 순서 · 완료
실제DOM/CSS/computed figure/heading·columnlabels·rows·caption의bbox/색/spacing·한영/desktop창폭분기와max계산을기록한다. 원본groups에서trial제외,unknown=total-equal-different,maximum=max(5,ceil(max(counts)/5)*5) 규칙을대조한다. 원본없는범위추가/퍼센트성공률재해석/새도표금지. 조립전직접내부heading/labels/caption에독립상태가있는지확인한다. 있으면필요한하위부터검증하며한단위만활성이다. Row의LOCKED계약은수정하지않는다.

## 이미 읽은 직접 원본 근거
source/original.js47 resultChart():figure.chart-heading+unit,aria-hidden result-columns, result-rows button들,figcaption. group필터trial; 공통축maximum 위규칙; label 각분류/3수치/상세. source/original.css45–47과generic h2/p/svg/figure CSS를다시computed값과대조한다. 아직native전체Chart 구현/검증없으며LOCK하지않는다.

## 원본 실행 조사 · 구현 전
source/resultchart-live.json에actual978inner/980outer figure의DOM/computed/bbox를보존했다. 직접head/labels/caption은독립입력·상태가없는부모내부layout이며Row만click을소유한다. figureborder1#41434a/radius10/bg#2b2d31/overflowhidden/marginbottom24. headingpadding20×20×16/gap16/center/space-between; 왼쪽intrinsicblock h2(font16/700/24/spacing-.24/#f2f3f5)+p(font12/19.1953125/#b5bac1/gap4). 오른쪽unit10/17×2/nowrap/right. 실제KOheading83.1953125,왼쪽231.4140625폭/unit76.9765625폭이다.

columns는원본Row동일5열grid(Compact media도동일)/padding12×20 또는12×16, top/bottom1#41434a/bg#232428/font11/17.59375. 내부label flexcenter/gap6/SVG14;전체aria-hidden이다. rows는잠긴Row조립/마지막border0. caption은font10/18,**#dbdee1**(muted아님),padding12×20/topborder1/bg#292b30;원본에compact caption padding override없다. actualKO전체height499.7890625=(83.1953125+43.59375+328+43+2). 상위results metric 숫자는이범위아니다.

JS47/52/53 실제확인: trial그룹제외·unknown계산·5단위공통축; emptycomparison는figure자체가없다. focus=true results진입일때만animateResult의number220/bar320를명시요청하고일반rerender는재생하지않는다. 부모전체page입장animation은Results/상위소유다. 이원본은HTML작은막대이며기존미검증native통계도표나일반차트관례를복제근거로쓰지않는다. WebResultChart 후보는잠긴Row/Count와Typography/Surface/Motion/SvgIcon만조립하며locked파일변경없다. 후보첫배치후양OS·한영·4폭·compact·그룹부분/zero/empty·axis갱신과입장lifecycle를대조한다.

## 후보 진행
WebResultChart를추가하여잠긴Row를조립했다. 빈/trial-only는figure숨김,공통축/3수치/한영/description/aria를동일원본규칙으로갱신하고ordinary update에는입장효과를재생하지않는다. PlayEntrance는잠긴Count에명시전달하며modal요청은분류key이벤트다. column header는입력없는aria-hidden자식이며내부측정glyph처럼접근성children을노출하지않는다. 최초Panel.Render override는Avalonia의sealed멤버로빌드실패하여기존잠긴Surface를감싸border/padding/bg를조립하는구조로교정했다. source조사→기록후구현했으며현재첫빌드/geometry/actuallifecycle 검증중이다. 아직LOCK하지않는다. 다음은원본function그대로실행하는한영/폭/compact/부분그룹/zero/empty/5단위축 fixture와native배치대조다.

## 재개 · 2026-10-07
AGENTS→MASTER_RULES→상태→활성명세→직접원본JS47/52/53·실행figure→후보와probe 순서로복구했다. 22개LOCKhash가그대로다. Mac38geometryfixture 실행/원본record 저장완료; 비교는아직대기. bridge allowlist source-resultchart 추가후 정확한fixture_server프로세스31519를닫고PTY3474로재실행했다. Windows사용허용을반복질문하지않고기존scopeguard를유지한다.

38최초비교의328실패는probe의Select 지연평가로이미제거된row의좌표와최종case column좌표를읽은증거오류였다. layout/style값에근거없이product를바꾸지않고현재case에서ToArray snapshot하도록probe만교정한다. 실패기록은합격으로사용하지않는다.

양OS38 geometry/update 비교가0실패다. sourceJS를그대로실행한9lifecycle 원본과native15attached phases를저장했다. Windows길어진invoke CLI가PrlJob오류로입력전실패해원본버튼새기록이없음을확인한뒤동일title/processguard의짧은명령으로실행하여fresh9기록을얻었다. native15phase는양OS정상종료했고실제4분류click도Mac완료. 차트h2의원본HeadingLevel2와columns aria-hidden을조립속성으로명시한다(잠긴Typography/Surface수정없음).

## 최종 조립 계약 · LOCK
구현 `src/Verifier.Desktop/WebResultChart.cs`. 직접하위 ResultSummaryRow/Count, Typography, SvgIcon, Surface, Motion의22LOCKhash 불변. SourceJS47과computed원본을따라 figure→heading→aria-hidden5열label→rows→caption을조립했다. 원본과동일4분류/3수치/공통최대축; trial은따로판정하며차트에넣지않는다. 빈범위는figure숨김/row제거. 한영과Compact은부모입력이다. Compact은원본≤1180viewport분기를부모가정하고전달한다. 모든수치가0이어도최대축5이며zero숫자도원본입장애니메이션대상이다.

`SetGroups`는원본rerender처럼기존row를제거·새로조립하며이전재생은취소/identity복원된다. ordinary rerender는입장재생하지않는다. Results부모가focused entry에서만`PlayEntrance(reducedMotion,cancellation)`를요청한다. `DetailRequested(categoryKey)`는원본data-action detail:key와동일하며modal body/returnfocus는ResultDetails/Results소유다. 원본h2 HeadingLevel2를조립속성으로지정했다. 단위표시두줄/캡션줄바꿈/마지막row border0/overflowradius10은원본계약이다. Top-level page/metric animation은여기서추가하지않는다.

## 완료 근거
- source/resultchart-{mac,windows}-comparison.json: 각38geometry/update, failed0. KO/EN×4창폭×wide/compact, 부분범위·중단·미시작·zero·empty·trial-only·5단위축경계. caption wrap/heading/unit/5열label/row/captionbox와안전한표시문자열·aria-name까지비교했다.
- source/resultchart-{mac,windows}-source-lifecycle.json: exact original resultChart+animateResult 함수로각9실행원본phase. source수치12×220ms,bar12×320ms/sourceease, ordinary rerender에서는효과없음. trial999를섞어도comparison축20.
- Mac native/resultchart-lifecycle.json, Windows source/resultchart-windows-lifecycle.json: 각15attachedphase. 입장시작/실제frame재생/완료,reduced,외부취소,row교체,detached/reattached/replay,empty/trial/axis갱신.
- source/resultchart-lifecycle-comparison.json: source18/native30phase/actual8요청,failed0. macCUA실제4클릭/WindowsguardedUIA의4Invoke가각data/schema/access/objects 이벤트로연결되었다. 이실제입력은모달내용검증이나Windowspointer4개추가검증이라는뜻이아니다. whole-rowpointer/Space/Enter/hover/pressed/focus/timing은잠긴Row의48actual/양OS72timing 계약을계승한다.
- screenshots/resultchart-paint-pair.png는원본CSSpixel사진과native2x비교로직접보았다. Windows resultchart-windows-native.png도직접보았다. 양OSglyphAA/subpixel/스크린샷리사이즈의bit-exactraster 동일을주장하지않는다. Surface/Count/Row의색/paint/정렬계약은잠긴증거를계승한다.
- Mac 최신heading속성지정뒤38다시0실패, Windows최신assembly hash갱신뒤38다시0실패. 빌드성공. 실제inputnative창은양OS정상종료했다. source측정실패CLI/지연평가실패는합격에사용하지않았다.

미확인컴포넌트내부차이 없음. 부모소유 MainWindow/Results/ResultDetails의조립은아직TODO다. 별도LiveCharts재설계는복제기준이아니다. 다음의존단위 Sidebar. 전체복제미완료이므로AGENTS임시재개제약유지.
