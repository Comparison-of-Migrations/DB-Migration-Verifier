// Official project-scoped AX/CGEvent input, exact owned row only.
import AppKit
import ApplicationServices
import Foundation
var releasePoint:CGPoint?
func fail(_ s:String)->Never{if let p=releasePoint{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
let args=CommandLine.arguments;guard args.count==2,["inspect","button-click","passive-click","button-cancel","space","enter","language-click"].contains(args[1])else{fail("Fixed row operation required")};let mode=args[1]
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.capabilityrowcheck");guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/CapabilityRowCheck.app"else{fail("Unique owned row app required")}
let ax=AXUIElementCreateApplication(app.processIdentifier),wins=(attr(ax,kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute)=="Replica CapabilityRow verification"};guard wins.count==1,let w=wins.first,let frame=rect(w)else{fail("Unique owned row window required")}
var buttons:[AXUIElement]=[],cells:[AXUIElement]=[],languages:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int){guard depth<30 else{return};if text(e,kAXIdentifierAttribute)=="CapabilityRow-permissions"{buttons.append(e)};if text(e,kAXIdentifierAttribute)=="CapabilityRow-language"{languages.append(e)};if text(e,kAXIdentifierAttribute)=="CapabilityRow-cell-0"{cells.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,depth+1)}}
visit(w,0);guard buttons.count==1,cells.count==1,let button=buttons.first,let b=rect(button),let c=rect(cells[0]),b.width>90,b.width<170,abs(b.height-44)<0.01,c.width>270,c.width<310,c.height>65,c.height<150 else{fail("Owned row/button geometry mismatch")}
print("Owned",text(w,kAXTitleAttribute),mode,"cell",text(cells[0],kAXRoleAttribute),"button",text(button,kAXRoleAttribute));guard mode != "inspect"else{exit(0)}
app.activate(options:[]);AXUIElementPerformAction(w,kAXRaiseAction as CFString);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Owned row lost foreground")}}
foreground();let previous=CGEvent(source:nil)?.location
func recordState()throws{foreground();Thread.sleep(forTimeInterval:0.3);let folder="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/capabilityrow-mac-input/";let data=try Data(contentsOf:URL(fileURLWithPath:folder+"capabilityrow-input.json"));try data.write(to:URL(fileURLWithPath:folder+"actual-"+mode+".json"));print(String(data:data,encoding:.utf8)!)}
if mode=="space"||mode=="enter"{
 guard AXUIElementSetAttributeValue(button,kAXFocusedAttribute as CFString,kCFBooleanTrue) == .success,let focused=attr(ax,kAXFocusedUIElementAttribute),CFGetTypeID(focused)==AXUIElementGetTypeID(),text(focused as!AXUIElement,kAXIdentifierAttribute)=="CapabilityRow-permissions"else{fail("Exact keyboard focus missing")}
 let key:CGKeyCode=mode=="space" ? 49:36;guard let down=CGEvent(keyboardEventSource:nil,virtualKey:key,keyDown:true),let up=CGEvent(keyboardEventSource:nil,virtualKey:key,keyDown:false)else{fail("Keyboard event unavailable")};down.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.08);up.post(tap:.cghidEventTap)
}else{
 print("Utility rects",languages.compactMap{rect($0)});let targetRect: CGRect;if mode=="language-click"{guard languages.count==1,let lr=rect(languages[0]),abs(lr.width-49.3828125)<0.01,abs(lr.height-12)<0.01,frame.contains(lr)else{fail("Exact utility geometry required")};targetRect=lr}else{targetRect=mode=="passive-click" ? c:b};let p=CGPoint(x:targetRect.midX,y:targetRect.midY);guard frame.contains(p)else{fail("Owned pointer target outside frame")}
 func send(_ t:CGEventType,_ p:CGPoint){guard let e=CGEvent(mouseEventSource:nil,mouseType:t,mouseCursorPosition:p,mouseButton:.left)else{fail("Pointer event unavailable")};e.post(tap:.cghidEventTap)}
 var down=false;defer{if down{send(.leftMouseUp,p)};if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let old=previous{send(.mouseMoved,old)}}
 send(.mouseMoved,p);Thread.sleep(forTimeInterval:0.18);foreground();var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(p.x),Float(p.y),&hit) == .success,let hit else{fail("Owned pointer hit unavailable")};var hitPid:pid_t=0;AXUIElementGetPid(hit,&hitPid);guard hitPid==app.processIdentifier else{fail("Pointer hit another app")};send(.leftMouseDown,p);down=true;releasePoint=p;Thread.sleep(forTimeInterval:0.08);var upPoint=p;if mode=="button-cancel"{upPoint=CGPoint(x:frame.maxX-30,y:frame.maxY-30);releasePoint=upPoint;send(.mouseMoved,upPoint);Thread.sleep(forTimeInterval:0.05)};send(.leftMouseUp,upPoint);down=false;releasePoint=nil;Thread.sleep(forTimeInterval:0.3);try recordState()
}
if mode=="space"||mode=="enter"{try recordState()}
