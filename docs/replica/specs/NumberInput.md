# NumberInput

LOCKED · TextInput을 조립한 숫자 포트 composite. 아래 최종 판정이 정본이며 이후 날짜별 기록은 조사 이력이다.

## 최종 판정 · 2026-10-07

**LOCKED** · 원본 Connection의 숫자 포트 입력 112×44, min=1/max=65535/step=1. 원본 웹·CSS/JS를 다시 확인하고 잠긴 TextInput을 조립했다. label/hint, 연결 입력 오류·연결 상태 무효화는 Field/Connection 부모 소유이며 해당 부모 완료를 주장하지 않는다.

| 항목 | 확인된 계약 / 증거 |
|---|---|
| 구조·크기·spacing | 단일 숫자 editor, 112×44, border1/radius8, padding9/12. Mac spinner11×22 / Windows15×22, 오른쪽13, 수직 중앙. `source/numberinput-windows-actual-comparison.json`, 양OS actual capture·pointer metadata, 기존 TextInput 계약. |
| 숫자·중간 편집 | RawText와 Value/validity를 분리. 빈값, 범위, 소수·지수·선행0,18자리 Decimal, 잘못된 입력, step 증감 **139/139 일치**. `source/numberinput-leadingzero-after-correction.json`. 일반 데이터 비교에 오차를 적용하지 않는다. |
| 실제 입력·선택 | 양OS 실제 키 입력/포커스/선택/arrow/OS paste. Windows trusted 1/e의 raw1e/Value빈값, Mac 실제 abc1e와 전각123의 정규화 확인. 기존 TextInput의 선택·붙여넣기·undo·커서 계약을 유지한다. `numberinput-windows-incomplete-comparison.json`, `numberinput-mac-source-os-paste-*`, `numberinput-mac-native-paste-*`. |
| 증감·반복·확정 | Windows 첫250ms/반복50ms, Mac readonly OS preference 또는500/50ms. monotonic deadline 예약. 키 증감은 change 확정, focused wheel은 blur 확정, 포인터는 release 확정. edit→hold의 중간 wrapper focus/change를 제거했다. actual down hold Mac500.80/500.22ms·median50.00/49.98ms·change각1. 양OS actual timing JSON에 연결하며 scheduler jitter를 완전 시간 일치라고 주장하지 않는다. |
| 상태·움직임 | focus/hover일 때 spinner, 읽기전용/비활성일 때 숨김. 포인터 위치의 위/아래 half만 hover/pressed. 원본 UA spinner에 별도 transition 없음. editor의 원본120ms focus/reduced 계약은 잠긴 TextInput 사용. |
| Mac 실제 paint | 공식 ScreenCaptureKit 단일 창 sRGB PNG. 위·아래 hover/pressed **4상태 전체22×44physical pixel 차이0**. `source/numberinput-mac-final-glyph-comparison.json`, `tools/ReplicaProbe/compare_number_spin_capture.py`, 연결 screenshots. hover invalidate 누락 교정 후 확인. |
| Windows 실제 paint | Segoe Fluent Icons, 원본em6/pressed5.5/baseline8.5/회색 AA. actual 크기·ink 위치·surface/foreground 상태 일치. 최신 hover invalidate 캡처도 최대채널차이2. `numberinput-windows-final-glyph-comparison.json`, `screenshots/numberinput-windows-native-hover-invalidation-final.png`. 아래 raster 검증 범위를 따른다. |
| 접근성 | 유일한 visible Spinner/editor에 Value/RangeValue와 실제 keyboard focus. Value assignment의 input/change 및 focus baseline 교정. 원본 raw 불완전 편집값 읽기와 actual focus 정상. `numberinput-windows-incomplete-comparison.json`, actual UIA inspect/keys. |

### 완료 조건의 적용 범위

판정은 현재 Mac ARM64·Windows11 ARM64 VM에서 실행한 Win-x64 후보, DPR2의 **원본 UI·입력·상태·기하·CSS 색 계약**에 대한 것이다. 다른 OS/배율/모든 설치 환경·부모 페이지 조립을 통과했다고 주장하지 않는다.

