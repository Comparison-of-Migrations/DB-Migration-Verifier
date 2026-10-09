"""Finite queued actual scope; preserves successful headless evidence and cleans owned windows."""
import argparse,json,subprocess,sys
from pathlib import Path
from driver import ps,q
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('--reuse-source',action='store_true');a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='home';m=json.loads(a.manifest.read_text());s=json.loads((a.manifest.parent/'windows-stage.json').read_text());assert m['owner']=='home' and m['component']=='welcomeentryhint'
def run(name,*args):
 result=subprocess.run([sys.executable,str(r/'tools/WindowsControl'/name),*map(str,args)],cwd=r,capture_output=True,text=True);print(name,result.stdout[-300:],flush=True)
 if result.returncode:raise RuntimeError(result.stderr+result.stdout)
def compare(candidate,label):
 subprocess.run([sys.executable,str(r/'tools/ReplicaProbe/compare_welcome_entry_hint.py'),'windows',str(candidate),label],cwd=r,check=True)
base="$app=Join-Path (Join-Path $env:TEMP "+q(s['guestFolder'])+") 'app';$path=Join-Path $app 'ReplicaProbe.exe';"
owned=base+"$A=[System.Windows.Automation.AutomationElement];$T=[System.Windows.Automation.TreeScope];$ws=$A::RootElement.FindAll($T::Children,[System.Windows.Automation.Condition]::TrueCondition);$native=@($ws|Where-Object{$_.Current.Name-eq'Replica WelcomeEntryHint verification'-and(Get-Process -Id $_.Current.ProcessId).Path-eq$path});$source=@($ws|Where-Object{($_.Current.Name.StartsWith('WelcomeEntryHint · original isolation')-or$_.Current.Name.StartsWith('WelcomeEntryHint preview · original isolation'))-and(Get-Process -Id $_.Current.ProcessId).Path.EndsWith('\\Microsoft\\Edge\\Application\\msedge.exe')});"
try:
 if not a.reuse_source:run('welcome_entry_hint.py','source-open');run('welcome_entry_hint.py','source-record');run('welcome_entry_hint.py','source-close')
 else:assert len(json.loads((r/'docs/replica/source/welcomeentryhint-windows-source.json').read_text())['rows'])==8
 headless=list((a.manifest.parent/'windows-headless').glob('*/welcomeentryhint-native.json'));assert len(headless)==1;compare(headless[0].parent,'windows-headless')
 run('welcome_entry_hint.py','auto','--manifest',a.manifest);compare(r/'docs/replica/source/welcomeentryhint-windows-native','windows')
 run('welcome_entry_hint_source_capture.py','en')
finally:
 print(ps(owned+"if($native.Count-gt1-or$source.Count-gt2){throw 'Owned cleanup uniqueness required'};foreach($w in @($native)+@($source)){$w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()};"))
 value=json.loads(ps(owned+"$p=@(Get-Process ReplicaProbe -ErrorAction SilentlyContinue|Where-Object{$_.Path-eq$path});[pscustomobject]@{nativeProcess=$p.Count;nativeWindow=$native.Count;sourceWindow=$source.Count;path=$path}|ConvertTo-Json"));assert value['nativeProcess']==value['nativeWindow']==value['sourceWindow']==0;(r/'docs/replica/source/welcomeentryhint-windows-cleanup.json').write_text(json.dumps(value,indent=2)+'\n');print('Exact owned cleanup0')
