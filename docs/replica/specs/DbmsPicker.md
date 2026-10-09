# DbmsPicker

IN PROGRESS · 2026-10-08. 단일활성. AGENTS→MASTER_RULES→STATUS→직접 하위 DbmsOption 명세→실행 connect()/DOM/CSS→잠긴 WebDbmsOption 계약을 실제 읽고 복구했다. 보호45개 통과. 전체 재분석/설계 초기화 없음.

## 원본과 경계
connect(): fieldset.dbms-fieldset → legend + div.dbms-grid(4 label.dbms-option) + p.dbms-note. SQL Server/Microsoft · 1433, PostgreSQL/PostgreSQL · 5432, MySQL/MySQL · 3306, MariaDB/MariaDB · 3306 순서. 실제 원본에서 모두 선택 가능. 실제 DB 접속 지원 완료로 표시하지 않는다.

fieldset border0/padding0/min-width0/margin-bottom24. legend font12/600/ink, 단위없는1.6, margin-bottom10, UA padding-inline2. 실제 DPR2 원본904/viewport1204: legend x0 y0 height19.1953125 width124.5625; grid y29.1953125/height88/4×218.5/gap10; note y127.1953125/height19.1953125; root146.390625. legend 내부 글자는 x2. note12/400/muted/margin-top10, parent폭 전체. 컴포넌트의 외부24px는 Connection 소유. card compact는 부모 폭이 아니라 viewport≤1180. 잠긴 DbmsOption을 수정하지 않는다.

원본 문구: KO 비교할 데이터베이스 종류 / EN Database engine to compare. note KO 원본과 대상은 같은 종류로 비교합니다. 선택하면 양쪽 예시 포트도 바뀝니다. / EN Source and target use the same engine. Selecting one updates both sample ports.

## 상태/입력
각 input id=dbms-i/name=dbms/aria-label=engine. 동일 그룹 checked 하나. programmatic 값 설정은 change 없음. true 사용자 선택만 상위 전달. label/Radio hover/focus/pressed/transition은 보호 하위 계약. 별도 Picker animation/hover 없음. engine 변경 시 원본 부모가 양쪽 port/connected/trial/visited를 갱신하고 render()한다. Picker에 이 부모 정책을 넣지 않는다. source render는 data-field보다 id를 먼저 사용한다. 실제 원본 PostgreSQL 본문 선택→양쪽5432/checked하나/새dbms-1 focus-visiblefalse 확인. Connection 교체 시 id+포커스 모드를 복원해야 한다.

## 조립/검증 계획
legend → 4 보호 카드 → note 순서. 조건64(언어2×실제 iframe viewport1000/1400×폭656/657/904/1060×선택4)를 literal connect()에서 추출해 양OS 동일 attached 후보와 비교한다. 고립 원본은 다중 fieldset radio name만 fixture별로 분리하여 다른 표본의 checked를 풀지 않게 한다; 실제 하나의 그룹 계약은 유지한다. native도 Picker instance마다 그룹 분리. 기하/문구/선택/카드 순서/부모 대응, 실제 arrow/label/Space/Tab/갱신을 확인한다. 완료 전 LOCK 금지.

## 현재 차이/다음 행동
후보 없음. 위 구조로 구현하고 원본64 수집부터 진행한다. 외부24px/부모 rerender/포트 동기화는 Connection 통합에서 검증한다. 전체 압축 복구 제약 유지.

## 첫 대조/교정
Mac64에서 카드·root·note·grid는 일치했고 legend intrinsicwidth만 CSS ceil 단위보다 0.006~0.008 작았다. legend UA padding2 표면과 CSS단위 intrinsic width ceil을 명시해 교정한다. 보호 하위 변경 없음. 소스 렌더는 id 우선 복원임을 코드/실제 입력으로 확인했으며 기존 추론을 폐기했다.

## 최종 판정 · LOCK
양OS64개 동일 attached 재사용 조건에서 root/legend UAbox/grid/note/카드4개 geometry·문구·selected 차이0. source/dbmspicker-{mac,windows}-comparison.json. 실제 Mac 본문PostgreSQL→ArrowRightMySQL→Space유지, MariaDB→ArrowRightSQL Server 순환/Tab그룹이탈 확인. Windows 실제 CLI/UIA Right2회/Space유지/Right2회순환 일치. 양OS언어변경·programmatic SQL→MySQL은 새 EngineChanged를 만들지 않으며 checked는 한 개. 제거/복귀 상태 유지. source/dbmspicker-{os}-input/ 및 원본 actual dbmspicker-source-input.json.
보호 하위 소스 변경 없음. 원본에 부모 Picker의 추가 motion/disabled 상태 없음; 카드 기존50timing/실제 label취소 검증은 하위 계약으로 조립한다. 양OScompactKR/wideEN PNG를 직접 검토했고 기하/색상 일치; 기본glyph/edge AA는 허용 OS 표현 경계다. 실제 연결/포트/reset/부모focus 복원은 Connection 소유이며 이LOCK의 완료 주장이 아니다.
DbmsPicker 단위 종료로 턴을 멈추지 않는다. 다음은 Connection의 직접 하위 고급 설정 section부터 Bottom-Up으로 검증한다. 전체 재개 제약 유지.
