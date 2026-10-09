# ActionButton

## 현재 상태

LOCKED · 후보 구현 및 원본 대조 완료. 직접 하위 SvgIcon/Typography/Surface/Motion은 LOCKED다. 부모·다른Atomic는 변경하지 않는다.

## 원본과 현재 구현

실행 http://127.0.0.1:8765/ 의button.btn, original.css의button/.btn/variant/parentoverride, original.js press 및data-action handler. 기존MainWindow.Action/Pulse,MainWindow.Shell.WithIcon,App.axaml은근사구현이며원본추측에쓰지않는다. Source manifest 및원본보호유지.

## 진행과 다음 행동

2026-10-06 Typography/Motion primitive 검증·LOCK뒤다음단일컴포넌트로진입했다. 현재실행Connection EN의버튼DOM/computed style·실제size·자식glyph/간격·부모관계를읽는다. .btn직접variants와상태CSS/press handler만조사해명세에기록한후구현한다. 후속 후보/증거는 아래 진행 절이 정본이며 LOCK은 아직 없다.

## LOCK 변경 기록

없음. 아직LOCKED가아니다.

## 원본 계약 · 2026-10-06 조사

- DOM: `button[type=button].btn[data-action]` → 선택적 decorative SVG → span(plain label). SVG18×18, flex:none, gap9. 일반 라벨만 있는 버튼도 존재한다. icon은 currentColor를 상속한다.
- border-box: border1, radius8, padding10px16px, inline-flex center/center, min-height44. primary는 min-height48/min-width164. font system-ui13/600, line-height20.8, letter-spacing normal, white-space normal. 부모 flex 항목의 stretch/alignment/explicit width는 부모 계약이다.
- base #383a40/#dbdee1/border#7c838c; hover #45474f.
- quiet transparent/transparent/#b5bac1; hover #3c3f46/#f2f3f5.
- primary #5865f2/white/border#5865f2; hover#4752c4; active#3c45a5. shadow0 2px3px #00000024. 실제 클릭 뒤 hover shadow는 일반 button:hover:not(:disabled)의 더 강한 specificity에 따라0 2px5px #00000018로 바뀐다. source/action-button-source-mac-interaction.json에서 직접 확인했다.
- danger transparent/#ff959d/border#94616a; hover#4a3038. 일반 hover shadow0 2px5px #00000018.
- disabled #3b3d44/#b5bac1/border#5d636d, shadow없음. quiet의 더 강한 `.btn.quiet` 배경/테두리와 충돌하는 실제 computed cascade는 fixture에서 확인해야 한다. 23case 실제 computed에서 quiet는 transparent/transparent/#b5bac1, danger는 transparent/#94616a/#ff959d를 유지함을 확인했다.
- focus-visible outline3px#A7B1FF, offset3. pointer focus와 keyboard focus를 구분한다. source computed resting outline none은 focus-visible의 삭제 근거가 아니다.
- CSS transition: background/color/shadow120ms, transform80ms, cubic-bezier(.22,1,.36,1). border-color는 transition 목록에 없다. active:notdisabled scale.98, origincenter.
- delegated click은 disabled 제외 → press() → data-action 즉시 수행. press는160ms ease-out, scale1/.98/1과opacity1/.86/1, fill none. click 효과를 기다린 뒤 action을 호출하지 않는다. reduced-motion에서는 press 생략하고 action은 실행한다. CSS reduced override도 원본에서 별도 확인한다.
- 실제 Connection EN3button의rect/computed/DOM 증거: `source/action-button-live-connect-en.json`. advanced252.515625×44/back92.140625×44/connect214.40625×48. 위치는 부모 조립에서 검증한다.

## 부모 override 소유권

td/table-toolbar font12, capability-details padding6×10, filter-row weight500/transparentborder/#b5bac1 + selected#3b3d52/#c6ccff. Home height52, entry min-width224, start paddinginline40와absolute right18 SVG, file-entry border#686e7b와hover#3c3f46, sample-entry weight500/paddinginline12는 **부모가 명시적으로 전달할 값**이다. Atomic가 action 이름으로 부모를 추측하지 않는다. icon-button44와category-toggle/result-row는 별도컴포넌트다.

