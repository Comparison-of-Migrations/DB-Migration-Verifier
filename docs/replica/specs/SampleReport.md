# SampleReport · LOCKED

2026-10-08 home worker. 원본/직접 부모는 보호 `source/original.js`의 home() 및 `.welcome-layout`; 자식은 LOCKED Typography/Surface/SvgIcon. 원본코드 복구/47 LOCK 확인 완료. 실행 Home/source8 양OS 조사 완료 뒤 구현 시작. 보호 live CSS/JS 해시가 일치함(`samplereport-live-source-version.json`).

## 원본 구조와 최종 CSS

aside.sample-report aria-label → report-head(span + file SVG), report-title(br), report-line 3개(span/b), report-foot(br). 상태는 KO/EN이며 수치는 3/2/45 고정 예시. 입력·hover·focus·독립 animation 없음. 전체 Home의 page-in은 부모 소유. viewport <=1180 display:none은 부모 소유.

최종 override: 배경 #2b2d31/border #4b4f59 1px/radius12, shadow `0 12px 30px #00000025`, padding18px22px, transform:none. Head 12px/1.6, padding-bottom10/border-bottom1. Title15px/1.55/650/letter-spacing-.02em, margin12px0 6px. Line padding8px0/bottom-border1 (foot가 뒤에 있어 세 줄 모두 border 존재), labels12px/1.6, b17px/1.6/600/tnum. Foot10px/1.7/margin-top12. Ink #f2f3f5, muted#b5bac1, red#ff959d, amber#f0c47c.

## 검증 계획

`prepare_sample_report.py`는 actual home()에서 aside만 추출. display:block isolation은 parent media 숨김만 해제한다. KO/EN × widths270/300/375.2/520=8. 브라우저 실측 geometry/text/color/name과 같은 인스턴스 language/width reuse 대조. 양OS native rendered PNG를 직접 확인하고 passive mount/remove/reuse 수명도 확인한다. fixture generator는 실행 전이며 실행/source manifest 버전 일치와 직접 Home 배치는 lease에서 확인한다.

## 현재 차이 / 다음

원본 8 geometry 측정 확보. live1280 부모 card326.1953125×312.0859375; 고정 cardheight312.0859375, head31/title46.5/각row44.1953125/foot34, 전부 CSS 최종 override와 일치. source fixture가 hidden7의 innerText를 빈값으로 읽는 문제를 발견하고 DOM childNodes textContent+BR로 수정했으며 재측정 대기. 구현 WebSampleReport + SampleReportProbe 작성 완료, 빌드/양OS native 비교는 lease 대기. LOCK 불가.

## 복구/변경 기록 · 2026-10-08

원본 실제 Home 새 owned탭은 보호 원본과 동일해시였으며 부모 viewport1280/page980/card326.1953125 geometry 확인. 원본 사용자 Connection 탭을 변경하지 않았다. direct screenshot로 Home paint 확인했으나 처음 캡처는 파일로 보존하지 못했으므로 LOCK paint 증거로 주장하지 않는다. 다음 lease에서 source fixture screenshot와 native PNG를 저장·직접 비교한다. 독립 CSS override가 `report-line:last-child` border 제거에 해당하지 않는 것도 세 줄 아래 foot 존재로 확인.

처음 native draft는 sealed Panel.Render를 override하여 공유 publish CS0239를 유발했다. border paint를 LOCKED WebSurfaceBox 자식으로 변경했으며 보호 파일은 수정하지 않았다. 후보 빌드 성공·native 검증은 대기 중이다.

## 최종 판정 · 2026-10-08 LOCK

양OS 실제 window/브라우저 DPR2, 각8 KO/EN × 270/300/375.2/520 기하·문구·색상·name·radius 차이0. 원본은 최종 CSS 원본으로 만들고 고정 `.sample-report`만 display:block로 표시했다. 폭375.2의 실제 CSS375.1953125 양OS 일치. 초기 hidden fixture innerText 빈값은 DOM nodeText+BR로 교정 후 **새** 양OS8 원본을 수집했다. 초기 tight flex child 폭의 미세차이는 부모 layout에서 CSS의 DPR×64 ceiling을 적용해 해결했으며 타 Typography 코드 수정 없음.

- 원본: [mac8](../source/samplereport-mac-source.json), [windows8](../source/samplereport-windows-source.json).
- native/layout/lifecycle: [mac8+3](../source/samplereport-mac-native/samplereport-native.json), [windows8+3](../source/samplereport-windows-native/samplereport-native.json).
- 비교: [mac0](../source/samplereport-mac-comparison.json), [windows0](../source/samplereport-windows-comparison.json).
- 실제 source paint 직접확인: [mac](../screenshots/samplereport-mac-source.png), [Windows 공식capture](../screenshots/samplereport-windows-source-full.png); Windows title/process/rectangle은 [guard](../source/samplereport-windows-source-capture.json).
- native KO270/EN375.2 PNG 두 언어 직접확인: source/samplereport-{mac,windows}-native/의 PNG. border/radius/icon/분리선/제목2줄/foot2줄과 실제 색상·shadow 확인. source screenshot 전달 출력크기와 native RT PNG 해상도가 달라 **픽셀동일**로 주장하지 않는다.
- actual mounted window에서 제거→언어갱신→재삽입3회 양OS, 높이312.0859375/opacity1/transform없음/접근성name 갱신. passive card이므로 사용자 pointer/keyboard/focus 상태는 적용되지 않는다. Home 입장motion/<=1180 숨김은 부모 소유이고 이 LOCK에 포함되지 않는다.
- 양OS publish0error. 저장 상태의 KO/EN fontmetric가 각각 source와 일치하며 OS 기본 font/raster와 Windows RGB antialias 표현차이는 사용자 허용된 OS renderer 차이로 보존. 배치·문구·색상 구현차이는 없음.

구현: `src/Verifier.Desktop/WebSampleReport.cs`. 상위 계약: SetPresentation(english), 자연 높이312.0859375, 부모 지정폭, 독립 입력/animation 없음. `.welcome-layout` media와 부모배치 검증은 Home에서 이어간다. [LOCK hash](../source/samplereport-lock.json).
