# MatchingDialog
LOCKED · 2026-10-08. 독립 원본21탭 Selection 대응 변경을 실제 클릭해 오른쪽 drawer를 열었다. 실제 mapping dialog가 matchingOptions label 카드로 조립되므로 작은 MatchOption부터 검증·LOCK한 뒤 모달을 조립한다. 검색/일대일 제약/적용·취소/포커스 복귀/모달 스크롤/애니메이션은 부모 범위이며 원본DOM/CSS/JS 확인 기록은 해당 착수 시 이어간다. MatchOption의 양OS 검증·LOCK 완료 후 현재 active는 MatchingDialog 하나다.

실제 원본21에서 선택한 archive.orders 카드 다음 disabled demo.orders 본문을 클릭하면 activeElement는 DIALOG로 이동한다(라디오 선택은 보존). 네이티브 고립 MatchOption canvas에는 focusable dialog 부모가 없어서 마지막 radio focus를 유지했다. 부모 모달 조립에서 focusable root·비활성label/빈부분 포인터에 대한 모달포커스 복귀를 원본과 대조해야 한다. 이것을 OS예외로 면제하거나 고립카드 전체모달 검증완료로 주장하지 않는다.

## 재개 · 2026-10-08
AGENTS → MASTER_RULES → COMPONENT_STATUS → 본 명세 → original.css/JS mapping/close/search → 실행 원본21 → 기존 MainWindow.Matching.cs 및 직접 하위 계약/코드를 실제 확인했다. verify_lock.py의30보호범위 통과. 설계를 초기화하지 않았다. 기존840×640 별도 창·컬럼선택 흐름은 웹 drawer의 근거로 사용하지 않는다.

## 원본 부모 계약
실제1280×720/DPR2의 초기 drawer: x760/y40/width500/height640. border1 #535963, radius14/0/0/14, shadow0 18 48 black alpha36. right20; width=min(500,viewport−32), maxheight=viewport−48, 수직 auto margin. 기본 bodyfont14/22.4. backdrop #0e0f1480 및 blur2. headpadding20/24, gap20, center, bottom border1 #41434a: height85; h2 18/27/700/letterspacing−.27, close44. footer topborder1, padding18/24, gap12, end, background#232428; buttons stretch48, primary minwidth164: height85.

body padding24/maxheight65dvh/overflowauto. 720높이에서는468이며 scrollHeight603. 원본 초기 scrollTop0; 방향키로 archive focus시 body19로 이동하는 실제 중첩 scrolling이 있다. match-source border1 #4b4f59/radius8/bg#232428/padding14/16; small12/19.2 marginbottom4 + 부모14/22.4 strut의 inline mono12. 실제height75.59375. browser topmargin20; h3 15/24/700. 잠긴Matching SearchInput margin10/14. list grid/gap8/padding4/maxheight300/overflowauto: 원본4카드 높이67.7890625, content303/client300. query-note top10/minheight22/font12/19.2. 마지막 small paragraph top16/font12/19.2. 실제 카드값과 부모 기하를 각각 대조한다.

검색 trim/lowercase/contains, 순서 유지; 검색은 draft를 바꾸지 않으며 숨겨진 선택을 적용할 수 있다. 새로운 옵션 DOM로 대체; note는 검색 안내로 변경. 없으면 p.table-empty padding28/font14/22.4/center, list86.3984375. 실제 ARCHIVE 검색은 list75.7890625/body378.578125/dialog550.578125. empty 검색은 body389.1875/dialog561.1875. 값 있는 search Escape는 검색만 지우고, 빈 search Escape는160ms close 후 호출 버튼으로 복귀. 취소는 mapping 변경 없음. apply는 현재 전체 maps의 다른 source 충돌을 재확인하고 오류Notice 또는 commit을 즉시 요청한 후 close한다. backdrop 클릭으로 닫지 않는다. showModal의 첫 포커스는 closebutton. Tab은 모달 내부; 라디오 그룹은 locked Radio 계약.

