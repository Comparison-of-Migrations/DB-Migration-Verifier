# PreparedDb
LOCKED · 2026-10-08. 부모 TrialPreparation 착수에서 발견한 작은 내부 컴포넌트. 잠긴 SvgIcon/Typography/Surface/Motion 조립 후 원본과 비교한다. 원본21 ready Selection의 .prepared-db DOM/computed 상태를 실제 확인했다.

## 원본 계약
div.prepared-db(role=status) = db.svg20 + div(강조 db명/메타 dbms·host/완료 hint) + check.svg20. flex align center/gap12/min-height64/padding10 16/border-left2 #6bdda3/bg#2b3330/radius0 8 8 0. 기본 max-width70%, @media(max-width:1180px)에서만66%. 부모 row justify-between/align center/gap24. 두icon flex:none/녹색. 텍스트 body min-width0/flex-shrink1. strong displayblock font13/20.8/700/ink/overflow-wrap:anywhere. meta/hint displayblock font11/17.6/muted/anywhere. actualsample Mac: card270.5625×75.984375; body172.5625×55.984375, name20.796875/meta17.59375/hint17.59375; icon x18/카드오른쪽16, vertical center. 부모 화면 폭에 따른70%/66% 제한 안에서 intrinsic width를 유지한다. hover/pressed/disabled/selected 없고 passive status이며 mount page-in200/sourceease animation, reduced skip. 부모 TrialPreparation에서 ready false→true 생성/ready true→false 제거.

## 다음 행동
원본 prepared-db literal을 lang/폭/DBMS/긴name·host의 고립 자료로 수집 → 후보 조립 → 양OS geometry/text/palette/줄바꿈 → mount/detach/reduced 상태 확인 → LOCK. 잠긴 하위 변경 금지.

현재 교정: media scope를 누락한 첫 fixture/native cap 비교의474차이를 합격으로 쓰지 않는다. 실제 source computed 폭459.1953125(656×70%)에서 조건 누락을 확인했고 원본 CSS47의1180px media를 직접 다시 읽었다. viewport1110/1280의 독립iframe에서 동일 원본 CSS를 실행해96조건을 새로 수집한다. native cap도 부모의 viewport 조건과 CSS quantized percentage를 반영하며 하위코드는 변경하지 않는다.

## 재개 · 2026-10-08
AGENTS → MASTER_RULES → 상태 → 명세 → CSS42/47·JS27 → WebPreparedDb/probe를 실제로 다시 읽고31 LOCK 무변경을 확인했다. 원본21은 Trial 화면이었으므로 준비 확인 버튼으로 Selection 복귀 후 실제 완료 카드270.5625×75.984375/max-width70%를 확인했다. 기존 결정 유지. Mac 새96조건 차이0; Windows96 및 양OS attached lifecycle10을 이어 검증한다.

## 검증·LOCK
양OS 각각96조건(lang2×viewport2×parent2×DBMS3×text4)의 크기/상대위치/줄바꿈/3문구/palette/radius/status 대조 차이0. `source/prepareddb-{mac,windows}-{source,comparison}.json`, `source/prepareddb-{mac,windows}-native/prepareddb-native.json`. 각OS 실제 attached lifecycle10: mount 시작(.35/y6)·완료(1/y0)·내용 갱신시 재시작 없음·detach/reattach·reduced 시작/진행중 전환·취소복원 및Polite 확인. 잠긴Motion Page 계약과 대조했다. `prepareddb-lifecycle.json`. 양OS `prepareddb.png`를 시각 확인했다. 기본 글꼴 래스터 차이는 OS 표현이며 앱의 기하/문구 차이 없음. pointer/키 입력은 passive 요소라 적용 없음. 부모 row/button/Notice 배치는 TrialPreparation에서 검증한다. 보호파일은 `source/prepareddb-lock.json`. 원본·대상/사용자DB 접근 없음.
