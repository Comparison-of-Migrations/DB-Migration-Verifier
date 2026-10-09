# Field

LOCKED · 2026-10-07. DbmsOption LOCK 뒤 직접 원본 field()/connectionSide()/trial(), 실제 Connection DOM/CSS와 기존 native helper를 확인했다. 검증 범위는 아래 최종 판정에 기록한다.

## 원본과 경계

`div.field → label.label[for=id] + input[id,data-field,type,value,autocomplete=off,spellcheck=false] (+ small)`.

- root margin-top16. label block/full width, 12px/600, color#dbdee1, unitless1.6, margin-bottom7.
- input는 LOCKED TextInput 또는 포트 NumberInput. font14/min-height44. password 예시는 disabled/빈값/placeholder이고 작은 도움말이 따라온다. 시안에 실제 비밀번호를 넣지 않는다.
- small block/full width,12px/400/#b5bac1/단위없는1.6, margin-top5. Connection의 password만 도움말을 갖는다. Trial의 시험값은 number가 아니라 text이므로 임의 numeric로 바꾸지 않는다.
- DPR2 label/각 hint 줄높이19.1953125. 실제1138 창의 host width238/port112/다른입력366. label y0,h19.1953125; input y26.1953125,h44. 도움말 없음 root70.1953125, 있음 hint y75.1953125/root94.390625. 언어·폭에 따른 label/hint 줄바꿈을 다시 수집한다.
- field-row의 두 열1fr/112와gap16 및 section padding/border는 ConnectionPanel 부모 소유다. root 상단margin16은 본 컴포넌트 소유며 normal block 부모 margin-collapse의 경계는 부모 조립에서 확인한다.

## 상태와 동작

label 전체 클릭은 연결된 input에 pointer focus. disabled password에 label click은 input을focus하지않고기존입력focus를해제한다(실제웹추가확인). 입력 focus/hover/disabled/수치 상태/keyboard/caret/transition은 잠긴 입력 계약을 사용한다. label/도움말/root에 원본 없는 hover, scale, 독립 mount 애니메이션을 추가하지 않는다. 실제 `.field`에는 aria-describedby가 없으므로 도움말을 임의 연결하지 않는다. input accessible name은 해당 label 문구다.

label click의disabled포커스해제는Source실측후후보를교정했다. 원본은body focus(입력focused없음),native는TopLevel focus없음으로대응한다.

input event에 따른 connected/trial 초기화·버튼/단계 상태 갱신은 부모 Connection/Trial의 계약이며 이 컴포넌트에서 재설계하지 않는다. 예시 password와 실제 앱의 활성 password 정책 차이도 부모에서 구분한다.

## 기존 native 차이

`MainWindow.HomeConnection.cs::Field`는 StackPanel gap6/label12muted이고 상단margin16/hint 구조가 없다. port도 기존 TextBox다. 현재 helper/상위화면은 변경하지 않고 별도 후보를 검증한다.

## 최종 판정 · 2026-10-07

양OS DPR2의 한·영 44개 geometry/palette/name/disabled 표본과 같은 Field의 입력 slot 교체 44회를 각각 대조해 전부 일치했다. 부모 전체 화면은 아직 조립하지 않았으며 ConnectionPanel의 두 열/section/margin collapse와 Connection·Trial의 상태 초기화는 해당 부모 검증에서 이어간다.

- `source/field-{mac,windows}-comparison.json`: 44 initial + 44 updates, failed0. native JSON/PNG는 `field-{mac,windows}-native/`.
- `source/field-mac-input-comparison.json`: 실제 서버·비활성 password·포트 라벨의 focus/blur 순서 일치. 재클릭은 `field-mac-source-repeat-label.json`과 최신 native events에 동일4순서로 기록.
- `source/field-windows-input-comparison.json`: 실제 native label3개와 Mac original 실제 event순서 일치. Windows original도 동일3개 full-width label70% 지점을 실제 클릭하여 활성/비활성/포트 최종 포커스를 각각 확인했다(`field-windows-input/source-pointer-{0,1,2}.json`). Windows native trace는 cleanup 전에 helper가 동결한 pointer-*-events.json이다.
- Windows Tab은 disabled를 건너뛰어 포트로 이동(`field-windows-input/key-tab-0.json`). 입력 자체의 상태·키/수치 동작/paint는 잠긴 TextInput·NumberInput 계약.
- `screenshots/field-mac-state-detail.png`는 원본/native 줄바꿈·수직 간격의 배치 비교. 2배 paint를 CSS pixels로 축소했으며 raster 비트 일치를 주장하지 않는다. `field-mac-input-final.png`는 캡처가 focusout을 일으켜 배치 근거로만 쓴다.

확인된 컴포넌트 범위의 남은 차이 없음. 임의 OS font/DPI·실제 자격증명 정책·전체 부모화면 검증으로 확대하지 않는다. `source/field-lock.json`으로 WebField.cs를 보호한다. 전체 복제는 진행 중이므로 AGENTS 임시 상기 의무를 유지한다.

## 교정·실패와 재개 기록

AGENTS→MASTER_RULES→상태→명세→직접 original field/Connection CSS와 live fixture→WebField/Probe→현재 차이를 복구했고 17LOCK 보호검사가 통과했다. 최초 도움말/간격 차이와 disabled 라벨의 focus해제 시점을 원본 실제 event에 맞춰 교정했다.

Windows LabelText의 빈 full-width 영역에 클릭이 전달되지 않는 것을 실제 pointer로 발견했다. 투명 Border LabelHitArea를 두고 그 전체 영역에 handlers를 옮겼다. 잠긴 Typography는 변경하지 않았다. physical pointer/DPI guard는 좌표 보호로 유지하며 DPI가 실제 원인이라고 기록하지 않는다. before-dpi-pointer-*와 초기 label-disabled.json은 실패 증거로 보존하고 합격에 사용하지 않았다.

사용자의 2026-10-07 프로젝트 검증 제어 일괄 허용을 WindowsControl에 기록했으며 추가 질문 없이 exact title/path/UIA frame/foreground guard를 사용했다. Windows source inline 검증 명령은 CLI 길이5244에서 입력 전 거부되어 SHA확인된 개발용 C# helper로 좁혔다. Edge UIA SetFocus의 지연은 제한시간 내 실제 focus 확인 후 입력하도록 보완했다. source3/native3 모두 실제 성공 기록을 받았다. 계정·OS 보안 설정·다른 앱·실제 DB를 변경하지 않았다.

다음은 독립 peer CategoryHeader 하나를 활성화해 후보 원본 비교를 진행한다. Field 상위가 완료됐다고 주장하지 않는다.
