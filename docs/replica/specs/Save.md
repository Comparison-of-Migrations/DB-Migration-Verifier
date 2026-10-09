# Save
LOCKED · 2026-10-08. SummaryList LOCK42 후 직접하위 PageHeading/SaveOptions/SummaryList/Notice/ActionButton/Motion 계약을 읽고 실제원본 tab1 Save DOM/computed/save()/footer()/render()/formatchange를 확인했다. frontend-only Product Design 흐름은 이번 native 복제 범위와 다르므로 새 웹/모바일/디자인을 만들지 않고 원본증거·시각대조 원칙만 적용한다.

## 구조·정책
.page에는 heading(bottom24), download-options(3열 gap14), section(margin-top30)/panel.summary-list 2행, saved 때 notice(ok,margin20), actions(margin-top24,flex-wrap,gap10)가 순서대로 있다. 실제width904: heading70.9921875/options110/list163/actions44. 모든소속하위 LOCKED. label media열은 shellviewport<=1100 여부로 전달. Page엔트리는200ms custom easing; formatchange도 원본render전체교체로 재생한다. reduced는생략. 화면 전체 scroll/focusTitle/언어·footer·파일picker/다운로드는 Application의정책이며 Page는스냅샷과요청API를제공한다.

Save formats0 허용. footer downloadDisabled=true/hint형식선택, 그밖downloadEnabled/hint브라우저 다운로드. format집합 삽입순서 보존. change후 Page재생성에서도 data-format 및FocusVisible복원. Saved notice는 다운로드요청을표시하고 디스크저장성공확정은하지않는다. Open/Restart는 application요청이며 원본·대상DB동작없음.

## 검증 계획
literal원본save()로 한영×부모656/904/1060×실제shellviewport1000/1400×savedfalse/true×formats0/all=48페이지 배치·목록·버튼·notice·문구 대조. 실제입력은formatchange/keyboard복원/삽입순서/언어/재열기·재검사요청과페이지entry detach/reduced를검증한다. 실제파일내용·대화상자·앱scroll은뒤Application통합범위이며여기완료로주장하지않는다.

## 초기 실패와 교정
처음 build의 attach API 이름 오류를 부모 attach/detach flag로 교정했다. 첫 비교184건은 숨긴 fixture의 innerText가 빈 문자열을 반환한 수집 오류였다. source textContent로 다시 수집하니 Mac48 모두 차이0. 해당 실패는 통과 증거에 쓰지 않는다. Windows 일시CLI 실패 뒤 읽기전용 inventory로 owned 창이 없음을 확인하고 원본 창을 새로 준비했다.

## 최종 판정 · LOCK43
양OS48 literal save() 페이지 조립 기하/문구/선택집합/notice/버튼 차이0(source/savepage-{mac,windows}-comparison.json), native PNG의한영saved 분기와원본실측구조대조. 실제Mac pointer→Space→Tab→Space에서Page전체교체후format identity 및모달리티 유지,삽입순서[html,json],0formats disabled/hint,재선택enabled 확인. Windows 실제Space3개로0formats→disabled/hint,재선택JSON→enabled/포커스복원 확인. 각OS-input의keyboard-restored/empty-formats 또는 key-space-*, requests-language/reduced-reattached 증거. 양OS 다운로드 요청/다시열기/재검사 요청전달,언어변경후선택·saved유지,detachopacity1/reattach/reducedopacity1을실제창검증했다. Mac normal 재부착의entry opacity<1도확인했고보호Motion public200ms계약을조립했다.
Page는파일 I/O를직접실행하지않고Request만전달한다. 실제export/filepicker/애플리케이션footer포커스·scroll·제목이동은Application 조립 검증에서 남아있다. Page완료를전체앱완료로주장하지않는다. 기본OSfont래스터차이만허용,기하/상태/입력의알려진차이없음. 보호42범위수정없음. LOCK.
