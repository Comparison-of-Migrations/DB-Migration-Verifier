# DB-Migration-Verifier

마이그레이션을 마친 개발자가 데이터·구조·객체·사용자·권한의 차이와 확인하지 못한 영역을 파악하도록 돕는 데스크톱 앱을 개발한다.

현재는 **C#·Avalonia 첫 실행판(0.1)**이다. PostgreSQL 17 실제 접속·일부 타입 전체 행 비교, 테이블·컬럼 수동 대응과 구조·제약·인덱스/partition catalog 비교, 선택·미검증·중단·JSON 저장/재열기와 CSV·HTML 보고서 내보내기가 연결됐다. 전체 제품 완성과 BR-B 최종 제출은 아직 아니다. [현재 구현·검증 범위](docs/implementation-review.md)를 먼저 확인한다. [HTML 시안](examples/ui-prototype/README.md)을 그대로 복제한 별도 `--replica-preview` 화면도 제공한다.

## 실행

.NET SDK **10.0.401**을 설치한 뒤 저장소 루트에서 실행한다. 패키지 버전과 lock 파일을 저장소에 고정했다.

```sh
dotnet restore --locked-mode
dotnet run --project src/Verifier.Desktop -p:RestoreLockedMode=true
```

macOS 개발용 .app은 `sh tools/package-macos.sh`로 만든다. `artifacts/dev/DB-Migration-Verifier.app`을 열면 별도 SDK 없이 실행한다. 서명·공증된 정식 배포물이 아니다. Windows는 `dotnet publish src/Verifier.Desktop -c Release -r win-x64 --self-contained -p:RestoreLockedMode=true -o artifacts/dev/win-x64`로 별도 실행 묶음을 만든다. 기본 DB 실행 경로는 Windows 교차 빌드만 확인했고 실제 값 검사·ACL은 미검증이다. 별도 복제 예시 화면의 실제 Windows UI 검증 범위는 아래와 같다.

검증용으로 설치했던 전용 SDK·빌드·런타임 캐시는 2026-10-09 사용자 요청으로 삭제했다. 기존 시스템 SDK 10.0.401은 남아 있으므로 위 실행 명령을 사용하면 된다. PATH에 없으면 `DMV_DOTNET`에 사용할 SDK 실행 파일 경로를 지정한다. Windows 검증 VM도 추가 허용에 따라 삭제했으며, 이전 검증 기록과 프로젝트 소스는 보존했다. [정리 내역](docs/replica/storage-cleanup-20261009.md)을 따른다.

## 원본 웹 복제 화면 실행

현재 웹 복제 화면은 기존 앱의 별도 실행 경로로 연결했다. 같은 SDK에서 다음과 같이 연다.

```sh
dotnet run --project src/Verifier.Desktop -p:RestoreLockedMode=true -- --replica-preview
```

이 화면은 원본 웹의 예시 연결·검사·결과 저장 흐름을 재현한다. 실제 DB 접속·마이그레이션 검증 결과와 구분한다. 한국어/영어, 작은/큰 데스크톱 창, 상태·입력·파일 대화상자·애니메이션을 원본과 비교한 범위와 완료된 전체 흐름 검증은 [복제 현재 상태](docs/replica/COMPONENT_STATUS.md)와 [통합 증거](docs/replica/specs/PageIntegration.md)에 기록한다. macOS와 Windows 전체 제품 빌드는 통과했으며, Windows 실제 UI 검증은 Parallels의 Windows 11 ARM에서 win-x64 실행 환경을 사용한다. 한국어/영어9페이지의 구현·실제 앱 연결·전체 흐름 검증을 완료했다. Windows 영어 흐름은 동일 앱 DLL을 공식 x64 실행기로 실행해 확인했으며, 배포용 apphost 시작 문제의 원인은 미확정이므로 배포 검증 완료로 주장하지 않는다. 위 기본 실행 경로의 실제 DB 값 검사·Windows ACL 검증 완료를 뜻하지 않는다.

## 세 행동과 확인 방법

