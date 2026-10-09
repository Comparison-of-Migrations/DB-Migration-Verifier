// Official input/capture restricted to the project's original scroll fixture.
import AppKit
import ApplicationServices
import ScreenCaptureKit
import Foundation
let args = CommandLine.arguments
var releasePoint:CGPoint?
func fail(_ message: String) -> Never { if let p=releasePoint{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};fputs(message + "\n", stderr); exit(1) }
guard args.count == 2, ["inspect", "wheel", "wheel-hover", "hover-edge", "capture", "drag-bottom","hide600","hide850"].contains(args[1]) else { fail("Fixed source scroll mode required") }
let mode = args[1]
let apps = NSRunningApplication.runningApplications(withBundleIdentifier: "com.google.Chrome")
guard apps.count == 1, let app = apps.first, app.bundleURL?.path == "/Applications/Google Chrome.app" else { fail("Owned Chrome missing") }
func attr(_ e: AXUIElement, _ n: String) -> CFTypeRef? { var v: CFTypeRef?; return AXUIElementCopyAttributeValue(e, n as CFString, &v) == .success ? v : nil }
func text(_ e: AXUIElement, _ n: String) -> String { attr(e,n) as? String ?? "" }
func rect(_ e: AXUIElement) -> CGRect? {
    guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute), CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID() else { return nil }
    var p=CGPoint.zero,s=CGSize.zero
    guard AXValueGetValue(a as! AXValue,.cgPoint,&p),AXValueGetValue(b as! AXValue,.cgSize,&s) else { return nil }
    return CGRect(origin:p,size:s)
}
let root=AXUIElementCreateApplication(app.processIdentifier)
let wins=(attr(root,kAXWindowsAttribute) as? [AXUIElement] ?? []).filter { text($0,kAXTitleAttribute).hasPrefix("ScrollViewport · original isolation") && text($0,kAXDocumentAttribute)=="http://10.211.55.2:8768/scrollviewport.html" }
guard wins.count==1,let window=wins.first,let windowRect=rect(window) else { fail("Unique owned source window/URL required") }
var panes:[AXUIElement]=[],records:[AXUIElement]=[],resets:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int) {
    guard depth<30 else { return }
    if text(e,kAXDescriptionAttribute)=="Original scroll fixture" || text(e,kAXTitleAttribute)=="Original scroll fixture" { panes.append(e) }
    if text(e,kAXRoleAttribute)=="AXButton" && (text(e,kAXTitleAttribute)=="Reset to top" || text(e,kAXDescriptionAttribute)=="Reset to top") { resets.append(e) }
    if text(e,kAXRoleAttribute)=="AXButton" && (text(e,kAXTitleAttribute)=="Record scroll state" || text(e,kAXDescriptionAttribute)=="Record scroll state") { records.append(e) }
    for c in attr(e,kAXChildrenAttribute) as? [AXUIElement] ?? [] { visit(c,depth+1) }
}
visit(window,0)
guard panes.count==1,records.count==1,let pane=rect(panes[0]),abs(pane.width-220)<0.01,abs(pane.height-400)<0.01,windowRect.contains(pane) else { fail("Exact owned pane geometry required") }
var metadata:[String:Any]=["mode":mode,"pid":app.processIdentifier,"title":text(window,kAXTitleAttribute),"pane":["x":pane.minX,"y":pane.minY,"width":pane.width,"height":pane.height],"api":"CGEvent/ScreenCaptureKit"]
print(String(data:try JSONSerialization.data(withJSONObject:metadata,options:.sortedKeys),encoding:.utf8)!)
guard mode != "inspect" else { exit(0) }
guard CGPreflightScreenCaptureAccess() else { fail("Existing capture permission unavailable; no settings changed") }
app.activate(options:[]);AXUIElementPerformAction(window,kAXRaiseAction as CFString)
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))
func foreground() { guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else { fail("Owned source lost foreground") } }
foreground()

