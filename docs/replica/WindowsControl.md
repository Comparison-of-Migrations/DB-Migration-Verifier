# Windows 검증 제어 경로

현재 상태: 공식 CLI + Windows UI Automation 제어 경로 확정. 프로젝트 소유 검증 창의 포커스·키 전달·실제 상태 변경과 원본 측정 버튼 실행을 확인했다. 현재 활성/LOCK 상태는 COMPONENT_STATUS가 정본이며 TextInput은 LOCK했고 NumberInput 원본을 조사 중이다. 부모 조립은 아직 완료하지 않았다. 압축 후 AGENTS/MASTER_RULES/COMPONENT_STATUS와 이 문서를 읽고 아래 초기진단보다 마지막 확정 경로/실제 증거를 우선한다.

## 확인된 경로

- 설치된 공식 `prlctl` 27.0.1: Windows 11의 `cmd /c ver` 실행 성공.
- `prlctl exec`: 고립된 예시 ZIP을 guest 임시 폴더에 다운로드/해제하고 동일132 fractional Motion probe 실행 성공. 결과JSON 회수 및 원본 비교 완료. CPU/target property 검사이며 실제 화면 조작 검사가 아니다.
- 설치된 `prlctl send-key-event --help`: key/scancode, press/release,delay,json 지원 확인. 실제 입력은 아직 전송하지 않았다. 공식 근거: https://kb.parallels.com/kr/129860/ . 일반 SDK는 deprecated이므로 새 SDK 의존을 추가하지 않는다: https://kb.parallels.com/en/130091 .
- `sendmouse-event`는 지원되는 명령으로 확인되지 않았다. 키 전달 기능을 마우스 제어까지 지원한다고 주장하지 않는다.

## 현재 UI 도구 경로의 실제 한계

CUA의 Parallels 관리 앱 AX 메뉴는 동작한다. 보기 → 전체 화면 종료를 실행해 VM 창 모드로 전환했다. 전체 화면의 windowNotFoundAtPosition 오류는 창 모드에서는 나타나지 않았지만, 두 좌표 배율의 taskbar 클릭 모두 결과 화면 변화가 없었다. AX Raise 이후에도 동일하다. Toolbar Keyboard의 AX action은 failure다. Windows guest helper/실제 prl_vm_app의 앱 연결은 timeout이다. Windows 실제 renderer의 bundle은 com.parallels.vm, 실행 중 prl_vm_app이며 관리 앱과 다르다. 단순히 같은 클릭을 반복하지 않는다.

OS 보안/접근성/공유/해상도 설정, 인증/라이선스, Parallels Tools 설치/재시작을 변경하지 않았다. Fullscreen→window는 진단용 표시 전환이고 DPR/창 배치는 증거 환경에 다시 기록해야 한다.

## 제안하는 후속

1. 공식 `prlctl exec`로 프로젝트 소유 fixture를 실행한다.
2. 공식 `send-key-event`로 테스트 창의 키 입력을 전달하고, 예상 창만 조작하도록 Windows UI Automation의 제목/프로세스 확인을 사용한다.
3. 클릭/선택/텍스트 입력은 Windows UI Automation의 Invoke/Selection/Value 패턴을 우선한다. 패턴 없는 자체 렌더 영역은 별도 입력 방식의 허용 및 실제 결과 확인이 필요하다.
4. 실제 화면은 CUA로 캡처하고 generated measurement JSON과 함께 검증한다. 실행 명령 성공을 화면 상태 성공으로 오인하지 않는다.
5. 계정/인증/사용자 DB/다른 탭과 앱은 대상에서 제외한다. 검증 파일·임시 fixture만 사용한다.

CUA 도구 지침은 다른 UI 제어 기술을 사용자가 명시적으로 요청할 때만 허용한다. 사용자가 “공식 CLI와 Windows UI Automation으로 진행”을 명시적으로 선택했다. 해당 범위의 프로젝트 검증 UI에 한해 전환이 허용되었다. 현재의 수동 Motion 창 요청은 반복하지 않는다.

