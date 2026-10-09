# TextInput

## 완료된 LOCK 예외 · NumberInput 실제 포커스 소유권 · 2026-10-06

변경 전에 기록한다. NumberInput의현재wrapperSpinnerpeer가editor를children에서숨겨WindowsUIASetFocus뒤wrapperHasKeyboardFocus=true여도FocusedElement=null이되는실제실패를확인했다. guard가키전송을막았으며이를성공으로쓰지않는다. 원본은단일Spinner에Value/RangeValue/Text/ScrollItem이고실제포커스된입력자체가그요소다. 영향은WebTextInput의sealed제약해제한줄과NumberInput의직접editor subtype/peer다. WebTextInput의기본template/style/삽입/IME/selection을바꾸지않고Number전용subtype가OnCreateAutomationPeer만대응할수있게한다. 일반text/search기본후보양OS박스/글자/초기값재검증후해시재잠금한다. NumberInput상위구현은예외교정검증이끝나기까지보류한다.

예외검증완료: production WebTextInput은sealed→class한줄외변경없다. 공개OnCreateAutomationPeer의Number전용override확장만허용하며defaultTextBoxpeer는동일하다. 양OS새publish로기존10case씩box/advance/fontfragment를원본과재대조해20/20통과했다(source/textinput-{mac,windows}-peer-default-comparison.json). 이벤트/IME/template코드는변경하지않았다. WebTextInput해시를갱신해재LOCK한다. NumberInput만다음단일활성이다.

LOCKED · Radio LOCK 후 검증한 단일 atomic. 원본의 text/password/port/search input 자체를 조사한다. Field/SearchInput 부모 label/icon/clear/layout은 부모 소유다. 기존TextBox/App.axaml을 원본의 근거로 사용하지 않는다.

## 최종 판정 · 2026-10-06

TextInput을 LOCK한다. 원본의 단일 줄 text/search 편집 표면 및 비활성 password 예시가 범위다. 실제 앱 페이지를 조립했다는 판정은 아니다. NumberInput, label/hint/icon/clear, 테이블 검색 갱신, 접속/validation은 직접 부모가 소유한다.

| 항목 | 원본 계약 / 확인 결과 | 근거 |
|---|---|---|
| 구조·부모 경계 | 실제 TextBox / PART_TextPresenter / PART_ScrollViewer. 부모 공급 width238/366/380, height44, border1/radius8/padding 일치. | live-connect/live-search JSON, 양OS native/font-fragment-comparison |
| 글자·spacing | text14/22.4, search13/20.8, KR/EN/empty/연속공백/장문20cases의 advance 차이 ≤1/64 CSS px, fontfragment높이20cases 일치. | textinput-{mac,windows}-font-fragment-comparison.json |
| 색·상태 | #1e1f22 / #dbdee1, placeholder#a3a9b2, disabledpreview편집거부. Mac동일browser palettecalibration/Win#063aa4 실제선택 일치. hover에는 원본의 추가surface변화없음. | Macpalettecalibration.jpg, Windowsnative-selected.png, disabled refusal |
| 포커스·움직임 | 120ms CSS ease/premultiplied RGBA/3px shadow/reversalshortening. common원본CSS72fractionaltracks통과, reduced/분리취소. | textinput-{mac,windows}-motion-comparison.json / actual reduced |
| 편집·선택 | 실제입력/부분·전체선택/arrow/replace/paste(CRLF제거·tab공백유지)/undo. blur범위유지·highlight숨김, SearchEscape빈값·focus유지, Enter연결action없음. | macpaste-{singleline,undo}, selection-blur-caret-final, search-reduced-final, enter-final; Winactualkeys |
| 장문·포커스 이동 | 고정238에서 EOF Mac544.5/caret755, Win509/caret720. blur0·range보존. Tab복귀0..105전체선택/offset0 양OS 실제일치. | long-{focused,blur,tab}-final.json / long-tab-source.json |
| 커서 | Mac14font16.5/search15, Windows18.5/search17. 공통inlinepositionround/EOFclamp. readonlyOSperiod Mac500/Win530ms, deadline유지 및 actual6toggles 확인. | sourcecaret images / fontfragmentcomparator / mac-blink-final.json / windows-blink.json |
| 조합 입력 | publicnative IMEclient의 ㄱ→가→commit가 단계에서 preedit와기존값보존. 진단fixture 성공이며 OS키보드언어 전환 시험은 아니다. | Macime-f9/f10/f11-final 및Windows immutableF9/F10/F11 |

