import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
let args=CommandLine.arguments;guard args.count==3,["count","close"].contains(args[1])else{fail("Fixed Preflight source mode required")}
let root=URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier"),lease=try JSONSerialization.jsonObject(with:Data(contentsOf:root.appendingPathComponent("docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any]
guard (((lease["resources"]as?[String:Any])?["macos"]as?[String:Any])?["owner"]as?String)=="preflight"else{fail("Mac source resource not owned")}
let url="http://10.211.55.2:8768/preflightpage.html?os=mac&record=1",title="PreflightPage · original isolation"
func owned()->[AXUIElement]{var result:[AXUIElement]=[];for app in NSRunningApplication.runningApplications(withBundleIdentifier:"com.google.Chrome").filter({$0.bundleURL?.standardizedFileURL.path=="/Applications/Google Chrome.app"}){let ax=AXUIElementCreateApplication(app.processIdentifier);result += (attr(ax,kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute).hasPrefix(title)&&text($0,kAXDocumentAttribute)==url}};return result}
let before=owned();if args[1]=="close"{guard before.count==1,let window=before.first,let b=attr(window,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID(),AXUIElementPerformAction(b as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Unique owned Preflight source close refused")};let end=Date(timeIntervalSinceNow:3);while !owned().isEmpty&&Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.05))}}
let count=owned().count;if args[1]=="close"&&count != 0{fail("Owned source remains")};let data=try JSONSerialization.data(withJSONObject:["sourceWindowCount":count,"url":url,"title":title],options:.sortedKeys);try data.write(to:URL(fileURLWithPath:args[2]));print(String(data:data,encoding:.utf8)!)
