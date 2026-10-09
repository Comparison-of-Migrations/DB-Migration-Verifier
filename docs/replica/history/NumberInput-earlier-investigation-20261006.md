# NumberInput 이전 조사 기록

현재 상태는 ../specs/NumberInput.md가 정본이다. 아래의 당시 다음 행동/미확인은 현재 상태가 아니다.

## 다음 행동

2026-10-06 재개: AGENTS→MASTER→STATUS→이명세→실행원본#source-port/원본CSS・JS→WebTextInput/WebNumberRules를실제로확인하고9LOCK해시통과. TextInput완료검증창은닫았다. NumberInput단일활성을유지한다. 원본의편집값과DOMvalue를구분하는후보규칙은실제기록71state대조에서71/71일치했다(source/numberinput-mac-rules-comparison.json). 이는입력규칙의부분검증이며UI/paint/전체NumberLOCK완료가아니다.

직접조립구조: WebNumberInput의Grid에잠긴WebTextInput과contentright의11/15×22 UAspinpaint를겹친다. 텍스트뷰포트는paddingright23/27로예약한다. 단일editor가IME/undo/selection/clipboard를계속소유하며Number부모의InsertionFilter로삽입전필터를적용한다. 키보드Up/Down과focusedwheel은step함수로접속하고spinnerpointer는focus/select・즉시step・250ms후50msrepeat・capture해제를구현한다. timer근거는[Chromium cc/input/scrollbar.h](https://chromium.googlesource.com/chromium/src/+/HEAD/cc/input/scrollbar.h),spin_button_element.cc다. WinFluentarrowbasis18/normal9・pressed8은primarynative_theme_fluent.h/cc기준이며glyphsize=ceil(9×15/18)=8. Macactual上部click3306→3307/focusedwheelUp→3308을확인했다. 실제heldpointer/Winhoverpressed는아직검증하지않았다.

sourceport선택자와computedstyle/actualgeometry를다시확인하고ArrowUp/Down·입력·invalid·min/max의원본행동을기록한다. 원본전체/기존부모페이지를재분석하지않는다.

실행원본port112×44/type=number/min1/max65535/defaultstep1,14/22.4・padding9/12・surface는TextInput과동일. 실제ArrowUp3306→3307,spinner上部pointerclick3307→3308확인. 원본JSinputhandler는문자열state를갱신하며연결상태를미확인으로바꾼다(부모소유). validateConnections는digitsregex/1–65535정수를연결전확인하며number원자자체가분수・지수입력을자동차단하는것이아니다. 첫격리fixture는.field input specificity/fieldmargin을누락해460폭/y36이어서원본크기검증근거로쓰지않고inlinewidth112/top0・rowmargin0으로수정했다. Pythonurllib의시스템인증서없음으로다운로드가실패해OStrust기반curl로primarysource를받았으며검증우회는하지않았다. html.css옛경로404는primarysearch로실제resources경로를확인했다.

