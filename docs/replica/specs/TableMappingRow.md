# TableMappingRow

LOCKED · 2026-10-07. CategoryHeader19번째 LOCK 후 활성화. 원본 `tableRows()`의 tr/4td/label.table-choice → checkbox+mono text, target mono, match-method(icon+text), quiet swap ActionButton을 조사한다. 초안 지도 IconButton 의존은 실제 text button 근거 확인 후 ActionButton으로 교정한다. 테이블 공통 열 너비와 border-collapse는 부모 소유이며 단일 행은 공통 열 너비를 입력받아 조립한다. 원본 DOM/CSS computed size/spacing/state/interaction을 실제 Selection에서 다시 측정한 뒤에만 구현한다. 아직 완료/LOCK 없음. 전체복제 임시 복구 제약 유지.

## 현재 판정
양OS48 layout/update·96 CSS timing·18 actual input snapshot씩 모두 통과했다. 직접 하위는 변경하지 않았다. 공통 열 auto-layout·테이블 외곽선·MatchingDialog·Selection은 부모 작업에서 조립한다. 다음 단일 작업은 결과 요약 행의 직접 내부 요소 조사다.

## 실제 원본 조사
`source/tablemappingrow-live.json`에서 현재952px table의4열은298.046875/243.96875/170.5/239.484375, 첫2행69px·마지막68.5px이다. td최종padding12px18px이며 border-collapse에서 윗선/아랫선이각0.5px를소유한다(마지막아랫선0). label은첫cell가용폭×min44, gap12, checkbox20, mono12/19.2/tabular. label전체는선택영역이고 row나대응button은선택을바꾸지않는다.
match-method는block flex gap6/icon14/12px19.2, 일반#b5bac1/수동#f0c47c. quietbutton은text600/12px19.2/icon18/gap9/padding10×16/min44. selected row#343640, hover#3a3c44(selected우선), background160ms CSS기본ease. 마지막row별border·table-shell외곽선·전체열auto배분은부모소유다.
실제수동대응`archive.orders`적용후열너비가271.4453125/222.1953125/240.25/218.109375로바뀌었다(`source/tablemappingrow-manual-live.json`). 단일row는부모가계산한4열폭을받고내부padding/정렬/상태를복제한다. 부모의table auto배분을고정비율로추측하지않는다. 검색/일괄선택/modal/매핑적용/announcer는Selection과MatchingDialog조립에서검증한다.
원본radio 선택중표시이름이‘선택’을포함하도록갱신되어Playwright setChecked의후확인이timeout났다. freshDOM에서실제선택을확인후대응적용을했고중복입력하지않았다. 실패를전환실패로추측하지않았다.

구현 후보의Panel.Render override는sealed API로compile실패했다. 공유rowborder는LOCKED WebSurface를half-border두께로조립하도록교정한다. 첫후보는아직검증/LOCK되지않았다. Choice cursor도actualcomputed default근거로Arrow를사용하며임의Hand를추가하지않는다.

첫48대조에서322개의내부box차이를찾았다. inherited unitless19.2의floor·intrinsic flex width올림과Method SVG의기본flex-shrink를후보가누락했다. manual610원본에서SVG는KO13.2421875/EN6.546875px로줄어들어text에더넓은폭을준다. source measured회복없이icon14fixed로가정하지않고intrinsic14:text비율에따른flex-shrink및layoutunit올림을조립한다. 잠긴Typography/ActionButton/SvgIcon은변경하지않고부모의원본role값을명시적으로전달한다. 같은overall width의column교체는row.Measure cache를무효화해야함도확인했다. 첫실패를통과로올리지않는다.

두번째48대조의남은141box차이는cell vertical-middle center의1/256px미양자화였다. CSSLayoutUnit floor로center를계산하고target td의unitless line-height도명시한다. 허용오차를넓히지않고재비교한다.

세번째대조의12차이는shrink분배결과의round-to-nearest였다. EN동일이름의icon계산12.863236px는128unit에서1646.494...이므로원본12.859375px로반올림한다. intrinsic contribution올림과최종flex분배반올림을구분한다. 특정문구/폭의예외값을추가하지않는다.

색상timing조사에서selected+hover는전환이없음을확인했다. 원본:has([data-table]:checked)의specificity가:hover보다높아selected#343640이유지된다. 실제원본label2번클릭후hover=true에서미선택#3a3c44→선택#343640을확인했다. 초안hover우선추론을교정하고static48의selected+hover조합을명시적으로넣는다. transition없음은fixture reverseDuration=null로기록하며존재하지않는animation을만들지않는다.

2026-10-07 복구: AGENTS→MASTER_RULES→상태→활성 명세→직접 tableRows/CSS/native를 다시 읽고19LOCK 보호를 확인했다. 최신Mac원본48/96측정을 플랫폼 정본으로 보존하고 selected+hover 우선순위 교정 후 재빌드·재대조부터 재개한다.

실제 원본 입력: Enter는checkbox를토글하지않지만 focus-visible=true가된다. native의첫Enter기록은handled key를bubble fixture가받지못해stale인상태였으므로probe만tunnel로교정한다(LOCKED checkbox는이미Enter focus-visible처리를갖고있음). 대상이름같은비포커스cell을클릭하면원본은기존input을blur한다. 후보row에부모tunnel blur를추가하고child checkbox/button은제외하여재검증한다. Windowspointer helper 첫compile의dir scope오류및이후UIAgeometryguard실패는입력합격으로쓰지않는다.

