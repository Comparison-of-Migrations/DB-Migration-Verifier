# Sidebar

## 상태·범위

LOCKED · 2026-10-07. 직접하위 SidebarItem/Typography/Surface는LOCKED다. 독립Divider는NOT PRESENT이며실제rail-note border는부모Surface로다룬다. 실제topAndRail의rail DOM/CSS,7단계상태/방문가능/실행중잠금/한영/높이scroll부터조사한다.

## 원본 위치

`examples/ui-prototype/index.html`: topAndRail(), .rail/.steps 및 하단 안내. 작업 직전 실행 웹의 해당 부분을 다시 조사한다. [원본 manifest](../source/manifest.json).

## 기존 Avalonia 위치

`src/Verifier.Desktop/`: MainWindow.Shell.cs Render. 기존 코드를 원본처럼 사용하지 않는다.

## 명세·검증

[_TEMPLATE](_TEMPLATE.md)의 DOM/CSS/geometry/spacing/type/상태/interaction/부모 관계/대응 구조/증거/차이를 착수 시 채운다. 값은 현재 미확인이다. LOCKED로 판정하지 않는다.

## 다음 행동·재개·LOCK 변경

원본조사→명세→조립→양OS/한영/높이·단계상태·실제nav검증→LOCK. 기존MainWindow sidebar로추정하지않는다.

## 실제원본조사 · 2026-10-07
source/sidebar-live.json: actualdesktop1280×720/DPR2 rail220×660, bgcolor#232428/no rightborder,padding30×14,gap32. title outer192/high18.6953125,paddinginline14/font11/1.7;nav gridgap8/7item412height. note topborder1#41434a,padding20×14,font12/1.6,strong13/700/1.6 +marginbottom8. note108.1875px 및tallwindow margin-top:auto. Desktop전체높이는부모shelltopbar60을빼서공급한다.

JS19 stage=trial?select:screen;current index와visited조건이별개다. done=visited&&i<index;enabled=visited&&screen!=running. screenrunning에서active도disabled다. nav클릭은sourceJS67에서같은guard후go(next);go는headingfocus/visited/re-render를소유하는Application부모다. sidebar는허용된NavigationRequested(key)만발행한다. 언어갱신에도이정책유지. 높이가짧으면overflowauto이며title/7단계/note min-content를임의로줄이지않는다.

source/sidebar-{mac,windows}-source.json 각62원본상태를새기록으로얻었다. 한영×400/660/960height×home/7단계/trial, reopen-sample/backwards/allvisited/runningcases포함. 현재livecaption샘플height660에서내용최소662.8828125→scrollHeight663이다. Mac은clientWidth220(overlay),Win은overflow시**clientWidth205(15px scrollbargutter)**이고960height에서는220이다. 이차이를임의간격으로보정하지않는다. 원본ScrollViewport에독립wheel/drag/overflow 상태가있으므로부모조립전에그하위부터조사·검증해야한다. Sidebar는WAITING CHILD로보존하고단일활성ScrollViewport로옮긴다. 후보상위Sidebar를먼저대충만들지않는다. generator의newline JS syntax실패는교정후새62원본기록으로대체했고성공으로쓰지않는다.

## 재개 · 2026-10-07
ScrollViewport가 사용자 OS 기본 표현 차이 조건으로 수직 계약을 LOCK했다. SidebarItem/Typography/Surface/ScrollViewport 직접 하위 모두 잠겼다. 실제 원본 ASIDE와 topAndRail(), 보호된62상태 source, 현재 상위 후보 부재를 확인하고 원본 padding/7단계/nav/note를 조립한다. 전체 앱 단계 전환은 부모 소유다. 단일 활성 Sidebar.

## 조립 계약 및 후보
WebSidebar는 WebSurface(#232428) 안에 잠긴 WebScrollViewport(padding30×14)를 조립한다. flow는 title(11/1.7,inline14) → gap32 → nav(7항목,gap8) → gap32/남는높이 → note(border-top1#41434a,padding20×14,strong13/700/1.6,gap8,body12/1.6)를 원본 flex column/margin-top:auto와 같은 최소높이로 배치한다. SetState는 원본 topAndRail 재렌더처럼 scroll child/항목을 새로 만들어 이전offset/포커스/held 입력을 남기지 않는다. 화면방문 정책은 source visited/screen에서 derived enabled/current/done, 요청은허용된key만발행. go()/headingfocus/실제페이지전환은Application소유.
SidebarProbe는 보호된 원본 sidebar-cases.json의 언어/높이/screen/visited만 전달하며 원본 geometry를 후보에 주입하지 않는다. 별도62source와 box/text/상태/접근이름을 비교한다. 실제 allvisited/running/reopened/locale 입력과 부모scroll은 후속 검증 전 LOCK하지 않는다.

## 최종 판정 · Sidebar LOCK · 2026-10-07
- `source/sidebar-{mac,windows}-comparison.json`: 각62 한영/400·660·960/9화면/방문역행·재열기·실행중 상태의 독립 DOM vs native 배치·문구·접근 이름·현재/완료/비활성 값 차이0. 기존 잠긴 하위는 수정하지 않았다. scroll gutter205/220·최소내용 높이·margin-top:auto 안내 배치도 포함한다.
- `source/sidebar-input-comparison.json`: actual Mac pointer/Space 및 Windows UIA·SendInput. 양OS allvisited connect/select/save 각1회, running disabled 요청0, 영어 reopened results1회. Mac 원본은 trusted 포인터/Space의 요청 배열과 일치했다. parent의 go()/headingfocus는 이 검증에 포함하지 않는다.
- source fixture는 원본 topAndRail/CSS와 고정 예시 visited 상태를 사용한다. 입력 기록은 원본의 visited/running guard에 해당하는 컴포넌트 요청까지만 기록한다. 실제 페이지 진입/모달/앱 State와 혼동하지 않는다. `sidebar-{18,45}.png`에서 부모 background/안내 border/배치와 하위 조립을 확인했다. 최종 앱 캡처 및 OS 표현 차이는 Application에서 별도로 검증한다.
- 새 창 직후 Windows UIA가 아직 창을 찾지 못한 요청은 거부되었고 최초 allvisits empty 파일은 통과에서 제외했다. 실제 새로 allvisits mode→세 클릭→fresh 기록으로 교체했다. disabled UIA provider의 일반 예외는 차단 성공으로 쓰지 않았고 exact title/path/geometry/foreground/hit/release/restore guard를 둔 공식 SendInput 실제 클릭으로 재검증했다.
- 중단으로 IAB 임시 탭이 제거돼 stale handle 실패1회. 기존 browser2의 남은 탭을 읽고 새 원본 fixture18을 생성/현재 상태로 복구했으며 사용자 원본탭/접속 입력은 변경하지 않았다. native Close 후 CUA procNotFound는 실제 프로세스 종료를 확인해 재실행하지 않았다. 두 후보 창은 닫혔다.
- production `WebSidebar.cs`를 LOCK했다. 다음 단일 활성 TopBar. 전체 복제/임시 복구 제약은 계속 유지한다.
