// Close only the historical, LOCKED CategoryHeader fixture after evidence preservation.
import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
let a=CommandLine.arguments,root="/Users/youptebook/workspace/DB-Migration-Verifier/"
guard a.count==2,a[1].hasPrefix(root+"artifacts/replica/workers/root/")else{fail("Owned cleanup evidence path required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:root+"docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any];guard ((lease["resources"]as![String:Any])["macos"]as![String:Any])["owner"]as?String=="root"else{fail("Root Mac lease required")}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.categoryheadercheck");guard apps.count<=1 else{fail("Historical fixture app ambiguous")}
var pid:Int32=0,closed=false
if let app=apps.first{guard app.bundleURL?.path==root+"artifacts/replica/CategoryHeaderCheck.app"else{fail("Exact historical project bundle required")};pid=app.processIdentifier;let window=AXUIElementCreateApplication(pid);let windows=attr(window,kAXWindowsAttribute)as?[AXUIElement] ?? [];let owned=windows.filter{attr($0,kAXTitleAttribute)as?String=="Replica CategoryHeader verification"};guard owned.count==1,windows.count==1,let w=owned.first,let b=attr(w,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID()else{fail("Exact single historical verification window required")};guard AXUIElementPerformAction(b as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Scoped close rejected")};closed=true;let end=Date(timeIntervalSinceNow:5);while NSRunningApplication(processIdentifier:pid)?.isTerminated==false&&Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.03))};guard NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.categoryheadercheck").isEmpty else{fail("Historical fixture process still running")}}
try JSONSerialization.data(withJSONObject:["component":"CategoryHeader","locked":true,"pid":pid,"closeRequested":closed,"nativeProcess":0,"nativeWindow":0,"method":"exact bundle/PID/title AXClose without activation"],options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:a[1]));print("Completed CategoryHeader fixture closed; process/window 0")