최종점검에서target mono linebox도48표본에명시적으로추가한다. 진단fixture의display:block span은원본td의익명linebox를측정하기위한것이며행높이/열배분의기존측정이유지되는지도대조한다. WindowsUIA좌표는physicalpixel로반올림되어internal fractionalbox와0.203125px차이가있었다. 입력guard의physical좌표허용은1px로명시하고실제internalgeometry1e-7비교는유지한다. 첫실제입력은timer가파일을쓰던중읽기충돌로실패했으며finallyrelease했고재입력전창을초기화한다. 고정fixture파일의읽기만boundedIOException retry한다.

held実測에서childChoice capture가native IsPointerOver를false로바꾸어미선택row hoverpaint가사라지는차이를발견했다. DOM :hover는pointer가row안에있으면유지되므로부모의enter/exit/move좌표로hover를추적한다. capture중에도geometry기준을사용하고밖으로drag한경우는해제한다. LOCK된Checkbox/ActionButton은수정하지않는다. held/drag/release를다시검증하기전LOCK하지않는다.

## 최종 판정 / TableMappingRow LOCK · 2026-10-07

- 구현: `src/Verifier.Desktop/WebTableMappingRow.cs`. LOCKED Checkbox / ActionButton / Typography / SvgIcon / Surface를 조립했다. 동일 인스턴스에 이름·수동 대응·열폭·선택·마지막 border를 교체하는48표본, KO/EN ×610/858/1058을 양OS에서 검증했다. target mono linebox도 포함하고 geometry 허용은1e-7px이다.
- 원본: tableRows() / 실제 Selection의 label 선택·비선택 영역 blur·Enter/Space·대응 요청을 확인했다(`source/tablemappingrow-live-input.json`). 모달은 parent boundary다. 원본 live DOM뿐 아니라 보존 CSS의 실제 cascade, mono font, unitless line-height·CSSLayoutUnit 양자화·flex shrink를 사용했다.
- 배치 비교: `source/tablemappingrow-{mac,windows}-comparison.json`, 비교 도구 `tools/ReplicaProbe/compare_table_row.py`, 원본과 candidate48rows 각 플랫폼에서 failed0.
- 전환: `source/tablemappingrow-{mac,windows}-timing-comparison.json`, 각96rows failed0. 실제 CSS animation timeline seek와 production private background track 대조, premultiplied color paint1byte / reverse cubic duration0.0001ms. selected+hover는 전환없음(native duration0)도 별도로 확인한다. DB 값 비교의 허용오차와 무관한 렌더 직렬화 경계다.
- 실제 입력: `source/tablemappingrow-input-comparison.json`, 양OS18snapshot씩36개 failed0. label held는 선택하지 않음→release1회, outside drag/cancel은 무선택, 대상cell blur, change요청만1회, Enter체크무변경/focus-visible, Space선택, Tab→change, Enter/Space 독립 요청, reduced hover/leave. 증거파일의 수정시간이 최신 published product DLL보다 이후임도 검사하여 옛 표본의 우연한 통과를 거부한다. outside-held paint는 마지막 timer의 중간 전환 표본이므로 terminal color로 주장하지 않는다. release 후 최종 transparent를 확인했다.
- held/leave 교정: child capture가 row의 native IsPointerOver를 지우는 현상은 부모의 pointer geometry로 추적했다. Windows uncaptured leave가 이전 좌표를 제공하는 경우는 exit 자체로 해제한다. captured drag는 현재 pointer 좌표로 판정한다. locked child는 변경하지 않았다.
- 입력 도구 실패(scope/geometry/파일읽기 경합/Parallels InvalidArgument)는 통과로 사용하지 않았다. 모호한 CLI 실패 후 실제 count를 읽어 수행되지 않았음을 확인하고 한 번만 재입력했다. IAB 모달 Escape 직후의 즉시 snapshot은 중간 상태였으므로 detached/visible 대기 후 Enter/Space request를 재검증했다. 원본의 이상 동작으로 재해석하지 않았다.
- paint: `screenshots/tablemappingrow-paint-pair.png`의 실제 source1CSSpx / native2x→logical 비교를 확인했다. 위치·색·glyph 배치는 맞으며 글꼴/사선 AA 때문에 bit-perfect raster를 주장하지 않는다.
- 확인 환경: macOS M4 Pro / Windows11 ARM64 guest, DPR2. Windows fixture는 win-x64 self-contained를 실행했다(artifacts의 역사적 windows-arm64-check 이름은 실제 RID 표시가 아니다). 다른 DPI·배포 OS/architecture는 상위 배포 검증 범위다.
- 구조 경계: 원본 table의 공유 auto-calculated4열을 parent input으로 받는다. 행 자체를 클릭해 대응 modal을 열지 않는다. 선택 hit area는 첫cell의 min44 label만, target/method는 비작동, change는 quiet button이다. 반쪽 collapsed border는 행의 share이고 전체 table border/radius/header/empty/search/bulk/column allocation/announcer·모달 apply는 아직 완료가 아니다.

양OS row verification 창을 닫았고 source fixture를 보존했다. 기존19LOCK/원본 CSS·JS 해시를 유지한다. 이 행을 잠가도 전체 앱 복제 완료가 아니며 AGENTS의 임시 복구 제약은 유지한다.