양OS 최신production빌드가 위 focus/scroll교정을 포함한다. 마지막WindowsTab은KeyUp/layout/render후immutableframe으로판정했다. UIA가나중에다시SetFocus한F8결과와일반focuscallback중간값을Tab그순간의증거로쓰지않는다. 366→238축소직후offset381은원본고정238EOF와다른전제여서통과로승격하지않았으며고정238에서509를재검증했다.

현재reference의KR/EN・systemfont・DPR2 환경 계약이다. Windows선택색은현재Edgeprofile의실측값이며모든OSaccent/브라우저를검증했다는주장은아니다. MacJPEG의ICC/AA/압축이달라rawRGB/rasterbitperfect주장을하지않는다. 실제글자advance/box/fontfragment/배치는위측정으로검증했다. RTL・복잡script・실제활성password는원본의대상UI에없어완료범위에포함하지않는다. 정상타이머에서도VM스케줄러 지연은있다(Win6×530ms대비23mslate); CSS/OSperiod계약과actual재생을구분한다.

구현 보호: WebTextInput.cs / WebEditorTextPresenter.cs / MacEditorShaping.cs / MacEditorSystemColors.cs / WebInputCaret.cs. 해시정본 source/textinput-lock.json. 명세변경근거없이이코드를바꾸지않는다. TextInput부모조립시이계약을재사용한다. 전체복제상기제약은계속유지한다.

## 다음 행동

NumberInput 하나를 활성화해 실행 원본 port의 UAstepper/hover/pressed/키보드/invalid/min/max/빈값부터 조사한다. TextInput을일반TextBox로대체하거나잠긴코드를임의변경하지않는다.

아래의 초기 실패·미확인·스냅 포커스 기록은 과거 조사 이력이다. 위 현재 상태를 덮어쓰지 않는다.

## 原본 조사 · 2026-10-06

실행 원본 connect의 text/password/number와 selection의 search를 확인했다. source/textinput-live-{connect,focus,search}.json. 일반 text font14/line22.4, border1/radius8, padding9px12px, min-height44, input background#1e1f22/text#dbdee1, focus border#a7b1ff/glowspread3#a7b1ff22/outline none. transition border/shadow120ms CSS ease. pointer로 textbox에 focus해도 focus-visible이 true지만 outline:none 유지. hover 배경/테두리 변화없음.

- 실제 disabled password도 background#1e1f22/color#dbdee1/border#7c838c다. 뒤 input:not checkbox/radio의 specificity가 input[disabled]를 덮어쓴다. placeholder#a3a9b2/opacity1, 실제 disabled라 편집 불가. 비밀번호 기능을 켠 것처럼 구현하지 않는다. 화면 폭에 맞춰 placeholder는 한 줄에서 잘린다.
- 일반 입력 width 부모공급: server238/port112/database366 at1138pxviewport, height44. source/target의 group/label/hint/state invalidate는 Field/Connection 부모 소유다. 원자는 글자/선택/caret/가로scroll과입력만 맡는다. 원본 field()는 autocomplete=off/spellcheck=false다. Enter에 앱 연결 action은 없다.
- search font13/line20.8, height44, padding9px44px9px40px, width380. icon/clear는 별도SearchInput 부모 소유. Chrome Escape는 값과 selection을 비우고 focus유지하며 source input handler가 결과를 다시 보여준다. 실제 orders→Escape→빈값/3행/clear disabled를 확인했다.
- port는 type=number/min1/max65535이며 ArrowUp3306→3307로 실제 state invalidation을 확인했다. 숫자 steppers/UA 입력 정리는 편집 textbox 원자와 다른 결합 동작이므로 NumberInput을 TextInput 하위 검증 뒤 별도 composite로 지도에 추가한다. 이를 일반TextBox로 대체해 완료라고 하지 않는다.
- aria-invalid=true CSS는 존재하지만 원본JS는 이 attribute를 설정하지 않고 notice error만 다시그린다. 원자가 arbitrary invalid indicator를 추가하지 않는다. 진단fixture에는 CSS상태를 확인할 수 있지만 원본화면동작처럼 주장하지 않는다.
- 기존 App.axaml/TextBox/PasswordTextBox/HomeConnection의 추가CA field와font/기본Fluent chrome는 Source of Truth가 아니다. official Avalonia12.1.3 TextBox/TextPresenter/template를 확인했다. own TextBox template에 실제TextPresenter/ScrollViewer를 두어 OS native 입력·선택·clipboard/IME 경로를 유지하고 원본 surface/spacing만 공급한다. 먼저 native editingfont/baseline을 source 캡처와 대조한다. 차이를 보기 전에 custom text renderer를 추측 도입하지 않는다.

