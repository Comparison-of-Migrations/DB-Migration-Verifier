# InfoDialog

LOCKED · root 검증 완료. 원본 연구/권한 모달을 복제한다. 잠긴 InfoDialogBody/Surface/Typography/IconButton/ActionButton/ScrollViewport/Motion을 조립하며 보호 코드를 수정하지 않는다.

## 실제 원본 조사

2026-10-08 own hidden 원본50에서 연구 반영 보기 및 연결→사전 확인→필요한 권한 보기를 실제 열었다. 사용자 탭은 변경하지 않았다. infodialog-live-{research,permissions}.json/screenshots가 현재 실행 근거다. DOM-only getAnimations 미지원 호출은 실패로 기록하며 computed animation과 원본 JS/CSS를 확인했다. source original.js openModal('research'/'permissions')와 closeModal, CSS 최종 dialog/head/body/footer 규칙을 따른다.

두 모달 공통: overlay → 중앙 dialog → header h2/close → body(잠긴 InfoDialogBody) → footer close. 연구1280×720는620×492.59375, 권한은620×368.390625. header85, footer81, body는각324.59375/200.390625. title18/27/700/-0.27, close44×44. dialog border1 #535963/radius14/#2b2d31/shadow0 18 48 black0.14/max-height viewport−48. body padding24/max65vh와 outer overflow는 독립. Header padding20 24/gap20, footer padding18 24/gap12/background#232428. 본문 반응형 Summary compact는 원본viewport≤1100 기준이며 dialog폭기준이 아니다.

## 상태·입력 계약

showModal은 header close에 초기포커스, pointer진입에서는 focus-visible없음. 원본 dialog modal-in280ms/ease, close JS160ms/ease-out/reduced즉시. head/footer/Escape로 닫으며 중복닫기차단, backdrop click은유지. open마다 footer와본문분기 재구성, body/outer scroll0. background blur2+inert, close후 callerfocus({preventScroll:true}); disconnectedcaller 재탐색은 Application 책임이다. 언어변경 재생성/전체페이지 focus/AXisolation은 마지막 Application 통합에서 추가 확인한다. 컴포넌트는 자신이 공급받은 opener/background의 복원을 검증한다.

## 계획·다음 행동

각언어×viewport960/1280×height400/600/720/960×research/permissions=32 실제원본literal조건을 고유endpoint로 기록한다. title/head/body/footer/본문위치/문구/색상/scrollHeight/center를 비교하고 Window의 기본root scrollbar offset은 measured client차이근거만 허용한다. Headless independent realSkia/scale2에서 geometry·input·open/close/duplicate/reduced·language/detach/remount 검증 후 HEADLESS_PASSED. 실제양OS pointer/key/AXfocus/scroll/close복귀/animation 확인 뒤만 LOCK. 현재 후보 및 Headless·실제 기하 검증을 완료했으며 실제 입력 잔여 범위는 아래 기록을 따른다.

## 복구

AGENTS→MASTER→COMPONENT_STATUS/PARALLEL/HEADLESS/rootworker→본문 및 직접 하위명세→실제원본 DOM/CSS/JS→후보→차이 순서로읽는다. 전체원본 재분석/설계초기화 없음. 단위완료뒤 자동 다음 Progress로 진행한다.

## 후보·Headless검증
실제원본32 수집 후 보호된 ResultDetails의 동일한 source dialog wrapper계약을 참고하되 새 WebInfoDialog 파일로만 조립했다. 보호본문body상태분기, viewport≤1100 compact, header/footer/Surface/scroll/focus/motion 계약 사용. 독립 snapshot7e8cae4c70974e0285beb4c90414121c/build0/0, run802cc580d6c14643b9880f5326c730ed 실제Skia/DPR2. 원본Mac32 기하/본문/색상/scrollHeight strict0. finite8 rawpointer/backdrop/keyTab/Escape/언어종류/detach/remount/restoration assertions와 normal/reduced open/close6 확인, PNG 직접검토. HEADLESS_PASSED 해당범위, componentINPROGRESS. 원본32fixture는programmatic openerfocus→headerfocus 조건이며 nativePNG의headeroutline도그조건; 실제pointer진입원본livePNG와동일상태라고주장하지않음. Actual양OS/shortheightscroll·focus·복귀·pointerclose검증남음.

## 실제 OS 진행 · 압축 복구 확인
2026-10-08 AGENTS/MASTER/상태/worker/본명세/직접 source·native 실제읽기 후 기록된 단계에서 재개했다. 실제Mac32 및 Windows32 원본 기하·본문·색상 strict0, normal/reduced 수명6 및 고유 PNG. Mac actual-macos-input에서 pointer초기focus-visiblefalse, Tabfootertrue, Escape복원, 짧은창End완료 body150/outer0→Tabfooter body150/outer76, header·footer닫기/background복원/reduced 실제입력을 확인했다. End직후54.5는 애니메이션 진행값이며 입력을 반복하지 않고 완료150을 새로 기록했다. 최초 backdropJSON은 automaticclose 후 덮여 stale로 보존; actual AX 열림 증거 및 reduced조건 freshbackdropJSON을 별도 보존했다. 창종료후 getAX의 재실행을 PID로 발견하고 다시 종료·exactprocess0 확인. Windows input/focus/scroll은 아직 미검증, LOCK하지 않는다. 각OS 글꼴 raster와 창장식의 기본경계만 OS표현차이이며 원본 기하·동작 차이 면제 없음.

