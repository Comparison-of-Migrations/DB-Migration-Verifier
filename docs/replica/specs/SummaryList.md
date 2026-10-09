# SummaryList
LOCKED · 2026-10-08. 직접하위SummaryItem/Surface LOCKED. 실제Review tab1 부모 .panel.panel-pad.summary-list computed grid/gap0/padding26/border1#41434a/radius10/background#2b2d31/shadow0 2px4px#00000010. 전체904×383, 내부850×329(row55×5+54). Save도같은panel조립이며research모달은plain .summary-list로부모dialog padding에서위치를정한다. Source CSS초기padding24/shadow0 1px3 #12는최종26/shadow0 2px4 #10으로override되어있다. 부모viewport<=1100이면label열130, 그밖170; 직접원본CSS media규칙을전달한다. 다음: 원본Review/Save/research 실제함수로조립fixture → 전체list/row기하·shadow·행갱신 → 양OS검증 → LOCK → 각페이지조립.

## 구현/측정 진행
WebSummaryList Decorator→protectedSurface→RowsPanel→lockedSummaryItem. panel=26padding/1border/10radius/#2b2d31/shadow0 2 4 #10, plain=0padding/no-border/no-shadow. rowgap0,lastborder0. sourcegrid implicit 열의auto min-content는row width0 probe의ValueControl.DesiredWidth+고정label+gap으로구하고최소column폭이넘으면모든row폭을함께확장한다. source120자SQL Server예시DB명도fixture에넣어원본overflow기하를확인한다. parser에CSS px를그대로준최초실행은FormatException이므로BoxShadow object의숫자속성으로조립값을지정했다(동일source값, 성공으로기록안함). 72case source(first24 Review/Trial/exclusions/120name/Save/research×한영×3width×2actualmedia).

72 Mac 첫대조: panel기하/문구/height/shadow/plain조립은일치. 120자EN DB명의implicitgrid min-content 폭6상태에서source1060.3046875/native1060.30078125(1/256) 차이로전row너비72필드만불일치. source최소트랙은DPR×64 단위의상향intrinsic폭을사용하므로부모RowsPanel 최소폭만ceiling으로양자화한다. rowpadding/text/shaping과LOCK범위는변경없음. 전체72 재검증 후Windows진행.

## 최종 판정 · 2026-10-08
SummaryItem 예외교정/reLOCK 후 양OS 최신72 조립조건 차이0(source/summarylist-{mac,windows}-comparison.json). Review/Trial/빈선택/120자명/Save/research ×한영×3폭×2실제viewport의panel/plain/root/row/label/value/lastborder/shadow기하를비교했다. 같은attached list에SetPresentation72회로행수/표면/media변화를검증. 양OS compact KR 및 long EN 실제PNG를검토했다. 긴unbreakable명 overflow는원본그대로이며 기본font raster차이만OS표현경계로허용한다. 원본처럼List자체의입력/focus/animation없음. 보호하위40개미변경, Row별도교정완료. LOCK.
