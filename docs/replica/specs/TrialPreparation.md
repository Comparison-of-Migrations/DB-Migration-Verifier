# TrialPreparation
LOCKED · 2026-10-08. MatchingDialog 양OS 검증/LOCK 뒤 source21에서 trial 선택→준비 버튼→예시 consent/confirm→Selection 복귀를 실제 조작했다. 실제 DB 접근/쓰기 없음. 원본 preparation DOM/최종 CSS/미준비 및 완료 치수를 다시 확인했다. 이 영역은 Notice + button + prepared-db card를 조립하며 Trial page의 Field/입력/분리판정은 부모 page 범위다.

## 원본 계약
trial이 미선택이면 빈문자열(높이0). 선택·미준비면 warn Notice(기본 margin-top20) + row margin-top16, align center/gap24/justify space-between. 버튼44 높이/기본variant/db아이콘/label 시험 DB 준비(영문 Prepare a test DB). ready면 ok Notice + label 시험 준비 수정(Edit test setup) + 오른쪽 prepared-db role=status. row 높이는 max(button44,cardheight). sample card db=migration_test,dbms=SQL Server,host=trial.example.invalid. 카드 intrinsic width270.5625/height75.984375(Mac DPR2). Notice/버튼 정책 문구는 original trialPreparation() literal. 버튼은 Trial page 요청만 전달하고 준비 판정은 하지 않는다.

## Bottom-Up 추가 의존
PreparedDb라는 내부 완료정보 카드가 기존 LOCK 컴포넌트에 없으므로 먼저 조사/명세/구현/대조/LOCK한다. 현재 본 parent는 WAITING CHILD이며 단일 활성은 PreparedDb다. 원본 전체/다른 page는 재분석하지 않는다. 전체 복제 임시 압축 재개 제약 유지.

## 현재 단계 · 2026-10-08
PreparedDb 양OS96/lifecycle10 검증 후 LOCK. 직접 하위 모두 잠겼으며 본 parent 하나만 활성. JS27/CSS42 원본을 재확인하고 원본21 준비완료 배치에서 조립 조사한다. 숨김·미준비·완료와 한국어/영어·두 desktop 폭·자료 길이의 부모 배치, 요청/갱신/mount 검증을 수행한다.

## 구조·부모 경계
원본21 actual #trial-preparation: ready 높이163.578125, notice71.59375 + row간격16 + card75.984375. notice의 margin-top20은 CSS 첫 자식 margin collapse로 wrapper 바깥에 있으며 root 자체에20을 둔다(native Notice.Margin=0은 이 부모 경계의 override). 미선택 wrapper높이0/내용없음. ready해제/재선택은 준비상태를 보존하며 DOM children을 새로 만든다. 버튼은 data-action=trial → Trial 페이지 이동이며 DB검증/쓰기 없음. WebTrialPreparation은 SetPresentation때 자식을 새로 조립해 같은 mount/reset을 유지한다. `.trial-prepared-row`는 left button/right status,24gap/center/between; 이 영역은 desktop폭656/904에서 확인한다.

최초 고립 native 창의 비어 있는 초기 상태에서 Canvas가 무한 폭 측정을 요청해 유효하지 않은 desired size가 발생했다. 비어 있는 요소의 무한 제약에는 폭0/높이0을 반환하도록 교정했다. 실패 실행을 검증 통과로 기록하지 않는다.

## 완료·증거
양OS 각각40조건(language2×viewport2×parent2×hidden/unprepared/ready/longLatin/longKorean)의 전체/자식 box·문구·kind·활성 상태 차이0: `source/trialpreparation-{mac,windows}-{source,comparison}.json`, native폴더의 `trialpreparation-native.json`. 각OS attached lifecycle7은 미준비·준비 생성(.35/y6)·완료(1/y0)·숨김높이0·제거된 카드detach 및 복원·reduced 생성을 확인했다. 해당 `trialpreparation-lifecycle.json`. `trialpreparation.png` 양OS 실제 렌더를 시각 확인했다. actual Mac pointer 준비/수정 요청2회, 숨김/English 복귀·영문pointer+Space 요청2회, Windows UIAInvoke 준비/수정/영문 요청3회·숨김 확인: `source/trialpreparation-{mac,windows}-input/` fresh files. 원본21 준비 선택/해제와 실제 준비/수정 페이지 이동·완료 복귀를 실제 확인했다. 원본과 native 각OS font에 따른 intrinsic 폭 차이는 해당OS원본과 비교하면0이며 OS글자 래스터만 유지한다. 이곳은 Trial 화면 요청만 전달하며 DB쓰기/준비 검증은 수행하지 않는다. LOCK: `source/trialpreparation-lock.json`. 하위 보호코드32개 무변경.
