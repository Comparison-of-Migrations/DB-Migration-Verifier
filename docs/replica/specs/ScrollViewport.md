# ScrollViewport

LOCKED · 2026-10-07. Sidebar62originalgeometry조사에서발견한직접하위 overflow:auto 계약. 원본 .rail/.content의독립 wheel/thumb/keyboard/bring-into-view/gutter를primitive로먼저검증한다. Sidebar는WAITING CHILD,동시상위구현없다. 기존nativeScrollViewerstyle로source를추정하지않는다.

## 현재 상태 · 이 절이 아래 과거 기록보다 우선한다
- 2026-10-07 사용자 추가 조건에 따라 OS 기본 scrollbar/renderer의 표현 경계는 강제 픽셀 복제를 요구하지 않는다. 아래 최종 판정이 과거 WAITING PAINT/재조사 문구보다 우선한다.
- 양OS18 layout +14 boundary 총64조건 및 minimum4×2 총8조건 차이0. 실제 wheel/keyboard/Page/Space/thumb drag/track click/hold/release/detach/포커스 경계 검증은 기존 연결 증거를 보존한다.
- Mac overlay6 endpoint/hide/reenter/hover/wheelcancel, 양OS held detach/track, Windows thumb4상태 실제 paint 통과. Mac thin cap48pixel/max20 및 Windows arrow AA1–2는 기본 renderer 표현 차이로 기록한다. 픽셀 완전 일치로 주장하지 않는다.
- 직접 부모 Sidebar62×2조건에는 수평 overflow가 없다. MainContent의 다른 overflow를 이 LOCK에서 검증한 것으로 확장하지 않는다. 해당 부모 착수 시 원본부터 조사한다.
- 다음은 Sidebar 조립. 전체 복제 완료 전까지 AGENTS의 임시 복구 제약은 유지한다.

## 原本 UA 조사의 근거
Mac/Win각18programmatic layout/offset 상태: source/scrollviewport-{mac,windows}-source-layout.json. viewport400/660/960,child200/1140+원본padding총60, top/123.456/end 요청. overflow時clientWidth220/205; nooverflow220. 123.456요청은양OSDPR2에서123.5로devicepixel 반올림. 최대offset800/540/240, nooverflow0. readonlyscrollHeight는viewport이상이며int로표시한다.

원본fixture는protectedsidebar-original.css의 .rail overflow:auto/padding을그대로쓰고자식은예시줄무늬box뿐이다. 이것이Sidebar상위명세대신이아니며OS UA 계약을고립측정한다. Chromium의scrollbar_theme_aura.cc/native_theme_aura.cc/native_theme_base.cc공식source를algorithm참고로보존했다. 다운로드시PythonSSLcert로첫읽기가실패해검증을해제하지않고시스템curl의기존trustedcert를사용했다. rawbase는429여서공식googlesource동일파일로읽었다. 실제실행paint가source참고보다우선하며현재UApaint/actualinput은검증중,후보아직없음.

## 재개 확인 · 2026-10-07
AGENTS → MASTER_RULES → COMPONENT_STATUS → 이 명세 → protected .rail 원본/실행 중 ScrollViewport fixture → 기존 후보 부재를 확인했다. verify_lock.py 23개 보호 범위 통과. 원본과 native 후보의 차이를 아직 검증하지 않아 IN PROGRESS를 유지한다. 사용자 일괄 허용이 이미 정본에 기록되어 있으며 재질문하지 않는다.

Windows actual source는 Fluent scrollbar다. Aura primary reference를 paint 기준으로 채택하지 않는다. 실제 gutter15px/track#2c2c2c/thumbnormal#9f9f9f/hover#d1d1d1, Fluent primary button18px/thumb9px/minlength17px를 대조 중이다. source programmatic offset 양OS18개 및 실제 Mac ArrowDown40/PageDown380, Windows trusted wheel(deltaY100px) 끝점100을 기록했다. Mac IAB 움직임 캡처에는 overlay thumb가 보이지 않아 paint 값을 추정하지 않는다. 다음은 source arrow/drag/pressed 및 Mac overlay 노출 확인 → 후보 구현 → 양OS 비교다.

## 후보 구현과 현재 검증 · 2026-10-07

