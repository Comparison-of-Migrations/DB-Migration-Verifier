// User-authorized project verification control, exact owned CategoryHeader fixture.
import AppKit
import ApplicationServices
import Foundation
let args=CommandLine.arguments
var releasePoint: CGPoint?
func fail(_ message:String)->Never{if let p=releasePoint{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};fputs(message+"\n",stderr);exit(1)}
guard args.count==2,["inspect","button-click","choice-click","button-hold"].contains(args[1]) else{fail("Expected fixed category operation")}
let mode=args[1],apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.categoryheadercheck")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/CategoryHeaderCheck.app" else{fail("Owned application must be unique and already running")}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
let root=AXUIElementCreateApplication(app.processIdentifier),wins=attr(root,kAXWindowsAttribute)as?[AXUIElement] ?? [],owned=wins.filter{text($0,kAXTitleAttribute)=="Replica CategoryHeader verification"}
guard owned.count==1,let w=owned.first,let frame=rect(w)else{fail("Exact owned window missing")}
var buttons:[AXUIElement]=[],checks:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int){guard depth<30 else{return};if text(e,kAXIdentifierAttribute)=="CategoryToggle"{buttons.append(e)};if text(e,kAXIdentifierAttribute)=="CategoryCheck"{checks.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,depth+1)}}
visit(w,0);guard buttons.count==1,checks.count==1,let b=rect(buttons[0]),let c=rect(checks[0]),abs(b.width-858) < 0.01,abs(b.height-64) < 0.01,abs(c.width-20) < 0.01,abs(c.height-20) < 0.01 else{fail("Owned category elements/geometry mismatch")}
let p=mode=="choice-click" ? CGPoint(x:c.minX-8,y:c.midY):CGPoint(x:b.minX+b.width*0.7,y:b.midY)
guard frame.contains(p),b.contains(p)else{fail("Pointer outside owned frame/header")};let meta:[String:Any]=["pid":app.processIdentifier,"title":text(w,kAXTitleAttribute),"mode":mode,"x":p.x,"y":p.y,"api":"CGEvent","expanded":attr(buttons[0],"AXExpanded") as? NSNumber ?? -1,"role":text(buttons[0],kAXRoleAttribute)]
print(String(data:try JSONSerialization.data(withJSONObject:meta,options:.sortedKeys),encoding:.utf8)!)
guard mode != "inspect"else{exit(0)}
app.activate(options:[]);AXUIElementPerformAction(w,kAXRaiseAction as CFString);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Owned category is not foreground")}}
func send(_ t:CGEventType,_ p:CGPoint){guard let e=CGEvent(mouseEventSource:nil,mouseType:t,mouseCursorPosition:p,mouseButton:.left)else{fail("Event unavailable")};e.post(tap:.cghidEventTap)}
foreground();let previous=CGEvent(source:nil)?.location;var down=false
defer{if down{send(.leftMouseUp,p)};if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let old=previous{send(.mouseMoved,old)}}
send(.mouseMoved,p);Thread.sleep(forTimeInterval:0.18);foreground();var hit: AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(p.x),Float(p.y),&hit) == .success,let target=hit else{fail("No owned point hit")};var hitPid:pid_t=0;AXUIElementGetPid(target,&hitPid);guard hitPid==app.processIdentifier else{fail("Pointer hit another app")};send(.leftMouseDown,p);down=true;releasePoint=p;Thread.sleep(forTimeInterval:mode=="button-hold" ? 0.18:0.08)
let folder="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/categoryheader-mac-input/"
if mode=="button-hold"{let data=try Data(contentsOf:URL(fileURLWithPath:folder+"categoryheader-input.json"));try data.write(to:URL(fileURLWithPath:folder+"actual-held.json"))}
// Always release, including unexpected foreground loss.
send(.leftMouseUp,p);down=false;releasePoint=nil;foreground();Thread.sleep(forTimeInterval:0.3);let data=try Data(contentsOf:URL(fileURLWithPath:folder+"categoryheader-input.json"));try data.write(to:URL(fileURLWithPath:folder+"actual-"+mode+".json"))
