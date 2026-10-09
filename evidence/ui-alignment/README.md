# UI-ALIGN-01 · 최종 교정 증거

2026-10-02. 웹 시안의 구조·위계·정렬·조작을 C#·Avalonia 네이티브 화면으로 교정했다. **이번 macOS 교정 묶음은 완료이고 전체 제품/BR-B·Windows 실제 UI 인수는 남아 있다.** 모든 데이터/암호는 격리된 합성 fixture이며 사용자 DB는 사용하지 않았다.

## 화면 대조

9페이지 × 960×720 / 1138×1088 / 1440×900 × 한국어/영어 = native 54개 + web 54개. 같은 논리 client 크기로 캡처했다. native Retina는 물리 2배이며 OS title bar 28 DIP가 추가된다. JPEG 헤더 실제 크기와 이름을 전부 대조했다. 요청한 1138×1097은 macOS 창 높이 제한으로 1088까지만 확인했으며 1097 통과로 세지 않는다. 축소/잘못된 크기 이름·수정 전 상태는 intermediate에 보존하고 최종 증거에서 제외했다.

| 페이지 | macOS 실제 앱 대표 | 웹 기준 대표 |
|---|---|---|
| 시작 | [00-final-home…](00-final-home-1440x900-kr.jpg) | [web-00-home…](web-00-home-1440x900-kr.jpg) |
| 연결 | [01-final-connect…](01-final-connect-1440x900-kr.jpg) | [web-01-connect…](web-01-connect-1440x900-kr.jpg) |
| 사전 확인 | [02-final-preflight…](02-final-preflight-1440x900-kr.jpg) | [web-02-preflight…](web-02-preflight-1440x900-kr.jpg) |
| 선택 | [03-final-selection…](03-final-selection-1440x900-kr.jpg) | [web-03-selection…](web-03-selection-1440x900-kr.jpg) |
| 시험 안내 | [03-final-trial…](03-final-trial-1440x900-kr.jpg) | [web-03-trial…](web-03-trial-1440x900-kr.jpg) |
| 실행 전 | [04-final-review…](04-final-review-1440x900-kr.jpg) | [web-04-review…](web-04-review-1440x900-kr.jpg) |
| 진행 | [05-final-progress…](05-final-progress-1440x900-kr.jpg) | [web-05-progress…](web-05-progress-1440x900-kr.jpg) |
| 결과 | [06-final-results…](06-final-results-1440x900-kr.jpg) | [web-06-results…](web-06-results-1440x900-kr.jpg) |
| 저장 | [07-final-save…](07-final-save-1440x900-kr.jpg) | [web-07-save…](web-07-save-1440x900-kr.jpg) |

파일 이름 뒤 크기와 kr/en을 바꾸면 나머지 대조 캡처가 있다. [review.json](review.json)은 각 캡처의 실제 크기·해시, 기준 HTML/현재 소스·macOS/Windows 바이너리 해시를 보관한다. 소형 창에서 긴 문장/본문은 스크롤하고 고정 footer가 조작 영역을 가리지 않는지 관찰했다. OS title bar·file dialog·글꼴 raster의 동일 픽셀을 요구하지 않았다. 기준 HTML은 변경하지 않았다.

## 실제 검사·파일 흐름