## 증거/재개

- source/windows-motion-playback-{native,comparison}.json
- source/windows-typography-initial-differences.json (49표본 차이 교정 대기)
- source/motion-playback-window/ (macOS actual)
- 다음: 전환 응답 수령 후 Windows UI Automation으로 프로젝트 검증 창의 존재·제목을 확인하고 한 가지 입력부터 결과를 확인한다. 허용되지 않으면 현 CUA 한계를 보고하고 자동 GUI 검증 완료를 주장하지 않는다.

## 임시 파일/서버

VM fixture: C:\Windows\Temp\DBMV-Replica-Playback-20261006\app .
격리 HTTP fixture는 bridge100의10.211.55.2:8768, PTY71497에서 실행 중이다. 예시 파일만 서비스하며 전체 작업 폴더를 노출하지 않는다. 사용이 끝나면 종료하고 정리 결과를 기록한다.

## 2026-10-06 공식 경로의 실제 성공

사용자가 공식CLI와Windows UI Automation 사용을 명시적으로 허용했다. `tools/WindowsControl/driver.py`가 해당 범위의 고립된 검증용 driver다. PowerShell은UTF16LE EncodedCommand로 전달해prlctl 인수 재조립의 따옴표 손실을 피한다. 사용자 계정은`--current-user`로실행한다. System계정 임시 폴더는사용자에게접근거부가나왔으므로ACL을바꾸지않고사용자TEMP에fixture를새로다운로드/해제했다. 현재정본경로는`%TEMP%/DBMV-Replica-Playback-20261006/app`이며C:/Windows/Temp경로는GUI실행에사용하지않는다.

- UI Automation으로실제원본Edge창/기존Typography창/새Motion창을찾았다.
- 새Motion창실행/9finite완료+2infinite재생JSON수령.
- Window의SetFocus는focusableelement없음으로실패했다. 지원되는WindowPattern의Minimized→Normal로project창을활성화한뒤,공식send-key-event의scancode C46/M50/R19를press/release로전달했다. 제목·프로세스path가정확히일치하지않으면입력을보내지않는다.
- C후cancelled제목+infinite취소/opacity1/identity복원, M후reduced제목+11effect생략, R후completed제목+9finite재완료를실제기록으로확인했다. source/motion-playback-window/windows-{completed,cancelled,reduced,replayed}.json.
- Edge원본측정버튼의InvokePattern을찾아실행했다.49새기록이실제도착했고DPR2/기준선도이전과동일했다. source/windows-source-typography-user-recorded.json에기존수동기록을보존했다.
- guest창모드진단으로줄어든viewport는원래전체화면으로복원했다. 아직OS보안/공유/계정/Tools/해상도설정변경없음.

한계:prlctl실행중일부JobResult/RetCode가InvalidArgument를반환했다. 실패시후속키전송을막았고실제상태를읽은후필요한명령만재실행했다. keyboard/dynamicUI성공을임의좌표마우스제어성공이라고부르지않는다. WindowsUIA의Invoke/Value/Selection패턴은실제대상마다지원여부와결과를확인해야한다.

## 현재 확정 경로 · 2026-10-06 후속 검증

이 절이 위의 초기 진단/미해결 기록보다 우선한다.