현재 단계: 후보 구현 전 명세 완료. 다음은 own template의 고립 text/placeholder/disabled/search 후보와 양OS glyph/상태/actual input 검증. 8priorLOCK 보호.

## 후보/초기 비교 · 2026-10-06

src/Verifier.Desktop/WebTextInput.cs와tools/ReplicaProbe/TextInputProbe.cs를만들었다. own template의actual TextPresenter/ScrollViewer,원본Surface/padding/placeholder와Escape 검색입력을갖춘초기후보다. 입력범위와parent경계는유지하며다른컴포넌트/App은변경하지않았다. 현재font/selection/caret/transition/actual입력이미완료이므로LOCK금지.

- 양식text/search/disabledpassword/공백/장문10cases 원본측정: source/textinput-mac-source.json, screenshots/textinput-mac-source.png. font14text22.4 /font13search20.8. nativebox10개는44pxheight/width/padding일치.
- sourcehostwidth146.5078125인데Avalonianative131.93359375, KR100.671875→99.75109375, 공백31.171875→26.263671875, searchorders39.03125→34.49951171875. 초기값은source/textinput-mac-initial-native.json, 최신후보JSON은artifacts/replica/textinput-mac/textinput-native.json이다. fontFamily.AppleSystemUIFont/fontsize14/weight400설정에도차이가있다. 기본편집글꼴/가변fontshaping을조사중이며단순전체xscale로고치지않는다.
- officialAvaloniafontcollection/FontManager/TextPresenter/Typeface코드를읽었다. SFNS.ttf를public EmbeddedFontCollection으로직접읽어도FontManagerGlyphTypeface의hmtx기준폭이131.9336으로같다. SKFont14폭은151.1152라서Skiafamilyalias교체만으로source146.5078을맞추지못한다. SourceUIFont의variation/shaping을검증한public경로가필요하다. plain lockedTypography의CoreText/HB코드는임의변경하지않는다.
- 초기probe는ScrollViewer의theme가없는detachedcanvas에서TextPresenter가없어실패했다. 실제owned Window에canvas를부착하고FluentTheme로measure한뒤고립bitmap를렌더하도록수정했다. Rect의computedAspectRatio(Infinity)는JSON로기록불가여서x/y/width/height만명시했다. 최종probeexit0/빌드0warning0error. 실패한probe를통과증거로기록하지않는다.
- WindowsTextInput원본page를officialCLI로열고UIAexacttitle/process의ownedrecord버튼Invoke를요청했다. 기록수령은다음확인해야한다. 서버는동일bridge격리folder/endpoint만유지하며PTY74056으로재시작했다.

현재다음행동: Windowsfresh원본JSON확인,Macsourcefontvariation과TextPresenter실제glyphshaping교정,selection/caret색및geometry/IME/단일line/scroll을원본에대조한다. focustransition은현재snap초기후보이며120msCSS ease로아직교정하지않았다. ReducedMotionproperty도초기후보에있으나transition완료전검증주장금지. sourceportnumber composite는후속TODO이며text완료와섞지않는다.

## 후속 교정/실제 검증 · 2026-10-06

빈 입력의 synthetic space 보존 문제는 수정 후 macOS 10개 재실행에서 width0으로 확인했다. Windows own CLI/UIA 실행·새 패키지·10개 렌더 JSON/2x PNG도 수령했다. 영어/한글/연속 공백/search/장문 폭은 양OS 모두 DOM 양자화1/64px 이내다. source/textinput-windows-native.json 및 screenshots/textinput-windows-{source,native}-2x.png.

