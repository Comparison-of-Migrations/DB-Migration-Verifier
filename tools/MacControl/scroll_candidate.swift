// Official CGEvent input, limited to the project-owned scroll verification window.
import AppKit
import ApplicationServices
import Foundation
import ScreenCaptureKit
let args=CommandLine.arguments
var releasePoint:CGPoint?
func fail(_ message:String)->Never{if let p=releasePoint{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};fputs(message+"\n",stderr);exit(1)}
guard args.count==2,["close","inspect","wheel-down","wheel-up","body-click","arrow-down-key","page-down-key","tall-page-key","home-key","end-key","drag-bottom","space-key","shift-space-key","tab-back","hide600","hide850","detach-settled","child-tab","short-key"].contains(args[1])else{fail("Fixed row operation required")}
let mode=args[1],apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.scrollviewportcheck")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/ScrollViewportCheck.app"else{fail("Unique running owned application required")}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
let root=AXUIElementCreateApplication(app.processIdentifier)
var owned:[AXUIElement]=[]
let readyDeadline=Date(timeIntervalSinceNow:5)
repeat{owned=(attr(root,kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute)=="Replica ScrollViewport verification"};if owned.count==1{break};RunLoop.current.run(until:Date(timeIntervalSinceNow:0.05))}while Date()<readyDeadline
guard owned.count==1,let w=owned.first,let frame=rect(w)else{fail("Exact row window missing")}
var panes:[AXUIElement]=[],closers:[AXUIElement]=[],resetButtons:[AXUIElement]=[],tallButtons:[AXUIElement]=[],bottomButtons:[AXUIElement]=[],detachButtons:[AXUIElement]=[],childButtons:[AXUIElement]=[],shortButtons:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int){if text(e,kAXIdentifierAttribute)=="ScrollFocusChild"{childButtons.append(e)};if text(e,kAXIdentifierAttribute)=="ScrollShort"{shortButtons.append(e)};guard depth<30 else{return};if text(e,kAXIdentifierAttribute)=="ScrollBottom"{bottomButtons.append(e)};if text(e,kAXIdentifierAttribute)=="ScrollDetach"{detachButtons.append(e)};if text(e,kAXIdentifierAttribute)=="ScrollReset"{resetButtons.append(e)};if text(e,kAXIdentifierAttribute)=="ScrollTall"{tallButtons.append(e)};if text(e,kAXIdentifierAttribute)=="ScrollClose"{closers.append(e)};if text(e,kAXIdentifierAttribute)=="ScrollCandidate"{panes.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,depth+1)}}
visit(w,0);if mode=="close"{guard closers.count==1,AXUIElementPerformAction(closers[0],kAXPressAction as CFString) == .success else{fail("Owned close failed")};exit(0)};if mode=="tall-page-key"{guard tallButtons.count==1,AXUIElementPerformAction(tallButtons[0],kAXPressAction as CFString) == .success else{fail("Owned tall control missing")};Thread.sleep(forTimeInterval:0.15)};guard panes.count==1,let pane=rect(panes[0]),abs(pane.width-220)<0.01,abs(pane.height-(mode=="tall-page-key" ? 660:400))<0.01,frame.contains(pane)else{fail("Owned scroll geometry mismatch")}
print(String(data:try JSONSerialization.data(withJSONObject:["pid":app.processIdentifier,"mode":mode,"pane":["x":pane.minX,"y":pane.minY,"width":pane.width,"height":pane.height]],options:.sortedKeys),encoding:.utf8)!)
guard mode != "inspect"else{exit(0)}
app.activate(options:[]);AXUIElementPerformAction(w,kAXRaiseAction as CFString);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Wrong foreground")}}
if mode=="detach-settled"{
 foreground()
 let folder="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/scrollviewport-mac-input/"
 let live=URL(fileURLWithPath:folder+"scrollviewport-input.json")
 let started=Date()
 guard bottomButtons.count==1,detachButtons.count==1,AXUIElementPerformAction(bottomButtons[0],kAXPressAction as CFString) == .success else{fail("Owned bottom control missing")}
 Thread.sleep(forTimeInterval:0.4);foreground()
 let before=try Data(contentsOf:live)
 guard let value=try JSONSerialization.jsonObject(with:before) as? [String:Any],value["offset"] as? Double == 800 else{fail("Owned settled baseline is not bottom")}
 guard AXUIElementPerformAction(detachButtons[0],kAXPressAction as CFString) == .success else{fail("Owned detach action failed")}
 Thread.sleep(forTimeInterval:0.6);foreground()
 guard let modified=try live.resourceValues(forKeys:[.contentModificationDateKey]).contentModificationDate,modified>started else{fail("No fresh owned detach record")}
 let after=try Data(contentsOf:live)
 guard let value=try JSONSerialization.jsonObject(with:after) as? [String:Any],value["offset"] as? Double == 0,rect(panes[0]) != nil else{fail("Owned reattached viewport did not reset")}
 try before.write(to:URL(fileURLWithPath:folder+"actual-detach-settled-before.json"))
 try after.write(to:URL(fileURLWithPath:folder+"actual-detach-settled.json"))
 print("Fresh owned detach: 800 -> 0, reattached");exit(0)
}
let previous=CGEvent(source:nil)?.location
var p=CGPoint(x:pane.midX,y:pane.minY+100),down=false
defer{if down{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let old=previous{CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:old,mouseButton:.left)?.post(tap:.cghidEventTap)}}
foreground();if mode=="child-tab"||mode=="short-key"{let buttons=mode=="child-tab" ? childButtons:shortButtons;guard buttons.count==1,AXUIElementPerformAction(buttons[0],kAXPressAction as CFString) == .success else{fail("Owned focus setup failed")};RunLoop.current.run(until:Date(timeIntervalSinceNow:0.15));foreground()};if mode=="drag-bottom"{guard resetButtons.count==1,AXUIElementPerformAction(resetButtons[0],kAXPressAction as CFString) == .success else{fail("Owned reset missing")};Thread.sleep(forTimeInterval:0.15)};CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.15)
var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(p.x),Float(p.y),&hit) == .success,let hit else{fail("No point hit")};var pid:pid_t=0;AXUIElementGetPid(hit,&pid);guard pid==app.processIdentifier else{fail("Other application hit")}
let wheelStarted=ProcessInfo.processInfo.systemUptime
if mode=="wheel-down"||mode=="wheel-up"||mode.hasPrefix("hide"){guard let wheel=CGEvent(scrollWheelEvent2Source:nil,units:.pixel,wheelCount:1,wheel1:mode=="wheel-up" ? 100:-100,wheel2:0,wheel3:0) else{fail("Owned wheel creation failed")};wheel.location=p;wheel.post(tap:.cghidEventTap)}
else if mode=="body-click"{CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);down=true;Thread.sleep(forTimeInterval:0.08);CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);down=false}
else if mode=="drag-bottom"{
 CGEvent(scrollWheelEvent2Source:nil,units:.pixel,wheelCount:1,wheel1:-100,wheel2:0,wheel3:0)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.1);foreground()
 p=CGPoint(x:pane.maxX-5,y:pane.minY+100);CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.3);foreground()
 CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);down=true;releasePoint=p;Thread.sleep(forTimeInterval:0.08);foreground()
 p=CGPoint(x:pane.maxX-5,y:pane.maxY-22);guard frame.contains(p)else{fail("Owned drag destination outside window")};CGEvent(mouseEventSource:nil,mouseType:.leftMouseDragged,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);releasePoint=p;Thread.sleep(forTimeInterval:0.08);foreground()
 CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);down=false;releasePoint=nil
}
else if mode.hasSuffix("-key")||(mode=="tab-back"||mode=="child-tab"){foreground();if mode=="tab-back"||mode=="child-tab"{guard resetButtons.count==1,AXUIElementSetAttributeValue(resetButtons[0],kAXFocusedAttribute as CFString,kCFBooleanTrue) == .success else{fail("Owned Tab anchor focus failed")}}else{print("Before key mouse point:",String(describing:CGEvent(source:nil)?.location),"expected:",p);CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);down=true;Thread.sleep(forTimeInterval:0.08);CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap);down=false;Thread.sleep(forTimeInterval:0.1)};let keys:[String:CGKeyCode]=["arrow-down-key":125,"page-down-key":121,"tall-page-key":121,"home-key":115,"end-key":119,"space-key":49,"shift-space-key":49,"tab-back":48,"child-tab":48,"short-key":49];guard let code=keys[mode]else{fail("Fixed key missing")};let keyDown=CGEvent(keyboardEventSource:nil,virtualKey:code,keyDown:true),keyUp=CGEvent(keyboardEventSource:nil,virtualKey:code,keyDown:false);if mode=="shift-space-key"||mode=="tab-back"||mode=="child-tab"{keyDown?.flags = .maskShift;keyUp?.flags = .maskShift};keyDown?.post(tap:.cghidEventTap);defer{keyUp?.post(tap:.cghidEventTap)};Thread.sleep(forTimeInterval:0.08)}
if mode=="wheel-down"||mode=="wheel-up"||mode=="drag-bottom"||mode.hasPrefix("hide") {
 guard CGPreflightScreenCaptureAccess() else{fail("Existing owned capture permission unavailable")}
 Thread.sleep(forTimeInterval:mode=="hide600" ? 0.6:mode=="hide850" ? 0.85:0.1);foreground()
 var content:SCShareableContent?,error:Error?,ready=false
SCShareableContent.getExcludingDesktopWindows(true,onScreenWindowsOnly:true) { content=$0;error=$1;ready=true }
let deadline=Date(timeIntervalSinceNow:5)
while !ready && Date()<deadline { RunLoop.current.run(until:Date(timeIntervalSinceNow:0.01)) }
guard ready,error==nil,let content else { fail("Owned capture inventory failed") }
let owned=content.windows.filter { $0.owningApplication?.processID==app.processIdentifier && ($0.title ?? "")=="Replica ScrollViewport verification" && abs($0.frame.minX-frame.minX)<1 && abs($0.frame.minY-frame.minY)<1 }
guard owned.count==1 else { fail("Owned capture window not unique") }
let config=SCStreamConfiguration();config.width=Int(frame.width*2);config.height=Int(frame.height*2);config.showsCursor=false;config.ignoreShadowsSingleWindow=true;config.colorSpaceName=CGColorSpace.sRGB
let captureDelay=(ProcessInfo.processInfo.systemUptime-wheelStarted)*1000
var image:CGImage?,captureError:Error?,captured=false
SCScreenshotManager.captureImage(contentFilter:SCContentFilter(desktopIndependentWindow:owned[0]),configuration:config) { image=$0;captureError=$1;captured=true }
let imageDeadline=Date(timeIntervalSinceNow:5)
while !captured && Date()<imageDeadline { RunLoop.current.run(until:Date(timeIntervalSinceNow:0.01)) }
guard captured,captureError==nil,let image,let cropped=image.cropping(to:CGRect(x:(pane.minX-frame.minX)*2,y:(pane.minY-frame.minY)*2,width:pane.width*2,height:pane.height*2)) else { fail("Owned pane capture failed") }
foreground();guard let point=CGEvent(source:nil)?.location else{fail("Owned pointer unavailable")};print("Capture mouse point:",point,"expected:",p);guard hypot(point.x-p.x,point.y-p.y)<1 else { fail("Owned pointer moved during capture") }
let folder="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/"
guard let png=NSBitmapImageRep(cgImage:cropped).representation(using:.png,properties:[:]) else { fail("Owned PNG failed") }
try png.write(to:URL(fileURLWithPath:folder+"screenshots/scrollviewport-mac-candidate-"+mode+".png"))
try JSONSerialization.data(withJSONObject:["mode":mode,"captureRequestedMsAfterWheel":captureDelay],options:.sortedKeys).write(to:URL(fileURLWithPath:folder+"source/scrollviewport-mac-capture-timing-"+mode+".json"))

}
Thread.sleep(forTimeInterval:1);foreground()
let folder="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/scrollviewport-mac-input/"
try Data(contentsOf:URL(fileURLWithPath:folder+"scrollviewport-input.json")).write(to:URL(fileURLWithPath:folder+"actual-"+mode+".json"))