1. `tools/WindowsControl/fixture_server.py`는 bridge100의10.211.55.2:8768에서 예시 패키지만 제공한다. 현재 서버 PTY74056. 종료한 경우 이 파일로 다시 시작한다. 원본 페이지 POST와 focus handshake JSON만 허용한다.
2. `driver.py`는 사용자TEMP의 검증 EXE 경로와 정확한 창 제목을 먼저 확인한다. SYSTEM TEMP를 GUI 실행에 사용하지 않는다.
3. 기존 WindowPattern minimize→normal은 키 포커스 보장이 없었다. 종료 후 콘솔이 포커스를 회수하는 실패가 있어 초기 방법을 정본으로 사용하지 않는다.
4. 검증 전용 Motion 상태 TextBlock에 Focusable=true를 추가했다. UIA의 유일한 focusable descendant를 SetFocus하고 FocusedElement.ProcessId가 검증 프로세스와 같은지 확인한다. 제품 화면/컴포넌트는 수정하지 않았다.
5. UIA 프로세스를 유지한 상태에서 nonce가 같은 focus-ready 증거를 수령한 뒤 공식 CLI scancode를 press/release로 전송한다. 창 제목이 목표 상태로 바뀌고 결과 파일 수정 시각이 이전보다 커져야 성공이다. 이전 JSON만 읽고 성공으로 판단하지 않는다.
6. C→M→R 세 단계 모두 이 강화된 경로로 exit0 및 새 상태 증거 확인. `source/motion-playback-window/windows-uia-{cancelled,reduced,completed}.json`, `screenshots/windows-control/motion-uia-client.png`. actual GUI/DPR2이며 CPU probe와 구분한다.
7. source 측정 버튼 InvokePattern도 재실행해49개 새 원본 기록 수령. 다른 Edge 탭은 조작하지 않았다.

긴 EncodedCommand(~4KB 이상)가 이 설치의 CLI에서 세션 오류로 거부되는 것을 별도 입력 길이 검사로 확인했다. 같은 허용된 inline 명령을 압축해 길이를 줄인다. 다운로드 .ps1 실행은 guest 정책에서 거부되어 폐기했으며 실행 정책/보안 설정은 바꾸지 않았다. Invoke-WebRequest는 HTML을 파싱하지 않는 UseBasicParsing으로 실행한다.

CUA Parallels 캡처는 guest가 갱신됐는데도 과거11:18 화면을 반환했다. 최신 증거는 공식 `prlctl capture`의11:33 화면과 UIA client rectangle을 사용한다. 실제 화면 캡처 전체와 프로젝트 창 crop을 함께 보존하며 exact raster 일치 증거로 주장하지 않는다.

실패는 성공으로 승격하지 않는다: 초기에 full-screen reduced/replay라고 복사한 파일은 새 입력 실패 후 과거 결과일 수 있다. 정본은 새 파일 시각까지 검증한 windows-uia-*이다. WindowPattern/좌표 클릭 성공을 일반화하지 않는다.

## Typography 실제 제어 후속 확인

N/R 전환(폭240·한글5행↔폭480·브랜드1행), system↔Consolas/tnum 전환도 강화된focus handshake/제목/path/새파일시각으로확인했다. source/windows-typography-{final,role}-live-{narrow,restored}.json. source/browser Document 및nativeclient UIArectangle을사용한공식capture에서Home16+role16 glyphinkbounds가일치했다. Windows제어는완료/미해결범위를구분하며임의좌표마우스제어성공으로일반화하지않는다.

새TEMP폴더의ZIP실행도확인했다(source/windows-control-clean-package.json). 최신packager는.deps.json/.runtimeconfig.json을반드시포함한다. 예전기존폴더에서동작한결과를cleanZIP통과로쓰지않는다. 사용자기존Typography창은건드리지않고이번에연새native창만정확한path/제목으로닫았다. 전체복제는진행중이므로격리fixture서버를후속검증용으로유지한다(PTY93774). 필요없어진전체작업종료시서버를중단한다.

ActionButton actual Space hold/release·disabled refusal/count 불변·M/reduced click과 projectwindow capture를 action_buttons.py로 확인했다. capture handshake JSON은 UTF8 byte로 POST한다. Spinner source CSS와 actual native lifecycle는 spinner.py로 검증했다. 컴포넌트 현재 진행/LOCK은 COMPONENT_STATUS가 정본이다.


Radio 방향키는 Parallels 공식 packer-plugin-parallels의 builder/parallels/common/scancode_to_keycode.go에서 PRL_KEY_DOWN104 / UP98 / LEFT100 / RIGHT102를 확인했다. send-key-event --key로 press/release를 보낸다. 단일 scan80은 keypad2이므로 방향키처럼 가정하여 보내지 않았다. source URL: https://github.com/Parallels/packer-plugin-parallels/blob/main/builder/parallels/common/scancode_to_keycode.go . Windows actual Down은 두 disabled를 건너뛰어 마지막 radio를 선택했고 fresh record의 Down-released/changes2/포커스를 확인했다. source/radio-windows-key-down-0.json. OS설정은 변경하지 않았다.

