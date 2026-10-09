// Official input restricted to the exact project-owned ResultSummaryRow verification window.
import AppKit
import ApplicationServices
import Foundation
let args=CommandLine.arguments
var releasePoint:CGPoint?
func fail(_ m:String)->Never{if let p=releasePoint{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};fputs(m+"\n",stderr);exit(1)}
guard args.count==2,["inspect","title-click","icon-click","count-click","chevron-click","hold","cancel","hover","leave"].contains(args[1])else{fail("Fixed row mode required")}
let mode=args[1],apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.resultsummaryrowcheck")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/ResultSummaryRowCheck.app"else{fail("Unique owned app required")}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
let root=AXUIElementCreateApplication(app.processIdentifier),wins=attr(root,kAXWindowsAttribute)as?[AXUIElement] ?? [],owned=wins.filter{text($0,kAXTitleAttribute)=="Replica ResultSummaryRow verification"}
guard owned.count==1,let w=owned.first,let frame=rect(w)else{fail("Exact row window missing")}
var rows:[AXUIElement]=[],peers:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ d:Int){guard d<30 else{return};if text(e,kAXIdentifierAttribute)=="ResultRowTarget"{rows.append(e)};if text(e,kAXIdentifierAttribute)=="ResultRowPeer"{peers.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,d+1)}}
visit(w,0);guard rows.count==1,peers.count==1,let b=rect(rows[0]),let pr=rect(peers[0]),abs(b.width-858)<0.01,abs(b.height-82)<0.01,abs(pr.minY-b.minY-112)<0.01 else{fail("Owned row geometry mismatch")}
let p=mode=="leave" ? CGPoint(x:frame.maxX-20,y:frame.maxY-40):CGPoint(x:b.minX+(mode=="icon-click" ? 35:mode=="title-click" ? 100:mode=="chevron-click" ? 830:440),y:b.minY+41)
guard frame.contains(p)else{fail("Outside owned frame")}
print(String(data:try JSONSerialization.data(withJSONObject:["pid":app.processIdentifier,"title":text(w,kAXTitleAttribute),"mode":mode,"x":p.x,"y":p.y,"api":"CGEvent"],options:.sortedKeys),encoding:.utf8)!)
guard mode != "inspect"else{exit(0)}
app.activate(options:[]);AXUIElementPerformAction(w,kAXRaiseAction as CFString);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Wrong foreground")}}
var point=p
func send(_ type:CGEventType){guard let e=CGEvent(mouseEventSource:nil,mouseType:type,mouseCursorPosition:point,mouseButton:.left)else{fail("Event unavailable")};e.post(tap:.cghidEventTap)}
foreground();let previous=CGEvent(source:nil)?.location;var down=false
defer{if down{send(.leftMouseUp)};if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let old=previous{CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:old,mouseButton:.left)?.post(tap:.cghidEventTap)}}
send(.mouseMoved);Thread.sleep(forTimeInterval:0.2);foreground();var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(p.x),Float(p.y),&hit) == .success,let target=hit else{fail("No point hit")};var pid:pid_t=0;AXUIElementGetPid(target,&pid);guard pid==app.processIdentifier else{fail("Another app hit")}
let folder="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/resultrow-mac-input/",input=folder+"resultrow-input.json"
if mode != "hover"&&mode != "leave"{send(.leftMouseDown);down=true;releasePoint=p;Thread.sleep(forTimeInterval:mode=="hold"||mode=="cancel" ? 0.2:0.08);if mode=="hold"||mode=="cancel"{try Data(contentsOf:URL(fileURLWithPath:input)).write(to:URL(fileURLWithPath:folder+mode+"-inside.json"))};if mode=="cancel"{point=CGPoint(x:frame.maxX-20,y:frame.maxY-40);guard frame.contains(point)else{fail("Cancel outside frame")};send(.mouseMoved);releasePoint=point;Thread.sleep(forTimeInterval:0.2);try Data(contentsOf:URL(fileURLWithPath:input)).write(to:URL(fileURLWithPath:folder+"cancel-outside.json"))};send(.leftMouseUp);down=false;releasePoint=nil}
foreground();Thread.sleep(forTimeInterval:0.34);try Data(contentsOf:URL(fileURLWithPath:input)).write(to:URL(fileURLWithPath:folder+"actual-"+mode+".json"))
