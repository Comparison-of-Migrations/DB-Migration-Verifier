// Readonly AX/close on exact frozen ProgressMeter verification process only.
import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func string(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
let a=CommandLine.arguments;guard a.count==4,let pid=Int32(a[1]),["inspect","close"].contains(a[2])else{fail("PID operation output required")}
let task=Process();task.executableURL=URL(fileURLWithPath:"/bin/ps");task.arguments=["-p",String(pid),"-o","command="];let pipe=Pipe();task.standardOutput=pipe;try task.run();task.waitUntilExit();let command=String(data:pipe.fileHandleForReading.readDataToEndOfFile(),encoding:.utf8) ?? "";guard command.contains("progressmeter/8f1dbca100d3408e859e1d5098cf68e7/build/bin/ReplicaProbe/release_osx-arm64/ReplicaProbe.dll --progressmeter-window")else{fail("Exact frozen native runtime required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any];guard ((lease["resources"]as![String:Any])["macos"]as![String:Any])["owner"]as?String=="root"else{fail("Root Mac lease required")}
let app=AXUIElementCreateApplication(pid);let windows=(attr(app,kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{string($0,kAXTitleAttribute)=="Replica ProgressMeter verification"};guard windows.count==1 else{fail("Exact unique meter window required")};let w=windows[0]
if a[2]=="close"{guard let b=attr(w,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID(),AXUIElementPerformAction(b as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Owned close refused")};print("Owned ProgressMeter closed");exit(0)}
var rows:[[String:Any]]=[]
func walk(_ e:AXUIElement,_ depth:Int){guard depth<30 else{return};let children=attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? [];let role=string(e,kAXRoleAttribute);if role=="AXProgressIndicator"{rows.append(["id":string(e,kAXIdentifierAttribute),"role":role,"title":string(e,kAXTitleAttribute),"description":string(e,kAXDescriptionAttribute),"value":attr(e,kAXValueAttribute)as?NSNumber ?? -1,"minimum":attr(e,kAXMinValueAttribute)as?NSNumber ?? -1,"maximum":attr(e,kAXMaxValueAttribute)as?NSNumber ?? -1,"children":children.count])};for c in children{walk(c,depth+1)}}
walk(w,0);guard rows.count==30 else{fail("Expected thirty native progress indicators, got \(rows.count)")};try JSONSerialization.data(withJSONObject:["pid":pid,"title":string(w,kAXTitleAttribute),"command":command,"rows":rows],options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:a[3]));print("Recorded native AX progress indicators: \(rows.count)")
