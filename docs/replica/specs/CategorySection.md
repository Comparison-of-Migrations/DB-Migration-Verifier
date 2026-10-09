# CategorySection

LOCKED · 2026-10-08. ConnectionPanel LOCK 이후 단일 활성. 잠긴 CategoryHeader/Typography/Surface/Motion 조립. 직접 실행 원본 category DOM과 select()/CSS를 확인하고 body 목록·expanded/selected에 따른 상태를 기록한다. 다음: 원본 조사 → 명세 → 조립 → 원본 비교 및 actual toggle → LOCK.

## 실제 원본 확인·계약
새 독립 원본 탭에서 home→connect→예시 연결→capabilities→select를 실제 클릭하여 데이터 분류를 펼쳤다. 실제 DOM은 category(head + body-wrap/body/ul/li), outerborder1/radius10/margin-bottom12/overflowhidden/#2b2d31. selectedborder#737cb8/inset3px#8b94ff, unselectedshadow0/1/2#00000005. border-color/background/box-shadow CSS160ms 기본ease. head와checkbox는 잠긴 CategoryHeader 계약.
body-wrap:grid0fr→1fr,200ms cubic-bezier(.22,1,.36,1),visibilityhidden→visible; closedaria-hidden/inert,openunsetinert. body min-height0/overflowhidden/padding0 20 0 64,border0/animationnone,openinset top1#41434a. ul:padding-left18/margin12top16bottom,font13/line1.9/#b5bac1. li는 실제 문자열(2혹은3항목)을폭에따라줄바꿈. DPR2lineheight24.6953125; 실제980outer/dataopen143.390625,head64,body77.390625,ul49.390625. 외부 부모가 연속카드와스크롤을조립한다. expanded상태만변경해도selected상태가유지되고선택checkbox는펼침상태를바꾸지않는다. 기본OS list-marker raster표현만 MASTER_RULES12범위로 기록한다.

## 진행 교정
첫192endpoint 비교에서 펼침 값이 내부 flow의 cached measure를 갱신하지 않는 결함(288 차이)을 발견하여 frame/state 갱신마다 직접 flow.InvalidateMeasure를 추가했다. 기하 재비교는 일치했다. 실제 shadow property 관찰을 추가하자 CSS RRGGBBAA와 Avalonia AARRGGBB 파싱의 차이로 unselected shadow가 투명 파랑이 된 것을 발견해 Color.FromArgb(5,0,0,0)로 교정했다. 이것은 OS 차이가 아닌 구현 결함이다. 원본CSS72 timing을 조사했으며 reverse시 visibility의 CSS기본ease는37.0913ms, grid의SourceEase는110.8022ms로 따로 끝난다. 별도의 visibility track을 추가하여 크기가 줄어드는 중에도 원본의 숨김 시점을 따른다. source72와native production track 대조가 다음 단계다.

양OS192 endpoint와72 CSS transition 표본에서 크기·문구·selected/expanded/hidden/inert·실제 border/shadow·별도 visibility duration이 모두 일치했다. 실제 클릭에서는 화면의 목록이 표시되지만 접근성 캐시가 초기 collapsed children을 유지하는 결함을 발견했다. Open setter가 peer의 children/parent를 갱신하고 List/ListItem 구조를 노출하도록 조립부를 교정했다. 하위 LOCK 수정 없음. 이 수정본의 실제 펼침/닫힘 노출을 재검증하기 전에는 LOCK하지 않는다. 외부shadow는 sourceoverflowhidden과 같이 ownshadow를 자르지 않도록 내부flow만9px radiusclip한다.

2026-10-08 실제검증: Mac clickexpand/checkbox/close/Space가 원본의 개별 requested state·최종 높이와 일치했다. 열린 목록의2개 상세가 실제 접근성 트리에 나타나고 닫으면 제거된다. Mac 기본 bridge에는 닫힌 빈 listcontainer와 native marker Text role이 남지만 숨긴 상세/포커스 대상/화면 내용은 남지 않는다. 원본브라우저의 AXListMarker와 OS control 표현 차이를 강제로 고치지는 않는다. Windows UIA invoke/toggle 및 reduced의4상태 최종 진행률1/0/1을 기록했다. 추가 descendant 텍스트 수집에서 HRESULT E_FAIL이 발생하여 해당 측정은 실패로 남겼고 합격에 쓰지 않는다. manual child-peer enumeration을 framework base traversal로 바꾼 뒤 actual 목록을 재검증한다. 단일 활성은 계속 CategorySection이다. ConnectionPanel shadow 재개는 보류 상태이며 보호 코드는 아직 안 바꿨다.

2026-10-08 재개: AGENTS→MASTER_RULES→상태→본 명세→직접 원본 CSS/select 및 실행 카드 DOM→WebCategorySection을 실제로 다시 확인했다. 잠긴 범위 검사는 통과했다. 기존 결정은 유지하며 Windows base traversal 수정본의 실제 목록 노출 검증부터 재개한다.

Windows raw tree walker에서 header 다음 sibling(body) 접근이 실패하는 것을 특정했다. 원본 DOM은 동일 부모의 고정 body이며 펼침이 부모를 바꾸지 않는다. Avalonia 공식 ControlAutomationPeer의 parent/children 캐시 구현을 확인해, 불필요한 InvalidateParent가 부모 cache의 TrySetParent 없이 body parent를 지우는 경로를 발견했다. Refresh는 child invalidation만 유지하도록 교정하고 양OS actual traversal을 다시 검증한다. 공식 근거: https://raw.githubusercontent.com/AvaloniaUI/Avalonia/main/src/Avalonia.Controls/Automation/Peers/ControlAutomationPeer.cs (설치된12.1.3과 완전히 동일한 버전 소스라고 주장하지 않는다).

## 최종 판정 · LOCK
최종 child-only invalidation 수정본에서 양OS192 배치/상태 비교 및72 실제CSS transition 대조 차이0. `source/categorysection-{mac,windows}-comparison.json`, `source/categorysection-{mac,windows}-motion-comparison.json`. 실제입력9상태는 `source/categorysection-input-comparison.json`: 펼침/선택 독립성/닫힘/Space/reduced의 요청·최종배치 일치. 마지막 Mac checkbox 직후 Record는 border transition 중간값이어서 `final-selected-during-transition.json`로 보존하고 endpoint 합격에서 제외했다. 정상 transition72 표본 및 settled 실제·reduced 결과는 별도 통과했다.
Windows 최종 UIA는 열린 목록에2종 상세를 읽고 닫힘0개, reduced열림2종을 읽었다. 잠긴 Typography wrapper/inner peer 때문에 각 text 이름이 두번 노출되는 기존 OS bridge 구조는 유지한다. Mac 최종 binary도 실제 목록2개 노출/닫힘제거/Space재노출을 확인했다. 일반 renderer/marker/창장식 표현 차이는 MASTER_RULES12 허용범위이며 상태/기하 누락은 없다. 양OS native 검증 창을 닫았다.
부모 Selection의 연속 카드 배치·범위 상태 및 전체 페이지 조립은 아직 미구현 범위다. 하위 LOCK 수정 없음. 다음 단일 활성: ConnectionPanel의 발견된 alpha shadow 결함을 교정·재검증한다.