let previous=CGEvent(source:nil)?.location
var destination=CGPoint(x:mode=="hover-edge" ? pane.maxX-5 : pane.midX,y:pane.minY+100)
defer { if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let previous { CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:previous,mouseButton:.left)?.post(tap:.cghidEventTap) } }
func clickOwned(_ element:AXUIElement) {
 guard let button=rect(element),windowRect.contains(button),button.width>0,button.height>0 else{fail("Owned source button geometry invalid")}
 destination=CGPoint(x:button.midX,y:button.midY);CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.1));foreground()
 var h:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(destination.x),Float(destination.y),&h) == .success,let h else{fail("Owned source button hit unavailable")};var owner:pid_t=0;AXUIElementGetPid(h,&owner);guard owner==app.processIdentifier else{fail("Other app at owned source button")}
 CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);releasePoint=destination;RunLoop.current.run(until:Date(timeIntervalSinceNow:0.08));foreground()
 CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);releasePoint=nil;RunLoop.current.run(until:Date(timeIntervalSinceNow:0.15));foreground()
}
if mode=="drag-bottom"{
 let sourceURL=URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/windows-source-sidebar.json")
 guard let modified=try sourceURL.resourceValues(forKeys:[.contentModificationDateKey]).contentModificationDate,Date().timeIntervalSince(modified)<60,
 let reference=try JSONSerialization.jsonObject(with:Data(contentsOf:sourceURL)) as? [String:Any],reference["offset"] as? Double == 0,reference["height"] as? Double == 400,reference["width"] as? Double == 220 else{fail("Fresh semantic source reset/record required")}
 destination=CGPoint(x:pane.midX,y:pane.minY+100)
}
guard CGWarpMouseCursorPosition(destination) == .success else{fail("Owned pointer placement failed")}
CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap)
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.15))
metadata["pointerBeforeWheel"]=["x":CGEvent(source:nil)?.location.x ?? -1,"y":CGEvent(source:nil)?.location.y ?? -1]
var hit:AXUIElement?
guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(destination.x),Float(destination.y),&hit) == .success,let hit else { fail("Owned hit unavailable") }
var pid:pid_t=0;AXUIElementGetPid(hit,&pid);guard pid==app.processIdentifier else { fail("Other application hit") }
var content:SCShareableContent?,error:Error?,ready=false
SCShareableContent.getExcludingDesktopWindows(true,onScreenWindowsOnly:true) { content=$0;error=$1;ready=true }
let deadline=Date(timeIntervalSinceNow:5)
while !ready && Date()<deadline { RunLoop.current.run(until:Date(timeIntervalSinceNow:0.01)) }
guard ready,error==nil,let content else { fail("Owned capture inventory failed") }
let owned=content.windows.filter { $0.owningApplication?.processID==app.processIdentifier && ($0.title ?? "").hasPrefix("ScrollViewport · original isolation") && abs($0.frame.minX-windowRect.minX)<1 && abs($0.frame.minY-windowRect.minY)<1 }
guard owned.count==1 else { fail("Owned capture window not unique") }
let config=SCStreamConfiguration();config.width=Int(windowRect.width*2);config.height=Int(windowRect.height*2);config.showsCursor=false;config.ignoreShadowsSingleWindow=true;config.colorSpaceName=CGColorSpace.sRGB
let wheelStarted=ProcessInfo.processInfo.systemUptime
if mode=="wheel" || mode=="wheel-hover" || mode=="drag-bottom" || mode.hasPrefix("hide") { guard let event=CGEvent(scrollWheelEvent2Source:nil,units:.pixel,wheelCount:1,wheel1:-100,wheel2:0,wheel3:0) else { fail("Owned wheel event unavailable") };metadata["wheelEventLocationBefore"]=["x":event.location.x,"y":event.location.y];event.location=destination;event.post(tap:.cghidEventTap) }
RunLoop.current.run(until:Date(timeIntervalSinceNow:mode=="hide600" ? 0.6:mode=="hide850" ? 0.85:0.1));foreground()
if mode=="wheel-hover" { destination=CGPoint(x:pane.maxX-5,y:pane.minY+100);CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.3));foreground() }
if mode=="drag-bottom" {
 destination=CGPoint(x:pane.maxX-5,y:pane.minY+100);CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.3));foreground()
 CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);releasePoint=destination
 RunLoop.current.run(until:Date(timeIntervalSinceNow:0.08));foreground()
 destination=CGPoint(x:pane.maxX-5,y:pane.maxY-22);guard windowRect.contains(destination)else{fail("Owned drag destination outside window")}
 CGEvent(mouseEventSource:nil,mouseType:.leftMouseDragged,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);releasePoint=destination
 RunLoop.current.run(until:Date(timeIntervalSinceNow:0.08));foreground()
 CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);releasePoint=nil
 RunLoop.current.run(until:Date(timeIntervalSinceNow:0.1));foreground()
}
metadata["captureRequestedMsAfterWheel"]=(ProcessInfo.processInfo.systemUptime-wheelStarted)*1000
var image:CGImage?,captureError:Error?,captured=false
SCScreenshotManager.captureImage(contentFilter:SCContentFilter(desktopIndependentWindow:owned[0]),configuration:config) { image=$0;captureError=$1;captured=true }
let imageDeadline=Date(timeIntervalSinceNow:5)
while !captured && Date()<imageDeadline { RunLoop.current.run(until:Date(timeIntervalSinceNow:0.01)) }
guard captured,captureError==nil,let image,let cropped=image.cropping(to:CGRect(x:(pane.minX-windowRect.minX)*2,y:(pane.minY-windowRect.minY)*2,width:pane.width*2,height:pane.height*2)) else { fail("Owned pane capture failed") }
foreground();let capturePoint=CGEvent(source:nil)?.location;print("Capture pointer:",String(describing:capturePoint),"expected:",destination);guard let point=capturePoint,hypot(point.x-destination.x,point.y-destination.y)<1 else { fail("Owned pointer moved during capture") }
let folder="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/"
guard let png=NSBitmapImageRep(cgImage:cropped).representation(using:.png,properties:[:]) else { fail("Owned PNG failed") }
try png.write(to:URL(fileURLWithPath:folder+"screenshots/scrollviewport-mac-chrome-source-"+mode+".png"))
try JSONSerialization.data(withJSONObject:metadata,options:.sortedKeys).write(to:URL(fileURLWithPath:folder+"source/scrollviewport-mac-chrome-control-"+mode+".json"))
if mode=="drag-bottom"||mode.hasPrefix("hide"){print("Owned source capture complete; semantic record is required before comparison")}else{clickOwned(records[0])}
