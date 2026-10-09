# MatchOption
LOCKED · 2026-10-08. 하위 Radio/Typography/Surface/Motion은 LOCKED. 실제 원본 Selection→첫 테이블 대응 변경을 열어 카드 DOM/computed geometry/상태를 확인했다. 부모 MatchingDialog는 WAITING CHILD.

## 원본 계약
label.match-option: radio20 + span(body:mono title+small note) + b.match-badge. flexcenter/gap12/border1/padding13 14/min-height64/radius8/transition160ms 기본ease. body flex1/min-width0; name ui-monospace12/normal1.6/overflow-wrap:anywhere. note11/1.6/margin-top3; badge11/600/nowrap. 비선택 빈badge도 gap12를 유지하되 badge0×0. 실제442폭에서 row67.7890625, radio x15/y23.890625, body x47/y14/h39.7890625, badge 오른쪽15px.
기본bg#2b2d31/border#7c838c. selectedbg#3b3d52/border#8b94ff/inset1#8b94ff. availablehover bg#383b44/border#a7b1ff (selectedshadow유지). unavailablebg#26272d/border#535963/name+badge#adb3bd/note#b5bac1, radio disabled/cursor notallowed; card hover selector에서 제외. keyboard focus-visible radio는 잠긴Radiooutline, parentoutline3#a7b1ff/offset2/radius8. label본문 클릭은 available radio를선택; 부모가badge/이름/동일원본/충돌 상태 결정. 선택그룹 방향키는 잠긴Radio계약. unchecked/checked/disabled/focus/hover/motion및한영줄바꿈을비교한다. 원본에 없는애니메이션 없음.

## 구현 예정
WebMatchOption은 부모정책을가지지않고 name/note/badge/selected/unavailable presentation과 SelectionRequested만 제공한다. 직접 하위 스타일 수정 없이 surface와 layout조립. 다음 literal source fixture→후보→양OS기하/실제입력/transition→LOCK.

## 진행 확인
각OS136 독립 원본 geometry/text/color/state 표본은 차이0. focus endpoint는 부모 outline와 잠긴Radio의 실제 focus paint를 함께 설정하며 마지막 PNG를 확인했다. 초기 무한 available width에서 body가 무한 desired size를 반환한 결함을 교정해 intrinsic measurement를 처리했다. 실제 CSS50 transition 중 hover reverse8ms의 R채널이1만큼 달랐는데 byte로 반올림한 중간색을 다시 transition 시작점에 넣어 생긴 오류였다. tolerance를 넓히지 않고 premultiplied floating Rgba를 track 내부에 보존하도록 교정한다. 실제 동작 및 수정본50timing/양OS 재검증 전까지 LOCK하지 않는다.

현재 최종 후보 양OS136조건 및50CSS transition 비교 차이0. `matchoption-{os}-comparison.json`/`matchoption-{os}-timing-comparison.json`. source fixture는 원본 matchingOptions literal markup/원본 CSS와 검증용 pseudo-state class mapping으로 구성하며 원본21탭 모달 동작을 실제 입력으로 별도 확인한다. native endpoint paint가 잠긴 Radio focus-visible를 함께 설정한다. Windows source motion 파일명 선택 오류로50표본이136파일을 덮어쓴 것을 발견하여 별도 timing 경로로 복구하고 양쪽 원본을 새로 기록했다; 잘못된 합격 상태는 사용하지 않았다. Windows 실제 body click으로 archive.orders를 선택했다. 제어CLI InvalidArgument 첫실패 후 events[]/helper DLL부재를 확인해 동작 미전달로 남기고 compile/run 분리 후 성공했다. 실제 disabled/cancel/keyboard/reduced 비교 진행 중; LOCK 전이다.

## 최종 판정 · LOCK
양OS136 static geometry/text/palette/state 및50 actual CSS timing 차이0. 각 source/native/comparison 및 timing-comparison JSON은 source/에 있다. actual9상태 대조 `matchoption-input-comparison.json`: Mac본문선택/disabled거부/Left/Reduced pointer, Windows본문선택/disabled거부/밖으로release취소/Right/Reduced Left. native와독립원본static442폭에각최종기하/선택/badge/disabled가일치한다. 원본21 actual본문archive선택/disabledorders차단/Leftcustomers/Rightarchive도 실제로 확인했다. UI Record가hover fade중일수있으므로 actual색은 settled136과CSS50독립근거로검증하고 actual중간색을endpoint합격으로사용하지않는다.
Windows cancelled pointer는직접검증했고Mac취소입력은별도로반복하지않았다; OS공유OnPointerReleased분기와Mac정상본문pointer경로를함께검증한범위다. 부모Dialog의disabledclick후DIALOG포커스는MatchingDialog명세의필수항목으로남겼다. 고립canvas와모달의포커스차이를OS표현예외로면제하지않는다. Renderer글자AA/창장식은OS기본표현경계. 하위잠긴코드변경없음. 양OSnative와고립IAB24/25종료. Windows source2개창정리후다음activeMatchingDialog로진행한다.
