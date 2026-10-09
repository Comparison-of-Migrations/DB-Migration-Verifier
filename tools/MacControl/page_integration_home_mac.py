"""Continue one owned App window; never repeat an input after a failed observer."""
from pathlib import Path
import argparse,json,subprocess,time
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('--from-step',default='start-observed',choices=['start-observed','back','scroll-down','research','close-dialog','language','scroll-up','open','cancel-file','sample','cleanup']);a=p.parse_args()
v=json.loads((a.manifest.parent/'current-native.json').read_text());out=Path(v['output']);helper=r/'artifacts/replica/workers/root/pageintegration/page-integration-input';assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['macos']['owner']=='root'
def raw():return json.loads((out/'pageintegration-actual-state.json').read_text())
def save(name,predicate,after=-1):
    end=time.monotonic()+5
    while time.monotonic()<end:
        value=raw()
        if value['snapshot']>after and predicate(value):
            (out/('home-'+name+'.json')).write_text(json.dumps(value,indent=2)+'\n');print(name,value['Screen'],value['focus'],value['offset'],flush=True);return value
        time.sleep(.03)
    raise RuntimeError('Fresh state predicate failed: '+name+'; do not repeat input')
def run(mode,identity='start'):
    result=subprocess.run([str(helper),str(v['pid']),mode,v['binary'],identity,str(out/'home-ax.json')],capture_output=True,text=True)
    (out/('helper-'+mode+'-'+identity+'.log')).write_text(result.stdout+result.stderr)
    if result.returncode:raise RuntimeError(result.stderr+result.stdout)
def click(step,identity,predicate):
    prior=raw()['snapshot'];run('click',identity);return save(step,predicate,prior)
def inspect():run('inspect');return json.loads((out/'home-ax.json').read_text())
steps=['start-observed','back','scroll-down','research','close-dialog','language','scroll-up','open','cancel-file','sample','cleanup'];start=steps.index(a.from_step)
for step in steps[start:]:
    if step=='start-observed':save(step,lambda s:s['Screen']=='connect' and s['focus']=='screen-title')
    elif step=='back':click(step,'Footer-back',lambda s:s['Screen']=='home' and s['focus']=='screen-title' and s['offset']==0)
    elif step=='scroll-down':
        prior=raw()['snapshot'];run('scroll-down');save(step,lambda s:s['offset']>0,prior)
    elif step=='research':click(step,'research',lambda s:s['dialog'] and s['focus']=='InfoDialog-close')
    elif step=='close-dialog':click(step,'InfoDialog-close',lambda s:not s['dialog'] and s['focus']=='research')
    elif step=='language':
        before=raw();value=click(step,'language',lambda s:s['English']!=before['English'] and s['focus']=='language');assert value['offset']==before['offset'],'Language changed parent scroll'
    elif step=='scroll-up':
        prior=raw()['snapshot'];run('scroll-up');save(step,lambda s:s['offset']==0,prior)
    elif step=='open':
        run('click','open');end=time.monotonic()+5
        while time.monotonic()<end:
            if inspect()['sheets']>0:break
            time.sleep(.05)
        assert inspect()['sheets']>0,'Actual file sheet not observed; do not repeat Open';(out/'home-open-file-ax.json').write_bytes((out/'home-ax.json').read_bytes())
    elif step=='cancel-file':
        prior=raw()['snapshot'];run('escape');end=time.monotonic()+5
        while time.monotonic()<end:
            if inspect()['sheets']==0:break
            time.sleep(.05)
        assert inspect()['sheets']==0;save(step,lambda s:s['Screen']=='home' and s['focus']=='open',prior)
    elif step=='sample':click(step,'sample',lambda s:s['Screen']=='results' and s['sampleRun']['Complete'])
    else:
        run('close');end=time.monotonic()+5
        while time.monotonic()<end and subprocess.run(['kill','-0',str(v['pid'])],capture_output=True).returncode==0:time.sleep(.05)
        assert subprocess.run(['kill','-0',str(v['pid'])],capture_output=True).returncode!=0
        (out/'cleanup.json').write_text(json.dumps({'nativeProcess':0,'nativeWindow':0,'pid':v['pid']},indent=2)+'\n')
