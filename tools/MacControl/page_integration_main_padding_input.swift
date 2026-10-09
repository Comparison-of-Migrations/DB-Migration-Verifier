import AppKit
import ApplicationServices
import Foundation
var held:CGPoint?
func release(){if let p=held{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);held=nil}}
func fail(_ s:String)->Never{release();fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
func box(_ b:CGRect)->[String:Double]{["x":b.minX,"y":b.minY,"width":b.width,"height":b.height]}
func num(_ d:[String:Any],_ key:String)->Double{((d[key] ?? d[key.uppercased()])as?NSNumber)?.doubleValue ?? -1}
let a=CommandLine.arguments,root="/Users/youptebook/workspace/DB-Migration-Verifier/"
guard a.count==8,let pid=Int32(a[1]),["source","native"].contains(a[2]),["wheel","click","next","close","inspect"].contains(a[3]),a[4].hasPrefix(root+"artifacts/replica/workers/preflight/"),a[5].hasPrefix(root+"artifacts/replica/workers/preflight/"),!a[6].contains("/"),!a[7].isEmpty else{fail("Owned finite padding operation required")}
let mode=a[3],source=a[2]=="source",identity=a[4],statePath=a[5],label=a[6],output=a[7]
guard output.hasPrefix(root+"artifacts/replica/workers/preflight/")else{fail("Owned output required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:root+"docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any];guard ((lease["resources"]as![String:Any])["macos"]as![String:Any])["owner"]as?String=="preflight"else{fail("Preflight Mac lease required")}
let launch=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:identity)))as![String:Any];guard (launch["pid"]as?NSNumber)?.int32Value==pid,let app=NSRunningApplication(processIdentifier:pid)else{fail("Exact launched PID required")}
let title:String
if source{
 guard app.executableURL?.path=="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",let profile=launch["profile"]as?String,profile.hasPrefix(root+"artifacts/replica/workers/preflight/mainpadding-source-native/"),let nonce=launch["nonce"]as?String else{fail("Owned private source browser required")}
 let ps=Process();ps.executableURL=URL(fileURLWithPath:"/bin/ps");ps.arguments=["-p",String(pid),"-o","command="];let pipe=Pipe();ps.standardOutput=pipe;try ps.run();ps.waitUntilExit();let command=String(data:pipe.fileHandleForReading.readDataToEndOfFile(),encoding:.utf8) ?? "";guard command.contains(profile),command.contains("Google Chrome")else{fail("Source exactprofile lost")};title="Main padding original "+nonce
}else{
 guard let bundle=launch["bundle"]as?String,bundle.hasPrefix(root+"artifacts/replica/workers/preflight/pageintegration/"),bundle.hasSuffix("/MainPaddingCheck.app"),app.bundleURL?.path==bundle else{fail("Frozen owned native bundle required")};title="Replica PageIntegration application verification"
}
let application=AXUIElementCreateApplication(pid);let windows=(attr(application,kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{source ? text($0,kAXTitleAttribute).hasPrefix(title):text($0,kAXTitleAttribute)==title};guard windows.count==1,let window=windows.first,let frame=rect(window)else{fail("One exact titled window required")}
if mode=="close"{guard let e=attr(window,kAXCloseButtonAttribute),CFGetTypeID(e)==AXUIElementGetTypeID(),AXUIElementPerformAction(e as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Exact close refused")};exit(0)}
var nodes:[AXUIElement]=[];func walk(_ e:AXUIElement,_ depth:Int){guard depth<40,nodes.count<1600 else{return};if nodes.contains(where:{CFEqual($0,e)}){return};nodes.append(e);for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{walk(c,depth+1)}};walk(window,0)
if mode=="inspect"{let rows=nodes.map{e->[String:Any]in var r:[String:Any]=["role":text(e,kAXRoleAttribute),"name":text(e,kAXTitleAttribute),"description":text(e,kAXDescriptionAttribute),"id":text(e,kAXIdentifierAttribute)];if let b=rect(e){r["box"]=box(b)};return r};try JSONSerialization.data(withJSONObject:["frame":box(frame),"nodes":rows],options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:output+"/"+label+"-ax.json"));exit(0)}
app.activate(options:[]);AXUIElementPerformAction(window,kAXRaiseAction as CFString);let end=Date(timeIntervalSinceNow:2);while NSWorkspace.shared.frontmostApplication?.processIdentifier != pid&&Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.02))};func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==pid else{fail("Own foreground lost")}};foreground();defer{release()}
if mode=="next"{guard !source,let down=CGEvent(keyboardEventSource:nil,virtualKey:100,keyDown:true),let up=CGEvent(keyboardEventSource:nil,virtualKey:100,keyDown:false)else{fail("Native fixture F8 only")};down.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.03);foreground();up.post(tap:.cghidEventTap);exit(0)}
let state=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:statePath)))as![String:Any];let point:CGPoint;var anchor:[String:Any]=[:]
if source{
 guard let rows=state["rows"]as?[[String:Any]],rows.count==1,let embedding=rows[0]["embedding"]as?[String:Any],let fb=embedding["frameBox"]as?[String:Any],let points=rows[0]["points"]as?[[String:Any]],let p=points.first(where:{$0["id"]as?String=="left-padding"}),let target=p["target"]as?[String:Any],target["id"]as?String=="main"else{fail("Fresh source DOM blank main required")}
 let roots=nodes.filter{text($0,kAXRoleAttribute)=="AXWebArea"&&text($0,kAXTitleAttribute)==title};guard roots.count==1,let web=roots.first,let b=rect(web)else{fail("Exact outer source WebArea anchor required")};point=CGPoint(x:b.minX+num(fb,"x")+num(p,"x"),y:b.minY+num(fb,"y")+num(p,"y"));anchor=["sourceWebArea":box(b),"iframe":fb,"domPoint":p]
}else{
 guard state["ready"]as?Bool==true,let h=state["heading"]as?[String:Any],let hb=h["box"]as?[String:Any],let name=h["text"]as?String,let p=state["point"]as?[String:Any]else{fail("Fresh native anchor required")}
 let matches=nodes.filter{e in let n=text(e,kAXTitleAttribute).isEmpty ? text(e,kAXDescriptionAttribute):text(e,kAXTitleAttribute);guard n==name,let b=rect(e)else{return false};return abs(b.width-num(hb,"width"))<0.1&&abs(b.height-num(hb,"height"))<0.1};guard let target=matches.first,let b=rect(target),!matches.contains(where:{rect($0) != b})else{fail("Consistent current heading native anchor required")};point=CGPoint(x:b.minX-num(hb,"x")+num(p,"x"),y:b.minY-num(hb,"y")+num(p,"y"));anchor=["headingAX":box(b),"managedHeading":hb,"managedPoint":p]
}
CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.05);foreground();let cursor=CGEvent(source:nil)!.location;guard frame.contains(point),hypot(cursor.x-point.x,cursor.y-point.y)<=1 else{fail("Fresh frame/cursor containment required")}
var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(point.x),Float(point.y),&hit) == .success,let hit else{fail("Fresh AX hit required")};var owner:pid_t=0;AXUIElementGetPid(hit,&owner);guard owner==pid else{fail("Fresh hit other application")}
let guardRecord:[String:Any]=["pid":pid,"mode":mode,"source":source,"point":["x":point.x,"y":point.y],"cursor":["x":cursor.x,"y":cursor.y],"frame":box(frame),"anchor":anchor,"hitPID":owner,"hitRole":text(hit,kAXRoleAttribute),"hitName":text(hit,kAXTitleAttribute),"foregroundPID":NSWorkspace.shared.frontmostApplication!.processIdentifier,"beforeInput":true]
try JSONSerialization.data(withJSONObject:guardRecord,options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:output+"/"+label+"-guard.json"))
if mode=="wheel"{guard let event=CGEvent(scrollWheelEvent2Source:nil,units:.pixel,wheelCount:1,wheel1:-100,wheel2:0,wheel3:0)else{fail("Wheel unavailable")};event.location=point;event.post(tap:.cghidEventTap)}else{CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap);held=point;Thread.sleep(forTimeInterval:0.03);foreground();release()}
try JSONSerialization.data(withJSONObject:["pid":pid,"mode":mode,"label":label,"sent":true],options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:output+"/"+label+"-sent.json"))
