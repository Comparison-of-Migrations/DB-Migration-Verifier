# IMPLEMENTATION-02 · 진행 중 증거

2026-10-02. 전체 검토 과정은 아직 진행 중이다. I01·I02 완료 부분만 아래에 구분한다. 모든 DB·보고서 내용은 합성 예시이며 실제 행 값/비밀번호는 포함하지 않는다.

## I01 · 잘못된 판정·파일 방지

- `i01-i02-tests.trx`: 실제 disposable PostgreSQL 17 TLS fixture 포함 59 통과, 실패/건너뜀 0. 빈 실제 DB, 정상 pgfoo schema 발견·차이, 순서/정확 수치/중복/RLS/권한/큰 행/TLS/무변경 및 파일·임시 무결성 반례.
- `i01-i02-core.trx`: 첫 core 46 통과. UI 색/HTML 읽기 label 변경 뒤 같은 46개를 재확인했다. 최종 core 로그는 아래 manifest에 연결한다.
- `i01-contradictory.json`: checksum을 다시 계산했지만 Equal+미확인 issue인 잘못된 합성 파일. 앱은 받아들이지 않는다.
- `i01-rejected-home-kr.*`: 오류 후 시작 화면 보존. `i01-rejected-preserve-en.*`: 이전 구현판의 안전한 오류·기존 run 보존. 최종 앱의 오류색/문구는 한국어 home 증거를 따른다.

## I02 · 실제 저장·재열기

- 최종 `i02-final-kr.json`, `.csv`, `.html`, `i02-final-en.html`: native 저장 dialog로 직접 만든 파일. CSV·HTML은 12개 모든 검사와 제외·미시작·판정·이유·finding을 보관한다. 값/키/접속 정보가 없다. JSON은 앱 재열기, CSV는 읽기용 표, HTML은 브라우저 읽기용이다.
- `i02-final-complete-kr.*`, `i02-final-complete-en.*`: 실제 파일 쓰기·다시 읽기 무결성 검증 뒤 초록 체크 완료 안내. 저장 완료는 DB 전체 일치를 뜻하지 않는다.
- `i02-save-960-{kr,en}.*`, `i02-save-1138-final-{kr,en}.*`, `i02-save-1440-{kr,en}.*`: 새 저장 화면의 desktop 창/언어 배치. 작은 창에서 본문은 스크롤하며 하단 실행 버튼을 유지한다.
- `i02-reduced-keyboard-kr.*`: Space 형식 토글·동작 줄이기. 미선택 저장 차단은 `i02-no-format-en.*`.
- `i02-partial.json`, `i02-partial-cancel-kr.*`: JSON 완료 뒤 CSV 단계 취소. JSON 보존과 CSV/HTML 미저장 표시. 앞서 찍은 `i02-save-complete-*`, `i02-save-all-*`는 안내 톤 수정 전 관측이며 최종 상태 증거와 구분한다.
- `i02-reopened-final-kr.*`: DB 연결 없이 같은 run을 JSON으로 다시 확인. `i02-html-final-{kr,en}.png`, `.ax.txt`: 실제 최종 파일을 브라우저에서 읽은 결과. 검사마다 번역 label과 안정된 enum 코드가 나란히 있다.

## 확인 경계

native macOS arm64 실행과 Windows x64 교차 publish만 수행했다. Windows 실제 UI, 다른 배율/모니터, 실제 첫 방문 동료, 스프레드시트 재저장, disk full의 native UI는 미확인이다. 결과 JSON 전체 메모리·대형 결과·Windows ACL 등은 I07/I12에 남아 있다. source/target에 앱 쓰기를 추가하지 않았다. 파일 실패의 원자 교체/기존 자료 보존 반례는 자동 시험에서 확인했다.

압축 후 정본·현재 소스·실제 증거를 다시 읽는 임시 AGENTS 절은 전체 과정 동안 유지한다. 상태와 다음 작업은 `docs/implementation-review.md`를 따른다.

## I03·I04 추가 증거

- [I03 실제 이름/컬럼 대응 JSON](i03-real-kr.json), 같은 이름의 CSV/HTML, i03-dialog/picker 계열 캡처: 한국어·영어 세 창 크기·취소/충돌/적용. i03-space-focus-960-en과 i03-unmatched-column-960-kr 계열은 키보드/동작 줄이기. [최종 picker](i03-picker-final-kr.jpg).
- [I04 실제 구조 결과](i04-real-kr.json)와 같은 이름의 CSV/HTML: 기본값·FK 동작·인덱스 순서·다단 partition의 네 차이, 선택 대응/12 검사 보존. [상세 화면](i04-schema-detail-kr.jpg), i04-detail-960/1440-{kr,en}은 긴 finding 배치.
- i04-reopened-{kr,en}은 DB 없는 재열기, [형식 1 호환](i04-legacy-format1-en.jpg). HTML은 별도 브라우저에서 표시를 확인했다.
- [새 fixture 전체 시험](i04-final.trx): 168 통과, 실패/건너뜀 0. i04-structure.trx의 166은 두 파일 불변식 시험 추가 이전 기록이다. macOS 실제 사용과 Windows x64 publish를 구분한다.
- 원본 제약 위반/권한·객체/다른 DBMS/대형 자원·Windows 실제 인수는 이 증거로 완료되지 않는다. 진행 중이므로 AGENTS의 임시 재개 조건을 유지한다.
