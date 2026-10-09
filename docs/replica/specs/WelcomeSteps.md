# WelcomeSteps · LOCKED

2026-10-08 WelcomeStep atomic LOCK 이후 활성 부모 group. 원본 home().welcome>.three-actions, 직접 자식 article3는 LOCKED WebWelcomeStep로 조립한다. 최초 originalCSS에는 margin48/border/padding이 있지만 최종 .welcome .three-actions가 border0/margin-top24/padding0을 적용하고 마지막 CSS가 auto3rows/rowgap8/subgrid를 적용한다. 열은 repeat(3,1fr)/column-gap32이며 media로 단일열 전환은 없다.

## 원본 준비

prepare_welcome_steps.py: literal home() 그룹을 원본CSS 전체와 함께 고유 welcomesteps fixture에 보존, KO/EN×width684/862/980/1120=8. 원본 margin-top24를 보존하며 fixture host의 padding은 isolation0. group root를 host에 대해, article roots를 group에 대해 측정하고 내부 text는 atomic local bounds도 기록한다. 새로운 고유 bridge home-welcomesteps-OS-source.json. 실제 source8/parentcomputed/paint 재확인 전 구현 없음.

## 남은 검증

양OS 실행 원본8, 조립 명세→구현→HEADLESS_PASSED→실제Mac/Windows8/lifecycle/paint→LOCK. 부모 Home 자체 진입animation은 group에 중복추가하지 않는다. 입력 없는 안내group이므로 childtext update/폭변경/remount가 실제 적용상태다.

## 직접 원본 조사 완료 · 2026-10-08

root 승인된 hidden IAB DOM8을 독립 수집했다. source/welcomesteps-mac-source.json: host-relative root x0/y24, width684/862/980/1120; column gap32/rowgap8/border0/padding0/margin24. actual columns684는206.6640625/206.6640625/206.671875, 980는305.328125/305.3359375/305.3359375로 cumulative CSS64분수 분배. root height는 native-independent natural3row max합+16이며 한영 wrap에 따라76.4453125/100.4921875/124.5390625. 사진 저장/성공 own탭 닫음. Windows source8 pending. 이 근거로 부모Margin.Top24와 article3의 max sharedtracks를 제품에 조립한다. 실제OS font/wrap는 각각 source 대비 검증한다.

## HEADLESS_PASSED · 2026-10-08

독립 immutable build22e97245b7cf4c93b116e671fedf4cd9/run1d5b558c4c30453db5165bb139068f4d compile0/0, originalMac group8 strict기하/문구/색차이0 및 기사 위치/공통row/24margin 확인. realSkia/DPR2 KO/EN PNG 직접확인, 같은인스턴스 width/language8와 mounted remove/language/remount3의 childcount3/숫자/언어/rooty24 assertions통과. source/welcomesteps-headless-validation.json 및 비교JSON에 범위/hash/환경 연결. 실제Mac/Windows proof pending; LOCK불가.

## 실제 macOS 통과 · 2026-10-08

같은22e97245b7cf4c93b116e671fedf4cd9 frozen binary에서 DBMV_HEADLESS 제거, UsePlatformDetect actualwindow/DPR2 실행. source8 strict차이0, mount/remove/language/remount3 assertions·rooty24·childcount3 및 KO1120 height76.4453125→EN100.4921875→KO76.4453125 갱신 확인. KO684/EN980 actual PNG 직접확인. run/output: /Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/workers/home/welcomesteps/22e97245b7cf4c93b116e671fedf4cd9/outputs/e2962a05f737413b97e0f5f2a98f77ac/actual-macos/candidate; source/welcomesteps-macos-run.json 및 mac-comparison.json. 자동닫힘 이후 exactownbinary process0 확인, 자원반납 통보. Root PageHeading 예외 재LOCK 후 전체verify_lock51통과. Windows source8/actual8 pending로 LOCK불가.

## 실제 Windows 통과 및 LOCK · 2026-10-08

Fresh Windows source8의 rootmargin24/기사위치/공유row/색/한영text를 cold isolated self-contained win-x64 a2e95fd6c9294a0f9cd995a5c2a2044f의 실제UsePlatformDetect와 비교해 strict차이0/DPR2. uniqueTEMP+full ZIP/runtime/coreclr/HeadlessDLL+hash guards, sameinstance8width/language와 mounted lifecycle3 childcount3/rooty24/numbers/한영제목갱신 assertions 확인. source/welcomesteps-windows-run.json, windows-comparison.json, windows-native/PNG3. source 원본 공식CLI capture는 title/msedgePID/rectangle/recordbutton focus guard와 화면 직접확인으로 저장했으며 top-levelUIA SetFocus 거부1회는 통과증거에쓰지않았다(확인된 소유recordbutton으로 다시focus해성공). screenshots/welcomesteps-windows-source-full.png 및 source/welcomesteps-windows-source-capture.json.

