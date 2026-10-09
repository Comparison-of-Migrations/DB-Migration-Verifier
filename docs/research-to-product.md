# 논문 검증기 분석과 제품 대응

기준 논문: **Functional Success versus Database-State, Behavioral, and Operational Contracts in AI-Generated PostgreSQL Schema Migrations**, DSJ 영문 v19. 저자 보고와 프로젝트 기록상 2026-09-30 투고 및 정정 발송 상태이며 심사 통과를 주장하지 않는다.

## 분석 범위와 출처 확인

2026-10-01에 수행한 정적 코드·보존 자료 분석이다. 이번 작업에서 DB에 접속하거나 SQL·마이그레이션·기존 재실행 장치를 실행하지 않았다. 아래 앱 검사와 확인 사례는 구현 계획이며 실제 앱의 탐지 결과가 아니다.

- 실제 제출 Word v19의 제목·초록·결과 문단을 확인했다. SHA-256: `a8a60473075f9cecface78dcab90643e27c855a349b26e328dce1a3e159c0da2`.
- 연구 저장소 기준 commit: `24d67502d58295d68c969927c19c473700e2e967`. 동결 tag: `v3-protocol-freeze-20260927-r7`.
- 정지 상태 검증의 공통 경로와 T07–T19 과제별 oracle 전체를 읽었다. 접속·기록 helper의 관련 부분, 공개 계약, 동결 전 과잉 판정 교정, 최종 사후 계약 probe와 기록도 대조했다.
- 아래 코드 23개는 현재 파일·동결 tag·제한 심사 ZIP의 `stationary-replay/` 사본 사이에 바이트 차이가 없었다. 목록별 `경로 + 공백 + SHA-256`을 경로순으로 정렬해 줄바꿈으로 결합한 UTF-8의 SHA-256은 `ff86a9bac5dcc16ae9a61b193663739cb9597fbc968f2c53b925852cbd345692`다.
- 제한 ZIP의 `stationary-replay/expected-judgments.jsonl`을 직접 집계했다. 1,239 실행 상태 → 1,062 populated → 1,008 기능 통과 → 954 전체 Gold 판정 가능 → 60 실패로 제출본과 일치했다. 이는 보존된 판정의 대조이며 새로운 DB 재검증이 아니다.
- 최종 사후 probe JSON 78기록(13개 생성물 × 6상태)에서 아래 사례의 관측을 대조했다. 별도 코드도 읽었지만 이번에는 재실행하지 않았다.

분석 원문은 로컬 연구 저장소 `/Users/youptebook/.codex/.chatgpt-projects/g-p-6aa7693dadf881918383a279698849ff/repo`에 있다. 아래 경로·줄 번호는 위 commit 기준이다. 공개 앱 저장소에는 제출 Word·제한 ZIP·실제 DB 자료·심사자 비밀 링크를 복사하지 않는다.

| 코드 경로 | 분석한 역할과 주요 위치 |
|---|---|
| `evaluation/v3_isolated_eval.py` | `task_modules:33`, `_modules:75`, `run:84`: 과제 분기·fixture 준비·마이그레이션 뒤 판정 호출 |
| `evaluation/contracts.py` | T01–T06의 고정 열·테이블·역할·인덱스 명세 |
| `evaluation/evaluate.py` | `classify:7`, `_evaluate:19`, `evaluate:84`: T01–T06 축별 판정·격리 확인 |
| `evaluation/gold.py` | `expected_data:26`, `data_checks:44`, `schema_checks:74`, `access_checks:158`, `object_checks:189` |
| `evaluation/behavior.py` | `access_runtime:73`, `future_inputs:113`, `audit_probe:171`, `sequence_probe:200`, `tenant_checks:215`, `run:277` |
| `evaluation/baseline.py` | `schema:19`, `crud:49`, `sample_ids:103`, `enhanced:135`, `run:190`: B/B+의 별도 검사 |
| `evaluation/limited.py` | `run:26`: T01–T06의 제한 집계·정적 검사 |
| `evaluation/v3_catalog_limited.py` | `SQL`, `augment:63`: T07–T19 각각 하나의 추가 catalog 신호 |
| `evaluation/core.py` | `aggregate:38`, `Checks:46`, `Database:74`: 예외·판정·catalog 수집·clone·데이터 수집 |
| `calibration/run.py` | `Harness.connect:44`, `query:49`, `database:68`: 자동 커밋·전체 결과 수집·SQL/값 사건 기록 |
| `evaluation/t07_oracle.py`–`evaluation/t19_oracle.py` (13개) | 아래 19과제 표의 변환·구조·접근·객체·미래 동작 검사 |

