"""Independent source snapshots, transitive MSBuild outputs and rendered headless processes."""
from pathlib import Path
import argparse, datetime, hashlib, json, os, platform, re, shutil, subprocess, sys, uuid

ROOT = Path(__file__).resolve().parents[2]
DOTNET = Path('/Users/youptebook/.cache/db-migration-verifier/dotnet/dotnet')
p = argparse.ArgumentParser()
sub = p.add_subparsers(dest='action', required=True)
b = sub.add_parser('build')
b.add_argument('owner', choices=['root', 'connection', 'preflight', 'home'])
b.add_argument('component')
b.add_argument('--runtime', default='osx-arm64', choices=['osx-arm64', 'win-x64'])
b.add_argument('--self-contained', action='store_true')
x = sub.add_parser('run')
x.add_argument('manifest', type=Path)
x.add_argument('probe_args', nargs=argparse.REMAINDER)
a = p.parse_args()

if a.action == 'build':
    if not re.fullmatch(r'[a-z][a-z0-9-]{0,63}', a.component):
        p.error('Component must be a lowercase owned path name')
    run = ROOT / 'artifacts/replica/workers' / a.owner / a.component / uuid.uuid4().hex
    source = run / 'source'
    source.mkdir(parents=True)
    ignore = shutil.ignore_patterns('bin', 'obj', '__pycache__', 'packages.lock.json')
    shutil.copytree(ROOT / 'src', source / 'src', ignore=ignore)
    shutil.copytree(ROOT / 'tools/ReplicaProbe', source / 'tools/ReplicaProbe', ignore=ignore)
    for name in ['Directory.Build.props', 'Directory.Build.targets', 'NuGet.Config', 'nuget.config', 'global.json']:
        if (ROOT / name).exists():
            shutil.copy2(ROOT / name, source / name)
    # Each executable compiles only the assigned probe; unrelated drafts cannot
    # break another worker's verification build through the shared Program routes.
    entries = {
        'pageheading': ('PageHeadingProbe.cs', 'PageHeadingProbeApplication'),
        'actionbuttonsemantic': ('ActionButtonSemanticsProbe.cs', 'ActionButtonSemanticsProbeApplication'),
        'headingsemantics': ('HeadingSemanticsProbe.cs', 'HeadingSemanticsProbeApplication'),
        'pageheadingsemantic': ('PageHeadingSemanticsProbe.cs', 'PageHeadingSemanticsProbeApplication'),
        'connectionpage': ('ConnectionPageProbe.cs', 'ConnectionPageProbeApplication'),
        'capabilityrow': ('CapabilityRowProbe.cs', 'CapabilityRowProbeApplication'),
        'welcomestep': ('WelcomeStepProbe.cs', 'WelcomeStepProbeApplication'),
        'welcomesteps': ('WelcomeStepsProbe.cs', 'WelcomeStepsProbeApplication'),
        'findingarticle': ('FindingArticleProbe.cs', 'FindingArticleProbeApplication'),
        'welcomepapercontent': ('WelcomePaperContentProbe.cs', 'WelcomePaperContentProbeApplication'),
        'infodialog': ('InfoDialogProbe.cs', 'InfoDialogProbeApplication'),
        'capabilityheader': ('CapabilityHeaderProbe.cs', 'CapabilityHeaderProbeApplication'),
        'capabilitytable': ('CapabilityTableProbe.cs', 'CapabilityTableProbeApplication'),
        'capabilityviewport': ('CapabilityViewportProbe.cs', 'CapabilityViewportProbeApplication'),
        'welcomepaper': ('WelcomePaperProbe.cs', 'WelcomePaperProbeApplication'),
        'welcomeentryhint': ('WelcomeEntryHintProbe.cs', 'WelcomeEntryHintProbeApplication'),
        'metricitem': ('MetricItemProbe.cs', 'MetricItemProbeApplication'),
        'metrics': ('MetricsProbe.cs', 'MetricsProbeApplication'),
        'progressmeter': ('ProgressMeterProbe.cs', 'ProgressMeterProbeApplication'),
        'progresspage': ('ProgressPageProbe.cs', 'ProgressPageProbeApplication'),
        'preflightpage': ('PreflightPageProbe.cs', 'PreflightPageProbeApplication'),
        'selectionpage': ('SelectionPageProbe.cs', 'SelectionPageProbeApplication'),
        'runningdot': ('RunningDotProbe.cs', 'RunningDotProbeApplication'),
        'homepage': ('HomePageProbe.cs', 'HomePageProbeApplication'),
        'resultspage': ('ResultsPageProbe.cs', 'ResultsPageProbeApplication'),
        'trialpage': ('TrialPageProbe.cs', 'TrialPageProbeApplication'),
        'pageintegration': ('PageIntegrationProbe.cs', 'PageIntegrationProbe'),
    }
    if a.component not in entries:
        p.error('Central agent must register the component probe before isolation')
    probe_file, application = entries[a.component]
    owned_prefix = probe_file.removesuffix('Probe.cs')
    probe_files = sorted(f.name for f in (source / 'tools/ReplicaProbe').glob(owned_prefix + '*.cs'))
    # Other agents' unfinished new UI files are not dependencies of this leaf.
    # Keep all LOCKED files and ordinary application sources; exclude only
    # explicitly registered unrelated, unprotected new component files in the
    # owned snapshot. A real cross-reference still fails compilation; no stub.
    protected = set()
    for lock in (ROOT / 'docs/replica/source').glob('*-lock.json'):
        protected.update(json.loads(lock.read_text()).get('files', {}).keys())
    excluded = []
    for other, (name, _) in entries.items():
        if other == a.component or a.component == 'pageintegration' or (a.component == 'homepage' and other == 'welcomeentryhint') or (a.component == 'progresspage' and other == 'runningdot'):
            continue
        prefix = 'Web' + name.removesuffix('Probe.cs')
        for candidate in (source / 'src/Verifier.Desktop').glob(prefix + '*.cs'):
            relative = str(candidate.relative_to(source))
            if relative not in protected:
                excluded.append(relative)
                candidate.unlink()
    project = source / 'tools/ReplicaProbe/ReplicaProbe.csproj'
    text = project.read_text().replace('<OutputType>Exe</OutputType>',
        '<OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems>')
    text = text.replace('</Project>', '<ItemGroup>' + ''.join(
        f'<Compile Include="{name}"/>' for name in [*probe_files, 'ReplicaPlatform.cs', 'IsolatedEntryPoint.cs'])
        + '</ItemGroup></Project>')
    project.write_text(text)
    (source / 'tools/ReplicaProbe/IsolatedEntryPoint.cs').write_text(
        'using Avalonia;\nusing Avalonia.Controls.ApplicationLifetimes;\n'
        'internal static class IsolatedEntryPoint { [System.STAThread] '
        f'private static void Main(string[] args)=>ReplicaPlatform.Configure<{application}>()'
        '.StartWithClassicDesktopLifetime(args); }\n')
    if a.component == 'pageintegration':
        (source / 'tools/ReplicaProbe/IsolatedEntryPoint.cs').write_text('internal static class IsolatedEntryPoint { [System.STAThread] private static void Main(string[] args)=>PageIntegrationProbe.Run(args); }\n')
    hashes = {str(f.relative_to(source)): hashlib.sha256(f.read_bytes()).hexdigest()
              for f in source.rglob('*') if f.is_file()}
    metadata = {'owner': a.owner, 'component': a.component, 'sourceHashes': hashes,
                'runtime': a.runtime, 'createdUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                'source': str(source), 'artifacts': str(run / 'build'), 'outputs': str(run / 'outputs'),
                'excludedUnrelatedUnlockedUI': excluded}
    cmd = [str(DOTNET), 'build', str(source / 'tools/ReplicaProbe/ReplicaProbe.csproj'),
           '-c', 'Release', '-r', a.runtime, '--artifacts-path', str(run / 'build'),
           '-p:RestorePackagesWithLockFile=false', '-nodeReuse:false', '-p:UseSharedCompilation=false', '-m:1', '--nologo']
    if a.self_contained:
        cmd.append('-p:SelfContained=true')
    metadata['buildCommand'] = cmd
    env = os.environ.copy()
    env['TMPDIR'] = str(run / 'tmp') + '/'
    Path(env['TMPDIR']).mkdir()
    result = subprocess.run(cmd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    (run / 'build.log').write_text(result.stdout)
    metadata['buildExitCode'] = result.returncode
    dlls = list((run / 'build/bin/ReplicaProbe').rglob('ReplicaProbe.dll'))
    if result.returncode == 0:
        if len(dlls) != 1:
            raise RuntimeError(f'Expected one owned binary, found {dlls}')
        metadata['binary'] = str(dlls[0])
        metadata['binarySha256'] = hashlib.sha256(dlls[0].read_bytes()).hexdigest()
    (run / 'build.json').write_text(json.dumps(metadata, indent=2) + '\n')
    print(run / 'build.json', flush=True)
    print(result.stdout[-4000:])
    sys.exit(result.returncode)
else:
    m = json.loads(a.manifest.read_text())
    if m['buildExitCode'] != 0:
        raise RuntimeError('Cannot run a failed build')
    binary = Path(m['binary'])
    if hashlib.sha256(binary.read_bytes()).hexdigest() != m['binarySha256']:
        raise RuntimeError('Owned binary changed')
    args = a.probe_args
    if args and args[0] == '--':
        args = args[1:]
    if not args:
        p.error('Probe arguments required')
    run = Path(m['outputs']) / uuid.uuid4().hex
    run.mkdir(parents=True)
    env = os.environ.copy()
    env['DBMV_HEADLESS'] = '1'
    env['DBMV_HEADLESS_SCALE'] = '2'
    env['TMPDIR'] = str(run / 'tmp') + '/'
    Path(env['TMPDIR']).mkdir()
    # Case input is copied into this process's evidence directory before execution.
    # Standard probe contract: flag, cases, output. Additional fixed flags follow output.
    if len(args) < 2 or not Path(args[1]).is_file():
        p.error('Use a case-driven probe with flag and a case JSON path')
    original_cases = Path(args[1]).resolve()
    cases = run / original_cases.name
    shutil.copy2(original_cases, cases)
    output = run / 'candidate'
    output.mkdir()
    extras = args[3:] if len(args) > 2 else []
    cmd = [str(DOTNET), str(binary), args[0], str(cases), str(output), *extras]
    evidence = {'phase': 'HEADLESS_RUNNING', 'owner': m['owner'], 'component': m['component'],
                'buildManifest': str(a.manifest.resolve()), 'binarySha256': m['binarySha256'],
                'hostOS': platform.platform(), 'renderer': 'Skia', 'headlessDrawing': False,
                'scale': 2, 'command': cmd, 'casesSha256': hashlib.sha256(cases.read_bytes()).hexdigest(),
                'startUtc': datetime.datetime.now(datetime.timezone.utc).isoformat()}
    print(run, flush=True)
    result = subprocess.run(cmd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, timeout=120)
    (run / 'run.log').write_text(result.stdout)
    evidence['processExitCode'] = result.returncode
    evidence['phase'] = 'HEADLESS_RECORDED' if result.returncode == 0 else 'HEADLESS_FAILED'
    evidence['note'] = 'Recorded is not passed. Compare original geometry/paint/input before HEADLESS_PASSED; OS checks remain.'
    (run / 'environment.json').write_text(json.dumps(evidence, indent=2) + '\n')
    print(result.stdout[-2000:])
    sys.exit(result.returncode)
