# SummaryItem
LOCKED · 2026-10-08. PageHeading LOCK39 이후 작은 요약행부터 착수한다. 직접하위 Typography/Surface LOCKED. 원본 .summary-item(span label + strong/div value), Review/Save/research모달. 원본실행DOM/computed와공통CSS·본문변형을먼저기록하며 기존native로추측하지않는다. 다음: 실제 DOM → 명세 → 한영폭/마지막border/mono+small/br → native → 양OS대조 → LOCK → SummaryList 조립.

## 원본 실제 조사
원본tab1 Review의6행: .summary-item gridcolumns170px1fr/gap20/padding16px0/borderbottom1px#41434a, lastchildborder0, align-items:start/font13/line22. labelspan13/22/#b5bac1, strongbody13/22/700/#dbdee1, divbody13/22/400/#dbdee1, mono12/22/tnum 및small같은font12인라인/#b5bac1. 실제wrapper850,row55/마지막54,label170/body660,y16. originalCSS @media(max-width:1100px)의labelcolumn130은뷰포트기준이며row폭으로추측하지않고부모가전달한다. br은강제줄바꿈; white-space:normal은내용별wrap. 직접하위Typography/Surface/InlineText 모두LOCKED. 다음: 원본Review/Save/research resource 행32? 실제case수는생성시확정 → 구조명세 → 구현 → 양OS대조.

## 구현/첫 대조
WebSummaryItem은sealed 보호Surface를상속하지않고Decorator→Surface(borderbottom/padding16)→Contents(label/value)을조립한다. label고정170/130, gap20, start정렬. value는일반13/22,strong13/22/700, mono InlineText12/22, br은각줄을쌓는다. sourcegrid1fr의min-content바닥은보호Typography width0 measure로unbreakable폭을얻어마지막column overflow도보존한다. readonly줄관찰이외LOCK수정없음.
실제Review6행/시험br/빈선택/제외없음/Save2행/research4행/긴DB명=16 templates, 한영×3폭602/850/1006×last2×viewport1000/1400=384. iframe의실제CSSmedia쿼리로130/170을검증하며row폭으로media를추정하지않았다. Mac384 기하/문구/색상 차이0(source/summaryitem-mac-comparison.json). Windows 동일조건 fresh출력을검증중.

## 최종 판정 · 2026-10-08
양OS384조건 모든row/label/value 기하·문구·color·borderbottom 차이0. sameattachedinstance에서SetPresentation384회로strong/plain/inline/br/언어/last/media컬럼교체를검증했다. 양OSmono-wrap와Save행actualpaint PNG를검토했고Source실행Review DOM의label/value좌표 및baselinealign-start와같다. Component에입력/focus/hover/animation없음(원본도없음). 기존Typography/Surface/InlineText는임의수정없음. 증거source/summaryitem-{mac,windows}-{source,comparison}.json 및native/summaryitem-native.json, summaryitem-wide-{ko-1,en-10}-602-false.png. 기본OSrenderer래스터차이외알려진차이없음. LOCK. 부모SummaryList의border/padding/shadow/lastchild조립은다음범위다.

## LOCK 예외 · 2026-10-08 SummaryList min-content
변경이유: 실제원본SummaryList120자DB명에서Windows자동열source791.71875에비해native최소값0으로계산하여전체row602로남는다. WindowsTextBlock가constraintwidth0일때glyph line의폭관찰을0으로처리하므로기존Row384(최소폭보다더넓은본문)에서는드러나지않았다. 영향: Contents.Measure의min-content probe constraint만0→1/(actualDPR×64). source근거: SummaryList literalReview120자한영·source actualiframe grid child폭 및72개대조172불일치. 원본의최소unbreakable단어폭을관찰해야한다. 배치/스타일/text/보호Typography 변경없음; protectedNotice도같은positive CSSunit probe를사용한다. 수정전기록, SummaryList WAITING CHILD/Row 단일활성으로돌려양OS384회귀후재LOCK한다.

## 재개 확인 · 2026-10-08
AGENTS/MASTER_RULES/상태/Row·List 명세와 실제 원본 SummaryList iframe 및 Save 부모DOM, 관련 CSS, 현재 두 native 파일을 다시 확인했다. 보호 hash의 SummaryItem 한 항목만 기록된 positive-unit 예외로 변경되어 있다. Windows 최신384 및 부모72 차이0 증거를 보존하고 macOS 동일 코드 회귀부터 재개한다. 전체 재설계하지 않는다.

## 예외 회귀 완료 · 재LOCK
positive-unit probe 교정 후 최신 동일코드 양OS384회귀와 부모SummaryList72회귀 모두 차이0. 기존행배치/스타일/줄바꿈 증거가 유지됐고 Windows120자 min-content 경계 차이도 없어졌다. 단일파일hash를갱신하여재LOCK.