1. 격리된 PostgreSQL17의 합성 원본/대상 8개 테이블에 strict TLS로 연결했다. [암호 마스킹](01-password-masked-kr.jpg)과 [실제 사전 확인](02-preflight-8-tables-kr.jpg)을 확인했다. source/target에 앱 쓰기는 없다.
2. 실제 대량 합성 입력(각 DB 500만 행)을 읽으며 세 크기 KR/EN 진행·resize 상태 보존을 확인했다. 영어 동작 줄이기에서도 실제 처리 행/분류·정적 진행 표시가 유지됐다. 총량/남은 시간을 추정하지 않았다. 규모별 성능·전체 자원 인수는 아니다.
3. 취소하면 DATA-01/SCHEMA-01은 Interrupted, 아직 시작하지 않은 ACCESS/OBJECT 등은 Pending이며 기존 차이를 보존한다. [최신 보고서](postgres-progress-final.json)는 `c461d961-75d0-45c2-b71e-77419da603ff`, [저장 완료](07-latest-saved-kr.jpg), [최신 앱 재열기](06-latest-reopened-kr.jpg)로 연결된다. 별도 [이전 중단 보고서](postgres-interrupted-final.json)의 `fd471d22-a9e6-4384-9b22-7cb323e54e37`도 보관한다.
4. 시작 화면에서 [의도한 형식 오류 파일](invalid-result.json)을 열어 [복구 가능한 안내](00-invalid-file-kr.jpg) 뒤 정상 실제 파일을 열었다. JSON 저장 취소는 [취소 안내](07-save-cancel-kr.jpg)를 보여준다. 실제 보고서 3개의 checksum과 원값/접속 비밀 키·합성 비밀 marker 부재를 재확인했다. checksum은 작성자 서명이 아니다.
5. [38개 회귀 결과](regression-38.trx): 통과38·실패0·미실행0. TLS·무쓰기 snapshot·엄격 값 대조·임시 무결성·중단/미시작·안전한 보고서 반례를 포함한다. 테스트 이름은 review.json에 있다. macOS arm64와 Windows x64 잠금 publish는 성공했다. macOS만 실제 실행했으며 Windows 런타임을 검증했다고 주장하지 않는다.

## 버튼과 선택 동작

아래는 실제 macOS 관찰과 제한이다. 모든 실행 Action의 공통 Pulse180ms·현재 단계/notice/dialog/선택 상태 전환, 포인터/키보드 focus·pressed·disabled를 소스와 실행 관찰로 대조했다. 기본 화면 전환200ms, 언어/고급 설정/notice160ms, dialog280ms, 최종 숫자 reveal220ms·막대320ms다. 숫자는 실제 최종값을 즉시 표시한다. 정지 캡처만으로 애니메이션을 증명하지 않으며 관찰 기록이지 녹화·프레임 성능 측정은 아니다. 동작 줄이기에서는 같은 실제 결과·초점·문구가 정적으로 남는다.

| ID | 행동 | 상태 | 확인 결과·제한 |
|---|---|---|---|
| B01 | 새 검사 시작 | 실행 확인 | 새 상태/결과 분리→연결. 주요 52h·페이지 전환 |
| B02 | 검사 결과 파일로 보기 | 실행 확인 | 홈에서 DB 없이 두 실제 보고서 재열기. 잘못된 형식 오류 뒤 정상 복구·open 취소 |
| B03 | 예시 결과 보기 | 실행 확인 | 합성 엔진 결과와 실제 DB를 별도 표시. 설명 preview 3/2/45와 구분 |
| B04 | 연구 반영 보기 | 실행 확인 | RP 연결·modal 진입/Tab/Escape/호출 초점 복귀 |
| B05/B07 | 두 연결/조회 확인 | 실행 확인 | 한 실제 Connect+Inspect 경로. 포트/쓰기 정지 차단·TLS 접속·busy 중복 차단 |
| B06 | 고급 연결 설정 | 실행 확인 | 전체 헤더 펼침/접힘·CA 필드·160ms 진입·입력 보존·Enter 재실행 초점 유지 |
| B08 | 필요한 권한 보기 | 실행 확인 | 관측한 제한과 필요한 권한 안내. 자동 부여 없음 |
| B09 | 검사 범위 선택 | 실행 확인 | 사전 표의 조회 제한을 선택/결과까지 유지 |
| B10 | 세부 검사 펼침 | 실행 확인 | 64h 전체 헤더·caret/본문 전환. 독립 checkbox Space 4→3→4 |
| B11/B25 | 대응 변경/대상 선택 | 제한 안내 확인 | 동일 이름만 지원. 수동 대응 미구현 modal·Escape 복귀; 실제 적용은 미검증 |
| B12 | 시험 DB 안내 | 제한 화면 확인 | 별도 DB 패널·비활성 입력/실행·원본/대상 쓰기 없음 |
| B13 | 시험안 준비/확인 | 미구현·비활성 확인 | 실제 생성/분리/쓰기 경로 없음. 준비 완료 우측 DB 표시 F05는 native 실제 인수 미완료 |
| B14 | 실행 전 확인 | 실행 확인 | 검사/테이블 선택 실제 요약. 빈 선택 차단. 검색의 숨은 선택 유지 |
| B15 | 비교 시작 | 실행 확인 | 확인 checkbox 전 disabled, 후 enabled. 실제 KR/EN 긴 실행·background 진행 |
| B16 | 검사 취소 | 실행 확인 | 요청 접수→부분 결과 정리→중단. 시작한/미시작 구분·기존 차이 보존. 요청 중은 매우 짧아 정지 캡처로 증명하지 않음 |
| B17 | 결과 보기 | 실제 종료 전환 확인 | native는 완료/중단 최종화 후 S6 자동 전환; 별도 중간 완료 클릭 없음 |
| B18 | 결과 행 전체/필터 | 실행 확인 | 80h 행 전체 상세·RP/위치·불일치/확인 불가 필터·스크롤/초점. 수치 겹침 설명 |
| B19 | 결과 저장 | 실행 확인 | 같은 보고서/run 범위 유지→S7·페이지 전환 |
| B20 | 선택 형식 저장 | 실행 확인·JSON 한정 | JSON 전체 card Space off→저장 차단→on 복구. native 저장·readback·취소. CSV/HTML 비활성 |
| B21 | 전체 재검사 | 실행 확인 | 새 run 준비·이전 부분 결과 합치지 않음·이어하기 없음 |
| B22 | DBMS 선택 카드 | 실행 확인·2종 | Left/Right/Down→SQL/PG 선택·포트1433/5432·초점. MySQL/MariaDB 비활성 |
| B23 | 검색 결과 전체 선택 | 실행 확인 | 현재 보이는 테이블만 선택/해제·일부 상태·빈 검색 비활성 |
| B24 | 선택 해제 | 실행 확인 | 전체7개→검색1개 해제→6/7 유지. 숨은 선택 보존 |
| B26 | 검색 지우기 | 실행 확인 | 빈 결과→전체 목록 복귀·선택 유지·입력 초점/개수 갱신 |
| N01 | 뒤로/방문 단계 | 실행 확인 | 입력 보존·미래/진행 중 이동 차단. 연결 복귀시 snapshot 폐기 |
| N02 | dialog 닫기/취소 | 실행 확인 | Tab 두 닫기 조작·Escape·호출 초점 복귀. OS 파일 dialog는 OS native |
| N03 | 언어/동작 줄이기 | 실행 확인 | 언어 후 입력/선택/결과 유지·Enter 같은 버튼 재실행·160ms 진입. reduced motion 정적 대체·busy 변경 차단 |