## 재개 기록

2026-10-06 압축 후 AGENTS→MASTER_RULES→STATUS→이명세→직접원본CSS/JS/liveConnectionEN→기존Action/WithIcon 및잠긴primitive계약을실제로읽었다.4LOCKhash통과. 기존Pulse180ms/.62opacity와WithIcon간격10은원본press160ms/.86 및gap9와다르므로복제근거로사용하지않는다. 원본/부모수정없이독립candidate를구현한다. 아직LOCK없음.

## 후보 및 검증 진행 · 2026-10-06

후보 `src/Verifier.Desktop/WebActionButton.cs`, 독립 probe `tools/ReplicaProbe/ActionButtonProbe.cs`. 기존 MainWindow/App/원본/잠긴4primitive를 수정하지 않았다. 실제 C# Button 입력/automation peer를 사용하고 paint-only Surface, 명시적 Typography, SvgIcon, public Motion.Play를 조립한다. parent overrides는 explicit padding/height/min-width/font/absolute trailing icon/파일버튼 palette이며 action이름으로 추측하지 않는다.

- macOS+Windows 실제 원본23case와 native의 외곽/라벨/SVG rect 차이0: source/action-button-{mac,windows}-layout-comparison.json.
- macOS parent override 포함28case local-box차이0: source/action-button-mac-parent-comparison.json. `.table-toolbar`의 바깥margin12는 부모책임으로 absolute위치비교에서 분리하고 button내부 offset/크기를 비교했다. capability fixture는 독립wrapper에서 실제font13이며 실제td의font12는 table case로검증한다. 실제부모배치 전체완료 주장없음.
- Windows28case source/windows-source-action-button.json 및windows-action-button-native-layout.json에서도 local-box차이0(비교기록을추가로보존할것). DPR2.
- source/action-button-live-home-ko.json: start224×52, padding40/absolute right18; file224×52/transparent-border#686e7b; sample52/500/padding12. 반올림값을임의padding상수로fit하지않고Blink fixed-point flex 기여/center와unitless line-height의해결규칙을candidate에서적용했다. Typography primitive변경없음.
- Windows 실제UIA Invoke: source/windows-action-button-click.json, click1/새창제목clicked, min opacity .874838→1/identity복원. `tools/WindowsControl/action_buttons.py`는정확한title/EXE path/automationId만대상으로한다. 이때패키지는23case/초기statecandidate이며이후parent/transition교정의최종실제검증을대신하지않는다.
- Mac 최신개발fixture 실제 click/Tab(skip disabled)/Return: source/action-button-mac-{click,keyboard-click}.json, screenshots/action-button-mac-keyboard-focus.png. 경로handle갱신오류는bundleID재연결뒤실제상태를확인했다. 첫실패를성공으로기록하지않았다.
- source/action-button-source-mac-interaction.json: 실제원본hoverprimary #4752c4/shadow0 2 5 #00000018, pointerfocus ring없음; Tab→quiet focus-visible3px#A7B1FF/offset3.
- screenshots/action-button-mac-initial-comparison.png는원본CSS-pixel screenshot과native2x의축소preview다. 동일DPR actual pixel일치증거가아니다.

CSS transition은property별clock을유지하고same target 재시작을피하며빠른reverse의shorteningfactor를구현했다. 아직 source fractional reverse/actual hover·pressed·disabled·reduced/statechanges 및최신Windowsfixture 대조가끝나지않았다. **IN PROGRESS 유지, LOCK 없음.** 다음은 이상태/시간표본검증→차이교정→최신actual양OS확인→LOCK이며다른Atomic/부모동시착수금지.

