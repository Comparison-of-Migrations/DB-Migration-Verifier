DB-Migration-Verifier — 개발 실행판 / Development build

실행 / Run
- macOS Apple Silicon: 같은 폴더의 DB-Migration-Verifier.app을 엽니다.
- Windows x64: 폴더 전체를 유지하고 Verifier.Desktop.exe를 엽니다.
- .NET SDK 설치나 앱 계정 생성은 필요하지 않습니다.
- Open DB-Migration-Verifier.app on macOS, or Verifier.Desktop.exe on Windows.
  Keep all accompanying files. No .NET SDK or app account is required.

이 앱은 마이그레이션을 마친 개발자가 두 DB의 데이터·구조·권한·객체를
비교하고 차이와 확인하지 못한 영역을 찾도록 돕습니다.
It compares two databases after migration and reports differences and unverified areas.

확인할 세 행동 / Three actions to verify
1. 새 검사: 같은 DBMS의 서로 다른 원본·대상에 접속하고 양쪽 쓰기 정지를
   확인합니다. 틀린 접속 정보는 연결 완료가 아니라 수정 가능한 오류여야 합니다.
   Start a new check, connect two distinct databases of the same DBMS, and confirm
   that writes are stopped. Incorrect credentials must produce an error.
2. 사전 확인 → 검사 선택 → 실행: 조회 가능한 범위와 대응을 확인합니다.
   선택 범위가 실행 전 요약과 검사 결과에서 같아야 합니다.
   Review capabilities, choose scope and matching, and run. The selected scope
   must remain consistent in the review and results.
3. 결과 상세 → JSON 저장 → 홈의 '검사 결과 파일로 보기': 같은 검사 ID와
   판정이 복원돼야 합니다. 불일치와 미검증은 함께 표시되어야 합니다.
   Inspect details, save JSON, then reopen it from the home screen. The run ID,
   verdicts, differences and unverified areas must be preserved.

DB 없이 화면을 둘러볼 때는 '예시 결과 보기'를 선택합니다.
예시 동작은 실제 DB 검증 증거가 아닙니다.
Use the example flow to explore without a database. It is not an actual DB verification.

기본 비교는 읽기 전용입니다. 선택 쓰기 시험은 분리 확인된 시험 DB만 사용합니다.
실제 DB·접속 설정·비밀번호·API 키는 이 묶음에 포함하지 않습니다.
서명·공증된 정식 배포물이 아니며 전체 검사 범위의 구현·인수가 진행 중입니다.
Comparison is read-only. Optional write tests require a verified separate test DB.
No user database or connection secrets are bundled. This development build is not
a signed/notarized release; implementation and full acceptance remain in progress.

라이선스 / License: Apache-2.0. See LICENSE, NOTICE, THIRD_PARTY_NOTICES.txt.
2026-10-14 dependency refresh: PKCS 9.0.21 and bundled .NET runtime 10.0.13.