## 조립·남은 검증
WebMatchingDialog를 별도 후보로 조립한다. MatchOption/SearchInput/IconButton/ActionButton/Typography/Surface/Motion/Notice 코드는 변경하지 않는다. ScrollViewport는 잠긴 수직 overflow 계약을 새 중첩 부모에서 사용하되 새 부모의 폭/gutter/scroll/focus를 별도로 확인한다. 실제 앱의 모달 호스트·배경 blur·열기/닫기·포커스 trap/복귀를 포함한 검증 전 LOCK하지 않는다. 현재 구현 및 독립 원본 모달 fixture를 진행한다.

## 현재 차이·검증 · 2026-10-08
압축 후 본 규칙/상태/명세/직접 원본 CSS·JS/실제 drawer 및 후보를 다시 확인했다. 30개 LOCK 보호 범위 모두 유지. macOS 48개 독립 언어·창 크기·검색 조건의 기하/문구/선택/scroll 대조 차이0. 실제 키보드 방향키로 archive 이동 시 body19/list0, Tab 순환과 검색 Escape·숨겨진 draft 적용을 확인했다. 최신 실제 창에서 적용 직전 충돌은 모달을 유지하고 Notice를 표시하며 Applied 이벤트를 발생시키지 않았다. 취소는 background enabled/effect와 opener focus를 복원했다. reduced open/close flag와 실제 닫힘을 기록했다. 증거: source/matchingdialog-mac-input/{keyboard-right-final,tab-cycle-final,collision-rejected-final,cancel-background-restored-final,reduced-open-final,reduced-close-final}.json.

발견한 앱 구현 차이: inner viewport clip이 양쪽13px라서 source의 left13/right0과 다름. 부모 clip만 교정한다. 하위 Surface 변경 없음. shadow 비교의 이전 제외를 제거하고 실제 후보 shadow를 수집해 엄격히 대조한다. 교정 후 Release 빌드 성공, macOS48개 geometry/state/shadow 비교 차이0. source/matchingdialog-mac-comparison.json 및 matchingdialog-mac-native/matchingdialog.png(모서리 직접 확인) 보관. CSS computed shadow alpha는 두 자리 직렬화이며 raw alpha36/255를 후보에서 같은 형식으로 수집했다. 옵션 viewport 이름을 target heading에 연결하고 query note를 Polite live로 설정했다. 최신 실제 AX에서 이름 있는 선택 목록·초기 close focus 확인(source/matchingdialog-mac-input/open-accessible-list-final.json). 보호된30범위 변경 없음.

이전 Windows blocker: current-user CLI empty exit1, read-only SYSTEM 진단 explorer0/LogonUI1. 이번 재개에서 직접 로그인된 current-user ver 성공으로 세션이 복구됐다. SYSTEM GUI를 쓰지 않고 최신 후보 hash 검증 배포 및 source/native48 수집을 완료했다. 실제 입력 검증 완료 후에만 LOCK한다. full Application 모달 배경 접근성 branch 격리는 상위 host에서 대조해야 하며 현재 후보는 background input disabled/blur/focus 복원까지 확인했다.

다음 행동: Windows 직접 로그인 완료 후 current-user 접근 재확인 → 최신 win-x64 publish assembly hash 검증 배포 → matching_dialog.py source-open/source-record/prepare/auto로48조건 수집 → compare_matching_dialog.py windows → 실제 검색/선택/닫기/포커스/충돌/reduced 및 부모 기하 대조. Windows 미확인으로 IN PROGRESS 유지하며 전체 임시 재개 제약은 제거하지 않았다.

Windows 재개 준비: 최신 후보 win-x64 self-contained publish 성공(0 오류). artifacts/replica/windows-arm64-check는 역사적 이름이며 실제 RID win-x64. VM 안의 기존 후보는 아직 교체하지 않았으므로 prepare 이후만 최신 Windows 검사로 인정한다. macOS 실제 검증 창은 종료했고 source21/27은 다음 재개용으로 보존했다.

