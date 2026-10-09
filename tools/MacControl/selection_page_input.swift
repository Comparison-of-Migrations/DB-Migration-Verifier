import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n) as? String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let p=attr(e,kAXPositionAttribute),let s=attr(e,kAXSizeAttribute),CFGetTypeID(p)==AXValueGetTypeID(),CFGetTypeID(s)==AXValueGetTypeID()else{return nil};var pp=CGPoint.zero,ss=CGSize.zero;guard AXValueGetValue(p as!AXValue,.cgPoint,&pp),AXValueGetValue(s as!AXValue,.cgSize,&ss)else{return nil};return CGRect(origin:pp,size:ss)}
let a=CommandLine.arguments;guard a.count==3,["guard","activate","inspect","language","expand-data","category-data","search-orders","deselect","bulk","mapping","escape","clear-search","search-none","passive","category-trial","trial","back","scroll-table","scroll-top","close","cleanup"].contains(a[1])else{fail("Fixed own Selection source input mode required")}
let root=URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier"),out=URL(fileURLWithPath:a[2]).standardizedFileURL,mode=a[1],title="SelectionPage Input · original isolation",url="http://10.211.55.2:8768/selectionpage-input.html?os=mac&batch="+out.lastPathComponent+"&resume=after-trial-navigation"
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:root.appendingPathComponent("docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any];guard (((lease["resources"]as?[String:Any])?["macos"]as?[String:Any])?["owner"]as?String)=="preflight"else{fail("Own Mac Selection lease absent")}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"com.google.Chrome").filter{$0.bundleURL?.standardizedFileURL.path=="/Applications/Google Chrome.app"}
func owned()->[AXUIElement]{apps.flatMap{(attr(AXUIElementCreateApplication($0.processIdentifier),kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute).hasPrefix(title)&&text($0,kAXDocumentAttribute)==url}}}
func save(_ label:String,_ data:Any)throws{try JSONSerialization.data(withJSONObject:data,options:[.prettyPrinted,.sortedKeys]).write(to:out.appendingPathComponent(label+".json"))}
if mode=="guard"{guard owned().isEmpty else{fail("Own source already exists")};exit(0)}
if mode=="cleanup"{guard owned().isEmpty else{fail("Own source remains")};try save("source-cleanup",["sourceWindowCount":0,"url":url,"title":title]);exit(0)}
guard owned().count==1,let w=owned().first else{fail("Unique exact source title/URL missing")};var pid:pid_t=0;AXUIElementGetPid(w,&pid);guard let app=NSRunningApplication(processIdentifier:pid),let frame=rect(w)else{fail("Exact source PID/frame absent")}
if mode=="close"{guard let b=attr(w,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID(),AXUIElementPerformAction(b as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Own source AXClose refused")};exit(0)}
func activate(){guard app.activate(options:[])else{fail("Own activation failed")};guard AXUIElementPerformAction(w,kAXRaiseAction as CFString) == .success else{fail("Own raise refused")};RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))}
if mode=="activate"{activate();exit(0)}
var nodes:[AXUIElement]=[]
func walk(_ e:AXUIElement,_ d:Int){guard d<25,nodes.count<2000,!nodes.contains(where:{CFEqual($0,e)})else{return};nodes.append(e);for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{walk(c,d+1)}};walk(w,0)
func name(_ e:AXUIElement)->String{for k in [kAXTitleAttribute,kAXDescriptionAttribute,kAXValueAttribute]{let s=text(e,k);if !s.isEmpty{return s}};return ""}
func tree(_ e:AXUIElement,_ d:Int)->[String:Any]{var v:[String:Any]=["role":text(e,kAXRoleAttribute),"id":text(e,kAXIdentifierAttribute),"title":text(e,kAXTitleAttribute),"description":text(e,kAXDescriptionAttribute),"value":text(e,kAXValueAttribute),"focused":attr(e,kAXFocusedAttribute)as?Bool ?? false];if let b=rect(e){v["box"]=["x":b.minX,"y":b.minY,"width":b.width,"height":b.height]};if d<20{var unique:[AXUIElement]=[];for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{if !unique.contains(where:{CFEqual($0,c)}){unique.append(c)}};v["children"]=unique.prefix(80).map{tree($0,d+1)}};return v}
let record=root.appendingPathComponent("docs/replica/source/preflight-selectionpage-mac-input.json")
if let web=nodes.last(where:{text($0,kAXRoleAttribute)=="AXWebArea"}){nodes=[];walk(web,0)}
let before=try Data(contentsOf:record),old=try JSONSerialization.jsonObject(with:before)as![String:Any];guard old["batch"]as?String==out.lastPathComponent else{fail("Fresh own source record required")}
let serial=(old["sequence"]as?Int) ?? 0
if mode=="inspect"{let web=nodes.filter{text($0,kAXRoleAttribute)=="AXWebArea"};try save("source-ax-"+((old["language"]as?String) ?? "unknown"),tree(web.last ?? w,0));try before.write(to:out.appendingPathComponent("source-inspect-"+((old["language"]as?String) ?? "unknown")+".json"));exit(0)}
activate();func front(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==pid else{fail("Own source lost foreground")}};front()
try before.write(to:out.appendingPathComponent(mode+"-before.json"))
func target(_ role:String,_ labels:[String],prefix:Bool=false)->AXUIElement{let found=nodes.filter{e in text(e,kAXRoleAttribute)==role&&(prefix ? labels.contains(where:{label in name(e).hasPrefix(label)}) : labels.contains(name(e)))};guard found.count==1 else{try? save(mode+"-missing",["role":role,"labels":labels,"count":found.count,"nodes":nodes.map{["role":text($0,kAXRoleAttribute),"name":name($0)]}]);fail("Unique own target missing "+mode+" count="+String(found.count))};return found[0]}
func click(_ e:AXUIElement){if let b=rect(e),!frame.contains(b){_ = AXUIElementPerformAction(e,"AXScrollToVisible" as CFString);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.15))};guard let b=rect(e),b.width>0,b.height>0,frame.contains(b)else{try? save(mode+"-geometry-rejected",["frame":["x":frame.minX,"y":frame.minY,"width":frame.width,"height":frame.height],"target":tree(e,0),"down":0]);fail("Exact source target outside own visible frame")};let p=CGPoint(x:b.midX,y:b.midY);CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.06);front();var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(p.x),Float(p.y),&hit) == .success,let hit else{fail("Own hit unavailable beforeDown")};var hp:pid_t=0;AXUIElementGetPid(hit,&hp);guard hp==pid else{fail("Own hit PID mismatch beforeDown")};var within=false;var ancestor:AXUIElement?=hit;for _ in 0..<20{guard let current=ancestor else{break};if CFEqual(current,e){within=true;break};ancestor=attr(current,kAXParentAttribute)as!AXUIElement?};if !within {
    let rejected:[String:Any] = ["target":tree(e,0),"hit":tree(hit,0),"down":0]
    try? save(mode+"-hit-rejected", rejected)
    // Only the observed trial-checkbox observer failure admits an independently proved guard.
    guard mode == "category-trial", let hb = rect(hit), !hb.contains(p),
        let web = nodes.first(where:{text($0,kAXRoleAttribute)=="AXWebArea"}), let viewport = rect(web),
        let freshData = try? Data(contentsOf:record), let fresh = try? JSONSerialization.jsonObject(with:freshData) as? [String:Any],
        fresh["batch"] as? String == out.lastPathComponent, let geometry = fresh["geometry"] as? [String:Any],
        let timestamp = geometry["timestamp"] as? Double, abs(Date().timeIntervalSince1970*1000-timestamp) < 1000,
        let trial = geometry["trial"] as? [String:Double], let clip = geometry["mainClip"] as? [String:Double],
        let domHit = geometry["hit"] as? [String:Any], domHit["category"] as? String == "trial" else {fail("Own target hit ancestry mismatch beforeDown")}
    let dom = CGRect(x:trial["x"]!,y:trial["y"]!,width:trial["width"]!,height:trial["height"]!)
    let main = CGRect(x:clip["x"]!,y:clip["y"]!,width:clip["width"]!,height:clip["height"]!)
    let mapped = dom.offsetBy(dx:viewport.minX,dy:viewport.minY)
    guard main.contains(CGPoint(x:dom.midX,y:dom.midY)), abs(mapped.midX-p.x) <= 1, abs(mapped.midY-p.y) <= 1,
        let cursor = CGEvent(source:nil)?.location, abs(cursor.x-p.x) <= 1, abs(cursor.y-p.y) <= 1 else {fail("Fresh DOM/mainclip/physicalcursor proof failed beforeDown")}
    let windows = CGWindowListCopyWindowInfo([.optionOnScreenOnly,.excludeDesktopElements],kCGNullWindowID) as? [[String:Any]] ?? []
    let atPoint = windows.first { item in
        guard (item[kCGWindowLayer as String] as? Int) == 0, let bounds = item[kCGWindowBounds as String] as? [String:Any], let box = CGRect(dictionaryRepresentation:bounds as CFDictionary) else{return false}
        return box.contains(p)
    }
    guard let atPoint, (atPoint[kCGWindowOwnerPID as String] as? Int) == Int(pid),
        let bounds = atPoint[kCGWindowBounds as String] as? [String:Any], let actual = CGRect(dictionaryRepresentation:bounds as CFDictionary),
        abs(actual.minX-frame.minX)<=2, abs(actual.minY-frame.minY)<=2, abs(actual.width-frame.width)<=2, abs(actual.height-frame.height)<=2 else {fail("Official window-at-point ownPID/frame proof failed beforeDown")}
    try? save(mode+"-alternative-hitguard",["pid":pid,"title":text(w,kAXTitleAttribute),"url":url,"windowAtPoint":atPoint,"geometry":geometry,"cursor":["x":cursor.x,"y":cursor.y],"axTarget":tree(e,0),"axHitRejected":tree(hit,0),"down":0])
};try? save(mode+"-hit",["pid":pid,"point":["x":p.x,"y":p.y],"target":tree(e,0),"hit":tree(hit,0)]);CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.03);CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)}
func key(_ code:CGKeyCode){front();CGEvent(keyboardEventSource:nil,virtualKey:code,keyDown:true)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.03);CGEvent(keyboardEventSource:nil,virtualKey:code,keyDown:false)?.post(tap:.cghidEventTap)}
if mode=="scroll-table"||mode=="scroll-top"{let point=CGPoint(x:frame.midX+frame.width*0.15,y:frame.midY);guard frame.contains(point)else{fail("Own scroll point missing")};CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.06);front();var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(point.x),Float(point.y),&hit) == .success,let hit else{fail("Own scroll hit unavailable")};var hp:pid_t=0;AXUIElementGetPid(hit,&hp);guard hp==pid else{fail("Own scroll hit PID mismatch")};try save(mode+"-guard",["pid":pid,"point":["x":point.x,"y":point.y],"hit":tree(hit,0)]);CGEvent(scrollWheelEvent2Source:nil,units:.pixel,wheelCount:1,wheel1:mode=="scroll-table" ? -700:700,wheel2:0,wheel3:0)?.post(tap:.cghidEventTap)}
else if mode=="escape"{key(53)}
else if mode=="search-orders"||mode=="search-none"{click(target("AXTextField",["테이블 검색","Search tables"]));let value=mode=="search-orders" ? "orders":"missing-table";for down in [true,false]{let event=CGEvent(keyboardEventSource:nil,virtualKey:0,keyDown:down)!;let chars=Array(value.utf16);event.keyboardSetUnicodeString(stringLength:chars.count,unicodeString:chars);event.post(tap:.cghidEventTap)}}
else{let labels:[String:[String]]=["language":["영어로 변경","Switch to Korean"],"expand-data":["데이터","Data"],"category-data":["데이터 검사 선택","Data check selection"],"deselect":["선택 해제","Deselect matches"],"bulk":["검색 결과 전체 선택","Select all matching tables"],"mapping":["대응 변경","Change match"],"clear-search":["테이블 검색 지우기","Clear table search"],"passive":["사용자·역할·소유자는 같은 이름으로 비교합니다.","Users, roles and owners are compared by the same name."],"category-trial":["별도 DB 동작 시험 검사 선택","Behavior tests in a separate DB check selection"],"trial":["시험 DB 준비","Prepare a test DB"],"back":["검사 선택으로","Back to selection"]];let role=mode.hasPrefix("category-")||mode=="bulk" ? "AXCheckBox":mode=="passive" ? "AXStaticText":"AXButton";click(target(role,labels[mode] ?? [],prefix:mode=="expand-data"||mode=="passive"))}
let observeAfter=Date(timeIntervalSinceNow:0.65);let end=Date(timeIntervalSinceNow:3);var fresh:Data?
while Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.08));if let d=try?Data(contentsOf:record),let s=try?JSONSerialization.jsonObject(with:d)as?[String:Any],s["batch"]as?String==out.lastPathComponent,(s["sequence"]as?Int ?? 0)>serial+3,Date()>=observeAfter{fresh=d;break}}
guard let fresh else{fail("Post input record absent; do not duplicate")};try fresh.write(to:out.appendingPathComponent(mode+"-after.json"));print(String(data:fresh,encoding:.utf8)!)
