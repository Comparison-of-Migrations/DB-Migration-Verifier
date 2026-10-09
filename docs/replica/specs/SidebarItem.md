# SidebarItem

## 상태·범위

LOCKED · 2026-10-07. 직접 하위 StepBadge/Typography/Surface/Motion이 LOCKED다. 원본 .step은 .btn과 CSS/자식구조가 다르므로 sealed WebActionButton의 private label/template을 임의변경하지 않는다. 실행 원본을 조사해 기존 ActionButton에서 어떤 계약을 재사용하고 어떤 부모 override가 필요한지 기록한 후 후보를 구현한다.

## 원본 위치

`examples/ui-prototype/index.html`: topAndRail(), .step/.step-index 상태. 작업 직전 실행 웹의 해당 부분을 다시 조사한다. [원본 manifest](../source/manifest.json).

## 기존 Avalonia 위치

`src/Verifier.Desktop/`: MainWindow.Shell.cs Render, App.axaml. 기존 코드를 원본처럼 사용하지 않는다.

## 명세·검증

[_TEMPLATE](_TEMPLATE.md)의 DOM/CSS/geometry/spacing/type/상태/interaction/부모 관계/대응 구조/증거/차이를 착수 시 채운다. 값은 현재 미확인이다. LOCKED로 판정하지 않는다.

## 다음 행동·재개·LOCK 변경

다음 행동: 실제 .step의 normal/active/done/disabled/hover/focus/press와 KO/EN 줄바꿈/부모폭을 조사해 기록한다. 아직 후보 구현/LOCK 변경 없음.

## 원본 조사 · 2026-10-07

실행 Selection의 KO/EN `.step` computed/box와 CSS/JS를 다시 확인했다. 실제 rail item 현재폭192, minheight52/border1transparent/r8/padding12vertical14horizontal/gap12/flex aligncenter. label폰트13/inherited1.6 linebox20.796875/normal400,active650. childbadge24/local15,14와 labelx51/y15.6015625(한줄). 실제labels KO DB연결/사전확인/검사선택/실행전확인/검사진행/결과확인/결과저장, EN Connect DBs/Preflight/Choose checks/Review/Run checks/Results/Save results. source/sidebaritem-live-selection-en.json.

normal bg투명/color#cdd0d6;disabled bg투명/color#b5bac1. hover-enabled bg#35373c/boxshadow none. active bg#3c3f4b/color#f2f3f5/weight650/inset3px0#8b94ff; hover활성에서더높은selector `.step:hover:not(:disabled)`가 background#35373c 및shadow none으로덮는다. active-disabled는active색과inset유지. activefocusoutline#bbc1ff,otherwise#a7b1ff/3px/offset3. buttonactive scale.98/80ms. click은source press160 재생 후 즉시stage이동,disabled실행금지. sourceclass `.step-index`는독립focus없음. visited/current/running 잠금 및 aria-current는 부모가공급한다.

WebActionButton은 `.btn` 전용 sealed/private template이고 source `.step` badge+label 및 flex-left/cascade와 달라 그대로 조립할 수 없다. 잠긴ActionButton을새스타일로재해석하거나 private를우회하지 않는다. 별도 WebSidebarItem Button을 구성해 잠긴 StepBadge/Typography/Surface/Motion을 직접 조립하고, ActionButton에서 이미 증명한 source button의입력/press/transition 규칙을 .step CSS에 맞춰 검증한다. 이는 COMPONENT_TREE의논리dependency를실제조립경계로좁힌것이다. locked코드변경은StepBadge상속색예외만이며그재검증이끝난후시작한다.

## 조립 명세 확정

source/sidebaritem-mac-source.json의132endpoint를 기록했다: 각7label KO/EN ×128/192/260px ×normal/active(84), KO/EN normal/active/done ×enabled/disabled ×hover/pressed/focus/idle(48). 폭128 EN Connect DBs는 label62×41.59375/button128×67.59375/badge local15/21.796875로두줄. span label의intrinsic폭이충분하면content폭만차지한다. Badge24+gap12/label사용가능폭=width-66, source flex min-content 경계는이표본에서넘지않는다. endpoint fixture의pseudo class는원본selector를동일순서/동일specificity로복사하며 endpoint의transition만0으로고정한다. timing및actual입력은별도검증하고endpoint수만으로LOCK하지않는다.

후보는 Button → focus outline → LOCKED Surface(border1/padding14,12/r8) → left flex panel → LOCKED StepBadge24 + gap12 + LOCKED Typography13 left/wordwrap. 원본CSSgrid/flex LayoutUnit 정렬 및 unitlesslineheight를 사용한다. .step별bg/color/shadow를기록한cascade로해석하고 sourcebutton의120ms색/배경/shadow,80msscale 및WebMotion.Press160/reduced 계약을 적용한다. SidebarItem의stage이동/visited/current 판단은부모책임으로남긴다. 후보구현후지금명세/원본132와수치를대조하며상위Sidebar는수정하지않는다.

