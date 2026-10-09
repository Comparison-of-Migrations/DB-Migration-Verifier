# Spinner

LOCKED · ActionButton의 직접 하위. 원본 실제 Connection busy를 확인한 뒤 범위를 추가했다. 다른 atomic/부모 변경 없이 이 primitive부터 검증한다.

## 원본 계약

`span.spinner`는16×16 border-box/flex:none, border2px currentColor, border-right transparent, radius50%, background transparent, opacity1. CSS `spin .8s linear infinite`은 center에서0→360deg. 실제 liveConnection EN busy는disabledprimary 안의 `.spinner`+span Checking…이며 currentColor#b5bac1이다. 회전 중 getBoundingClientRect는16px보다커질수있다. 실제 측정19.586px은 layout16px의 변경 근거가 아니다.

Reduced CSS는animation/transition none, border모든side currentColor/background currentColor/opacity.7이다. Motion public Spinner800ms/linear/repeat/center/cancel/reduced 계약은 이미LOCKED이고변경하지않는다. OSreducedquery는상위책임이며primitive는explicitbool을받는다.

## 후보 및 확인 순서

`WebSpinner`에서currentColor를명시적으로받아2px ring(rightquadranttransparent) 또는reducedfull disc를paint한다. attach시에publicMotion.Play, detach/reduced시dispose. 장식요소이며input/action이없다. source fixture에서정지시간/색/normal/reduced를읽고동일크기후보와대조한다. 최종 후보와 검증 결과는 아래 계약을 따른다.


## 최종 계약과 검증 · 2026-10-06

구현: `src/Verifier.Desktop/WebSpinner.cs`. 16px 장식 Control이 외부 currentColor와 ReducedMotion bool을 받는다. 정상은 2px ring에서 오른쪽90deg 부분을 비우고, reduced는 full disc/opacity.7이다. attach에서 public Motion.Play(Spinner), reduce/detach에서 dispose, normal 복귀에서 새 playback을 사용한다. OS 설정·입력·버튼 이름을 추측하지 않는다.

원본: source/spinner-source-fixture.html + exact original CSS. fixture의 reduced class는 원본 media 선언을 복사한 진단이며 실제 OS 설정을 바꾸지 않았다. 3color×5state15case 양OS width/height16/currentColor/opacity가 일치한다(source/spinner-layout-comparison.json). 800ms/linear/center/repeat/fill은 LOCKED Motion 계약이다.

Windows 실제 DPR2 source capture/native2x의15case paint는 screenshots/spinner-windows-comparison.png 및 source/spinner-windows-{raster,ink}-comparison.json에 있다. maxROI MAE1.324/255, 모든 visible stroke의 양방향 거리≤1physicalpx. edge AA 미세 차이를 bit-for-bit 일치로 주장하지 않는다. Mac logical API screenshot/native1x도15state 형태를 확인했으나 동일DPR2 pixel 증거는 아니다.

actual macOS/Windows 창(source/spinner-{mac,windows}-window.json): attach 후 회전/infinite pending, reduced 전환 후 opacity.7/transform 없음, normal child detach 후 completion 종료/transform 복원, normal 복귀 후 opacity1/회전 재개를 확인했다. macwindow screenshot도 보관했다.

기존4primitive hash 유지, build0warning/0error. 구조·크기·색상·상태·paint·animation/lifecycle 검증 범위에서 LOCK한다. 부모 gap·currentColor 전달·연결 busy550ms·페이지 absolute 배치는 이 LOCK에 포함하지 않고 부모 조립에서 검증한다.
