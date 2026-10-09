// Exact original Progress fixture; project-owned measurements only.
import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let p=attr(e,kAXPositionAttribute),let s=attr(e,kAXSizeAttribute),CFGetTypeID(p)==AXValueGetTypeID(),CFGetTypeID(s)==AXValueGetTypeID()else{return nil};var a=CGPoint.zero,b=CGSize.zero;guard AXValueGetValue(p as!AXValue,.cgPoint,&a),AXValueGetValue(s as!AXValue,.cgSize,&b)else{return nil};return CGRect(origin:a,size:b)}
let a=CommandLine.arguments,root="/Users/youptebook/workspace/DB-Migration-Verifier/",url="http://10.211.55.2:8768/progresspage-input.html?os=mac"
guard a.count==3,["prepare","inspect","hover","leave","tick","language","record","close"].contains(a[1]),a[2].hasPrefix(root+"artifacts/replica/workers/root/")else{fail("Fixed source scope required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:root+"docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any]
guard ((lease["resources"]as![String:Any])["macos"]as![String:Any])["owner"]as?String=="root"else{fail("Root Mac lease required")}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"com.google.Chrome");guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Applications/Google Chrome.app"else{fail("Official Chrome required")}
func owned()->[AXUIElement]{(attr(AXUIElementCreateApplication(app.processIdentifier),kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute).hasPrefix("Progress input · original isolation")&&[url,url+"&lang=en"].contains(text($0,kAXDocumentAttribute))}}
guard owned().count==1,let w=owned().first,let frame=rect(w)else{fail("Exact source title/URL/window required")}
if a[1]=="prepare"{
 var size=CGSize(width:1400,height:950),position=CGPoint(x:80,y:60)
 guard let s=AXValueCreate(.cgSize,&size),let p=AXValueCreate(.cgPoint,&position),AXUIElementSetAttributeValue(w,kAXSizeAttribute as CFString,s) == .success,AXUIElementSetAttributeValue(w,kAXPositionAttribute as CFString,p) == .success else{fail("Owned source window geometry preparation refused")}
 print("Prepared owned original viewport; inspect fresh geometry before input");exit(0)
}
if a[1]=="close"{
 guard let b=attr(w,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID(),AXUIElementPerformAction(b as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Scoped source close refused")}
 let end=Date(timeIntervalSinceNow:3);while !owned().isEmpty&&Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.03))};guard owned().isEmpty else{fail("Source still open")}
 try JSONSerialization.data(withJSONObject:["sourceWindow":0,"url":url,"method":"exact title/URL AXClose"],options:[.prettyPrinted]).write(to:URL(fileURLWithPath:a[2]));exit(0)
}
var nodes:[AXUIElement]=[]
func walk(_ e:AXUIElement,_ d:Int){guard d<25 else{return};nodes.append(e);for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{walk(c,d+1)}}
walk(w,0)
if a[1]=="inspect"{
 let rows=nodes.map{e->[String:Any]in var x:[String:Any]=["role":text(e,kAXRoleAttribute),"title":text(e,kAXTitleAttribute),"description":text(e,kAXDescriptionAttribute),"value":text(e,kAXValueAttribute)];if let b=rect(e){x["box"]=["x":b.minX,"y":b.minY,"width":b.width,"height":b.height]};return x}
 try JSONSerialization.data(withJSONObject:["rows":rows,"pid":app.processIdentifier,"url":url],options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:a[2]));exit(0)
}
app.activate(options:[]);AXUIElementPerformAction(w,kAXRaiseAction as CFString);let end=Date(timeIntervalSinceNow:2);while NSWorkspace.shared.frontmostApplication?.processIdentifier != app.processIdentifier&&Date()<end{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.02))};guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Own source foreground required")}
if a[1]=="hover"||a[1]=="leave"{
 let tables=nodes.filter{text($0,kAXRoleAttribute)=="AXTable"};guard tables.count==1,let table=tables.first else{fail("Unique source Table required")}
 let rows=(attr(table,kAXChildrenAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXRoleAttribute)=="AXRow"};guard rows.count==5,let b=rect(rows[1]),b.width>100,b.height>20 else{fail("Five original rows with measurable first body required")}
 let point=a[1]=="hover" ? CGPoint(x:b.minX+b.width*0.27,y:b.midY):CGPoint(x:frame.maxX-80,y:frame.minY+180)
 guard frame.contains(point)else{fail("Owned source point required")};CGEvent(mouseEventSource:nil,mouseType:.mouseMoved,mouseCursorPosition:point,mouseButton:.left)?.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.25)
 var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(point.x),Float(point.y),&hit) == .success,let hit else{fail("Source hit missing")};var pid:pid_t=0;AXUIElementGetPid(hit,&pid);guard pid==app.processIdentifier else{fail("Source hit another app")}
}
let names=a[1]=="tick" ? ["Record first live update"]:a[1]=="language" ? ["Language"]:["Record current state","Progress input recorded"]
let buttons=nodes.filter{text($0,kAXRoleAttribute)=="AXButton"&&names.contains(text($0,kAXTitleAttribute))}
guard buttons.count==1,let b=buttons.first,let br=rect(b),frame.contains(br),AXUIElementPerformAction(b,kAXPressAction as CFString) == .success else{fail("Exact original measurement control required")}
print("Original Progress source action",a[1])
