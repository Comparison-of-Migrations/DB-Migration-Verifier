// Example-only diagnostics: sample the original browser's actual CSS transitions.
const motionButton=document.createElement('button');motionButton.id='section-motion';motionButton.textContent='Record category motion';motionButton.style.cssText='position:fixed;right:20px;top:560px';document.body.append(motionButton);
motionButton.onclick=async()=>{
 const rows=[],durations=[];
 for(const scenario of ['expand','collapse','expand-reverse','select','unselect','select-reverse']){
  const e=document.querySelector('.fixture>.category').cloneNode(true);e.style.cssText='position:absolute;left:30px;top:30px;width:676px;opacity:0;pointer-events:none';document.body.append(e);
  const wrap=e.querySelector('.category-body-wrap'),head=e.querySelector('.category-head'),input=e.querySelector('input'),expansion=scenario.startsWith('expand')||scenario==='collapse',reverse=scenario.endsWith('reverse');
  for(const node of [e,wrap,head])node.style.transition='none';
  const initial=scenario==='collapse'||scenario==='unselect';input.checked=expansion?false:initial;wrap.classList.toggle('is-open',expansion?initial:true);e.getBoundingClientRect();
  for(const node of [e,wrap,head])node.style.transition='';getComputedStyle(wrap).gridTemplateRows;getComputedStyle(e).borderColor;
  function change(value){if(expansion)wrap.classList.toggle('is-open',value);else input.checked=value;getComputedStyle(wrap).gridTemplateRows;getComputedStyle(e).borderColor;}
  change(!initial);let animations=e.getAnimations({subtree:true}).filter(a=>a instanceof CSSTransition);for(const a of animations){a.pause();a.currentTime=0;}
  if(reverse){for(const a of animations)a.currentTime=30;wrap.getBoundingClientRect();getComputedStyle(e).borderColor;change(initial);animations=e.getAnimations({subtree:true}).filter(a=>a instanceof CSSTransition);for(const a of animations){a.pause();a.currentTime=0;}}
  durations.push({scenario,animations:animations.map(a=>({property:a.transitionProperty,duration:a.effect.getComputedTiming().duration}))});
  for(const ms of [0,.5,8,16,30,40,60,80,120,160,200,240]){
   for(const a of animations)a.currentTime=ms;
   const c=getComputedStyle(e),v=getComputedStyle(wrap);rows.push({scenario,ms,height:wrap.getBoundingClientRect().height,visibility:v.visibility,border:c.borderColor,shadow:c.boxShadow});
  }
  e.remove();
 }
 const response=await fetch('/evidence/source-sidebar.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows,durations})});if(!response.ok)throw Error('Motion evidence failed');motionButton.textContent='Category motion recorded';
};
