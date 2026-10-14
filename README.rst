DB-Migration-Verifier
=====================

**Know what changed. See what could not be verified.**

A desktop tool for comparing two databases after a migration. Inspect data,
structure, permissions, and related objects in one workflow, with explicit
reasons whenever an observation is incomplete.

마이그레이션 전후 두 DB의 차이와 확인하지 못한 영역을 함께 보여주는 데스크톱 앱입니다.

.. image:: https://img.shields.io/badge/license-Apache%202.0-blue
   :target: LICENSE
   :alt: Apache License 2.0
.. image:: https://img.shields.io/badge/.NET-10.0-512BD4
   :target: global.json
   :alt: .NET 10.0
.. image:: https://img.shields.io/badge/desktop-macOS%20%7C%20Windows-5865F2
   :target: https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/releases
   :alt: macOS and Windows desktop
.. image:: https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/actions/workflows/build.yml/badge.svg
   :target: https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/actions/workflows/build.yml
   :alt: Source build status

`Download preview builds <https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/releases>`_
· `Report an issue <https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/issues/new/choose>`_

.. image:: .github/assets/results.png
   :alt: Actual macOS comparison result showing differences and unverified checks side by side
   :width: 100%

Actual app screenshot from an owned synthetic SQL Server comparison. The counts
illustrate that one check can contain both a known difference and an unverified
observation; they are not a database-wide success rate.

Why use it?
-----------

* **Compare more than row counts.** Inspect exact values, schema and constraints,
  visible permissions, and related object definitions.
* **Keep unknowns visible.** Incomplete visibility, unsafe execution, unsupported
  observations, and interrupted work stay distinct from an equal result.
* **Review the scope first.** Select checks, match tables and columns, and review
  the effective scope before starting.
* **Keep a portable result.** Export JSON, CSV, or HTML. Reopen JSON with the
  original run identity and findings.
* **Separate observation from writes.** Source and target comparison is read-only.
  Optional behavior checks require a separate, explicitly confirmed trial DB.
* **Work locally.** Korean and English UI, OS credential storage when selected,
  protected temporary storage, and paged result browsing.

Quick start
-----------

1. Download the ZIP for your OS from `Releases <https://github.com/Comparison-of-Migrations/DB-Migration-Verifier/releases>`_
   and extract it into a new folder. The .NET runtime is included.
2. Open ``DB-Migration-Verifier.app`` on macOS or ``Verifier.Desktop.exe`` on Windows.
   Read the included ``README.txt`` for the platform instructions.
3. Connect two distinct databases, complete preflight, choose the scope and
   mappings, and run the comparison.
4. Review differences and unverified reasons. Save JSON and reopen it to retain
   the complete result; use CSV or HTML for sharing a readable report.

**Preview builds are unsigned.** Apple notarization and trusted Windows signing
are pending. OS protection prompts may prevent execution; signed distribution
has not been qualified. Do not use this preview as a migration certification.

Database and platform status
----------------------------

.. list-table:: Current development coverage
   :header-rows: 1
   :widths: 28 72

   * - Engine / platform
     - What has been exercised
   * - PostgreSQL 17
     - Native comparison, observation limits, result export/reopen, and separate trial paths.
   * - MySQL 8.4 / MariaDB 11.4
     - Native comparison and engine-specific metadata, visibility, integrity, and trial paths within the implemented scope.
   * - SQL Server 2022
     - Local engine coverage and a native x64 server slice: semantic checks, a 601-column comparison, 20,000 rows with a 16 MiB cell, and separate trial DB checks.
   * - macOS ARM64
     - The unchanged shipping apphost completed connection, comparison, details, three export formats, and JSON reopen on an owned fixture.
   * - Windows x64 build
     - The shipping apphost completed the same bounded flow on Windows 11 ARM using x64 emulation. Native x64 hardware qualification remains open.

The native DB flow evidence above belongs to the implementation baseline before
the October 14 dependency refresh. The refreshed packages are checked separately
for startup, navigation, and certificate/connection handling.

Coverage varies by engine, object type, privileges, and selected checks. The app
reports those limits. Larger native inventories, server-side resource budgets,
positive EKM provider acceptance, and all production configurations are not
qualified. Arbitrary CLR, dynamic code, or external side effects are not executed
to manufacture a result; safe definition observations and unverified reasons are retained.

Development/test databases and temporary deployment files were removed after
validation. No fixture credentials, generated result dumps, or internal planning
records are included in this source distribution.

Build from source
-----------------

Install the exact .NET SDK from ``global.json`` (10.0.401), then run::

   git clone https://github.com/Comparison-of-Migrations/DB-Migration-Verifier.git
   cd DB-Migration-Verifier
   dotnet restore DB-Migration-Verifier.slnx --locked-mode
   dotnet build DB-Migration-Verifier.slnx -c Release --no-restore
   dotnet run --project src/Verifier.Desktop

The source distribution contains the application and packaging implementation.
The build workflow compiles source; it does not claim native DB or GUI acceptance.

To create self-contained development packages from a suitable build host::

   sh tools/package-macos.sh
   sh tools/package-windows.sh
   python3 tools/package-archives.py

Packaging preserves third-party notices. It does not sign or notarize the binaries.

Project layout
--------------

::

   src/Verifier.Core/       comparison, scope, protected storage, reports
   src/Verifier.Data/       DB adapters, TLS, observation and trial execution
   src/Verifier.Desktop/    Avalonia desktop workflow and OS integrations
   tools/                  build and distribution packaging
   .github/                source build workflow and issue forms

Contributing
------------

Open an issue describing the observed behavior, expected result, DB engine/version,
OS/architecture, and the smallest reproducible case. A useful fix preserves exact
comparison semantics, scope consistency, cancellation, and explicit unknowns.
Keep changes focused and describe how the affected path was checked.

Never attach real connection strings, passwords, keys, customer rows, or raw
production dumps. Use sanitized schema and synthetic data. For a suspected
vulnerability, use GitHub's private vulnerability reporting instead of a public issue.

License
-------

DB-Migration-Verifier is distributed under the `Apache License 2.0 <LICENSE>`_.
Copyright 2026 DB-Migration-Verifier contributors.
Third-party components retain their own terms; see `THIRD_PARTY_NOTICES.txt <THIRD_PARTY_NOTICES.txt>`_.
