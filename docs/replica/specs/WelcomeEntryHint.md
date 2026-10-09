# WelcomeEntryHint · HomePage 내부 검증

WelcomePaper LOCK 이후 Home .home-entry 직접자식 p.entry-hint를 최소 단위로 조사한다. 잠긴 SvgIcon(info15) + Typography(span) 부모flex만 소유하며 실제 시작/파일/예시 actions 및 section geometry는 다음 부모범위다.

실제 원본 home(): p.entry-hint 내부 info svg와 span. KO `저장한 결과 JSON은 DB 연결이나 새 검사 없이 바로 열어 확인할 수 있습니다.` / EN `Open a saved result JSON without connecting to a DB or running a new check.`. 최종 CSS `.entry-hint{display:flex;align-items:center;gap:8px;font-size:12px;color:var(--muted);margin-top:14px}.entry-hint>.svg{width:15px;height:15px;flex:none}`. Browser 최종computed/실배치/OS기본font/wrap/lifecycle 확인 전 구현 확정 없음.

Standalone width438/600/744/980 한영8에서 narrowwrap와넓은부모를 조사. Fixture exacthome().entry-hint/originalCSS를 보존하고 ancestorwelcome만 제공한다. Root-relative margin14 포함host계약으로 측정; parent actions내 marginposition은 Entry조립에서검증. 장식아이콘/안내문이라 자체action/선택/animation없음; actualpassive focus정책은 부모Entry범위에서원본근거로검증한다. directchildren잠김, sourceDOM→명세→headless→bothOS/lifecycle/paint→LOCK 후 actions부모진행.

## 실제 source8 및 구현
[Mac fresh source](../source/welcomeentryhint-mac-source.json): font system-ui12/line19.2/400, muted181/186/193, margin14/gap8/icon15stroke1.7. NativeText wrapper에서 실제 OS naturalText 측정 후 CSSceil(DPR×64) naturalspanwidth와 available-23 중 작은값 사용, 보호Typography를 다시 finitewidth로 측정한다. KO natural373.203125는8조건1줄, EN443.234375는438조건만 span415×38.390625로2줄. 1줄row19.1953125/icony16.09375, EN438row38.390625/icony25.6953125 (parent-relative14 포함). 부모는 floorcenter와 quantized19.2 lineHeight를 적용한다. 보호Icon/Text변경 없음. Sourceiframe read-only evaluate의 crossiframe sanitizedcontent 접근1회 실패는 fixture record와 frameLocator 실제DOM으로 확인·저장했다. ownhidden tab12닫힘. 별도pointer/key가 없는 장식안내문; 부모 passive focus정책은 Entry원본조립 검증범위.

작업 규칙: [MASTER_RULES](../MASTER_RULES.md) · [검증 운영](../HEADLESS_VERIFICATION.md).

## HEADLESS_PASSED / actualMac 完了
Mac c8d216/run d3cf0538 headless8/fullmounted3 strict0, realSkia/DPR2非空PNGを原解像度視認。actualAX範囲のためprobe-only --welcomeentryhint-window（同じ自動8/3記録後window維持）追加、product変更なし。新Mac746c27 actual8/fullmounted3 strict0、[actual AX](../source/welcomeentryhint-mac-ax.json)で本文AXStaticText1・decorativeicon除外を確認。最初driverはOSタイトルバーの同じAXStaticTextも数えてguard停止、exactownedwindowtitleを除外して本文1を再確認、product変更なし。Windows初期0809はholdflag追加前の未stage包、finale1190全runtimepack/stage準備。Mac新build2812はMSBuild childnode退出2でbuild失敗、同じsource fresh746c成功0warnings/errors、検査通過再実行ではない。

Mac sourcepreview exactEN438原解像度paintとactualEN438/KO438を直接確認、samewrap/infoicon/文字色。[原本画面](../source/welcomeentryhint-mac-source-en-438.png)、[actual comparison](../source/welcomeentryhint-mac-comparison.json)、[actual environment](../../../artifacts/replica/workers/home/welcomeentryhint/746c27c4bf584cd38f347a44969b4573/actual-macos/environment.json)。[cleanup](../source/welcomeentryhint-mac-cleanup.json) exactnative0/source12/13closed。親に新action/focus/animation無し、親Entryのpassivefocusはそこでsource-first検証。Windows実source/native/AX/paint未完、LOCKではない。

