import AppKit
import CoreGraphics
import ScreenCaptureKit
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
let a=CommandLine.arguments;guard a.count==3 else{fail("Owned bundle/output required")}
let bundle=URL(fileURLWithPath:a[1]);let apps=NSWorkspace.shared.runningApplications.filter{$0.bundleURL?.standardizedFileURL==bundle.standardizedFileURL};guard apps.count==1,let app=apps.first else{fail("Exact app missing")}
let rows=CGWindowListCopyWindowInfo(.optionOnScreenOnly,kCGNullWindowID) as?[[String:Any]] ?? [];let wins=rows.filter{($0[kCGWindowOwnerPID as String] as?Int)==Int(app.processIdentifier)&&($0[kCGWindowName as String] as?String)=="Replica SelectionPage verification"};guard wins.count==1,let id=wins[0][kCGWindowNumber as String] as?UInt32 else{fail("Exact verification window missing")}
guard CGPreflightScreenCaptureAccess()else{fail("Existing scoped capture permission missing")}
var inventory:SCShareableContent?,ready=false
SCShareableContent.getExcludingDesktopWindows(true,onScreenWindowsOnly:true){v,e in inventory=e==nil ? v:nil;ready=true}
let end=Date(timeIntervalSinceNow:5);while !ready&&Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.02))}
guard let target=inventory?.windows.first(where:{$0.windowID==id})else{fail("Owned capture window missing")}
let config=SCStreamConfiguration();config.width=Int(target.frame.width*2);config.height=Int(target.frame.height*2);config.showsCursor=false;config.ignoreShadowsSingleWindow=true;config.colorSpaceName=CGColorSpace.sRGB
var image:CGImage?,finished=false
SCScreenshotManager.captureImage(contentFilter:SCContentFilter(desktopIndependentWindow:target),configuration:config){v,e in image=e==nil ? v:nil;finished=true}
let limit=Date(timeIntervalSinceNow:5);while !finished&&Date()<limit{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.02))}
guard let image,let png=NSBitmapImageRep(cgImage:image).representation(using:.png,properties:[:])else{fail("Scoped image missing")};try png.write(to:URL(fileURLWithPath:a[2]));print("Captured exact project verification window")