Windows glyph 가장자리의 최대2/255 명암 차이는 같은 위치·크기·원색을 서로 다른 Chromium/SkiaSharp rasterizer로 그린 결과다. 원본 CSS 색이나 크기를 바꾸어 이 차이를 숨기지 않았다. 이 항목은 Typography/TextInput에서 이미 확정한 계약과 동일하게 **raster bit-for-bit 동일성을 주장하지 않는 검증 범위**이며, 미확인 UI 상태를 면제하는 조건이 아니다. 모든 조사한 상태의 기하/색/interaction을 실제 확인했고 차이 수치도 보존한다. source/current renderer build 설정과 독립 font refit은 조사 증거이며 제품에 임의 font-size/hinting/gamma 보정값을 채택하지 않았다. `source/numberinput-windows-glyph-refit.json`; 공식 Chromium `skia/BUILD.gn`, Skia `SkMaskGamma.cpp`. Mac의4-sample 맞춤은 실제 source mask의 확인 가능한 paint 계약이며4상태 재검증했다.

Edge의 추가 UIA TextPattern은 원본 `<input type=number>`의 DOM/CSS/JS interaction에 사용되지 않는 브라우저 automation protocol이다. user 요구는 화면과 상태·인터랙션 복제이며 Chromium의COM provider 전체 복제가 아니다. Avalonia12.1.3 internal AutomationNode는 TextPattern을 public peer에 매핑하지 않는다. 이 framework ABI를 구현하기 위한 fork는 현재 component/제품 UI 계약에 적용하지 않는다. 대신 앱이 실제 사용하는 값 읽기/설정·범위·키보드·포커스는 원본과 검증했다. 추가 pattern 부재는 접근성의 전 기능 동등성으로 주장하지 않으며 보존한다. 이는 구현된 기능을 통과로 위장하는 면제가 아니라 브라우저 전용 protocol과 앱 입력 계약의 명시적 경계다.

숫자에는 번역할 label이 없고 KR/EN의 label/hint는 부모가 공급한다. 원본 포트에 없는 readOnly/disabled의 추가 상태는 방어 기능이며 원본 신규 화면을 설계하지 않는다. 실제 DB 결과나 서버 접속을 검증하는 컴포넌트가 아니다.

### 재개 지점

NumberInput의 보호 범위는 `source/numberinput-lock.json`. 부모는 이 계약을 조립하고 임의 변경하지 않는다. 다음 단일 활성은 **StatusPill**: 실제 원본 pill DOM/computed/부모 override부터 조사한다. NumberInput을 다시 설계하지 않는다. 전체 복제는 미완료이므로 AGENTS의 임시 재개 제약을 유지한다.

[이전 조사 기록](../history/NumberInput-earlier-investigation-20261006.md)은 역사 자료다. 그 안의 오래된250ms(Mac)/83state/다음 행동을 현재 계약으로 쓰지 않는다. 이 아래 날짜별 기록은 각 교정 전 근거이며 위 현재 상태가 우선한다.

## 원본 숫자 규칙 · 부분 조사

- Mac실제24stepcase: 빈값Up/Down1, min1Down값/표기유지, max65535Up유지, 0/-1은Up1·Down유지,65536은Down65535·Up유지,분수1.5→Up2/Down1,1e3→1001/999. source/numberinput-mac-step-source.json.
- 실제typing14+lexical9case: 알파벳/공백무시,연속decimal/exponent중복필터,전각123→ASCII123. '-'/'+'/'e'/'1e'는화면편집값을유지하지만DOMvalue빈값/badInputtrue. '1.'→DOMvalue'1', '+1'→'1', '+1e3'→badInputtrue, 01/1.0/1e03표기는유지한다. 별도RawText/Value가필요하다. source/numberinput-mac-{typing,lexical}-source.json. OSIME키전환시험과혼동하지않는다.
- 실제spinner상부pointerclick확인. ::-webkit-inner-spin-button CSScomputed는parentinput값을반환하므로112width를widget폭이라고기록하지않는다. actualfocusedcrop과ChromiumUAhtml.css의focus/hover때만표시를확인했다. source/numberinput-live-up.json / screenshots/numberinput-mac-source-focused.png(JPEG실제포맷).
- Chromium primary: number_input_type.cc의FilterBeforeTextInserted/ConvertFromVisibleValue/SanitizeValue, spin_button_element.cc의focus/step/repeat/scroll, UAresources/html.css, NativeThemeBase/Aura의partsize/arrowpaint. 다운로드진단은artifacts/replica/numberinput-research. Macoverlaypartwidth11후보/Windowswidth미확인,hover/pressed색및geometry는아직구현/완료로쓰지않는다.

