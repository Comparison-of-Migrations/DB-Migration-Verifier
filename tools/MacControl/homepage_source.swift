// Approved native fallback for the exact original Home fixture; geometry remains the IAB evidence.
import AppKit
import ApplicationServices
import Foundation
var release:CGPoint?
func fail(_ s:String)->Never{if let p=release{CGEvent(mouseEventSource:nil,mouseType:.leftMouseUp,mouseCursorPosition:p,mouseButton:.left)?.post(tap:.cghidEventTap)};fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func str(_ e:AXUIElement,_ n:String)->String{attr(e,n) as? String ?? ""}
func rect(_ e:AXUIElement)->CGRect?{guard let a=attr(e,kAXPositionAttribute),let b=attr(e,kAXSizeAttribute),CFGetTypeID(a)==AXValueGetTypeID(),CFGetTypeID(b)==AXValueGetTypeID() else{return nil};var p=CGPoint.zero,s=CGSize.zero;guard AXValueGetValue(a as! AXValue,.cgPoint,&p),AXValueGetValue(b as! AXValue,.cgSize,&s) else{return nil};return CGRect(origin:p,size:s)}
func textRect(_ e:AXUIElement)->CGRect?{var range=CFRange(location:0,length:str(e,kAXValueAttribute).utf16.count);guard range.length>0,let value=AXValueCreate(.cfRange,&range) else{return rect(e)};var result:CFTypeRef?;guard AXUIElementCopyParameterizedAttributeValue(e,kAXBoundsForRangeParameterizedAttribute as CFString,value,&result) == .success,let result,CFGetTypeID(result)==AXValueGetTypeID() else{return rect(e)};var box=CGRect.zero;return AXValueGetValue(result as! AXValue,.cgRect,&box) ? box:rect(e)}
let modes=["inspect","close","passive-help","passive-hint","passive-gap","language","callbacks"]
let args=CommandLine.arguments;guard args.count==2,modes.contains(args[1]) else{fail("Fixed source scope required")};let mode=args[1],root="/Users/youptebook/workspace/DB-Migration-Verifier/"
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:root+"docs/replica/ENVIRONMENT_LEASE.json"))) as! [String:Any];guard ((lease["resources"] as? [String:Any])?["macos"] as? [String:Any])?["owner"] as? String == "home" else{fail("Mac source resource required")}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"com.google.Chrome");guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Applications/Google Chrome.app" else{fail("Official Chrome required")}
let ax=AXUIElementCreateApplication(app.processIdentifier),wins=(attr(ax,kAXWindowsAttribute) as? [AXUIElement] ?? []).filter{str($0,kAXTitleAttribute).hasPrefix("HomePage · original isolation") && str($0,kAXDocumentAttribute)=="http://10.211.55.2:8768/homepage.html"};guard wins.count==1,let w=wins.first,let frame=rect(w) else{fail("Exact unique source title/URL required")}
if mode=="close"{guard let b=attr(w,kAXCloseButtonAttribute),CFGetTypeID(b)==AXUIElementGetTypeID(),AXUIElementPerformAction(b as! AXUIElement,kAXPressAction as CFString) == .success else{fail("Owned close failed")};print("Own source closed");exit(0)}
var nodes:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ d:Int){guard d<40 else{return};nodes.append(e);for c in attr(e,kAXChildrenAttribute) as? [AXUIElement] ?? []{visit(c,d+1)}}
visit(w,0)
func name(_ e:AXUIElement)->String{let a=str(e,kAXTitleAttribute),b=str(e,kAXDescriptionAttribute),c=str(e,kAXValueAttribute);return !a.isEmpty ? a:(!b.isEmpty ? b:c)}
func one(_ names:[String],_ role:String)->AXUIElement{let matches=nodes.filter{names.contains(name($0)) && str($0,kAXRoleAttribute)==role};guard matches.count==1 else{fail("Exact source control required: "+names.joined(separator:"/")+" matches\(matches.count)")};return matches[0]}
if mode=="inspect"{let data=nodes.map{e->[String:Any] in var v:[String:Any]=["role":str(e,kAXRoleAttribute),"name":name(e),"title":str(e,kAXTitleAttribute),"description":str(e,kAXDescriptionAttribute),"value":str(e,kAXValueAttribute),"id":str(e,kAXIdentifierAttribute)];if let b=rect(e){v["box"]=["x":b.minX,"y":b.minY,"width":b.width,"height":b.height]};return v};try JSONSerialization.data(withJSONObject:data,options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:root+"docs/replica/source/homepage-mac-source-native-ax.json"));print("Source AX recorded");exit(0)}
app.activate(options:[]);AXUIElementPerformAction(w,kAXRaiseAction as CFString);RunLoop.current.run(until:Date(timeIntervalSinceNow:0.15))
func foreground(){guard NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier else{fail("Owned Chrome lost foreground")}}
foreground();let old=CGEvent(source:nil)?.location
func send(_ type:CGEventType,_ p:CGPoint){guard let e=CGEvent(mouseEventSource:nil,mouseType:type,mouseCursorPosition:p,mouseButton:.left) else{fail("Pointer unavailable")};e.post(tap:.cghidEventTap)}
func click(_ p:CGPoint){guard frame.contains(p) else{fail("Outside owned source frame")};send(.mouseMoved,p);Thread.sleep(forTimeInterval:0.12);foreground();var hit:AXUIElement?;guard AXUIElementCopyElementAtPosition(AXUIElementCreateSystemWide(),Float(p.x),Float(p.y),&hit) == .success,let hit else{fail("Source hit missing")};var pid:pid_t=0;AXUIElementGetPid(hit,&pid);guard pid==app.processIdentifier else{fail("Hit another application")};release=p;send(.leftMouseDown,p);Thread.sleep(forTimeInterval:0.06);send(.leftMouseUp,p);release=nil;Thread.sleep(forTimeInterval:0.25)}
defer{if let release{send(.leftMouseUp,release)};if NSWorkspace.shared.frontmostApplication?.processIdentifier==app.processIdentifier,let old{send(.mouseMoved,old)}}
if mode=="language"{let b=one(["Toggle Home fixture"],"AXButton");guard AXUIElementPerformAction(b,kAXPressAction as CFString) == .success else{fail("Own language setup failed")};print("Own source toggle");exit(0)}
let start=one(["새 검사 시작","Start a new check"],"AXButton")
if mode.hasPrefix("passive-"){
    guard AXUIElementSetAttributeValue(start,kAXFocusedAttribute as CFString,kCFBooleanTrue) == .success else{fail("Source button focus required")};Thread.sleep(forTimeInterval:0.1)
    guard let f=attr(ax,kAXFocusedUIElementAttribute),CFGetTypeID(f)==AXUIElementGetTypeID(),["새 검사 시작","Start a new check"].contains(name(f as! AXUIElement)) else{fail("Exact source focus required")}
    // Original ko/en960 DOM boxes were recorded while scrollY=0. Chrome's static-text
    // AX reports clipped height1, so anchor the observed offsets to the fresh button.
    guard let b=rect(start),b.width>=223,b.width<=225,b.height>=51,b.height<=53 else{fail("Fresh original224x52 anchor required")}
    let dx=mode=="passive-help" ? 325.0:342.0
    let dy=mode=="passive-help" ? -56.5:(mode=="passive-hint" ? 75.59765625:55.09765625)
    try? Data(contentsOf:URL(fileURLWithPath:root+"docs/replica/source/home-homepage-mac-input.json")).write(to:URL(fileURLWithPath:root+"docs/replica/source/homepage-mac-before-"+mode+".json"))
    let point=CGPoint(x:b.minX+dx,y:b.minY+dy)
    print("Original passive anchor \(b) point \(point)")
    click(point);print("Passive original pointer complete");exit(0)
}
let targets=[["새 검사 시작","Start a new check"],["검사 결과 파일로 보기","View a result file"],["예시 결과 보기","See sample results"],["연구 반영 보기","Research mapping"]]
for names in targets{let b=one(names,"AXButton");guard AXUIElementSetAttributeValue(b,kAXFocusedAttribute as CFString,kCFBooleanTrue) == .success else{fail("Source callback focus required")};Thread.sleep(forTimeInterval:0.1);guard let r=rect(b),r.width>90,r.width<230,r.height>=43,r.height<=53 else{fail("Source callback geometry required")};click(CGPoint(x:r.midX,y:r.midY));for key:CGKeyCode in [36,49]{guard AXUIElementSetAttributeValue(b,kAXFocusedAttribute as CFString,kCFBooleanTrue) == .success else{fail("Exact source key focus required")};foreground();guard let down=CGEvent(keyboardEventSource:nil,virtualKey:key,keyDown:true),let up=CGEvent(keyboardEventSource:nil,virtualKey:key,keyDown:false) else{fail("Source key unavailable")};down.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.06);up.post(tap:.cghidEventTap);Thread.sleep(forTimeInterval:0.25)}}
print("Original callbacks12 complete")
