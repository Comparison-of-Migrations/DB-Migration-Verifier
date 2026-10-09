# 조립 후 통합 검증 경로

root 소유 · 계획이며 아직 실행/통과 증거 아님. UI 정본은 실제 원본과 original.js/css; 상태는 COMPONENT_STATUS. 아래는 잠긴 단위들을 조립할 때 놓치기 쉬운 경계를 실제 입력으로 확인할 목록이다.

## 검증 환경

한영, desktop 창 폭960/1138/1440, 기본 OS 설정의 macOS/Windows. 원본과 후보의 같은 상태/폭을 기록한다. OS 표현 차이는 원인·범위를 분리하고 레이아웃/상태/동작 오류 면제로 사용하지 않는다. 샘플과 실제 DB 결과를 혼동하지 않는다.

## 최초 진입·연결·조회

1. Home 시작/예시/저장 결과 열기/연구 반영 버튼 각각 실제 입력. 처음은 visited빈값, stage버튼 모두 비활성.
2. 새 검사: original.initial()과 같은 선택/예시값으로 초기화; Connection 제목 포커스 및 body스크롤0.
3. 연결 필수/port범위/bad접속/sameDB 오류; 오류가 입력만으로 사라지지 않고 다시 확인하면 갱신되는 정책. 잘못된 입력 후 창 계속 동작.
4. 엔진 변경 시 양쪽port/연결무효/시험준비무효/visitedreset. 기존 advanced/error 유지. radio포커스+모달리티 복원.
5. 연결 성공 busy550ms/버튼disabled/spinner/입력허용, 끝나면 연결확인상태+현재 화면에 preflight버튼이 있으면 그 버튼 포커스. busy중입력은 source처럼550ms완료전에 다시 검증하지 않는다. Back/engine/advanced/fields가허용된 상태를 유지하며, 그중이동/새start/restart 뒤에도 pendingcallback이 현재globalstate에 적용되는 경계를 별도검증한다. input시 기존입력/포커스/캐럿 보존하고 pill/success/footer/stages 부분만 갱신.
6. Preflight 이동/permissions모달 표시 및 닫힘/Escape/기존 opener복원. 일부미조회가 객체없음/일치로 바뀌지 않음.

## 선택·대응·시험·실행전

7. category checkbox와 header펼침 독립 입력, 선택집계·선택0/테이블0/trial미준비 footer조건. 펼침 상태/선택 상태 서로 자동 변경되지 않음.
8. table검색/clear후focus/검색결과전체선택·중간상태/선택해제/검색0(empty)/언어전환. 검색은 기존전체선택을 덮어쓰지 않음.
9. matching모달: 현재선택·검색·불가대상·중복방지·취소·적용·명칭불일치 보존/포커스/본문scroll/반응.
10. Trial 준비 화면: 값/주소/DB변경→ready/consent무효, 같은DB차단, 필수값확인, 준비→선택복귀→우측준비정보. 시험의 예시를 실제 쓰기로 오인하지 않음.
11. Review 요약/제외범위/시험분리정보·확인체크→footer조건/Start. 언어전환·동일화면 render포커스 유지.

## 실행·결과·저장·재열기

12. 실행중 sidebar전부비활성, 140ms샘플tick의 처리수/총수/분류상태 및 meter전환. 끝나기전 cancel실제입력→running분류 interrupted/미시작보존/부분결과/중단이유. 전체재검사만 제공.
13. 자연완료시 취소버튼에 있던 포커스를 View results로 이동. 초기/진행/완료/중단 상태를 같다고 표시하지 않음.
14. 결과 normal진입시 count/bar모션, filter재render시 settled재생안함. 분류전체행/Chart/Trial행 상세, 각filter내용/필터포커스/일치와 미검증별집계/제외범위.
15. 결과에서 전체재검사: source JS restart()의 초기화 범위 그대로(불필요한 전체state초기화 없음). 이동된 제목포커스/스크롤0.
16. JSON/CSV/HTML 저장선택/삽입순서/모두해제/저장완료/saved안내/파일내용; native 파일대화상자는 OS 표현 허용 경계. 실제 값/비밀 저장 금지.
17. 저장한 JSON 재열기: 새 검사/DB연결 없이 결과복원, visited 결과/저장만, maps/tables/groups/판정유효성 원본조건 그대로. 손상/다른형식/구조불량 시 toast, 기존결과/입력 유지. OS창선택취소 보존.
18. 원본 title/data-action/id 기반 focus복원 및 modal언어전환; 재render 스크롤보존, go() 스크롤0. 모션/입력hold의 닫힘·분리 처리를 검증한다. 비동기 연결550ms는 원본에서 화면이동으로 취소되지 않으므로 일반적인 자동취소를 추가하지 않고 해당수명/상태반영을 별도 대조한다.

직접 하위 LOCK 전에 상위 후보 구현/완료를 선언하지 않는다. root가 단위 증거와 이 통합 경로의 실제 결과를 대조하고, 전체 완료 뒤에만 AGENTS 임시 재개 제약을 제거한다.