근거: [CSS shadow interpolation](https://drafts.csswg.org/css-backgrounds/#propdef-box-shadow) (none을transparent/zero-length shadow로보충), [CSS transition reversing](https://drafts.csswg.org/css-transitions/#reversing), [Blink LayoutUnit](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/platform/geometry/layout_unit.h). 적용결과는원본의실제computed/rect와대조하며문서만으로통과시키지않는다.


## 재개 및 실제 입력 후속

2026-10-06 압축 후 AGENTS → MASTER_RULES → STATUS → 이 명세 → 직접 CSS/JS 및 live Home 버튼 → 후보/제어 driver를 실제로 확인했다. 4 LOCK hash 유지. 원본 104 상태 및 13 배경색 fractional/reverse 표본이 일치했다(`source/action-button-state-comparison.json`). 역방향 duration 차이 약1.17e-12ms. 중간색을 byte로 먼저 반올림했던 문제는 부동소수 premultiplied color track으로 교정했다. CSS #RRGGBBAA와 Avalonia #AARRGGBB 차이로 빠졌던 그림자 alpha도 교정했다. 기존 preview 이미지는 최종 raster 증거가 아니다.

최신 Windows 실제 Space press/hold/release에서 `source/windows-action-button-key-held.json`은 pressed=true/hover=false/focus=true, bg#3c45a5, scale.98, primary shadow0 2 3 alpha36, click0을 기록했다. release 후 새 click1과 fill-none 복원을 확인했다. M → reduced → UIA Invoke 후 실제 click2 기록도 수령했다. disabled Invoke는 ElementNotEnabledException 대신 generic UIA provider exception을 반환해 driver가 실패했다. 앱 실패/버튼 동작 성공으로 승격하지 않으며 exception과 clickcount 불변을 함께 확인하는 진단으로 수정할 예정이다. 최신 actual 기록을 보존한 뒤 남은 shadow/color/scale 시간 표본과 raster 비교를 마치며, 아직 LOCK하지 않는다.


## 최종 대조 및 발견한 직접 하위 의존

104 상태, 기존13 배경색 표본, 추가39개 배경/글자/그림자/scale fractional·reverse 표본을 통과했다. `source/action-button-track-comparison.json`: source 직렬화 오차 범위의 geometry0.0001px/scale1e-6, painted RGBA byte 일치, reverse duration 최대차2.01e-10ms. none과 time0의 invisible zero-length shadow는 paint 동등성만 정규화했다. shadow track도 중간 alpha를 부동소수로 유지하고 최종 paint에서만 byte로 바꾼다.

최신 actual Windows Space→click1 및 disabled Invoke 거부/count1불변, M→reduced→click2/allopacity1은 `windows-action-button-final-{keyboard,reduced}-click.json`과 disabled-invoke에 남겼다. title 변화만 확인하던 driver에 새 POST 수정시각 확인을 추가했다. 수정 전 keyboard 보관이 과거JSON을 읽은 실패는 최신 fresh 검사로 대체했고 성공으로 쓰지 않는다. 실제 macOS 최종 pointer hover는 #4752c4/shadow0 2 5 alpha24, Tab은 disabled를 건너quiet, Return은 click2 및 opacity/transform 복원. 해당 final 증거와 keyboard-focus screenshot을 연결했다.

Windows 실제 DPR2 native/source11 normal ROI 비교는 `source/action-button-windows-raster-comparison.json` 및 `screenshots/action-button-windows-raster-comparison.png`. source와native renderer edge AA 차이는 남아 있으며 max ROI MAE0.182/255다. 실제 keyboardfocus quiet행은 normal 비교에서 제외하고 focus증거를 별도로 쓴다. 양OS28case local-box 차이0. 상위 페이지/부모 absolute 배치는 아직 승인하지 않는다.

LOCK 직전 실제 Connection busy 상태를 추가 확인했다. `.btn.primary:disabled` 안의 SVG가 **16px `.spinner`**로 바뀌고 EN라벨은 Checking…이다. source UI에 존재하는 이 직접 하위가 아직 복제되지 않아 ActionButton은 잠그지 않는다. 다음 단일 활성은 Spinner를 Bottom-Up으로 조사/구현/검증하는 것이며, Spinner LOCK 후 이 버튼의 loading child+gap/label을 조립하고 완료 판정한다. 원본/live의 실제 disabled/Checking…/spin800ms/currentColor/transparent rightborder/opacity1을 확인했고, 단색 opacity.7 reduced 표시는 source CSS/Motion 계약과 함께 검증한다.


## 최종 계약 · ActionButton LOCK · 2026-10-06

`src/Verifier.Desktop/WebActionButton.cs`의 독립 candidate를 잠근다. base/primary/quiet/danger, plain KR/EN label+선택적SVG18/gap9, parent explicit override, CSS hover/pressed/selected/disabled/focus/transition, 즉시action+source press160ms, reduced bool을 제공한다. 원본 busy는 `IsLoading=true`로 LOCKED Spinner16을 조립하며 enabled/disabled와라벨은부모가별도로설정한다. loading 해제는 spinner를visual tree에서제거해loop를dispose한다. source대로parent가color와reduced를child에전달한다. `AbsoluteTrailingIcon`/palette/font/padding/height/min-width는parentexplicit값이며action이름을추측하지않는다.

직접하위 SvgIcon/Typography/Surface/Motion/Spinner LOCK 유지. 기존 MainWindow helper나App.axaml을원본으로채택하지않았다. App의일반Button style과ContentPresenter pseudoclass가후보의local sizing/자체template/명시적childpaint에끼어들지않음을코드와계약으로확인했다. 실제상위App조립/절대배치는추후해당컴포넌트에서검증한다.

최종확인: macOS/Windows DPR2 32case (Home/부모override 및KR/EN normal/reduced loading 포함) outer/locallabel/SVG/spinnerrect차이0. source/action-button-{mac,windows}-loading-comparison.json. 116 pseudo-state paint와52 timing표본 통과:state/track-comparison.json. 실제양OSpointer/keyboard·disabled refusal·reduced click과actual busy child색·opacity·제거/normal복귀는final input 및action-button-{mac,windows}-busy-window.json에보관했다. actual source connectbusy DOM도추가로확인했다. 양OS build0warning/0error.

Windows동일DPR2 normalpaint11case, Spinner15case edge AA차이는각raster/ink보고서에서보존한다. renderer AA의미세차이를pixel-perfect라고부르지않으며구조/metric/state/color차이를그이유로면제하지않는다. macOS APIlogicalcapture를DPR2동일pixel증거로올리지않는다. 다른DPR/Winx64/페이지별absolute위치및연결550ms/DB동작은이Atomic LOCK범위가아니다.

조립은이publiccandidate를사용한다. 새경계/부모override가실제원본에서발견되어수정이필요하면LOCK예외이유/영향/원본근거를먼저기록하고영향범위를다시대조한다. 기존초기진단의‘미완료’는이최종범위의완료판정보다우선하지않는다.

## LOCK 예외 · 2026-10-08 · 수정 전
이유: 원본 고급 설정의 button은 aria-expanded/aria-controls를 가지며 버튼 접근성 peer에 펼침 상태가 필요하다. 영향: 클래스 sealed 제약 한 단어만 제거해 상위 AdvancedConnection의 국소 subclass가 ButtonAutomationPeer/IExpandCollapseProvider를 제공한다. 기존 template/크기/paint/input/motion 메서드는 변경하지 않는다. 원본 근거: live connect() 고급 버튼 expanded/collapsed AX 및 original.js aria-expanded/controls. public primitive에 새 일반 상태/디자인을 추가하지 않는다. subclass의 펼침 동작은 상위 section에서 대조한다. 기본 ActionButton 양OS 기존32 layout/loading 표본과 코드 나머지 불변을 재확인한 뒤 재LOCK한다.

## sealed 예외 재LOCK
나머지 소스는 저장한 이전 코드와 byte-equivalent이며 class sealed 한 단어만 제거했다. 양OS 기존32조건(local root/text/icon/spinner/normal/reduced loading)의 원본 대조 차이0: action-button-{os}-unseal-comparison.json. 기존 paint/transition/input 코드 변경이 없으므로 이전116상태/52timing actual 증거 유지. 기본 Button peer는 그대로이고 새 subclass의 expanded peer는 AdvancedConnection에서 별도 검증한다. source 일반버튼의 spinner key 부재를 null로 정규화했다; 첫 serializer 비교 실패를 통과로 사용하지 않았다. 단일 예외 종료 후 재LOCK했다.

## LOCK 예외 · Windows 부모 접근성 중복 · 2026-10-08 수정 전
원본 actual Windows Table permissions Button name=필요한 권한 보기, ControlType.Button, focusable=true, children0. 후보는 같은 이름/역할/입력에 visible label의 Text→Text2를 하위로 노출한다. primary: artifacts/replica/workers/preflight/capabilitytable/f021e0de19e4455d953d37728054df00/actual-windows/input/{source,native}-ax-initial.json 및 remount. 앱 누락을 OS차이로 면제하지 않는다. 변경 전 hash b2fef60897a17bea1d1493d40829b71f5a5828ce826dc683960ebdcb88a2f458.
이유/영향: 표의 directButton 및 Home/Results/공유 버튼 부모 트리에서 라벨 중복 없이 원본 leaf Button을 제공한다. Windows만 ButtonAutomationPeer의 children empty; 기본 Button 역할/Invoke/이름/상태/포커스, Mac 기본peer, 모든 layout/paint/input/motion 코드는 그대로. AdvancedConnection은 자체peer override를 그대로 유지하고 이번 변경 영향 밖임을 코드로 확인한다. 기본버튼 KOEN/normal/disabled/loading/selected8 및 detach/remount/headless+actual 양OS peer 확인 후 reLOCK. 기존 geometry116/timing52/actualinput/busy 증거는 동일코드 재사용, 부모 전체완료로 확대하지 않는다. 보호검사 현재 실패는 preflight의 기록된 Row 예외 하나이며 ActionButton 기존hash일치 확인 후 변경한다.

좁은 교정 checkpoint: Macaf085/3c63 headless8+remount8는 이전 defaultpeer Button/child1/name/Invoke 그대로. 실제Mac3f7f PID64815 AXButton/name/child1 8 기록/close/exit0; 최초진단이 Windows child0를 Mac에도 가정하여 실패했으며 Mac 변경 없이 기존default 회귀 기대값1로 교정. 이는 새원본Mac descendant동등성 주장이 아니라 Windows-only 변경의 비영향 검증이고 기존Mac layout/input 증거 유지. Win8ffa/fcac headless8+remount8 Button/name/child0/Invoke 통과, actualWindows 대기.

검증 재사용 조정(추가 실행 전): Windows 새headless8/remount8로 normal/disabled/loading/selected의 동일leafpeer를 검증했고 oldlayout/paint/input/busy 코드 byte-equivalent proof product-change-proof.json을 저장했다. actual새영향은 preflight의 동일Buttonhash frozenRow 부모 KOEN2 permissions UIA directchildren0/name/role로 확인한다. 기존Windows 실제state/input/paint 조건은 재사용하고 별도8상태창은 같은변경을 중복확인하므로 실행하지 않는다. actual없이 LOCK하는 것이 아니며 Row새영향48/parentscope 필수검증은 유지한다.

## Windows leaf peer 예외 재LOCK
후보0556d146. Win8ffa/fcac headless8+remount8 Button/name/child0/Invoke, Macaf085/3c63 headless8+remount8 unchangeddefault와 actual3f7f8 name/role/child1/cleanup0 회귀 완료. 실제Windows preflight756/3c907 부모KOEN2 permissions Button은 sourceexactname/ControlType.Button/children[]와 일치하며 같은Buttonhash0556d146 확인, actualcleanup0. changedButton 부모 증거 capabilityrow-shared-button-parent-evidence.json을 lockmanifest에 연결했다. 원본glyph/state/input/shape/motion/busy 코드의 byte-equivalent를 product-change-proof로 확인해 기존검증을 재사용했다. standaloneWin8 새창을 중복실행하지 않았고 부모실제UIA를 확인했으므로 headless-only LOCK이 아니다. 이 변경은 Advanced 자체override peer의 새부모 완성을 의미하지 않는다.
