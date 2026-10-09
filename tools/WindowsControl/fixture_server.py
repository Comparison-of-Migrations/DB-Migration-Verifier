from pathlib import Path
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
import json,functools,uuid,re
repo=Path(__file__).resolve().parents[2]
folder=repo/'artifacts/replica/windows-download'
allowed={'source-sidebar.json','source-resultchart.json','source-resultrow-motion.json','source-resultrow.json','source-resultcount.json','source-tablerow-motion.json','source-tablemappingrow.json','source-categoryheader-motion.json','source-categoryheader.json','source-field.json','source-dbmsoption-motion.json','source-dbmsoption.json','source-typography-notice.json','source-notice.json','source-sidebaritem-motion.json','source-sidebaritem.json','source-stepbadge.json','source-searchinput.json','source-iconbutton.json','source-statuspill.json','source-numberinput.json','source-textinput.json','textinput-input.json','source-radio.json','source-radio-motion.json','radio-input.json','source-checkbox-motion.json','source-checkbox.json','checkbox-input.json','source-spinner.json','action-button-input-state.json','action-button-reduced.json','action-button-click.json','source-action-button-states.json','source-action-button.json','source-typography.json','typography-candidate-layout.json','typography-candidate-window.json','typography-live-narrow.json','typography-live-restored.json','motion-window-completed.json','motion-window-cancelled.json','motion-window-reduced.json','control-ready.json','source-typography-extended.json'}
# Explicit per-owner routes avoid shared evidence files across OS resources.
for owner in ['root','connection','preflight','home']:
 for platform in ['mac','windows']:
  for component in ['pageheading','connectionpage','capabilityrow','welcomestep','welcomesteps','findingarticle','infodialog']:
   for kind in ['source','input','control-ready']:
    allowed.add(f'{owner}-{component}-{platform}-{kind}.json')
class Handler(SimpleHTTPRequestHandler):
 def do_POST(self):
  name=self.path.removeprefix('/evidence/')
  components=json.loads((repo/'docs/replica/EVIDENCE_COMPONENTS.json').read_text())
  dynamic={f'{owner}-{component}-{platform}-{kind}.json' for owner in ['root','connection','preflight','home'] for component in components if re.fullmatch(r'[a-z][a-z0-9-]{0,63}',component) for platform in ['mac','windows'] for kind in ['source','input','control-ready']}
  if not self.path.startswith('/evidence/') or name not in allowed|dynamic:
   self.send_error(404);return
  length=int(self.headers.get('Content-Length','0'))
  if length<1 or length>2_000_000:
   self.send_error(413);return
  data=self.rfile.read(length)
  try:json.loads(data)
  except ValueError:self.send_error(400);return
  out=repo/'docs/replica/source'/(name if name.split('-')[0] in {'root','connection','preflight','home'} else 'windows-'+name)
  temporary=out.with_name(out.name+'.'+uuid.uuid4().hex+'.tmp')
  temporary.write_bytes(data)
  temporary.replace(out)
  self.send_response(200);self.end_headers();self.wfile.write(b'OK')
print('Isolated VM bridge fixture server:10.211.55.2:8768',flush=True)
ThreadingHTTPServer(('10.211.55.2',8768),functools.partial(Handler,directory=str(folder))).serve_forever()
