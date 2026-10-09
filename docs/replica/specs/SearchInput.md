# SearchInput

LOCKED · 2026-10-07 · 단일활성. 직접하위 TextInput/IconButton/Surface/SvgIcon/Motion LOCK계약으로조립한다. 원본은현재실행웹이며기존native검색helper를원본대신사용하지않는다.

## 원본 조사

actual Selection 및 Matching에서 DOM/CSS/JS/직접부모를재확인했다. div.search-field(relative,minwidth180,maxwidth380,flex1)→SVG search18(local13/13,pointer-events:none)+sr label+input[type=search,autocomplete=off]+Table에만quiet IconButton clear44(top0/right0). Matching은clear노드가없고 `.match-browser .search-field`에서maxwidth:none/margin10px0 14px. 입력font13/lineheight20.8/padding9px44px9px40px,minheight44. Matching에서도padding-right44가남는원본을임의개선하지않는다. placeholder색#a3a9b2, input배경#1e1f22/fg#dbdee1,border#7c838c/radius8; focus#a7b1ff/3px glow는TextInput LOCK계약이다. icon색#b5bac1.

- KO Tables label테이블검색/placeholder테이블이름으로검색/clear테이블검색지우기; EN Search tables/Search by table name/Clear table search.
- KO Matching 대상테이블검색/대상테이블이름검색; EN Search target tables/Search target table names.
- tableQuery는input의raw문자열, clear disabled는!tableQuery이므로공백도지우기활성이다. 검색규칙 trim/toLowerCase 및테이블선택/대응보존은부모책임이며이컴포넌트에서임의필터하지않는다.
- clear button은값을빈문자열로바꾸고결과갱신/fade160을부모에요청한뒤입력포커스를복원한다. Escape UA clear는값이있고editable일때만이며별도의buttonpress/결과fade를추가하지않는다.
- actual Tables orders→Escape: value빈값/3행/clear disabled/input포커스. actual Matching archive→Escape:값빈값/4옵션/같은input포커스/대화상자유지. empty에서두번째Escape는부모닫기애니메이션이끝나면modal제거/opener포커스복귀. 즉시DOM읽기에dialog가남아있었다고Escape규칙을재해석하지않는다.

근거: source/original.css/JS의search-field/select/matchingOptions/updateTableResults/syncTableSelection/clear-search/closeModal; source/searchinput-live-matching-ko.json 및 IconButton의현재table부모기록. UA공식근거: https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/core/html/forms/search_input_type.cc 의HandleKeydownEvent(비어있지않은editable Escape). 원본서식/버전은수정하지않았다.

## Avalonia 대응

WebSearchInput→overlay Grid→LOCKED WebTextInput(SearchMode=true)+LOCKED SvgIcon18(local13/13,hit-test없음)+LOCKED WebIconButton(right0/top0,Tables만). Context가원본maxwidth/margin/clear유무를명시한다. 이름/placeholder는부모가공급한다. QueryChanged는값만알리며 ClearInvoked는버튼으로지운경우만별도로알린다. parent가결과fade와필터를조립한다. TextProperty의동기변경에서raw빈값에따라clear enabled동기화한다. TextChanged의posted발송에의존해clear상태가늦게바뀌지않게한다. Tunnel Escape는값이있을때만clear/Handled이며empty는parent로전달한다.

구현: src/Verifier.Desktop/WebSearchInput.cs. 아래 범위 검증 후 LOCK했다. 다른컴포넌트/LOCKED코드는수정하지않는다.

## 다음 행동

SearchInput 계약은 이후 MatchingDialog/Selection 조립에서 사용한다. 다음 단일활성은 StepBadge다. 전체복제재개제약을 유지한다.

### 현재 검증

Mac48layout/7same객체raw값갱신은대조통과했다. width160은min180으로,Tables452는max380으로,Matching452는452로원본과일치한다. 두언어의empty/value/space/placeholder/접근성이름/아이콘13·13및18×18/clear44우측/입력padding40·9·44·9가일치한다.

Mac actual창에서Tab/typedorders→Escape로값만clear(parentEscape0), emptyEscape는parentEscape1. orders재입력→Tab으로clear포커스→Space는clear1회/입력포커스복귀/disabled동기반영. Matching으로Tab이동후Escape로valueclear만, emptyEscape는parent전달(전체parent2). source/searchinput-mac-actual/searchinput-input.json 및screenshots/searchinput-mac-actual.png. 초기CUA외부변경알림시입력을계속하지않고AX/기록을다시확인한후진행했다. nullable빌드진단은getter의명시적빈값으로교정했으며실패publish를실행하지않았다.

Windows원본48/DPR2는UIAInvoke의freshPOST로보존했다. source/searchinput-windows-source.json. Windows 후보 48layout/7update와 actual 입력·포커스·Escape 검증을 완료했다. 현재server32527.


## 최종 판정 · 2026-10-07

- 맥락 복구: AGENTS → MASTER_RULES → 상태 → 본 명세 → 실행 중인 Selection DOM/CSS/JS → WebSearchInput → 남은 차이를 실제로 다시 확인했다. 설계를 초기화하지 않았다.
- 양 OS 48layout/7same-object updates 모두 통과: source/searchinput-{mac,windows}-comparison.json. parent margin도 실제 원본 48개에서 수집해 후보와 대조했다(source/searchinput-source-margins.json): Tables 0, Matching top10/bottom14. 나머지 min/max 폭·padding·icon/clear 상대 위치·언어·raw value 일치.
- Windows actual source Escape는 input/keyup-Escape/value빈값/동일 input focus/clear disabled로 fresh POST 확인: source/searchinput-windows-source-input.json. Native actual26events에서 부모 Escape 2회(빈값), 버튼 clear 1회 및 input focus복귀 확인: source/searchinput-windows-actual.json. source와 native의 nonempty Escape가 부모를 호출하지 않는 경계가 일치한다.
- UIA focus handshake 파일을 읽는 순간 partial JSON이 발생한 한 번의 실패는 키를 보내기 전에 중단됐다. helper는 partial JSON을 확인 성공으로 취급하지 않도록 수정했다. 실패를 성공에 합산하지 않았다. 검증한 Windows/macOS native 창은 닫았다.
- Editor normal/focus/disabled/readOnly/invalid 전이 및 버튼 hover/pressed/focus/disabled/reduced는 각각 TextInput/IconButton의 LOCK 계약을 사용한다. 이 컴포넌트는 새 paint/motion을 추가하지 않는다. QueryChanged/raw와 ClearInvoked/button-only 경계 및 empty Escape 부모 전달을 별도 검증했다.
- 검색 결과의 필터·선택 보존·dialog 닫기·result fade는 부모의 이후 검증 범위다. 본 LOCK을 전체 페이지 동작 완료로 쓰지 않는다. 기존 MainWindow 검색 helper는 상위 조립 때 교체한다.
- 알려진 컴포넌트 차이: 없음. 현재 두 OS/DPR2 및 원본 context 범위. source/searchinput-lock.json으로 구현 파일을 보호한다.