TextInput 측정의 허용된 source-textinput.json/textinput-input.json endpoint를 추가하고 동일고립서버를재시작했다(PTY74056). 전체repo노출이나공유설정변경은없다. 현재Radio는LOCK이며TextInput진행상태는COMPONENT_STATUS/명세가정본이다.

## 현재 fixture · NumberInput 재개

2026-10-06 TextInput actual 제어/검증을마치고9번째LOCK으로보호했다. source-numberinput.json endpoint를추가한동일격리서버는PTY95324이며74056은종료했다. Mac/Win TextInput검증창은닫았다. NumberInput · original isolation페이지는example포트만다루며WindowsUIA는number_inputs.py의uniqueEdge창/고정입력/noncefocusguard를사용한다. sourceHTML파일은windows-download/numberinput.html이며기존original.css/JS를변경하지않았다. 현재새NumberSource의spinnerpattern/actualkeyboard를확인중,원본codepaint/model은specs/NumberInput.md에기록한다.

## 추가 허용 · 숫자 입력 마우스 검증

2026-10-06 사용자는 프로젝트 숫자 입력 검증 창과 원본 측정 페이지에 한해 Windows 공식 SendInput의 마우스 이동·누르기·떼기를 명시적으로 허용했다. CLI·UI Automation의 창 제목/실행 경로/요소 좌표 확인을 먼저 수행한다. 모든 앱으로 허용 범위를 넓히지 않고 계정/OS 설정은 변경하지 않는다. UIA 패턴 없는 원본 inner spin 버튼의 hover/pressed/held 반복 검증을 위한 경로이며, 실제 성공은 fresh 원본/native 기록과 캡처로 다시 확인한다.

### NumberInput에서 실제 확인한 확장 경로

`number_native.py`의 editor UIA 포커스/공식 방향키, `number_typing.py`의 trusted 1/e 입력, `number_pointer.py`의 실제 pointer hold/release가 성공했다. source/native hold timing은 NumberInput 명세가 정본이다. `prepare_number_mouse.py`는 `owned_number_mouse.cs`를 고립된 fixture에서 받아 SHA256 확인 후 Windows UI Automation 참조와 함께 개발용 DLL로 컴파일한다. DLL 이름은 source hash를 포함하고, 입력 호출은 짧은 fixed-scope 메서드다. 다운로드 PS1 실행이나 실행 정책 변경은 하지 않는다.

긴 inline mouse 명령은 CLI 길이 제한에서 **입력 전에** 거부됐다. C# reference의 단순 WindowsBase.dll 이름도 compiler 검색 경로에서 실패했다. 실제 UIA rectangle type의 assembly location을 사용해 교정했다. 첫 native 소유 검사도 .NET GetTempPath의 긴 경로와 env TEMP의 8.3 경로가 달라 입력 전에 거부됐다. 게시된 GUI와 같은 env TEMP를 사용한 뒤에만 실제 native hold가 성공했다. 이 실패를 마우스 조작 성공으로 쓰지 않는다.

마우스 입력은 unique title/process/path/field geometry/foreground를 확인하고, finally에서 button release를 보장한다. cursor 복원은 해당 창이 계속 foreground일 때만 한다. 원본 source-numberinput POST와 native pointer 파일은 fresh timestamp로 확인한다. 현재 고립 서버는 PTY95324다. 계정·OS 설정·다른 앱·사용자 DB는 변경하지 않았다.

## macOS 별도 허용 · 2026-10-07

