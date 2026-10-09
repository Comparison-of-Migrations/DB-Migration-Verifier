# TopBar

LOCKED · 2026-10-07. Sidebar LOCK 후 단일 활성. 직접 하위 SvgIcon/Typography/Surface/ActionButton은 잠겼다.

원본 topAndRail() header.topbar / brand / preview / button.language. 해당 실행 원본의 DOM/computed/배치/상태를 확인하고 language를 기존 ActionButton의 명시적 부모 override로 표현 가능한지 확인한다. .btn라고 추정하지 않는다. 필요하면 새 leaf를 먼저 작업한다. 언어 상태 변경은 부모 요청을 전달하며 화면 전체 전환은 Application에서 검증한다.

## 실제 조사·조립 계약
실행 중 원본 header:height60/padding0×24/bg#1e1f22/border-bottom1#393b41/flex center/gap20. brand:icon30(padding5/icon20,#5865f2,radius8)→gap12→span14/650/1.6/letter-spacing-.35. preview:inline12/1.6,#b5bac1,border-left1#41434a/padding-left16/margin-left:auto. language:min44/padding0×12/border1 transparent/radius8,font12/400/1.6/color#dbdee1,hover#383a40; generic button hover shadow/pressed transform/pressWAAPI/focus는 하위 기존 계약.
원본CSS shell60px row를 fixture의 고정 부모 높이로 공급한다. 한영×960/1000/1138/1280=8범위. button.language는 .btn class가 아니지만 잠긴 ActionButton에 정확한 base/font12/weight400/padding12×0/transparent border+resting background/hover#383a40 부모 override를 모두 공급하면 현재 실제 계약을 표현한다. 새 CSS 추정/LOCK 하위 변경 없이 조립한다. 원본 aria-label은 KO영어로변경/EN Switch to Korean; 부모가 locale를 변경한다. 후보 WebTopBar의 LanguageRequested는 다음언어만 요청하고 자동언어변경은 부모 소유. native/source8조건의 box/text/name을 독립 비교하고 실제 click/Space 후 교체는 후속이다.

## 재개 확인 · 2026-10-07
압축 뒤 AGENTS/MASTER_RULES/STATUS/본 명세를 실제 읽고 실행 원본 header의 DOM/computed 값과 WebTopBar를 대조했다. 기존26 LOCK 검사를 통과했다. 대기 중 Windows 실제 요청은 en → ko 각각 한 번으로 완료됐다. 비교 도구의 결과 메타데이터가 복사 과정에서62로 남아 있어 실제 비교 대상8에 맞게 교정했다. 데이터 비교 자체는8건을 검사했고 재실행한다.

## 최종 검증·LOCK
한영×960/1000/1138/1280의8조건을 각 OS의 실행 원본 DOM과 독립 비교했다. brand/icon/text/preview/language의 위치·크기·문구·접근성 이름에 차이0. 증거: source/topbar-{mac,windows}-{source,comparison}.json 및 native/topbar-native.json. 클릭 en → Tab/Space ko의 Mac trusted 원본과 native 요청/갱신 box가 일치했다. Windows UIA 실제 invoke en → ko도 각1회이며 교체된 버튼이 새 문구와 이름을 갖는다. source/topbar-input-comparison.json과 양OS input 폴더에 원본/후보를 보존했다. native/topbar.png의 최종 English 구성을 시각 확인했다. 원본에 상단 disabled 조건은 없어 별도 disabled 상태를 발명하지 않는다.

최신 사용자 조건에 따라 OS 기본 글꼴의 실제 폭/AA 차이는 각 OS 원본과 비교하며 OS 사이의 픽셀 동일성은 요구하지 않는다. 논리 색·기하·동작은 면제하지 않았다. 각 버튼의 hover/pressed/focus/motion은 변경 없는 LOCKED ActionButton 명세를 따른다. 전체 페이지 언어 전환·포커스 정책은 Application 조립의 검증 대상이다. 이 단위가 전체 앱 통합 완료를 의미하지 않는다.

LOCKED: WebTopBar.cs; source/topbar-lock.json. 하위 수정 없음. 다음 직접 하위가 잠긴 ConnectionPanel을 조사한다.
