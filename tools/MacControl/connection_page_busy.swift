// Approved project-only AX/CGEvent verification, exact owned ConnectionPage app/window.
import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
guard CommandLine.arguments.count==2,["normal","bad"].contains(CommandLine.arguments[1])else{fail("Fixed normal/bad busy-input scenario required")}
let value=CommandLine.arguments[1]=="bad" ? "bad.example.invalid":"source-busy.example.invalid"
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.connectionpagecheck")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/ConnectionPageCheck.app"else{fail("Unique owned ConnectionPage app required")}
let windows=attr(AXUIElementCreateApplication(app.processIdentifier),kAXWindowsAttribute)as?[AXUIElement] ?? [],owned=windows.filter{text($0,kAXTitleAttribute)=="Replica ConnectionPage verification"}
guard owned.count==1,let w=owned.first,let frame=rect(w)else{fail("Unique owned ConnectionPage window required")}
func target(_ id:String)->AXUIElement{var items:[AXUIElement]=[];func visit(_ e:AXUIElement,_ depth:Int){guard depth<30 else{return};if text(e,kAXIdentifierAttribute)==id{items.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,depth+1)}};visit(w,0);guard items.count==1 else{fail("Unique owned fixed element required: "+id)};return items[0]}
let button=target("Footer-connect")
guard attr(button,kAXEnabledAttribute)as?Bool==true,let b=rect(button),abs(b.height-48)<0.01,b.width>=164,b.width<350 else{fail("Owned check button geometry/state mismatch")}
let p=CGPoint(x:b.midX,y:b.midY);guard frame.contains(p)else{fail("Pointer outside owned frame")}
app.activate(options:[]);AXUIElementPerformAction(w,kAXRaiseAction as CFString);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.15))
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Owned ConnectionPage not foreground")}}
func send(_ t:CGEventType,_ point:CGPoint){CGEvent(mouseEventSource:nil,mouseType:t,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap)}
let previous=CGEvent(source:nil)?.location;var down=false
defer{if down{send(.leftMouseUp,p)};if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let old=previous{send(.mouseMoved,old)}}
foreground();var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(p.x),Float(p.y),&hit) == .success,let hit else{fail("Owned button hit missing")};var pid:pid_t=0;AXUIElementGetPid(hit,&pid);guard pid==app.processIdentifier else{fail("Pointer hit another app")}
send(.mouseMoved,p);send(.leftMouseDown,p);down=true;Thread.sleep(forTimeInterval:0.025);send(.leftMouseUp,p);down=false
let deadline=Date(timeIntervalSinceNow:0.35);var busy=false
repeat{if attr(target("Footer-connect"),kAXEnabledAttribute)as?Bool==false{busy=true;break};RunLoop.current.run(until:Date(timeIntervalSinceNow:0.01))}while Date()<deadline
guard busy else{fail("Busy state not observed within bounded interval")};foreground()
let host=target("source-host");guard let h=rect(host),h.width>100,h.height>=44,frame.contains(CGPoint(x:h.midX,y:h.midY))else{fail("Owned input geometry mismatch")}
guard AXUIElementSetAttributeValue(host,kAXFocusedAttribute as CFString,kCFBooleanTrue) == .success else{fail("Owned busy field focus refused")}
guard AXUIElementSetAttributeValue(host,kAXValueAttribute as CFString,value as CFString) == .success else{fail("Owned busy input refused")}
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.8))
let folder=URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/connectionpage-mac-input")
try Data(contentsOf:folder.appendingPathComponent("connectionpage-input.json")).write(to:folder.appendingPathComponent(CommandLine.arguments[1]=="bad" ? "busy-invalid.json":"busy-input.json"))
print("Owned busy input delivered; inspect new timeline/state evidence")
