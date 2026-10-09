"""Fixed preflight/CapabilityRow isolated runtime identity; no generic runtime fallback."""
from pathlib import Path
import json,re
from driver import q

def load(stage):
 v=json.loads(Path(stage).read_text())
 if v['owner']!='preflight' or v['component']!='capabilityrow' or not v['selfContained']:
  raise RuntimeError('Complete owned CapabilityRow stage required')
 if not re.fullmatch(r'DBMV-Isolated-preflight-capabilityrow-[0-9a-f]{32}',v['guestFolder']):
  raise RuntimeError('Fixed owned guest folder mismatch')
 base="$base=Join-Path $env:TEMP "+q(v['guestFolder'])+";$app=Join-Path $base 'app';"
 return v,base