다음은Windows원본UIApattern/step/paint조사,그리고keyboard+clipboard에동일numberfilter를넣을공용TextInput조립경로를확정하는것이다. 현재후보구현전,NumberInput LOCK금지. 전체복제상기제약유지.

## 확정할 후보 계약 · 구현 전

WebNumberRules는원본number의RawText→Value/validity,선택위치기반삽입필터,step1/min1/max65535증감을분리한다. NumberInput은잠긴WebTextInput을PaddingRight23(Mac11widget)/27(Win15widget)로조립하고UAstepper를content영역오른쪽에둔다. 원본처럼stepper는focus/hover시에만보이며범위경계로버튼을disabled시키지않는다. 부모Field는Value(정규화/빈값)와InputEvent를받아연결validation/invalidation을처리한다. 텍스트변경후사후강제숫자화/클램프는하지않는다.

Windows원본UIA에는Spinner/Value/RangeValue/Text/ScrollItem이있고innerbuttonchildren은없다. rect224×88physical=DPR2/112×44와ArrowUp3306→3307이실제일치했다. focuspaintlosslesscrop에서stepper15CSSpx,normalbackground#2c2c2c/arrow#9f9f9f를확인했다. Macactual11CSSpx/primaryNativeThemeAuraoverlaypartsize11,normalbackground#424242/arrowwhite와일치한다. 원본BrowserWidgetheight22CSSpx(paint整数)/linebox22.4,widgetright=contentright가근거다. WinFluentarrowはSegoe Fluent Icons U+EDDB/EDDCのfontglyph,MacAuraarrowはPathForArrow。hover/pressed/holdrepeat/scroll/clipboard/actualIME는아직검증하지않았으므로후보완료/LOCK으로쓰지않는다.

## 2026-10-06 재개 · 이번 교정의 원본 근거

AGENTS·MASTER_RULES·상태·이 명세와 직접 원본 `#source-port` DOM/CSS/JS 및 Number 후보를 다시 확인했다. 9개 LOCK 해시는 통과했다. 기본 기하 112×44, padding 9/12, font 14/22.4는 실행 원본에서 재확인했다.

기존 123개 수치 상태는 교정 후 모두 일치했다. 이어서 Chromium `Decimal::FromString`의 소수점 뒤 앞자리 0도 18자리 안에 포함되는 규칙을 확인했다. 실제 원본 `0.0000000000000000001e20`은 valueAsNumber=10이지만 rangeUnderflow=true이며 Up→1이다. 소수 앞자리 0을 제거했던 후보와 달라 이 규칙을 교정한다. 추가 16개 상태의 원본은 `source/numberinput-mac-leadingzero-step-source.json`이다.

버튼 반복의 기존 250ms는 Windows/base 기준이다. Mac `scrollbar_theme_mac.cc`는 기본 500ms/50ms이며 `content/browser/theme_helper_mac.mm`는 NSScrollerButtonDelay/NSScrollerButtonPeriod가 존재할 때 읽는다. Number 소유 helper에서 읽기만 수행하고 설정은 바꾸지 않는다. source timing과 실제 held 입력은 아직 검증 전이다.

`text_field_input_type.cc`는 방향키에서 change를 확정하고 포인터에서는 capture 해제 시 change를 확정한다. wheel은 input 후 blur에서 확정한다. 후보의 매 step마다 즉시 change를 보내던 경로도 Number 범위에서 교정한다. Alt+Down은 source 증감 대상이 아니다. 잠긴 TextInput이나 부모 설계는 변경하지 않는다.

### 실제 Windows held 대조에서 찾은 차이

공식 SendInput+UIA로 원본/후보의 실제 상부 버튼 hold와 release를 수집했다. 원본 첫 반복 250.10ms/후보 253.76ms, 이후 중앙 간격은 원본 52.0ms/후보 59.64ms다. 두 쪽 모두 release 뒤 change 한 번이다(`source/numberinput-windows-hold-timing-comparison.json`). 전체 누름 길이는 로컬 POST 지연을 포함해 다르므로 최종 숫자나 입력 횟수가 같은지로 시간 일치를 판단하지 않는다. 후보 timer의 상대 50ms 재예약이 누적되는 차이를 줄이기 위해 Number 소유 monotonic clock 기준으로 다음 반복 시간을 예약한다. 아직 교정 후 검증 전이다.