문헌 원문: `docs/03-task-contracts.md`, `docs/15-state-behavior-contracts.md`, `docs/21-task-contracts-T07-T19.md`, `docs/53-v3-ai-operational-integration-and-oracle-audit.md`, `paper/submissions/dsj/manuscript-en.md`, `paper/submissions/dsj/gold-contract-independent-review.md`, `paper/submissions/dsj/contract-witness-map.md`, `paper/submissions/dsj/independent_contract_probe.py`와 `independent-contract-probe-20260930-v3.json`.

## 제출 최종본의 근거 요약

19개 과제·190회 생성 중 177개 유효 생성물을 평가했다. 기능 통과·Gold 판정 가능 populated 평가 954개 중 동결 Gold 실패는 60개(6.29%)이고 10개 생성물의 반복 평가다. 사후 검토에서 7개 생성물·42개 평가에 계약 위반 근거가 지지됐으며 3개·18개는 관측된 실패 기제로 결함을 입증하지 못했다. 기존 데이터 변환 손상은 확인되지 않았다. 제한 정지 검사 V_all은 60개 동결 실패 판정을 탐지하지 못했다. 이 수치는 산업 오류율·데이터 손실률이 아니다.

| 연구 관찰 | 제품 반영 | 판정·화면 | 확인할 증거 |
|---|---|---|---|
| 기본 기능 성공만으로 넓은 계약 충족을 보장하지 못함 | 데이터·구조·권한·객체 분리 검사 | 분류별 판정 | 데이터 일치·권한 불일치 예시를 실제 탐지 |
| 기존 데이터와 향후 동작은 다른 축 | 읽기 비교와 시험 DB 동작 실행 분리 | 비교 일치와 동작 실패 동시 표시 | 선택한 동작의 실행·기대·결과 |
| Unknown·미실행을 성공으로 바꿀 수 없음 | 권한·중단·미지원·선택 제외를 명시 | 범위와 확인 불가 이유 | 일부 조회 실패·취소에서도 전체 통과 안 함 |
| Gold의 과잉 해석·probe 경계 문제 | 과제 전용 가정 분리, 정의 차이와 동작 결함 분리 | 정의 불일치를 데이터 손상이라고 단정 안 함 | 허용된 구현 차이에 대한 반례 검사 |

## 검증기의 실제 처리 구조

1. `v3_isolated_eval`은 미리 정의한 합성 입력을 일회용 PostgreSQL에 만들고 pre를 저장한 뒤 post DB를 생성한다. 고정된 migration bundle을 post에 실행한다. 이 실행·복제 장치는 앱의 제품 기능으로 이식하지 않는다.
2. T01–T06은 `evaluate._evaluate`에서 Gold의 데이터·구조·접근·객체 검사와 `behavior.run`의 역할·미래 동작을 합친다. B/B+와 제한 V는 별도 코드 경로다.
3. T07–T19는 각각의 oracle `evaluate`에서 같은 축을 채우지만, 실제 SQL·비교 필드·쓰기 probe는 과제마다 다르다. 모든 객체에 동일한 깊이의 검사를 적용하는 범용 장치가 아니다.
4. `Database.state`는 주로 고정 `app` schema의 catalog와 전체 행을 수집한다. T15는 `catalog`, T17은 `secure` schema에 별도 수집기를 추가했다. 동일 schema 밖을 자동으로 전부 발견하지 않는다.
5. 쓰기 probe와 일부 역할 읽기는 `db.clone`에서 수행한다. 전후 snapshot digest로 관측 대상의 변경 여부를 확인한다. 그러나 일부 oracle의 view 조회와 역할 읽기는 post에서 직접 수행한다. 모든 읽기 경로가 일관되게 별도 공간에 격리된 것은 아니다.
6. `aggregate`는 관측 FAIL → UNKNOWN → PASS 순으로 집계한다. 실행 실패의 `I_abort`와 실행·기능·전체 계약 판정은 분리한다. 앱의 검사 취소를 논문의 마이그레이션 안전 중단과 같은 사건으로 취급하지 않는다.

