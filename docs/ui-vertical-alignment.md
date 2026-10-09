# UI-VERTICAL-01 · 전 페이지 수직 정렬

2026-10-02 사용자 요청. 상태: 완료. 실제 Avalonia 앱과 HTML 기준을 모두 점검하며 레이아웃의 수직 기준을 확인한 후 수정한다. 기존 UI-ALIGN-01 증거는 이전 이력이다.

## 이론 조사·적용 기준

공식 원문을 확인한 뒤 다음 기준으로 교정한다. 아래 수치는 이 프로젝트의 적용 결정이며 외부 이론의 보편 법칙이나 픽셀 강제값으로 인용하지 않는다.

1. **공통 시작선·수직 리듬:** [Carbon 2x Grid](https://www.carbondesignsystem.com/building-blocks/foundations/2x-grid/overview)의 keyline·콘텐츠에 맞춘 자동 높이·8px 단위 원칙을 따른다. 같은 역할의 항목은 같은 행을 공유하고, 간격은 기존 4·8 DIP 토큰을 사용한다. 설명이 두 줄로 늘어도 다음 항목의 시작선이 제각각 밀리지 않게 한다.
2. **한 행의 조작 요소:** 아이콘·체크·상태·버튼과 그에 딸린 한 줄/짧은 그룹은 행 중앙을 공유한다. 버튼끼리는 같은 중심선, 동일 역할의 버튼 그룹은 같은 높이를 사용한다. [Carbon 버튼 규격](https://www.carbondesignsystem.com/building-blocks/core/components/button/specifications)의 일관된 그룹 크기 원칙을 참고한다. 중요도별 강조와 44/48/52h는 기존 정책을 유지한다.
3. **여러 줄의 읽기 요소:** 설명·dialog 본문과 긴 label/value 요약은 상단부터 읽으며, 요약 label과 value의 첫 줄을 같은 크기/행 높이로 맞춘다. 카드끼리 높이가 달라도 내용 그룹을 임의로 수직 중앙에 띄우지 않는다. 표의 짧은 복합 셀은 내용 그룹 전체를 중앙에 놓는다.
4. **외부 박스와 내부 콘텐츠 구분:** [Avalonia 배치 문서](https://docs.avaloniaui.net/docs/layout/positioning-controls)에서 VerticalAlignment·고정 높이·padding의 관계를 확인했다. 입력/버튼의 내부 정렬은 VerticalContentAlignment로 별도 지정한다. 고정 크기 아이콘은 부모 행 안에서 Center를 명시한다. 텍스트 줄 상자는 공통 line height를 쓰며 글꼴의 잉크 경계를 임의의 음수 margin으로 맞추지 않는다.
5. **플랫폼 기본값의 한계:** [공식 Fluent CheckBox 템플릿](https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Themes.Fluent/Controls/CheckBox.xaml)에는 표시 영역이 Top, 내용은 별도 정렬인 구조가 있다. 기존 44h/긴 카드에서는 표시와 내용의 중심이 달라질 수 있어 실제 사용 중인 고정 패키지에서 교정을 확인한다. [TextBox 템플릿](https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Themes.Fluent/Controls/TextBox.xaml)은 입력과 placeholder 모두 내부 정렬 속성에 연결된다. 패키지의 repository commit `8eeda4f6f546165b3f72e63c9f42247abb306905`의 템플릿도 직접 확인했다. 템플릿의 고정 정렬은 일반 스타일로 교정되지 않아 AlignedCheckBox가 적용 직후 표시 컨테이너만 Center로 지정한다. Fluent theme/접근성/Space/삼중 상태는 상속하며 최신 화면·키보드·마스킹으로 확인한다.

검증: 최소/중간/넓은 창 KR/EN에서 같은 역할의 중심·첫 줄·줄바꿈 후 시작선, 고정 footer·본문 스크롤을 확인한다. 물리 Retina 픽셀과 DIP를 구분한다. 공통 정렬은 원인 위치에서 고치고 페이지별 보정 margin을 쌓지 않는다.

## 발견 목록 · 수정 전

| 발견 | 교정 방향 | 이유 |
|---|---|---|
| 입력/placeholder가 44h 박스 상단으로 치우침 | 단일 행 입력 내부 Center | 외부 박스 중앙과 실제 입력 내용 중앙이 다름 |
| checkbox 표시와 label/긴 형식 설명의 높이가 다름 | 표시 템플릿과 내용의 공통 중심 | Fluent 표시 영역은 상단 고정, 카드의 내용은 중앙 |
| icon·caret와 짧은/여러 줄 행의 중심이 다름 | 공통 아이콘 Center | 고정 높이 아이콘의 기본 Stretch를 중앙 정렬로 오인 |
| 사전 확인의 짧은 복합 셀이 긴 셀과 다른 높이에서 시작 | 복합 셀 전체 Center | 행의 의미 단위가 동일하지만 셀별 StackPanel이 상단 시작 |
| 3개 사용 단계의 영문 제목 줄바꿈 뒤 설명 시작선이 달라짐 | 번호·제목·설명을 공통 Grid 행으로 구성 | 서로 독립된 StackPanel의 높이가 다름 |
| 긴 요약 label이 값의 중간으로 내려감 | label/value Top·동일 글꼴/행 높이 | 첫 줄의 비교/읽기 순서가 깨짐 |
| footer의 44h/48h 행동과 긴 안내가 엇갈림 | 조작 영역 Center·안내 Center | 동일 행동 줄이 서로 다른 기본 배치를 사용 |


## 페이지별 진행

| 범위 | 상태 | 발견·수정 | 실제 확인·증거 |
|---|---|---|---|
| 공통 header·rail·footer·notice·버튼·입력 | 확인 | 내부 Center, 표시 컨테이너 Center, 아이콘 Center | 네이티브 한영·3크기 상하 스크롤 캡처 |
| 시작 | 확인 | 세 단계 공통 Grid 행 | 00-home 한영·3크기 |
| 연결·고급 설정 | 확인 | 입력 내부·radio 표시·행 중심 | 01-connect/01-advanced 한영·3크기·하단 |
| 사전 확인·권한 안내 | 확인 | 복합 셀 그룹 중앙·dialog 상단 읽기 | 02-preflight 한영·3크기 및 dialog |
| 선택·검색·대상 표 | 확인 | 체크 표시·분류 내용·표 폰트/중앙 | 03-selection 한영·3크기·하단, Space·독립 선택·검색 없음·7/8 삼중 상태 확인 |
| 별도 시험 안내 | 확인 | 패널 제목 상단·본문 상단, 행동 줄 중앙 | 03-trial 한영·3크기·하단 |
| 실행 전 확인 | 확인 | label/value 13px·22h·Top, 체크 중앙 | 04-review 한영·3크기·하단 |
| 진행·중단 | 확인 | 상태 행 그룹 Center, 취소 행동 Center | 실제 PostgreSQL 5백만 행 예시·한영·3크기, 2회 중단 |
| 결과·상세·필터 | 확인 | 이름/상태 그룹과 숫자/막대 그룹 Center, caret Center | 06-results 한영·3크기·하단, 상세/필터 |
| 저장·형식·오류/취소 | 확인 | 요약 Top·공통 64h 내용 그룹과 표시 Center | 07-save 한영·3크기, Space·저장/취소 확인·DB 연결 없이 같은 run 재열기 확인·손상 파일 오류 복구 |
| dialog·공통 상태·키보드 | 확인 | 읽기 본문 Top·행동 Center·Fluent 의미 유지 | 연구/권한 dialog·Escape, Space, 방향키, 마스킹, 손상 파일·복구 |
| 최종 정리·임시 제약 제거 | 완료 | 전 범위 증거·문서·빌드 해시 대조 | 임시 AGENTS 절·작업 중 상기 지시 제거, 상시 UI 기준 유지 |

## 최종 확인

macOS native 60조합/주요 캡처 108개와 HTML 60조합을 대조했다. 실제 PostgreSQL 진행·중단·resize, 저장·취소·재열기, 일부 선택·Space·방향키·마스킹·손상 파일 복구를 확인했다. 마지막 HTML 카드 설명 줄바꿈과 연구 영역 flex-wrap도 공통 Grid/늘어나는 내용 영역으로 교정했다. 단계/카드의 시작선 차이 0px, 연구 아이콘 중심 차이 0px을 확인했다. [증거 안내](../evidence/ui-vertical-alignment/README.md), [점검 목록](../evidence/ui-vertical-alignment/review.json)을 따른다.

회귀 38 통과, macOS ARM64·Windows x64 publish 성공. Windows 실제 UI·다른 배율·모니터·첫 방문 평가는 미수행이다. 동작 시험·수동 대응·CSV/HTML 등 미구현 기능은 기존의 미구현 표시를 유지한다. DB 비교 엔진·엄격 비교·값 비노출 정책은 변경하지 않았다. 일회용 DB fixture와 비밀값 임시 디렉터리를 정리했다. 문서 링크·최신 소스/빌드 해시·비밀값 점검·fixture 정리를 대조한 뒤 임시 상기 제약을 제거했다. 완료 기록과 상시 UI 기준은 유지한다.
