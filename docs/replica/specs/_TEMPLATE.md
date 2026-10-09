# <Component>

## 상태·범위

TODO / IN PROGRESS / LOCKED · 담당 단계 · 원본 캡처 버전 · 직접 부모/자식 · 변경 가능한 구현 심볼 범위.

## 원본 조사

- 실행 URL, 화면/언어/창 크기/배율/데이터 상태
- 원본 DOM과 CSS/JS 위치, 우선순위·override
- 실제 크기/좌표·padding/margin/gap·정렬·색·border/radius
- typography/font/line-height와 icon
- hover/pressed/selected/disabled/focus/처리중/오류/빈 상태: 해당 여부와 원본 근거
- animation/transition: 속성·시간·easing·시작/종료·동작 줄이기
- interaction: 클릭·키보드·초점·입력·취소·데이터별 전환
- 부모/자식 관계와 부모 안 배치

## Avalonia 대응

기존 파일/심볼, 대응 구조, 원본 대비 차이. 바꿀 범위만 명시한다.

## 비교·완료 체크

구조 / 크기 / 위치 / spacing / typography / 색 / border/radius / 상태 UI / interaction / 부모 배치. 각 항목에 원본과 native 증거, 환경, 확인 결과를 연결한다. 미확인은 미확인으로 쓴다.

## 알려진 차이·다음 행동

차이와 그 원본 근거, 수정 범위, 다음 하나의 행동.

## 재개 기록

압축·중단 뒤 규칙 → 상태 → 이 명세 → 관련 원본 → 구현 → 차이 확인 사실과 재개 단계.

## LOCK 변경 기록

LOCK 전에는 없음. 변경 전에 이유 / 영향 컴포넌트 / 원본 근거를 기록. 변경 후 재검증 증거와 새 LOCK 판정.
