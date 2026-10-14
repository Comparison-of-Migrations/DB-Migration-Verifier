DB-Migration-Verifier
=====================

|readme-ko| |readme-en| |guide-ko| |guide-en|

.. |readme-ko| image:: https://img.shields.io/badge/README-%ED%95%9C%EA%B5%AD%EC%96%B4-0057B8?style=for-the-badge
   :target: README.ko.rst
   :alt: README 한국어

.. |readme-en| image:: https://img.shields.io/badge/README-English-0057B8?style=for-the-badge
   :target: README.rst
   :alt: README English

.. |guide-ko| image:: https://img.shields.io/badge/%EC%82%AC%EC%9A%A9%20%EC%84%A4%EB%AA%85%EC%84%9C-%ED%95%9C%EA%B5%AD%EC%96%B4-087F8C?style=for-the-badge
   :target: guide/USER_GUIDE.ko.rst
   :alt: 사용 설명서 한국어

.. |guide-en| image:: https://img.shields.io/badge/USER%20GUIDE-English-087F8C?style=for-the-badge
   :target: guide/USER_GUIDE.en.rst
   :alt: User guide English

**무엇이 달라졌는지, 무엇을 확인하지 못했는지 함께 봅니다.**

마이그레이션을 마친 두 데이터베이스의 데이터·구조·권한·관련 객체를
비교하는 데스크톱 앱입니다. 관측이 불완전한 곳은 이유와 함께 표시합니다.

.. image:: https://img.shields.io/badge/license-Apache%202.0-blue
   :target: LICENSE
   :alt: Apache License 2.0
.. image:: https://img.shields.io/badge/.NET-10.0-512BD4
   :target: global.json
   :alt: .NET 10.0
.. image:: https://img.shields.io/badge/desktop-macOS%20%7C%20Windows-5865F2
   :target: https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/releases
   :alt: macOS 및 Windows
.. image:: https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/actions/workflows/build.yml/badge.svg
   :target: https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/actions/workflows/build.yml
   :alt: 소스 빌드 상태

`Preview 다운로드 <https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/releases>`_
· `문제 제보 <https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/issues/new/choose>`_

.. image:: .github/assets/results.png
   :alt: 불일치와 미검증을 함께 표시한 실제 macOS 비교 결과 화면
   :width: 100%

소유 합성 SQL Server를 비교한 실제 앱 화면입니다. 같은 검사에 알려진 차이와
미검증 관측이 함께 있을 수 있으므로, 이 숫자를 전체 DB 성공률로 해석하지 않습니다.

주요 기능
---------

* **행 수 이상의 비교:** 정확한 값, 구조와 제약, 조회 가능한 권한과 관련 객체 정의를 확인합니다.
* **확인 불가 보존:** 조회 권한 부족, 안전하게 실행할 수 없는 동작, 지원하지 않는 관측,
  중단된 작업을 일치와 구분합니다.
* **실행 전 범위 검토:** 검사와 테이블·컬럼 대응을 선택하고 실제 실행 범위를 확인합니다.
* **결과 보관:** JSON·CSV·HTML로 저장하고, JSON을 다시 열어 검사 ID와 판정을 복원합니다.
* **관측과 쓰기 분리:** 원본·대상 비교는 읽기 전용입니다. 선택적인 동작 시험은
  별도로 확인하고 확정한 시험 DB를 사용합니다.
* **로컬 사용:** 한국어·영어 UI, 선택 시 OS 비밀번호 보관, 보호 임시 저장과 결과 페이지 탐색을 제공합니다.

빠른 시작
---------

1. `Releases <https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/releases>`_ 에서
   OS에 맞는 ZIP을 내려받아 새 폴더에 풉니다. .NET 런타임이 포함돼 있습니다.
2. macOS는 ``DB-Migration-Verifier.app``, Windows는 ``Verifier.Desktop.exe`` 를 엽니다.
   묶음 안의 ``README.txt`` 에 실행 안내가 있습니다.
3. 서로 다른 두 DB를 연결하고 사전 확인·검사 범위·대응을 검토한 뒤 비교합니다.
4. 불일치와 미검증 이유를 확인합니다. JSON은 결과 보관·재열기에, CSV·HTML은 열람·공유에 사용합니다.

자세한 화면별 절차는 `한국어 사용 설명서 <guide/USER_GUIDE.ko.rst>`_ 를 참고하세요.

**현재 실행 묶음은 미서명 Preview입니다.** Apple 공증과 신뢰된 Windows 서명은 대기 중이며
OS 보호 알림으로 실행이 제한될 수 있습니다. 정식 배포 인수와 마이그레이션 인증을 완료한 제품으로 제공하지 않습니다.