## 다섯 검증 축 → 앱 검사·판정·화면·증거

다음은 확정 제품 범위에 대한 구현 대응안이다. DBMS별 정확한 수집 필드와 알고리즘·인수 조건은 다음 명세 작업에서 확정한다. 연구 축과 화면 분류는 연결하되, 기존 코드에서는 제약의 미래 쓰기 시험이 G_C에, 역할의 쓰기 시험이 G_A에 들어가기도 했다는 점을 보존한다. 축 이름만 보고 읽기/쓰기를 구분하지 않는다.

| 연결 ID·축 | 기존 검사의 구체 내용·근거 | 앱에서 구현할 검사 | 판정·화면 | 구현 후 남길 확인 증거 |
|---|---|---|---|---|
| RP-D01 · G_D | `gold.data_checks`; 각 oracle `_data`·`_expected`: 행별 값·관계 복원 비교 | 대응 테이블·컬럼의 타입 있는 전체 값 비교. 행 순서와 무관하게 중복 개수도 보존. 건수·합계는 보조 신호 | 데이터: 누락·추가·값 불일치·대응/조회 불가. 값·실제 키 비노출 | 건수·합계는 같지만 값/소속을 교환한 합성 DB, NULL/빈 문자열/공백/대소문자·희소 키·중복·키 없음 반례 |
| RP-C01 · G_C | `gold.schema_checks`; oracle `_structure`: 열·자료형·필수값·키·참조·검증 상태 | 테이블/열 정의, 자료형 세부 속성, 열 순서, NULL 허용, PK/UNIQUE/FK/CHECK, 참조 동작·지연·활성/검증 상태, default/generated/identity 비교 | 구조·제약: 정의 불일치와 확인된 제약 위반을 구분 | 데이터는 같고 제약만 누락/비활성/미검증인 합성 DB. 동일 건수에서 참조만 바뀐 DB |
| RP-C02 · G_C/G_O | T11 `_structure`; T18 `_partition_mapping`·`_structure`: 표현식 고유 인덱스·분할 경계 | 인덱스 key 순서·표현식·필터·유일성·상태와 분할 key·방법·경계·부모/자식 관계 비교 | 구조·제약 또는 관련 객체의 세부 항목. 부모/자식 데이터의 중복 집계를 피하도록 범위 명시 | 표현식/필터만 변경한 인덱스, 분할 경계만 변경한 DB; 세부 지원 필드는 DBMS 명세에서 결정 |
| RP-A01 · G_A | `gold.access_checks`, 각 `_access`: owner·role·membership·default ACL·effective privilege | 동일 이름 사용자·역할·소유자, 역할 속성·상속, schema/테이블/열/함수/sequence 권한·부여 가능 여부·기본 권한·관련 서버 설정 비교 | 사용자·권한: 손실/확대/정의 차이/조회 불가. 비밀번호나 로그인 검증을 한 것으로 표시하지 않음 | 데이터 일치·읽기 권한 누락, PUBLIC/외부 역할 권한 확대, owner/상속 차이, catalog 조회 제한 사례 |
| RP-A02 · G_A/G_B | `behavior.tenant_checks`, T17 `_access`·`_future_writes`: 비소유자 역할과 RLS 실제 관측 | 기본 비교에서 행 정책·활성/강제·보안 실행 모드 정의 비교. 실제 역할 읽기/쓰기·필터 효과는 별도 시험 DB에서 준비된 계정·방법으로만 수행 | 권한 정의 비교와 실제 역할 시험의 수행 범위를 별도로 표시 | 서로 다른 역할로 허용/거부·0행 필터·실제 영향 행을 구분하는 시험. 준비하지 못한 역할은 미검증 |
| RP-O01 · G_O | `gold.object_checks`, `Database.state`, 과제별 G_O: view signature·값, routine mode, trigger binding, sequence 설정 | view/routine/trigger 정의·signature·결합·활성·실행 주체·참조, sequence 설정·소유/결합·현재 상태 비교. DBMS 제공 의존 관계의 수집 범위 기록 | 관련 객체: 정의/결합/상태 차이와 실행 미검증 분리. 함수 이름만으로 식별하지 않고 schema·signature 포함 | 함수 본문만 변경, trigger 비활성/연결 변경, view signature 변경, sequence 상태·권한·소유 차이 사례 |
| RP-B01 · G_B 및 쓰기 G_C/G_A | `behavior.future_inputs`·`audit_probe`·`sequence_probe`, 각 `_future_writes` | 별도 시험 DB에서 자료형·키·제약으로 기본 시험 생성, 사용자가 값·기대 결과·역할·트랜잭션 경계를 확인 후 실행. 임의 업무 의미를 자동 추정하지 않음 | 동작 시험: 기대 충족/불충족/판정 불가/미실행. 시험 판정과 정리 결과 분리 | 합법 입력 승인·불법 입력 거부, 새 값 조회, 자동 ID 충돌, 준비된 trigger 기대 부작용, 지연 제약의 commit 시점 검사 |
| RP-S01 · UNKNOWN/관측 격리 | `Checks.test`, `aggregate`, `evaluation_valid`, `isolation` | 읽을 수 있는 범위 사전 확인, 대상별 이유·완료/중단/미시작, 관측/파일 무결성 실패 때 해당 판정 확정 금지. 전체 재검사만 제공 | 분류별 일치/불일치 수와 판정 불가·미검증·선택 제외 범위를 함께 표시 | 연결 끊김·취소·권한 부족·정의 숨김·임시 자료 손상·공간 부족이 전체 통과로 바뀌지 않는 사례 |

