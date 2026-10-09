// Read-only actual AX on one exact frozen project window. No pointer or keyboard events.
import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ name:String)->CFTypeRef?{var value:CFTypeRef?;return AXUIElementCopyAttributeValue(e,name as CFString,&value) == .success ? value:nil}
func text(_ e:AXUIElement,_ name:String)->String{attr(e,name)as?String ?? ""}
let args=CommandLine.arguments;guard args.count==5,let pid=Int32(args[1]),["inspect","close"].contains(args[2])else{fail("PID operation exactBinary output required")}
let binary=args[3];let flag="actionbuttonsemantic";let title="Replica Action button semantics verification";guard binary.contains("/artifacts/replica/workers/root/actionbuttonsemantic/"),binary.hasSuffix("/ReplicaProbe.dll")else{fail("Owned frozen binary required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any];guard ((lease["resources"]as![String:Any])["macos"]as![String:Any])["owner"]as?String == "root"else{fail("Root Mac lease required")}
let process=Process();process.executableURL=URL(fileURLWithPath:"/bin/ps");process.arguments=["-p",String(pid),"-o","command="];let pipe=Pipe();process.standardOutput=pipe;try process.run();process.waitUntilExit();let command=String(data:pipe.fileHandleForReading.readDataToEndOfFile(),encoding:.utf8) ?? "";guard command.contains(binary+" --"+flag+"-window")else{fail("Exact frozen native command required")}
let app=AXUIElementCreateApplication(pid);let windows=(attr(app,kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute)==title};guard windows.count==1 else{fail("Unique owned window required")};let window=windows[0]
if args[2]=="close"{guard let close=attr(window,kAXCloseButtonAttribute),CFGetTypeID(close)==AXUIElementGetTypeID(),AXUIElementPerformAction(close as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Owned close refused")};print("Owned heading window closed");exit(0)}
var rows:[[String:Any]]=[]
func walk(_ node:AXUIElement,_ depth:Int){guard depth<30 else{return};let children=attr(node,kAXChildrenAttribute)as?[AXUIElement] ?? [];let id=text(node,kAXIdentifierAttribute);if id.hasPrefix("ButtonSemantics-"){rows.append(["id":id,"role":text(node,kAXRoleAttribute),"title":text(node,kAXTitleAttribute),"description":text(node,kAXDescriptionAttribute),"value":(attr(node,kAXValueAttribute)as?NSNumber as Any?) ?? (attr(node,kAXValueAttribute)as?String as Any?) ?? -1,"children":children.count])};for child in children{walk(child,depth+1)}}
walk(window,0);guard rows.count==8 else{fail("Four scoped text peers required, got \(rows.count)")};try JSONSerialization.data(withJSONObject:["pid":pid,"command":command,"title":text(window,kAXTitleAttribute),"rows":rows],options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:args[4]));print("Recorded four actual heading/text peers")
