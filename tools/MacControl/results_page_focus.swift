// Approved project scope: exact owned ResultsPage path/title/frame/foreground/hit and held release.
import AppKit
import ApplicationServices
import Foundation
let args=CommandLine.arguments
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n) as? String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
guard args.count==4,["click","space","hold"].contains(args[1]),args[3].hasPrefix("/Users/youptebook/workspace/DB-Migration-Verifier/")else{fail("Fixed focus operation/output required")}
let source=args[2]=="/Applications/Google Chrome.app"
guard source || (args[2].hasPrefix("/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/workers/connection/resultspage/")&&args[2].hasSuffix("/ResultsPageCheck.app"))else{fail("Owned runtime path required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any]
guard ((lease["resources"]as![String:Any])["macos"]as![String:Any])["owner"]as?String == "connection"else{fail("Connection Mac resource required")}
let apps=NSWorkspace.shared.runningApplications.filter{$0.bundleURL?.path==args[2]};guard apps.count==1,let app=apps.first else{fail("Exact process required")}
let ax=AXUIElementCreateApplication(app.processIdentifier)
let windows=(attr(ax,kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{source ? text($0,kAXTitleAttribute).hasPrefix("ResultsPage tracks · original isolation") : text($0,kAXTitleAttribute)=="Replica ResultsPage verification"}
guard windows.count==1,let window=windows.first,let frame=rect(window),frame.width>=600,frame.width<=3000,frame.height>=500,frame.height<=2000 else{fail("Exact owned window/frame required")}
var targets:[AXUIElement]=[]
func find(_ e:AXUIElement,_ depth:Int){guard depth<35 else{return};if text(e,kAXRoleAttribute)==kAXButtonRole && (source ? text(e,kAXTitleAttribute)=="확인 불가·미검증" : text(e,kAXIdentifierAttribute)=="Results-filter-unknown"){targets.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{find(c,depth+1)}}
find(window,0)
guard targets.count==1,let button=targets.first,let box=rect(button),frame.contains(box),box.width>=80,box.width<=250,box.height>=30,box.height<=70,(attr(button,kAXEnabledAttribute)as?Bool)==true else{fail("Fixed enabled visible filter required")}
app.activate(options:[]);AXUIElementPerformAction(window,kAXRaiseAction as CFString);Thread.sleep(forTimeInterval:0.1)
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Owned foreground lost")}}
foreground();let begin=Date();let previous=CGEvent(source:nil)?.location;let point=CGPoint(x:box.midX,y:box.midY)
var held=false;var key=false
defer{if held{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap)};if key{CGEvent(keyboardEventSource:nil,virtualKey:49,keyDown:false)?.post(tap:.cghidEventTap)};if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let old=previous{CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:old,mouseButton:.left)?.post(tap:.cghidEventTap)}}
if args[1]=="space"{guard (attr(button,kAXFocusedAttribute)as?Bool)==true else{fail("Current filter focus required; do not change focus")};key=true;CGEvent(keyboardEventSource:nil,virtualKey:49,keyDown:true)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.08);CGEvent(keyboardEventSource:nil,virtualKey:49,keyDown:false)?.post(tap:.cghidEventTap);key=false}else{
 CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.18);foreground();var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(point.x),Float(point.y),&hit) == .success,let hit else{fail("Fresh filter hit missing")};var pid:pid_t=0;AXUIElementGetPid(hit,&pid);guard pid==app.processIdentifier else{fail("Filter hit another process")};held=true;CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:args[1]=="hold" ? 0.25:0.02);foreground();CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap);held=false
}
Thread.sleep(forTimeInterval:0.5);foreground()
try JSONSerialization.data(withJSONObject:["mode":args[1],"pid":app.processIdentifier,"api":"CGEvent","elapsedMs":Date().timeIntervalSince(begin)*1000,"title":text(window,kAXTitleAttribute),"source":source,"buttonRect":["x":box.minX,"y":box.minY,"width":box.width,"height":box.height]],options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:args[3]+"/focus-"+args[1]+"-control.json"))
print("Guarded focus input completed",args[1])
