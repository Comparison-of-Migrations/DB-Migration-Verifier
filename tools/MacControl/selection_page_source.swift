import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n) as? String ?? ""}
let a=CommandLine.arguments;guard a.count==3,["guard","activate","close","cleanup"].contains(a[1]) else{fail("Selection source-only mode required")}
let root=URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier"),lease=try JSONSerialization.jsonObject(with:Data(contentsOf:root.appendingPathComponent("docs/replica/ENVIRONMENT_LEASE.json"))) as![String:Any]
guard (((lease["resources"] as?[String:Any])?["macos"] as?[String:Any])?["owner"] as?String)=="preflight" else{fail("Source Mac not owned")}
let title="SelectionPage · original isolation",url="http://10.211.55.2:8768/selectionpage.html?os=mac&record=1&batch="+URL(fileURLWithPath:a[2]).lastPathComponent,apps=NSRunningApplication.runningApplications(withBundleIdentifier:"com.google.Chrome").filter{$0.bundleURL?.standardizedFileURL.path=="/Applications/Google Chrome.app"}
let owned=apps.flatMap{app in (attr(AXUIElementCreateApplication(app.processIdentifier),kAXWindowsAttribute) as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute).hasPrefix(title)&&text($0,kAXDocumentAttribute)==url}}
let out=URL(fileURLWithPath:a[2]);try FileManager.default.createDirectory(at:out,withIntermediateDirectories:true)
if a[1]=="guard"{guard owned.isEmpty else{fail("Own source already exists")};exit(0)}
if a[1]=="activate"{guard owned.count==1,let w=owned.first else{fail("Own source not ready to activate")};var pid:pid_t=0;AXUIElementGetPid(w,&pid);guard let app=NSRunningApplication(processIdentifier:pid),app.activate(options:[]) else{fail("Own source activation failed")};guard AXUIElementPerformAction(w,kAXRaiseAction as CFString) == .success else{fail("Own source raise failed")};print("Exact source foreground");exit(0)}
if a[1]=="close"{guard owned.count==1,let w=owned.first,let b=attr(w,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID(),AXUIElementPerformAction(b as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Exact Selection source close guard failed")};print("Owned Selection source closed");exit(0)}
guard owned.isEmpty else{fail("Owned Selection source remains")};try JSONSerialization.data(withJSONObject:["sourceWindowCount":0,"exactTitle":title,"exactURL":url]).write(to:out.appendingPathComponent("source-cleanup.json"));print("Owned source0")
