# UI-VERTICAL-01 · 수직 정렬 완료 증거

2026-10-02. 최신 macOS Avalonia 앱과 HTML 시안의 수직 정렬 교정이다. 이론·발견·페이지별 수정은 [작업 기록](../../docs/ui-vertical-alignment.md), 검증 목록·소스/빌드 해시는 [review.json](review.json)에 있다.

## 화면 확인

- [captures.json](captures.json): 10개 화면/변형 × 한국어·영어 × 960×720·1138×1088·1440×900 = 60조합. 각 조합의 상단과 필요한 하단을 포함해 주요 native 캡처 108개다. Retina 캡처는 2배 물리 픽셀이며 macOS 제목줄 56px을 포함한다.
- 파일 이름은 `00-home`, `01-connect`, `01-advanced`, `02-preflight`, `03-selection`, `03-trial`, `04-review`, `05-progress`, `06-results`, `07-save` + 폭 + 언어다. `-bottom`은 본문 하단, `.ax.txt`는 같은 시점의 접근성 트리다. `sheets/`는 이 실제 캡처를 나란히 묶은 비교표이며 새로운 렌더나 예시 이미지가 아니다.
- [최종 시작](00-final-home-kr.jpg), [작은 창 시작 하단](00-home-small-bottom-kr.jpg), [입력·radio](01-connect-1138-kr.jpg), [선택 행](03-selection-1138-kr.jpg), [결과 행](06-results-1138-kr.jpg), [저장 선택](07-save-1138-en.jpg)으로 대표 정렬을 확인한다.
- [web-captures.json](web-captures.json)은 HTML 시안 60조합이며 실제 DB/네이티브 증거와 구분한다. 마지막 Home·Save 수정은 다시 캡처했다. 첫 화면 연구 아이콘/본문 그룹 중심 차이 0px, 단계/형식 카드 제목·설명 시작선 차이 0px, 형식 선택 중심 오차는 브라우저 소수점 반올림 0.004px 이하이다. 다른 페이지는 앞선 시각·수치 관찰 자료다.

## 상호작용과 실제 검사

체크/삼중 상태/Space, DBMS 방향키, 검색 없음·해제, 독립 선택·펼침, 비밀번호 접근성 마스킹, 결과 상세·미검증 필터, 연구/권한 dialog·Escape를 실제 조작했다. [7/8 일부 선택](03-bulk-mixed-confirmed.jpg)의 접근성 Value 2가 증거다. `03-bulk-partial`은 검색 후 1개 표시의 중간 기록이므로 일부 선택 통과 증거로 쓰지 않는다.

실제 PostgreSQL TLS 예시 DB에 양쪽 각각 5백만 합성 행을 추가해 두 언어·세 크기에서 진행/resize를 보고 중단했다. 알려진 차이와 미검증·중단을 유지하며 전체 일치로 표시하지 않았다. [PostgreSQL 중단 보고서](postgres-interrupted.json), [합성 결과](synthetic-result.json)는 앱에서 저장한 안전한 envelope이며 SHA-256을 확인했다. 저장 dialog에서 붙은 중복 `.json` 확장자만 파일명에서 정리했고 내용은 바꾸지 않았다. 합성 run `ea20642e-0e9f-4d55-aa8d-f1340899a67a`를 DB 연결 없이 재열어 같은 ID·예시 표시·판정을 확인했다. 진행 중 취소 캡처는 전환 순간을 포함하며 최종 중단은 저장 보고서로 대조한다.

[손상 파일 오류](00-invalid-file-kr.jpg)는 기존 예시 손상 JSON을 실제로 연 결과다. 언어 변경 후 오류가 해제된 [영문 시작](00-error-recovery-en.jpg)과 [계속 연결 화면 이동](01-error-recovered-connection-en.jpg)을 확인했다. 파일 취소·저장·저장 취소 자료도 별도로 있다. OS 파일 dialog의 외형은 앱 레이아웃 범위와 구분한다.

## 빌드·제한·정리

[회귀 결과](regression.trx): 38 통과, 실패·건너뜀 0. 최신 native 소스로 macOS ARM64와 Windows x64 publish를 성공했다. Windows의 실제 UI 실행과 다른 배율·모니터·첫 방문 동료 평가는 이번에 수행하지 않았다. 동작 시험·수동 대응·CSV/HTML 등 기존 미구현 기능은 그대로 미구현으로 표시한다.

검사에 생성한 일회용 PostgreSQL 컨테이너·전용 이미지·임시 인증서/비밀번호 디렉터리를 정리했다. 실제 사용자 DB를 변경하지 않았다. [비밀값 점검](credential-scan.json)은 최상위 텍스트·JSON 증거에 임의 fixture 비밀번호가 없음을 확인한 기록이다. 앱 결과에는 원값·행 키·접속 설정을 넣지 않는다. 수정 전·중간 캡처는 최종 캡처 목록에서 제외했다.
