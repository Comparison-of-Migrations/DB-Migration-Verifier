# ResultDetails
IN PROGRESS · 2026-10-08. TrialPreparation 완료 후 다음 의존 작업으로 자동 진행한다. 현재 단계는 실제 원본 결과 상세 drawer와 JS/CSS 조사다. 하위 구성 요소가 기존 LOCK에 없는 경우 내부 요소부터 명세/검증/잠금하며 parent는 WAITING CHILD로 전환한다. 기존 Avalonia로 원본을 추측하거나 MatchingDialog LOCK을 임의 변경하지 않는다. 전체 복제/조립과 임시 복구 제약은 계속 유지한다.

## 실제 원본 조사
원본30 Home 예시 결과 보기 → 데이터 분류 전체행으로 detail을 실제 열었다. mapping과 달리 class drawer가 없는 중앙 dialog(width620/maxheight vh−48/border1/radius14/bg2b2d31,border535963). head padding20 24,gap20/h2+close44; bodypadding24/overflowauto/maxh65vh; footerpadding18 24/gap12/justifyend/bg232428/borderTop1/닫기button44. body는 기본ul muted/padding-left20의 검사문장2~3개 + optional실행 Notice +trial이면cleanupwarn Notice +marginTop16 small 비밀값비표시 문구. 실제 데이터 dialog620×401.5859375. StatusPill/ResultSummaryRow는 내부에 없으므로 tree의 추정 의존을 실제 구성으로 바로잡았다. 먼저 작은 BulletList를 검증·LOCK하며 본 parent는 대기한다. Modal frame/close/Escape/focus/background는 부모 조립 때 확인한다.

## 현재 단계
BulletList/ResultDetailsBody 양OS 검증 후 LOCK. 원본 중앙modal(width620)은 mapping drawer(width500,right20)와 다르다. 하위35개 LOCK 무변경 확인 후 body·closeicon·footerbutton·title·Surface·ScrollViewport·Motion을 조립한다. head85/footer81, bodypadding24/max65vh와 outermax vh−48, 중앙 auto margin, 양OS 실제close/Escape/focus/scroll을 비교한다. 최신 OS 표현 예외는 measuredroot gutter만 적용하며 앱의 배치 차이를 숨기지 않는다.

## 후보 구조·검증 범위
WebResultDetails는 잠긴 body/title/IconButton/ActionButton/Surface/Motion/ScrollViewport를 조립한다. surface620/radius14all/border1/원본shadow, head85·footer81, BodyViewport padding24/max65vh와 outermax vh−48을 독립 관리한다. body·전체스크롤은 원본overflow:auto이며 원본의 초점 스크롤은 각 잠긴 viewport 계약을 사용한다. source openModal/closeModal literal iframe36조건(lang2×두폭×세높이×data/trial 완료24 + 기본창에서interrupted/pending/absent12)과 대조한다. 닫기160/동작줄이기 즉시·Escape·initialclosefocus·Tabcycle·opener 복귀·배경Blur2/입력차단을 actual fixture에서 확인한다. Application 전체 background AX isolation과 disconnected opener 재탐색은 페이지/앱 조립 책임으로 남긴다. 보호된 MatchingDialog 코드는 변경하지 않았다.

스크롤 검증을 실제로 하기 위해 동일원본 dialog의 높이400 desktop고립조건4개를 추가한다(총40). 이는 새 모바일 UI가 아니라 부모 viewport가 짧을 때 원본의65vh/전체maxvh−48 두 스크롤 계약을 확인하는 stress조건이다. 최초36의 성공여부와 별도로 확장 후 전조건을 다시 비교한다.

## actual focus 교정
실제 원본30 pointer 행 진입의 headclose는 active이나 :focus-visible=false/outline none였다. native Unspecified자동focus는 키보드 outline을 켜므로 OS면제가 아니라 현재부모 동작교정 대상이다. Open에 부모의 초기 NavigationMethod를 받으며 pointer/keyboard 진입을 그대로 전달한다. modal 내부의 pointer/key modality를 추적해 닫기 뒤 opener에도 같은 modality로 focus를 돌린다. geometry fixture의 programmatic진입은 기존Unspecified를 유지한다. 잠긴 ActionButton의 포커스 계약을 바꾸지 않는다. 페이지/앱 부모는 실제 진입방식을 제공해야 한다.

