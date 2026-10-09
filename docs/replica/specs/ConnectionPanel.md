# ConnectionPanel

LOCKED · 2026-10-08 shadow 교정 후 재검증. TopBar LOCK 후 단일 활성. 직접 하위 Field/TextInput/NumberInput/StatusPill/SvgIcon/Typography/Surface는 LOCKED.

원본 connectionSide(side)의 panel panel-pad와 form-head/role-mark, host+port field-row, db/user/password field를 실행 원본에서 조사한다. 다른 페이지나 잠긴 하위는 바꾸지 않는다. DBMS 선택·connected/validation 정책은 Connection 페이지의 부모 소유다. 다음: 직접 원본 DOM/computed/connectionSide CSS → 명세 → 조립 → 양OS 원본 비교·실제 label/input 확인 → LOCK.

## 원본 실측 계약
실행 원본 connectionSide(source/target)는 panel 배경#2b2d31/border1#41434a/radius10/padding26. form-head flex center/gap12/padding-bottom20/border-bottom1. role-mark40×40/border1#4c505a/bg#353740/radius8, icon22/#b5bac1. 제목18/700/27/tracking-.27/#f2f3f5, small12/19.2/#b5bac1. pill margin-left:auto, nowrap. field-row 열1fr/112와gap16. 나머지 field는 정상 block으로 순서대로 배치; field자체 margin-top16 유지. Password 예시는 빈값/disabled/실제비밀번호입력금지 placeholder와 hint. 실행 원본442폭의 section높이490.171875, inner388, header67.1953125, firstrow86.1953125였다. 좁은 영어/연결확인 pill은 flex shrink와 small줄바꿈을 직접 비교한다. input변화·접속/검증 정책은 부모이며 본컴포넌트는 입력 변경을 요청한다.

## 진행·차이 교정
양OS 원본40조건(한영×원본/대상×연결확인 여부×328/338/409/442/522폭)을 독립 수집했다. 첫 probe가 fields의 IEnumerable을 즉시 동결하지 않아 마지막 상태가 모든 행에 기록됐다. 그 기록은 실패 증거로 남기고 ToArray로 측정 즉시 동결했다. 원본 flex shrink는 role-mark의 content basis38을 사용하므로 border-box40을 가중치로 사용한 후보를 교정했다. mark center와 intrinsic pill폭의 1/128 quantization을 부모 조립에서 적용했다. 잠긴 하위 코드는 변경하지 않았다. mac40조건 최종 차이0. Windows 동일 비교 및 실제 input 전달 확인이 남았다.

## 최종 판정·LOCK
최종 각OS40조건의 original DOM/independent native box·label·값·disabled·hint 배치 차이0: source/connectionpanel-{mac,windows}-comparison.json. source와native 입력 전달은 source/connectionpanel-input-comparison.json 및 input/에 보존했다. Mac 숫자 AX setValue는 이 도구에서 값 변경을 전달하지 않아 실패로 남기고 실제 클릭/전체선택/키보드로5432입력을 확인했다. Windows ValuePattern은 네 값을 직접 전달했다. 초기 delayed TextChanged 이벤트가 기록에 섞이는 것을 발견해 TextProperty 동기 전달로 바꾸고 새 binary 양OS40조건+실제 입력을 재검증했다. source/native의 입력 방법에 따른 문자별 event개수는 기능 차이로 주장하지 않는다. 마지막 값·fieldkey·비활성password가 일치한다.

전체 Connection 페이지의 connected 초기화·검증버튼과 실제 DB 연결은 부모 소유. 원본 시안 password disabled 정책을 그대로 유지한다. 상시 요구사항의 실제 자격증명은 제품 구현 단계와 구분한다. native/connectionpanel.png를 시각 확인했다. 기하/색/배치/문구는 각OS원본 기준으로 복제하며 OS 기본 raster 표현은 MASTER_RULES12를 적용한다. WebConnectionPanel.cs를 connectionpanel-lock.json으로 보호했다. 하위 LOCK 변경 없음. 다음 CategorySection 진행.

## LOCK 재개 필요 · 발견 기록
CategorySection의 실제shadow property 수집 중 동일한 CSS8자리 색 파싱을 사용한 ConnectionPanel의 shadow도 확인했다. Color.Parse("#00000012")는 CSS black alpha18이 아닌 Avalonia alpha0/blue18이다. 이전40조건 기하/입력 검증은 그대로지만 shadow 검증은 빠졌다. OS 표현 예외가 아니다. CategorySection을 먼저 마친 뒤 ConnectionPanel을 한 개 활성으로 재개하고 변경 이유·영향·원본근거를 기록하여 Color.FromArgb(18,0,0,0) 및 실제shadow snapshot을 검증해야 한다. 현재 protected 코드는 아직 변경하지 않았다. Connection 페이지 상위 조립 전에 해결한다. 이전 전체완료 주장은 이 발견으로 제한된다.

## LOCK 예외 변경 기록 · 2026-10-08 (수정 전)
이유: 실제 Connection 상위에서 사용될 패널 shadow가 원본과 다르다. 영향: WebConnectionPanel outer surface paint만, 잠긴 Field/입력/기하는 변경하지 않는다. 근거: 실행 원본 `.panel.panel-pad` computed shadow는 `rgba(0, 0, 0, 0.063) 0px 2px 4px 0px`; original.css의 후속 `.panel` override가 `0 2px 4px #00000010`이다. 앞 발견 기록의 alpha18/y1/blur3 제안은 초기 CSS만 반영한 것으로 폐기한다. 최종 원본 기준 Color.FromArgb(16,0,0,0),OffsetY2,Blur4로 교정하며 실제 shadow property를 양OS40조건에 추가한다. CSS기본renderer blur 알고리즘 표현경계는 OS예외 적용 가능하나 선언/실제 property 불일치는 허용하지 않는다.

## shadow 교정 최종 판정 · 재LOCK
실제 원본 shadow를 포함해 양OS40조건 모두 차이0를 다시 수집·비교했다. 각 `connectionpanel-{os}-source.json`와 `{os}-native/connectionpanel-native.json`, 비교JSON에 실제 shadow가 포함된다. 원본 `rgba(0,0,0,.063) 0 2 4 0`과 native 실제 surface alpha16/y2/blur4 일치. Mac 재생성 connectionpanel.png도 시각 확인했다. 변동은 외부 shadow 한 선언이고 입력 정책·이벤트·하위기하 수정 없음; 앞선 actual input 증거는 그대로 유지한다. 잠긴 하위 변경 없음. 두 자동검증 native 창 정상종료, Windows 원본 fixture 창 종료. 다음 단일 활성 MatchingDialog.
