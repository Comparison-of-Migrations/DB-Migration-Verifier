"""Isolated original Notice geometry, inline baseline, kind, and update corpus."""
from pathlib import Path
import json,html
root=Path(__file__).resolve().parents[2]; out=root/'artifacts/replica/windows-download'
(out/'notice-original.css').write_text((root/'docs/replica/source/original.css').read_text())
texts={
 'ko':[('보이지 않는 객체를 “없음”으로 처리하지 않습니다','읽지 못한 항목은 검사 후에도 확인 불가로 남습니다. 읽을 수 있는 부분의 차이는 계속 확인할 수 있습니다.'),('연결 정보를 확인하세요','다른 예시 시험 DB를 지정하세요. 분리를 확인할 수 없으면 쓰기 시험은 실행할 수 없습니다.'),('양쪽 예시 연결을 확인했습니다','이어서 어떤 범위를 읽을 수 있는지 확인합니다. 실제 서버에 접속한 결과는 아닙니다.'),('실행 범위','12 / 12 항목 처리 · 완료')],
 'en':[('Hidden objects are not treated as absent','Unreadable items remain unverified. Differences in readable areas can still be checked.'),('Check connection details','Choose another sample test database. Writes cannot run without established isolation.'),('Both sample connections are checked','Next, check visibility. This is not evidence of a real server connection.'),('Execution scope','12 / 12 checks processed · Completed')]}
cases=[]
for lang in texts:
 for width in [240,452,860]:
  for j,kind in enumerate(['warn','error','ok','']):
   title,text=texts[lang][j];cases.append(dict(id=f'{lang}-{width}-{kind or "normal"}',lang=lang,width=width,title=title,text=text,kind=kind))
 for j,(title,text) in enumerate([('',''),('','Message' if lang=='en' else '안내'),('Title' if lang=='en' else '제목',''),('   ','   '),('A\n B','C\n D')]):
  cases.append(dict(id=f'{lang}-empty-{j}',lang=lang,width=452,title=title,text=text,kind=''))
for lang in texts:
 for width in [240,452,860]:
  cases.append(dict(id=f'{lang}-{width}-cleanup',lang=lang,width=width,title='시험 판정과 정리는 별도입니다' if lang=='ko' else 'Behavior outcomes and cleanup are separate',text='예시: 기대 3개 충족·1개 불충족. 앱 생성 자료 정리 실패 1건, 추적 가능한 잔여 T-001과 확인 불가 기록을 별도로 보고합니다. 실제 DB 변경은 없습니다.' if lang=='ko' else 'Sample: 3 expectations met, 1 unmet. One cleanup failure, a traceable remaining artifact T-001, and unconfirmed effects are reported separately. No real DB was changed.',kind='warn'))
 cases.append(dict(id=f'{lang}-longword',lang=lang,width=240,title='DiagnosticToken'*8,text='DiagnosticToken'*6,kind='error'))
parts=['<!doctype html><meta charset="utf-8"><title>Notice · original isolation</title><link rel="stylesheet" href="notice-original.css"><style>body{min-width:960px;height:10000px}.fixture{position:absolute}.fixture .notice{animation:none}.baseline{display:inline-block;width:0;height:0;padding:0;margin:0;vertical-align:baseline}</style>']
for i,c in enumerate(cases):
 icon='<path d="m12 3 10 18H2L12 3Z"></path><path d="M12 9v5m0 3v.1"></path>' if c['kind'] in ['warn','error'] else '<circle cx="12" cy="12" r="9"></circle><path d="M12 11v6m0-10v.1"></path>'
 marker='<span class="baseline" data-baseline></span>' if c['title'].strip() else ''
 parts.append(f'<div class="fixture" lang="{c["lang"]}" style="left:30px;top:{30+i*260}px;width:{c["width"]}px"><div data-id="{c["id"]}" class="notice {c["kind"]}" role="{"alert" if c["kind"]=="error" else "note"}"><svg class="svg" viewBox="0 0 24 24" aria-hidden="true">{icon}</svg><div><strong>{html.escape(c["title"])}</strong>{marker}<p>{html.escape(c["text"])}</p></div></div></div>')
parts.append('''<button type="button" id="notice-record" style="position:fixed;right:20px;top:20px">Record Notice</button><script>
function measurements(){return [...document.querySelectorAll('.notice')].map(e=>{const r=e.getBoundingClientRect(),s=getComputedStyle(e),d=e.querySelector('div'),b=d.getBoundingClientRect(),p=e.querySelector('p'),strong=e.querySelector('strong'),marker=e.querySelector('[data-baseline]');const box=n=>{const q=n.getBoundingClientRect();return {x:q.x-r.x,y:q.y-r.y,width:q.width,height:q.height}};return{id:e.dataset.id,rect:{width:r.width,height:r.height},icon:box(e.querySelector('svg')),body:box(d),paragraph:box(p),titleFragments:[...strong.getClientRects()].map(q=>({x:q.x-r.x,y:q.y-r.y,width:q.width,height:q.height})),baseline:marker?box(marker).y:null,role:e.getAttribute('role'),style:{bg:s.backgroundColor,color:s.color,border:s.borderColor,radius:s.borderRadius,borderWidth:s.borderWidth,padding:s.padding,gap:s.gap,marginTop:s.marginTop,titleColor:getComputedStyle(strong).color,titleWeight:getComputedStyle(strong).fontWeight,titleSize:getComputedStyle(strong).fontSize,lineHeight:s.lineHeight,paragraphHeight:getComputedStyle(p).lineHeight}}})}
document.getElementById('notice-record').onclick=async()=>{const response=await fetch('/evidence/source-notice.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({dpr:devicePixelRatio,rows:measurements()})});if(!response.ok)throw Error('Example evidence failed');document.getElementById('notice-record').textContent='Notice recorded';};
</script>''')
(out/'notice.html').write_text('\n'.join(parts))
for dest in [out/'notice-cases.json',root/'docs/replica/source/notice-cases.json']:dest.write_text(json.dumps(cases,ensure_ascii=False,indent=2)+'\n')
print(len(cases),'Notice cases')
