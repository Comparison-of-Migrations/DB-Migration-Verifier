// Fixed diagnostic controls in the owned ConnectionPage verification app; no OS preference changes.
import AppKit
import ApplicationServices
import Foundation
func fail(_ message:String)->Never{fputs(message+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ name:String)->CFTypeRef?{var value:CFTypeRef?;return AXUIElementCopyAttributeValue(e,name as CFString,&value) == .success ? value:nil}
func text(_ e:AXUIElement,_ name:String)->String{attr(e,name)as?String ?? ""}
guard CommandLine.arguments.count>=2,CommandLine.arguments.count<=3,["language","reduced","mount","record"].contains(CommandLine.arguments[1])else{fail("Fixed owned diagnostic required")}
let action=CommandLine.arguments[1]
let label=CommandLine.arguments.count==3 ? CommandLine.arguments[2]:nil
if let label,!["language","lifecycle-normal","lifecycle-reduced"].contains(label){fail("Fixed evidence label required")}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.connectionpagecheck")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/ConnectionPageCheck.app"else{fail("Unique owned ConnectionPage app required")}
let windows=attr(AXUIElementCreateApplication(app.processIdentifier),kAXWindowsAttribute)as?[AXUIElement] ?? []
let owned=windows.filter{text($0,kAXTitleAttribute)=="Replica ConnectionPage verification"}
guard owned.count==1,let window=owned.first else{fail("Unique owned ConnectionPage window required")}
var targets:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int){guard depth<30 else{return};if text(e,kAXIdentifierAttribute)=="ConnectionPage-"+action{targets.append(e)};for child in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(child,depth+1)}}
visit(window,0)
guard targets.count==1,let target=targets.first,text(target,kAXRoleAttribute)==kAXButtonRole,attr(target,kAXEnabledAttribute)as?Bool==true else{fail("Unique enabled owned diagnostic button required")}
var pid:pid_t=0;AXUIElementGetPid(target,&pid);guard pid==app.processIdentifier else{fail("Owned diagnostic process mismatch")}
guard AXUIElementPerformAction(target,kAXPressAction as CFString) == .success else{fail("Owned diagnostic AXPress refused")}
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.3))
if let label{let folder=URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/connectionpage-mac-input");try Data(contentsOf:folder.appendingPathComponent("connectionpage-input.json")).write(to:folder.appendingPathComponent(label+".json"))}
print("Owned diagnostic delivered; inspect new state and lifecycle timeline")
