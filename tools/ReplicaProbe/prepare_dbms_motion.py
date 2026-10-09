from pathlib import Path
r=Path(__file__).resolve().parents[2];o=r/'artifacts/replica/windows-download'
(o/'dbmsoption-motion.html').write_text('''<!doctype html><meta charset="utf-8"><title>DbmsOption motion · original isolation</title><link rel="stylesheet" href="dbmsoption-original.css"><style>body{padding:30px}.dbms-option{width:240px;margin:14px 0}.dbms-option.probe-hover{background:#383b44;border-color:#a7b1ff}</style><button type="button" id="dbms-motion-record">Measure DbmsOption motion</button><div id="fixtures"></div><script>
const rows=[];
document.getElementById('dbms-motion-record').onclick=async()=>{
 rows.length=0;
 for(const scenario of ['normal-hover','selected-hover','select','unselect','hover-reverse']){
  const el=document.createElement('label');el.className='dbms-option';el.innerHTML='<span>SQL Server</span><input type="radio">';el.style.transition='none';const radio=el.querySelector('input');radio.checked=['selected-hover','unselect'].includes(scenario);document.getElementById('fixtures').append(el);getComputedStyle(el).backgroundColor;el.style.transition='';getComputedStyle(el).backgroundColor;
  if(scenario==='select')radio.checked=true;else if(scenario==='unselect')radio.checked=false;else el.classList.add('probe-hover');getComputedStyle(el).backgroundColor;
  let animations=el.getAnimations();for(const a of animations){a.pause();a.currentTime=0;}let duration=null;
  if(scenario==='hover-reverse'){for(const a of animations)a.currentTime=30;getComputedStyle(el).backgroundColor;el.classList.remove('probe-hover');getComputedStyle(el).backgroundColor;animations=el.getAnimations();for(const a of animations){a.pause();a.currentTime=0;}duration=animations.find(a=>a.transitionProperty==='background-color')?.effect.getTiming().duration;}
  for(const ms of [0,.5,8,16,30,40,60,80,120,160]){for(const a of animations)a.currentTime=ms;const s=getComputedStyle(el);rows.push({scenario,ms,bg:s.backgroundColor,border:s.borderColor,shadow:s.boxShadow,reverseDuration:duration});}el.remove();
 }
 const response=await fetch('/evidence/source-dbmsoption-motion.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,reduced:matchMedia('(prefers-reduced-motion: reduce)').matches,rows})});if(!response.ok)throw Error('Example evidence failed');document.getElementById('dbms-motion-record').textContent='DbmsOption motion recorded';
};</script>''')
p=r/'tools/WindowsControl/fixture_server.py';p.write_text(p.read_text().replace("allowed={'source-dbmsoption.json'","allowed={'source-dbmsoption-motion.json','source-dbmsoption.json'"));print('Prepared DbmsOption CSS transition fixture')
