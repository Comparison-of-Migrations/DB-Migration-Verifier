"""Original table-shell horizontal overflow isolation; diagnostic descendants preserve visual overflow."""
from pathlib import Path
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/windows-download'
(o/'capabilityviewport-original.css').write_bytes((r/'docs/replica/source/original.css').read_bytes())
script=r'''
const cases=[];for(const width of [478,478.25,610,656])for(const mode of ['fit','intrinsic','descendant'])for(const height of [120,684.6796875]){
 const f=document.createElement('div');f.className='fixture';f.dataset.id=width+'-'+mode+'-'+height;f.style.width=width+'px';
 const shell=document.createElement('div');shell.className='table-shell';const content=document.createElement('div');content.className='logical';const extent=document.createElement('div');extent.className='extent';
 const logicalWidth=mode==='fit'?width-20:mode==='intrinsic'?width+137.7265625:width,extentWidth=mode==='descendant'?width+20.2734375:logicalWidth;
 content.style.cssText='width:'+logicalWidth+'px;height:'+height+'px';extent.style.cssText='width:'+extentWidth+'px;height:'+height+'px';content.append(extent);shell.append(content);f.append(shell);document.getElementById('fixtures').append(f);cases.push({id:f.dataset.id,width,mode,height,logicalWidth,extentWidth,shell,content,extent});
}
const first=cases[0];
function rect(e,origin){const q=e.getBoundingClientRect();return{x:q.x-origin.x,y:q.y-origin.y,width:q.width,height:q.height}}
function sample(c,label){const e=c.shell,q=e.getBoundingClientRect(),s=getComputedStyle(e);return{id:c.id,label,width:c.width,mode:c.mode,height:c.height,logicalWidth:c.logicalWidth,extentWidth:c.extentWidth,shell:rect(e,q),content:rect(c.content,q),extent:rect(c.extent,q),clientWidth:e.clientWidth,clientHeight:e.clientHeight,scrollWidth:e.scrollWidth,scrollHeight:e.scrollHeight,offset:e.scrollLeft,top:e.scrollTop,overflowX:s.overflowX,overflowY:s.overflowY,tabIndex:e.tabIndex,active:document.activeElement===e}}
async function record(){await document.fonts.ready;const samples=[];for(const c of cases){c.shell.scrollLeft=1e9;const max=c.shell.scrollLeft;for(const [label,offset] of [['zero',0],['fractional',123.456],['maximum',max]]){c.shell.scrollLeft=offset;samples.push({...sample(c,label),maximum:max})}c.shell.scrollLeft=0}const os=new URLSearchParams(location.search).get('os')==='mac'?'mac':'windows';const response=await fetch('/evidence/preflight-capabilityviewport-'+os+'-source.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,samples})});if(!response.ok)throw Error('Record rejected');document.getElementById('capabilityviewport-record').textContent='Viewport diagnostics recorded'}
document.getElementById('capabilityviewport-record').onclick=record;
'''
(o/'capabilityviewport.html').write_text('<!doctype html><meta charset="utf-8"><title>CapabilityViewport · original isolation</title><link rel="stylesheet" href="capabilityviewport-original.css"><style>.fixture{position:absolute;left:30px;top:40px}.fixture:not(:first-child){visibility:hidden}.table-shell{border:0;border-radius:0;background:#2b2d31}.logical{position:relative}.extent{background:repeating-linear-gradient(90deg,#232428 0px,#232428 20px,#41434a 20px,#41434a 21px)}</style><div id="fixtures"></div><button id="capabilityviewport-record" style="position:fixed;right:20px;top:20px">Record CapabilityViewport</button><script>'+script+'</script>')
print('CapabilityViewport source24, offsets72 prepared')
