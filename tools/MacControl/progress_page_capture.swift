import AppKit
import CoreGraphics
import ScreenCaptureKit
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
let a=CommandLine.arguments;guard a.count==4,let pid=Int32(a[1])else{fail("Owned PID/binary/output required")}
let root="/Users/youptebook/workspace/DB-Migration-Verifier/",binary=a[2],output=a[3]
guard binary.hasPrefix(root+"artifacts/replica/workers/root/progresspage/"),binary.hasSuffix("/ReplicaProbe.dll"),output.hasPrefix(root)else{fail("Owned paths required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:root+"docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any];guard ((lease["resources"]as![String:Any])["macos"]as![String:Any])["owner"]as?String=="root"else{fail("Root Mac lease required")}
let process=Process();process.executableURL=URL(fileURLWithPath:"/bin/ps");process.arguments=["-p",String(pid),"-o","command="];let pipe=Pipe();process.standardOutput=pipe;try process.run();process.waitUntilExit();let command=String(data:pipe.fileHandleForReading.readDataToEndOfFile(),encoding:.utf8) ?? "";guard command.contains(binary+" --progresspage-window")else{fail("Exact owned command required")}
let rows=CGWindowListCopyWindowInfo(.optionOnScreenOnly,kCGNullWindowID) as?[[String:Any]] ?? [];let wins=rows.filter{($0[kCGWindowOwnerPID as String] as?Int)==Int(pid)&&($0[kCGWindowName as String] as?String)=="Replica ProgressPage verification"};guard wins.count==1,let id=wins[0][kCGWindowNumber as String] as?UInt32 else{fail("Exact verification window missing")}
guard CGPreflightScreenCaptureAccess()else{fail("Existing scoped capture permission missing")}
var inventory:SCShareableContent?,ready=false
SCShareableContent.getExcludingDesktopWindows(true,onScreenWindowsOnly:true){v,e in inventory=e==nil ? v:nil;ready=true}
let end=Date(timeIntervalSinceNow:5);while !ready&&Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.02))}
guard let target=inventory?.windows.first(where:{$0.windowID==id})else{fail("Owned capture window missing")}
let config=SCStreamConfiguration();config.width=Int(target.frame.width*2);config.height=Int(target.frame.height*2);config.showsCursor=false;config.ignoreShadowsSingleWindow=true;config.colorSpaceName=CGColorSpace.sRGB
var image:CGImage?,finished=false
SCScreenshotManager.captureImage(contentFilter:SCContentFilter(desktopIndependentWindow:target),configuration:config){v,e in image=e==nil ? v:nil;finished=true}
let limit=Date(timeIntervalSinceNow:5);while !finished&&Date()<limit{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.02))}
guard let image,let png=NSBitmapImageRep(cgImage:image).representation(using:.png,properties:[:])else{fail("Scoped image missing")};try png.write(to:URL(fileURLWithPath:output));print("Captured exact project verification window")
