// Official input/capture restricted to the project's original scroll fixture.
import AppKit
import ApplicationServices
import ScreenCaptureKit
import Foundation
import CoreMedia
import CoreVideo
import CoreImage
let args = CommandLine.arguments
var releasePoint:CGPoint?
func fail(_ message: String) -> Never { if let p=releasePoint{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};fputs(message + "\n", stderr); exit(1) }
guard (args.count == 2 || (args.count == 3 && ["hover","reenter","wheelcancel","reenter-expanded","hover-short","dragdetach","trackclick"].contains(args[2]))), ["source","candidate"].contains(args[1]) else { fail("Fixed source scroll mode required") }
let mode = args[1]
let scenario=args.count==3 ? args[2]:"hide"
let apps = NSRunningApplication.runningApplications(withBundleIdentifier: mode=="source" ? "com.google.Chrome":"local.dbmv.replica.scrollviewportcheck")
guard apps.count == 1, let app = apps.first, app.bundleURL?.path == (mode=="source" ? "/Applications/Google Chrome.app":"/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/ScrollViewportCheck.app") else { fail("Owned Chrome missing") }
func attr(_ e: AXUIElement, _ n: String) -> CFTypeRef? { var v: CFTypeRef?; return AXUIElementCopyAttributeValue(e, n as CFString, &v) == .success ? v : nil }
func text(_ e: AXUIElement, _ n: String) -> String { attr(e,n) as? String ?? "" }
func rect(_ e: AXUIElement) -> CGRect? {
    guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute), CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID() else { return nil }
    var p=CGPoint.zero,s=CGSize.zero
    guard AXValueGetValue(a as! AXValue,.cgPoint,&p),AXValueGetValue(b as! AXValue,.cgSize,&s) else { return nil }
    return CGRect(origin:p,size:s)
}
let root=AXUIElementCreateApplication(app.processIdentifier)
var wins=(attr(root,kAXWindowsAttribute) as? [AXUIElement] ?? []).filter { mode=="source" ? (text($0,kAXTitleAttribute).hasPrefix("ScrollViewport · original isolation") && text($0,kAXDocumentAttribute)=="http://10.211.55.2:8768/scrollviewport.html") : text($0,kAXTitleAttribute)=="Replica ScrollViewport verification" }
let readyDeadline=Date(timeIntervalSinceNow:5)
while wins.isEmpty && Date()<readyDeadline{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.05));wins=(attr(root,kAXWindowsAttribute) as? [AXUIElement] ?? []).filter { mode=="source" ? (text($0,kAXTitleAttribute).hasPrefix("ScrollViewport · original isolation") && text($0,kAXDocumentAttribute)=="http://10.211.55.2:8768/scrollviewport.html") : text($0,kAXTitleAttribute)=="Replica ScrollViewport verification" }}
guard wins.count==1,let window=wins.first,let windowRect=rect(window) else { fail("Unique owned source window/URL required") }
var panes:[AXUIElement]=[],records:[AXUIElement]=[],resets:[AXUIElement]=[],detachButtons:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int) {
    guard depth<30 else { return }
    if (mode=="source" && (text(e,kAXDescriptionAttribute)=="Original scroll fixture" || text(e,kAXTitleAttribute)=="Original scroll fixture")) || (mode=="candidate" && text(e,kAXIdentifierAttribute)=="ScrollCandidate") { panes.append(e) }
    if (text(e,kAXRoleAttribute)=="AXButton" && (text(e,kAXTitleAttribute)=="Reset to top" || text(e,kAXDescriptionAttribute)=="Reset to top")) || text(e,kAXIdentifierAttribute)=="ScrollReset" { resets.append(e) }
    if text(e,kAXRoleAttribute)=="AXButton" && (text(e,kAXTitleAttribute)=="Record scroll state" || text(e,kAXDescriptionAttribute)=="Record scroll state") { records.append(e) }
    if text(e,kAXIdentifierAttribute)=="ScrollDetach" || (text(e,kAXRoleAttribute)=="AXButton" && (text(e,kAXTitleAttribute)=="Detach viewport" || text(e,kAXDescriptionAttribute)=="Detach viewport")){detachButtons.append(e)}
    for c in attr(e,kAXChildrenAttribute) as? [AXUIElement] ?? [] { visit(c,depth+1) }
}
visit(window,0)
guard panes.count==1,let pane=rect(panes[0]),abs(pane.width-220)<0.01,abs(pane.height-400)<0.01,windowRect.contains(pane) else { fail("Exact owned pane geometry required") }
// The continuous official stream is initialized BEFORE the input. It avoids
// counting screenshot initialization latency as the scrollbar's fade duration.
guard CGPreflightScreenCaptureAccess() else{fail("Existing capture permission unavailable")}
app.activate(options:[]);AXUIElementPerformAction(window,kAXRaiseAction as CFString)
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Owned stream lost foreground")}}
foreground()
if mode=="candidate"{guard resets.count==1,AXUIElementPerformAction(resets[0],kAXPressAction as CFString) == .success else{fail("Owned reset missing")};Thread.sleep(forTimeInterval:0.1)}
let previous=CGEvent(source:nil)?.location
var destination=CGPoint(x:pane.midX,y:pane.minY+100)
defer{if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let previous{CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:previous,mouseButton:.left)?.post(tap:.cghidEventTap)}}
guard CGWarpMouseCursorPosition(destination) == .success else{fail("Owned pointer placement failed")}
CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap)
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.15));foreground()
func pointerGuard(){guard let point=CGEvent(source:nil)?.location,hypot(point.x-destination.x,point.y-destination.y)<1 else{fail("Owned stream pointer moved")}}
pointerGuard()
var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(destination.x),Float(destination.y),&hit) == .success,let hit else{fail("Owned point missing")};var hitPid:pid_t=0;AXUIElementGetPid(hit,&hitPid);guard hitPid==app.processIdentifier else{fail("Other application at owned point: hitPID=\(hitPid) expectedPID=\(app.processIdentifier) pane=\(pane) point=\(destination) window=\(windowRect)")}
var content:SCShareableContent?,inventoryError:Error?,ready=false
SCShareableContent.getExcludingDesktopWindows(true,onScreenWindowsOnly:true){content=$0;inventoryError=$1;ready=true}
let inventoryDeadline=Date(timeIntervalSinceNow:5)
while !ready && Date()<inventoryDeadline{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.01))}
guard ready,inventoryError==nil,let content else{fail("Owned inventory failed")}
let matching=content.windows.filter{$0.owningApplication?.processID==app.processIdentifier && (mode=="source" ? ($0.title ?? "").hasPrefix("ScrollViewport · original isolation") : ($0.title ?? "")=="Replica ScrollViewport verification") && abs($0.frame.minX-windowRect.minX)<1 && abs($0.frame.minY-windowRect.minY)<1}
guard matching.count==1 else{fail("Owned capture window not unique")}
let config=SCStreamConfiguration();config.width=Int(windowRect.width*2);config.height=Int(windowRect.height*2);config.showsCursor=false;config.ignoreShadowsSingleWindow=true;config.colorSpaceName=CGColorSpace.sRGB;config.pixelFormat=kCVPixelFormatType_32BGRA;config.minimumFrameInterval=CMTime(value:1,timescale:60);config.queueDepth=5;config.capturesAudio=false
final class Collector:NSObject,SCStreamOutput{
 var inputTime:Double?;var rows:[[String:Any]]=[];var images:[Double:CGImage]=[:]
 let crop:CGRect;let x:Int;let y:Int;let lock=NSLock();let context=CIContext();var problem:String?
 init(crop:CGRect){self.crop=crop;x=Int(crop.minX+214*2);y=Int(crop.minY+100*2)}
 func stream(_ stream:SCStream,didOutputSampleBuffer sampleBuffer:CMSampleBuffer,of type:SCStreamOutputType){
  guard type == .screen,let buffer=CMSampleBufferGetImageBuffer(sampleBuffer)else{return}
  lock.lock();defer{lock.unlock()}
  guard let started=inputTime else{return}
  let time=(CMTimeGetSeconds(CMSampleBufferGetPresentationTimeStamp(sampleBuffer))-started)*1000
  guard time>=0,time<=2100 else{return}
  guard CVPixelBufferGetWidth(buffer)>x,CVPixelBufferGetHeight(buffer)>y else{problem="Stream dimensions do not contain guarded pane";return}
  CVPixelBufferLockBaseAddress(buffer,.readOnly);defer{CVPixelBufferUnlockBaseAddress(buffer,.readOnly)}
  guard let base=CVPixelBufferGetBaseAddress(buffer)else{return}
  let pixel=base.advanced(by:y*CVPixelBufferGetBytesPerRow(buffer)+x*4).assumingMemoryBound(to:UInt8.self)
  let band=(200..<220).map{logicalX -> [Int] in let px=base.advanced(by:y*CVPixelBufferGetBytesPerRow(buffer)+Int(crop.minX+Double(logicalX)*2)*4).assumingMemoryBound(to:UInt8.self);return [Int(px[2]),Int(px[1]),Int(px[0])]}
  rows.append(["ms":time,"rgb":[Int(pixel[2]),Int(pixel[1]),Int(pixel[0])],"rightBand":band,"width":CVPixelBufferGetWidth(buffer),"height":CVPixelBufferGetHeight(buffer)])
  for requested in [100.0,250,550,650,850,1300,1700] where time>=requested && images[requested]==nil{
   if let full=context.createCGImage(CIImage(cvPixelBuffer:buffer),from:CGRect(x:0,y:0,width:CVPixelBufferGetWidth(buffer),height:CVPixelBufferGetHeight(buffer))),let pane=full.cropping(to:crop){images[requested]=pane}
  }
 }
}
let collector=Collector(crop:CGRect(x:(pane.minX-windowRect.minX)*2,y:(pane.minY-windowRect.minY)*2,width:pane.width*2,height:pane.height*2))
let stream=SCStream(filter:SCContentFilter(desktopIndependentWindow:matching[0]),configuration:config,delegate:nil)
try stream.addStreamOutput(collector,type:.screen,sampleHandlerQueue:DispatchQueue(label:"dbmv.owned.scroll.capture"))
var started=false,startError:Error?
stream.startCapture{startError=$0;started=true}
let startDeadline=Date(timeIntervalSinceNow:5)
while !started && Date()<startDeadline{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.01))}
guard started,startError==nil else{fail("Owned stream failed to start")}
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.15));foreground();pointerGuard()
collector.lock.lock();collector.inputTime=CMTimeGetSeconds(CMClockGetTime(CMClockGetHostTimeClock()));collector.lock.unlock()
guard let wheel=CGEvent(scrollWheelEvent2Source:nil,units:.pixel,wheelCount:1,wheel1:-100,wheel2:0,wheel3:0)else{fail("Owned wheel creation failed")};wheel.location=destination;wheel.post(tap:.cghidEventTap)
let inputClock=collector.inputTime!
let deadline=Date(timeIntervalSinceNow:scenario=="hide" ? 1.1:1.95)
var changed=false,exited=false,reentered=false,held=false
defer{if held{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap)}}
while Date()<deadline{
 RunLoop.current.run(until:Date(timeIntervalSinceNow:0.01));foreground();pointerGuard()
 let elapsed=(CMTimeGetSeconds(CMClockGetTime(CMClockGetHostTimeClock()))-inputClock)*1000
 if scenario != "hide" && !changed && elapsed >= (["hover","hover-short","reenter-expanded","dragdetach","trackclick"].contains(scenario) ? 350:650){
  changed=true
  if scenario=="trackclick"{
   destination=CGPoint(x:pane.maxX-5,y:pane.minY+300);guard CGWarpMouseCursorPosition(destination) == .success else{fail("Owned track placement failed")};CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap)
   RunLoop.current.run(until:Date(timeIntervalSinceNow:0.3));foreground();pointerGuard()
   CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);releasePoint=destination
   RunLoop.current.run(until:Date(timeIntervalSinceNow:0.08));foreground();pointerGuard()
   CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);releasePoint=nil
  }
  else if scenario=="dragdetach"{
   destination=CGPoint(x:pane.maxX-5,y:pane.minY+100);guard CGWarpMouseCursorPosition(destination) == .success else{fail("Owned held placement failed")};CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap)
   RunLoop.current.run(until:Date(timeIntervalSinceNow:0.1));foreground();pointerGuard()
   CGEvent(mouseEventSource:nil,mouseType:.leftMouseDown,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);held=true;releasePoint=destination
   RunLoop.current.run(until:Date(timeIntervalSinceNow:0.1));foreground();pointerGuard()
   guard detachButtons.count==1,AXUIElementPerformAction(detachButtons[0],kAXPressAction as CFString) == .success else{fail("Owned held detach action failed")}
  }
  else if scenario=="wheelcancel"{guard let again=CGEvent(scrollWheelEvent2Source:nil,units:.pixel,wheelCount:1,wheel1:-100,wheel2:0,wheel3:0)else{fail("Owned repeat wheel missing")};again.location=destination;again.post(tap:.cghidEventTap)}
  else{destination=CGPoint(x:pane.maxX-5,y:pane.minY+100);guard CGWarpMouseCursorPosition(destination) == .success else{fail("Owned edge placement failed")};CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap)}
 }
 if scenario != "hide" && scenario != "wheelcancel" && scenario != "dragdetach" && scenario != "trackclick" && !exited && elapsed >= (scenario=="hover-short" ? 450:scenario=="reenter-expanded" ? 900:1100){
  exited=true;destination=CGPoint(x:pane.midX,y:pane.minY+100);guard CGWarpMouseCursorPosition(destination) == .success else{fail("Owned body placement failed")};CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap)
 }
 if scenario=="reenter-expanded" && !reentered && elapsed>=1550{
  reentered=true;destination=CGPoint(x:pane.maxX-5,y:pane.minY+100);guard CGWarpMouseCursorPosition(destination) == .success else{fail("Owned reentry placement failed")};CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap)
 }
}
if held{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:destination,mouseButton:.left)?.post(tap:.cghidEventTap);held=false;releasePoint=nil}
var stopped=false;stream.stopCapture{_ in stopped=true}
let stopDeadline=Date(timeIntervalSinceNow:3)
while !stopped && Date()<stopDeadline{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.01))}
collector.lock.lock();defer{collector.lock.unlock()}
guard collector.problem==nil,collector.rows.count>=3,collector.rows.contains(where:{($0["ms"] as? Double ?? 0)>750})else{fail("Insufficient fresh owned frames")}
let folder="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/"
for (time,image) in collector.images{guard let png=NSBitmapImageRep(cgImage:image).representation(using:.png,properties:[:])else{fail("Owned pane PNG failed")};try png.write(to:URL(fileURLWithPath:folder+"screenshots/scrollviewport-mac-"+mode+"-"+scenario+"-stream-"+String(Int(time))+".png"))}
try JSONSerialization.data(withJSONObject:["mode":mode,"scenario":scenario,"api":"ScreenCaptureKit SCStream; only unique owned fixture window","clock":"CMClock host time / CMSampleBuffer presentation timestamp","sampleLogicalPoint":[214,100],"rows":collector.rows],options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:folder+"source/scrollviewport-mac-"+mode+"-"+scenario+"-stream.json"))
print("Fresh owned stream frames:",collector.rows.count)
