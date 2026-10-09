# SaveOptions
LOCKED · 2026-10-08. DownloadOption을 양OS 검증·LOCK한 뒤 시작했다.

## 원본·부모 계약
실제 원본30 Save의 .download-options > label.download-option×3, json/csv/html 순서. grid columns repeat(3,1fr), gap14; 내용이 가장 높은 카드에 나머지를 stretch한다. 원본 UI의 state.formats 집합을 change마다 add/delete하고 render() 전체 DOM을 재생성한다. render는 data-format으로 focus를 복원하고 keyboard/pointer modality를 유지한다. 새 checkbox의 첫 checked는 transition의 중간 값이 아니라 endpoint다. source Save의 format0도 허용되며 footer만disabled/형식선택 hint가된다.

카드 부모는 선택 집계와 세 카드의 재생성·포커스 보존을 소유하고, Save page는 app 전체 렌더/scrollTop/언어 버튼·다운로드/파일내용을 소유한다. SaveOptions에서 추가문구·icon·새 animation을 만들지 않는다. 명세 기존 IconButton 의존을 추측근거로 쓰지 않는다.

## 원본 확인
직접 원본30에서 JSON 본문 click→모두해제: formats[], format=json focus/visible=false, footer disabled와‘저장할 형식을 선택하세요.’ 확인. 이어 실제Space→formats[json],focus/visible=true,footerenabled 확인. src original.js save/change/render와 CSS grid를 직접 확인했다. 부모 width656/980/1060의 CSS64·DPR 단위 fr잔여분을 같은 원본 fixture에서 각각 측정할 것이다.

## 계획
LOCKED 카드만 조립한다. 한영×3부모폭×선택집합(0/default/복수/all) 24배치 조건과 양OS actual pointer/Space/Enter/Tab·선택change후재생성·언어갱신을대조한다. 폭/height/fr분할은원본DOM실측, 키보드입력은현재실제포커스검증후전달한다. 부모페이지 전체동작/OS파일대화상자는여기에서완료주장하지않는다.

## 조사·교정 기록
원본 3열 width656에서 마지막 열은209.328125이고 .0078125px를 남긴다. 단순 double 잔여전부배분은209.3359375가 되어 틀렸다. source DOM을 폭657/700/904.25까지 확장해 float fr값을 CSS64·DPR 단위 누적 경계로 절삭하는 대응을 검증했고 mac48 errors[]다. 기존UIA physical정수반올림guard를실제배치오차허용으로혼동하지않는다.

첫 native 조립 측정은 options.Select를즉시배열로materialize하지않아 나중에Serialization할때 이미detach된과거카드의 TranslatePoint가null이되었다. 실제보이는배치는맞았고측정도구를.ToArray로교정했다. 실패484를성공으로기록하지않았다.

실제 원본36/native창에서 JSON 해제→CSV선택→JSON재선택은 원본JS Set의삽입순서[CSV,JSON]를보존한다. 후보가canonical정렬[JSON,CSV]로반환한차이를발견해선택순서List로교정한다. 시각카드순서는항상JSON/CSV/HTML로유지한다. 아직이교정후입력재검증중이며LOCK하지않는다.

## 최종 계약·검증 · LOCKED
구현 src/Verifier.Desktop/WebSaveOptions.cs는 잠긴 DownloadOption만 조립한다. 부모폭은 finite이며3열은JSON/CSV/HTML순서, gap14,각폭은측정한fr누적CSS단위로정한다. 가장높은카드에모두stretch한다. SelectedFormats는JS Set삽입순서이고해제후재선택은끝에붙는다. SetPresentation은선택집합과언어를적용해새card를만들고현재format input의keyboard/pointerfocus를복원한다. 선택change는새checkbox의첫checked endpoint를사용한다. WebFormatSelection(Format,Selected,FocusVisible)을페이지에전달해전체페이지재생성에서도정확한포커스복원이가능하게한다. Save페이지가외부scrollTop/파일출력/공통footer disabled·hint를소유한다.

양OS 한영×부모폭656/657/700/904.25/980/1060×선택4집합=48각조립 geometry·카드높이·내부배치가errors[]다. source/saveoptions-{mac,windows}-{source,comparison}.json 및 각 native/saveoptions-native.json. literal원본save()에서가져온DOM/CSS를사용했고native값을원본측정에주입하지않았다.

실제mac원본36/native 및Windowsnative:모두해제→focusjson/false/downloadEnabledfalse,Space선택→json/true,TabCSV→focuscsv,Enter무토글,Space복수선택→changes3/JSONCSV,언어변경→동일집합/입력포커스없음,JSON해제후재선택→CSVJSON순서. source/saveoptions-mac-source-input.json,각OS-input/{insertion-order-final,english-insertion-order-final}.json, key-enter/tab/space 및 Mac space-recreated/multi-csv 기록. 새metadata API뒤각OS실제pointer+Space도재검증: {pointer,keyboard}-request-final.json의Format/Selected/FocusVisible가실제값과일치했고callbackassert가통과했다.

최종nativePNG는세card의색·정렬·간격을직접시각확인했다. 부모에는추가hover/press/animation이없고원자checkbox스타일은기존LOCK계약이다. 부모를둘러싼Page-entry animation은Save페이지에서다룬다. 기본OSfont renderer edge만허용된표현범위이며알려진geometry/input차이는남기지않았다. 실제native검증창은정상닫았다. 전체앱통합은완료가아니며임시복구제약을유지한다.