- 구현: `src/Verifier.Desktop/WebScrollViewport.cs`. 내부 content host는 padding·clip·OS gutter·extent·device-pixel offset을 처리한다. Windows Fluent/Mac overlay의 후보 paint가 추가되었다. 다른 LOCKED 컴포넌트는 변경하지 않았다.
- `ScrollViewportProbe.cs` / `compare_scroll_viewport.py`: 최종 후보 양OS 각각18개 원본 layout/programmatic-offset 상태에서 차이0. 출처는 `source/scrollviewport-{mac,windows}-source-layout.json`, 결과는 `{os}-layout-comparison.json`이다. 실제 키보드/휠/드래그 구현 완료를 뜻하지 않는다. 빌드 경고0/오류0, verify_lock.py 23개 범위 통과.
- Windows 실제 원본: arrow-down은 기존100→140(+40), drag-bottom은140→800, pressed-thumb는offset0 유지, pressed-arrow는0→40. 각 JSON/PNG는 source와screenshots의 `scrollviewport-windows-source-*`에 보존했다.
- Mac 실제 Chrome: CUA wheel(deltaY80/trusted) 0→80, 공식 CGEvent pixel wheel(deltaY100/trusted) 80→180. 공식 캡처로 width7px의 숨겨지는 thumb와 wheel-hover의 width11px/track 표시를 확인했다. `tools/MacControl/scroll_source.swift`는 Chrome path/정확한 title·URL/pane220×400/foreground/hit/포인터 복귀/단일창 캡처를 검사한다. OS 권한 설정은 바꾸지 않았다.
- Chromium main은 설치된 브라우저 revision이라는 뜻이 아니다. 실제 Windows 화살표는 main의9px/-1/+1 기준과 달라, 보존한 원본 crop와 Segoe Fluent Icons raster 진단을 대조했다. normal em7.5/x3.75/baseline12.75/hintNone/subpixel=true, down-pressed em6.5/x4.25/baseline12.25가 확인됐다(`scrollviewport-windows-glyph-fit.json`). 현재 후보에는 normal을 반영했다. pressed의 실제 연결과 up-pressed는 남아 있다.
- thumb 길이/위치의 rounding은 CSS px가 아니라 실제 device px에서 이루어진다. DPR2 Windows 길이121.5px, Mac logical thumb133.5px에서 top/bottom2px inset을 뺀 paint129.5px다. 후보는 RenderScaling에서 round/truncate한 뒤 logical px로 되돌리도록 교정했다.
- 최종 Windows top 실제원본/후보 PNG 쌍: `screenshots/scrollviewport-windows-top-pair.png`(좌원본/우후보), 직접 열어 확인했다. content 영역은 pixel 차이0. thumb 경계67pixel(max채널33), up40/down43pixel(max채널2) raster 차이가 남는다. `source/scrollviewport-windows-top-paint-comparison.json`. 이를 완전한 paint 일치/LOCK 증거로 사용하지 않는다.

## 다음 행동 · 단일 활성 유지

1. 실제 후보 창에서 양OS wheel event 단위·focus·keyboard·scrollbar pointer를 측정하고, 원본의 이동량/animation/drag/hold/release/clamp/bring-into-view를 연결한다. 읽기/빌드는 병행할 수 있지만 실제 UI 입력과 foreground를 사용하는 검증은 순차 실행한다.
2. Windows thumb raster 차이, Mac overlay hide/expand/cancel/재연결 및 actual paint를 대조한다. normal 캡처 하나로 모든 상태를 통과 처리하지 않는다.
3. fractional extent/동적 내용/resize/no-overflow/부모 실제 배치를 확인한다. tiny scrollbar 및 수평 overflow는 현재18개 범위에 포함되지 않으며 필요한 원본 범위부터 조사한다.
4. 미검증이 해소된 뒤 ScrollViewport를 LOCK하고 Sidebar62개 조립/입력 검증으로 간다. Sidebar 후보를 먼저 만들지 않는다. 전체 복제 완료 전 AGENTS 임시 복구 제약은 유지한다.

## 재개 확인 · 실제 입력 단계
AGENTS/MASTER_RULES/상태/이 명세, 실행 중 원본 #scroll-target의 DOM·computed overflow:auto/padding30×14/color-scheme:dark/220×400, 후보와 직접 UA 알고리즘을 다시 확인했다. 보호23개 변경 없음. 먼저 고립 후보의 실제 휠 단위를 기록하는 persistent probe를 추가했으며 입력 처리는 측정 뒤 구현한다.

## 実入力 후보 연결 · 2026-10-07
원본100px trusted wheel은 실제 Avalonia에서 Mac deltaY=-2/Win=-1로 도착한다. 변환50/100을 후보에 연결했다. Windows 실제 후보 wheel-down100/wheel-up0/arrow-down40/drag-bottom800; Mac 실제 후보 wheel-down100/wheel-up0/body-click focus/ArrowDown40/PageDown420(기존40+380)/End800/Home0 확인. 후보 원시 events/frames는 source/scrollviewport-{os}-input/actual-*.json에 보존한다. sourceMacwheel은 즉시, sourceWinwheel은 eased 이동이어서 후보 플랫폼 분기를 연결했으나 retarget/실제 animation 시간 정밀 비교는 완료 전이다.

진단 JSON 동시 읽기에 Mac PageDown 기록이 부분 저장된 것을 발견했다. 해당 기록은 통과로 사용하지 않았으며 probe를 임시 파일→원자 교체로 수정한 뒤 새 PageDown420 기록을 다시 검증했다. 다른 잠긴 code는 변경하지 않았다.

