# WelcomePaper · LOCKED

2026-10-08 WelcomePaperContent LOCK 이후 `.paper` 부모조립. 직접자식 LOCKED SvgIcon(file22)/WelcomePaperContent(small/h2/p)/ActionButton(quiet research). 원본 home()의 실제최종CSS GRID를 사용하며이전 flex 구현으로추측하지않는다.

## 코드 및 실행 준비

final .paper displaygrid/columns22px minmax(0,1fr) auto/align-itemscenter, gap14. @media(max-width1100) columns22px minmax(0,1fr), button grid-column2/justify-selfstart; 마지막CSS marginleft0이이전36px값을덮는다. 상단padding26/하단22/bordertop1#41434a/margintop30. 실제computed/좌표/색확인 전 값·배치 구현확정없음. Parent icon decorative; research button은한영단일요청을 Root InfoDialog handler에 전달한다. 모달내용과포커스수명은RootApplication책임.

prepare_welcome_paper.py는 exactoriginalhome().paper와 originalCSS전체를 각iframe에보존. KO/EN×actualviewport960/1100/1101/1180/1280/1440=12 independent source. Home현재Page폭조건(min1060, viewport-rail224-양쪽padding)을fixtureinput으로적용하며Paper자체margin/padding/border를제거하지않는다. iframeactualmedia조건을사용한다. Parenthost는absleft30/top60으로isolated, root는host-relative로기록. uniqueowner/component/OS endpoint준비후 hiddenDOMsource12→명세→구현/headless→actual양OS layout/lifecycle/actualresearchrequest→LOCK. 보호child수정없음.

## 직접 실행 source12 · 2026-10-08

Mac hiddenownIAB12 freshlyrecorded, compact960とwide1101を実DOM/paintで直接確認、成功tab9closed。最終grid正しくcompact960: root680×199.578125/host-relativey30, contentx36/y27/w644/h92.578125、icon22/y62.2890625、buttonx36/y133.578125/108.2265625×44。ENwide1101: root821×160.7734375/y30、contentx36/y27/w619.6640625/h111.7734375、buttonx669.6640625/y60.8828125/w151.3359375/h44、icony71.8828125。Buttonfont13/quiet600、iconstroke1.7/muted、border1/paddingtop26/bottom22/col-rowgap14。Wideはrow内centerをCSS64分数単位にfloorする。ソースrow測定値をnative入力には使わず、availablewidth/ViewportWidth/Englishとnativechildnaturalmeasureを入力に計算する。原本12には本来viewportmedia1100/1101が適用されるdistinctiframeURLを使用。CSSvisibilityhiddenframe read timeout1は採用せず、fixtureUIで選択して可視状態で読み直し済。

## 再開/実装 · 2026-10-08
AGENTS→MASTER→COMPONENT_STATUS→PARALLEL→worker→HEADLESS→resourceJSON→本spec/directsource/child/nativeを実読、verify_lock全成功。WebWelcomePaperはprotectedSurface1pxrule/Icon22/Content/quietButtonを組立て。ViewportWidth<=1100 compact、contentwidth=available-36、wide=available-50-nativebuttonnaturalwidth。rowmaxnaturalheightとCSSfloorcenterで配置、MarginTop30/paddingtop26+border1/bottom22/gap14。ResearchRequestedを単一clickforward、原本文字KO研究反映見る/ENResearch mapping exact。最初headlessgeometry全0、probeがButton外Foreground黒を誤読15、実表示childtextを読むprobe修正。product変更無し。2b02...再比較、actualOS未完。

## HEADLESS_PASSED
Frozen build [2b02 manifest](../../../artifacts/replica/workers/home/welcomepaper/2b02a77dc1e64419aea1bce4a7bab4aa/build.json), [run3cb83](../../../artifacts/replica/workers/home/welcomepaper/2b02a77dc1e64419aea1bce4a7bab4aa/outputs/3cb83ca3485e41cb92036f028d511112/environment.json). 独立realSkia/DPR2/source12 strict0、attached KOcompact→ENwide→KOcompact lifecycle3全rowstrict0、rawpointer/Enter/Spaceそれぞれ単一callback counts1/2/3 focus=true。原解像度PNGと原本compact/wide視認。比較正本 [headless comparison](../source/welcomepaper-headless-comparison.json)。OS未完、LOCKではない。両OS枠依頼。

Actual validation 준비는 코드/빌드와 분리했다. Mac immutable 전체 bundle을 own build 폴더에 복사(실행 전 hash/closed-process/resource owner 검사), guarded Swift AX/CGEvent helper compile 완료. Windows frozen selfcontained `1d460855d25c47f7ab0f3f53cce27d6b` entire ZIP hash/deps/coreclr pack 완료, 실제 stage/input은 resource grant 뒤 수행. 독립 file 준비는 OS 입력/포커스를 변경하지 않았다.