실제 normal/held Windows 화살표는 HEAD Fluent 코드로 만든 첫 후보보다 작다. GlyphRun의 ClearType를 원본의 grayscale AA와 같은 Skia paint로 바꿨지만 크기 차이는 남았다. 실행 Edge 버전은 151.0.4129.86이다. 이 실제 캡처가 HEAD 일반값보다 우선하며, glyph의 실제 size/baseline을 별도 진단 렌더와 대조한다. 스타일을 눈대중으로 잠그지 않는다.

### 실제 Windows glyph 치수 확정 · 구현 전

원본 normal upper의 30×22 physical crop과 같은 Segoe Fluent Icons/Skia grayscale AA 진단을 비교했다. em=6/baseline=8.5가 총 RGB 차이 180으로 가장 가까웠고, pressed는 em=5.5/baseline=8.25 또는 8.5에서 차이 48이다. 실제 Edge151 renderer의 상태 치수이며 HEAD Fluent 일반 arrow=8/7 후보는 맞지 않았다. normal/pressed의 기준선을 공통 8.5로 두고 em만 6→5.5로 바꾼 뒤 실제 창 hover/held 캡처를 재검증한다. 원본 normal down은 upper를 수직 반전한 것과 총 RGB 차이 48이다. 실제 lower glyph도 별도로 비교한다. `source/numberinput-windows-{glyph-fit,pressed-glyph-fit}.json` 및 해당 screenshots가 근거다. 눈대중 크기 보정은 하지 않았다.

## 2026-10-07 실제 재검증 및 다음 교정 근거

최신 후보의 실제 Windows upper/lower hold 반복 중앙값은 50.008/49.981ms다. 원본 52.0/50.05ms와 비교해 이전 59.64ms 누적 재예약 차이가 줄었다. 첫 반복은 원본 250.10/262.20ms, 후보 256.43/255.94ms다. 현재 OS 스케줄링 편차를 시간의 완전 일치로 쓰지 않는다. `source/numberinput-windows-anchored-hold-comparison.json`.

actual hover/upper pressed/lower pressed glyph 30×44physical crop의 최대 채널 차이는 2, RGB 합계 차이는 216/198/192다. 크기·위치는 일치하고 grayscale rounding 차이가 남는다. `source/numberinput-windows-final-glyph-comparison.json`와 `screenshots/numberinput-windows-final-*-comparison.png`는 실제 창 캡처이며 CPU 진단만의 증거가 아니다.

연속 native 검사의 값 재할당(3340→3306) 후 다음 pointerdown 직후 change가 1회 더 나왔다. UIA Value setter를 일반 property setter로 연결했던 후보에서 programmatic assignment가 이전 focus baseline을 유지한 것이 원인이다. 원본 UIA Value 변경은 input/change를 즉시 발생시키고, pointerdown 이후에는 release의 change만 나온다. 관련 raw 원본/native down hold event가 근거다. Chromium TextFieldInputType::SetValue는 no-event assignment와 input+change assignment를 구분하고, 같은 DOM Value라도 badInput editor 문자열을 갱신한다. Number 소유 assignment 경로에 이 구분을 반영한다. LOCKED TextInput은 변경하지 않는다.

압축 복구 순서를 실제 수행하고 9 LOCK을 확인했다. macOS CGEvent held 제어는 추가 범위 질문이 대기 중이며 응답 전 사용하지 않는다.

### 編集 후 pointer focus 차이 · 변경 전

실제 macOS에서 값을 붙여넣어 바꾼 뒤 아래 버튼을 hold하면 후보는 pointerdown 직후 change와 release change 두 번을 발생시켰다. 원본은 같은 편집 후 hold에서도 release 한 번이다. `source/numberinput-mac-{source,native}-down-hold.json`. 후보의 투명 UserControl wrapper가 Focusable=true여서 기본 포인터 focus가 editor→wrapper→editor로 이동하고 중간 LostFocus에서 편집을 확정하는 것이 원인이다. 원본은 단일 input의 focus를 유지한다. Number wrapper를 비포커스 컨테이너로 하고 실제 editor/NumberPeer를 focus 대상으로 유지한다. LOCKED TextInput의 focus 정책은 수정하지 않는다.

### 공식 macOS capture에서 찾은 paint 차이 · 변경 전

