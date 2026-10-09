# BulletList
LOCKED · 2026-10-08. 원본 detail body의 ul.muted padding-left20. 기본 ul displayblock/margin14px 0, li displaylist-item/font14/lineheight22.4/colorb5bac1/list-style-type disc/outside. 구조 ul → li(text); 항목 추가/삭제와 줄바꿈은 Typography 계약을 사용한다. 기본 목록은 동작/hover/animation없음. Root는 List peer, 항목은 ListItem. UI를 재설계하거나 categorysection LOCK 내부를 변경하지 않는다. 원본30과 직접 JS60/CSS body/default 목록을 확인했다. 다음: 실제ul/li computed/box 확인 → 24 source/native조건과 언어/목록 갱신/empty 접근성 → LOCK.

## 비교 중 기록
첫 Mac24조건의 모든 box/문구는 일치했으나 markerLine 진단 문자열이 .NET 부동소수점 일반 직렬화로22.400000000000002px가 되어 CSS22.4px와20목록에서 달랐다. 명목 CSS px 진단을 소수점3자리로 직렬화하고 실제 linebox22.3984375의 엄격한 기하 대조는 유지한다. 배열 항목은 항목별 비교하도록 측정기를 교정해 차이 위치를 분명히 한다. 후보 UI 수치나 비교 허용오차를 바꾸지 않는다.

## 완료·LOCK
원본30 actualul font14/line22.4/margin14 0/paddingLeft20/listoutside disc/markercolorb5bac1 확인. 양OS 각각24조건(lang2×width438/570×5분류+empty)의 list/li box·순서·문구·줄바꿈·색/marker글자크기 차이0. `source/bulletlist-{mac,windows}-{source,comparison}.json`, native의 `bulletlist-native.json`. 동일 attached 목록의24회 갱신에서 실제 peer List→ListItem 개수가 source 항목개수와 같고 empty는0: `bulletlist-peer.json`. 양OS `bulletlist.png` 시각 확인. 기본 브라우저 disc는 native systemfont의 •로 표현하고 default font/marker raster와 OS AX marker역할 표현 차이는 MASTER_RULES12 범위로 유지한다. li/텍스트의 크기와 위치·색은 면제하지 않았다. passive 목록이라 hover/pressed/action/animation없음. 외부 margin14는 CSS collapse의 대상이며 부모가 sibling max-margin을 조립한다(빈목록 top/bottom collapse 포함). 기존 CategorySection의 private 목록/LOCK을 변경하지 않았다. 보호 `source/bulletlist-lock.json`.