## 실제 macOS 완료
같은 frozen2b02 전체 runtime의 actual12/lifecycle3 strict0: [comparison](../source/welcomepaper-mac-comparison.json), [environment](../../../artifacts/replica/workers/home/welcomepaper/2b02a77dc1e64419aea1bce4a7bab4aa/actual-macos/environment.json). 한영 compact960/wide1101 실제 browser 입력과 AX/CGEvent native pointer/Enter/Space 각각 단일 요청 및 focus=true, [input6 comparison](../source/welcomepaper-mac-input-comparison.json). 원본 iframe별 counter1/2/3, native 같은instance누적1..6를 delta로 비교했다. Source는 exact home().paper/press()를 유지하되 research parent 요청만 counter에 기록; 실제 openModal(research)는 root통합 범위다. Primitive button press/focus animation은 기존 LOCK계약, 부모에는 새 animation 없음. Native input fixture는 ReducedMotion=true에서 callback/배치/포커스 검증; source는 settled activation을 사용한다.

actualMacPNG wide/compact 및 source input screenshot을 원해상도 확인했다. 본문의 줄바꿈/타이틀/아이콘/버튼은 source와 같고 기본OS text renderer AA 차이는 MASTER OS표현 경계이며 기하 면제 없음. 독립 Python Popen input 앱은 세션 종료로 사라져 initial bundle guard 실패, 입력 전송 없었다. 정상 bundle launch(open) 뒤 exact bundle/title/foreground/rect/AXhit guards로 재검증했다. own tab10 closed, AX close 및 exactpathprocess0 [cleanup](../source/welcomepaper-mac-cleanup.json), root Mac반납 통보. Windows 미완이므로 LOCK 아님.

## 새 검증 우려 · 완료 전 passive click
CapabilityHeader 실제 원본/교정에서 passive 전체 영역이 이전 button focus를 해제해야 한다는 동작 확인. Paper에도 research button과 passive text/padding이 함께 있으므로 기존 activation6 증거가 이 범위를 포함한다고 주장하지 않는다. 제품변경 전 별도 finite headless body/gap raw click 측정과 새 원본 실제 focus 관찰을 요청했다. 원본/후보 확인 뒤 차이가 있으면 그 영향만 수정·검증한다. 기존 통과12/lifecycle/activation6 불필요한 반복 없이 보존.

有限headless passive新scope [195dd/run14b2](../../../artifacts/replica/workers/home/welcomepaper/195dd001d61d4811b7d239b99ce92002/outputs/14b2e071d6d04651947ec98f1662be4c/candidate/welcomepaper-passive.json) は unchanged productでbody/gap両方focusResearch=trueを記録。原本新観察待ち、完了ではない。

## 제품 교정 전 원본·영향 기록
Fresh ownIAB tab11 actual source: research focus 후 body click 및 paper center의 제목/본문 사이 실제 gap(DIV hit, titleBottom186.3828125 < pointY189.7890625 < bodyTop190.3828125)이 모두 focus=false, requests unchanged로 기록 [body](../source/welcomepaper-mac-input/source-passive-body.json)/[gap](../source/welcomepaper-mac-input/source-passive-gap.json). Native finite oldproduct는 body와 top padding gap 모두 research focus=true. 영향은 WelcomePaper 부모의 passivehit/focus 정책이며 보호 Content/Icon/Button 코드에는 없다. 교정 필요: Paper BackgroundTransparent로 실제빈 영역도hit target을 제공하고, PointerPressed tunnel에서 ResearchButton 내부를 제외한 passiveleftclick만 Focus(null,Pointer). 기존buttonactivation·palette·geometry·children는 그대로이며 새parent에만 적용. 수정 뒤 finitepassive와 activations, actual양OS affectedinput, geometry/paint unchanged 확인 후 LOCK.

## Passive 교정 검증
제품 부모만 교정한 frozenMac6825, finitepassive run7019에서 body/gap 모두 focusResearch=false/FocusManager null로 source와 일치. Geometry+palette/lifecycle/activation headless run8536 source12strict0/lifecycle3strict0, pointerEnterSpace singlecallbacks1..3도 유지. 실제 같은correctedbinary Mac12/lifecycle3 strict0, guarded activationcompact/wide6+passivebody/gap2 모두 통과 [input8 summary](../source/welcomepaper-mac-input-corrected/summary.json). 각passive는 button AX focus 후 클릭해 prior focus가 존재하며 requests6 unchanged/focusfalse를 확인. [corrected cleanup](../source/welcomepaper-mac-corrected-cleanup.json): tab11closed/exactnativepath0, 원래와 corrected actual KOcompact/ENwide PNG bytehash 동일로 시각 변화 없음. Windows frozen36f86 전체runtime packed, 아직 OS실행 전이다.

