# Metrics

LOCKED · 2026-10-08. Product SHA `642ea3ed0cd5bd4d719224e4f528b11454ecded6a6771c96f3e5e0568bcb5d12`. [보호 manifest](../source/metrics-lock.json). 지침/운영은 [MASTER_RULES](../MASTER_RULES.md)/[HEADLESS_VERIFICATION](../HEADLESS_VERIFICATION.md). 잠긴 MetricItem4개/Typography/Motion/Surface 변경 없음.

원본 `.metrics`는 이름·동작·포커스가 없는 flex parent다. wrap/center, gap40이며 viewport≤1180에서는 가로·세로 gap28. padding8top24bottom/border-bottom1 #41434a/margin-bottom24. 마지막 검사ID에 margin-left:auto를 적용하여 해당 줄의 오른쪽 끝에 붙이고, 그 줄의 최대높이 기준으로 center 정렬한다. 네 child는 source의 자연 크기로 whole-item line packing을 하며 강제폭/원본좌표를 후보에 공급하지 않는다. 적용하는 desktop parent폭≥326에서 각 literal child가 개별적으로 들어간다. 직접 source DOM/최종CSS는 [live original](../source/metrics-live-original.json), 양OS72 측정에 보존했다.

`SetPresentation(english,different,unknown,equal,runId)`는 숫자/ID literal을 잠긴 child에 전달한다. `ViewportWidth`는 부모가 원본 viewport를 전달한다. `PlayEntrance(reducedMotion,cancellation)`는 focused Results 진입에만 숫자3개220 entrance를 요청하며 ID는 제외한다. ordinary rerender/attach/reappend는 자동 재생하지 않는다. presentation 변경/detach는 child effect 정리 계약을 계승한다. Summary 계산/데이터/OS reduced 조회/전체page200/scroll/filter/modal/footer는 ResultsPage 또는 App 책임이다. 8개 Text의 순서 외 별도의 Group name/action/focus가 없다.

완료 범위는 KR/EN×viewport1000/1280×parent326/400/480/656/904/1060×sample/zero/large =72 whole-parent 조건, 각 조건의4child/label/value box·literal·gap·외부margin. source/native8 상태는 attached-no-auto/normal-start/end/detached/reappend-no-auto/suppliedreduced/narrowEnglish reuse/wideKorean reuse다. 수치3개 start(.6,Y5), end(1,Y0)와 ID1/Y0, 제거/복구 및 갱신을 비교했다. Nodeidentity는 public reuse API diagnostic이며 원본 render(false)가 새DOM을 만든다는 정책을 바꾸지 않는다. 220ms duration은 원본/잠긴 Motion 계약이다. 별도 실제220 elapsed 실측/OSpreference변경을 주장하지 않는다.