## Windows 독립 RECORD
Finale1190 runtime를 ownUUID 전체ZIP hash/coreclr/HeadlessDLL guards 아래 --headless-only stage했다. [Win headless environment](../../../artifacts/replica/workers/home/welcomeentryhint/e1190d67f0b049f98ca557eca7f089de/windows-headless/2fa23ce85bf043d3a7de8ba350567d8c/environment.json) 독립 realSkia/DPR2 layout8/mounted3/paint를 기록, [headless cleanup](../../../artifacts/replica/workers/home/welcomeentryhint/e1190d67f0b049f98ca557eca7f089de/windows-headless/2fa23ce85bf043d3a7de8ba350567d8c/cleanup.json) exactprocess0. FreshWindows원본 비교전 RECORD이며 Macsource값으로 Windows통과 주장없음. Native8/AX/paint는 rootgrant뒤실행. Protected/product변경없음.

## 실제 Windows 접근성 차이 · 제품 교정 전
Fresh original KO438 실제 UIA는 본문 Text1 [source peer](../source/welcomeentryhint-windows-source-peer.json), native600KO는 같은본문/같은844×38rect의Text2 [native diagnostic](../source/welcomeentryhint-windows-peer-diagnostic.json). 보호WebSystemTextBlock wrapperText와 WindowsWebTextBlock childText가 둘다노출되며 현재parent가경계를제공하지않는다. Geometry8/lifecycle3/headless8은각OSsource와strict0 확인, 차이는peer중복범위다. 수정 이유: source처럼본문단일Text노출. 영향: 새WelcomeEntryHint의TextHost peerboundary만; 보호Typography/Icon변경없음. 계획: 얇은ownedDecorator에 동일lockedHintText를넣고 Windows만 이름이본문인 no-childTextpeer, Mac은기존defaultchildpeer유지. 크기/palette/문구/레이아웃은같으며 nativechildnaturalmeasure 유지. 교정후peer한영/언어갱신과영향geometry8/mounted3/paint를양OS검증, 완료전LOCK없음.

Peer교정구현: owned HintHost Decorator에 동일protectedHintText 조립, Windows만 singleTextName/nochildrenpeer. Macbasepeerflatten유지. Probeoutsidefixture utilitytoggle로 KO→EN→KO peer갱신을실제검증예정. 최초2ba0 compile에서 ownnullableoverride와 동시draftCapabilityViewport error2, 각각소유자가교정한뒤 correctedMac2cc813/Win a45f24 0warnings/errors. Olde119before-peer-native에actual8/paint 보존, source/native diagnostic창closed exact0 후Windows반납. CorrectedMacheadless2cc813/69be72 affected8/fullmounted3strict0; 이전통과조건은productpeer구조영향때문에다시검증, 무변경반복아님.

Corrected Windows a45f24/run15ff2995 source8/fullmounted3strict0 HEADLESS_PASSED (freshoriginal unchanged이므로재수집없음). Corrected Mac2cc813/69be72 및 Windowsa45/15ff의 KO438/EN438 PNG 4개가 before-peer headless와byte-identical [paint equality](../source/welcomeentryhint-peer-paint-unchanged.json), peer교정의시각변화없음. Actual양OS peer3/affectedlayout8는 OSgrant대기, 아직LOCK아님.

## Corrected actualMac 완료
Mac2cc actual8/fullmounted3 strict0, actual AX KO→EN→KO 본문1씩. [AX](../source/welcomeentryhint-mac-ax-corrected.json), [paint](../source/welcomeentryhint-mac-corrected-paint.json) 제품영역 이전actual과2조건 pixel동일. 전체windowPNG 차이는 canvas0,0 probe-only 언어utility; 표준headless4PNG동일증거 유지. [cleanup](../source/welcomeentryhint-mac-corrected-cleanup.json) exactnative0, Hintsource12/13closed. 별도HomehiddenDOM14만계속. 단독LOCK없이 HomePage 내부증거로보존.

## Corrected actualWindows 완료
Win a45f24 actual8/fullmounted3 strict0, actual AX KO→EN→KO 본문Text1씩, originalEN본문Text1추가확인. [AX](../source/welcomeentryhint-windows-ax.json), [원본 EN peer](../source/welcomeentryhint-windows-source-en-peer.json), [원본 paint](../source/welcomeentryhint-windows-source-en-438.png), [cleanup](../source/welcomeentryhint-windows-corrected-cleanup.json) exactnative/source0. 첫원본preview Window.SetFocus가UIA비포커스창이라거절되어이미활성화된고유창foregroundguard로capture수정, 제품불변; outerfinally CLI Invalidargument는freshread로0확인. 통과actual8/3/peer3는반복없음. Mac/Win 적용내부Hint 범위완료, 단독LOCK없이HomePage부모에포함.