## 재개·진행 기록
2026-10-08 압축 뒤 AGENTS/규칙/상태/본 명세·직접 원본 CSS/JS와 원본30 열린 detail/후보 코드를 실제 복구했다. 보호된35개 unchanged. 최신 focus 교정 후 Mac40 배치 차이0. 실제 pointer headfocus는 outline없음, Tab→footer→head 및 ShiftTab→footer, Escape 후 opener복귀/배경해제 확인. 원본 Escape 완료 후 detail:data/focusvisibletrue로 복귀했다. 짧은 창 실제 스크롤을 위해 interactive trial fixture를600→400으로 바꾸며, focus-visible는 probe의 읽기 전용 진단으로 기록한다. native 파일은 각 동작 fresh 기록으로 보존한다. Windows40 비교와 actual 검증은 계속 진행 중이다.

Windows 첫 auto 실행은 기존 x64 apphost에 ARM64 DLL을 복사한 게시 실수로 assembly architecture 오류가 나며 입력/비교 전에 실패했다. windows-arm64-check는 역사적 폴더명이고 실제 검증 RID는 win-x64라는 기존 WindowsControl/MatchingDialog 기록을 확인했다. win-x64로 재게시하여 hash 교체 후 fresh40부터 검증한다. 실패를 통과 증거로 쓰지 않는다.

Windows40 raw比較で root位置差18値と English footer幅/x差40値を検出。rootは source環境945×585/client gutter15で中心が7.5移動した実測でありOS境界。English footer60のまま残るのは文言Closeへ切替後の古い測定が残るアプリ差で、OS面除しない。原本openModalはinnerHTMLで毎回新しいfooterbuttonを作るため、本parentもOpen毎に新しいLOCK済みbuttonを配置する。保護されたActionButton/Typographyを変更しない。

짧은400px actual에서 원본 Tab(head→scrollable body→footer)는 footer포커스 시 dialog.scrollTop58로 마지막 버튼을 노출한다. 후보는 footerfocus여도 outerOffset0으로 남았다. 기존 ScrollViewport의 RequestBringIntoView 계약을 부모가 명시적으로 요청하지 않은 조립 누락이므로 ResultDetails의 head/footer GotFocus에서 배치 후 BringIntoView를 요청한다. 잠긴 viewport를 변경하지 않는다. body wheel은 Mac107 실제 양수 이동을 확인했다.

실제 원본30 footer에서 Tab은 document BODY라는 무표시 focus 정류장으로 이동한다. 기존 후보는 곧바로head로 순환하여 정류장을 생략했다. ResultDetails 자체를Tab stop으로 포함하여 원본의 무표시정류장을 복제하며 화면에 새UI를 추가하지 않는다. source iframe에서locator.press 재focus를 섞은 End는 outer40이므로 pure 실제키 시퀀스의 source 검증과 구분한다. 후보 Endbody107 자체는 확인됐지만 nested focus-scroll 비교가 끝나기 전 LOCK하지 않는다.

오염 원인 확정: iframe이 실제 입력 대상으로 활성화되지 않은 locator.press는 keydown 증거 없이 자체 focus/scroll(outer40)을 만들었다. source34 실제 텍스트 클릭으로 iframe 활성화→native Tab→Tab→End의 keydown 진단에서 beforebody0/outer0, 완료body107/outer0을 확인했다. 잘못된40을 앱 요구로 복제하지 않는다. 같은 실제 시퀀스 Tabfooter는 outer76(maximum), 다음Tab은 BODY 정류장이다. 기존 parent BringIntoView nearest58은 locator 기반 수치였으므로 교정한다. Blink focus의 center-if-needed(부분적으로 가려진 버튼만 중앙정렬, scroll범위로clamp)를 본parent에서 적용해 실제76을 따른다. head부분가림시0으로돌아간다.

탭 경계 추가 조사에서 BODY 정류장은 AX에 focused UI가 없고 반복Tab도 페이지 요소로 복귀하지 않아 browser chrome으로 나간 host 포커스였다. 앱 DOM 내부 컴포넌트 정류장으로 간주한 앞선 해석을 폐기하며 native root의 추가Tab stop을 채택하지 않는다. 브라우저 주소창/도구바와 native OS window chrome은 대상앱 밖의 기본host표현이고 복제UI에 새 정류장을 만들어 강제로 맞추지 않는다. 앱 내부 head/body/footer 이동·배경입력차단은 그대로 검증한다. 보호된MatchingDialog와 같은Cycle계약을 유지한다.

중단 후 재개: 규칙/상태/명세와 실행 원본의 데이터 상세·직접 CSS/JS/후보를 실제 재확인, LOCK35 무변경. 중단된 publish process가 남아 있지 않아 재게시한다. 후보 코드 재확인 중 문자열 교정으로 headClick이 중복 연결된 것과 초기 폐기되는footer GotFocus가head를참조한 것을 발견해 정리했다. Close guard가중복을막았지만불필요한연결을유지하지않는다. 최종footer는Open마다새로조립하고정확한callback을연결한다.
