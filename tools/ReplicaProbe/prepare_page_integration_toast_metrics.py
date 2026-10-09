"""Literal original Toast three natural messages; no source/timer rewrite."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
js=r'''
const frame=document.querySelector('iframe'),q=new URLSearchParams(location.search),rows=[];await new Promise(r=>frame.onload=r);const w=frame.contentWindow,d=frame.contentDocument;await d.fonts.ready;
for(const text of ['예시 시험 준비를 확인했습니다. 실제 DB 검증은 아닙니다.','Sample trial setup confirmed. No actual DB was verified.','Match applied. Name differences remain part of verification.']){w.toast(text);const e=d.querySelector('.toast');await new Promise(r=>setTimeout(r,240));await Promise.all(e.getAnimations().map(a=>a.finished));const s=w.getComputedStyle(e),canvas=d.createElement('canvas'),ctx=canvas.getContext('2d');ctx.font=s.font;ctx.fontKerning=s.fontKerning;ctx.letterSpacing=s.letterSpacing;const measured=ctx.measureText(text);const range=d.createRange();range.selectNodeContents(e);const b=e.getBoundingClientRect(),r=range.getBoundingClientRect();rows.push({text,dpr:w.devicePixelRatio,computedFont:s.font,fontKerning:s.fontKerning,letterSpacing:s.letterSpacing,computedWidth:s.width,computedHeight:s.height,border:s.borderLeftWidth,paddingLeft:s.paddingLeft,paddingRight:s.paddingRight,canvasAdvance:measured.width,textRangeWidth:r.width,box:{x:b.x,y:b.y,width:b.width,height:b.height},parent:{innerWidth:w.innerWidth,documentClientWidth:d.documentElement.clientWidth}})}
const result={phase:'ORIGINAL_NATURAL_TOAST_METRICS',recording:q.get('recording'),rows,scope:'three literal original app toast natural widths/font/range/canvas; actualDPR, no lifetime repeat'};await fetch('/evidence/home-pageintegrationtoast-mac-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(result)});frame.remove();document.getElementById('result').textContent='Recorded3';
'''
h='<meta charset="utf-8"><title>Toast natural metrics · home</title><p id="result">Measuring</p><iframe width="960" height="900" style="border:0" src="home-pageintegrationtoast-inner.html"></iframe><script>(async()=>{'+js+'})();</script>'
(root/'artifacts/replica/windows-download/home-pageintegrationtoast-metrics.html').write_text(h)
(root/'artifacts/replica/workers/home/toast-metrics-script.js').write_text('(async()=>{'+js+'})();')
s=(root/'tools/MacControl/page_integration_toast_source.py').read_text()
s=s.replace("p=argparse.ArgumentParser();p.add_argument('motion',choices=['normal','reduced']);p.add_argument('--geometry-only',action='store_true');p.add_argument('--target',choices=['ko-960-normal']);a=p.parse_args()", "p=argparse.ArgumentParser();p.add_argument('scale',type=int,choices=[1,2]);a=p.parse_args()")
a=s.index("url='http://");b=s.index('stream=',a)
s=s[:a]+"url='http://10.211.55.2:8768/home-pageintegrationtoast-metrics.html?recording='+nonce\ncommand=[str(chrome),'--headless=new','--no-first-run','--no-default-browser-check','--user-data-dir='+str(profile),'--force-device-scale-factor='+str(a.scale),'--window-size=1400,1000','--disable-background-timer-throttling',url]\n"+s[b:]
s=s.replace("assert d['motion']==a.motion and len(d['rows'])==(2 if a.target else 8) and len(d['lifetimes'])==(0 if a.geometry_only else 2)","assert len(d['rows'])==3 and all(r['dpr']==a.scale for r in d['rows'])")
(root/'tools/MacControl/page_integration_toast_metrics.py').write_text(s)