## 현재 검증 · 2026-10-07

양OS132endpoint layout/paint/name/current-status 비교 통과(source/sidebaritem-{mac,windows}-comparison.json). 원본의pressed DOMRect는transform 후 큰fixture y좌표를float32로 반환해inverse계산오차가최대0.0009567px였다. 제품spacing을변경하지 않고 비교기는각행absolute좌표의float32 ULP2개/scale로오차경계를계산한다. 비pressed box는기존1e-8,Typography advance는LOCK1/64 계약을유지한다. 수치경계를시각적대충일치로바꾸지않았다.

첫probe는상위surface와badge하위surface를함께Single조회해측정전에중단됐다. probe를정확한소유_surface로좁힌뒤에만132결과를회수했다. 실패를통과로쓰지않는다. macOS actual4click에서 Space/Enter·disabledTabskip·activeMotionCompleted·reducedMotion없음 확인. source/sidebaritem-mac-actual/sidebaritem-input.json와screenshots/sidebaritem-mac-actual.png. source 실제원본의Space는사전확인으로,Enter는검사선택으로이동했다(screenshots/sidebaritem-source-keyboard.png). Mac창은닫았고닫힌handle은재선택하지않는다.

현재Windows actual검증창과색/배경/그림자/scale 중간시간 대조가남아미LOCK이다. MainWindow/상위Sidebar는미수정. 서버43790(84184종료)에서예시source-sidebaritem.json만추가했으며Windows/macos공식Number전용capture/input범위는확대하지않았다.

## 검증 확대와 현재 다음 행동

실제 WindowsSpace1/activeEnter2/reducedSpace3 및 disabledInvoke거부·count1불변을 확인했다(source/sidebaritem-windows-actual.json/disabled.json). 일반/현재단계 click의MotionPending true, reduced false 및끝scale1을확인했다. 첫Windows키attempt는guestsession생성실패로handshake이전에중단돼키를보내지않았고read-only현재count0확인후에만재시도했다. native검증창은exactTitle/path WindowPattern으로닫았다.

양OS CSS source50중간시점과 실제private transition algorithm/injectedclock50대조는통과했다. normalhoverBG/activehoverinsetshadow/disabledcolor(배지상속)/pressedscale/hover30ms후reverse shortenedDuration91.783766287ms 포함. source/native timing/comparison JSON을링크한다. sourceCSSserializedRGB정수/alpha3decimal과candidate8bit는명시적rounding허용(color.51byte,alpha1byte/scale1e-6/shadowX1e-5/duration1e-6)으로대조하며픽셀대충유사기준이아니다. native 12focusendpoint PNG의outline중심pixel은원본computedcolor와정확히같았다(sidebaritem-{mac,windows}-focus-paint.json). disabledfocus표본은CSS endpoint검증이며실제disabled가focus된다는주장이아니다.

같은붙은객체에서Content/Index/State/Width/IsEnabled를132번갱신하는검증을추가했다. mac132update통과; Windows132update재측정만현재남았다. 제품은바꾸지않고probe만확장했다. 그결과까지확인한뒤LOCK한다. 현재sourcefixture서버23663(43790종료), sidebaritem-motion의예시50표본POST만추가했다. 잠긴14파일범위는보호중이고전체재개제약유지.

## 최종 판정 · SidebarItem LOCK · 2026-10-07

Windows132same-object update까지대조통과했다(132endpoint+132update/양OS). 제품파일의마지막변경은source aria-current=step을Automation ItemStatus=step으로대응한것이며이후probe만확장했다. layout/paint/name/status·sourceCSS timing50/양OS·exact focuscolor pixel12/양OS·sourceSpaceEnter navigation·nativeactual Mac4/Win3 클릭 및disabled/reduced/animation완료 증거는 위 JSON/화면에 연결한다. 기존14LOCK 범위는변경되지않았다. 잠긴StepBadge inherited색예외는이미자체양OS78+78재검증후재LOCK했다.

본컴포넌트에독립적인Pointer handler를추가하지않고Avalonia Button의입력경로를사용한다. hover/pressed/focus는endpoint CSS와동일코드Resolve/Paint/실제키입력으로확인했다. 물리적pointer hold는이단위에서별도로촬영하지않았으며Number전용CGEvent/SendInput/capture범위를확대하지않았다. 실제global stage/visited/running 판단·parentreplace/focusheading·전체Sidebar배치는상위의미완료영역이다. 이를현재버튼의완료로대신하지않는다. 현재macOS/Windows실제DPR2/원본KOEN/128·192·260폭범위에서알려진컴포넌트차이없음.

source/sidebaritem-lock.json으로WebSidebarItem.cs를보호한다. 기존 MainWindow.Shell.cs의sidebar helper는원본과다르므로상위Sidebar조립전까지유지하고그것을새LOCK성공이라고주장하지않는다. 다음단일활성Notice. 전체복제AGENTS재개제약유지.