Windows 원본 pressed-up도 실제 촬영했다. hold-down650ms는 첫40 이후 반복 이동하며 source frames266.7→60/400→166.5/600.1→326.5; source actual 기울기 약800px/sec. Chromium 공식 cc/input/scrollbar.h의250ms지연·delta×20 compositor velocity를 보존/기록했다. 후보에 hold반복/해제/밖으로 이동 시 정지 연결 중이며 아직 actual 검증 전이다.

## 페이지 이동의 OS 차이 확인 및 교정
실제 Windows source400px의 PageDown350(ArrowDown40 후390)을 확인해 후보 공통380이 잘못된 것을 발견했다. 추가660px/child3000 source는 Windows577.5, Mac640이다. source/scrollviewport-{os}-source-page-tall.json 참조. Chromium scroll_utils.h/cc는 device 길이의0.875·Mac 최대40devicepx겹침을 정의한다. 따라서 후보는 RenderScaling에서 계산한 뒤 logical px로 변환한다. 최신 Windows 실제 후보400→350,660→577.5 모두 원본과 일치. 첫 Mac Tall 입력은 버튼에 포커스가 남아 offset0/입력 events없음이어서 통과가 아니며, helper가 pane 실제 클릭·포커스 후 키를 보내게 교정하고 재측정한다.

원본 refresh 직후 UIA focus 활성화가 늦어 foreground guard가 차단한1회는 입력 성공으로 사용하지 않았다. 명시한150ms 안정화 뒤 title/path/foreground guard로 재확인했다. CUA가 닫기 클릭 후 창 상태를 바꾸지 않은 경우도 성공으로 사용하지 않고, 정확한 owned window/Close 버튼의 공식 AXPress로 닫았다. Tall 측정에서 뷰포트 일부가 창에 잘릴 수 있으므로 전체pane 캡처를 만들지 않고 입력/offset 기록만 읽는다. 프로젝트 밖 픽셀을 캡처하지 않는다.

## 경계 조건 및 포커스 재검증
Mac Tall의 실제 pointer/key events가 도착한 새 기록에서640/focused=true를 확인했다. 원본과 같으며 이전 offset0 측정은 실패 기록으로 대체했다. 추가 source14개를 양OS의 실제 원본 DOM에서 수집했다: 내용 축소/복원,400→660→960resize, NaN/+Infinity/-Infinity,1140.375px소수점 extent의끝점, nearest bring-into-view. 원본의 비유한값은 모두0이어서 후보의+Infinity최대값처리를0으로 수정했다. Mac native14개 차이0. Windows는 최종 후보로 재검증 중이다. 원본/후보 boundary 파일·비교기는 source/scrollviewport-{os}-source-boundaries.json, {os}-native/scrollviewport-native-boundaries.json, tools/ReplicaProbe/compare_scroll_boundaries.py다.

macOS 얇은thumb 실제후보는 strict pointer/capture guard를 통과한 새100px wheel capture로 확인했다. 원본offset180과 후보100의thumb원점을정규화해폭7px·길이129.5px·색을 비교했다. source/scrollviewport-mac-thin-thumb-comparison.json: 캡 AA 경계48pixel/max20차이. Windows는 source와후보 실제창 모두0offset에서content차이0,thumb경계67pixel/max33,arrowmax2. Fluent thumb를공식SkRRect(999)와같은primitive로paint해도경계차이가남는다. 어느쪽도bitexact paint/LOCK 완료로 주장하지 않는다.

## macOS 드래그 및 트랙 교정
Source Chrome의 AXPress는 성공 반환 뒤 실제 저장이 갱신되지 않는 경우가 있었으며, 이를 완료로 쓰지 않았다. 프로젝트 측정 버튼의 CUA 실제 클릭으로0offset 새 기록을 만든 뒤, 공식 CGEvent leftMouseDragged와 owned-pane guard로 드래그했다. Source800/후보800을 새 JSON으로 확인했다. 도중 다른 앱이 측정 버튼 좌표를 가린 guard 실패도 입력 성공으로 쓰지 않았다. 원본 버튼 조작은 CUA, 프로젝트 스크롤바 조작/캡처만 공식 helper로 나누어 검증했다.

동일800offset의expanded track 실제캡처에서 배경그라데이션방향과우측border누락을 발견했다. NativeThemeMac PaintScrollBarTrackGradient는vertical scrollbar에서세로gradient이며, inner51/229뿐아니라outer40/216도그린다. 후보의가로그라데이션을세로로교정하고우측1pxborder를연결했다. 재캡처전의차이를원본과일치한다고처리하지않는다.

또한 Windows 휠/스크롤바 click의inverse duration식을primary의14/60초−delta/3600초(100~200ms)로바로잡았다. 키보드는150ms constant다. 연속 휠 retarget시원본의현재속도·2.5duration bound·초기bezier기울기를유지하도록후보에연결했으며actualburst/반전검증은다음단계다. 기존code는retarget때정지기울기로다시시작하는차이가있었다.

