// §14: exact historical ActionFooter measurement window only; no activation.
import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
let a=CommandLine.arguments,root="/Users/youptebook/workspace/DB-Migration-Verifier/"
guard a.count==2,a[1].hasPrefix(root+"artifacts/replica/workers/root/")else{fail("Owned evidence path required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:root+"docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any]
guard ((lease["resources"]as![String:Any])["macos"]as![String:Any])["owner"]as?String=="root"else{fail("Root Mac lease required")}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"com.google.Chrome")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Applications/Google Chrome.app"else{fail("Official Chrome required")}
func owned()->[AXUIElement]{(attr(AXUIElementCreateApplication(app.processIdentifier),kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{(attr($0,kAXTitleAttribute)as?String ?? "").hasPrefix("ActionFooter · original isolation")&&(attr($0,kAXDocumentAttribute)as?String)=="http://10.211.55.2:8768/actionfooter.html"}}
let windows=owned();guard windows.count<=1 else{fail("Historical source ambiguous")}
var closed=false
if let w=windows.first{
 guard let b=attr(w,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID(),AXUIElementPerformAction(b as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Exact source close refused")}
 closed=true;let end=Date(timeIntervalSinceNow:3);while !owned().isEmpty&&Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.03))}
}
guard owned().isEmpty else{fail("Historical source remains")}
try JSONSerialization.data(withJSONObject:["component":"ActionFooter","locked":true,"chromePID":app.processIdentifier,"closeRequested":closed,"sourceWindow":0,"url":"http://10.211.55.2:8768/actionfooter.html","method":"exact title/URL AXClose without activation; other tabs untouched"],options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:a[1]))
print("Completed ActionFooter source window 0")