Windows source의 glyph ink가 native보다 1 physical px 위에 있는 차이를 발견했다. LOCKED WindowsWebTextBlock의 CssBaseline public 계약을 호출하고 ShapedBuffer의 glyph offset Y에만 차이를 공급해, 선택/커서/편집 linebox를 이동시키지 않는다. 최신 win-arm64 실행에서 EN/KR/spaces/search/장문6개 inkbounds는 정확히 같다. glyph shaping/run은 public TextLayout 경로며 source source/target DB는 접속하지 않는다.

포커스는 snap에서120ms CSS ease track으로 수정했다. RGBA premultiplied interpolation, shadow none↔spread3, reversal shortening, ReducedMotion snap, detached frame 취소를 갖춘 후보다. derivative 원본 CSS fixture(textinput-motion.html)에서36 fractional forward/reverse를 수령했다(source/textinput-mac-motion-source.json). native실제 production FocusTrack을 반사/고정clock으로 측정했다(textinput-mac-motion-native.json). reverse duration69.32594647ms가 원본과4.1e-9ms 차이이며 중간 spread 일치. 정식 comparator/Windows timing/reduced 실제 창은 아직 남았다.

실제 입력: Mac 연속 공백6문자/전체 선택/Left→0 collapse/shiftRight→0..1 partial을 증거 JSON으로 기록했다. 초기 Shift 대문자는 CUA가 거부했고 입력 성공으로 기록하지 않는다. 첫 partial replacement은 예상과 달라 새 단계별 증거로 확인했으며 일반화하지 않는다. Windows UIA ValuePattern orders입력→Escape 빈값/focus유지와 disabledpassword ValuePattern refusal은 확인했다. Ctrl+A는 실제 KeyDown/Up Control+A가 도착했으나 선택0..0으로 남았다. **실패를 통과로 쓰지 않는다.** 현재 원인 진단은 native presenter attachment/keymap/source/handled를 owned probe에 기록해 다음 후보에서 확인한다.

선택색은 미완료다. Win source의 UIA TextPattern.DocumentRange.Select + official capture는 실제 RGB(6,58,164)/white 및44physicalpx selection height를 보여준다. Native Fluent 색과 달라 교정 근거를 조사한다. Mac은 Chromium primary code의 dark NSColor.selectedTextBackgroundColor→sRGB/204alpha 경로를 확인해 MacEditorSystemColors 후보를 만들었고 process drawing appearance만 잠시 바꿔 finally복구한다(사용자 OS/App preference 변경 아님). 캡처가 JPEG이므로 현재 약간의 채널 차이를 exact raster 일치로 주장하지 않는다. 현재 단일line paste/longscroll/caretblink/IME/palette/Windows selectAll/reduced 실제 및부모재조립은 미검증, TextInput LOCK 금지.

종료 주의: Mac close 후 getAXState는 같은 앱을 재실행할 수 있다. 실제 한 번 재실행돼 다시 close/listApps로 종료를 확인했다. closed handle에 AX를 호출하지 않는다. 서버/AGENTS 임시 제약은 전체복제 종료까지 유지한다.

Windows Ctrl+A 診断の結果: presenter/keymap/routed keyは正常だった。changes logはSelectionEnd22→IsFocused変化→SelectionEnd0→refocusを示し、Avalonia12.1.3 TextBox.ClearSelectionOnLostFocus defaulttrueが検証host終了時に範囲を消していた。原本browserはbuttonをclickしてblurしても0..22を保つことをactualで確認(source/textinput-mac-source-blur-selection.json)。原本契約に合わせClearSelectionOnLostFocus=falseを候補に設定した。Ctrl+A処理を独自再実装しない。次のfresh実行で再確認する。元の失敗証拠は診断であり成功に置き換えない。

比較スクリプトtools/ReplicaProbe/compare_textinput.pyを実行し、mac/windows advance各10とMacproduction focus track36を通過。source/textinput-{mac,windows}-advance-comparison.jsonとtextinput-mac-motion-comparison.json。各結果は部分検証でありcomponent acceptanceとは区別する。8既存LOCKの保護も再検査通過。

