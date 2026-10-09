import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n) as? String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let p=attr(e,kAXPositionAttribute),let s=attr(e,kAXSizeAttribute),CFGetTypeID(p)==AXValueGetTypeID(),CFGetTypeID(s)==AXValueGetTypeID()else{return nil};var pp=CGPoint.zero,ss=CGSize.zero;guard AXValueGetValue(p as!AXValue,.cgPoint,&pp),AXValueGetValue(s as!AXValue,.cgSize,&ss)else{return nil};return CGRect(origin:pp,size:ss)}
let a=CommandLine.arguments;guard a.count==3,["guard","activate","inspect","language","expand-data","category-data","search-orders","deselect","bulk","mapping","escape","clear-search","search-none","passive","category-trial","trial","back","scroll-table","scroll-top","close","cleanup"].contains(a[1])else{fail("Fixed own Selection source input mode required")}
let root=URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier"),out=URL(fileURLWithPath:a[2]).standardizedFileURL,mode=a[1],title="SelectionPage Input · original isolation",url="http://10.211.55.2:8768/selectionpage-input.html?os=mac&batch="+out.lastPathComponent+"&resume=query-none"
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:root.appendingPathComponent("docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any];guard (((lease["resources"]as?[String:Any])?["macos"]as?[String:Any])?["owner"]as?String)=="preflight"else{fail("Own Mac Selection lease absent")}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"com.google.Chrome").filter{$0.bundleURL?.standardizedFileURL.path=="/Applications/Google Chrome.app"}
func owned()->[AXUIElement]{apps.flatMap{(attr(AXUIElementCreateApplication($0.processIdentifier),kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute).hasPrefix(title)&&text($0,kAXDocumentAttribute)==url}}}
func save(_ label:String,_ data:Any)throws{try JSONSerialization.data(withJSONObject:data,options:[.prettyPrinted,.sortedKeys]).write(to:out.appendingPathComponent(label+".json"))}
if mode=="guard"{guard owned().isEmpty else{fail("Own source already exists")};exit(0)}
if mode=="cleanup"{guard owned().isEmpty else{fail("Own source remains")};try save("source-cleanup",["sourceWindowCount":0,"url":url,"title":title]);exit(0)}
guard owned().count==1,let w=owned().first else{fail("Unique exact source title/URL missing")};var pid:pid_t=0;AXUIElementGetPid(w,&pid);guard let app=NSRunningApplication(processIdentifier:pid),let frame=rect(w)else{fail("Exact source PID/frame absent")}
if mode=="close"{guard let b=attr(w,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID(),AXUIElementPerformAction(b as!AXUIElement,kAXPressAction as CFString) == .success else{fail("Own source AXClose refused")};exit(0)}
func activate(){guard app.activate(options:[])else{fail("Own activation failed")};guard AXUIElementPerformAction(w,kAXRaiseAction as CFString) == .success else{fail("Own raise refused")};RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))}
if mode=="activate"{activate();exit(0)}
var nodes:[AXUIElement]=[]
func walk(_ e:AXUIElement,_ d:Int){guard d<25,nodes.count<2000,!nodes.contains(where:{CFEqual($0,e)})else{return};nodes.append(e);for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{walk(c,d+1)}};walk(w,0)
func name(_ e:AXUIElement)->String{for k in [kAXTitleAttribute,kAXDescriptionAttribute,kAXValueAttribute]{let s=text(e,k);if !s.isEmpty{return s}};return ""}
func tree(_ e:AXUIElement,_ d:Int)->[String:Any]{var v:[String:Any]=["role":text(e,kAXRoleAttribute),"id":text(e,kAXIdentifierAttribute),"title":text(e,kAXTitleAttribute),"description":text(e,kAXDescriptionAttribute),"value":text(e,kAXValueAttribute),"focused":attr(e,kAXFocusedAttribute)as?Bool ?? false];if let b=rect(e){v["box"]=["x":b.minX,"y":b.minY,"width":b.width,"height":b.height]};if d<20{var unique:[AXUIElement]=[];for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{if !unique.contains(where:{CFEqual($0,c)}){unique.append(c)}};v["children"]=unique.prefix(80).map{tree($0,d+1)}};return v}
let record=root.appendingPathComponent("docs/replica/source/preflight-selectionpage-mac-input.json")
if let web=nodes.last(where:{text($0,kAXRoleAttribute)=="AXWebArea"}){nodes=[];walk(web,0)}
let before=try Data(contentsOf:record),old=try JSONSerialization.jsonObject(with:before)as![String:Any];guard old["batch"]as?String==out.lastPathComponent else{fail("Fresh own source record required")}
let serial=(old["sequence"]as?Int) ?? 0
if mode=="inspect"{let web=nodes.filter{text($0,kAXRoleAttribute)=="AXWebArea"};try save("source-ax-"+((old["language"]as?String) ?? "unknown"),tree(web.last ?? w,0));try before.write(to:out.appendingPathComponent("source-inspect-"+((old["language"]as?String) ?? "unknown")+".json"));exit(0)}