G_O의 O는 객체 축이다. 아래 제외하는 운영 조건 O01–O07과 다르다.

## 19개 과제별 세부 대응

논문은 **정해진 구조/값 변환의 정확성**을 평가했고, 현재 앱은 **원본이 정확히 옮겨졌는지 엄격하게 비교**한다. 아래 변환식은 연구 과제의 합성 계약이며 앱의 모든 DB에 적용할 규칙이 아니다. 앱은 변환 의도를 묻거나 임의 변환을 정답으로 승인하지 않는다. 테이블·컬럼 이름 대응 후 값/구조가 다르면 불일치로 보고한다. 과제 전용 변환과 업무 쓰기는 연구 재현용 합성 사례 또는 사용자가 기대를 확인한 선택 시험으로만 연결한다. 이는 검사 분류를 축소한다는 뜻이 아니라 자동으로 정답을 만들 수 있는 범위를 구분하는 것이다.

| 과제 | 원 검증 코드·정확한 검사 대상 | 앱에 연결할 부분과 이식 경계 |
|---|---|---|
| T01 컬럼 rename | `gold.expected_data:26`, `behavior.view_expected:31`, `object_checks:189`: name→display_name 값·email·ID, view signature/출력, 인덱스, 새 행 조회 | 이름 대응 후 전체 값·NULL/type·열/객체·권한 비교. 고정 view의 별칭 기대는 합성 사례 전용 |
| T02 금액 backfill | `gold.expected_data:26`, `behavior.audit_probe:171`: 정확한 Decimal cents/100, 기존 cents/memo·감사 0행, UPDATE별 감사·동일값 UPDATE·임시 이름 충돌 시험 | decimal 무오차 비교·default/trigger/보안 모드·감사 테이블 값 비교. cents/100·정확히 1이벤트는 일반 DB 규칙으로 하드코딩하지 않음 |
| T03 TEXT→ENUM | `gold.schema_checks:74`, `behavior.future_inputs:113`: legacy→archived, enum labels/순서, view TEXT signature, 새 허용/금지 입력 | 열 타입·enum 정의·값·view 비교. 상태 치환은 자동 승인하지 않으며 enum 새 입력은 선택 시험 |
| T04 NULL backfill | `gold.expected_data:26`, `behavior.sequence_probe:200`: NULL만 user_<id>, 빈/공백 보존, sequence 결합·자동 ID·rollback gap | NULL/빈/공백 엄격 비교, NULL 허용·default·sequence 정의/상태 비교. 채움 식은 과제 전용. 번호 gap을 곧 데이터 손상이라 부르지 않음 |
| T05 부서 정규화 | `gold.data_checks:44`, `behavior.run:277`: code로 직원 소속 복원, 부서 집합·기존 project/assignment 관계, 조회 함수·선택 identity 발급 | 전체 행·관계·FK·함수·identity 비교, 같은 건수에서 소속 교환 반례. 부서 join 복원과 code의 업무 의미는 일반 비교의 숨은 가정으로 쓰지 않음 |
| T06 연결 테이블 | `gold.expected_data:26`, `behavior.tenant_checks:215`: non-NULL 링크 정확한 multiset, 다대다·무소속, 보존 테이블, 열 권한·RLS·tenant 쓰기 | 전체 연결 행/중복·참조·정책·권한 비교, 준비된 역할 시험. 두 tenant 이름·배정 규칙을 일반 DB에 강제하지 않음 |
| T07 주문/품목 분할 | `t07_oracle.expected_items:89`, `_structure:100`, `_access:135`, `_future_writes:167`: 슬롯→품목, 수량×단가/합계, shipment FK, position·허용 쓰기 | 행/관계·제약·함수 권한·trigger 비교와 선택 동작 시험. 자동 커밋을 숨은 요구로 강제하지 않음. 품목 범위·합계는 합성 계약 |
| T08 고객 병합 | `t08_oracle._expected:76`, `_data:100`, `_structure:124`, `_future_writes:188`: external_key/legal_id 동치, CRM 우선, 출처·하위 FK, 새 고객 쓰기 | PK/후보키·관계·출처 테이블 전체 값·identity 객체 비교. 업무 병합/우선순위는 일반화하지 않음. 내부 sequence는 발견해 비교하지만 존재만으로 손상 판정하지 않음 |
| T09 PK 전환 | `t09_oracle._data:55`, `_structure:73`, `_future_writes:127`: old ID↔public_code, payment 참조, legacy lookup, 정상/고아 입력 | 키·복합/참조 열·문자 공백/대소문자·매핑 테이블 값 비교. 키 재발급 동치는 과제 mapping 없이는 승인하지 않음 |
| T10 FK 도입 | `t10_oracle._structure:54`, `_future_writes:82`: 기존 값 그대로, FK valid, NULL 허용, 정상 연결·고아 거부 | FK 존재/열/참조/검증/활성·NULL·고아 점검을 비교와 구분, 원본에서 확인된 고아는 원본 결함 표시. 새 FK 추가는 정의 차이로 보고 |
| T11 casefold UNIQUE | `t11_oracle._structure:48`, `_future_writes:80`: 원 email 보존, lower(email) 고유 인덱스·필터, 대소문자만 다른 신규 입력 거부 | 인덱스 표현식/유일성/필터/상태 비교, 원값 대소문자 보정 금지. casefold 업무 규칙은 범용 값 비교에 적용하지 않음 |
| T12 시간대 변환 | `t12_oracle._valid_wall:13`, `_data:68`, `_structure:86`, `_future_writes:121`: offset 기반 동일 시점·wall-time 복원, DST 반복/존재하지 않는 시각·금지 zone | 날짜·시간·정밀도·zone 관련 정의와 값 비교, 명시한 기대값의 선택 시험. 두 zone·DST 날짜는 과제 전용이며 시간을 임의 보정하지 않음 |
| T13 JSON 정형화 | `t13_oracle._expected:48`, `_structure:61`, `_future_writes:93`: 키 누락·JSON null·SQL NULL·false 구분, extra 중첩 값 보존·view 재구성 | JSON/SQL NULL 및 타입 구분, 중첩/배열·미지 key 비교. 문자열 JSON과 JSONB 등의 비교 표현은 DBMS/타입별 명세로 결정. tier/marketing 추출은 과제 전용 |
| T14 정책 삭제 | `t14_oracle._expected:67`, `_data:78`, `_structure:93`, `_future_writes:142`: 날짜·legal_hold·link 기반 허용 삭제, 1회 purge log·쓰기 거부 | 누락/추가 행·로그·FK·권한 비교. 앱은 삭제 허용 의도를 묻거나 날짜로 차이를 숨기지 않음. 2025-01-01 정책은 합성 과제 전용 |
| T15 schema 이동 | `t15_oracle._catalog:47`, `_structure:88`, `_access:112`, `_future_writes:145`: schema-qualified 실제 테이블·호환 view·권한, 양쪽 API 쓰기가 같은 행에 반영 | schema를 포함한 객체 식별·수동 테이블 대응·view/routine/참조/권한 비교. 호환 view 실행은 별도 시험 DB. app/catalog 두 이름에 한정하지 않음 |
| T16 감사 구조 변경 | `t16_oracle._expected:78`, `_structure:86`, `_future_writes:150`: 과거 로그 1:1·timestamp, trigger 연결·1회 이벤트·NULL 전이·ID 중복 | 감사 테이블도 전체 값 비교, trigger/routine/sequence 결합·mode 비교. 기록 부작용 기대는 선택 시험이며 출처 불명 감사 행을 자동 정리하지 않음 |
| T17 민감 필드/RLS | `t17_oracle._secure:57`, `_expected:98`, `_structure:104`, `_access:131`, `_future_writes:189`: ID/tenant/body 대응, column ACL·owner 우회·역할별 허용/필터/수정 | 여러 schema 전체 값·권한·정책·보안 모드 비교, owner 읽기와 tenant 동작 구분. 실제 역할 시험은 준비된 별도 DB/계정에 한정 |
| T18 partition | `t18_oracle._partition_mapping:83`, `_data:109`, `_structure:125`, `_future_writes:177`: 범위로 자식 대응, 부모/자식 행·복합 FK·경계 INSERT routing | 분할 key/방법/경계·참조·부모/자식 데이터 비교, DB 정의에서 유도한 경계의 선택 시험. 연구의 네 날짜 구간은 일반 DB에 강제하지 않음 |
| T19 identity | `t19_oracle._next:27`, `_structure:78`, `_acl:32`, `_future_writes:121`: 기존 ID/title, identity mode·sequence 결합, max(frontier,maxPK+1), 자동/명시 ID | identity/default·sequence 설정·현재 상태·권한·결합 비교, 별도 DB 자동 발급 시험. `_next=last_value+is_called`는 이 과제 INCREMENT 1 조건 전용 |