## 재개 확인 · 연속 입력/상태 경계 단계
AGENTS → MASTER_RULES → COMPONENT_STATUS → 이 명세 → 실제 #scroll-target의 DOM/computed overflow:auto·padding30×14·220×400 → 현재 WebScrollViewport를 다시 읽었다. verify_lock23 통과. 직전 Mac native drag 작업의 fresh events/offset800 및 정상 종료를 확인했다. 양OS18배치·14경계는 통과했으며, 연속 입력/포커스/자동숨김·캡처해제는 아직 미검증이어서 잠그지 않는다.

Mac track 교정 후 동일800offset 실제 캡처를 다시 대조했다. thumb 차이는841→80pixel(max8), track 평균채널차이는0.307이다. 원본 inner/outer border·세로gradient 방향을 복구했지만 색공간/AA·stripe fractional raster 차이가 남는다. 새 증거 source/scrollviewport-mac-drag-paint-comparison-latest.json 및 screenshots/scrollviewport-mac-expanded-pair.png. 단순 pixel0 주장 없이 실제 bounds/색 정의와 raster 차이를 구분한다.

## 実移動時間とSpace/outlineの差分
連続入力の初回sourceは前のno-overflow状態を残していた。offset0/scrollHeight400の記録は入力一致の証拠に採用せず、helperのbaseline resetに既存Restore overflowのUIA Invokeを追加してfresh1200/205を確認し再収集した。候補も同じ開始条件にする。
単一trusted wheel100の実曲線fitは174ms、arrow40は197msだった。primaryのinverse durationはcompositor device-pixel deltaを使うため、候補のlogical deltaだけの200msは不正確だった。RenderScalingを掛けて100px/DPR2→177.78ms、40px→200msへ修正した。fitには原本compositor/DOMイベントの時差があるので174msを任意定数にせず、各segmentの実入力で再検証する。probe毎16ms全JSON書き込みが候補の測定負荷になっていたためframe採取を維持し保存のみ200msに抑え、capturedAtUtcを付けた。
実原本でbody click→Spaceは380、Shift+Spaceは逆方向、focus-visibleは#5865f2/3px/offset3。候補にはSpaceが欠けており、owner clipがoutlineを切る構造だった。content hostのclipを維持しowner外outlineを描画、ownerフォーカス時だけ表示する候補を追加した。実入力再検証前なので完了扱いしない。

## フォーカス境界の原本実測
Mac IABの原本でno-overflow body clickはBODY focus/owner false。focusable childを一つ含む400pxoverflow fixtureではRecord→Shift+Tabがchildへ移り、さらにShift+TabはBODYへ移る。parentは余計なTab停止点にならず、本文clickでもowner focus=falseだった。候補の無条件Focusable=trueはこの契約に合わないため、overflowがありvisible/enabled/tab-focusable descendantが無い場合だけFocusable/IsTabStopを有効にする。今後 Sidebarを組み立てる際の余計なTab停止を防ぐ原本根拠であり一般UXによる変更ではない。
Windows候補の遅延ファイル読みによるfocused=falseは、helper終了後のCLIウィンドウによるactivation変更の可能性がある。foreground guard内でnative Record stateのUIA Invoke→fresh JSON snapshotへ変更し、過去file/読み遅れを成功判定にしない。最初のfresh guard失敗は記録更新を確認できなかったため不採用。

### フォーカス測定方法の訂正
locator.pressは要素を強制focusしてからキーを送るため、最初のbody-click→Space記述のfocus-visible=trueは実際の本文clickの証拠ではなかった。reload→本文clickだけ→readではBODY focus、Tabを挟まないnative pressKey(null,Space)でもBODY focus/outline無しのまま380へ移動した。Record→Shift+TabならASIDE focus/outline#5865f2/3px/offset3になる。child有りではASIDEのTab停止もない。これらを別状態として扱う。Avaloniaではpointer-selected scroll boxへのkey配送に内部focusが必要なので、pointer focus時のoutlineを抑え、Tab/programmatic focusだけ表示する。FocusableとIsTabStopを分け、子buttonがある場合の余計なTab停止を外した。DOM activeElementとnative内部focusを同一値の証拠にしない。
Record stateのUIA Invoke自体がnative buttonへfocusを動かすことをfresh blur eventで確認した。診断用RecordのみFocusable/IsTabStop=falseにして収集操作の介入を避ける。候補frame snapshotとOffsetChangedのchangesを別記録し、timerが前frameの値を読んだ遅れと実際の状態更新時刻を区別する。

