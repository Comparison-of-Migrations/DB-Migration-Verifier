# Choice
LOCKED · 2026-10-08. Review 조립 전 실제 label.choice의 input+plain span 작은 컴포넌트부터 확인했다. 직접하위 Checkbox/Typography LOCKED. Trial consent도 같은 구조다. CategoryHeader의strong/small 변형은 이미 해당부모 계약이며 새plain Choice에 재해석하지 않는다.

## 원본
실제Review tab1 label.choice padding8px0/min-height48/gap12/displayflex/aligncenter/cursorpointer. width904/rootheight48/input20×20(x0,y14)/span14px/22.3984375/400/#dbdee1(x32,y12.796875,자연폭277.2578125). 마지막CSS가초기min44/gap10을override한다. :has(:focus-visible) radius6만있고 border/bg/outline없다. input의focusoutlined/pressed/transition은기존Checkbox가소유. plain span은flex0 1 auto라자연폭까지줄고여유폭에서늘리지않으며minimum unbreakable폭은보존한다. label본문클릭→inputfocuspointer+toggle; input자체클릭은중복toggle없음,Space토글/Enter무토글. 부모의margin20(Review)/16(Trial), Page전체재생성과data-flag복원은부모가소유한다.

## 계획
literal Review/Trial label DOM 한영×2variant×폭200/412/656/904×checked2=32조건(실제수는생성확정), native같은attached표시갱신. label 클릭/checkbox 클릭/Space/Enter/disabled input 및outside release는직접검증; source기존Checkbox 상태계약을추측해서변경하지않는다. 새로운애니메이션없음.

## 최종 판정 · LOCK44
양OS32조건 label/input/root 기하·줄바꿈·선택·문구 차이0(source/choice-{mac,windows}-comparison.json). 실제 원본 label본문클릭→selectedtrue/focuspointerfalse,Space→false/focusvisibletrue,Enter무변화와Mac동일동작을대조했다. native checkbox직접클릭은changes1만증가하여중복없고 disabled본문클릭도변화없음. Mac screenshot에서정확한label영역→밖으로drag후변화없음; Windows공식SendInput의title/path/label기하/foreground/hitProcess/해제guard로본문click→changes2,밖release→changes2유지,disabledlabel→변화없음을확인했다. WindowsSpace/Enter와수명은각key/pointer JSON증거이며초기launch직후레이스/CLI일시실패는성공으로기록하지않고창읽기확인후재검증했다.
양OS native actual갱신/원본CSS의checkboxown상태계약 유지, label 자체animation없음. width/min-content/중앙정렬/sourceNaturalspan폭 일치. 알려진구현차이없고기본OSfont래스터차이만허용했다. 직접하위Checkbox/Typography 보호hash미변경. 다음Review의margin·본문조립/페이지재생성은새부모범위.