- Headless realSkia/DPR2 `be4de9ce957a492993d0a0b480828be8`/run `4ba13ca9e7f345d38d26488d48035c32`: [72/8 strict0](../source/metrics-headless-comparison.json), 3PNG 직접 확인.
- macOS 동일 frozenbe4 actual72/8 strict0: [비교](../source/metrics-mac-comparison.json), [actual8Text/원본 비교](../source/metrics-mac-peer-comparison.json), 실제 CUA screenshot와 native3PNG 확인. exactownedapphost absent/process0 [정리](../source/metrics-mac-cleanup.json).
- Windows fullcold win-x64 self-contained `340993c3faab4b85bcc00533042385f2`: [freshsource72](../source/metrics-windows-source.json), [native72/8 strict0](../source/metrics-windows-comparison.json), source/native 모두 정확한 여덟 control+content Text 이름/순서/내용 일치. [원본 peer](../source/metrics-windows-source-peer.json), [native peer](../source/metrics-windows-peer.json), [foreground/bounds guard actual screen](../source/metrics-windows-native/native-frame.png). Native3PNG/actualframe 직접 확인. Exactprocess0/sourceWindows0 [정리](../source/metrics-windows-cleanup.json).
- [Frozen product/lockedchild 동일 해시](../source/metrics-frozen-product.json), [원본1px/#41434a border 실제PNG centerpixel9개](../source/metrics-border-paint-proof.json). 양OS typography/palette는 잠긴 MetricItem을 재사용하며 새 parent 배치/paint/peer를 검증했다. 기존 통과 child 조건을 반복하지 않았다.

Mac probe는 기본 NSWindow이며 실제 MainWindow는 기존 Opened의 잠긴 WebSurfaceColorSpace.Apply를 호출한다. 본 단위 CSS RGBA/native RenderTarget PNG와 실제 창 배치·peer를 비교했고 기본 창 색관리/AA를 bit-exact screenshot 동일이라고 주장하지 않는다. 최종 앱 sRGB 창환경 연결/paint는 root 통합 범위다. OS 표현 차이로 배치/내용/모션 차이를 면제하지 않았다. 실제 입력 대상이 없는 passive parent이며 새 hover/pressed/disabled 행동을 추가하지 않았다.

실패 보존: 최초 fixture body 생성 전 script로 empty0 record가 나왔으며 [FAILED empty](../source/metrics-fixture-empty-failed.json)에 보존한 뒤 own harness 수정. 첫0fc16d probe는 deferred child/state IEnumerable가 최종조건에서 직렬화되어1302 stale box 진단 차이 발생. 해당 frozen failed output을 보존하고 eager snapshots로 probe만 교정했다. 제품 layout 교정 없이 다음 전체72/8 strict0을 확인했다. 두 실패를 합격으로 쓰지 않았다. 성공 전용 IAB 탭 및 양OS 검증창 정리, 사용자 원본탭 무변경.

다음은 §13에 따라 잠긴 Metrics/ResultChart/FindingArticle/Details 등을 조립하는 ResultsPage다.

## 당시 조사·교정 이력

아래 당시 상태는 현재 LOCK 판정을 대체하지 않는다.

# Metrics

IN PROGRESS · HEADLESS72/8 및 actualMac72/8/8Text/paint 통과, Windows actual72/8/8Text/paint 대기. 지침은 [MASTER_RULES](../MASTER_RULES.md)/[HEADLESS_VERIFICATION](../HEADLESS_VERIFICATION.md). Root 배정: WebMetrics.cs/본 명세/MetricsProbe 및 prepare/compare/Windows metrics tools/metrics-* 증거. 잠긴 MetricItem4개를 조립하며 Typography/Motion/Surface 변경 금지.

실제 원본 Home→예시 결과 전용 IAB 탭에서 DOM/computed 확인 후 탭 닫았다. [live original](../source/metrics-live-original.json): viewport1280×720 DPR2, Metrics980×109.59375. flex-wrap:wrap/align-items:center/gap40, padding8top24bottom, border-bottom1 #41434a, margin-bottom24. 원본 media max1180에서는 gap28. 네 div는 flex0 1 auto/min-width:auto, 마지막 검사ID에 margin-left:auto. Sample3/2/45/ID; 마지막 ID만 height35.1875이고 숫자height76.59375 line center에 정렬된다. 결과 데이터 runSummary의 different/unknown/equal, run.id와 언어 literal을 전달한다. Source render(false)는 새 DOM을 만들지만 entrance를 요청하지 않으며 render(true)만 숫자3개220 entrance. Metrics 자체 page transition 없음.

계획: exact unchanged Metrics template/CSS에서 KR/EN×viewport1000/1280×parent326/400/480/656/904/1060×sample/zero/large3 =72 parent조건, 전체4child box/row grouping/IDauto margin/padding/border/외부margin 비교. 자연폭으로 flex line packing하여 space가부족할 때 whole child가 다음line으로 가는지, 행별center/auto margins/rowgap CSS정밀도를 source에서 측정한후 구현한다. actual narrow326는 최소모바일외 폭으로 각childmin-content를 강제침범하지 않는다. 서로 다른OS source naturalwidth로 own-parent layout 재현해야 하며 source배정좌표를 candidate에 공급하여 차이를 가리지 않는다. 갱신/언어/폭/attach/detach/reduced/explicitentrance 수명 별도, passive8Textpeer/paint 양OS 확인. 미확인 상태이며 구현/LOCK 없음.


HEADLESS_PASSED72/finite8 · frozenbe4de9ce957a492993d0a0b480828be8/run4ba13ca9e7f345d38d26488d48035c32, productSHA642ea3ed0cd5bd4d719224e4f528b11454ecded6a6771c96f3e5e0568bcb5d12. Root/4child/label/value whole-row geometry/gap28/40/margin24/text/passive peer and source8/native8 motion/detach/reuse strict0. Representative en1280-326-large PNG直接確認: three numeric rows, ID rightalign onlastline, borderfullwidth. Firstfixture source0 was body-not-created harness failure, preserved metrics-fixture-empty-failed.json; corrected source now72. Firstprobe0fc16d deferred child/state arrays serialized lastcondition, failedsnapshot retained; eager snapshots correction is probe-only and candidate unchanged. Width>=326 cases ownfour lockednaturalitems alwaysfit individually, no newmin-contentprimitive needed. Source finite narrowreuse adopts same4children into actual1000iframe (gap28) then1280iframe (gap40), references retained. Reduced suppliedskip, actualOSpreference Resultsparent. Exact220 wallclock not yet measured here; Motioncontract/source duration. ActualOS72/8/8Text/paint pending, NOTLOCK.


ActualMac be4 frozen72/8 strict0, actualCUA exact8Text/unnamedcontainer and screenshotpaint inspected. Cleanup exactapphost absent/process0 and ownedtabs0; Macresource released. Probe/product unchanged. Windows fullcold340993c3faab4b85bcc00533042385f2 compile0/0 prepared, own metrics.py guard/record/peer/capture next actual. Outer second source iframe visibilityhidden excludes unrelated duplicate accessible fixture; DOMgeometry/CSS/media childmeasure unchanged, only harness accessibility scope. Mainparent72source measured prior version remains geometryvalid.


Frozen 양OS 제품/lockedchild 해시는 [metrics-frozen-product](../source/metrics-frozen-product.json), Mac 원본 literal과 actual8Text 비교는 [peer comparison](../source/metrics-mac-peer-comparison.json), 기존렌더 border centerpixel 검증은 [border proof](../source/metrics-border-paint-proof.json). Windowscold340993 전체runtime staged 완료, native창 미실행. ActualMacgenericcontainer에 이름/동작/포커스가 추가되지 않았고 원본 genericdiv의 8개 literal Text가순서대로 같음. 언어/폭 재사용 nodeidentity 상태는 publicAPI diagnostic이며 원본 render(false)가 새DOM을 만든다는 사실을 동일nodeidentity 주장으로 바꾸지 않는다.

Paint 환경 범위: 실제 Mac probe는 기본 NSWindow이며 real MainWindow는 기존 Opened에서 잠긴 WebSurfaceColorSpace.Apply를 호출한다. 본 단위는 CSS RGBA/실제 native RenderTarget PNG 및 실제 창 내용·배치·peer를 비교했다. 기본 native 창 capture의 색관리/AA를 bit-exact CSS pixel 동일으로 주장하지 않는다. 최종 앱의 sRGB 창환경 연결/paint는 root 통합 검증에 포함하며 Metrics 자체의 palette를 바꾸지 않는다.
