# IconButton

LOCKED · 2026-10-07 · 단일 활성. ActionButton/SvgIcon/Typography/Motion LOCK 뒤 조립한다. ActionButton 보호 코드는 수정하지 않는다.

## 원본 조사

actual KO Selection의 search-clear 및 mapping dialog close를 DOM/computed/geometry와 직접 확인했다. `.btn.quiet.icon-button` button[type=button] 아래에는 aria-hidden SVG close 하나만 있고 visible label/span은 없다. accessible name은 KO `테이블 검색 지우기`/`닫기`, EN `Clear table search`/`Close`. svg path는 m6 6 12 12M6 18 18 6이며 SvgIcon LOCK 계약을 따른다.

- border-box44×44, border1 transparent, padding0, radius8, icon18×18 local13/13. 원본gap9는 자식이 하나라 공간에 기여하지 않는다.
- normal/disabled quiet는 background/border transparent, foreground#b5bac1. enabled hover는 #3c3f46/#f2f3f5 및 일반button shadow0 2 5 #00000018. pressed scale.98, focus-visible outline3 #a7b1ff offset3.
- transition bg/color/shadow120ms cubic-bezier(.22,1,.36,1), transform80ms 같은ease. click의press()는160ms ease-out scale1→.98→1/opacity1→.86→1. reduced 생략은 Motion/ActionButton LOCK을 재사용한다.
- search 부모는position:absolute top0 right0, table-toolbar font12. dialog 부모는flex item44×44/font13. 부모 위치·언어전환·닫기·검색정책은 composite/section 책임이다. 여기서 부모를 새로 구현하지 않는다.
- 원본actual clear click: orders→빈값, 표시1→3행, 선택3/3 보존, clear disabled=true 및 table-search로포커스복귀. dialog close actual은 modal 제거 및 mapping opener에포커스복귀. 값변환/DB쓰기 없음.

근거: source/original.css/JS의 icon-button/search-field/dialog-head/press/syncTableSelection/closeModal 및 source/iconbutton-live-search-ko-disabled.json, screenshots/iconbutton-live-search.png. 실제 원본은 index.html이고 보관 CSS/JS를 후보 수정에 맞춰 바꾸지 않는다.

## Avalonia 대응

WebIconButton.Create는 LOCKED WebActionButton을44×44/Padding0/quiet/close/empty Content로 조립한다. 보이는 텍스트를 accessible name으로 넣지 않고 AutomationProperties.Name을 명시한다. LOCKED absolute icon slot RightInset12로 local13/13을 만든다. 일반leading+gap9 슬롯은 빈label에도gap을 계산하므로 아이콘이옆으로쏠린다. 해당 코드를 바꾸지 않고 이미 검증된 absolute slot을 사용한다. SetAccessibleName은 부모의KO/EN명칭갱신용이다. 클릭·disabled·keyboard·focus·transition/playback은 동일잠긴button.

구현: src/Verifier.Desktop/WebIconButton.cs. 아직 검증/LOCK 아님.

## 다음 행동

고립 원본에서 두직접부모/KOEN/disabled/reduced 표본을측정하고 native 실제lifetime/상태paint/actual입력과대조한다. 다른컴포넌트와 LOCKED코드는수정하지 않는다. 전체복제재개제약 유지.

### Windows 원본 회수 방식 교정

원본 UIA Invoke는 실제 성공했지만 Edge의 readonly textarea ValuePattern은 값9824자에서 잘려 JSON이 완결되지 않았다. 이 결과를 통과로 사용하지 않는다. 이전부터 쓰던 예시전용 fixture POST 방식으로38이아닌44상태 측정값을 회수한다. UIA조작대상/OS설정은 확대하지 않으며 원본웹코드도 변경하지 않는다.

### 현재 검증

source 고립12표본(두부모×KOEN×enabled/disabled/reduced)에서44상태. macOS/Windows actualDesktop scale2의44상태/12이름갱신 대조는 각각통과했다. 버튼44×44,아이콘local13/13및18×18,emptylabel,색/그림자/focus/disabled/pressedscale 일치. native는same LOCKED ActionButton이며interaction/timing을새로구현하지않는다. 원본transition/playback및SVGprimitive계약을재사용한다.

Mac actual키입력: Tab첫enabled포커스→Enter클릭1→Spacepressedtrue/클릭2/pressedfalse→Tab이disabled를건너reduced포커스→Space클릭3. source/iconbutton-mac-actual/iconbutton-input.json. CUA AXclick은실제창이보이는데offscreen으로거부했고화면좌표한번도클릭변화가없어마우스통과로기록하지않는다. Number전용CG권한을확대하거나Codex차단을우회하지않는다. 새interaction코드는없고잠긴ActionButton의actualpointer계약을조립해사용한다. WindowsactualUIA/key확인이남았으며아직LOCK하지않는다.

## 최종 판정 · IconButton LOCK

양OS 각각44 endpoint/12이름갱신이대조통과했고 actual버튼활성화를확인했다. Mac Enter/Space/disabled Tab/reduced Space, Windows uniqueowned UIAInvoke/disabledInvoke거부 및 Space/Enter/reducedSpace를 fresh입력기록으로확인했다. Windows 최종클릭5,35이벤트와 disabled전후1/1불변이다. 첫reducedSpace의CLIpress/release가클릭으로이어지지않아실패로보존하며, exactAutomationId 포커스검사와150ms press/release 간격 후같은창에서fresh5클릭을확인했다. 운영체제나접근권한은변경하지않았다.

증거: source/iconbutton-{mac,windows}-source.json, iconbutton-{mac,windows}-native/iconbutton-native.json/png, iconbutton-{mac,windows}-comparison.json, iconbutton-mac-actual/iconbutton-input.json, iconbutton-windows-actual.json, iconbutton-windows-disabled.json. CUA의Macpointer좌표거부는컴포넌트차이로꾸미거나통과로쓰지않는다. 별도pointer구현은없으며 sameLOCKED ActionButton의 actualpointer/hover/pressed/transition 계약을재사용한다. 크기/상태조립은이검증의범위이며 SearchInput 및 dialog의부모위치/정책은해당상위에서검증한다. 원본없는visiblelabel/hover/애니메이션을추가하지않았다.

구현보호: source/iconbutton-lock.json(WebIconButton.cs 전체). 다음단일활성은SearchInput, 직접하위 TextInput/IconButton/Surface/SvgIcon/Motion는LOCKED다. 전체복제임시재개제약유지.