[고급 설정 초점](01-final-advanced-focus-kr.jpg), [동작 줄이기 입력 보존](01-final-reduced-motion-en.jpg), [DBMS 방향키](01-keyboard-dbms-kr.jpg), [최종 연구 dialog](research-dialog-final-kr.jpg)도 보관했다. 새 언어 버튼은 전환 뒤 Enter로 다시 실행되고 고급 설정도 Enter 접힘 뒤 호출 초점을 유지했다. modal에서 Tab→닫기·Escape→원래 연구 버튼 초점 복귀를 관찰했다.

## 의도한 차이와 남은 인수

- 접속/조회는 실제 단일 경로이며 종료 후 자동 결과 전환이다. 예시의 가짜 성공/별도 완료 클릭을 이식하지 않았다.
- 수동 대응·시험 DB 준비/쓰기·CSV/HTML은 기능이 미구현이므로 안내와 disabled 상태를 확인했다. 실제 준비 완료의 우측 DB 정보 F05는 아직 native 실제 동작 인수 완료가 아니다.
- 실제 CheckResult는 불일치/확인 불가가 겹칠 수 있다. 배타적 누적/성공률 대신 공통 0 기준의 독립 수치/막대를 썼다. native Grid 표현이며 LiveCharts2 적용 계획은 별도 남는다.
- Windows 실제 UI·두 OS 배율/모니터 전환·처음 보는 동료 1분 평가/익명 감상·미구현 전체 기능·배포/사이트 연결은 남는다. UI에서 timeout/강제 끊김을 새로 주입하지 않았고 엔진 반례를 UI 실행 증거로 대체하지 않는다.
- 코드는 로컬 변경이며 commit/push·서명 배포하지 않았다. 일회용 fixture를 제거한 뒤에도 [같은 보고서 재열기](06-reopened-without-db-kr.jpg)를 확인했다. R12에서 이번 임시 AGENTS 절을 제거했고 영구 RULE/디자인 기준은 유지했다.