1. **두 DB 연결** — 같은 DBMS의 서로 다른 원본·대상을 입력하고 양쪽 쓰기 정지를 확인한다. PostgreSQL은 TLS VerifyFull이며 사설 CA의 경우 인증서 경로를 지정한다. 서버 식별을 위한 `pg_control_system()` 조회 권한이 필요하다. 비밀번호는 저장하지 않고 화면·접근성에서도 마스킹한다.
2. **범위 선택·비교** — 사전 확인에서 가능한 부분을 보고 분류와 테이블을 선택한다. 값·중복·누락/추가의 엄격한 행 다중집합을 비교한다. 같은 건수라도 값이 다르면 불일치여야 한다. 구현·관측이 부족하면 일치 대신 미검증/판정 불가가 나와야 한다.
3. **차이·미검증 확인·보관** — 결과 분류 행 전체를 눌러 검사·RP 연구 ID·안전한 위치·종류·내부 번호를 확인하고 JSON·CSV·HTML 중 필요한 형식으로 저장한다. 시작 화면의 ‘검사 결과 파일로 보기’로 DB 접속 없이 같은 run ID·선택 테이블·판정·미검증 이유를 다시 확인한다.

‘예시 결과 보기’는 같은 엔진을 합성 자료로 실행한다. 실제 DB 결과와 구분한다. [보관된 합성 DB 결과](evidence/native-01/postgres-result.json)는 실제 TLS PostgreSQL fixture의 결과이며 원값·접속 정보가 없다. 파일 checksum은 우발적 손상 확인용이며 작성자 인증 서명이 아니다. 잘못된 형식·버전·checksum은 열기 오류로 처리한다.

## 첫 실행판의 경계

- PostgreSQL 17의 일반 기본 테이블, 같은 이름 자동 대응과 테이블·컬럼 일대일 수동 대응, 전체 행의 중복 보존 비교. int2/int4/int8/text/varchar/bpchar/bool/UUID/numeric/bytea codec을 구현했으며 실제 fixture의 상세 검증 타입은 구현 기록에 구분한다. 큰 숫자는 임의 정밀도로 비교한다.
- RLS·외부/분할·계산 컬럼·미지원 타입은 확인 불가다. 키에 따른 수정 컬럼 추적은 후속 작업이며 현재는 어느 쪽에만 관측된 행인지 보고한다. 대응은 값 변환 승인이 아니며 이름·타입·순서 차이는 구조 결과에 남긴다. 실제 행 키·값을 노출하지 않는다.
- 행당 1 MiB 관측 제한을 초과하면 판정하지 않는다. DB 총 크기에 따른 샘플·건수/해시 통과는 없다. 대형 LOB 스트림, 대규모 메타데이터·결과의 메모리 예산과 전체 자원 검증은 남아 있다.
- 임시 조각은 macOS/Unix owner-only, run별 메모리 키의 AES-GCM, 순서·길이·건수·완료 표식 검증과 정상 종료/취소 정리를 갖춘다. Windows ACL, 강제 종료 후 잔여 정리, 경로 바꿔치기·공간 부족의 전체 반례는 미완료다. Windows 값 경로는 현재 판정을 차단한다.
- MS SQL 접속·TLS 설정·일부 컬럼 메타데이터 코드는 구현했다. 실제 서버 검증·전체 값은 남아 있다. MySQL/MariaDB 어댑터는 아직 없다.
- PostgreSQL의 테이블/컬럼 선언·제약·인덱스/partition 속성은 실제 catalog를 비교한다. 제약 위반과 권한·객체 전체 검사, 별도 시험 DB 쓰기는 아직 남아 있다. 첫 실행판에는 쓰기 경로가 없다. 이력·OS 비밀 저장소·LiveCharts2·정식 릴리스·포트폴리오 연결·첫 방문 동료 평가는 후속 작업이다.

## 개발 검증

```sh
dotnet test
sh tools/test-postgres.sh
```

