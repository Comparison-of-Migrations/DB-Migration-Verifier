# 병렬 복제 작업 · 2026-10-08

사용자 확정: 하위 에이전트 3개는 gpt-6.1-sol / high. 단일 활성 컴포넌트는 에이전트별 적용. 원본 조사 → 명세 → 구현 → 비교·수정 → 검증 → LOCK 순서를 생략하지 않는다. 전체 작업까지 이어 간다.

## 파일 소유권

- root: AGENTS.md, MASTER_RULES.md, COMPONENT_STATUS.md, COMPONENT_TREE.md, 이 파일, tools/ReplicaProbe/Program.cs, 공유 프로젝트/런타임/서버 설정, 최종 InfoDialogBody/InfoDialog 및 MainContent/MainWindow 통합. 에이전트는 공유 파일 변경을 root에 요청한다.
- connection: WebConnectionPage.cs, specs/Connection.md, ConnectionPage 전용 probe/prepare/compare/driver, connectionpage-* 증거/LOCK, tools/MacControl/connection_page*.swift 및 독립 컴파일 산출물, workers/connection. Connection LOCK 뒤 FindingArticle(WebFindingArticle/spec/probe/prepare/compare/Win finding_article 관련파일과 findingarticle-* 증거)을 소유; protected Typography/Surface/Motion 읽기만. 이후 MetricItem→Results subtree는 다음단위배정요청.
- preflight: CapabilityTable/그 내부 미잠금 컴포넌트 → Preflight; 새로운 WebCapability*/WebPreflight* 파일, 동일 이름의 명세/probe/prepare/compare/driver, capability*/preflight* 증거/LOCK, workers/preflight. Home/Connection 파일 수정 금지.
- home: Home의 아직 없는 작은 섹션 → Home; 새로운 WebWelcome*/WebSampleReport*/WebHome* 파일, 동일 명세/probe/prepare/compare/driver, welcome*/samplereport*/homepage* 증거/LOCK, workers/home. Preflight/Connection 파일 수정 금지.
- LOCKED 파일은 어느 에이전트도 임의 변경 금지. 반드시 원본 근거·이유·영향을 기록하고 root와 범위/차례를 정한 뒤 회귀 검증 및 재LOCK.
- 새 파일도 전역 이름 충돌을 피한다. 공통 helper를 새로 필요로 하면 root에게 알린다. manifest/hash는 본인 소유 새 컴포넌트만 생성한다.

## 검증 환경 사용 차례 · 최신 추가 조건

[HEADLESS_VERIFICATION](HEADLESS_VERIFICATION.md)가 검증 운영 정본이다. Headless는 독립 snapshot/build/process/output으로 병렬 실행한다. sameApplication/Dispatcher/global상태 공유 금지. HEADLESS_PASSED와 LOCK 구분.

현재 실제OS 소유자는 [ENVIRONMENT_LEASE.json](ENVIRONMENT_LEASE.json)의 resources.macos/resources.windows를 각각 읽는다. 입력/포커스/캡처를 바꾸는 같은환경만 순차 사용한다. guest공식CLI가 Mac포인터/foreground를 건드리지 않는 작업은 동시허용한다. 겹치는 자원은 root가 배정한다. 공유 설정/project/Program/server는root소유. source bridge와배포binary는고유prefix 사용. 기존전역owner/단일shared obj/runtime 방식은 폐기했다.

## 압축 복구

복구 순서는 [MASTER_RULES §5](MASTER_RULES.md#5-맥락-압축기억-불확실-시-복구)를 따른다. 이 문서는 파일 소유권·다음 배정만 관리한다.

## 통합 대기

root는 각 LOCK 증거/보호 검사를 확인해 전역 상태를 반영한다. 공유 Program.cs 진입점은 에이전트가 정확한 코드/flags를 요청하면 root가 추가한다. 다음 독립 작업을 배분하고 모든 페이지/항해/모달/저장·재열기/중단 동작을 통합 대조한다. 작업 지속·임시 제약 수명은 MASTER_RULES를 따른다.

## 다음 담당 예약 (현재 단위 LOCK 이후)

connection → Results의 아직 없는 작은 Metrics/filter/findings/TrialResult → Results. preflight → Selection의 toolbar/table → Selection. home → Trial의 아직 없는 form → Trial. root → InfoDialogBody → PageHeading Home부모 좁은LOCK예외 및 재검증 → InfoDialog/Progress → 최종 MainContent/MainWindow. 예약은 현재 컴포넌트 완료를 대체하지 않으며, 새소유파일은 root배정때 확정한다. 환경검증은 작은 안정checkpoint에서 차례를 반납해 다른 담당자가 비교차이를 분석할 수 있게 한다.

페이지 완성·부분 앱 연결·장식 묶음 정책은 [MASTER_RULES §13](MASTER_RULES.md#13-페이지-완성--실제-앱-연결--사용자-흐름-검증--2026-10-08) 참조. 현재 담당 예약: connection Metrics→ResultsPage, preflight CapabilityViewport→CapabilityTable→PreflightPage→SelectionPage, home correctedHint(parent scope)→HomePage→TrialPage, root PageIntegration→ProgressPage→전체 흐름.

2026-10-09 active narrow exception: home owns WebTrialPage.cs in-place SetViewport/Split gap only (TrialPage pre-change spec); root owns ReplicaWorkspace resize caller. No other protected child change. Existing constructor proofs reused under actual diff, new API remains IN PROGRESS until both OS affected verification.

2026-10-09 Trial resize61e 재LOCK 이후 home의 다음 한정 소유는 WebConnectionPage.cs viewport-only resize API와 자체Connection-resize probe/fixture/증거다. 기존 connection 담당은 Results/marker 범위만 유지하고 해당 파일을 동시에 수정하지 않는다. 원본 새resize 증거/차이 확인과 변경 전 LOCK 예외 기록 후에만 변경한다. root는 Workspace caller만 담당한다.

Connection resize의 직접 AdvancedConnection 의존 경계도 home가 새원본 펼침resize4 증거와 해당명세 변경전예외기록을 먼저 확보한 뒤 한정 viewport/gap API를 맡는다. Connection 자체와 Advanced를 동시에 활성구현하지 않고 의존하위→재LOCK→Connection 순서로 전환한다. 다른기능/보호하위 재설계 없음.

2026-10-09 Selection baseline8275/a836/331b LOCK 뒤 부모 exactannouncement 계약에필요한 readonlyLastChangeReason Publish metadata 좁은예외는 preflight가변경전명세/원본handler근거/새event-only validation 후재LOCK한다. root binder는재LOCK뒤연결, 원래ViewChanged/geometry/보호하위불변.
