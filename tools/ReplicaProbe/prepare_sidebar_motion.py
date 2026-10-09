from pathlib import Path
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/windows-download'
(o/'sidebaritem-motion.html').write_text('''<!doctype html><meta charset="utf-8"><title>SidebarItem motion · original isolation</title><link rel="stylesheet" href="sidebaritem-original.css"><style>body{padding:30px}.step{width:192px;margin:14px 0}</style><button type="button" id="sidebar-motion-record">Measure SidebarItem motion</button><div id="fixtures"></div><script>
const rows=[];
function sample(el,scenario,ms,duration){const s=getComputedStyle(el),b=getComputedStyle(el.firstElementChild);rows.push({scenario,ms,bg:s.backgroundColor,color:s.color,badgeColor:b.color,shadow:s.boxShadow,transform:s.transform,reverseDuration:scenario==='hover-reverse'?duration:null})}
document.getElementById('sidebar-motion-record').onclick=async()=>{
 rows.length=0;
 for(const scenario of ['normal-hover','active-hover','disable-color','pressed-scale','hover-reverse']){
  const el=document.createElement('button');el.type='button';el.className='step'+(scenario==='active-hover'?' active':'');el.innerHTML='<span class="step-index">3</span><span>검사 선택</span>';el.style.transition='none';document.getElementById('fixtures').append(el);getComputedStyle(el).backgroundColor;el.style.transition='';getComputedStyle(el).backgroundColor;
  if(scenario==='disable-color')el.disabled=true;else el.classList.add(scenario==='pressed-scale'?'probe-active':'probe-hover');getComputedStyle(el).backgroundColor;
  let animations=el.getAnimations();for(const a of animations){a.pause();a.currentTime=0;}
  let duration=null;
  if(scenario==='hover-reverse'){for(const a of animations)a.currentTime=30;getComputedStyle(el).backgroundColor;el.classList.remove('probe-hover');getComputedStyle(el).backgroundColor;animations=el.getAnimations();for(const a of animations){a.pause();a.currentTime=0;}duration=animations.find(a=>a.transitionProperty==='background-color')?.effect.getTiming().duration;}
  for(const ms of [0,8,16,30,40,60,80,100,120,160]){for(const a of animations)a.currentTime=ms;sample(el,scenario,ms,duration)}
  el.remove();
 }
 const response=await fetch('/evidence/source-sidebaritem-motion.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,reduced:matchMedia('(prefers-reduced-motion: reduce)').matches,rows})});if(!response.ok)throw Error('Example evidence failed');document.getElementById('sidebar-motion-record').textContent='SidebarItem motion recorded';
};</script>''')
p=r/'tools/WindowsControl/fixture_server.py';p.write_text(p.read_text().replace("allowed={'source-sidebaritem.json'","allowed={'source-sidebaritem-motion.json','source-sidebaritem.json'"));print('Prepared fixed SidebarItem CSS transition fixture')