## 最新の実検証と回帰
Windowsの中間縮小/復元・child Tab・no-overflow本文/Spaceの3件はfresh source/nativeを比較して全条件通過した。source/scrollviewport-windows-state-boundary-comparison.json参照。中間縮小は実際に原本/候補が一度0より大きく動いた後0へ戻り、復元後も再開しない。child Tabは原本scroll-child focus eventと候補ScrollChild active/no-extra-Tabを確認。no-overflowはmaximum0/clientWidth220を維持し、本文clickが以前のbutton focusを解除してSpaceで別のbuttonを誤作動させない。
Windows UIAが同じpid/同じHWNDを二度返した場合があった。実際の二窓ではないことをreadonly列挙で確認し、同一handle/pidだけ重複除去した。異なる窓/title/pathのunique guardは維持する。PrlJob InvalidArgumentのshort-key初回はsnapshot不存在を確認した後にだけ再入力し、新鮮な成功記録を収集した。
最終Windows18 geometry＋14 boundaryは最新candidateで差異0。Mac同じ回帰を実行中。locked23コードは変更無し。Mac hide600は公式capture時にpointerが(449,408)から(451.543,826.332)へ移動したため2回reject、画像を成功として保存していない。pointer/foreground guardを弱めず、別の検証を先へ進める。

## 押下端点の差分修正
Windows上向きarrowをoffset0で押したfresh rasterは原本normalと同一だった。候補は動けない端点にもpressed6.5px/209色を描いており138pixel/max104の実差分を発見した。source原本pressed-upとresetのcropは同一159色/7.5pxである。上端/下端で移動できない方向はnormalを維持し、移動可能方向だけpressedを描くよう候補を修正した。既存hoverをthumbしか追跡していなかったため、arrow hoverもpart別に追跡する。新しいデザインではなくUA hover/端点の実測補完。再ビルド・actual再比較前なので完了扱いしない。
Windows held-pauseは両側で一定区間のflat停止、戻ると約800px/sec再開を確認した。原本390~560msは127固定/20frame、候補108固定/6frame。held時間とpointer配送の差があるため最後の293.5対276.5を同一値とは主張せず、停止区間と再開の速度で検証する。

### 端点/hoverの再検証完了
最新候補のfresh Windows上端hover/下向きenabled hover/上向きenabled pressed/上端pressedを原本と再比較した。glyph cropの最大channel差は順に2/1/1/2。端点pressedの旧max104差を解消し、既存normalのAA差だけになった。source/scrollviewport-windows-arrow-state-comparison.json。enabled状態は別名pressed-up-enabledに保存し、offset0の端点証拠と混同しない。source/nativeのenabled末尾offset60も一致。
Mac hide再試験ではeventのlocationをowned paneへ明示し、captureが完了するまで単一呼出しにしたがforeground guardで拒否された。元のhover/foreground/入力解放guardは保つ。pointer移動の原因は確定していないのでユーザー操作やOS設定のせいと断定しない。

## DOM除去/再挿入の実差分
Windows原本をwheel animation中にremove→200ms後に同じnodeをappendすると、一度49まで動いた後0となり、再挿入後も0だった。候補はcallback取消だけで51を保持していた。source/scrollviewport-windows-source-detach-pending.jsonとactual-detach-pending-before.json参照。DetachedFromVisualTreeでoffsetを0へリセットし、host再配置と通知だけ行いDidScroll/overlay再起動を起こさないよう修正した。旧callback revision無効化も維持する。actual再検証前なのでLOCKしない。

## 재개 확인 · 2026-10-07 후속 작업
AGENTS→MASTER_RULES→상태→명세→실행 중 원본 aside/overflow:auto/padding30×14/220×400→WebScrollViewport를 다시 확인했다. LOCK23 보호 검사는 통과했다. Windows 이전 검증 창의 안전한 Close 요청은 성공했다. detach 교정 후 회귀 및 Mac 재연결/숨김 검증을 계속한다.

## 재연결 교정 최종 확인
Windows animation 중 detach→재삽입 원본/후보0, Mac settled800→detach→재삽입0을 fresh actual 기록으로 확인했다. source/scrollviewport-{windows,mac}-detach-comparison.json. 최신 후보 양OS18배치+14경계 총64조건 차이0. 보호23개는 유지했다. Mac hide600 후보는 strict pointer/capture guard를 통과해 이미지/시점 기록을 저장했지만, source hide600과 후보 hide850은 포인터 이동 guard로 거부되었으므로 숨김 일치 증거가 아니다.

## Mac 연속 프레임 측정과 재진입 차이
단일 캡처 초기화 시간이 fade 관찰에 섞이지 않도록 Apple SCStreamOutput/CMSampleBuffer presentation timestamp 공식 API로 입력 전에 owned window stream을 시작했다. exact path/title/URL/pane/foreground/hit/pointer/release guards는 유지하고 pane만 증거로 보존한다. source62/native61 hide frames를 얻었다. 단일600ms 캡처는 실제 프레임 시각이 없으므로 duration 근거로 쓰지 않는다. 공식 API 근거: https://developer.apple.com/documentation/screencapturekit/scstreamoutput 및 scstreamconfiguration.
650ms fading 중 track 재진입에서 source는 즉시 thumb alpha1로 복귀하는 반면 후보는 흐린 상태를 유지했다(약695ms source145/146/148, 후보88/88/91). source109/native108 actual frames, candidate-reenter-stream-before.json 보존. primary ScrollbarAnimatorMac MouseDidEnter→FadeOutAnimationCancel의 thumb_alpha1/기존trackalpha1 계약과 일치하는 실제 차이이므로 후보를 교정한다. 다른 LOCKED 컴포넌트는 수정하지 않는다.

