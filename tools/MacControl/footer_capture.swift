// Capture only the unique, previously authorized project footer fixture window.
import AppKit
import ApplicationServices
import ScreenCaptureKit
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as!AXValue,.cgPoint,&p),AXValueGetValue(b as!AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
let _=NSApplication.shared
let args=CommandLine.arguments;guard args.count==2,["source","candidate"].contains(args[1])else{fail("Fixed footer capture required")};let source=args[1]=="source"
let bundle=source ? "com.google.Chrome":"local.dbmv.replica.actionfootercheck"
let path=source ? "/Applications/Google Chrome.app":"/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/ActionFooterCheck.app"
let title=source ? "ActionFooter · original isolation":"Replica ActionFooter verification"
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:bundle);guard apps.count==1,let app=apps.first,app.bundleURL?.path==path else{fail("Unique owned footer app required")}
let wins=(attr(AXUIElementCreateApplication(app.processIdentifier),kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{(source ? text($0,kAXTitleAttribute).hasPrefix(title):text($0,kAXTitleAttribute)==title) && (!source || text($0,kAXDocumentAttribute)=="http://10.211.55.2:8768/actionfooter.html")};guard wins.count==1,let w=wins.first,let frame=rect(w)else{fail("Unique owned footer window required")}
var targets:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ d:Int){guard d<30 else{return};if source ? text(e,kAXRoleAttribute)=="AXWebArea"&&text(e,kAXTitleAttribute)==title : text(e,kAXIdentifierAttribute)=="FooterCandidate"{targets.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,d+1)}}
visit(w,0);guard targets.count==1,let target=rect(targets[0])else{fail("Unique owned footer geometry required")}
let footer=source ? CGRect(x:target.minX+30,y:target.minY+72,width:target.width-220,height:84):target
guard abs(footer.width-780)<0.01,abs(footer.height-84)<0.01 else{fail("Expected scoped footer780×84")}
guard CGPreflightScreenCaptureAccess()else{fail("Existing capture permission required")}
var inventory:SCShareableContent?,ready=false
SCShareableContent.getExcludingDesktopWindows(true,onScreenWindowsOnly:true){ value,error in inventory=error==nil ? value:nil;ready=true}
let deadline=Date(timeIntervalSinceNow:5);while !ready&&Date()<deadline{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.02))}
guard let inventory else{fail("Owned capture inventory failed")}
let matching=inventory.windows.filter{$0.owningApplication?.processID==app.processIdentifier && (source ? ($0.title ?? "").hasPrefix(title):$0.title==title)};guard matching.count==1 else{fail("Unique owned capture window required")}
let config=SCStreamConfiguration();config.width=Int(frame.width*2);config.height=Int(frame.height*2);config.showsCursor=false;config.ignoreShadowsSingleWindow=true;config.colorSpaceName=CGColorSpace.sRGB
var image:CGImage?,finished=false
SCScreenshotManager.captureImage(contentFilter:SCContentFilter(desktopIndependentWindow:matching[0]),configuration:config){ value,error in image=error==nil ? value:nil;finished=true}
let end=Date(timeIntervalSinceNow:5);while !finished&&Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.02))}
guard let image,let crop=image.cropping(to:CGRect(x:(footer.minX-frame.minX-12)*2,y:(footer.minY-frame.minY-20)*2,width:(footer.width+24)*2,height:(footer.height+32)*2)),let png=NSBitmapImageRep(cgImage:crop).representation(using:.png,properties:[:])else{fail("Owned footer image failed")}
let out="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/screenshots/actionfooter-mac-"+args[1]+"-english.png";try png.write(to:URL(fileURLWithPath:out));print(out)