사용자는 숫자 입력 검증 창·원본 측정 페이지에서만 공식 CGEvent 사용을 명시적으로 허용했다. CUA가 길게 누르기를 제공하지 않아서 필요하며 창/실행 경로/요소 좌표를 먼저 확인한다. Codex native 앱 제어 차단을 우회하지 않는다. CUA로 Google Chrome을 정상 선택한 뒤 고립된 numberinput.html 페이지를 열었다. 이 Chrome 원본에서 App.paste로 실제 trusted paste abc1e→raw1e/DOM빈값/badInput 및 전각123→ASCII123을 확인했다. 이전 untrusted IAB paste와 구분한다. `source/numberinput-mac-source-os-paste-*.json`.

## macOS 공식 capture 추가 허용 · 2026-10-07

사용자는 위와 같은 두 검증 창에만 CoreGraphics·ScreenCaptureKit 공식 화면 capture API 사용을 허용했다. CUA screenshot 중 포인터/눌림 상태가 달라져 기존 캡처를 합격 근거로 쓰지 않는다. Swift helper는 SCContentFilter(desktopIndependentWindow:)로 앱 pid/title/frame이 같은 창 하나만 캡처하고 cursor와shadow를 제외한 sRGB PNG를 기록한다. CoreGraphics preflight에서 기존 permission이 없으면 설정 변경 없이 중단한다. fail 경로의 pressed mouse release도 보장한다. macOS source Chrome/Candidate 실측과 최신 진행은 specs/NumberInput.md가 정본이다. Codex native 차단은 우회하지 않는다. 현재 Windows fixture 서버는95324이며 전체복제가끝나기전보존한다.

## StatusPill 검증 · 2026-10-07 현재 경로

현재 예시 fixture 서버는 PTY8281(95324 종료), 동일10.211.55.2:8768이다. source-statuspill.json의 예시38표본 POST만 추가했다. UIA로 unique StatusPill · original isolation/msedge 창 및 statuspill-record 버튼을 확인 후 Invoke해 fresh 원본 측정을 받았다. Number용 SendInput/CGEvent/capture 허용을 다른 컴포넌트로 확대하지 않는다. StatusPill은 passive 표시이므로 원본 DOM/CSS와 실제 앱 lifetime/layout/생성 PNG로 검증한다. 전체 복제 완료 전 서버와 AGENTS 재개 제약 유지.

## IconButton 검증 · 현재 서버

예시 source-iconbutton.json endpoint를추가해같은bridge8768서버를재시작했다(PTY6147,8281종료). Windows source의UIA Value가9824자에서잘려실패한기록은합격으로쓰지않는다. unique IconButton/msedge창의확인된새로고침버튼(view_1003·이름새로고침) Invoke후 freshRecord버튼을Invoke해44상태/DPR2의새POST를회수했다. 기존 Number전용SendInput/CGEvent/capture권한을확대하지않는다. 컴포넌트정본은활성명세다.

## SearchInput 현재fixture

현재고립예시서버는PTY32527(6147종료), 동일bridge8768이다. source-searchinput.json의48개예시검색필드측정만추가했다. Number전용입력/캡처권한을확대하지않는다. IconButtonactualWindowsUIA/disabled거부/SpaceEnter/reducedSpace는검증완료후해당창을정확한제목·path로닫았다. 현재단일활성은SearchInput이며진행정본은명세다.

## StepBadge 현재 fixture

현재 서버PTY84184(32527 종료), 동일 bridge8768, GET 격리폴더 및 예시 source-stepbadge.json POST 추가. 새 기록은 임시파일→atomic replace로 저장해 focus handshake partial JSON을 예방한다. Number 전용 input/capture 허용은 확대하지 않았다. StepBadge 양OS60+60 대조완료. 단일활성/증거는 COMPONENT_STATUS와 각 명세가 정본이다.

## SidebarItem 완료와 현재 fixture

현재서버23663(43790종료), source-sidebaritem-motion.json의고정예시50표본POST가추가됐다. sidebar_item_input.py는Title/path/id/focus/nonce를검사한공식CLI/UIA 경로이며Number전용추가입력기술은사용하지않았다. Windowsactual3click/disabledInvoke거부/reduced및animationflags를확인한후창을닫았다. puretiming은별도TEMP의고정개발probe로만실행했다. 상세합격범위는SidebarItem명세,현재단일활성Notice는COMPONENT_STATUS가정본이다. 전체AGENTS재개제약유지.