## Mac 확장 중 이탈/확장 후 재진입의 실제 차이
추가 actual stream: source/candidate hover-short110frame, reenter-expanded109frame. 350ms 진입→450ms 이탈 시 source 확장은 계속되어650ms에 전체width11이지만 후보는width를 중간에서 멈췄다. 확장 후 fade중1550ms재진입은 source 기존width11/trackalpha1을유지하고 후보가width7/trackalpha0부터재시작했다. before JSON과 rightBand(pixel200..219)를보존했다. primary독립track/expand timer와FadeOutAnimationCancel계약으로수정한다. source 실제 관찰이 근거이며 UX 개선을 추가하지 않는다.

## 이번 후속 작업의 최종 증거와 재개 지점
- 최신 빌드 mac osx-arm64 / Windows win-x64 모두 성공. Windows runtime 전송은 두 DLL 해시 검증을 거쳤다. 양OS18+14 최신 회귀 차이0. Windows guest는 ARM64 VM이며 win-x64 프로그램 실행 증거다; Windows x64 실기기 검증으로 과장하지 않는다.
- Mac overlay 6조건 / focus2조건 / 양OStrack2조건 / 양OSheld detach가 실제 입력에서 통과했다. 두 후보 창은 안전하게 닫았다. source 측정 창은 다음 비교를 위해 보존한다. 원본 웹 시안은 수정하지 않았다.
- Windows held-detach 첫 공식 CLI 호출은 InvalidArgument로 실패했다. shared source record 및 capture에서0/빈frames를 확인한 후 재실행한 fresh43positive+96zero source와18positive+38zero native만 통과 근거로 썼다. Mac 창 종료 직후 재실행의 -600은 실제 종료를 readonly 확인한 뒤 새 앱에서 short-key를 측정했다. 실패 기록을 성공 증거로 사용하지 않았다.
- Windows cap의 채널 행을 실제 source/candidate reset 이미지에서 비교했다. source는 대칭이고 candidate는 CPU 경계 양자화의 비대칭을 보였다. 단순 linear-sRGB 혼합 가설은 평균 오차를 줄여도 max17.94가 남아 원인 확정/교정 근거가 아니다. 색상이나 LUT를 임의로 바꾸지 않았다. 다음 조사에서 현재 실제 이미지 및 Fluent drawRRect 계약부터 재개한다.

## Windows cap 진단 · 동일 기하의 raster 경로 비교
원본 cap을반지름9physical px의정확한원형면적 coverage(256×256각pixel샘플)와비교했을때MAE0.408/max5.080이었다. linear-sRGB혼합예측은MAE1.768/max17.018로더나빠임의gamma교정은배제한다. 원본과같은capsule기하를반원+중앙직사각형으로그려Skia의oval전용raster경로와rrect경로를진단한다. 크기/색/동작을바꾸는개선이아니라남은paint원인의비교후보이며실측전통과로쓰지않는다.

## cap analytic coverage의 실제 발견
반원 면적샘플 대신 Skia GPU의circle distance coverage clamp(radius+0.5−distance(pixel center),0,1)를비교하니source상단cap162pixel의채널값이전부차이0이었다. source/scrollviewport-windows-cap-analytic-diagnostic.json. primary Skia GrCircleEffect의radius+0.5/clamp distance 코드도 같은식이다: https://skia.googlesource.com/skia/+/0307e9114c30263a9f88f9a084bfd45f6e2613b6/src/gpu/effects/generated/GrCircleEffect.cpp. 설치브라우저revision확인증거가아니며실제sourcepixel측정이우선이다. oval후보actual Reset은unique guard실패로촬영하지못했으므로actual성공으로쓰지않는다. 동일capsule기하/색에analytic coverage를사용하는SkiaRuntimeShader후보로확인하며임의LUT/색상보정은사용하지않는다.

## Windows cap 교정 실제 검증 완료
- 최종 runtime shader는 source와 동일한 capsule 기하·색·device scale에 Skia primary analytic coverage를 적용한다. 임의 pixel LUT/색상 보정 없음. paint alpha를 두 번 적용하지 않도록 shader를 White paint로 그렸다.
- 원본과 최종 후보의 normal thumb 전체18×243physical crop: offscreen/실제 창 모두 차이0. hover/pressed/끝점drag도 각각 원점 정규화 뒤 같은18×243crop 전체 차이0. source/scrollviewport-windows-analytic-{offscreen,actual}-comparison.json 및 analytic-state-comparison.json.
- oval 전용 raster 후보는 offscreen60pixel/max17이 남아 채택하지 않았다. 최종 shader에서 normal의 기존67pixel/max33을 해소했다.
- startup 직후 UIA 창이 아직 없었던 실패는 입력 성공으로 쓰지 않았다. launch는 새로 시작한 정확한pid의owned제목이 나타날 때까지 최대6초 실제 상태를 확인하도록 수정했다. 이후 reset 및3개 actual paint 입력 성공, 후보 창 안전 종료.
- 재개 시 현재 파일에서 최소 높이 측정부터 시작한다. 원본 .rail/.content padding과 적용 부모만 조사하며 23개 LOCKED를 바꾸거나 source를 개선하지 않는다. Sidebar 상위는 아직 WAITING CHILD다.