## 사후 검토가 요구하는 판정 개선

| 실제 근거 | 확인된 사실 | 앱 설계에 반영할 원칙 |
|---|---|---|
| T07-R01, 별도 probe 6/6 | outsider에 신규 함수 EXECUTE가 허용됐다. 자동 커밋 오류와 독립적인 권한 증인 | 권한 확대를 데이터 일치에 가리지 않고 별도 표시. 모든 DB에서 PUBLIC 권한 자체를 결함으로 단정하지 않으며 원본 비교·명시 기대를 기준으로 판정 |
| T07-R02/R05/R07/R10, 각 6/6 | 지정 계약에서 금지한 세 번째 품목 입력이 수락됐다 | 단순 건수/합계 또는 기존 데이터 일치와 미래 제약을 분리. 일반 DB에는 position 1·2 규칙을 자동 도입하지 않음 |
| T07-R08, 6/6 | 한 트랜잭션의 합법 주문/품목 쓰기도 42501로 실패했다 | 실제 시험 역할·positive control·실패 단계 기록. 앱 자신의 조회 권한 부족을 이런 동작 불충족과 구분 |
| T12-R03, 6/6 | 기존 값 변환과 조회는 통과했지만 금지된 신규 시각 입력을 수락했다 | 일회성 데이터 검사·지속 제약 정의·선택 쓰기 시험을 각각 표시 |
| T07-R06, 6/6 | 주문 단독 자동 커밋은 실패했으나 합법 주문/품목의 단일 트랜잭션은 성공했다 | 시험의 트랜잭션 경계·문장 순서·commit 시 기대를 명시. 미시험 경로를 성공/실패로 확대하지 않음 |
| T08-R06/R08, 각 6/6 | 허용된 IDENTITY 내부 sequence가 있었고 최신 별도 probe의 자동 ID 삽입도 성공했다 | 객체 전체를 발견해 엄격 비교하되 추가 객체의 존재·정의 차이와 입증된 결함을 분리. 옛 검토의 미시험 상태와 최신 제한 관측을 구분 |
| T05 populated UNKNOWN 54개 | 보존 원장의 `G_C.no_unrequested_nonkey_constraints`가 모두 UNKNOWN. 인식하지 못한 추가 제약의 의미 동등성을 증명하지 못했다 | 정의 차이는 보고할 수 있지만 의미적 위반은 증거 없이 확정하지 않음. 일치/불일치와 건강성 판단을 분리 |