Ownsource/nativewindow 닫힘 및 exactstagedprocess0 확인: welcomesteps-windows-cleanup.json. Mac은같은frozen22e binary로통과했고 ownprocess0. 양OS원본8+HEADLESS8+actual8/각lifecycle3/paint로 완결. protected atomic 수정없음. 기본OS font/raster/wrap 차이는 각OS원본과bounds일치를확인했으며 KO1120의76.4453125(Mac)/100.4921875(Windows)도 각source와동일. 전체픽셀동일성을 주장하지않는다.

Group자체입력/독립animation은없다. Page입장/포커스는Home부모계약이다. 실제원본에group media1열전환은없으며 원본desktop 폭범위684/862/980/1120에서3columns를검증했다. 적용범위의 알려진 구현 차이/미검증 없음. source/welcomesteps-lock.json으로 product잠금. 다음 최소WelcomePaperContent→WelcomePaper로자동진행.

## LOCK 변경 사전기록 · Home 신규824 폭 · 2026-10-08
root승인: 새HomePage부모원본16에서viewport1100/page824의 article3 및그 number/title/body width253.328125 vs잠긴cumulativeQ253.3359375, 1/128px 실제기하차이. [source](../source/homepage-mac-source.json), [비교](../source/homepage-headless-comparison.json). 직접DOM재확인columns253.328/253.336/253.328, x0/285.328125/570.6640625, lastend823.9921875, parent824/gap32. 영향 WelcomeSteps 열폭계산 및Home부모; 보호Step파일불변. 수정전에adjacent/fractional822/823/823.5/824/824.5/825/826 +기존684/862/980/1120 양언어22조건조사. 제안candidate: CSS track fraction의float32 intermediate를double로누적quantize하는식; 현재는추정이고실제22DOM/Windows와대조전수정하지않음. 기존8조건증거보존, 변경영향과신규fractional조건양OS/headless+actualHome부모확인후재LOCK.

Mac추가22 original검증완료: [tracks source](../source/homepage-tracks-mac-source.json), [formula](../source/homepage-tracks-mac-formula.json), 후보double fraction=(float)(space/3), Q(fraction*i)누적위치/폭이22exact일치. 기존8폭/한영분배불변. root한정승인아래product열계산3줄교정, Windowssource22/headless/actual양OS 및Home16 확인전재LOCK未完. 다른LOCK파일불변.

새Mac frozen69eba/runb129a headless22 strict0/DPR2/realSkia·신규EN824PNG직접확인, lifecycle3기존assertions 유지. Win afc621全runtimebuild0/0 pack완료, 독립headlessstage준비. 실제Mac69eba bundle/output준비. 신규Actual22/원본Windows22未完.

Win afc621/run42e67d independentheadless22 realSkia/DPR2 기록·EN824PNG시각확인. 기존Windows원본8 subset비교strict0 [legacy](../source/homepage-tracks-windows-legacy-headless-comparison.json); 신규14는freshWindowssource대기여서22전체PASSED승격없음. actualOS는root앱연결slot後배정.

新actualMac69eba22 strict0/lifecycle3/EN824 actualPNG直接視認、nativeProcess/window0 [cleanup](../source/homepage-tracks-mac-cleanup.json)。ownedtracksIAB15閉鎖、独立HomeDOM14のみ継続。binary/probe/product凍結一致。Macgrant短slice終了root返納、Windowsfresh22/native22待機、再LOCK未完。

## 좁은 예외 재LOCK 완료 · 2026-10-08
FreshWindows22 [source](../source/homepage-tracks-windows-source.json), frozenafc621/headless42e67d22 strict0 [headless](../source/homepage-tracks-windows-headless-comparison.json), actual22/lifecycle3 strict0 [actual](../source/homepage-tracks-windows-comparison.json). KO824/EN824 actualPNG 직접확인, [cleanup](../source/homepage-tracks-windows-cleanup.json) exactnative/source0. Mac69eba/headlessb129a22 및actual22 strict0/paint/lifecycle3/cleanup0 앞기록유효. 두OS22 모두원본fractional分配一致, 이전8조건은같은batch에포함되고paint/input코드불변이므로기존증거유지. 유일변경은float32fraction을double로누적quantize하는column폭3줄, 보호WelcomeStep/타LOCK파일불변. 기본OS글꼴/래스터차이범위는이전명세와같으며 각OSnative/source기하·문구·색일치확인. Home페이지전체부모는아직未검증, 별도HomePage명세에서새16 확인. 이번예외의알려진차이/미확인없음, source/welcomesteps-lock.json 새hash로재LOCK.