최종 mac osx-arm64 publish도 성공했다(Windows 전용 analytic shader 분기는 Mac에서 실행하지 않는다). 양쪽 후보 창은 닫힌 상태이며, IAB scroll fixture는 restore-overflow/reset-top으로 정상 시작 상태를 복원하고 Sidebar/ScrollViewport 탭을 다음 작업용으로 보존했다. 임시 재개 제약은 전체 복제가 끝나지 않아 유지한다.

## 재개 확인 · 최소 높이 단계
AGENTS → MASTER_RULES → COMPONENT_STATUS/의존 지도 → 현재 명세 → 실행 원본 .rail computed border-box/overflow:auto/padding30×14/220×400 → WebScrollViewport measure/arrange를 확인했다. 보호23개 통과. 최소 요청 높이를 실제 원본 UI로 측정하며 Sidebar는 아직 조립하지 않는다. 최신 사용자 end-to-end 지시는 AGENTS에 보존했고 하위 완료마다 턴을 끝내지 않는다.

## 최소 높이 실제 확인 및 후속 교정
한영 직접 부모의 padding30×2를 유지한 실제 원본 UI에서 요청60/36/20/0은 양OS 모두 실제60으로 제한됐다. 후보 MinHeight의 padding 하한/coercion을 연결했고 양OS4+4조건 차이0(각 minimum-comparison.json). 임의 tiny 범위 면제가 아니다. 수평은 직접 Sidebar62×양OS에서scrollWidth=clientWidth로 발생하지 않으며 MainContent 계약은 상위 착수 시 별도 검증한다. Mac thumb의 CPU cap AA 차이는 Windows에서 확인한 동일 capsule analytic coverage를 alpha128에도 적용해 재비교한다. 아직 LOCK하지 않는다.

## 렌더 경계 조사 보존 · 2026-10-07
최소높이 양OS8조건은 차이0이다. Source CSS/실제 부모62조건은 수평overflow가 없어 Sidebar 수직 계약만 검증한 것으로 기록한다. Mac 250ms fully-visible 실제프레임의 thin thumb crop14×259은 analytic shader 및 원본 full-width radius SKPath 모두48pixel/max20 차이가 남았다. 표준 cubic circle path는max38로 악화돼 채택하지 않았다. 100ms 캡처는 후보 표시 이전 프레임이어서 비교 실패이며 250ms 새 프레임으로 대체했다. shader를 Mac에 적용하거나 cubic를 제품 구현으로 채택하지 않는다. 원본 full-width radius SKPath와 alpha128을 복구했다. `source/scrollviewport-mac-{analytic,path,cubic}-thin-comparison.json`을 보존한다.
NativeThemeMac→gfx Canvas의drawRoundRect 및Skia fill-rrect 공식 코드를읽고 source/gfx-canvas.cc와skia-fill-rrect.cpp로 보존했지만 설치된 브라우저 revision과 같은 것으로 주장하지 않는다. GPU/software cap coverage 차이의 원인·교정이 아직 확정되지 않아LOCK하지 않는다. Mac 후보 창은 닫았다. 다음조사: 동일bounds/alpha에서 원본 cap의 실제 coverage 방식과 renderer 경로를 확인한다.
현재 UI 입력·크기 계약은 검증했으나 이 차이를 면제하지 않고 WAITING PAINT로 보존한다. Sidebar는WAITING CHILD. 직접하위가모두잠긴 독립 ActionFooter로순차전환한다(동시활성/상위Sidebar조립 없음). 사용자 end-to-end 지시에 따라 미해결을숨기지않고 다른독립작업을진행하며 이후반드시이차이를돌아와해결한다.