동결 전 교정에서도 특정 로그 이유·함수/trigger 이름·partition 이름·sequence 내부 표현·정수 저장 폭의 과잉 강제가 발견됐다. 앱은 현재 두 DB의 엄격한 정의 차이를 숨기지 않되, 그 차이를 자동으로 업무 손상이나 결함으로 바꾸지 않는다. '원본에 존재하는 결함'은 명시한 불변식 위반을 실제 관측한 경우에만 붙인다.

## 그대로 옮기지 않을 구현과 누락 위험

아래는 코드에서 확인한 제품 이식상의 차이이며 논문 결과를 소급 변경하는 근거가 아니다.

| 확인 지점 | 원 구현의 범위/한계 | 앱에서 필요한 처리 |
|---|---|---|
| `Harness.query`, `Database.state` | `fetchall`, 전체 dict/list, `ORDER BY 1,2`, snapshot 전체 JSON/digest. 고정 fixture 크기·열 구성 | 분할 읽기·타입별 무손실 표현·키 기반 대응 또는 키 없는 행 multiset 비교·보호된 외부 처리·완전성 검사. 샘플/건수/해시만으로 정확 일치 확정 금지 |
| `Harness.query`, Gold evidence | SQL·params·결과 rows·expected/actual·예외 문자열을 기록. 합성 연구용 | 비교 중 값과 영구 보고서를 분리. 위치/종류/내부 식별번호만 기록. DB 오류·정의 SQL에도 값/비밀이 섞일 수 있으므로 기록용 표현을 별도 설계 |
| `Database.state` catalog | 고정 schema·제한 relkind·지정 역할만 수집. FK 대상은 schema 없는 이름, 일부 함수 ACL은 signature 없는 이름. 수집한 정보 전부를 모든 과제가 판정하지 않음 | schema와 객체 종류·routine signature를 포함한 식별, DBMS별 전체 수집/검사 목록·완전성 표시. materialized view·domain·확장 타입 등 현재 코드가 포괄하지 않는 범위도 지원/미검증을 명시 |
| `gold.schema_checks`와 oracle `_structure` | 어떤 경로는 열 set을 비교해 순서 누락, 어떤 경로는 일부 FK/제약만 존재 검사. 수집 가능한 deferrability·참조 열/동작·definition을 일관되게 비교하지 않음 | 공통 검사 항목별 명시적인 필드 비교. 과제 하나의 통과를 모든 제약/객체 보존으로 확대하지 않음 |
| `Checks.test` | 연결/일반 예외는 UNKNOWN이나 대부분의 SQL 오류는 FAIL. 접근 거부가 기대인 probe와 observer 권한 부족을 공통 helper만으로 구분하지 못함 | 검사 목적·사전 확인·DBMS 오류 종류에 따라 불일치/판정 불가/기대 거부 구분. 서버 오류의 존재만으로 데이터 손상 판정 금지 |
| `_access`, 과제별 G_O | view 조회·routine 호출 일부가 post에서 직접 이루어짐. 겉보기 SELECT의 무변경을 일괄 보장하는 경로가 아님 | 기본 비교는 메타데이터·검증 가능한 데이터 읽기로 구성. 사용자 정의 view/routine 실행은 별도 시험 DB에 연결하고 단순 정의 수집과 구분 |
| `Database.clone/drop`, fixture 준비 | CREATE DATABASE/template 복제·관리자 관측·고정 역할 생성·전용 clone 폐기를 전제 | 앱은 실제 원본/대상 접속 계정의 권한을 확인. 자동 전체 복제·역할 생성·DB 삭제 기능을 가져오지 않음. 선택 시험 자료 정리는 확정 정책 준수 |
| `sequence_probe`, T19 `_next` | INCREMENT 1·비순환·고정 cache·정지 상태를 전제. rollback gap은 허용 | 설정/상태의 차이를 보고, 충돌·소진 등 건강성은 지원 조건에서만 판정. 비교를 위해 nextval/자동 INSERT로 원본·대상을 전진시키지 않음 |
| 전후 digest/isolation | 관측한 catalog/행/sequence가 같다는 확인. 수집하지 않은 외부 부작용·모든 객체·전체 DB 무변경 증명은 아님 | 원본/대상 쓰기 경로 분리·무변경 구현 검증과 수집 범위 기록. snapshot hash 일치만으로 부작용 없음 보장 금지 |
| B/B+/V/G_all/classify | 실험 실행·기능·계약과 task별 reference를 묶은 연구 분류. B+는 T01–T06만 적용 | 앱 결과는 확정된 실행 상태/검사 판정/선택 범위 모델로 새로 집계. 'BASELINE_MISSED_CONTRACT_VIOLATION' 등을 사용자 DB의 안전 인증으로 사용하지 않음 |

