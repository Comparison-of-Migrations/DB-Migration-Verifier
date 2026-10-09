// Original footer measurement through an owned Chrome window at its real backing scale.
import AppKit
import ApplicationServices
import Foundation
let expectedURL="http://10.211.55.2:8768/actionfooter.html"
let args=CommandLine.arguments
guard args.count==2,args[1]=="inspect" || ["1000","1138","1280"].contains(args[1])else{fputs("Fixed footer viewport required\n",stderr);exit(1)}
let width=Int(args[1]) ?? 0
func fail(_ message:String)->Never{fputs(message+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID()else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as! AXValue,.cgPoint,&p),AXValueGetValue(b as! AXValue,.cgSize,&s)else{return nil};return CGRect(origin:p,size:s)}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"com.google.Chrome")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Applications/Google Chrome.app"else{fail("Owned Chrome path required")}
let root=AXUIElementCreateApplication(app.processIdentifier)
let windows=(attr(root,kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute).hasPrefix("ActionFooter · original isolation")&&text($0,kAXDocumentAttribute)==expectedURL}
guard windows.count==1,let window=windows.first,let original=rect(window)else{fail("Unique owned footer window/URL required")}
var docs:[AXUIElement]=[],records:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ depth:Int){guard depth<30 else{return};if text(e,kAXRoleAttribute)=="AXWebArea"&&text(e,kAXTitleAttribute)=="ActionFooter · original isolation"{docs.append(e)};if text(e,kAXRoleAttribute)=="AXButton"&&(text(e,kAXTitleAttribute)=="Record footer"||text(e,kAXDescriptionAttribute)=="Record footer"){records.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,depth+1)}}
visit(window,0)
guard docs.count==1,records.count==1,let document=rect(docs[0])else{fail("Owned footer document/record required")}
if args[1]=="inspect"{print("Owned footer window",original,"document",document);exit(0)}
var settable:DarwinBoolean=false
guard AXUIElementIsAttributeSettable(window,kAXSizeAttribute as CFString,&settable) == .success,settable.boolValue else{fail("Owned footer window cannot resize")}
let extra=original.width-document.width
var size=CGSize(width:Double(width)+extra,height:original.height)
guard let value=AXValueCreate(.cgSize,&size),AXUIElementSetAttributeValue(window,kAXSizeAttribute as CFString,value) == .success else{fail("Owned footer resize failed")}
let layoutDeadline=Date(timeIntervalSinceNow:2)
while Date()<layoutDeadline{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.05));if let current=rect(docs[0]),abs(current.width-Double(width))<0.01{break}}
guard text(window,kAXDocumentAttribute)==expectedURL,let resized=rect(docs[0]),abs(resized.width-Double(width))<0.01 else{fail("Owned footer viewport differs after resize: beforeWindow=\(original.width) beforeDocument=\(document.width) nowWindow=\(rect(window)?.width ?? -1) nowDocument=\(rect(docs[0])?.width ?? -1) requested=\(width)")}
let rootPath="/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/source/"
let source=URL(fileURLWithPath:rootPath+"windows-source-sidebar.json")
let prior=(try FileManager.default.attributesOfItem(atPath:source.path)[.modificationDate]as?Date) ?? .distantPast
guard AXUIElementPerformAction(records[0],kAXPressAction as CFString) == .success else{fail("Owned footer record refused")}
let deadline=Date(timeIntervalSinceNow:4)
var bytes:Data?
while Date()<deadline{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.05));if let attrs=try?FileManager.default.attributesOfItem(atPath:source.path),let modified=attrs[.modificationDate]as?Date,modified>prior{bytes=try Data(contentsOf:source);break}}
guard let bytes,let result=try JSONSerialization.jsonObject(with:bytes)as?[String:Any],result["dpr"]as?Int==2,result["viewport"]as?Int==width,(result["rows"]as?[[String:Any]])?.count==44 else{fail("No fresh footer viewport/DPR2/44 evidence")}
try bytes.write(to:URL(fileURLWithPath:rootPath+"actionfooter-mac-source-\(width).json"),options:.atomic)
print("Fresh original footer: viewport",width,"DPR2 44 branches")
