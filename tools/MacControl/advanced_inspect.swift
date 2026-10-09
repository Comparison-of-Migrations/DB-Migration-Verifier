// User-authorized project verification control, exact owned CategoryHeader fixture.
import AppKit
import ApplicationServices
import Foundation
let args=CommandLine.arguments
var releasePoint: CGPoint?
func fail(_ message:String)->Never{if let p=releasePoint{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};fputs(message+"\n",stderr);exit(1)}
guard args.count==2,["inspect","button-click","choice-click","button-hold"].contains(args[1]) else{fail("Expected fixed category operation")}
let mode=args[1],apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.advancedconnectioncheck")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/AdvancedConnectionCheck.app" else{fail("Owned application must be unique and already running")}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
let root=AXUIElementCreateApplication(app.processIdentifier),wins=attr(root,kAXWindowsAttribute)as?[AXUIElement] ?? [],owned=wins.filter{text($0,kAXTitleAttribute)=="Replica AdvancedConnection verification"}
guard owned.count==1,let w=owned.first,let frame=rect(w)else{fail("Exact owned window missing")}
var buttons:[AXUIElement]=[],checks:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int){guard depth<30 else{return};if text(e,kAXIdentifierAttribute)=="advanced"{buttons.append(e)};if text(e,kAXIdentifierAttribute)=="CategoryCheck"{checks.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,depth+1)}}
visit(w,0);guard buttons.count==1,let b=rect(buttons[0]),b.width>100,b.width<450,abs(b.height-44)<0.01 else{fail("Owned advanced geometry mismatch")}
let meta:[String:Any]=["pid":app.processIdentifier,"title":text(w,kAXTitleAttribute),"expanded":attr(buttons[0],"AXExpanded") as? NSNumber ?? -1,"role":text(buttons[0],kAXRoleAttribute),"identifier":text(buttons[0],kAXIdentifierAttribute)]
print(String(data:try JSONSerialization.data(withJSONObject:meta,options:.sortedKeys),encoding:.utf8)!)