## 최종 판정 · OS 기본 표현 조건 적용 · 2026-10-07
원본 실제 .rail ASIDE의 overflow:auto/padding30×14/width220 및 WebScrollViewport를 다시 확인했다. 최소 높이 완료 증거와 64배치·경계, 실제 입력·상태 자료를 재확인했다. cap48pixel/max20은 같은bounds/alpha/색/radius에서 round-rect raster 경계에만 존재한다. Windows glyph 경계1–2, Mac expanded track 평균0.307과 thumb AA도 기존 자료에 명시했다. logical 값/입력/레이아웃 누락이 아니라 기본 브라우저·Skia 렌더 경로의 AA/색 출력 표현에 남는 차이이므로 최신 사용자 조건에 따라 추가 renderer 역공학·임의 pixel 보정을 중단한다. 설치 브라우저의 정확한 GPU shader revision을 확인한 것으로 주장하지 않는다.
이 조건 이전의 ‘모든 cap 픽셀을 해결해야 LOCK’ 요구는 최신 지시로 대체한다. 기존 rejected analytic/cubic 실험은 채택하지 않는다. 현재 Mac은 full-width-radius SKPath/alpha128, Windows는 이미 통과한 analytic coverage다. 제품 치수/색/동작을 새로 바꾸지 않는다. `WebScrollViewport.cs`의 Sidebar 수직 계약을 LOCK하고 직접 하위가 모두 LOCK된 Sidebar를 순차 활성화한다. MainContent 수평 확장이 필요하면 LOCK 예외 절차를 따르고 필요한 원본 범위를 새로 검증한다.

## MainContent-only layout branch exception before edit · 2026-10-09
Summary newWindows30 found only Review-default-ko-960 mismatching; source targeted diagnostic fda231: content740x720, client725, scroll735, page669x678.78125; candidate target samecontent740x720 but client740, scroll720, page684x656.78125. Mono source474.50390625 fits wide480 but not narrow465: two auto-scrollbar fixed points, browser selects conservative narrow overflow branch. SourceSource30 other29 exact, nofont/value/padding/Documentchrome difference. ROOT adds explicit PreferNarrowOverflowLayout public property DEFAULTFALSE, existing Sidebar+Document policies unchanged; only actual Workspace mainViewport opts in. OnWindows finiteheight, measure narrow scrollbar branch before deciding, use wide branch when narrow stillfits. Mac path unchanged. Need targeted1/wholeSummary30 and affected rootparent states bothOS/actualWindows comparisons before reLOCK; existing unchanged Sidebar64/input/Chrome proofs reused. No forcedper-page widths, no comparator tolerance.

Parent regression diagnostic2026-10-09: directSummary1280→960 corrected target138/0/full30actual2599/0/PID4464close0, but new3f08 ParentResize1280→1100→960 has5 ReviewKO960 geometry differences (2590others0): sourceclient740/page684/height656.78125 vs candidate725/669/678.78125. SourceReview textidentical; no state.run dependency. Unconditional narrow-first remains IN PROGRESS; measure actualoriginal6 media/history branches before further policychange. Failed comparison preserved; no tolerance/font/forcedsize edits.

## Before-refinement existing-gutter branch · 2026-10-09
Root active after dependent InlineWindows d135 reLOCK. Fresh immutable source pageintegrationparentresize-windows-history-source.json sixpaths/18 states proves initial1280 gutter15 because widepadding72+page656.78125>720. Direct960 retains15; via1100/1180/1179 narrows padding56 and fits even current-gutter measure, removesbar, then960 wide684 stays0; via1181 retains15 then960narrow669. DOM page/heading/summary same, fonts/text/DPR2 unchanged. Current unconditionalnarrowfirst passes directSummary30 but historicalparent48 has5diff ReviewKO960; notOSwaiver. Root changes only opted-in Windows main initialmeasure from forced15 to existingGutterWidth, then classifyoverflow/re-measure chosenwidth; property/defaultfalse/Sidebar/Document/Mac unchanged. Originalc6 beforemanifest retained and narrowfirst diagnostic history preserved. Affected newhistory18 headless+actual, targetedhistoricalReviewKO4 +directSummary affectedheadless required; unchanged2590/32/sidebar/motion/input proofs reuse, not repeated. No reLOCK until nativehistory and changedparentgeometry pass. Recovery rules/status/spec/exactsource18/current measure and direct Workspace background/host allocations actually read.

Existing-gutter main refinement reLOCK c721: source sixpaths18 vs headless18 and actual18 strict2499/0 each, samepage/heading/summary references, fulltext/wrap/mainpadding/client/gutter/page/choice geometry exact. HistoricalReviewKO4 strict233/0; unchanged44parentstates previousstrict2595 proof reused under new48composition. DirectSummaryKO960 strict138/0 retains existingactual30/32 visual/clip proofs; branchformula affects only opted-in Windowsmain, defaultfalse/Sidebar/Document/Mac unchanged. Native4924 first3paths8 strict1113/0 then initial no-op1280 freshnessgate stalled(causeunproved/beforesequence missing); before1181 changedinput, immediateexactWindowPattern.Close/process/window0. Observer-only newb3e/f0f sameDesktophashes as93d/c721, publicfixturestartindex3, initial1280 guardedsettledseed notno-opSize; changedwidth nativeclient/freshUTC/seq/two stablegeometry snapshots +before/aftercounter. Remaining3paths10 actual1389/0/PID8984 immediatelyclose/process/window0; composed18 strict2499/0 with preservedPID/clocks/first8. Headless source/input/oldpass notrepeated or substitutedfornative. Protectedmanifest renewed; diagnosticfailure/raw evidence retained. Windowswholephysicalflow stillpending separately.
