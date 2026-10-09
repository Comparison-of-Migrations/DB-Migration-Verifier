# InfoDialogBody

LOCKED · 2026-10-08 root 소유. 연구/권한 모달 본문은 양OS32/수명7 검증을 완료했다. 상위 InfoDialog의 조립 계약으로 사용한다. 아래 조사·준비 문구는 완료한 과정이다. AGENTS/MASTER/STATUS/병렬 파일·환경 차례 및 SummaryList/SummaryItem/Notice/Typography LOCK 계약을 읽고 original.js openModal('research'/'permissions') literal branch와 original.css를 확인했다. 라이브 조사 전 후보 구현하지 않는다.

## 원본 코드 조사

연구 본문: muted p(14/22.4), plain summary-list(4 protected summary rows), small p(12/19.2) margin-top16. 연구 row labels는 RP-D01·RP-C01/02, RP-A01/02·RP-O01, RP-B01, RP-S01. SummaryList label track은 부모 viewport≤1100에서130, 그밖170; gap20/padding-block16/마지막border0. 실제 연구 설명과 예시 탐지 증거 미확인 문구 그대로.

권한 본문: warn Notice(margin-top20 보호 계약) + muted p margin-top16. 2grant미검증/DBMS별 조회범위 안내/자동admin권한 부여 없음/연결부터 전체재검사 문구 그대로. Notice 자체 entry200ms만 있으며 본문/summary 입력·애니메이션 없음.

## 검증 계획

literal branch 독립 추출, 2lang×2iframeviewport×4본문폭(326/438/570/571)×2종류=32조건. root/paragraph/list/notice/summary row/문구/줄바꿈 원본 측정과 양OS 후보 대조. 양OS attached 언어/종류/폭 갱신·제거/복귀/notice normal-reduced 수명. 본문은 비동작 설명, 입력/닫힘/focus/backdrop은 상위 InfoDialog 범위. 잠긴 child 코드 변경 금지.

## 현재 다음 의존 작업

상위 InfoDialog는 다른 직접 하위 LOCK 및 root 활성 PageHeading 완료 뒤 조립한다. 본문 재설계·불필요한 재검증은 하지 않는다.

## 실제 원본 확인 · 2026-10-08
root 소유 임시 원본 tab46에서 Home 연구 버튼 및 Connection→Preflight 권한 버튼을 실제로 열고 본문 computed/font/margin/geometry와 두 PNG를 보관했다. 사용자 원본 tab1은 변경하지 않았다. 32 literal 조건의 기록 버튼을 눌러 source/infodialogbody-mac-source.json에 즉시 보존; 첫 narrow KR 연구 본문326×406.1875. 두 조사 tab46/47은 닫았다. source/infodialogbody-live-{research,permissions}.json과 screenshots PNG가 라이브 근거. 이제 보호 하위만 조립해 구현; 환경은 Connection에 반납.

## 후보 구현
WebInfoDialogBody는 연구 첫문단→plain 보호SummaryList→16gap 작은문단, 권한20gap 보호Notice→16gap 문단을 조립한다. Notice.Margin=0으로 자식 margin중복을 피하고 부모가 원본20px을 담당한다. 문단 line-height만 source CSS64 기하로 적용한다. 부모dialogpadding/scroll/focus는 아직 상위 작업이다. InfoDialogBodyProbe32 재사용/normal260ms/제거/복귀/reduced7수명 측정 준비. 아직 build/native실행 전, LOCK 아님. tools/ReplicaProbe/Program.cs root소유 --infodialogbody 경로 추가. 환경 차례 connection→preflight→home→root를 기다린다.

## 최종 검증·LOCK · 2026-10-08
mac/windows 각각32 literal조건 root/문단/summary/notice 기하·문구·줄바꿈·색상 엄격 대조 차이0. source/infodialogbody-{mac,windows}-comparison.json, 양OS native/infodialogbody-lifecycle.json의7 attached상태(normal mount/end, 종류교체, 제거, 재부착, reduced) 모두 통과. actual source 연구/권한모달 computed와 source/native PNG를 직접 확인했다. 본문은 설명 내용이며 입력·포커스·scroll·닫힘은 상위 InfoDialog 소유로 명시했다. 기본OS 글꼴 래스터 표현 차이는 사용자 허용 범위이고 기하/문구 차이는 없다. 자식 보호 코드 변경 없음. 고유 Windows 원본 창 닫기 완료, Mac 자동 probe 종료. 알려진 본문 구현 차이 없음. LOCK.

압축 복구: AGENTS/MASTER/STATUS/본명세/직접 source/native를 다시 실제 읽고48보호 확인; 요약의 이전 빌드대기 문구를 실제 양OS32/7 증거로 갱신했다. 과거 준비 계획은 완료 증거로 사용하지 않는다.