현재 코드에 없는 다중 DBMS adapter, 인증/TLS, 권한 자동 안내 UI, 대용량·임시 파일 무결성/보호/정리, 중단/취소, 값 비노출 보고서, Windows/macOS 패키지는 신규 구현·실제 검증이 필요하다. 이 분석은 이들의 지원을 입증하지 않는다.

## 이식 경계와 다음 명세 작업

2026-10-02 첫 실제 이식 증거가 생겼다. RP-D01은 PostgreSQL 17 합성 DB에서 같은 건수의 문자·중복 차이, 순서 무관·큰 numeric 값으로 확인했다. RP-C01은 일부 컬럼 속성 수집·차이 보고만 구현했으며 SCHEMA-01 전체를 통과로 표시하지 않는다. RP-S01은 중단 시 차이 보존, RLS/권한·미지원/큰 행·임시 손상에서 일치 차단, 안전한 파일 재열기와 연결했다. 세부 증거·남은 RP 범위는 [NATIVE-01 구현 기록](native-implementation.md)과 [검증 기록](../evidence/native-01/review.json)이 정본이다. 연구 전체 Gold·G_A/G_O/G_B가 제품 구현 완료된 것은 아니다.

논문 검증기는 Python·PostgreSQL 과제별 기대 변환·역할·객체와 SQL probe를 포함한다. 임의 DB에서 동일한 Gold를 실행할 수 있다고 가정하지 않는다. 공통 원리를 C#·다중 DBMS에 이식하는 새 제품 검증이 필요하다. 위 대응표는 정지 상태 검증 경로 분석의 결과이며 제품 코드나 테스트 완료표가 아니다.

