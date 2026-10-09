# Headless 병렬 검증과 실제 OS 검증 분리

2026-10-08 사용자 추가 조건. 원본 우선/Bottom-Up/에이전트별 단일 활성/파일 소유권/LOCK은 유지한다. 이 문서는 검증 운영 정본이다.

## 병렬 headless
원본 조사→명세→구현 후 담당자가 독립 프로세스에서 구조/기하/spacing/wrap/색상/border/radius/실제 렌더링/선택·disabled·오류·busy·입력 및 이벤트/언어·크기 변경/제거·재부착을 검증한다. 동일 Application/Dispatcher/변경 가능한 전역 상태를 공유하지 않는다. 원본 측정값과 실제 동작을 비교하고 환경에 맞춰 제품/원본을 바꾸지 않는다.

실행 도구: `python3 tools/ReplicaProbe/isolated.py build <owner> <component>` → 출력 build.json. `python3 tools/ReplicaProbe/isolated.py run <build.json> -- <probe-flag> <cases.json>` → 각 실행 outputs/UUID/candidate 및 environment.json. build는 src+probe+중앙props의 독립 source snapshot/버전hash, 모든 참조 프로젝트 obj/bin, temp를 owner/component/UUID 아래 분리한다. 공통 설정/Program/ReplicaPlatform/isolated.py는 root만 변경한다. snapshot이므로 다른 담당자 변경 중 재컴파일하지 않는다. native도 해당 isolated binary를 이용하고 공유 generic binary를 덮어쓰지 않는다. Windows cold실행은 build --runtime win-x64 --self-contained 뒤 WindowsControl/stage_isolated.py pack/stage를 사용해 전체exe/runtime/deps/HeadlessDLL를 고유TEMP에 검증·배포한다. stage는Windows자원소유/실행중아님/ZIP·binary해시/coreclr 존재를 확인한다. 기존2DLL 교체와 frameworkdependent 묶음은 cold실행 증거로쓰지 않는다. build는 해당probe+공통ReplicaPlatform만 컴파일하는entrypoint를 snapshot에 생성하여 다른probe draft오류에 묶이지 않는다.

Avalonia.Headless12.1.3 + 실제 Skia, UseHeadlessDrawing=false, process별 DBMV_HEADLESS=1/DBMV_HEADLESS_SCALE=2. macOS host의 실제 .AppleSystemUIFont/Menlo 및 native text renderer를 유지한다. 빈 mock drawing은 화면 증거로 쓰지 않는다. [공식 설정](https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform). 원본/후보 창 크기·배율·글꼴·언어·데이터·모션 시점은 각 probe/명세/environment에 기록한다. 다른OS 글꼴/플랫폼과 비교할 수 없는 것은 미검증이다. Mac headless로 Windows native 표현을 통과했다고 주장하지 않는다.

HEADLESS_RECORDED는 실행/증거 저장만 의미한다. 원본 엄격 비교·상태/입력/수명 등 해당범위를 통과한 뒤 명세/worker상태에 HEADLESS_PASSED와 정확한 범위를 기록한다. component는 IN PROGRESS 유지. 같은코드/조건의 통과검사는 변경·실패·새우려 때만 반복한다.

## 실제 OS 자원
ENVIRONMENT_LEASE.json resources.macos/resources.windows를 독립 관리한다. 같은OS 포인터/키보드/길게누르기/창 활성/포커스/복원/IME/클립보드/파일대화상자/플랫폼 접근성/실제모션/전체플로우는 한 담당자씩. Windows 공식 CLI/UIA/guestSendInput·캡처가 Mac host 포인터/foreground를 바꾸지 않을 때 Mac 검증과 동시에 허용한다. Parallels hostGUI 조작이 필요하면 root에게 겹치는 Mac 자원도 확보한다. 브라우저 원본도 담당자 고유 탭/fixture와 endpoint 사용; 사용자탭·다른창은 변경하지 않는다.

원본 bridge는 `/<evidence>/<owner>-<component>-<mac|windows>-<source|input|control-ready>.json` 고유 endpoint를 갖고 source/동일파일명으로 저장한다. 예: /evidence/connection-connectionpage-windows-input.json. 기존source-sidebar/control-ready는 하위의 동시검증에 쓰지 않는다. fixture HTML·served binary·ZIP도 담당자 prefix로 분리한다. 서버는root소유.