Windows actual 기하32는 build03bd88…/actual-windows/compare.json의 strict0와 PNG 직접검토로 확인했다. 실제양OS normal/reduced 수명6: open opacity0→1, close중visible 유지·closingtrue→close1, 중복 close는 event1, reduced open opacity1/close 즉시 event2. 현재 제품 hash b8770fea5f54d8907587d0bac7a3293a95eed69b77ee6a477b7a8cfbcfa6b894로 양OS frozen snapshot이 동일하다. Windows 실제 입력만 잔여다. 입력 helper info_dialog_key/pointer/source_input은 고유runtime·title·path·resource·focus·nonce·press-release/freshrecord guard를 사용하며 다른 담당자 OS grant 동안 실행하지 않았다.

남은 Windows 입력 검증은 동일 product frozen03bd88…를 그대로 사용한다. tools/WindowsControl/info_dialog_verify_windows.py의 native/source/compare 세 고정 단계: pointeropen→Tabfooter→Escape복원→짧은창 bodyTab/End/footerscroll→pointerfooterclose→headerclose 및 native reduced. 빌드/실행/측정 출력을 담당·component·UUID/actual-windows/{input,source-input}로 분리한다. 사용한 stage/runtime 해시와 사례는 build.json/windows-stage.json으로 추적한다. 후보와 원본의 visible/focus/scroll/focus-visible를 엄격 대조하며 실패 파일을 보존한다. 원본 singleiframe 진단과 전체Application focus/inert 노출 경계는 상위 통합 검증에서 별도 확인하며 여기서 전체앱 완료를 주장하지 않는다.

추가 actualMac 포커스 순환: own실행원본54에서 pointerheader→Tabfooter→Tab은 browserhost로포커스전달(document.activeElement=BODY, app배경컨트롤포커스없음)→다음Tabheader. source/infodialog-mac-source-cycle.json 보존. 같은frozen native는 footer→Tabheader 및 header→ShiftTabfooter 확인(actual-macos-input/cycle-{forward,backward}.json). 앱 내부 두컨트롤 순서는 동일하고 브라우저host의 별도정류점은 native앱의컨트롤로 만들지 않는다. 원본54와native창닫음·exactprocess0. Windows 첫 normalpointer/Tab통과 후 Escape는실제닫힘/복원됐으나 recorder의 CLI Invalidargument가 반환됐다. 키를 다시보내지 않고 guardedfreshread escape-restore.json으로 visiblefalse/backgroundtrue/openerfocus 확인한 뒤 native-rest의읽기전제검사로짧은창검증부터재개한다. 실패는통과 입력으로재표기하지 않고 전달된Escape와진단recorder실패를 구분한다.

Windows 진단 교정: Edge 캐시로 이전 fixture가 열린 실패를 버전 hash query로 구분하고 새 capturedAtUtc/자동 POST 수집을 확인했다. 실제 source event history에 기존 CLI `--key 105`가 End 대신 PageDown으로 전달된 것을 발견하여 이전 small-end 증거를 small-pagedown-before-key-correction으로 보존했다. 실제 End는 공식 SendInput VK_END로 별도 검사한다. 키 helper는 한 guest 호출에서 exactwindow/path/foreground 검증·press/release·650ms 안정화를 수행하여 nonce/분리 key-call 대기 비용을 줄인다. 포인터 선택은 브라우저 창장식 Close와 문서 Close를 구분하며, 버튼을 미리 UIA focus하는 방식 대신 창만 활성화한 뒤 실제 pointer를 전달한다. 제품 구현은 변경하지 않았다. 영향 범위는 실제 Windows entryfocus/End 및 미완료 sourceclose이며 기존 기하/headless/Mac 통과범위는 유지한다.

## 완료 판정 · 2026-10-08
동일 제품 b8770fea…의 Mac7e8/Win03bd frozen을 검증했다. Headless32/finite8/lifecycle6, 실제 각OS32/lifecycle6/paint, Mac actual pointer/key/scroll/복원 및 Windows source/native 입력8라벨 strict0(input-comparison.json). Windows 실제 VK_END history와 body172/outer76를 확인했다. Source Record와 native Record UIA Invoke는 포커스 표시를 바꾸므로 초기 pointer 증거는 실제 open callback의 pointer-open-event.json을 사용한다. callback captureUtc가 해당 실제 pointer 실행 중 갱신됐고 reduced open opacity1/headerfocus/focusVisiblefalse이며 원본 initialpointer와 일치한다. 기존 tainted 기록은 별도 보존했다. 자동 source POST는 pointer후650ms 안정화한 고정 라벨을 사용하여 이전 focusin 기록을 채택하지 않는다. 내부 closing guard는 숨긴 이후 nextOpen까지 유지되지만 UI/입력은 숨김·복원 완료; visible중 motion 및 duplicate guard를 검증했다. 전역 Application inert AX/미연결 opener/언어 재생성 정책은 부모 범위로 남긴다. 부모 전달 background와 opener 복원은 본 범위에서 양OS 완료. 직접 하위 변경 없음, LOCK hash 보존. Exact Win source/native cleanup0, Mac exactprocess0이며 OS 자원 해제/후속 담당 배정. 다음 Root는 Progress의 미잠금 작은 내부 컴포넌트부터 조사한다.
