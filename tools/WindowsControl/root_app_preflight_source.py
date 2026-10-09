"""Fresh Windows typography/document parent measurements; original code unchanged.

The fixture's automatic recorder performs programmatic navigation, not OS input.
Only this exact project-owned Edge window is closed after evidence is saved.
"""
from pathlib import Path
import json
import time
import uuid
from driver import ps, q

root = Path(__file__).resolve().parents[2]
lease = json.loads((root / 'docs/replica/ENVIRONMENT_LEASE.json').read_text())
assert lease['resources']['windows']['owner'] == 'root'
nonce = uuid.uuid4().hex
output = root / 'artifacts/replica/workers/root/pageintegration/preflight-source-windows' / nonce
output.mkdir(parents=True)
endpoint = root / 'docs/replica/source/root-appflow-preflight-windows-source.json'
title = 'Root preflight parent · original isolation'
guard = (
    "$wins=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll("
    "[System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)"
    "|Where-Object{$_.Current.Name.StartsWith(" + q(title) + ")-and "
    "(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('Microsoft\\Edge\\Application\\msedge.exe')});"
)
launch_requested = False
try:
    prior = ps(guard + "[Console]::WriteLine($wins.Count);", retry_read=True)
    assert prior.strip() == '0', 'Owned source already active; no new launch'
    launch_requested = True
    url = f'http://10.211.55.2:8768/root-app-preflight.html?os=windows&record=1&recording={nonce}'
    (output / 'launch.txt').write_text(ps(
        "Start-Process msedge.exe -ArgumentList @('--new-window','--start-maximized',"
        "'--force-device-scale-factor=2'," + q(url) + ");[Console]::WriteLine('Owned source requested');",
        timeout=15))
    deadline = time.monotonic() + 30
    while time.monotonic() < deadline:
        try:
            data = json.loads(endpoint.read_text())
            if data.get('recording') == nonce:
                assert len(data['rows']) == 8
                assert len({row['id'] for row in data['rows']}) == 8
                assert all(row['dpr'] == 2 for row in data['rows'])
                (output / 'source.json').write_text(json.dumps(data, indent=2) + '\n')
                break
        except (FileNotFoundError, json.JSONDecodeError):
            pass
        time.sleep(.05)
    else:
        raise RuntimeError('Fresh source8 absent; preserve attempt, no relaunch')
finally:
    if launch_requested:
        result = ps(guard +
            "if($wins.Count-ne1){throw 'Exact source window not unique; no broad close'};"
            "$wins[0].GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close();"
            "Start-Sleep -Milliseconds 120;" + guard +
            "if($wins.Count){throw 'Owned source still open'};[Console]::WriteLine('OWNED_SOURCE_WINDOW0');",
            timeout=15)
        (output / 'cleanup.json').write_text(json.dumps({'sourceWindow': 0, 'result': result}, indent=2) + '\n')
print(output)