실제OS 적용항목과 부모안배치를 모두 확인해야 LOCK한다. HEADLESS_PASSED만으로 LOCK하지 않고 미확인을 OS차이로 면제하지 않는다. 기존 LOCK의 이미완료한 실제OS 증거는 다시 반복하지 않는다.

## 재개와 지속기록
복구 순서는 [MASTER_RULES §5](MASTER_RULES.md#5-맥락-압축기억-불확실-시-복구)를 따른다. 활성 컴포넌트/구현 hash/환경·명령·출력/통과 범위/남은 OS/차이·수정/다음 행동을 명세와 worker 상태에 기록한다.

## 운영 확인
root 최초 isolated build의 OpenedEvent 없는 API 실패는 보존하고 IsVisibleProperty class handler로DPR을 설정했다. 수정된 isolated build는0warnings/errors, Homeheading12 실제 원본 strict0/dpr2 및 실제renderPNG 직접 확인. 이 smoke는 infrastructure 검증이며 각컴포넌트 LOCK을 대체하지 않는다.

원본 DOM-only 조사 경계: 담당자 고유 hidden IAB 탭에서 DOM/compute/고유fixture 측정 기록만 수행하고 실제 host포인터·foreground·키보드·포커스·캡처를 조작하지 않는 조사 작업은 다른 담당자의 Mac native입력과 독립 진행할 수 있다. 실제 입력/포커스/OS 캡처는 Mac자원 차례가 필요하다. Windowsguest 원본은 Windows자원을 계속 따른다. 조사결과를 actualOS입력 증거로 승격하지 않는다.

## 검증 처리량 개선 · 2026-10-08
실제OS 입력 대기 중에도 `stage_isolated.py stage BUILD.json --headless-only`로 고유 owner/component/UUID의 전체 runtime을 배포할 수 있다. 이는 입력 권한을 주지 않으며 native 창 생성·포커스·캡처를 포함하지 않는다. 독립 DBMV_HEADLESS=1 프로세스·출력만 사용한다. 실제OS driver는 자원별 소유권 검사를 유지한다. 동일 구현/조건에서 통과한 기하·입력은 반복하지 않고, 변경 영향 범위/실패/새우려만 재검증한다. 실제 입력은 안정된 순서로 묶고 신선한 진단값을 각 단계별 저장한다. 관찰자가 원본 포커스를 변경하면 진단 문제를 분리하여 기록하고 포커스를 바꾸지 않는 관찰로 다시 확인한다. 검증 항목·LOCK 조건은 줄이지 않는다.

Parallels CLI 전송 자원은 독립 프로세스와 구분한다. 2026-10-08 guest 명령 전송 충돌 방지를 위해 driver.ps의 CLI 전송만 파일 lock으로 순차 처리한다. headless/native 검증 프로세스와 owner 출력은 계속 독립이다. 명령이 시작되지 않았다고 명시된 session 생성 거절만 제한 재시도하며, 결과 불명확한 입력·변경 명령은 자동 재실행하지 않는다.

같은날 긴 ProgressMeter batch의 compressed argument6716자가 Parallels 세션 생성 단계에서 거절됨을 재현했다. 압축 wrapper도3500자를 넘으면 생성한 프로젝트 검증 명령만 고유 UTF8.txt/URL/SHA로 전달하고 같은 inline ScriptBlock으로 실행한다. OS execution policy를 변경하지 않으며 전송 파일은 finally 정리한다. 이는 OS 입력 lease를 확장하지 않는다.

독립 컴포넌트 snapshot에서는 등록된 다른 미잠금 신규 UI 파일만 제외한다. 보호 LOCK 파일·일반 앱 소스·담당 컴포넌트는 유지하고 제외 경로를 build.json에 기록한다. Stub/원본·제품 변경은 하지 않으며 실제 교차 참조가 있으면 빌드는 그대로 실패한다. 전체 Application 조립 검증은 이 필터를 적용하지 않는 전체 제품 빌드를 사용한다. 2026-10-08 RunningDot/Viewport snapshot이 다른 담당 Hint/Viewport draft 오류에 묶인 실제 병목을 해소하기 위한 한정이다.

Home/Results 병렬 snapshot에서 MSB4166 reused-node unexpected exit 재현: source/obj 격리 외 build node/compiler서버 재사용도 root에서중단(-nodeReuse:false, UseSharedCompilation=false, m:1). 각담당독립 process는동시에실행하며 한build안 node만single. 기존failure보존; 제품/원본변경없음.
