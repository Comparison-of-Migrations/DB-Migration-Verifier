# ResultDetails
LOCKED · 2026-10-08. 원본 결과 상세 중앙 모달의 조립 계약이다. 과거 조사·실패·폐기한 해석은 [조사 기록](../history/ResultDetails-20261008.md)에 보존한다. 현재 계약은 이 명세가 정본이다.

## 원본 위치·구조
실행 원본30의 예시 결과 → 분류 전체행 `data-action="detail:data"`로 열어 확인했다. `source/original.js` openModal(detail), closeModal과 original.css의 최종 dialog override를 따른다. MatchingDialog의 오른쪽 drawer와 다르다.

Overlay → 중앙 dialog → head(h2 + close icon button) → body(검사 목록 + optional scope notice + trial cleanup warning + privacy text) → footer(close button). 내부 내용은 LOCKED ResultDetailsBody다. StatusPill/ResultSummaryRow는 내부에 없다.

## 크기·배치·색·글자
- dialog: width min(620, viewport−32), border1 #535963, radius14 all corners, #2b2d31, shadow 0 18 48 rgba(0,0,0,.14), max-height viewport−48, 중앙 배치.
- head: padding20 24, gap20, bottom border1 #41434a. h2 18/27/700, spacing−.27, #f2f3f5; close44×44. 기본 head85.
- body: padding24, overflow:auto, max65vh. 목록/공지/문구 spacing은 ResultDetailsBody 계약이다. outer overflow와 body overflow는 독립이다.
- footer: padding18 24, gap12, right aligned, top border1 #41434a, #232428; base close button44. 기본 footer81.
- backdrop: #0e0f14 alpha128, 실제 배경 blur2.
- 사용 글꼴은 잠긴 OS system typography 계약이며 래스터/AX 기본 표현 차이를 강제 보정하지 않는다.

## 상태·입력·애니메이션
Open마다 원본 innerHTML처럼 새 footer button을 조립한다. 언어 변경 시 문구·intrinsic width가 갱신된다. Open은 두 스크롤을0으로 초기화하고 닫기 아이콘에 포커스를 둔다. 부모는 실제 pointer/keyboard 진입 NavigationMethod를 제공한다. pointer 자동 focus에 keyboard outline을 표시하지 않는다.

원본 modal-in280/source ease, close160/ease-out, reduced 즉시를 잠긴 Motion으로 재생한다. head/footer close 및 Escape는 한 번만 닫힌다. 배경 입력을 잠그고 blur2를 적용하며 닫을 때 이전 IsEnabled/effect를 복구하고 opener에 현재 입력 modality로 focus를 반환한다. 배경 클릭은 닫지 않는다. 내부 버튼의 hover/pressed/focus/animation은 각각 LOCKED 자식 계약이다.

Tab은 head→(실제 overflow가 있는 body)→footer로 이동한다. 가려진 head/footer 포커스는 원본의 center-if-needed를 outer viewport 범위로 clamp하여 노출한다. 한영 400px stress에서 body End107/outer0, footer focus outer76을 실제 키 입력으로 확인했다. 브라우저 주소창·도구바로 나가는 host Tab 경계는 앱 내부 UI가 아니며 native OS chrome과 강제로 맞추지 않는다. native는 모달 내부 Cycle을 유지한다.

## Avalonia 구조·구현
[WebResultDetails.cs](../../../src/Verifier.Desktop/WebResultDetails.cs). Surface + ScrollViewport + Typography + IconButton + ActionButton + ResultDetailsBody + Motion만 조립한다. `_flow`는 head/body/footer의 세로 배치와 max-height를 소유한다. 보호된35개 자식 코드는 변경하지 않았다.

## 검증 결과·증거
- 양OS 각40: 한영×viewport960/1280×height600/720/960×data/trial 완료24, interrupted/pending/absent12, height400 stress4. 중앙/내부 box·문구·색·shadow·scrollHeight·상태 엄격 비교에서 미해결 차이0.
- [mac 비교](../source/resultdetails-mac-comparison.json), [Windows 비교](../source/resultdetails-windows-comparison.json). source/native JSON은 같은 source/의 resultdetails-{os}-source.json과 resultdetails-{os}-native에 보존한다.
- 양OS attached lifecycle6: normal opacity0→1, 닫는 동안 visible/closing, 중복 요청에도 close1회, reduced open1/즉시close. 각 native/resultdetails-lifecycle.json.
- 실제 Mac pointer 진입/no outline, Tab/ShiftTab, backdrop 유지, head/footer/Escape, caller 복귀·배경 복원, body wheel/End, footer/head 자동 노출, reduced. [최종 내부 End](../source/resultdetails-mac-input/short-body-end-final.json), [최종 footer](../source/resultdetails-mac-input/short-footer-focus-scroll-final.json), [즉시 닫기](../source/resultdetails-mac-input/reduced-close-final.json).
- 실제 Windows UIA/공식 CLI 키: trial→Tab body→End107/outer0→Tab footer/outer76→Escape/caller 복귀, reduced open/close. [End](../source/resultdetails-windows-input/short-body-end-final.json), [footer](../source/resultdetails-windows-input/short-footer-final.json), [Escape](../source/resultdetails-windows-input/escape-final.json), [reduced](../source/resultdetails-windows-input/reduced-close-final.json).
- [원본 실제 키 시퀀스](../source/resultdetails-mac-source-input.json). iframe 미활성 locator.press의 자체 focus/scroll은 keydown 증거가 없었으므로 폐기했다. 실제 텍스트 클릭으로 활성화 후 native keydown/scroll을 확인했다.
- 원본30 실물과 양OS native PNG를 시각 확인했다. 측정·검증 도구: prepare_result_details.py, ResultDetailsProbe.cs, compare_result_details.py, WindowsControl/result_details.py 및 result_details_key.py.

## 허용된 OS/host 표현 차이
Windows의 원본 최소 desktop 높이720보다 작은 조건은 default root scrollbar15 때문에 client viewport가 줄었다. 중앙 좌표 raw18차이(12조건 x7.5, 그중6조건 y7.5)는 measured inner/client 크기와 일치한다. 비교 파일의 rawErrors와 allowedOSOffsets에 보존했고 native 위치를 강제로 이동하지 않았다. 내부 geometry/text/state는 면제하지 않았다. 각 OS system font의 raster/기본 scrollbar·window chrome 표현만 최신 사용자 조건을 따른다.

## 직접 부모 책임·미확인 범위
Application 전체 background AX isolation, disconnected opener의 data-action 재탐색, Results 데이터/분류 클릭 연결은 페이지/앱 조립 책임이다. 현재 컴포넌트 LOCK은 전체 앱 조립 완료가 아니다. 실제 DB/개인정보/쓰기 작업은 수행하지 않았다. 현재 컴포넌트의 알려진 앱 배치/입력 차이는 없다.

## LOCK
resultdetails-lock.json에 WebResultDetails.cs 전체 SHA256을 기록했다. 임의 변경하지 않는다. 상위 조립에 필요한 예외는 MASTER_RULES의 사전 기록·영향 재검증 절차를 따른다. 양OS 검증 창은 정상 종료했다. 전체 복제가 남아 있어 AGENTS 임시 복구 조건은 유지한다.