단일 줄 입력의 또 다른 실제 차이: source fixture에 `a\nb\tc`를 fill하면 `ab\tc`(4문자)로 정리되지만 native App.paste는첫줄a만남겼다. 원본 HTML의 CR/LF제거를 WebTextInput의 TextInput와 clipboard paste 경로에 공급했다. publicSelectedText는native HandleTextInput/undo/selection 경로를 유지한다. clipboard예상예외는officialClipboardHelper의4가지(timeout/cancel/access/COM)와같이중단하며값을로그하지않는다. 초기빌드는clipboardextensionnamespace누락으로실패,usingAvalonia.Input.Platform추가후0warning0error성공. 새 GUI paste/undo검증은아직남았다.

Mac색상비교는현재capture의encoding을먼저구분한다. BrowserJPEG는ICC없음, nativeJPEG는4064byte Display ICC가있고nativeimage를sRGB로변환하면채널이달라진다. 이 두 JPEG의rawRGB를정확한palette차이증거로쓰지않는다. AppKit public performAsCurrentDrawingAppearance diagnostic을실행해DarkAqua selectedTextBackground의sRGB가(0.24705882,0.38823530,0.54509807)임을확인했고후보helper는Chromium204alpha계약을사용한다. originalsource색상은primary https://chromium.googlesource.com/chromium/src/+/main/content/browser/theme_helper_mac.mm 와third_party/blink/renderer/core/layout/layout_theme{,_mac}.cc/mm에근거한다. 메모리/captureprofile없이bitperfect라고주장하지않는다.

Windows는현재실행원본Edge profile의actual selection RGB#063aa4/white를candidate에직접반영했다. Fluent기본palette를원본처럼쓰지않는다. 이값은현재referenceprofile계약이며다른브라우저/OSaccent까지검증했다고일반화하지않는다. nativeactual선택capture는재실행후확인할것.

Windows fresh Ctrl+A는ClearSelectionOnLostFocus=false교정후0..22를보존했다. 초기 실패를키분석결과로보존하며원본Keyboard처리는그대로쓴다. driver는반복동일키증거를timestamp archive로보존하고Ctrl+A에실제expectedrange검사를추가했다. prepare1회는prlctl InvalidArgument로실패했다. guestDLL hash를read-only로확인해published와다른것을확인한후재prepare를실행했고성공했다. failedprepare를새패키지검증으로쓰지않는다.

Mac 새GUI 실제 multiline/tab paste는`ab\tc`(4문자)로source와일치하고Cmd+Z가원래source.example.invalid로복구됐다. source/textinput-mac-paste-{singleline,undo}.json. 양OSproduction focus tracks36씩=72개는common원본CSS Macfixture의fractionalcomputed와통과했다(Windows 원본에서 별도motionfixture를실행했다는주장은아님). source/textinput-windows-motion-{native,comparison}.json.

다음GUIprobe는width238↔366(F6/F7),ReducedMotion(F8),public nativeIMEclient의diagnostic preeditㄱ→가→commit가(F9/F10/F11)를기록한다. diagnostics는OS키보드언어를변경하거나실제IME키조합통과라고주장하지않는다. 이probe가받은nativeclient/presenter형상과TextInput commit경로를검증한다. palette/scroll/input상태증거에viewport/extent/offset/preedit를추가했다. 새로운probe의actual실행은아직남았다.

현재 후속 검사에서 실제 blur 표시 차이를 발견했다. source는 selection0..60 범위를 유지하지만 다른 버튼으로focus를옮기면highlight를그리지않는다. native는false ClearSelectionOnLostFocus로범위만보존한상태에서bluehighlight도남았다. public TextPresenter.ShowSelectionHighlight를IsFocused에bind해범위보존과focus표시를분리했다. 새실제창확인이남았다. 실패capture는artifacts/replica/textinput-{source,native}-selection-blurred.bin에보존했다.

Mac palette의rawJPEG채널차이는source내동일인코딩calibration으로분리했다. 실제60space선택과RGBA(63,99,139,.8)CSS기준블록을같은원본browser에서캡처하면두중앙영역의최빈값은모두(64,85,116)이다. screenshots/textinput-mac-palette-calibration.jpg. 이결과는current원본의선택색이후보가쓰는AppKit sRGB/204alpha와같다는근거다. 서로ICC가다른source/nativeJPEGrawbyte완전일치주장은아니다.