2026-10-07 사용자가 같은 두 검증 창에만 CoreGraphics·ScreenCaptureKit 공식 capture 사용을 명시적으로 허용했다. `tools/MacControl/number_pointer.swift`는 CUA로 확인한 앱 경로/bundle/unique title/source URL/6개 input/112×44/foreground/시작 예시 값 3306을 검사하고 CGEvent를 보낸다. capture는 ScreenCaptureKit의 desktopIndependentWindow 하나만 필터링하며 이미 있는 캡처 권한을 읽고 없으면 중단한다. 권한 설정을 변경하거나 Codex native 접근 차단을 우회하지 않는다. 오류 시 누름 해제를 보장한다.

Cua capture 호출 중 실제 포인터 위치/누름 상태가 달라졌으므로 해당 기존 up/down-pressed 파일은 눌림 합격 증거가 아니다. 공식 lossless sRGB capture를 정본으로 사용한다. `screenshots/numberinput-mac-down-pressed-official-comparison.png`의 왼쪽은 원본, 오른쪽은 후보다. 배경 #424242/#b1b1b1은 일치하지만 후보 triangle은 alias 및 위/아래 방향의 y배치가 달랐다. 원본 upper AA path는 (3,7)→(8,7)→(5.5,4), lower는 중심 y5.5에 대해 반전한 것과 일치하는 pixel bounds(x6..15,y8..13 / y30..35)다. 기존 후보의 bottom+1과 Aliased override를 Number 범위에서 교정하고 공식 capture로 재비교한다. HEAD 일반 PathForArrow 계산만으로 완료하지 않는다.

Windows UIA의 TextPattern 추가 제공은 Avalonia12.1.3 public NumberPeer로 구현할 수 있는 범위인지 조사했다. 공식 태그 AutomationNode.GetPatternProvider는 Value/Range/Scroll 등으로 제한되고 TextPattern 분기가 없다. 현재 UIA Value/Range/editor focus와 실제 화면 입력은 정상이다. 추가 pattern 차이는 아직 기술적 한계로 보존하며 UI 복제 완료로 임의 면제하지 않는다. primary: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Windows/Avalonia.Win32.Automation/AutomationNode.cs .

### macOS 4-sample coverage 확인 · 변경 전

AA+위치 교정 후 실제 glyph RGB 합계 차이가 39528→1371로 줄었지만 32개 edge pixel의 차이가 최대37이었다. 원본 edge는 배경66→113/161/208/255의 1/4 coverage이고 좌우의 coverage가 비대칭이다. 후보는 Skia analytic AA의100/124/148/197/221이다. 원본 vertices(physical 6,14),(16,14),(11,8)에 표준 4-sample 위치(3/8,1/8),(7/8,3/8),(1/8,5/8),(5/8,7/8)를 적용하면 원본 행별 mask와 일치한다(예: row9의좌2sample/우1sample). 이는 눈대중 AA 조정이 아니라 actual GPU MSAA coverage 대조다. Number 소유 Mac painter에서 device pixel native rect 계산 및 4-sample coverage를 사용하고, 원본 전체 glyph과 다시 비교한다. source/down pressed 공식 PNG와 행별 RGB가 근거다.

### 2026-10-07 복구 및 hover 갱신 근거 · 변경 전

AGENTS→MASTER→상태→활성 명세→실행 원본 source-port DOM/computed 112×44/padding9·12→현재 Number 코드를 실제 확인했다. 9LOCK 통과. official upper-hover를 새로 촬영해도 원본 상부 배경79/후보66이다. 후보의 state==1 색상은 이미79이므로 색상값을 바꾸지 않는다. NumberSpin의 IsPointerOver 변경을 명시적으로 invalidate하도록 하여 상태 변경 후 paint를 검증한다. 그 외 컴포넌트는 수정하지 않는다.

### 캡처 중 다른 제어와의 간섭 검출

교정 후 upper pressed를 다시 촬영하는 동안 Windows 검증 호출과 겹쳐 native 창이 비활성/스핀 미표시로 저장됐다. 이 표본은 실패이며 앞선 pressed 일치를 대체하는 합격으로 쓰지 않는다. Mac helper 완료 전 다른 GUI 제어를 하지 않고 capture 전/후 foreground·pointer 위치·예시 결과값을 확인한 경우만 PNG를 증거로 저장하도록 강화한다. 추가 허용 범위는 바꾸지 않는다.