DB·플랫폼 확인 범위
-------------------

.. list-table:: 현재 개발 확인 범위
   :header-rows: 1
   :widths: 28 72

   * - 엔진 / 플랫폼
     - 실제 확인한 범위
   * - PostgreSQL 17
     - 실제 비교, 관측 한계, 결과 저장·재열기와 별도 시험 경로.
   * - MySQL 8.4 / MariaDB 11.4
     - 구현 범위 내 실제 비교와 엔진별 metadata·가시성·무결성·시험 경로.
   * - SQL Server 2022
     - 로컬 엔진 검사와 native x64 서버의 일부 범위: 의미 검사, 601컬럼 비교,
       2만 행·16MiB 셀 비교와 별도 시험 DB 검사.
   * - macOS ARM64
     - 소유 시험 환경에서 실제 배포 실행 파일의 연결·비교·상세·세 형식 저장·JSON 재열기.
   * - Windows x64 빌드
     - Windows 11 ARM의 x64 에뮬레이션에서 같은 한정된 흐름을 확인했습니다.
       native x64 장비 인수는 남아 있습니다.

위 실제 DB 흐름은 2026-10-14 의존성 갱신 전 구현 기준의 증거입니다.
갱신한 묶음은 시작·화면 이동과 인증서·접속 정보 처리의 직접 영향을 별도로 확인했습니다.

확인 범위는 DB 엔진·객체 종류·권한·선택한 검사에 따라 달라지며 앱에 제한을 표시합니다.
더 큰 실제 카탈로그, 서버 자원 예산, 실제 EKM 제공자와 모든 운영 환경은 인수하지 않았습니다.
임의 CLR·동적 코드·외부 부작용을 실행해 결과를 추정하지 않으며,
안전하게 관측한 정의와 확인 불가 이유를 보존합니다.

검증 후 개발·시험 DB와 배포 임시 파일은 제거했습니다. 공개 소스에는 시험 자격,
생성 결과 덤프나 내부 작업 기록을 포함하지 않습니다.

소스에서 실행하기
-----------------

``global.json`` 에 지정한 .NET SDK 10.0.401을 설치한 뒤 실행합니다::

   git clone https://github.com/Comparison-of-Migrations/DB-Migration-Verifier.git
   cd DB-Migration-Verifier
   dotnet restore DB-Migration-Verifier.slnx --locked-mode
   dotnet build DB-Migration-Verifier.slnx -c Release --no-restore
   dotnet run --project src/Verifier.Desktop

공개 소스는 앱과 패키징 구현을 포함합니다. 자동 빌드는 소스 컴파일을 확인하며
실제 DB·GUI 인수 통과를 대신하지 않습니다.

알맞은 빌드 호스트에서 런타임 포함 개발 묶음을 만들려면 다음을 실행합니다::

   sh tools/package-macos.sh
   sh tools/package-windows.sh
   python3 tools/package-archives.py

패키징은 의존성 고지를 보존합니다. 서명·공증은 별도입니다.

구성
----

::

   src/Verifier.Core/       비교, 범위, 보호 저장, 결과 보고서
   src/Verifier.Data/       DB 연결, TLS, 관측, 별도 시험 실행
   src/Verifier.Desktop/    Avalonia 화면과 OS 연동
   tools/                  빌드·배포 패키징
   guide/                  한국어·영어 사용 설명서
   .github/                소스 빌드와 이슈 양식

기여하기
--------

관측한 동작과 기대 결과, DB 종류·버전, OS·아키텍처, 재현 가능한 작은 예시를 이슈에 적어 주세요.
수정은 정확한 비교 의미·범위 일관성·취소·확인 불가 표시를 유지해야 합니다.
변경 범위를 좁게 유지하고 어떤 영향을 확인했는지 설명해 주세요.

실제 접속 문자열·비밀번호·키·고객 데이터·운영 덤프를 첨부하지 마세요.
비식별 구조와 합성 데이터를 사용하고, 보안 취약성은 GitHub 비공개 취약성 제보로 알려 주세요.

라이선스
--------

DB-Migration-Verifier는 `Apache License 2.0 <LICENSE>`_ 으로 배포합니다.
Copyright 2026 DB-Migration-Verifier contributors.
의존성은 각자의 라이선스를 유지합니다. `THIRD_PARTY_NOTICES.txt <THIRD_PARTY_NOTICES.txt>`_ 를 참고하세요.
