# WelcomeStep · LOCKED

2026-10-08 원본 home() `.three-actions article`의 세 variant를 잠갔다. 숫자 div → h3 → p, 자식 LOCKED Typography. 독립 입력·animation이 없는 안내 article이며 부모가 3열/공유 subgrid tracks/바깥 여백과 페이지 진입을 소유한다.

## 원본 및 구현 계약

최종 CSS `.three-actions`의 auto3rows/rowgap8, article grid/subgrid/rowspan3/gap8/align-items:start, number/p margin0을 실제 양OS에서 확인했다. Number12px ui-monospace/monospace normalweight/normal lineheight는14px, h3 14px/700/1.6의 실제 linebox22.3984375, p13px/1.85의 실제 linebox24.046875이며 색은 number/body #b5bac1/title #f2f3f5다. 한영 exact home() 문구를 사용한다.

WebWelcomeStep는 native 자연 text heights를 NaturalTracks로 제공하고 SetSharedTracks로 부모의 세 최대 row값을 받는다. 소스 측정값을 후보 입력에 넣지 않는다. SetPresentation(index0..2,English)는 같은 article의 문구·HeadingLevel3·접근성 이름을 갱신한다. 부모 Fixture에서 actual3sibling/sharedrows 안에 article24를 배치해 검증했다. available width684/862/980/1120의 실제 CSS cumulative64분수 열 분배를 사용하며 각 article 위치는 부모 WelcomeSteps의 후속 계약이다.

## 원본·검증 증거

prepare_welcome_step.py는 exact original home().three-actions 및 모든 원본CSS를 추출한다. KO/EN×4width×3article=24조건 각OS. [Mac 원본](../source/welcomestep-mac-source.json), [Windows 원본](../source/welcomestep-windows-source.json), [computed subgrid](../source/welcomestep-mac-subgrid.json), [Mac 원본 화면](../screenshots/welcomestep-mac-source.png), [Windows 원본 화면](../screenshots/welcomestep-windows-source-full.png). 원본은 당시 단독 source lease에서 수집했고 이후 bridge endpoint만 owner/component/OS 고유 이름으로 변경했다. 문구/CSS/geometry는 변경없다.

HEADLESS_PASSED: immutable isolated Mac build988f938066a94990a7b962f14cf3e378에서 realSkia/DPR2 24 strict 차이0, mounted remove→language→remount3, 비어있지 않은 KO684/EN980 PNG 직접확인. [명령/hash/environment/범위](../source/welcomestep-headless-validation.json), [비교0](../source/welcomestep-headless-comparison.json).

실제 macOS: 동일 binary를 DBMV_HEADLESS 없이 UsePlatformDetect 실제 window로 실행. 24 strict차이0/DPR2, lifecycle3 및 KO/EN paint 직접확인. [실행/hash](../source/welcomestep-macos-run.json), [비교0](../source/welcomestep-mac-comparison.json). 실제 native 자동종료 후 자원반납.

실제 Windows: 소유 probe만 포함한 isolated cold self-contained win-x64 build e12785c65f594f86b268bef9e8f21ee3, build0warning/0error. 전체 runtime ZIP/binary/cases hashes/uniqueTEMP/coreclr/HeadlessDLL guard로 stage했고 DBMV_HEADLESS 없이 actual24 실행. [배포·실행·lifecycle3](../source/welcomestep-windows-run.json), [비교0](../source/welcomestep-windows-comparison.json), [KO paint](../source/welcomestep-windows-native/welcomestep-ko-684.png), [EN paint](../source/welcomestep-windows-native/welcomestep-en-980.png), [자동종료 확인](../source/welcomestep-windows-cleanup.json). 각OS original과 비교해 기하/문구/색상 차이0. 언어 false→true→false, 재부착 후 갱신 확인. 숫자01/02/03·제목 갱신 및 sharedtrack 유지도 기록했다.

## OS 표현 및 LOCK

기본OS font/raster 차이는 각OS 원본과 actual bounds/wrap 일치를 확인하고 허용한다. KO1120 Mac 원본은 전체76.4453125, Windows 원본은 두 번째 본문 wrap으로100.4921875이며 후보가 각각의 source와 정확히 일치한다. OS간 줄수를 강제로 같게 만드는 보정은 하지 않는다. 부모 전체여백/media/진입animation은 후속 WelcomeSteps/Home 범위이며 독립 article에는 해당없다. 알려진 article 구현 차이·미검증 적용상태 없음.

[LOCK manifest](../source/welcomestep-lock.json). 보호 기존파일 수정없음. 현재 root PageHeading 예외로 verify_lock이 그 파일에서만 실패하는 것은 해당명세에 미리 기록된 별도 작업이다. 압축복구 때 AGENTS/MASTER/status/parallel/worker/활성spec/source/native/차이를 실제 확인 후 actualWindows 단계부터 재개했다. 다음 활성 WelcomeSteps group으로 자동 계속한다.