- **이어갈 작업:** 각 RP 항목을 DBMS별 대상·수집 필드·접속/권한 조건·읽기/쓰기 경계·비교 알고리즘·예외/범위 판정·반례·증거로 구체화하고 [요구사항](requirements.md)의 인수 기준과 연결한다.
- **기본 비교:** 원본과 대상의 엄격한 일치/차이와 관측 가능 범위를 확인한다. 업무 변환의 정당성을 숨은 규칙으로 판정하지 않는다.
- **선택 시험:** G_B 및 실제 실행이 필요한 G_A/G_C/G_O 부분은 별도 시험 DB와 사용자가 확인한 기대에 한정한다. 준비/지원/권한이 부족하면 미검증으로 남긴다.
- **제품에서 제외:** 마이그레이션 실행, 원본/대상의 시험 쓰기, 자동 복제/수정, O01–O07 운영 시험. 운영 구현 전체를 이번 이식 대상으로 분석하거나 지원했다고 주장하지 않는다. 앱 검사 중단 표시와 O04의 마이그레이션 중단/재시도 시험은 구분한다.
- **지속할 핵심:** 상세 비교를 자동화하고 미검증 영역을 명확히 표시한다. 연구를 제품에서 썼다는 주장은 RP 대응별 실제 검사·판정·화면·증거가 생긴 뒤 완성한다.

심사자료 DOI: https://doi.org/10.5281/zenodo.23033566 — 메타데이터와 제한 파일 접근을 구분한다. 비밀 링크·비공개 원자료를 이 저장소에 넣지 않는다. 기존 도구 대비 우월성과 외부 독립 검증을 주장하지 않는다.

## IMPLEMENTATION-02 현재 이식 증거

RP-D01: 수동 테이블·컬럼 대응 뒤 순서가 바뀐 전체 값을 실제 PostgreSQL에서 엄격 비교하며 이름/순서 차이를 숨기지 않는다. RP-C01: 선언·NULL·default/generated/identity·제약 정의/참조 동작/지연/검증 상태의 속성 변형을 관측했다. RP-C02: 인덱스 키/포함/식/조건/방식/상태와 다단 partition 연결·경계의 실제 차이를 표시했다. RP-S01: 부분 관측·중복 fact·잘못된 보고서의 일치 차단과 차이/미검증 보존을 연결했다. 최종 168 시험과 한영 native/파일 증거는 [현재 검토](implementation-review.md)와 [증거 안내](../evidence/implementation-review/README.md)를 따른다. 원본 위반(DATA-02), RP-A/O/B 전체 이식은 아직 완료가 아니다. NATIVE-01의 이전 부분 구현 기록과 현재 확장을 구분한다.