WindowsF10read후preeditassert실패는CLIhost종료후focus이동으로composition이취소된finalsnapshot을읽었기때문이다. 검증probe는F6–F11키도착후layout완료시immutabletextinput-state-KEY.json/PNG를보존하도록교정했다. 실제OSIME키조합을실행했다는주장과구분한다.

커서 조사에서 실제 차이를 추가로 발견했다. nativebar는22.4pxlinebox전체높이,source는Mac14pxfont16.5/search13pxfont15,Windows14pxfont18.5/search17높이다. sourceinlinefontspan을본문CSSfont/lineheight로실제측정했고source/textinput-{mac,windows}-font-fragments.json에저장했다. sourcefontTop은Mac2.5/Windows1.5이다. initialnativecaret는end에서1px일찍그려지는것도발견했다. Chromium inline/caret_rect.cc는fontfragment높이/halfbar감산후localpositionround를쓴다. publicnativeTextLayout/hit testing/IME는유지하고nativecaretbrush만transparent로해WebInputCaretoverlay가sourcefontmetrics높이와위치를그리게하는후보를추가했다. selection/text/presenter를재구현하지않는다. 실제비교전LOCK금지.

Blink interval도native기본500ms와WindowsOS530ms차이를발견했다. officialChromium ui/native_theme/os_settings_provider_win.cc는GetCaretBlinkTime을쓰며currentVMAPI도530이다. MacdefaultsOn/Off는없음,Chromiumprovider는nonblinkingAccessibility preference/NSUserDefaults후fallback500을쓴다. 후보는해당readonlypublicAPI값을읽으며OS설정을변경하지않는다. staticprofile기준이라runtime설정변경을검증했다고주장하지않는다. WebInputCaret자체timer와원본실제capture가남았다.

장문focus변경에서도실제차이를발견했다. sourceMacEnd는scrollLeft544.5,blur후0이나native는544.328125를blur후에도유지했다. source/textinput-mac-long-{focused-source,focused-caret,blur-before}.json. 후보는sourcedevicepixeloverflow에맞춰content측정폭을DPRgrid로ceil하고blur시scrolloffset만0으로복구하며selection/caretindices는보존한다. refocus는publicTextLayout caretrect의BringIntoView를사용한다. 새로운actualfocus/blur/refocus대조가남았다. 이전focusedsource를blur후저장한textinput-mac-long-source.json은scrollLeft0이며focusedproof로쓰지않는다.

latestnativefontfragmentcomparator20cases통과. Macactual500mstimer6toggle3.001s,WindowssourceOS530ms및actual6toggle3.383s를저장했다. WindowsVMschedulerjitter가있으므로정확한530ms간격실측통과라고주장하지않는다. 일반fontbox/declaredinterval과실제toggle가능을구분한다. source/textinput-{mac,windows}-blink.json. Windowsactual선택capture의ownedcrop에서10477pixels가source#063aa4와정확히같다: screenshots/textinput-windows-native-selected.png.

Windows원본장문keyboardLeft→Right의focus유지진단을추가해scrollLeft509를확인했다(source/textinput-windows-long-focused-source.json). 처음UIA range변경후record버튼Invoke는blur후값0이므로focusedproof가아니다. TextPatternRangeEndpointnamespace초기오류도수정했다. SourceMAC544.5/Windows509는overflow폭을devicepixelnearest로round한값과같아후보ceil을nearest로교정했다. 실제sourceWindowslosslesscapture에서EOFcaret는document-relativex508/509,y1289..1325(37physicalpx)이다. inline/caret_rect.cc의max(viewport,line-width)-1rightlimit/halfbar/nearestCSSround를후보Caret에도공급했다.

Blinkprimaryplatform/timer.cc는반복deadline을유지하고latewakeup시period/20최소간격다음tick으로snap한다. 후보의DispatcherTimer누적지연을같은deadline계산으로교정했다. 새로운actual6toggle재검증이남았다. 기존VM3.383s결과를새교정통과로쓰지않는다. sourceRowfontHeight/style와sourceOriginalCSS/JS를변경하지않고derivativefixture에진단만추가했다.