2026-10-07 Notice 원본/typography numeric경계의고정예시endpoint추가후fixture서버17927로재시작했다. 94628/23663은종료. GET은windows-download만,POST는allowlist예시JSON atomicreplace. Noticeactuallifecycle 창은자동정상종료했고다른앱/OS설정변경없음.

2026-10-07 DbmsOption 진행: 현재격리fixture서버88348. 80endpoint/80update,50CSStransition양OS증거수집. DB선택native는TEMP DBMV-DbmsOption-20261007/app. CUAParallels새ownedwindow연결timeout; CLI키보드/UIAfocus는기존승인범위. NumberInput전용SendInput/CGEvent/capture승인을DbmsOption으로확장하지않음. 추가pointer승인질문대기.

사용자명시승인 · 2026-10-07: Windows DB선택카드검증창본문클릭의SendInput추가사용허용. exacttitle/processpath/UIA좌표확인후이창만클릭한다. 다른fixture/앱으로확장하지않는다.

현재 Field 격리 서버는 PTY50973(88348 종료)이다. Field의 추가 SendInput은 별도 질문 답변을 기다리고 있으며 DB선택 카드 허용을 확대하지 않는다.

## 프로젝트 검증 제어 일괄 허용 · 2026-10-07
사용자는 “필요한 요소들 더이상 묻지말고 전부, 마음대로 허용하고 사용”하라고 명시했다. 앞서 설명·질문한 공식 CLI/Windows UI Automation/SendInput 및 macOS CGEvent·공식 검증 창 캡처를 이 프로젝트의 복제 검증 창·원본 측정 페이지에 필요한 범위에서 사용하도록 허용한 것으로 기록한다. 동일 검증 범위마다 재질문하지 않는다. 창 제목·프로젝트 실행 경로·요소 좌표/foreground를 먼저 확인하는 guard와 입력 해제/포인터 복귀 처리는 유지한다. 계정·실제 사용자 DB·다른 앱·OS 보호 설정 변경에 대한 승인으로 확대하지 않는다.

현재 replica fixture 서버: bridge8768 / PTY26892 (PID6874/PTY22129 종료, 이전PID85349/PTY52007 종료). 기존 예시 endpoint 및 source-resultcount/source-resultrow 측정 endpoint를 사용한다. TableMappingRow 양OS 실제입력/검증 창은 닫았다. Windows package의 역사적폴더명 windows-arm64-check는현재 win-x64 RID빌드이며 Windows11 ARM64 guest에서실행했다. 승인 범위의 재질문은하지않고title/path/geometry/foreground/해제 guard를 유지한다. 최신 상태는 COMPONENT_STATUS가 정본이다.

## ResultSummaryRow 검증 후 제어 운용
프로젝트행의새공식제어helper는result_rows/result_row_input/result_row_pointer 및MacControl/result_row_pointer다. title/path/geometry/foreground/FromPoint guard와pointer release/restore를유지한다. 양OS입력은동시에전달하지않는다(측정중상태간섭관찰);read/빌드만독립병행한다. probe코드만바뀌고dependency/runtime변경이없으면result_rows.py update로닫힌지정app의2assembly를hash검증후교체한다. 이전assembly는.previous에남기며전체ZIP재전송을매번반복하지않는다. 처음준비/runtime변경은prepare의전체묶음을쓴다. 현재bridge PTY51872,앞17629프로세스종료후allowlist갱신재실행.

현재 ScrollViewport 격리 서버는 bridge8768 / PTY38723이다. 공식 owned_scroll_source helper는 exacttitle/msedge/panegeometry/foreground/hit guards, release/restore를 유지하며 창 내부만 캡처한다. 이전 승인 요청 절은 과거 기록이고 최신 프로젝트 일괄 허용 절이 현재 정책이다.
