// Official CGEvent input, limited to the project-owned row verification window.
import AppKit
import ApplicationServices
import Foundation
let args=CommandLine.arguments
var releasePoint:CGPoint?
func fail(_ message:String)->Never{if let p=releasePoint{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};fputs(message+"\n",stderr);exit(1)}
guard args.count==2,["inspect","choice-click","target-click","change-click","choice-hold","choice-cancel","hover","leave"].contains(args[1])else{fail("Fixed row operation required")}
let mode=args[1],apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.tablemappingrowcheck")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/TableMappingRowCheck.app"else{fail("Unique running owned application required")}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
let root=AXUIElementCreateApplication(app.processIdentifier),wins=attr(root,kAXWindowsAttribute)as?[AXUIElement] ?? [],owned=wins.filter{text($0,kAXTitleAttribute)=="Replica TableMappingRow verification"}
guard owned.count==1,let w=owned.first,let frame=rect(w)else{fail("Exact row window missing")}
var checks:[AXUIElement]=[],buttons:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int){guard depth<30 else{return};if text(e,kAXIdentifierAttribute)=="TableRowCheck"{checks.append(e)};if text(e,kAXIdentifierAttribute)=="TableRowChange"{buttons.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,depth+1)}}
visit(w,0);guard checks.count==1,buttons.count==1,let c=rect(checks[0]),let b=rect(buttons[0]),abs(c.width-20)<0.01,abs(c.height-20)<0.01,abs(b.height-44)<0.01 else{fail("Owned row controls/geometry mismatch")}
let source=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/tablemappingrow-mac-source.json")))as![String:Any]
guard let row=(source["rows"]as![[String:Any]]).first(where:{($0["id"]as!String)=="ko-858-0-1"})else{fail("Measured fixture row missing")}
let cols=row["columns"]as![Double]
guard abs(b.minX-c.minX-cols.prefix(3).reduce(0,+))<0.01 else{fail("Shared columns mismatch")}
let p:CGPoint
if mode=="target-click"||mode=="hover"{p=CGPoint(x:c.minX+cols[0]+10,y:c.midY)}else if mode=="change-click"{p=CGPoint(x:b.midX,y:b.midY)}else if mode=="leave"{p=CGPoint(x:frame.maxX-20,y:frame.maxY-40)}else{p=CGPoint(x:c.maxX+40,y:c.midY)}
guard frame.contains(p)else{fail("Outside owned frame")}
print(String(data:try JSONSerialization.data(withJSONObject:["pid":app.processIdentifier,"title":text(w,kAXTitleAttribute),"mode":mode,"x":p.x,"y":p.y,"api":"CGEvent"],options:.sortedKeys),encoding:.utf8)!)
guard mode != "inspect"else{exit(0)}
app.activate(options:[]);AXUIElementPerformAction(w,kAXRaiseAction as CFString);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Wrong foreground")}}
var eventPoint=p
func send(_ type:CGEventType){guard let e=CGEvent(mouseEventSource:nil,mouseType:type,mouseCursorPosition:eventPoint,mouseButton:.left)else{fail("Event unavailable")};e.post(tap:.cghidEventTap)}
foreground();let previous=CGEvent(source:nil)?.location;var down=false
defer{if down{send(.leftMouseUp)};if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let old=previous{CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:old,mouseButton:.left)?.post(tap:.cghidEventTap)}}
send(.mouseMoved);Thread.sleep(forTimeInterval:0.2);foreground();var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(p.x),Float(p.y),&hit) == .success,let target=hit else{fail("No point hit")};var pid:pid_t=0;AXUIElementGetPid(target,&pid);guard pid==app.processIdentifier else{fail("Another app hit")}
let folder="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/tablemappingrow-mac-input/"
if mode != "hover"&&mode != "leave"{send(.leftMouseDown);down=true;releasePoint=p;Thread.sleep(forTimeInterval:mode=="choice-hold"||mode=="choice-cancel" ? 0.2:0.08);if mode=="choice-hold"{try Data(contentsOf:URL(fileURLWithPath:folder+"tablemappingrow-input.json")).write(to:URL(fileURLWithPath:folder+"actual-held.json"))};if mode=="choice-cancel"{try Data(contentsOf:URL(fileURLWithPath:folder+"tablemappingrow-input.json")).write(to:URL(fileURLWithPath:folder+"cancel-held-inside.json"));eventPoint=CGPoint(x:frame.maxX-20,y:frame.maxY-40);guard frame.contains(eventPoint)else{fail("Cancel point outside owned frame")};send(.mouseMoved);releasePoint=eventPoint;Thread.sleep(forTimeInterval:0.2);try Data(contentsOf:URL(fileURLWithPath:folder+"tablemappingrow-input.json")).write(to:URL(fileURLWithPath:folder+"cancel-held-outside.json"))};send(.leftMouseUp);down=false;releasePoint=nil}
foreground();Thread.sleep(forTimeInterval:0.34);try Data(contentsOf:URL(fileURLWithPath:folder+"tablemappingrow-input.json")).write(to:URL(fileURLWithPath:folder+"actual-"+mode+".json"))