최신scroll Offsetphysicalpixel snap후MacactualEnd544.5/blur0/범위105보존이원본과일치했다. SourceTab복귀는0..105전체선택이면서offset0이나native는544.5였다. source/textinput-mac-long-tab-source.json을근거로OnGotFocus Tab경로는offset0을유지하고다른focus만caretBringIntoView한다. 원본조회중잘못된#field-search-empty선택자는no_matches로실패했고실제DOMid search-placeholder를읽은뒤수정했다. Macdeadlinecaret6toggles3000.14ms를새증거textinput-mac-blink-final.json에저장했다.

최신Tab분기후Macactual0..105/offset0복귀가source와일치(source/textinput-mac-long-tab-final.json). 이어Right로range105collapse/offset544.5,500ms6blink2999.89ms확인. 최신Win6blink3203.12ms(6×530=3180대비23.12mslate),deadline누적지연교정전3383ms와구분한다. WindowsF6로366→238축소직후offset381이었다. source고정238End509와다른transition전제가있어assert실패를통과로쓰지않는다. 동일238전제에서long입력/Right를새로검증중이다.

복구후Winactual고정238에서F6immutableframe의Endoffset509/caretX720이source와일치했다. 이어Tab→search/F8immutableframe은offset0/caret105범위보존/selectionhighlightfalse로일치(source/textinput-windows-long-{focused,blur}-final.json). 일반JSON은focuscallback중간값을포함할수있어freshKEY만으로기하판정하지않고layout/render후immutableframe을사용한다. 최신Mac/Win빌드는OnGotFocusTab분기포함,Mac10box/fontfragmentcomparator재통과.

WinSourceTab실제keyup진단offset0/0..105확인(source/textinput-windows-long-tab-source.json). nativeShiftTab후F8검증은UIASetFocus가다시들어가는다른경로여서offset509로실패했다. productionTab동작차이라고추정해추가수정하지않는다. 진단probe에TabKeyUp/layout/render후immutableframe을추가하고driver가그기록을읽도록변경해consolefocus이동전의실제Tab결과를검증한다. 원본/production코드변경없음.

새probe다운로드/launch직후F6의unique-windowguard가실패(창생성전)해width238검증전제가충족되지않았다. 이후실제TabKeyUp0..105/offset0은받았으나366width이므로238동일전제로F6와Tab/ShiftTab을새로검증한다. 후속Windows명령은set-e로실패시멈추게한다. 오류를통과로쓰지않는다.

## LOCK 예외 · NumberInput 삽입 필터 조립 · 2026-10-06 · 변경 전 기록

1. 이유: 잠긴TextInput의keyboard와clipboard는서로다른삽입경로다. NumberInput부모가두경로에동일한숫자필터를적용해야한다. 사후Text교정은선택/undo/입력중표기를깨뜨릴수있고,복제된별도편집기를만드는것은조립계약을분기시킨다.
2. 영향: TextInput의TextInput/clipboard삽입과NumberInput부모. 서체/geometry/paint/focus/timer는변경하지않는다. 선택적으로공급하는InsertionFilter(Func<string,string>) 한개를SingleLine후두경로에서호출한다. null이면원래동작을그대로유지한다.
3. 원본 근거: Chromium NumberInputType::FilterBeforeTextInserted가키입력/붙여넣기의선택위치와중간표기를같은규칙으로검사한다. 실제Mactyping14+lexical9 및step24기록으로원본text input과number의필터차이를확인했다. https://chromium.googlesource.com/chromium/src/+/main/third_party/blink/renderer/core/html/forms/number_input_type.cc .

TextInput을일시적으로단일활성LOCK예외검증으로두고NumberInput구현은의존검증뒤에시작한다. nullfilter의CRLF/tab/undo실제paste와defaultgeometry를확인한뒤LOCK해시를갱신한다. 미검증상태에서기존LOCKhash를덮어쓰지않는다.

LOCK예외검증완료: nullInsertionFilter의Mac실제paste a\nb\tc→ab\tc 및CmdZ원래값복구;양OSdefault10씩box/fontfragment/advance기존source와재통과(textinput-{mac,windows}-filter-default-comparison.json). source/clipboard별동작은변경하지않으며필터가없는기본계약을보존한다. source/textinput-lock.json의WebTextInput해시를검증된새값으로갱신해재LOCK했다. NumberInput만다음활성이다.
