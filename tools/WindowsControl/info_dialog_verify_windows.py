"""Fixed InfoDialog actual input sequence. No shared binary, credentials or OS settings."""
from pathlib import Path
import argparse,subprocess,sys,json
r=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('--phase',choices=['native','native-rest','native-tail','native-final','source','source-rest','source-closure','native-end','compare'],required=True);a=p.parse_args();assert json.loads((r/'docs/replica/ENVIRONMENT_LEASE.json').read_text())['resources']['windows']['owner']=='root';b=a.manifest.parent/'actual-windows'
def call(name,*args):
 result=subprocess.run([sys.executable,str(r/'tools/WindowsControl'/name),*map(str,args)],cwd=r,capture_output=True,text=True);print(name, args, result.stdout[-500:],flush=True)
 log=b/'input-driver-logs';log.mkdir(parents=True,exist_ok=True)
 (log/(str(len(list(log.glob('*.log'))))+'.log')).write_text(str(args)+'\n'+result.stdout+'\n'+result.stderr)
 if result.returncode:raise RuntimeError(result.stdout+result.stderr)
def key(value,label,source=False):call('info_dialog_key.py',a.manifest,*(['--source']if source else[]),'--key',value,'--label',label)
def pointer(target,label,source=False):call('info_dialog_pointer.py',a.manifest,*(['--source']if source else[]),'--target',target,'--label',label)
if a.phase in ['native','native-rest','native-tail','native-final']:
 if a.phase=='native':
  call('info_dialog.py','launch',a.manifest)
  pointer('open','pointer-open');key('tab','tab-footer');key('escape','escape-restore')
 elif a.phase=='native-rest':
  call('info_dialog.py','read',a.manifest,'--label','before-rest')
  state=json.loads((b/'input/before-rest.json').read_text());assert not state['visible'] and state['focus']=='InfoDialog-open'
 if a.phase not in ['native-tail','native-final']:
  call('info_dialog.py','invoke',a.manifest,'--target','small')
  call('info_dialog_key.py',a.manifest,'--target','close','--key','tab','--label','small-body');key('end','small-end')
 elif a.phase=='native-tail':
  call('info_dialog.py','read',a.manifest,'--label','before-tail')
  state=json.loads((b/'input/before-tail.json').read_text());assert state['visible'] and state['focus']=='InfoDialog-body' and state['row']['bodyScrollTop']==172
 if a.phase!='native-final':key('tab','small-footer');pointer('footer-close','footer-close-restore')
 else:
  call('info_dialog.py','read',a.manifest,'--label','before-final');state=json.loads((b/'input/before-final.json').read_text());assert not state['visible'] and state['focus']=='InfoDialog-open'
 pointer('open','small-reopen');pointer('close','header-close-restore')
 call('info_dialog.py','invoke',a.manifest,'--target','reduced');pointer('open','reduced-open');pointer('close','reduced-close')
elif a.phase=='source':
 call('info_dialog_source_input.py','open')
 pointer('open','pointer-open',True);key('tab','tab-footer',True);key('escape','escape-restore',True)
 call('info_dialog_source_input.py','invoke','--target','small')
 pointer('open','small-open',True);key('tab','small-body',True);key('end','small-end',True);key('tab','small-footer',True);pointer('footer-close','footer-close-restore',True)
 pointer('open','small-reopen',True);pointer('close','header-close-restore',True)
elif a.phase=='source-rest':
 pointer('open','small-reopen',True);key('tab','small-body',True);key('end','small-end',True);key('tab','small-footer',True);pointer('footer-close','footer-close-restore',True)
 pointer('open','small-reopen',True);pointer('close','header-close-restore',True)
 call('info_dialog_source_input.py','invoke','--target','restore')
 pointer('open','pointer-open',True);key('tab','tab-footer',True);key('escape','escape-restore',True)
elif a.phase=='native-end':
 pointer('open','pointer-open');key('tab','small-body');key('end','small-end');key('tab','small-footer');pointer('footer-close','footer-close-restore')
elif a.phase=='source-closure':
 call('info_dialog_source_input.py','invoke','--target','small')
 pointer('open','small-reopen',True);key('tab','small-body',True);key('end','small-end',True);key('tab','small-footer',True);pointer('footer-close','footer-close-restore',True)
 pointer('open','small-reopen',True);pointer('close','header-close-restore',True)
else:
 report=[]
 for name in ['pointer-open','tab-footer','escape-restore','small-body','small-end','small-footer','footer-close-restore','header-close-restore']:
  source=json.loads((b/'source-input'/(name+'.json')).read_text());native=json.loads((b/'input'/(name+'.json')).read_text());diff=[]
  for k in ['visible','focus']:
   if source[k]!=native[k]:diff.append([k,source[k],native[k]])
  if source['visible']:
   for sk,nk in [('bodyScrollTop','bodyScrollTop'),('dialogScrollTop','dialogScrollTop')]:
    if source[sk]!=native['row'][nk]:diff.append([sk,source[sk],native['row'][nk]])
  if native['backgroundEnabled']==native['visible']:diff.append(['background isolation',native['backgroundEnabled'],native['visible']])
  if name in ['pointer-open','tab-footer']and source['focusVisible']!=native['focusVisible']:diff.append(['focusVisible',source['focusVisible'],native['focusVisible']])
  report.append({'label':name,'differences':diff})
 (b/'input-comparison.json').write_text(json.dumps(report,indent=2)+'\n');print(report);assert not any(x['differences']for x in report)
