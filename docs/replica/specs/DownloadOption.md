# DownloadOption
LOCKED · 2026-10-08. SaveOptions의 가장 작은 내부 카드부터 진행한다. 직접 원본30 Save 화면/DOM/CSS를 확인했다. 기존MainWindow.Save의 100px 체크박스Panel은 원본 근거가 아니다.

## 원본 DOM·상태
`label.download-option > input[type=checkbox][data-format] + span(strong + small)` 세 종류 json/csv/html, 한영. 선택 변경은 change의 state.formats 갱신 후 render. 부모 Save가 파일 저장/다시 열기/전체 재검사를 소유한다. 독립 카드는 selection request만 올린다. 카드 전체 클릭은 checkbox를focus/toggle하며 Enter는 토글하지 않고 Space는토글한다. 선택0/1/복수는부모상태다.

## 실제 CSS·크기
flex/aligncenter/gap12, padding22, border1/radius8, default #2b2d31/#41434a, pointer cursor, 별도 카드 transition/hover 없음. checkbox20×20, margin0/selfcenter. span minheight64/selfstretch, title14/22.4/700/ink block/marginbottom4, description small inline12/19.2/muted. small inline은 부모14/22.4 strut baseline과 linebox 안에서 배열하며 단순 block으로 해석하지 않는다. span은flex1이아니라 intrinsicwidth/shrink를사용한다. 기본 card110 높이, checkboxx23/y45, spanx55/y23/h64; 원본30 카드317.328/317.336px.

선택 CSS 특이성: :has(input:checked)가 :has(:checked)보다 specificity가 높아 배경#3a3e58/테두리#5865f2가 최종이다. 뒤의 :has(:checked) insetshadow1 #8b94ff는별도속성으로적용된다. 파일 마지막 색만읽으면오답이다. 실제computed확인했다.

## 계획·현재 단계
하위Checkbox/Typography/Surface는LOCK계약사용. 명세→Card 구현→원본 한영3형식×대표카드폭×checked0/1 배치/문구/팔레트/라벨 기준선 비교→실제label/checkbox/pointer/Space/Enter/갱신·focus→LOCK한다. 예상OSfont표현차이만기록하며크기누락은면제하지않는다. 부모3열폭/전체형식집계/페이지렌더포커스는SaveOptions/Save에서검증한다.

## 재개 확인 · 2026-10-08 후속
AGENTS/MASTER_RULES/COMPONENT_STATUS/이 명세/SaveOptions를 실제 읽고 36개 LOCK 보호 검사를 통과했다. 실행 중 원본30 Save의 세 label DOM/computed geometry·palette를 다시 확인했다. 직접 하위 Checkbox/Typography/Surface와 관련된 MatchOption/Notice의 baseline 조립 계약, 기존 MainWindow.Save를 확인했다. 기존 Save는 원본과 다르므로 복제 근거로 사용하지 않는다. 활성 카드는 아직 구현 전이며 아래 조립부터 재개한다.

## 검증 기록 · 2026-10-08
독립 원본 literal fixture의 한영×3형식×폭209.328125/317.328125/344×checked0/1 =36조건을 양OS에서 수집했다. border-box/20px checkbox/span intrinsic width·stretch64/strong block/description inline baseline 및 줄별 폭·선택 팔레트·insetshadow 모두 compare_download_option.py 오차0. source/downloadoption-{mac,windows}-{source,comparison}.json 및 각 native/downloadoption-native.json. 폭209의 Windows Korean description은2줄이 되어 실제 card117.1953125; 원본과 같은OS 대조값을 사용하며 Mac110을 강제로 적용하지 않는다. 기본 OS font raster 경계는 사용자 허용 범위이며 기하/기준선 오류를 면제하지 않았다.

실제 mac 원본35 입력 sequence 및 native 대응: source/downloadoption-mac-source-input.json / source/downloadoption-mac-input/{enter-final,pointer-after-key-final,english-update-final,reduced-html-final}.json. Windows native 실제body pointer/Enter 무토글/Space/click-after-key/영어/reduced 및cancel은 source/downloadoption-windows-input의 fresh JSON으로 보존했다. 부모 재생성·3열집계·전체페이지 scroll/focus는 SaveOptions/Save에서 따로 검증한다.

발견한 Checkbox의 same-focus pointer 표시 문제는 사전LOCK예외 기록 후 FocusFromPointer만 추가해 양OS72 endpoint와 실제 동일입력 재검증·재LOCK했다. Native 창에서 추가언어/형식갱신은 선택과 changes를 유지했고 ReducedMotion에서도 본문click이한번만토글했다.

검증 도구 제한/교정: native 창 닫은뒤 getAXState가자동재실행하는것을확인해 이후닫기는inventory로검증한다. Windows helper의복사된옛choice ID와정수UIA rectangle의엄격한fractional폭guard가입력전에거부됐다. 정확format-json/scale확인및UIA1physicalpixel반올림범위로guard만교정했으며actual layout 비교tol.001은늘리지않았다. 첫Checkbox회귀stdout에진단문구가붙어JSON읽기실패; 출력분리후새36기록을수집해72대조했다. 실패는통과로쓰지않았다.

## 최종 판정 · LOCKED
양OS36 최종 배치/상태/inline baseline 비교 errors[] 및 실제 입력 검증을 완료했다. 수정한 공개 부모 포커스 진입도 Checkbox 회귀 후 재LOCK했다. 구현은 src/Verifier.Desktop/WebDownloadOption.cs, 검증 도구 prepare_download_option.py / DownloadOptionProbe.cs / compare_download_option.py. 원본에 카드 자체 transition/hover가 없으므로 새 효과를 만들지 않는다. lower Checkbox의 검증된 transition만 유지한다. 이번 고립 카드의 직접 selected animation은 원자 input 계약이며 원본 Save의 전체 재생성에 의한 endpoint는 다음 부모에서 재검증한다. 파일 출력/실제DB/전체화면은 완료가 아니다.
