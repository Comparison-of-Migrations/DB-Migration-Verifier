# ActionFooter

LOCKED · 2026-10-07. ScrollViewport cap 차이를숨기지않고대기기록한뒤 직접하위가모두LOCKED인독립섹션을작업한다. 단일활성ActionFooter.

## 원본과 범위
`docs/replica/source/original.js` footer()/btn()/render(), original.css .footer/.footer-hint/.footer-actions와1180px breakpoint. 실행중원본connect의 DOM을새로확인했다: footer flex/align-items:center/gap12, padding16×40, border-top1#41434a, background#2b2d31,min-height84. 실제전체폭1204의footer폭984. quiet44/primary48은중앙정렬이고content높이51, top20.5/18.5. 힌트12px/1.6, flex1, footer-actions gap10/margin-left:auto. 본문실행·DB접속은부모책임이며하단은허용action을요청한다.

## 직접 하위
ActionButton/Spinner/SvgIcon/Typography/Surface/Motion 모두LOCKED. plain .btn를조립하므로기존지도IconButton을ActionButton으로교정했다. 잠긴하위의스타일·동작을바꾸지않는다.

## 다음 행동
한영×실제breakpoint폭1000/1138/1280×footer분기22조건의원본DOM/computed/geometry/disabled/action을수집→명세→후보조립→양OS비교/actual입력·언어갱신→차이교정→LOCK. ScrollViewport/Sidebar를동시에변경하거나미해결을완료로기록하지않는다.

## 수집·조립 계약
Mac 실제IABviewport1000/1138/1280의각44원본=132조건을수집했다. source/actionfooter-mac-source-{width}.json. 임시viewportoverride는즉시reset했고사용자원본의연결입력은조작하지않았다. 화면22분기×한영:home;connect미확인/busy/연결됨/연결됨busy;capabilities;select분류0/테이블0/trial미준비/준비/일반;trial동의전/동의/준비;review쓰기미정지/정지;running진행/완료/사용자중단;results;save형식0/선택.
후보WebActionFooter는부모에서힌트·왼쪽행동·오른쪽행동목록을받아조립하며screen분기/DB작동의판정은페이지부모가소유한다. Probe입력에는원본텍스트/action/variant/icon/enabled/loading만넘기고원본geometry값은넘기지않는다. viewport는부모실제입력이며후보는viewport−220폭과1180mediaquery의28/40padding을사용한다. 부모로부터들어온각상태의실제배치/갱신132조건을비교한다. 이검증을페이지분기구현완료로주장하지않는다.
FooterSurface border-top1/padding16×28 또는40/min84/shadow0−4 16 #00000018, flexrowgap12. 힌트flex1,actionsrowgap10;직접하위btnquiet44/primary48·spinner16은LOCKED계약대로조립한다. native각box/text/enabled/action/name/variant을별도sourcegeometry와비교하고실제클릭·키입력/갱신은후속이다. Windows는원본전용새Edge창에서1000/1138/1280 viewport를실제UIAresize/record로수집중이며title/process/doc/새POSTguard를유지한다.

## 배율 조건 교정 및 Mac 배치 통과
IABviewportoverride가DPR1로바뀌어nativeDPR2비교조건이틀렸다. 이첫132개는`actionfooter-mac-source-dpr1-{width}.json`으로보존하고통과근거에서제외했다. override는reset한상태다. 원본전용Chrome창을ActionFooter측정페이지로전환하고공식AXSize로실제1000/1138/1280폭을조절해DPR2의새44×3자료를수집했다. footer_source.swift는exactbundle/path/title/URL/doc및AXsize-settable/새POST/DPR2/viewport/44guard를확인한다. 첫resize가아직AXDoc에반영되지않은실패는readonlyinspect로실제1000반영을확인한뒤,추가resize없이최대2초layout조건을기다리도록교정했다.
처음nativeCanvas측정에서무제한width를DesiredSize로반환해창이중단됐다. footer의intrinsic측정은힌트·버튼의유한합으로반환하도록교정했고실제붙은새창132조건을다시수집했다. 이실패를통과로쓰지않는다.
Mac재수집원본과후보132layout/update/descriptor/icon조건은모두차이0. source/actionfooter-mac-comparison.json. Windows원본132조건도수집완료(DPR2, 실제UIA Transform.Resize); 후보새assembly를hash검증후닫힌전용runtime에준비중이다. 실제입력/갱신lifecycle및양OS대표paint비교는아직미검증,LOCK하지않는다.

## 재개 확인 및 현재 단계 · 2026-10-07
AGENTS/MASTER_RULES/상태/이 명세 → original footer()/btn()/press()/CSS → WebActionFooter/ActionFooterProbe를 다시 읽었다. verify_lock23 통과. 최신 사용자 OS 기본 표현 차이 허용을 규칙에 반영했다. Mac/Windows 각각132조건의 독립 배치·descriptor 비교는 실패0이며 Windows 신규 자료도 확인했다. 실제 입력·갱신·부모 안 대표 화면 확인을 계속하며 아직 LOCK하지 않는다.

## 최종 입력·표현 확인 및 LOCK · 2026-10-07
- 양OS 각132조건의 geometry/hint/buttons/icons/enabled/update는 독립 원본과 차이0. actual 입력 `source/actionfooter-input-comparison.json`: 양OS 연결요청1회, busy/분류0 요청0회, 영어 trial-ready 요청1회; Mac 실제 pointer→Space 누적2회는 trusted 원본과 일치했다. Windows는 실제 UIA Invoke 입력이며 Mac은 actual pointer/Space 및 고정 AX 상태 변경을 사용했다. 같은 action을 가진 trial 두 버튼은 이름으로 구별한다. 부모의 화면 전환/접속/실행 정책은 추후 Page에서 검증한다.
- `screenshots/actionfooter-mac-{source,candidate}-english.png`: 실제 DPR2의 동일780×84 섹션에서 대표 부모 배치·문구·경계·그림자를 확인했다. 첫 비교는 후보에 hover가 남아 부적합했으므로 포인터를 밖으로 옮겨 normal로 재촬영했다. 각 child의 motion/paint는 기존 LOCK 계약을 그대로 조립한다.
- 기본 렌더 경로의 알려진 표현 차이: 논리 primary 배경은 원본 CSS/후보 Brush 모두 #5865f2이고 후보 offscreen PNG는88/101/242다. macOS 실제 ScreenCaptureKit 출력은 source87/101/242, candidate85/101/251; border도65/67/74 vs65/67/75다. 부모 body/footer 영역은 RGB가 동일하다. 코드의 지정 색·기하를 임의로 보정하지 않고 2026-10-07 사용자 OS 기본 표현 차이 허용을 적용한다. 전체 캡처 픽셀 일치로 주장하지 않는다.
- 측정 오류: 진단용 bare Button의 중심 pointer는 callback에 도달하지 않아 고정 AXPress로 상태를 바꿨다(제품 footer 버튼 입력과 별개). trial action ID가 두 번 존재하는 조건은 unique guard가 거부했으며 실제 이름/현재 AX 위치로 분리했다. capture helper 첫 컴파일·window-title·CG 초기화 오류는 증거에서 제외했고 title/URL/path/geometry가 일치한 공식 캡처만 보존했다.
- `src/Verifier.Desktop/WebActionFooter.cs` 잠금. 실제 부모 ViewportWidth와 Content 변경을 명시적으로 전달하는 계약이며 default OS 표현 허용 외 미확인 단위를 이 LOCK에 포함하지 않는다. 원본/잠긴 하위는 수정하지 않았다. 두 후보 창은 전용 Close로 종료했다.
