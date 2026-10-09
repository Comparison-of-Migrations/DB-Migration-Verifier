// Official AX/CGEvent input scoped to this worker's frozen HomePage bundle.
import AppKit
import ApplicationServices
import Foundation
var releasePoint:CGPoint?
func fail(_ s:String)->Never { if let p=releasePoint { CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap) };fputs(s+"\n",stderr);exit(1) }
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef? { var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil }
func text(_ e:AXUIElement,_ n:String)->String { attr(e,n) as? String ?? "" }
func rect(_ e:AXUIElement)->CGRect? { guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID() else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as! AXValue,.cgPoint,&p),AXValueGetValue(b as! AXValue,.cgSize,&s) else{return nil};return CGRect(origin:p,size:s) }
let args=CommandLine.arguments
let modes=["inspect","close","pointer","enter","space","neutral-enter","neutral-space","language","passive-help","passive-hint","passive-gap"]
guard args.count==5,modes.contains(args[1]),["start","open","sample","research"].contains(args[4]) else { fail("Fixed Home operation required") }
let mode=args[1],path=args[2],output=args[3],identity=args[4]
let root="/Users/youptebook/workspace/DB-Migration-Verifier/"
guard path.hasPrefix(root+"artifacts/replica/workers/home/homepage/"),path.hasSuffix("/HomePageCheck.app"),output.hasPrefix(root) else {fail("Owned paths required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:root+"docs/replica/ENVIRONMENT_LEASE.json"))) as! [String:Any]
guard ((lease["resources"] as? [String:Any])?["macos"] as? [String:Any])?["owner"] as? String == "home" else {fail("Home Mac lease required")}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.homepagecheck")
guard apps.count==1,let app=apps.first,app.bundleURL?.path==path else {fail("Unique owned Home application required")}
let ax=AXUIElementCreateApplication(app.processIdentifier),windows=(attr(ax,kAXWindowsAttribute) as? [AXUIElement] ?? []).filter{text($0,kAXTitleAttribute)=="Replica HomePage verification"}
guard windows.count==1,let w=windows.first,let frame=rect(w) else {fail("Unique Home window required")}
if mode=="close" {guard let b=attr(w,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID(),AXUIElementPerformAction(b as! AXUIElement,kAXPressAction as CFString) == .success else{fail("Owned close failed")};exit(0)}
var nodes:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int){guard depth<35 else{return};nodes.append(e);for c in attr(e,kAXChildrenAttribute) as? [AXUIElement] ?? [] {visit(c,depth+1)}}
visit(w,0)
func one(_ id:String)->AXUIElement {let matches=nodes.filter{text($0,kAXIdentifierAttribute)==id};guard matches.count==1 else{fail("Exact unique control required: "+id)};return matches[0]}
if mode=="inspect" {let values=nodes.map{e->[String:Any] in var v:[String:Any]=["role":text(e,kAXRoleAttribute),"id":text(e,kAXIdentifierAttribute),"title":text(e,kAXTitleAttribute),"description":text(e,kAXDescriptionAttribute),"value":text(e,kAXValueAttribute)];if let b=rect(e){v["box"]=["x":b.minX,"y":b.minY,"width":b.width,"height":b.height]};return v};try JSONSerialization.data(withJSONObject:values,options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:output+"/homepage-ax.json"));print("AX recorded");exit(0)}
app.activate(options:[]);AXUIElementPerformAction(w,kAXRaiseAction as CFString);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.15))
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Owned Home lost foreground")}}
foreground()
let button=one(mode=="language" ? "HomePage-toggle":identity)
guard let b=rect(button),frame.contains(b),b.width>90,b.width<230,(mode=="language" ? abs(b.height-24)<0.01:abs(b.height-(identity=="research" ? 44:52))<0.01) else{fail("Exact Home control geometry required")}
if mode=="language" {guard AXUIElementPerformAction(button,kAXPressAction as CFString) == .success else{fail("Owned fixture toggle failed")};Thread.sleep(forTimeInterval:0.25);let data=try Data(contentsOf:URL(fileURLWithPath:output+"/homepage-input.json"));try data.write(to:URL(fileURLWithPath:output+"/actual-language.json"));print(String(data:data,encoding:.utf8)!);exit(0)}
func focus(){guard AXUIElementSetAttributeValue(button,kAXFocusedAttribute as CFString,kCFBooleanTrue) == .success,let f=attr(ax,kAXFocusedUIElementAttribute),CFGetTypeID(f)==AXUIElementGetTypeID(),text(f as! AXUIElement,kAXIdentifierAttribute)==identity else{fail("Exact keyboard focus required")}}
if mode=="enter" || mode=="space" || mode.hasPrefix("neutral-") {if mode.hasPrefix("neutral-"){guard let f=attr(ax,kAXFocusedUIElementAttribute),CFGetTypeID(f)==AXUIElementGetTypeID(),text(f as! AXUIElement,kAXIdentifierAttribute)=="home-body" else{fail("Neutral Home focus required")}}else{focus()};let key:CGKeyCode=mode.hasSuffix("space") ? 49:36;guard let down=CGEvent(keyboardEventSource:nil,virtualKey:key,keyDown:true),let up=CGEvent(keyboardEventSource:nil,virtualKey:key,keyDown:false) else{fail("Key event unavailable")};down.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.08);up.post(tap:.cghidEventTap)} else {
 var target=b
 if mode.hasPrefix("passive-") {focus();let data=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:output+"/homepage-input.json"))) as! [String:Any];let row=data["row"] as! [String:Any],buttons=row["buttons"] as! [[String:Any]],startBox=buttons[0]["box"] as! [String:Double];guard let sr=rect(one("start")) else{fail("Start geometry missing")};let origin=CGPoint(x:sr.minX-startBox["x"]!,y:sr.minY-startBox["y"]!);let box:[String:Double];if mode=="passive-help"{box=(row["help"] as! [String:Any])["box"] as! [String:Double]}else if mode=="passive-hint"{box=((row["hint"] as! [String:Any])["text"] as! [String:Any])["box"] as! [String:Double]}else{let entry=row["entry"] as! [String:Double];box=["x":entry["x"]!+entry["width"]!/2,"y":entry["y"]!+entry["height"]!-12,"width":1,"height":1]};target=CGRect(x:origin.x+box["x"]!,y:origin.y+box["y"]!,width:box["width"]!,height:box["height"]!) }
 let p=CGPoint(x:target.midX,y:target.midY);guard frame.contains(p) else{fail("Pointer outside owned window")};let old=CGEvent(source:nil)?.location
 func send(_ t:CGEventType,_ p:CGPoint){guard let e=CGEvent(mouseEventSource:nil,mouseType:t,mouseCursorPosition:p,mouseButton:.left) else{fail("Pointer event unavailable")};e.post(tap:.cghidEventTap)}
 var down=false;defer{if down{send(.leftMouseUp,p)};if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let old{send(.mouseMoved,old)}}
 send(.mouseMoved,p);Thread.sleep(forTimeInterval:0.12);foreground();var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(p.x),Float(p.y),&hit) == .success,let hit else{fail("Owned hit unavailable")};var hitPid:pid_t=0;AXUIElementGetPid(hit,&hitPid);guard hitPid==app.processIdentifier else{fail("Hit another application")};send(.leftMouseDown,p);down=true;releasePoint=p;Thread.sleep(forTimeInterval:0.06);send(.leftMouseUp,p);down=false;releasePoint=nil
}
foreground();Thread.sleep(forTimeInterval:0.25);let data=try Data(contentsOf:URL(fileURLWithPath:output+"/homepage-input.json"));let state=try JSONSerialization.jsonObject(with:data) as! [String:Any];try data.write(to:URL(fileURLWithPath:output+"/actual-"+(state["english"] as! Bool ? "en":"ko")+"-"+mode+"-"+identity+".json"));print(String(data:data,encoding:.utf8)!)
