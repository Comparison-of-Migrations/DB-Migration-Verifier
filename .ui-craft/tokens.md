# 화면 토큰 — Discord 다크 테마 참고 시안

2026-10-02 복제 작업에서는 [실행 원본과 컴포넌트 명세](../docs/replica/MASTER_RULES.md)의 실제 값이 이 기존 토큰 표보다 우선한다. 불일치는 해당 명세에 기록하며 근사값으로 대체하지 않는다.

2026-10-01. PlandoSee 제약 해제 뒤 사용자가 제안한 Discord 테마를 반영한다. [Discord 공식 브랜드 안내](https://discord.com/branding)의 Blurple #5865F2와 [공식 테마 설명](https://support.discord.com/hc/en-us/articles/207260127-How-to-Change-Discord-Color-Themes-and-Customize-Appearance-Settings)을 참고했다. 회색 면·입력 경계·상태색은 DB 검증 화면의 가독성에 맞춘 시안 값이다. 정책은 [RULE](../RULE.md), 구조·선택 요소는 [제품 설계](../docs/product-design.md)가 정본이다. 실제 Avalonia 지원과 사용자 최종 승인 증거는 별도로 확보한다.

| 역할 | 값 | 용도 |
|---|---|---|
| 핵심 행동 | #5865f2 / hover #4752c4 / pressed #3c45a5 | 주요 버튼·선택 채움. Blurple |
| 선택 텍스트·초점 | #b4bdff / #a7b1ff | 어두운 바탕의 가독성과 키보드 초점. 채움색과 구분 |
| 본문·제목·보조 | #dbdee1 / #f2f3f5 / #b5bac1 | 제목·입력·보조 문구의 위계 |
| 작업·본문·탐색·입력 | #313338 / #2b2d31 / #232428 / #1e1f22 | 탐색·작업·입력 영역을 밝기로 구분 |
| 선택 면·경계 | #3b3d52 / #8b94ff | 체크·라디오와 함께 선택 표현 |
| 그룹·입력 경계 | #41434a / #7c838c | 그룹 경계와 입력/선택 외곽을 구분 |
| 일치·불일치·미검증 | #6bdda3 / #ff959d / #f0c47c | 문구·아이콘·패턴과 함께 표시. 완료와 일치 분리 |
| 글꼴 | system-ui, -apple-system, Segoe UI, sans-serif | 외부 요청 없이 OS 글꼴. 숫자·객체 이름은 시스템 mono |
| 타입 | 첫 제목 36/44, 제목 28/36, 섹션 18/26, 본문 14/22, 안내 12/18 | 데스크톱 입력·판정 가독성 우선 |
| 간격 | 4·8·12·16·24·32·40 DIP | 그룹 간 24/32, 본문 여백 32/40 |
| 모서리 | 입력/버튼 8, 선택 카드/패널 10, dialog 14 DIP | 역할별로 구분 |
| 버튼 | 핵심 48h·최소 164w, 첫 시작 52h, 보조/중단 44h | 핵심 행동은 하단 오른쪽, 정보 행동은 해당 항목 옆 |
| 선택 요소 | checkbox/radio 20, label 최소 44h, 분류 헤더 기본 64h, DBMS 카드 최소 88h, 대상 목록 최소 60h | 기본 조작 의미 유지, 선택·미선택·일부·비활성·오류 상태 |
| 화면 골격 | 상단 60h, 탐색 220w, 본문 최대 1120w, 고정 하단 최소 84h | 최소 960×720 DIP 설계안. 작은 데스크톱 창에서도 왼쪽 단계 목록 유지 |
| 피드백 | press 80ms, 색/초점 120ms, check/선택 160ms, 상태/화면 200ms, dialog 280ms | exit 160ms, 결과 숫자 220ms·막대 320ms. 동작을 지연시키지 않음 |
| easing | cubic-bezier(0.22,1,0.36,1) | 짧게 감속하며 상태 변화 전달 |

색 변경만으로 선택을 표현하지 않는다. 체크 표시·라디오·일부 선택·선택 문구가 함께 바뀐다. label 전체가 클릭 영역이며 선택 시 페이지 전체를 다시 등장시키지 않는다. 검색은 숨은 선택을 바꾸지 않고 일괄 선택은 검색 결과에만 적용한다. 모든 작동 버튼과 선택 요소에 키보드 초점·누름·비활성 이유와 실제 기능에 연결된 피드백을 둔다. 동작 줄이기에서는 이동·회전 대신 상태 문구·정적 표시를 유지한다.

Avalonia에서는 RadioButton 카드 ControlTheme·CheckBox·가상 ListBox·TextBox와 공통 Grid/Border 구성 및 상태 스타일로 구현했다. 실제 적용/크기/동작 기록은 [UI 교정 증거](../evidence/ui-alignment/README.md)를 따른다. [공식 Control themes](https://docs.avaloniaui.net/docs/styling/control-themes), [상태 스타일](https://docs.avaloniaui.net/docs/styling/pseudoclasses), [Transitions](https://docs.avaloniaui.net/docs/graphics-animation/control-transitions). 현재 HTML의 네이티브 입력 의미와 스타일을 네이티브 앱 구현 증거로 취급하지 않는다.

## 수직 정렬 적용

조작 행·짧은 복합 셀은 전체 내용 그룹 Center, 긴 label/value는 동일 13px/22h 첫 줄 Top, 나란한 단계/형식 카드는 공통 제목·설명 시작선을 사용한다. 입력은 외부 높이와 내부 VerticalContentAlignment를 구분하며 아이콘·선택 표시 컨테이너에도 Center를 적용한다. 네이티브 Fluent 체크박스의 Top 표시 컨테이너만 AlignedCheckBox에서 교정하고 키보드·삼중 상태·접근성은 상속한다. 근거·페이지별 관찰은 [수직 정렬 기록](../docs/ui-vertical-alignment.md), 최신 실행 증거는 [UI-VERTICAL-01](../evidence/ui-vertical-alignment/README.md)을 따른다. 44/48/52h 행동 위계와 기존 간격·애니메이션은 유지한다.