## Windows 독립 준비 · 사용자 속도 개선 지시
최종36f86 cold entire stage를 root가 추가한 `stage --headless-only`로 actual lease 없이 hash/coreclr/HeadlessDLL/closedowned guards 아래 완료. native 창/포인터/키/포커스 조작 없음. 새 Windows 조건만 독립 realSkia headless UUID69a973...로 record했다: source12 비교는 actual 원본Windows수집 전이므로 `WINDOWS_HEADLESS_RECORDED`; rawactivation3 singlecallback/focus 및 passive2 focusnull은 통과. [environment](../../../artifacts/replica/workers/home/welcomepaper/36f86fad33d2407fb87d7ef2ec58019a/windows-headless/69a973f496e14d92b29bf667d62ebf9b/environment.json). 기존Mac기통과검사반복없음. 남은Win actualsource12/native12/input을한resource slice에묶어진행.

## Windows 재개 · 새 입력 관찰
규칙/worker/spec/directoriginalPaperCSS·home/native 실제 읽기 및 verify_lock 성공. Fresh Windows12와 final36f86 actual12/lifecycle3 strict0, 동일Windowsheadless12/lifecycle3도 strict0 확인. Source compact/wide activation6 single 요청·focus 유지. Source passive driver는 본문 HTML id가 Edge UIA에 노출되지 않아 클릭 전에 guard가 차단했다. 실제 source UIA Text Name은 exact EN 본문, rect144,607,1162,72; driver의 source-only fallback을 exactName+TextControl로 보완하며 title/path/geometry/foreground/hit/해제 guard 유지. 제품·fixture 변경 없음; 이미통과한 기하/activation 재실행 없음.

## 최종 완료 판정 · LOCK
최종 코드 frozenMac6825 / Windows36f86, HEADLESS_PASSED와 actual양OS를 각각 확인. 한영×viewport960/1100/1101/1180/1280/1440의 12조건 모두 각OS fresh 원본과 엄격 tolerance.001 차이0; fullrow attached3수명도 차이0. Windows 실제 기본SegoeUI와 Mac 기본SystemFont의 naturalwrap/buttonwidth는 각OS 원본과 같아 강제폰트/기하 보정 없음. Win EN1180 높이141.578125와 Mac160.7734375의 차이는 각OS 원본도 동일하게 가지는 기본font경계. 기하/색/텍스트/상태 면제 없음.

최종 Windows [원본12](../source/welcomepaper-windows-source.json), [actual 비교](../source/welcomepaper-windows-comparison.json), [headless 비교](../source/welcomepaper-windows-headless-comparison.json), [actual run](../source/welcomepaper-windows-run.json), [실제입력8](../source/welcomepaper-windows-input/summary.json). KOcompact/ENwide 각pointer/Enter/Space singlecallback/focus 유지6, passivebody/gap은 이전 researchfocus 이후 requests unchanged/focusfalse2. 기존 source periframe counter1..3와 native instance누적1..6는 단일요청 delta로 비교. 원본 actual openModal(research)는 root Application의 기능/포커스통합 범위이며 이 부모는 ResearchRequested만 전달한다.

[Source Windows 화면](../source/welcomepaper-windows-input/source-frame.png)/[native 화면](../source/welcomepaper-windows-input/native-frame.png)을 원해상도 직접확인: 같은 title/bodywrap/line/icon/quietbutton/focusring. 독립official capture CLI의 source 긴argument session실패2회는 입력변경 없이 닫고 복구했다. driver 명령을 짧게 구성하여 같은 scopedUIA/title/path/foreground/bounds guards 아래 성공; capture-only original새창이며 기존input 증거를 반복/덮지 않음. 실제KO/EN renderPNG도 native source경계 비교로 확인. OS 기본raster AA만 허용.

[Windows cleanup](../source/welcomepaper-windows-cleanup.json) exactunique apppath nativeProcess0/nativeWindow0/sourceWindow0, Mac corrected cleanup0 확인. 보호child Typography/Surface/Icon/ActionButton/Content 코드 변경 없음. 부모 margin30/rule1/padding26+22/gap14/viewportmedia1100, 내용측정/center정렬/언어/수명/실제입력/paint에 남은 구현차이 없음. 최종 보호는 WebWelcomePaper 전체파일이며 원본 변경 또는 부모 요구로 변경하려면 이유·영향·source근거 기록 후 영향범위 재검증한다.