두 번째 명령은 Docker·OpenSSL·Python 3가 필요하다. 고정된 공식 PostgreSQL 17 이미지의 **새 일회용 컨테이너**만 만들고 loopback의 임의 포트에 TLS로 연결한다. 기존 DB·컨테이너를 사용하지 않는다. fixture 값·계정은 합성이며 암호/인증서는 임시 폴더에서 생성하고 종료 후 컨테이너·자체 이미지·폴더를 정리한다. PostgreSQL 환경이 없으면 관련 테스트는 명시적으로 건너뛰며 통과로 계산하지 않는다.

첫 실행판 이력은 [구현 기록](docs/native-implementation.md)과 [NATIVE-01 검증](evidence/native-01/review.json)에 있다. 이후 macOS 화면 교정·한국어/영어 대조·38개 회귀와 제한은 [UI-ALIGN-01 기록](docs/ui-alignment.md) 및 [현재 화면 증거](evidence/ui-alignment/README.md)를 따른다.

## 문서 지도

| 목적 | 정본 |
|---|---|
| UI 1차 체크포인트·남은 작업 | [체크포인트](docs/ui-first-checkpoint.md) |
| Codex 작업 시작 | [AGENTS.md](AGENTS.md) |
| 사용자 확정 규칙 | [RULE.md](RULE.md) |
| Web → Avalonia 복제 규칙·압축 후 복구 | [MASTER_RULES](docs/replica/MASTER_RULES.md) |
| 복제 컴포넌트 현재 상태·LOCK | [COMPONENT_STATUS](docs/replica/COMPONENT_STATUS.md) |
| 복제 컴포넌트 의존·원본/구현 위치 | [COMPONENT_TREE](docs/replica/COMPONENT_TREE.md) |
| 화면 작업마다 읽을 디자인 맥락 | [디자인 brief](.ui-craft/brief.md) |
| Discord 다크 테마를 참고한 색·선택 요소·동작 기본값 | [화면 토큰](.ui-craft/tokens.md) |
| 눌러 볼 화면 시안·확인 방법·예시 증거 | [시안 안내](examples/ui-prototype/README.md) |
| 화면 흐름·접속·상세 검사·판정 설계 | [제품 설계](docs/product-design.md) |
| 연구 결과의 제품 반영 | [연구 연결](docs/research-to-product.md) |
| 요구사항·반례·확보할 검증 증거 | [요구사항](docs/requirements.md) |
| 현재 상태·미확정·다음 작업 | [상태](docs/status.md) |
| 웹 시안과 실제 앱의 페이지별 교정 기록 | [UI 교정](docs/ui-alignment.md) |
| 레이아웃 이론·전 페이지 수직 정렬·최신 실제 증거 | [수직 정렬](docs/ui-vertical-alignment.md) |

`src/`는 Core/Data/Desktop, `tests/`는 공통 반례와 합성 PostgreSQL 검증, `tools/`는 실행·개발용 묶음·격리 테스트 절차다. 실제 사용자 데이터·비밀번호는 저장소에 보관하지 않는다.

## 결과 파일 보관

세 형식을 선택하고 형식마다 저장 위치를 확인한다. 중간 취소/실패 시 이미 저장·검증한 형식을 표시하며 화면의 결과는 유지된다. JSON은 앱 재열기용, CSV는 전체 검사/issue/finding/선택 범위의 읽기용 표, HTML은 외부 요청 없이 브라우저에서 읽는 보고서다. CSV·HTML에도 화면 필터와 관계없이 선택 제외·중단·미검증을 포함한다.

HTML은 메타문자를 escape하며 실행 스크립트를 포함하지 않는다. CSV는 필드를 quote하고 수식형 이름에 보호용 작은따옴표를 붙인다. 원래 이름은 JSON에 보존한다. CSV 재저장의 안전을 모든 스프레드시트 버전에 보장하지 않으며 DB로 다시 입력할 데이터 파일이 아니다. JSON checksum은 우발적 손상 검출용으로 작성자 인증이 아니다. 빈 inventory나 모순된 상태를 일치로 받아들이지 않는다. 현재 큰 행/대형 결과·Windows 실제 실행 등 지원 제한은 [구현 검토](docs/implementation-review.md)를 따른다.
