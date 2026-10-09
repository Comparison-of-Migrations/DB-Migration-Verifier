# Notice

LOCKED · 2026-10-07 · SvgIcon/Typography/Surface/Motion LOCK 계약을 사용한다.

## 현재 원본 조사

실행 중인 Preflight warn DOM/CSS/box를 다시 확인했다. notice(title,text,kind)는 div.notice(role error:alert/기타note) → SVG18(warn/error:warning,normal/ok:info) + div → strong inline13/600 + p block12/400. root font14/unitless1.6, pad14vertical16horizontal/border1/r8/gap12/margin-top20/align-items:center, directSVG margin-top0/align-self:center. childdiv는flex:1이아니며intrinsic maxcontent폭을사용하다available폭에맞춰shrink한다. 실제원본 warn폭860/높이71.59375, 내용div폭497.0859375/높이41.59375/local47,15,SVGlocal17/26.796875. strongglyphbox local47/18.5/h15, p는첫라인22.3984375뒤19.1953125. root14 strut와inline strong13의baseline 관계를검증해야하며, 단순13/12 StackPanel로추측하지않는다.

palette: 현재warn bg#39332b/border#6b573b/fg#f0c47c,strong#f2f3f5. 기타normal/ok/error는원본final CSS와actual사례를더확인한다. root animation page-in200ms SourceEase는LOCKED Motion계약, reduced는그계약을따른다. title/message/role/전체폭은부모가제공한다. notice helper에는button slot이없으므로unused .notice .btn을추가하지않는다.

## 다음 행동

현재 Notice는source 조사단계이며native후보없다. 모든실사용kind/KOEN/폭 및titleinline baseline marker의고립원본을수집한다. 명세확정→작은textline/본문→icon row/surface순으로조립·양OS대조한다. SidebarItem까지LOCKED이고상위Sidebar/MainWindow/기존Noticehelper를아직수정하지않았다. 전체재개제약유지. 현재fixture서버23663.

## 재개 확인 · 2026-10-07
AGENTS→MASTER_RULES→상태→명세→실행 중인 Preflight warn/원본 CSS·helper·호출부→기존 MainWindow Notice→차이를 실제 읽었다. 15 LOCK 해시 통과. 현재 단계는 원본 inline baseline·kind·줄바꿈 고립 측정이며 기존 상위 helper는 수정하지 않는다. 공식 macOS 캡처 API 허용은 NumberInput 검증 두 창에 한정하며 이미 기록된 범위를 유지한다.

## 구현 전 계약 · 고립 원본 34상태 양OS
240/452/860폭, KO/EN, warn/error/ok/normal, 빈 제목·빈 본문·공백·개행 collapse를 측정했다. 제목은14px/1.6 부모 strut의 줄 높이22.3984375(2배율), 본문19.1953125. inline 마지막 baseline는 Mac 첫줄body상대16, wrap마다22.3984375 증가. Windows 실제값은notice-windows-source.json에 보관한다. 내용은max-content를available(root−64)에clamp하며 아이콘18과gap12, surfaceborder1+pad16/14. 빈 내용일 때도icon18이48px 높이를 만든다. normal bg#2b2d31,okfg#7ee4b0, 나머지는 최초 명세palette를 확정한다. strongcolor#f2f3f5. Source has no hover/pressed/focus/action; updates replacecontent and mountrestarts200ms Page/reduced skips.
작은 내부 NoticeText부터 구현한다. 14px 부모 strut는LOCKED Typography의layout-only helper로baseline을계산하고 화면/접근성 내용에는 넣지 않는다. title13/600의lineHeight는strut높이, strut와title baseline차이만glyphtransform에적용한다. 그후 LOCKED SvgIcon/Surface/Motion조립. 기존MainWindow helper와다른LOCK코드변경없다.

## 길게 이어진 오류 문구 · 구현 전 추가 근거
42상태로확장. 34상태양OSgeometry/baseline/palette/update완전일치. 그러나원본directdiv min-width:auto이므로unbrokenDiagnosticToken는240폭에서도833.171875폭으로넘친다(Mac). 임의overflow-wrap/축소를넣지않는다. LOCKEDTypography의wrap-with-overflow를0폭으로측정해min-contentadvance를구하고max-content/availableclamp의하한으로적용한다. 기존34상태및긴정리보고/토큰다시검증후판정한다.

## 직접 하위 예외 조사 대기
42상태확장으로Maccleanup의T-001줄바꿈불일치를발견했다. Notice는미완료로유지하고Typography만활성으로전환하여구체적인소스break근거를조사한다. intrinsic overflowwidth수정후Windows42확인준비완료. 서버현재94628.

## 하위 재LOCK 후 단일활성 재개
Typography숫자경계교정/회귀완료후Notice만다시활성. Mac42endpoint/42reuse전체일치. Windows42는longword2표본의body폭이176으로clamp되어남았다. Typography의0폭측정은WindowsTextLayout이0줄을반환하므로min-content측정constraint를1 LayoutUnit(1/(scale×64))의양수로설정한다. sourcemin-content자동폭과layoutoverflow를재대조하고기존34/42결과를다시검증한다.

## 접근성 및 mount 검증 전 대응
원본은note/erroralert의passive컨테이너로키보드action없다. Native는Group peer의ClassName Note/Alert 및LiveSetting Off/Assertive로대응하고원본문구를Name으로전달한다. Glyphstruthelper는opacity0/Raw로content접근성에서제외한다. mount는잠긴Page200ms실재생, property갱신은DOM내용교체와같이명시적인부모remount없이entry를재시작하지않는다. 부모render의교체는나중에페이지조립에서검증한다. 자동actual창probe로mount·완료·갱신·detachcancel·remount·reduced및peer실값을검증한다.

## 최종 판정 · Notice LOCK
42 endpoint/42same-objectupdates 양OS 전체geometry·내용div max/min-content·3폭·KOEN·4kind·빈제목/본문/공백collapse·inlinebaseline·palette·border1/r8/pad16/14/gap12/margin20 정확히대조했다. notice-{mac,windows}-comparison.json. glyphstrut는14px기준line22.3984375/제목13/600,본문12/19.1953125이며inlinebaseline다름을보존한다. longwordoverflow도source와동일.
actualowned창의init .35/Y6→완료1/Y0, propertyupdate playback유지,remount새playback,detachcancel복원,reducedskip,peerName/GroupNoteAlert/LiveSetting/비focus를양OS검증:notice-{mac,windows}-lifecycle/notice-lifecycle.json,notice-lifecycle-comparison.json. 소리screen-reader전달이나새배율을검증했다고주장하지않는다. motionduration200/custom곡선은LOCKEDMotion계약을조립하며source실제computedanimation이같다. hover/pressed/disabled/focus/button상태는원본helper에없으므로N/A,임의추가없음.
notice-mac-overview.png는원본1x/native2x다운샘플의시각대조이며같은줄바꿈·제목/본문·아이콘중앙·여백/곡률을직접확인했다. exactraster equality증거로사용하지않는다. Surface/Svg/Typography/Motion계약을사용하고원본CSSJS/기존MainWindowhelper는변경하지않았다. 보호해시notice-lock.json. 다음단일활성DbmsOption,전체재개제약은유지한다. 현재fixture서버17927.