## Windows 검증 · 이번 재개
AGENTS/MASTER_RULES/상태/본 명세/직접 source CSS·JS/후보 및 WindowsControl 경로를 다시 읽고30LOCK 보호 범위를 확인했다. 원본 페이지 로딩 전 record 버튼 조회 실패는 통과로 쓰지 않았다. 확인된 record 버튼을 준비 제한8초 안에서 기다리게 했고, idempotent 측정에서만 기존 CLI Invalid argument의 제한 retry를 적용한다.

최신 Windows source/native48: 32조건 원본 기하/상태/문구/shadow 직접 일치. 아래 원본 최소높이보다 작은 600px의16조건만 root classic scrollbar 때문에 전체 drawer 위치가 달라진다. 실제 source 환경을 JSON에 추가 측정했다: html/body min-width960/min-height720, inner960×600→client945×585, inner1280×600→client1265×600. 수직15px/수평15px root gutter가 원인이며 x15/y7.5 또는 x15/y0 이동만 존재한다. 모든 내부 box의 크기/상대위치, 글자/선택/scroll은 일치한다. 최신 사용자 OS 기본 차이 허용에 따라 추가 native 위치 보정은 하지 않는다. 비교기는 raw396개 차이를 보존하고 실제 환경의 정확한 root 이동만 분리한 뒤 .001 기하 기준과 문구/상태의 엄격한 비교를 유지한다. 미해결차이0, allowedOSOffsets16(source/matchingdialog-windows-comparison.json). OS 원인을 확인 못한 차이를 일반 면제하지 않는다. PNG matchingdialog-windows-native/matchingdialog.png를 직접 열어 확인했다.

Windows 실제 현재: 초기 close focus, 방향키로 archive 선택/중첩 body19·list0, 검색0개/draft 보존, 검색 Escape clear와 빈 검색 Escape close/opener·background 복원, close→search→선택radio→cancel→apply→close Tab 순환을 확인했다. source/matchingdialog-windows-input의 현재 fresh JSON에 기록. 숨겨진 draft 적용·취소·충돌·reduced를 이어 확인한다. 전체 Application의 배경 accessibility branch 격리는 부모 host의 명시적 조립 계약으로 남으며 본 standalone background disabled/blur/focus 복원과 구분한다.

## 최종 판정 · LOCK
양OS 각각48개 drawer geometry/text/state/shadow를 대조했다. Mac raw 차이0, Windows supported-height32조건 raw차이0/600px16조건의396개 raw 위치차이는 측정된 root OS scrollbar offset으로만 구분하며 내부 .001 기준·상태·문구 차이0. 원본 CSS/JS나 후보의 geometry를 OS예외 목적으로 변경하지 않았다. 모든 raw/허용 내역을 comparison JSON에 보존한다.

실제 Mac/Windows 입력 증거는 각 matchingdialog-{os}-input/: 첫 close focus, available 선택·unavailable 본문 선택 차단과 모달root focus, 그룹 방향키 body19/list0, 검색 빈결과와 숨겨진 draft 보존, Escape 검색 clear/빈검색 close, Tab 모달순환, hidden draft apply, cancel/opener·background disabled/blur 복원, 적용 직전 collision Notice와 추가Applied 없음, normal/reduced open/close. Windows collision-notice-ax-final.json에서 오류문구 실제 노출까지 확인. 폰트 래스터/창 장식은 OS 기본 표현 경계이며 강제 보정하지 않는다. standalone backdrop 색/blur2/disabled 복원 계약을 확인했고 전체 Application 배경 AX branch 격리와 호출페이지 렌더·toast는 부모 Application/Selection 조립 책임이다. 해당 부모 검증을 이 컴포넌트 LOCK으로 완료 처리하지 않는다.

빌드 양OS 성공. 보호된30하위 코드 변경 없음. candidate file을 matchingdialog-lock.json으로 잠근다. 고립Windows native/source 창은 정리하고, 원본21와 source27은 다음 복구용으로 보존한다. 다음 활성은 TrialPreparation이다. 전체 조립이 남았으므로 임시 압축 재개 제약을 유지한다.
