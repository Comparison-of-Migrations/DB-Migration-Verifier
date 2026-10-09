import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
let args=CommandLine.arguments;guard args.count==4,["inspect","close"].contains(args[1])else{fail("Fixed hint operation required")};let mode=args[1],expectedPath=args[2],output=args[3];let leaseURL=URL(fileURLWithPath:"/Users/youptebook/workspace/DB-Migration-Verifier/docs/replica/ENVIRONMENT_LEASE.json");guard let lease=try? JSONSerialization.jsonObject(with:Data(contentsOf:leaseURL)) as? [String:Any],let resources=lease["resources"] as? [String:Any],let mac=resources["macos"] as? [String:Any],mac["owner"] as? String == "home",expectedPath.hasPrefix("/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/workers/home/welcomeentryhint/"),expectedPath.hasSuffix("/WelcomeEntryHintCheck.app"),output.hasPrefix("/Users/youptebook/workspace/DB-Migration-Verifier/")else{fail("Owned hint resource/path required")}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.welcomeentryhintcheck");guard apps.count==1,let app=apps.first,app.bundleURL?.path==expectedPath else{fail("Unique owned hint app required")};let ax=AXUIElementCreateApplication(app.processIdentifier);let wins=(attr(ax,kAXWindowsAttribute)as?[AXUIElement] ?? []).filter{text($0,kAXTitleAttribute)=="Replica WelcomeEntryHint verification"};guard wins.count==1,let w=wins.first else{fail("Unique hint window required")}
if mode=="close"{guard let close=attr(w,kAXCloseButtonAttribute),CFGetTypeID(close)==AXUIElementGetTypeID(),AXUIElementPerformAction(close as! AXUIElement,kAXPressAction as CFString) == .success else{fail("Owned close failed")};print("Owned hint closed");exit(0)}

var allRows:[[String:Any]]=[]
let expectedNames=["저장한 결과 JSON은 DB 연결이나 새 검사 없이 바로 열어 확인할 수 있습니다.","Open a saved result JSON without connecting to a DB or running a new check.","저장한 결과 JSON은 DB 연결이나 새 검사 없이 바로 열어 확인할 수 있습니다."]
for (index,expected) in expectedNames.enumerated(){
 var texts:[AXUIElement]=[],toggles:[AXUIElement]=[]
 func visit(_ e:AXUIElement,_ depth:Int){guard depth<30 else{return};if text(e,kAXRoleAttribute)=="AXStaticText" && expectedNames.contains(text(e,kAXValueAttribute)){texts.append(e)};if text(e,kAXIdentifierAttribute)=="WelcomeEntryHint-toggle"{toggles.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,depth+1)}};visit(w,0)
 guard texts.count==1,text(texts[0],kAXValueAttribute)==expected else{fail("Actual hint peer mismatch")};allRows.append(["name":expected,"textPeerCount":1,"role":"AXStaticText"])
 if index<2{guard toggles.count==1,AXUIElementPerformAction(toggles[0],kAXPressAction as CFString) == .success else{fail("Owned utility missing")};RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2))}
}
try JSONSerialization.data(withJSONObject:["textPeers":allRows,"pid":app.processIdentifier,"title":text(w,kAXTitleAttribute)],options:.prettyPrinted).write(to:URL(fileURLWithPath:output));print("Actual hint Text peer1 KOENKO decorative icon excluded")
